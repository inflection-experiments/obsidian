using Silk.NET.Vulkan;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Entities;
using STLViewer.Math;
using System.Runtime.InteropServices;
using Veldrid.SPIRV;
using Veldrid;

namespace STLViewer.Infrastructure.Graphics.Vulkan;

/// <summary>
/// Vulkan renderer implementation using Silk.NET.
/// </summary>
public unsafe class VulkanRenderer : IRenderer
{
    private Vk? _vk;
    private Instance _instance;
    private PhysicalDevice _physicalDevice;
    private Device _device;
    private Queue _graphicsQueue;
    private CommandPool _commandPool;
    private CommandBuffer _commandBuffer;
    private RenderPass _renderPass;
    private Silk.NET.Vulkan.Pipeline _graphicsPipeline;
    private PipelineLayout _pipelineLayout;
    private Silk.NET.Vulkan.Buffer _vertexBuffer;
    private DeviceMemory _vertexBufferMemory;
    private Silk.NET.Vulkan.Buffer _indexBuffer;
    private DeviceMemory _indexBufferMemory;
    private DescriptorSetLayout _descriptorSetLayout;
    private DescriptorPool _descriptorPool;
    private DescriptorSet _descriptorSet;
    private Silk.NET.Vulkan.Buffer _uniformBuffer;
    private DeviceMemory _uniformBufferMemory;
    private void* _uniformBufferMapped;

    private ICamera? _camera;
    private int _viewportWidth;
    private int _viewportHeight;
    private bool _initialized;
    private bool _disposed;

    // Graphics state
    private uint _graphicsQueueFamily;
    private uint _indexCount;

    /// <inheritdoc/>
    public RendererType Type => RendererType.Vulkan;

    /// <inheritdoc/>
    public bool IsInitialized => _initialized;

    /// <summary>
    /// Initializes a new instance of the VulkanRenderer class.
    /// </summary>
    /// <param name="vk">The Vulkan API instance.</param>
    public VulkanRenderer(Vk vk)
    {
        _vk = vk ?? throw new ArgumentNullException(nameof(vk));
    }

    /// <inheritdoc/>
    public Task InitializeAsync(int width, int height)
    {
        if (_initialized)
            return Task.CompletedTask;

        _viewportWidth = width;
        _viewportHeight = height;

        // Initialize Vulkan
        CreateInstance();
        SelectPhysicalDevice();
        CreateLogicalDevice();
        CreateCommandPool();
        CreateCommandBuffer();
        CreateRenderPass();
        CreateDescriptorSetLayout();
        CreateGraphicsPipeline();
        CreateUniformBuffer();
        CreateDescriptorPool();
        CreateDescriptorSet();

        _initialized = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Resize(int width, int height)
    {
        _viewportWidth = width;
        _viewportHeight = height;
        // In a real implementation, we'd need to recreate the swapchain and framebuffers
    }

    /// <inheritdoc/>
    public void SetCamera(ICamera camera)
    {
        _camera = camera;
    }

    /// <inheritdoc/>
    public void Render(STLModel model, RenderSettings renderSettings)
    {
        if (!_initialized || _camera == null)
            return;

        // Update model data if needed
        UpdateModelData(model);

        // Update uniform buffer
        UpdateUniformBuffer(renderSettings);

        // Record command buffer
        RecordCommandBuffer(renderSettings);
    }

    /// <inheritdoc/>
    public void Clear(Color clearColor)
    {
        // Clear values will be set in the render pass
    }

    /// <inheritdoc/>
    public void Present()
    {
        // Submit command buffer
        SubmitCommandBuffer();
    }

    /// <inheritdoc/>
    public RendererInfo GetInfo()
    {
        PhysicalDeviceProperties properties;
        _vk!.GetPhysicalDeviceProperties(_physicalDevice, &properties);

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

    private void CreateInstance()
    {
        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)Marshal.StringToHGlobalAnsi("STL Viewer"),
            ApplicationVersion = Vk.MakeVersion(1, 0, 0),
            PEngineName = (byte*)Marshal.StringToHGlobalAnsi("STL Viewer Engine"),
            EngineVersion = Vk.MakeVersion(1, 0, 0),
            ApiVersion = Vk.Version12
        };

        var createInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo
        };

