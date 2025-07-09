using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Silk.NET.Vulkan;
using Silk.NET.Core.Native;

namespace STLViewer.UI.Controls
{
    public unsafe partial class VulkanRenderer
    {
        private void CreateInstance()
        {
            var appInfo = new ApplicationInfo
            {
                SType = StructureType.ApplicationInfo,
                PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("STL Viewer"),
                ApplicationVersion = new Version32(1, 0, 0),
                PEngineName = (byte*)Marshal.StringToHGlobalAnsi("No Engine"),
                EngineVersion = new Version32(1, 0, 0),
                ApiVersion = Vk.Version12
            };

            var extensions = GetRequiredExtensions();
            var createInfo = new InstanceCreateInfo
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &appInfo,
                EnabledExtensionCount = (uint)extensions.Length,
                PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions)
            };

            if (_vk.CreateInstance(in createInfo, null, out _instance) != Result.Success)
            {
                throw new Exception("Failed to create Vulkan instance");
            }
        }

        private string[] GetRequiredExtensions()
        {
            return new[]
            {
                "VK_KHR_surface",
                "VK_KHR_win32_surface" // For Windows
            };
        }

        private void CreateSurface(Control control)
        {
            // Platform-specific surface creation
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var platformHandle = control.TryGetPlatformHandle();
                if (platformHandle == null)
                    throw new Exception("Failed to get platform handle");

                var win32SurfaceCreateInfo = new Win32SurfaceCreateInfoKHR
                {
                    SType = StructureType.Win32SurfaceCreateInfoKhr,
                    Hinstance = Marshal.GetHINSTANCE(typeof(VulkanControl).Module),
                    Hwnd = platformHandle.Handle
                };

                if (_vk.CreateWin32SurfaceKhr(_instance, in win32SurfaceCreateInfo, null, out _surface) != Result.Success)
                {
                    throw new Exception("Failed to create window surface");
                }
            }
            else
            {
                throw new PlatformNotSupportedException("Only Windows is currently supported");
            }
        }

        private void PickPhysicalDevice()
        {
            uint deviceCount = 0;
            _vk.EnumeratePhysicalDevices(_instance, ref deviceCount, null);

            if (deviceCount == 0)
                throw new Exception("Failed to find GPUs with Vulkan support");

            var devices = new PhysicalDevice[deviceCount];
            fixed (PhysicalDevice* devicesPtr = devices)
            {
                _vk.EnumeratePhysicalDevices(_instance, ref deviceCount, devicesPtr);
            }

            foreach (var device in devices)
            {
                if (IsDeviceSuitable(device))
                {
                    _physicalDevice = device;
                    break;
                }
            }

            if (_physicalDevice.Handle == 0)
                throw new Exception("Failed to find a suitable GPU");
        }

        private bool IsDeviceSuitable(PhysicalDevice device)
        {
            var indices = FindQueueFamilies(device);
            bool extensionsSupported = CheckDeviceExtensionSupport(device);

            bool swapChainAdequate = false;
            if (extensionsSupported)
            {
                var swapChainSupport = QuerySwapChainSupport(device);
                swapChainAdequate = swapChainSupport.Formats.Any() && swapChainSupport.PresentModes.Any();
            }

            return indices.IsComplete() && extensionsSupported && swapChainAdequate;
        }

        private void CreateLogicalDevice()
        {
            var indices = FindQueueFamilies(_physicalDevice);
            var uniqueQueueFamilies = new[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value }.Distinct().ToArray();

            using var mem = GlobalMemory.Allocate(uniqueQueueFamilies.Length * sizeof(DeviceQueueCreateInfo));
            var queueCreateInfos = (DeviceQueueCreateInfo*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref mem.GetPinnableReference());

            float queuePriority = 1.0f;
            for (int i = 0; i < uniqueQueueFamilies.Length; i++)
            {
                queueCreateInfos[i] = new()
                {
                    SType = StructureType.DeviceQueueCreateInfo,
                    QueueFamilyIndex = uniqueQueueFamilies[i],
                    QueueCount = 1,
                    PQueuePriorities = &queuePriority
                };
            }

            PhysicalDeviceFeatures deviceFeatures = new();

            var extensions = new[] { "VK_KHR_swapchain" };
            var createInfo = new DeviceCreateInfo
            {
                SType = StructureType.DeviceCreateInfo,
                QueueCreateInfoCount = (uint)uniqueQueueFamilies.Length,
                PQueueCreateInfos = queueCreateInfos,
                PEnabledFeatures = &deviceFeatures,
                EnabledExtensionCount = (uint)extensions.Length,
                PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions)
            };

            if (_vk.CreateDevice(_physicalDevice, in createInfo, null, out _device) != Result.Success)
            {
                throw new Exception("Failed to create logical device");
            }

            _vk.GetDeviceQueue(_device, indices.GraphicsFamily!.Value, 0, out _graphicsQueue);
            _vk.GetDeviceQueue(_device, indices.PresentFamily!.Value, 0, out _presentQueue);
        }

        private QueueFamilyIndices FindQueueFamilies(PhysicalDevice device)
        {
            var indices = new QueueFamilyIndices();

            uint queueFamilyCount = 0;
            _vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilyCount, null);

            var queueFamilies = new QueueFamilyProperties[queueFamilyCount];
            fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
            {
                _vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilyCount, queueFamiliesPtr);
            }

            uint i = 0;
            foreach (var queueFamily in queueFamilies)
            {
                if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
                {
                    indices.GraphicsFamily = i;
                }

                _vk.GetPhysicalDeviceSurfaceSupportKhr(device, i, _surface, out var presentSupport);

                if (presentSupport)
                {
                    indices.PresentFamily = i;
                }

                if (indices.IsComplete())
                    break;

                i++;
            }

            return indices;
        }

        private bool CheckDeviceExtensionSupport(PhysicalDevice device)
        {
            uint extensionCount = 0;
            _vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extensionCount, null);

            var availableExtensions = new ExtensionProperties[extensionCount];
            fixed (ExtensionProperties* availableExtensionsPtr = availableExtensions)
            {
                _vk.EnumerateDeviceExtensionProperties(device, (byte*)null, ref extensionCount, availableExtensionsPtr);
            }

            var requiredExtensions = new HashSet<string> { "VK_KHR_swapchain" };

            foreach (var extension in availableExtensions)
            {
                var extensionName = Marshal.PtrToStringAnsi((IntPtr)extension.ExtensionName);
                requiredExtensions.Remove(extensionName);
            }

            return requiredExtensions.Count == 0;
        }

        private SwapChainSupportDetails QuerySwapChainSupport(PhysicalDevice device)
        {
            var details = new SwapChainSupportDetails();

            _vk.GetPhysicalDeviceSurfaceCapabilitiesKhr(device, _surface, out details.Capabilities);

            uint formatCount = 0;
            _vk.GetPhysicalDeviceSurfaceFormatsKhr(device, _surface, ref formatCount, null);

            if (formatCount != 0)
            {
                details.Formats = new SurfaceFormatKHR[formatCount];
                fixed (SurfaceFormatKHR* formatsPtr = details.Formats)
                {
                    _vk.GetPhysicalDeviceSurfaceFormatsKhr(device, _surface, ref formatCount, formatsPtr);
                }
            }

            uint presentModeCount = 0;
            _vk.GetPhysicalDeviceSurfacePresentModesKhr(device, _surface, ref presentModeCount, null);

            if (presentModeCount != 0)
            {
                details.PresentModes = new PresentModeKHR[presentModeCount];
                fixed (PresentModeKHR* presentModesPtr = details.PresentModes)
                {
                    _vk.GetPhysicalDeviceSurfacePresentModesKhr(device, _surface, ref presentModeCount, presentModesPtr);
                }
            }

            return details;
        }
    }
}
