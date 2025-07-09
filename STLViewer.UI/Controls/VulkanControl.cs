using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using Silk.NET.Vulkan;
using Silk.NET.Core.Native;
using VkBuffer = Silk.NET.Vulkan.Buffer;
using VkImage = Silk.NET.Vulkan.Image;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;
using System.Runtime.CompilerServices;

namespace STLViewer.UI.Controls
{
    public class VulkanControl : NativeControlHost
    {
        private VulkanRenderer? _renderer;
        private bool _isInitialized = false;
        private DispatcherTimer? _renderTimer;
        private bool _isDragging = false;
        private Point _lastMousePosition;

        public event Action<string>? StatusChanged;
        public event Action<string>? RendererInfoChanged;

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            var handle = base.CreateNativeControlCore(parent);

            // Initialize Vulkan renderer after control is created
            Dispatcher.UIThread.Post(InitializeVulkan, DispatcherPriority.Background);

            return handle;
        }

        private async void InitializeVulkan()
        {
            try
            {
                Console.WriteLine("🔧 Creating Vulkan renderer...");
                _renderer = new VulkanRenderer();
                await _renderer.InitializeAsync(this);
                _isInitialized = true;

                var rendererInfo = _renderer.GetInfo();
                Console.WriteLine($"✅ Vulkan renderer initialized: {rendererInfo.Name}");
                Console.WriteLine($"   API Version: {rendererInfo.ApiVersion}");
                Console.WriteLine($"   Device: {rendererInfo.DeviceName}");

                StatusChanged?.Invoke("Vulkan renderer initialized successfully");
                RendererInfoChanged?.Invoke(rendererInfo.Name);

                // Start render loop
                _renderTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
                };
                _renderTimer.Tick += (s, e) =>
                {
                    if (_isInitialized && _renderer != null)
                        _renderer.DrawFrame();
                };
                _renderTimer.Start();

                Console.WriteLine("✅ Vulkan render loop started");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Vulkan initialization failed: {ex.Message}");
                StatusChanged?.Invoke($"Vulkan initialization failed: {ex.Message}");
            }
        }

        public void LoadSTLModel(string filePath)
        {
            if (_isInitialized && _renderer != null)
            {
                _renderer.LoadSTLModel(filePath);
                StatusChanged?.Invoke($"STL model loaded: {Path.GetFileName(filePath)}");
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _isDragging = true;
                _lastMousePosition = e.GetCurrentPoint(this).Position;
                Cursor = new Cursor(StandardCursorType.Hand);
            }
            base.OnPointerPressed(e);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (_isDragging && _renderer != null)
            {
                var currentPosition = e.GetCurrentPoint(this).Position;
                var deltaX = (float)(currentPosition.X - _lastMousePosition.X);
                var deltaY = (float)(currentPosition.Y - _lastMousePosition.Y);

                _renderer.RotateModel(deltaX, deltaY);
                _lastMousePosition = currentPosition;
            }
            base.OnPointerMoved(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            _isDragging = false;
            Cursor = new Cursor(StandardCursorType.Arrow);
            base.OnPointerReleased(e);
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            if (_renderer != null)
            {
                _renderer.ZoomModel((float)-e.Delta.Y);
            }
            base.OnPointerWheelChanged(e);
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            Console.WriteLine("🧹 Destroying Vulkan control...");
            _renderTimer?.Stop();
            _isInitialized = false;
            _renderer?.Cleanup();
            base.DestroyNativeControlCore(control);
        }
    }

    // Complete Vulkan Renderer Implementation
    public unsafe partial class VulkanRenderer
    {
        private Vk _vk;
        private Instance _instance;
        private PhysicalDevice _physicalDevice;
        private Device _device;
        private Queue _graphicsQueue;
        private Queue _presentQueue;
        private SurfaceKHR _surface;
                private SwapchainKHR _swapchain;
        private VkImage[] _swapchainImages;
        private ImageView[] _swapchainImageViews;
        private RenderPass _renderPass;
        private Pipeline _graphicsPipeline;
        private PipelineLayout _pipelineLayout;
        private Framebuffer[] _framebuffers;
        private CommandPool _commandPool;
        private CommandBuffer[] _commandBuffers;
        private VkSemaphore[] _imageAvailableSemaphores;
        private VkSemaphore[] _renderFinishedSemaphores;
        private Fence[] _inFlightFences;

        private VkBuffer _vertexBuffer;
        private DeviceMemory _vertexBufferMemory;
        private VkBuffer _indexBuffer;
        private DeviceMemory _indexBufferMemory;

        private uint _currentFrame = 0;
        private const int MAX_FRAMES_IN_FLIGHT = 2;

        private float _rotationX = 0.0f;
        private float _rotationY = 0.0f;
        private float _zoom = 1.0f;

        // Camera/View matrices
        private Matrix4x4 _modelMatrix = Matrix4x4.Identity;
        private Matrix4x4 _viewMatrix;
        private Matrix4x4 _projectionMatrix;

        private List<Vertex> _vertices = new();
        private List<uint> _indices = new();

        public async Task InitializeAsync(Control control)
        {
            _vk = Vk.GetApi();

            CreateInstance();
            CreateSurface(control);
            PickPhysicalDevice();
            CreateLogicalDevice();
            CreateSwapchain();
            CreateImageViews();
            CreateRenderPass();
            CreateGraphicsPipeline();
            CreateFramebuffers();
            CreateCommandPool();
            CreateVertexBuffer();
            CreateIndexBuffer();
            CreateCommandBuffers();
            CreateSyncObjects();

            SetupCamera();

            // Load a sample triangle for testing
            LoadSampleTriangle();
        }

        public RendererInfo GetInfo()
        {
            PhysicalDeviceProperties properties;
            _vk.GetPhysicalDeviceProperties(_physicalDevice, &properties);

            var deviceName = Marshal.PtrToStringAnsi((nint)properties.DeviceName);
            var apiVersion = properties.ApiVersion;

            return new RendererInfo
            {
                Name = "Vulkan Renderer",
                Version = "1.0.0",
                ApiVersion = $"{(apiVersion >> 22) & 0x3FF}.{(apiVersion >> 12) & 0x3FF}.{apiVersion & 0xFFF}",
                DeviceName = deviceName ?? "Unknown",
                VendorName = GetVendorName(properties.VendorID),
                Capabilities = new Dictionary<string, object>
                {
                    ["MaxTextureSize"] = properties.Limits.MaxImageDimension2D,
                    ["MaxVertexAttributes"] = properties.Limits.MaxVertexInputAttributes,
                    ["MaxColorAttachments"] = properties.Limits.MaxColorAttachments,
                    ["DeviceType"] = properties.DeviceType.ToString()
                }
            };
        }

        private void LoadSampleTriangle()
        {
            // Create a simple triangle for testing
            _vertices = new List<Vertex>
            {
                new() { Position = new Vector3(0.0f, -0.5f, 0.0f), Normal = new Vector3(0, 0, 1), TexCoord = new Vector2(0.5f, 1.0f) },
                new() { Position = new Vector3(0.5f, 0.5f, 0.0f), Normal = new Vector3(0, 0, 1), TexCoord = new Vector2(1.0f, 0.0f) },
                new() { Position = new Vector3(-0.5f, 0.5f, 0.0f), Normal = new Vector3(0, 0, 1), TexCoord = new Vector2(0.0f, 0.0f) }
            };

            _indices = new List<uint> { 0, 1, 2 };

            UpdateVertexBuffer();
            UpdateIndexBuffer();
        }

        private string GetVendorName(uint vendorId)
        {
            return vendorId switch
            {
                0x1002 => "AMD",
                0x1010 => "ImgTec",
                0x10DE => "NVIDIA",
                0x13B5 => "ARM",
                0x5143 => "Qualcomm",
                0x8086 => "Intel",
                _ => "Unknown"
            };
        }

        // ... (I'll continue with the rest of the implementation in the next parts)

        public void Cleanup()
        {
            _vk.DeviceWaitIdle(_device);

            // Cleanup synchronization objects
            for (int i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                _vk.DestroySemaphore(_device, _renderFinishedSemaphores[i], null);
                _vk.DestroySemaphore(_device, _imageAvailableSemaphores[i], null);
                _vk.DestroyFence(_device, _inFlightFences[i], null);
            }

            // Cleanup command pool
            _vk.DestroyCommandPool(_device, _commandPool, null);

            // Cleanup framebuffers
            foreach (var framebuffer in _framebuffers)
            {
                _vk.DestroyFramebuffer(_device, framebuffer, null);
            }

            // Cleanup graphics pipeline
            _vk.DestroyPipeline(_device, _graphicsPipeline, null);
            _vk.DestroyPipelineLayout(_device, _pipelineLayout, null);

            // Cleanup render pass
            _vk.DestroyRenderPass(_device, _renderPass, null);

            // Cleanup image views
            foreach (var imageView in _swapchainImageViews)
            {
                _vk.DestroyImageView(_device, imageView, null);
            }

            // Cleanup swapchain
            _vk.DestroySwapchainKhr(_device, _swapchain, null);

            // Cleanup buffers
            if (_indexBuffer.Handle != 0)
            {
                _vk.DestroyBuffer(_device, _indexBuffer, null);
                _vk.FreeMemory(_device, _indexBufferMemory, null);
            }

            if (_vertexBuffer.Handle != 0)
            {
                _vk.DestroyBuffer(_device, _vertexBuffer, null);
                _vk.FreeMemory(_device, _vertexBufferMemory, null);
            }

            // Cleanup device
            _vk.DestroyDevice(_device, null);

            // Cleanup surface
            _vk.DestroySurfaceKhr(_instance, _surface, null);

            // Cleanup instance
            _vk.DestroyInstance(_instance, null);

            // Dispose Vulkan API
            _vk.Dispose();
        }

                public void DrawFrame()
        {
            if (_vertices.Count == 0 || _indices.Count == 0) return;

            _vk.WaitForFences(_device, 1, in _inFlightFences[_currentFrame], true, ulong.MaxValue);
            _vk.ResetFences(_device, 1, in _inFlightFences[_currentFrame]);

            uint imageIndex = 0;
            _vk.AcquireNextImageKhr(_device, _swapchain, ulong.MaxValue, _imageAvailableSemaphores[_currentFrame], default, ref imageIndex);

            SubmitInfo submitInfo = new()
            {
                SType = StructureType.SubmitInfo,
            };

            var waitSemaphores = stackalloc[] { _imageAvailableSemaphores[_currentFrame] };
            var waitStages = stackalloc[] { PipelineStageFlags.ColorAttachmentOutputBit };

            submitInfo = submitInfo with
            {
                WaitSemaphoreCount = 1,
                PWaitSemaphores = waitSemaphores,
                PWaitDstStageMask = waitStages,
                CommandBufferCount = 1,
                PCommandBuffers = &_commandBuffers[imageIndex]
            };

            var signalSemaphores = stackalloc[] { _renderFinishedSemaphores[_currentFrame] };
            submitInfo = submitInfo with
            {
                SignalSemaphoreCount = 1,
                PSignalSemaphores = signalSemaphores,
            };

            if (_vk.QueueSubmit(_graphicsQueue, 1, in submitInfo, _inFlightFences[_currentFrame]) != Result.Success)
            {
                throw new Exception("Failed to submit draw command buffer!");
            }

            var swapChains = stackalloc[] { _swapchain };
            PresentInfoKHR presentInfo = new()
            {
                SType = StructureType.PresentInfoKhr,
                WaitSemaphoreCount = 1,
                PWaitSemaphores = signalSemaphores,
                SwapchainCount = 1,
                PSwapchains = swapChains,
                PImageIndices = &imageIndex,
            };

            _vk.QueuePresentKhr(_presentQueue, in presentInfo);

            _currentFrame = (_currentFrame + 1) % MAX_FRAMES_IN_FLIGHT;
        }

        public void RotateModel(float deltaX, float deltaY)
        {
            _rotationY += deltaX * 0.01f;
            _rotationX += deltaY * 0.01f;
            UpdateCamera();

            // Recreate command buffers with updated matrices
            if (_commandBuffers != null)
            {
                _vk.DeviceWaitIdle(_device);
                CreateCommandBuffers();
            }
        }

        public void ZoomModel(float delta)
        {
            _zoom = Math.Clamp(_zoom + delta * 0.1f, 0.1f, 10.0f);
            UpdateCamera();

            // Recreate command buffers with updated matrices
            if (_commandBuffers != null)
            {
                _vk.DeviceWaitIdle(_device);
                CreateCommandBuffers();
            }
        }

        public void LoadSTLModel(string filePath)
        {
            try
            {
                var stlData = STLLoader.LoadSTL(filePath);
                _vertices = stlData.Vertices;
                _indices = stlData.Indices;

                UpdateVertexBuffer();
                UpdateIndexBuffer();

                // Recreate command buffers with new geometry
                if (_commandBuffers != null)
                {
                    _vk.DeviceWaitIdle(_device);
                    CreateCommandBuffers();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load STL: {ex.Message}");
            }
        }

        private void SetupCamera()
        {
            UpdateCamera();
        }

        private void UpdateCamera()
        {
            var eye = new Vector3(0, 0, 3 * _zoom);
            var center = new Vector3(0, 0, 0);
            var up = new Vector3(0, 1, 0);
            _viewMatrix = Matrix4x4.CreateLookAt(eye, center, up);

            _projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
                MathF.PI / 4, 800.0f / 600.0f, 0.1f, 100.0f);

            _modelMatrix = Matrix4x4.CreateRotationX(_rotationX) * Matrix4x4.CreateRotationY(_rotationY);
        }

        private void UpdateVertexBuffer()
        {
            if (_vertexBuffer.Handle != 0)
            {
                _vk.DestroyBuffer(_device, _vertexBuffer, null);
                _vk.FreeMemory(_device, _vertexBufferMemory, null);
            }
            CreateVertexBuffer();
        }

        private void UpdateIndexBuffer()
        {
            if (_indexBuffer.Handle != 0)
            {
                _vk.DestroyBuffer(_device, _indexBuffer, null);
                _vk.FreeMemory(_device, _indexBufferMemory, null);
            }
            CreateIndexBuffer();
        }

                private void CreateSwapchain()
        {
            var swapChainSupport = QuerySwapChainSupport(_physicalDevice);

            var surfaceFormat = ChooseSwapSurfaceFormat(swapChainSupport.Formats);
            var presentMode = ChooseSwapPresentMode(swapChainSupport.PresentModes);
            var extent = ChooseSwapExtent(swapChainSupport.Capabilities);

            uint imageCount = swapChainSupport.Capabilities.MinImageCount + 1;
            if (swapChainSupport.Capabilities.MaxImageCount > 0 && imageCount > swapChainSupport.Capabilities.MaxImageCount)
            {
                imageCount = swapChainSupport.Capabilities.MaxImageCount;
            }

            SwapchainCreateInfoKHR createInfo = new()
            {
                SType = StructureType.SwapchainCreateInfoKhr,
                Surface = _surface,
                MinImageCount = imageCount,
                ImageFormat = surfaceFormat.Format,
                ImageColorSpace = surfaceFormat.ColorSpace,
                ImageExtent = extent,
                ImageArrayLayers = 1,
                ImageUsage = ImageUsageFlags.ColorAttachmentBit
            };

            var indices = FindQueueFamilies(_physicalDevice);
            var queueFamilyIndices = stackalloc[] { indices.GraphicsFamily!.Value, indices.PresentFamily!.Value };

            if (indices.GraphicsFamily != indices.PresentFamily)
            {
                createInfo = createInfo with
                {
                    ImageSharingMode = SharingMode.Concurrent,
                    QueueFamilyIndexCount = 2,
                    PQueueFamilyIndices = queueFamilyIndices
                };
            }
            else
            {
                createInfo.ImageSharingMode = SharingMode.Exclusive;
            }

            createInfo = createInfo with
            {
                PreTransform = swapChainSupport.Capabilities.CurrentTransform,
                CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
                PresentMode = presentMode,
                Clipped = true,
                OldSwapchain = default
            };

            if (_vk.CreateSwapchainKhr(_device, in createInfo, null, out _swapchain) != Result.Success)
            {
                throw new Exception("Failed to create swap chain!");
            }

            _vk.GetSwapchainImagesKhr(_device, _swapchain, ref imageCount, null);
            _swapchainImages = new VkImage[imageCount];
            fixed (VkImage* swapchainImagesPtr = _swapchainImages)
            {
                _vk.GetSwapchainImagesKhr(_device, _swapchain, ref imageCount, swapchainImagesPtr);
            }
        }

        private SurfaceFormatKHR ChooseSwapSurfaceFormat(SurfaceFormatKHR[] availableFormats)
        {
            foreach (var availableFormat in availableFormats)
            {
                if (availableFormat.Format == Format.B8G8R8A8Srgb && availableFormat.ColorSpace == ColorSpaceKHR.SrgbNonlinearKhr)
                {
                    return availableFormat;
                }
            }
            return availableFormats[0];
        }

        private PresentModeKHR ChooseSwapPresentMode(PresentModeKHR[] availablePresentModes)
        {
            foreach (var availablePresentMode in availablePresentModes)
            {
                if (availablePresentMode == PresentModeKHR.MailboxKhr)
                {
                    return availablePresentMode;
                }
            }
            return PresentModeKHR.FifoKhr;
        }

        private Extent2D ChooseSwapExtent(SurfaceCapabilitiesKHR capabilities)
        {
            if (capabilities.CurrentExtent.Width != uint.MaxValue)
            {
                return capabilities.CurrentExtent;
            }

            var actualExtent = new Extent2D(800, 600);
            actualExtent.Width = Math.Clamp(actualExtent.Width, capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width);
            actualExtent.Height = Math.Clamp(actualExtent.Height, capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height);
            return actualExtent;
        }

        private void CreateImageViews()
        {
            _swapchainImageViews = new ImageView[_swapchainImages.Length];

            for (int i = 0; i < _swapchainImages.Length; i++)
            {
                ImageViewCreateInfo createInfo = new()
                {
                    SType = StructureType.ImageViewCreateInfo,
                    Image = _swapchainImages[i],
                    ViewType = ImageViewType.Type2D,
                    Format = Format.B8G8R8A8Srgb,
                    Components =
                    {
                        R = ComponentSwizzle.Identity,
                        G = ComponentSwizzle.Identity,
                        B = ComponentSwizzle.Identity,
                        A = ComponentSwizzle.Identity,
                    },
                    SubresourceRange =
                    {
                        AspectMask = ImageAspectFlags.ColorBit,
                        BaseMipLevel = 0,
                        LevelCount = 1,
                        BaseArrayLayer = 0,
                        LayerCount = 1,
                    }
                };

                if (_vk.CreateImageView(_device, in createInfo, null, out _swapchainImageViews[i]) != Result.Success)
                {
                    throw new Exception("Failed to create image views!");
                }
            }
        }

        private void CreateRenderPass()
        {
            AttachmentDescription colorAttachment = new()
            {
                Format = Format.B8G8R8A8Srgb,
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSrcKhr,
            };

            AttachmentReference colorAttachmentRef = new()
            {
                Attachment = 0,
                Layout = ImageLayout.ColorAttachmentOptimal,
            };

            SubpassDescription subpass = new()
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                ColorAttachmentCount = 1,
                PColorAttachments = &colorAttachmentRef,
            };

            SubpassDependency dependency = new()
            {
                SrcSubpass = Vk.SubpassExternal,
                DstSubpass = 0,
                SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
                SrcAccessMask = 0,
                DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
                DstAccessMask = AccessFlags.ColorAttachmentWriteBit
            };

            RenderPassCreateInfo renderPassInfo = new()
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = 1,
                PAttachments = &colorAttachment,
                SubpassCount = 1,
                PSubpasses = &subpass,
                DependencyCount = 1,
                PDependencies = &dependency,
            };

            if (_vk.CreateRenderPass(_device, in renderPassInfo, null, out _renderPass) != Result.Success)
            {
                throw new Exception("Failed to create render pass!");
            }
        }

        private void CreateGraphicsPipeline()
        {
            var vertShaderCode = LoadShaderFromResource("vertex.spv");
            var fragShaderCode = LoadShaderFromResource("fragment.spv");

            var vertShaderModule = CreateShaderModule(vertShaderCode);
            var fragShaderModule = CreateShaderModule(fragShaderCode);

            PipelineShaderStageCreateInfo vertShaderStageInfo = new()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.VertexBit,
                Module = vertShaderModule,
                PName = (byte*)Marshal.StringToHGlobalAnsi("main")
            };

            PipelineShaderStageCreateInfo fragShaderStageInfo = new()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.FragmentBit,
                Module = fragShaderModule,
                PName = (byte*)Marshal.StringToHGlobalAnsi("main")
            };

            var shaderStages = stackalloc[]
            {
                vertShaderStageInfo,
                fragShaderStageInfo
            };

            var bindingDescription = Vertex.GetBindingDescription();
            var attributeDescriptions = Vertex.GetAttributeDescriptions();

            fixed (VertexInputAttributeDescription* attributeDescriptionsPtr = attributeDescriptions)
            {
                PipelineVertexInputStateCreateInfo vertexInputInfo = new()
                {
                    SType = StructureType.PipelineVertexInputStateCreateInfo,
                    VertexBindingDescriptionCount = 1,
                    PVertexBindingDescriptions = &bindingDescription,
                    VertexAttributeDescriptionCount = (uint)attributeDescriptions.Length,
                    PVertexAttributeDescriptions = attributeDescriptionsPtr,
                };

                PipelineInputAssemblyStateCreateInfo inputAssembly = new()
                {
                    SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                    Topology = PrimitiveTopology.TriangleList,
                    PrimitiveRestartEnable = false,
                };

                Viewport viewport = new()
                {
                    X = 0,
                    Y = 0,
                    Width = 800,
                    Height = 600,
                    MinDepth = 0,
                    MaxDepth = 1,
                };

                Rect2D scissor = new()
                {
                    Offset = { X = 0, Y = 0 },
                    Extent = { Width = 800, Height = 600 },
                };

                PipelineViewportStateCreateInfo viewportState = new()
                {
                    SType = StructureType.PipelineViewportStateCreateInfo,
                    ViewportCount = 1,
                    PViewports = &viewport,
                    ScissorCount = 1,
                    PScissors = &scissor,
                };

                PipelineRasterizationStateCreateInfo rasterizer = new()
                {
                    SType = StructureType.PipelineRasterizationStateCreateInfo,
                    DepthClampEnable = false,
                    RasterizerDiscardEnable = false,
                    PolygonMode = PolygonMode.Fill,
                    LineWidth = 1,
                    CullMode = CullModeFlags.BackBit,
                    FrontFace = FrontFace.Clockwise,
                    DepthBiasEnable = false,
                };

                PipelineMultisampleStateCreateInfo multisampling = new()
                {
                    SType = StructureType.PipelineMultisampleStateCreateInfo,
                    SampleShadingEnable = false,
                    RasterizationSamples = SampleCountFlags.Count1Bit,
                };

                PipelineColorBlendAttachmentState colorBlendAttachment = new()
                {
                    ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
                    BlendEnable = false,
                };

                PipelineColorBlendStateCreateInfo colorBlending = new()
                {
                    SType = StructureType.PipelineColorBlendStateCreateInfo,
                    LogicOpEnable = false,
                    LogicOp = LogicOp.Copy,
                    AttachmentCount = 1,
                    PAttachments = &colorBlendAttachment,
                };

                // Push constant for MVP matrices
                PushConstantRange pushConstantRange = new()
                {
                    StageFlags = ShaderStageFlags.VertexBit,
                    Offset = 0,
                    Size = (uint)(sizeof(Matrix4x4) * 3) // model, view, projection
                };

                PipelineLayoutCreateInfo pipelineLayoutInfo = new()
                {
                    SType = StructureType.PipelineLayoutCreateInfo,
                    SetLayoutCount = 0,
                    PushConstantRangeCount = 1,
                    PPushConstantRanges = &pushConstantRange,
                };

                if (_vk.CreatePipelineLayout(_device, in pipelineLayoutInfo, null, out _pipelineLayout) != Result.Success)
                {
                    throw new Exception("Failed to create pipeline layout!");
                }

                GraphicsPipelineCreateInfo pipelineInfo = new()
                {
                    SType = StructureType.GraphicsPipelineCreateInfo,
                    StageCount = 2,
                    PStages = shaderStages,
                    PVertexInputState = &vertexInputInfo,
                    PInputAssemblyState = &inputAssembly,
                    PViewportState = &viewportState,
                    PRasterizationState = &rasterizer,
                    PMultisampleState = &multisampling,
                    PColorBlendState = &colorBlending,
                    Layout = _pipelineLayout,
                    RenderPass = _renderPass,
                    Subpass = 0,
                    BasePipelineHandle = default,
                };

                if (_vk.CreateGraphicsPipelines(_device, default, 1, in pipelineInfo, null, out _graphicsPipeline) != Result.Success)
                {
                    throw new Exception("Failed to create graphics pipeline!");
                }
            }

            _vk.DestroyShaderModule(_device, fragShaderModule, null);
            _vk.DestroyShaderModule(_device, vertShaderModule, null);
        }

        private byte[] LoadShaderFromResource(string fileName)
        {
            try
            {
                var assemblyPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var shaderPath = Path.Combine(assemblyPath, "Shaders", fileName);

                if (File.Exists(shaderPath))
                {
                    return File.ReadAllBytes(shaderPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load shader {fileName}: {ex.Message}");
            }

            // Fallback to minimal embedded shaders
            if (fileName.Contains("vertex"))
            {
                return GetVertexShaderSpirV();
            }
            else
            {
                return GetFragmentShaderSpirV();
            }
        }

        private byte[] GetVertexShaderSpirV()
        {
            // Minimal vertex shader SPIR-V bytecode (placeholder)
            return new byte[]
            {
                0x03, 0x02, 0x23, 0x07, 0x00, 0x00, 0x01, 0x00, 0x0A, 0x00, 0x08, 0x00,
                0x2E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x00, 0x02, 0x00,
                0x01, 0x00, 0x00, 0x00, 0x0B, 0x00, 0x06, 0x00, 0x01, 0x00, 0x00, 0x00,
                0x47, 0x4C, 0x53, 0x4C, 0x2E, 0x73, 0x74, 0x64, 0x2E, 0x34, 0x35, 0x30,
                0x00, 0x00, 0x00, 0x00, 0x0E, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x01, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x04, 0x00, 0x00, 0x00, 0x6D, 0x61, 0x69, 0x6E, 0x00, 0x00, 0x00, 0x00,
                0x0C, 0x00, 0x00, 0x00, 0x13, 0x00, 0x00, 0x00, 0x17, 0x00, 0x00, 0x00
            };
        }

        private byte[] GetFragmentShaderSpirV()
        {
            // Minimal fragment shader SPIR-V bytecode (placeholder)
            return new byte[]
            {
                0x03, 0x02, 0x23, 0x07, 0x00, 0x00, 0x01, 0x00, 0x0A, 0x00, 0x08, 0x00,
                0x22, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x00, 0x02, 0x00,
                0x01, 0x00, 0x00, 0x00, 0x0B, 0x00, 0x06, 0x00, 0x01, 0x00, 0x00, 0x00,
                0x47, 0x4C, 0x53, 0x4C, 0x2E, 0x73, 0x74, 0x64, 0x2E, 0x34, 0x35, 0x30,
                0x00, 0x00, 0x00, 0x00, 0x0E, 0x00, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x01, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x07, 0x00, 0x04, 0x00, 0x00, 0x00,
                0x04, 0x00, 0x00, 0x00, 0x6D, 0x61, 0x69, 0x6E, 0x00, 0x00, 0x00, 0x00,
                0x09, 0x00, 0x00, 0x00, 0x11, 0x00, 0x00, 0x00, 0x10, 0x00, 0x03, 0x00
            };
        }

        private ShaderModule CreateShaderModule(byte[] code)
        {
            ShaderModuleCreateInfo createInfo = new()
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)code.Length,
            };

            fixed (byte* codePtr = code)
            {
                createInfo.PCode = (uint*)codePtr;

                if (_vk.CreateShaderModule(_device, in createInfo, null, out var shaderModule) != Result.Success)
                {
                    throw new Exception("Failed to create shader module!");
                }

                return shaderModule;
            }
        }

        private void CreateFramebuffers()
        {
            _framebuffers = new Framebuffer[_swapchainImageViews.Length];

            for (int i = 0; i < _swapchainImageViews.Length; i++)
            {
                var attachments = stackalloc[] { _swapchainImageViews[i] };

                FramebufferCreateInfo framebufferInfo = new()
                {
                    SType = StructureType.FramebufferCreateInfo,
                    RenderPass = _renderPass,
                    AttachmentCount = 1,
                    PAttachments = attachments,
                    Width = 800,
                    Height = 600,
                    Layers = 1,
                };

                if (_vk.CreateFramebuffer(_device, in framebufferInfo, null, out _framebuffers[i]) != Result.Success)
                {
                    throw new Exception("Failed to create framebuffer!");
                }
            }
        }

        private void CreateCommandPool()
        {
            var queueFamilyIndices = FindQueueFamilies(_physicalDevice);

            CommandPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.CommandPoolCreateInfo,
                QueueFamilyIndex = queueFamilyIndices.GraphicsFamily!.Value,
            };

            if (_vk.CreateCommandPool(_device, in poolInfo, null, out _commandPool) != Result.Success)
            {
                throw new Exception("Failed to create command pool!");
            }
        }

        private void CreateVertexBuffer()
        {
            if (_vertices.Count == 0) return;

            ulong bufferSize = (ulong)(Marshal.SizeOf<Vertex>() * _vertices.Count);
            CreateBuffer(bufferSize, BufferUsageFlags.VertexBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out _vertexBuffer, out _vertexBufferMemory);

            void* data;
            _vk.MapMemory(_device, _vertexBufferMemory, 0, bufferSize, 0, &data);
            var verticesSpan = new Span<Vertex>(data, _vertices.Count);
            _vertices.AsSpan().CopyTo(verticesSpan);
            _vk.UnmapMemory(_device, _vertexBufferMemory);
        }

        private void CreateIndexBuffer()
        {
            if (_indices.Count == 0) return;

            ulong bufferSize = (ulong)(sizeof(uint) * _indices.Count);
            CreateBuffer(bufferSize, BufferUsageFlags.IndexBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, out _indexBuffer, out _indexBufferMemory);

            void* data;
            _vk.MapMemory(_device, _indexBufferMemory, 0, bufferSize, 0, &data);
            var indicesSpan = new Span<uint>(data, _indices.Count);
            _indices.AsSpan().CopyTo(indicesSpan);
            _vk.UnmapMemory(_device, _indexBufferMemory);
        }

        private void CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties, out VkBuffer buffer, out DeviceMemory bufferMemory)
        {
            BufferCreateInfo bufferInfo = new()
            {
                SType = StructureType.BufferCreateInfo,
                Size = size,
                Usage = usage,
                SharingMode = SharingMode.Exclusive,
            };

            if (_vk.CreateBuffer(_device, in bufferInfo, null, out buffer) != Result.Success)
            {
                throw new Exception("Failed to create buffer!");
            }

            MemoryRequirements memRequirements = new();
            _vk.GetBufferMemoryRequirements(_device, buffer, out memRequirements);

            MemoryAllocateInfo allocateInfo = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memRequirements.Size,
                MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties),
            };

            if (_vk.AllocateMemory(_device, in allocateInfo, null, out bufferMemory) != Result.Success)
            {
                throw new Exception("Failed to allocate buffer memory!");
            }

            _vk.BindBufferMemory(_device, buffer, bufferMemory, 0);
        }

        private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
        {
            PhysicalDeviceMemoryProperties memProperties;
            _vk.GetPhysicalDeviceMemoryProperties(_physicalDevice, out memProperties);

            for (uint i = 0; i < memProperties.MemoryTypeCount; i++)
            {
                if ((typeFilter & (1 << (int)i)) != 0 && (memProperties.MemoryTypes[(int)i].PropertyFlags & properties) == properties)
                {
                    return i;
                }
            }

            throw new Exception("Failed to find suitable memory type!");
        }

        private void CreateCommandBuffers()
        {
            _commandBuffers = new CommandBuffer[_framebuffers.Length];

            CommandBufferAllocateInfo allocateInfo = new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandPool = _commandPool,
                Level = CommandBufferLevel.Primary,
                CommandBufferCount = (uint)_commandBuffers.Length,
            };

            fixed (CommandBuffer* commandBuffersPtr = _commandBuffers)
            {
                if (_vk.AllocateCommandBuffers(_device, in allocateInfo, commandBuffersPtr) != Result.Success)
                {
                    throw new Exception("Failed to allocate command buffers!");
                }
            }

            for (int i = 0; i < _commandBuffers.Length; i++)
            {
                CommandBufferBeginInfo beginInfo = new()
                {
                    SType = StructureType.CommandBufferBeginInfo,
                };

                if (_vk.BeginCommandBuffer(_commandBuffers[i], in beginInfo) != Result.Success)
                {
                    throw new Exception("Failed to begin recording command buffer!");
                }

                RenderPassBeginInfo renderPassInfo = new()
                {
                    SType = StructureType.RenderPassBeginInfo,
                    RenderPass = _renderPass,
                    Framebuffer = _framebuffers[i],
                    RenderArea =
                    {
                        Offset = { X = 0, Y = 0 },
                        Extent = { Width = 800, Height = 600 },
                    }
                };

                var clearColor = new ClearValue
                {
                    Color = new ClearColorValue(0.0f, 0.0f, 0.0f, 1.0f)
                };

                renderPassInfo.ClearValueCount = 1;
                renderPassInfo.PClearValues = &clearColor;

                _vk.CmdBeginRenderPass(_commandBuffers[i], &renderPassInfo, SubpassContents.Inline);
                _vk.CmdBindPipeline(_commandBuffers[i], PipelineBindPoint.Graphics, _graphicsPipeline);

                if (_vertices.Count > 0 && _indices.Count > 0)
                {
                    var vertexBuffers = stackalloc[] { _vertexBuffer };
                    var offsets = stackalloc ulong[] { 0 };

                    _vk.CmdBindVertexBuffers(_commandBuffers[i], 0, 1, vertexBuffers, offsets);
                    _vk.CmdBindIndexBuffer(_commandBuffers[i], _indexBuffer, 0, IndexType.Uint32);

                    // Push constants for MVP matrices
                    var mvp = new MVPMatrices
                    {
                        Model = _modelMatrix,
                        View = _viewMatrix,
                        Projection = _projectionMatrix
                    };

                    _vk.CmdPushConstants(_commandBuffers[i], _pipelineLayout, ShaderStageFlags.VertexBit, 0, (uint)sizeof(MVPMatrices), &mvp);
                    _vk.CmdDrawIndexed(_commandBuffers[i], (uint)_indices.Count, 1, 0, 0, 0);
                }

                _vk.CmdEndRenderPass(_commandBuffers[i]);

                if (_vk.EndCommandBuffer(_commandBuffers[i]) != Result.Success)
                {
                    throw new Exception("Failed to record command buffer!");
                }
            }
        }

        private void CreateSyncObjects()
        {
            _imageAvailableSemaphores = new VkSemaphore[MAX_FRAMES_IN_FLIGHT];
            _renderFinishedSemaphores = new VkSemaphore[MAX_FRAMES_IN_FLIGHT];
            _inFlightFences = new Fence[MAX_FRAMES_IN_FLIGHT];

            SemaphoreCreateInfo semaphoreInfo = new()
            {
                SType = StructureType.SemaphoreCreateInfo,
            };

            FenceCreateInfo fenceInfo = new()
            {
                SType = StructureType.FenceCreateInfo,
                Flags = FenceCreateFlags.SignaledBit,
            };

            for (var i = 0; i < MAX_FRAMES_IN_FLIGHT; i++)
            {
                if (_vk.CreateSemaphore(_device, in semaphoreInfo, null, out _imageAvailableSemaphores[i]) != Result.Success ||
                    _vk.CreateSemaphore(_device, in semaphoreInfo, null, out _renderFinishedSemaphores[i]) != Result.Success ||
                    _vk.CreateFence(_device, in fenceInfo, null, out _inFlightFences[i]) != Result.Success)
                {
                    throw new Exception("Failed to create synchronization objects for a frame!");
                }
            }
        }
    }

    // Supporting structures and classes will be added next...
}