        Result result = _vk!.CreateInstance(&createInfo, null, out _instance);
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create Vulkan instance: {result}");
        }
    }

    private void SelectPhysicalDevice()
    {
        uint deviceCount = 0;
        _vk!.EnumeratePhysicalDevices(_instance, &deviceCount, null);

        if (deviceCount == 0)
        {
            throw new InvalidOperationException("No Vulkan-capable devices found");
        }

        var devices = stackalloc PhysicalDevice[(int)deviceCount];
        _vk.EnumeratePhysicalDevices(_instance, &deviceCount, devices);

        // Select the first suitable device (in a real implementation, we'd rank devices)
        for (int i = 0; i < deviceCount; i++)
        {
            if (IsDeviceSuitable(devices[i]))
            {
                _physicalDevice = devices[i];
                break;
            }
        }

        if (_physicalDevice.Handle == 0)
        {
            throw new InvalidOperationException("No suitable Vulkan device found");
        }
    }

    private bool IsDeviceSuitable(PhysicalDevice device)
    {
        // Find graphics queue family
        uint queueFamilyCount = 0;
        _vk!.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, null);

        var queueFamilies = stackalloc QueueFamilyProperties[(int)queueFamilyCount];
        _vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, queueFamilies);

        for (uint i = 0; i < queueFamilyCount; i++)
        {
            if (queueFamilies[i].QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                _graphicsQueueFamily = i;
                return true;
            }
        }

        return false;
    }

    private void CreateLogicalDevice()
    {
        var queuePriority = 1.0f;
        var queueCreateInfo = new DeviceQueueCreateInfo
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueFamilyIndex = _graphicsQueueFamily,
            QueueCount = 1,
            PQueuePriorities = &queuePriority
        };

        var deviceFeatures = new PhysicalDeviceFeatures();

        var createInfo = new DeviceCreateInfo
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = 1,
            PQueueCreateInfos = &queueCreateInfo,
            PEnabledFeatures = &deviceFeatures
        };

        Result result = _vk!.CreateDevice(_physicalDevice, &createInfo, null, out _device);
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create logical device: {result}");
        }

        _vk.GetDeviceQueue(_device, _graphicsQueueFamily, 0, out _graphicsQueue);
    }

    private void CreateCommandPool()
    {
        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit,
            QueueFamilyIndex = _graphicsQueueFamily
        };

        Result result = _vk!.CreateCommandPool(_device, &poolInfo, null, out _commandPool);
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create command pool: {result}");
        }
    }

    private void CreateCommandBuffer()
    {
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1
        };

        Result result = _vk!.AllocateCommandBuffers(_device, &allocInfo, out _commandBuffer);
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to allocate command buffer: {result}");
        }
    }

    private void CreateRenderPass()
    {
        var colorAttachment = new AttachmentDescription
        {
            Format = Format.B8G8R8A8Srgb,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.PresentSrcKhr
        };

        var depthAttachment = new AttachmentDescription
        {
            Format = Format.D32Sfloat,
            Samples = SampleCountFlags.Count1Bit,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.DontCare,
            StencilLoadOp = AttachmentLoadOp.DontCare,
            StencilStoreOp = AttachmentStoreOp.DontCare,
            InitialLayout = ImageLayout.Undefined,
            FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
        };

        var colorAttachmentRef = new AttachmentReference
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal
        };

        var depthAttachmentRef = new AttachmentReference
        {
            Attachment = 1,
            Layout = ImageLayout.DepthStencilAttachmentOptimal
        };

        var subpass = new SubpassDescription
        {
            PipelineBindPoint = PipelineBindPoint.Graphics,
            ColorAttachmentCount = 1,
            PColorAttachments = &colorAttachmentRef,
            PDepthStencilAttachment = &depthAttachmentRef
        };

        var attachments = stackalloc AttachmentDescription[] { colorAttachment, depthAttachment };

        var renderPassInfo = new RenderPassCreateInfo
        {
            SType = StructureType.RenderPassCreateInfo,
            AttachmentCount = 2,
            PAttachments = attachments,
            SubpassCount = 1,
            PSubpasses = &subpass
        };

        Result result = _vk!.CreateRenderPass(_device, &renderPassInfo, null, out _renderPass);
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create render pass: {result}");
        }
    }

    private void CreateDescriptorSetLayout()
    {
        var uboLayoutBinding = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.VertexBit
        };

        var layoutInfo = new DescriptorSetLayoutCreateInfo
        {
            SType = StructureType.DescriptorSetLayoutCreateInfo,
            BindingCount = 1,
            PBindings = &uboLayoutBinding
        };

        Result result = _vk!.CreateDescriptorSetLayout(_device, &layoutInfo, null, out _descriptorSetLayout);
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create descriptor set layout: {result}");
        }
    }

    private void CreateGraphicsPipeline()
    {
        // Create shader modules (simplified - in real implementation, load from SPIR-V)
        var vertShaderCode = GetVertexShaderSpirV();
        var fragShaderCode = GetFragmentShaderSpirV();

        var vertShaderModule = CreateShaderModule(vertShaderCode);
        var fragShaderModule = CreateShaderModule(fragShaderCode);

        var vertShaderStageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.VertexBit,
            Module = vertShaderModule,
            PName = (byte*)Marshal.StringToHGlobalAnsi("main")
        };

        var fragShaderStageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.FragmentBit,
            Module = fragShaderModule,
            PName = (byte*)Marshal.StringToHGlobalAnsi("main")
        };

        var shaderStages = stackalloc PipelineShaderStageCreateInfo[] { vertShaderStageInfo, fragShaderStageInfo };

        // Vertex input
        var bindingDescription = GetVertexBindingDescription();
        var attributeDescriptions = GetVertexAttributeDescriptions();

        var vertexInputInfo = new PipelineVertexInputStateCreateInfo
        {
            SType = StructureType.PipelineVertexInputStateCreateInfo,
            VertexBindingDescriptionCount = 1,
            PVertexBindingDescriptions = &bindingDescription,
            VertexAttributeDescriptionCount = 2,
            PVertexAttributeDescriptions = attributeDescriptions
        };

        // Input assembly
        var inputAssembly = new PipelineInputAssemblyStateCreateInfo
        {
            SType = StructureType.PipelineInputAssemblyStateCreateInfo,
            Topology = Silk.NET.Vulkan.PrimitiveTopology.TriangleList,
            PrimitiveRestartEnable = false
        };

        // Viewport
                    var viewport = new Silk.NET.Vulkan.Viewport
        {
            X = 0.0f,
            Y = 0.0f,
            Width = _viewportWidth,
            Height = _viewportHeight,
            MinDepth = 0.0f,
            MaxDepth = 1.0f
        };

        var scissor = new Rect2D
        {
            Offset = { X = 0, Y = 0 },
            Extent = { Width = (uint)_viewportWidth, Height = (uint)_viewportHeight }
        };

        var viewportState = new PipelineViewportStateCreateInfo
        {
            SType = StructureType.PipelineViewportStateCreateInfo,
            ViewportCount = 1,
            PViewports = &viewport,
            ScissorCount = 1,
            PScissors = &scissor
        };

        // Rasterizer
        var rasterizer = new PipelineRasterizationStateCreateInfo
        {
            SType = StructureType.PipelineRasterizationStateCreateInfo,
            DepthClampEnable = false,
            RasterizerDiscardEnable = false,
            PolygonMode = PolygonMode.Fill,
            LineWidth = 1.0f,
            CullMode = CullModeFlags.BackBit,
            FrontFace = Silk.NET.Vulkan.FrontFace.CounterClockwise,
            DepthBiasEnable = false
        };

        // Multisampling
        var multisampling = new PipelineMultisampleStateCreateInfo
        {
            SType = StructureType.PipelineMultisampleStateCreateInfo,
            SampleShadingEnable = false,
            RasterizationSamples = SampleCountFlags.Count1Bit
        };

        // Depth testing
        var depthStencil = new PipelineDepthStencilStateCreateInfo
        {
            SType = StructureType.PipelineDepthStencilStateCreateInfo,
            DepthTestEnable = true,
            DepthWriteEnable = true,
            DepthCompareOp = CompareOp.Less,
            DepthBoundsTestEnable = false,
            StencilTestEnable = false
        };

        // Color blending
        var colorBlendAttachment = new PipelineColorBlendAttachmentState
        {
            ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
            BlendEnable = false
        };

        var colorBlending = new PipelineColorBlendStateCreateInfo
        {
            SType = StructureType.PipelineColorBlendStateCreateInfo,
            LogicOpEnable = false,
            AttachmentCount = 1,
            PAttachments = &colorBlendAttachment
        };

        // Pipeline layout
        var pipelineLayoutInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
            PSetLayouts = null
        };

        Result result;
        fixed (DescriptorSetLayout* setLayoutsPtr = &_descriptorSetLayout)
        {
            pipelineLayoutInfo.PSetLayouts = setLayoutsPtr;

            result = _vk!.CreatePipelineLayout(_device, &pipelineLayoutInfo, null, out _pipelineLayout);
            if (result != Result.Success)
            {
                throw new InvalidOperationException($"Failed to create pipeline layout: {result}");
            }
        }

        // Graphics pipeline
        var pipelineInfo = new GraphicsPipelineCreateInfo
        {
            SType = StructureType.GraphicsPipelineCreateInfo,
            StageCount = 2,
            PStages = shaderStages,
            PVertexInputState = &vertexInputInfo,
            PInputAssemblyState = &inputAssembly,
            PViewportState = &viewportState,
            PRasterizationState = &rasterizer,
            PMultisampleState = &multisampling,
            PDepthStencilState = &depthStencil,
            PColorBlendState = &colorBlending,
            Layout = _pipelineLayout,
            RenderPass = _renderPass,
            Subpass = 0
        };

        result = _vk.CreateGraphicsPipelines(_device, default, 1, &pipelineInfo, null, out _graphicsPipeline);
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create graphics pipeline: {result}");
        }

        // Clean up shader modules
        _vk.DestroyShaderModule(_device, vertShaderModule, null);
        _vk.DestroyShaderModule(_device, fragShaderModule, null);
    }

    private ShaderModule CreateShaderModule(byte[] code)
    {
        fixed (byte* codePtr = code)
        {
            var createInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)code.Length,
                PCode = (uint*)codePtr
            };

            Result result = _vk!.CreateShaderModule(_device, &createInfo, null, out var shaderModule);
            if (result != Result.Success)
            {
                throw new InvalidOperationException($"Failed to create shader module: {result}");
            }

            return shaderModule;
        }
    }

    private void CreateUniformBuffer()
    {
        var bufferSize = (ulong)Marshal.SizeOf<UniformBufferObject>();

        CreateBuffer(bufferSize, BufferUsageFlags.UniformBufferBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
            out _uniformBuffer, out _uniformBufferMemory);

        void* mappedMemory;
        _vk!.MapMemory(_device, _uniformBufferMemory, 0, bufferSize, 0, &mappedMemory);
        _uniformBufferMapped = mappedMemory;
    }

    private void CreateDescriptorPool()
    {
        var poolSize = new DescriptorPoolSize
        {
            Type = DescriptorType.UniformBuffer,
            DescriptorCount = 1
        };

        var poolInfo = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = 1,
            PPoolSizes = &poolSize,
            MaxSets = 1
        };

        Result result = _vk!.CreateDescriptorPool(_device, &poolInfo, null, out _descriptorPool);
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create descriptor pool: {result}");
        }
    }

    private void CreateDescriptorSet()
    {
        var setLayouts = stackalloc DescriptorSetLayout[] { _descriptorSetLayout };
        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = _descriptorPool,
            DescriptorSetCount = 1,
            PSetLayouts = setLayouts
        };

        Result result = _vk!.AllocateDescriptorSets(_device, &allocInfo, out _descriptorSet);
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to allocate descriptor set: {result}");
        }

        var bufferInfo = new DescriptorBufferInfo
        {
            Buffer = _uniformBuffer,
            Offset = 0,
            Range = (ulong)Marshal.SizeOf<UniformBufferObject>()
        };

        var descriptorWrite = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = _descriptorSet,
            DstBinding = 0,
            DstArrayElement = 0,
            DescriptorType = DescriptorType.UniformBuffer,
            DescriptorCount = 1,
            PBufferInfo = &bufferInfo
        };

        _vk.UpdateDescriptorSets(_device, 1, &descriptorWrite, 0, null);
    }

    private void UpdateModelData(STLModel model)
    {
        var triangles = model.Triangles;
        var vertices = new List<Vertex>();
        var indices = new List<uint>();

        uint vertexIndex = 0;
        foreach (var triangle in triangles)
        {
            vertices.Add(new Vertex { Position = triangle.Vertex1, Normal = triangle.Normal });
            vertices.Add(new Vertex { Position = triangle.Vertex2, Normal = triangle.Normal });
            vertices.Add(new Vertex { Position = triangle.Vertex3, Normal = triangle.Normal });

            indices.Add(vertexIndex++);
            indices.Add(vertexIndex++);
            indices.Add(vertexIndex++);
        }

        _indexCount = (uint)indices.Count;

        // Create vertex buffer
        var vertexBufferSize = (ulong)(Marshal.SizeOf<Vertex>() * vertices.Count);
        CreateBuffer(vertexBufferSize, BufferUsageFlags.VertexBufferBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
            out _vertexBuffer, out _vertexBufferMemory);

        // Upload vertex data
        void* data;
        _vk!.MapMemory(_device, _vertexBufferMemory, 0, vertexBufferSize, 0, &data);
        var verticesSpan = CollectionsMarshal.AsSpan(vertices);
        fixed (Vertex* verticesPtr = verticesSpan)
        {
            System.Buffer.MemoryCopy(verticesPtr, data, (long)vertexBufferSize, (long)vertexBufferSize);
        }
        _vk.UnmapMemory(_device, _vertexBufferMemory);

        // Create index buffer
        var indexBufferSize = (ulong)(sizeof(uint) * indices.Count);
        CreateBuffer(indexBufferSize, BufferUsageFlags.IndexBufferBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit,
            out _indexBuffer, out _indexBufferMemory);

        // Upload index data
        _vk.MapMemory(_device, _indexBufferMemory, 0, indexBufferSize, 0, &data);
        var indicesSpan = CollectionsMarshal.AsSpan(indices);
        fixed (uint* indicesPtr = indicesSpan)
        {
            System.Buffer.MemoryCopy(indicesPtr, data, (long)indexBufferSize, (long)indexBufferSize);
        }
        _vk.UnmapMemory(_device, _indexBufferMemory);
    }

    private void UpdateUniformBuffer(RenderSettings renderSettings)
    {
        var ubo = new UniformBufferObject
        {
            Model = Matrix4x4.Identity,
            View = _camera!.ViewMatrix,
            Projection = _camera.ProjectionMatrix
        };

        System.Buffer.MemoryCopy(&ubo, _uniformBufferMapped, Marshal.SizeOf<UniformBufferObject>(), Marshal.SizeOf<UniformBufferObject>());
    }

    private void RecordCommandBuffer(RenderSettings renderSettings)
    {
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        _vk!.BeginCommandBuffer(_commandBuffer, &beginInfo);

        var clearColor = new ClearValue
        {
            Color = new ClearColorValue(renderSettings.BackgroundColor.R, renderSettings.BackgroundColor.G,
                renderSettings.BackgroundColor.B, renderSettings.BackgroundColor.A)
        };

        var clearDepth = new ClearValue
        {
            DepthStencil = new ClearDepthStencilValue(1.0f, 0)
        };

        var clearValues = stackalloc ClearValue[] { clearColor, clearDepth };

        var renderPassInfo = new RenderPassBeginInfo
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = _renderPass,
            RenderArea = new Rect2D
            {
                Offset = { X = 0, Y = 0 },
                Extent = { Width = (uint)_viewportWidth, Height = (uint)_viewportHeight }
            },
            ClearValueCount = 2,
            PClearValues = clearValues
        };

        _vk.CmdBeginRenderPass(_commandBuffer, &renderPassInfo, SubpassContents.Inline);

        _vk.CmdBindPipeline(_commandBuffer, PipelineBindPoint.Graphics, _graphicsPipeline);

        var vertexBuffers = stackalloc Silk.NET.Vulkan.Buffer[] { _vertexBuffer };
        var offsets = stackalloc ulong[] { 0 };
        _vk.CmdBindVertexBuffers(_commandBuffer, 0, 1, vertexBuffers, offsets);
        _vk.CmdBindIndexBuffer(_commandBuffer, _indexBuffer, 0, IndexType.Uint32);

        var descriptorSets = stackalloc DescriptorSet[] { _descriptorSet };
        _vk.CmdBindDescriptorSets(_commandBuffer, PipelineBindPoint.Graphics, _pipelineLayout, 0, 1, descriptorSets, 0, null);

        _vk.CmdDrawIndexed(_commandBuffer, _indexCount, 1, 0, 0, 0);

        _vk.CmdEndRenderPass(_commandBuffer);

        Result result = _vk.EndCommandBuffer(_commandBuffer);
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to record command buffer: {result}");
        }
    }

    private void SubmitCommandBuffer()
    {
        var commandBuffers = stackalloc CommandBuffer[] { _commandBuffer };
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = commandBuffers
        };

        Result result = _vk!.QueueSubmit(_graphicsQueue, 1, &submitInfo, default);
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to submit command buffer: {result}");
        }

        _vk.QueueWaitIdle(_graphicsQueue);
    }

    private void CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags properties, out Silk.NET.Vulkan.Buffer buffer, out DeviceMemory bufferMemory)
    {
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive
        };

        Result result = _vk!.CreateBuffer(_device, &bufferInfo, null, out buffer);
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to create buffer: {result}");
        }

        MemoryRequirements memRequirements;
        _vk.GetBufferMemoryRequirements(_device, buffer, &memRequirements);

        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = FindMemoryType(memRequirements.MemoryTypeBits, properties)
        };

        result = _vk.AllocateMemory(_device, &allocInfo, null, out bufferMemory);
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"Failed to allocate buffer memory: {result}");
        }

        _vk.BindBufferMemory(_device, buffer, bufferMemory, 0);
    }

    private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        PhysicalDeviceMemoryProperties memProperties;
        _vk!.GetPhysicalDeviceMemoryProperties(_physicalDevice, &memProperties);

        for (uint i = 0; i < memProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1 << (int)i)) != 0 &&
                (memProperties.MemoryTypes[(int)i].PropertyFlags & properties) == properties)
            {
                return i;
            }
        }

        throw new InvalidOperationException("Failed to find suitable memory type");
    }

    private static VertexInputBindingDescription GetVertexBindingDescription()
    {
        return new VertexInputBindingDescription
        {
            Binding = 0,
            Stride = (uint)Marshal.SizeOf<Vertex>(),
            InputRate = VertexInputRate.Vertex
        };
    }

    private static VertexInputAttributeDescription* GetVertexAttributeDescriptions()
    {
        var attributes = (VertexInputAttributeDescription*)Marshal.AllocHGlobal(2 * sizeof(VertexInputAttributeDescription));

        // Position attribute
        attributes[0] = new VertexInputAttributeDescription
        {
            Binding = 0,
            Location = 0,
            Format = Format.R32G32B32Sfloat,
            Offset = 0
        };

        // Normal attribute
        attributes[1] = new VertexInputAttributeDescription
        {
            Binding = 0,
            Location = 1,
            Format = Format.R32G32B32Sfloat,
            Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.Normal))
        };

        return attributes;
    }

        private static byte[] GetVertexShaderSpirV()
    {
        var vertexShaderGlsl = @"
#version 450

layout(binding = 0) uniform UniformBufferObject {
    mat4 model;
    mat4 view;
    mat4 proj;
} ubo;

layout(location = 0) in vec3 inPosition;
layout(location = 1) in vec3 inNormal;

layout(location = 0) out vec3 fragNormal;
layout(location = 1) out vec3 fragPos;

void main() {
    gl_Position = ubo.proj * ubo.view * ubo.model * vec4(inPosition, 1.0);
    fragNormal = mat3(transpose(inverse(ubo.model))) * inNormal;
    fragPos = vec3(ubo.model * vec4(inPosition, 1.0));
}
";

        var vertexShaderDescription = new ShaderDescription(
            ShaderStages.Vertex,
            System.Text.Encoding.UTF8.GetBytes(vertexShaderGlsl),
            "main"
        );

        var compilationResult = SpirvCompilation.CompileGlslToSpirv(
            "vertex.glsl",
            vertexShaderGlsl,
            ShaderStages.Vertex,
            new GlslCompileOptions()
        );

        return compilationResult.SpirvBytes;
    }

    private static byte[] GetFragmentShaderSpirV()
    {
        var fragmentShaderGlsl = @"
#version 450

layout(location = 0) in vec3 fragNormal;
layout(location = 1) in vec3 fragPos;

layout(location = 0) out vec4 outColor;

void main() {
    // Simple lighting calculation
    vec3 lightDir = normalize(vec3(1.0, 1.0, 1.0));
    vec3 normal = normalize(fragNormal);
    float diff = max(dot(normal, lightDir), 0.0);

    vec3 ambient = vec3(0.1, 0.1, 0.1);
    vec3 diffuse = diff * vec3(0.8, 0.8, 0.8);
    vec3 color = ambient + diffuse;

    outColor = vec4(color, 1.0);
}
";

        var fragmentShaderDescription = new ShaderDescription(
            ShaderStages.Fragment,
            System.Text.Encoding.UTF8.GetBytes(fragmentShaderGlsl),
            "main"
        );

        var compilationResult = SpirvCompilation.CompileGlslToSpirv(
            "fragment.glsl",
            fragmentShaderGlsl,
            ShaderStages.Fragment,
            new GlslCompileOptions()
        );

        return compilationResult.SpirvBytes;
    }

    private static string GetVendorName(uint vendorId)
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

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing && _initialized && _vk != null)
            {
                _vk.DeviceWaitIdle(_device);

                if (_uniformBufferMapped != null)
                    _vk.UnmapMemory(_device, _uniformBufferMemory);

                _vk.DestroyBuffer(_device, _uniformBuffer, null);
                _vk.FreeMemory(_device, _uniformBufferMemory, null);

                _vk.DestroyBuffer(_device, _indexBuffer, null);
                _vk.FreeMemory(_device, _indexBufferMemory, null);

                _vk.DestroyBuffer(_device, _vertexBuffer, null);
                _vk.FreeMemory(_device, _vertexBufferMemory, null);

                _vk.DestroyDescriptorPool(_device, _descriptorPool, null);
                _vk.DestroyDescriptorSetLayout(_device, _descriptorSetLayout, null);
                _vk.DestroyPipeline(_device, _graphicsPipeline, null);
                _vk.DestroyPipelineLayout(_device, _pipelineLayout, null);
                _vk.DestroyRenderPass(_device, _renderPass, null);
                _vk.DestroyCommandPool(_device, _commandPool, null);
                _vk.DestroyDevice(_device, null);
                _vk.DestroyInstance(_instance, null);
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct Vertex
{
    public Vector3 Position;
    public Vector3 Normal;
}

[StructLayout(LayoutKind.Sequential)]
internal struct UniformBufferObject
{
    public Matrix4x4 Model;
    public Matrix4x4 View;
    public Matrix4x4 Projection;
}
