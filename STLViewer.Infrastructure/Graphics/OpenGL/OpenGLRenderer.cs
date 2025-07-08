using Silk.NET.OpenGL;
using STLViewer.Core.Interfaces;
using STLViewer.Domain.Entities;
using STLViewer.Domain.ValueObjects;
using STLViewer.Math;
using System.Runtime.InteropServices;

namespace STLViewer.Infrastructure.Graphics.OpenGL;

/// <summary>
/// OpenGL renderer implementation using Silk.NET.
/// </summary>
public unsafe class OpenGLRenderer : IRenderer
{
    private GL? _gl;
    private ICamera? _camera;
    private uint _shaderProgram;
    private uint _vao;
    private uint _vbo;
    private uint _ebo;
    private int _viewportWidth;
    private int _viewportHeight;
    private bool _initialized;
    private bool _disposed;

    // Shader uniform locations
    private int _modelUniform;
    private int _viewUniform;
    private int _projectionUniform;
    private int _colorUniform;
    private int _lightDirUniform;
    private int _lightColorUniform;
    private int _ambientColorUniform;
    private int _enableLightingUniform;

    // Vertex data for current model
    private float[]? _vertices;
    private uint[]? _indices;
    private int _indexCount;
    private STLModel? _currentModel;

    /// <inheritdoc/>
    public RendererType Type => RendererType.OpenGL;

    /// <inheritdoc/>
    public bool IsInitialized => _initialized;

    /// <summary>
    /// Initializes a new instance of the OpenGLRenderer class.
    /// </summary>
    /// <param name="gl">The OpenGL context.</param>
    public OpenGLRenderer(GL gl)
    {
        _gl = gl ?? throw new ArgumentNullException(nameof(gl));
    }

    /// <inheritdoc/>
    public Task InitializeAsync(int width, int height)
    {
        if (_initialized)
            return Task.CompletedTask;

        _viewportWidth = width;
        _viewportHeight = height;

        // Set up OpenGL state
        _gl!.Enable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.CullFace);
        _gl.CullFace(GLEnum.Back);
        _gl.FrontFace(FrontFaceDirection.Ccw);

        // Create shader program
        CreateShaderProgram();

        // Create vertex array object
        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();

        _gl.BindVertexArray(_vao);

        // Set up vertex attributes
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);

        // Position attribute (location 0)
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)0);
        _gl.EnableVertexAttribArray(0);

        // Normal attribute (location 1)
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)(3 * sizeof(float)));
        _gl.EnableVertexAttribArray(1);

        _gl.BindVertexArray(0);

        // Get uniform locations
        GetUniformLocations();

        _initialized = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Resize(int width, int height)
    {
        _viewportWidth = width;
        _viewportHeight = height;
        _gl!.Viewport(0, 0, (uint)width, (uint)height);
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

        // Update model data only if model changed
        if (_currentModel != model)
        {
            UpdateModelData(model);
            _currentModel = model;
        }

        // Set up render state
        SetupRenderState(renderSettings);

        // Set uniforms
        SetUniforms(renderSettings);

        // Draw the model based on render mode
        if (renderSettings is IRenderModeSettings modeSettings)
        {
            switch (modeSettings.RenderMode)
            {
                case Core.Interfaces.RenderMode.Surface:
                    DrawModel(renderSettings);
                    break;

                case Core.Interfaces.RenderMode.Wireframe:
                    _gl!.PolygonMode(GLEnum.FrontAndBack, GLEnum.Line);
                    DrawModel(renderSettings);
                    break;

                case Core.Interfaces.RenderMode.ShadedWireframe:
                    // Draw solid first
                    _gl!.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
                    DrawModel(renderSettings);

                    // Then draw wireframe on top
                    _gl.Enable(EnableCap.PolygonOffsetLine);
                    _gl.PolygonOffset(-1.0f, -1.0f);
                    _gl.PolygonMode(GLEnum.FrontAndBack, GLEnum.Line);

                    // Use darker color for wireframe overlay
                    var baseColor = renderSettings.Material.DiffuseColor;
                    var wireColor = new STLViewer.Math.Color(
                        baseColor.R * 0.3f,
                        baseColor.G * 0.3f,
                        baseColor.B * 0.3f,
                        baseColor.A);
                    _gl.Uniform4(_colorUniform, wireColor.R, wireColor.G, wireColor.B, wireColor.A);

                    DrawModel(renderSettings);
                    _gl.Disable(EnableCap.PolygonOffsetLine);
                    break;
            }
        }
        else
        {
            // Fallback to original behavior
            DrawModel(renderSettings);
        }
    }

    /// <inheritdoc/>
    public void Clear(Color clearColor)
    {
        _gl!.ClearColor(clearColor.R, clearColor.G, clearColor.B, clearColor.A);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    /// <inheritdoc/>
    public void Present()
    {
        // OpenGL context swapping is typically handled by the windowing system
        // This method is here for interface compliance
    }

    /// <inheritdoc/>
    public RendererInfo GetInfo()
    {
        var info = new RendererInfo
        {
            Name = "OpenGL Renderer",
            Version = "1.0.0",
            ApiVersion = System.Runtime.InteropServices.Marshal.PtrToStringAnsi((nint)_gl!.GetString(StringName.Version)) ?? "Unknown",
            DeviceName = System.Runtime.InteropServices.Marshal.PtrToStringAnsi((nint)_gl.GetString(StringName.Renderer)) ?? "Unknown",
            VendorName = System.Runtime.InteropServices.Marshal.PtrToStringAnsi((nint)_gl.GetString(StringName.Vendor)) ?? "Unknown"
        };

        // Get capabilities
        _gl.GetInteger(GetPName.MaxTextureSize, out int maxTextureSize);
        _gl.GetInteger(GetPName.MaxVertexAttribs, out int maxVertexAttribs);
        _gl.GetInteger(GetPName.MaxColorAttachments, out int maxColorAttachments);

        info.Capabilities["MaxTextureSize"] = maxTextureSize;
        info.Capabilities["MaxVertexAttribs"] = maxVertexAttribs;
        info.Capabilities["MaxColorAttachments"] = maxColorAttachments;

        return info;
    }

    private void CreateShaderProgram()
    {
        var vertexShaderSource = GetVertexShaderSource();
        var fragmentShaderSource = GetFragmentShaderSource();

        var vertexShader = CreateShader(ShaderType.VertexShader, vertexShaderSource);
        var fragmentShader = CreateShader(ShaderType.FragmentShader, fragmentShaderSource);

        _shaderProgram = _gl!.CreateProgram();
        _gl.AttachShader(_shaderProgram, vertexShader);
        _gl.AttachShader(_shaderProgram, fragmentShader);
        _gl.LinkProgram(_shaderProgram);

        // Check for linking errors
        _gl.GetProgram(_shaderProgram, ProgramPropertyARB.LinkStatus, out int linkStatus);
        if (linkStatus == 0)
        {
            var infoLog = _gl.GetProgramInfoLog(_shaderProgram);
            throw new InvalidOperationException($"Shader program linking failed: {infoLog}");
        }

        // Validate the program
        _gl.ValidateProgram(_shaderProgram);
        _gl.GetProgram(_shaderProgram, ProgramPropertyARB.ValidateStatus, out int validateStatus);
        if (validateStatus == 0)
        {
            var infoLog = _gl.GetProgramInfoLog(_shaderProgram);
            // Log warning but don't fail - validation can fail on some drivers for legitimate programs
            System.Diagnostics.Debug.WriteLine($"Shader program validation warning: {infoLog}");
        }

        // Clean up shaders
        _gl.DeleteShader(vertexShader);
        _gl.DeleteShader(fragmentShader);
    }

    private uint CreateShader(ShaderType type, string source)
    {
        var shader = _gl!.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        // Check for compilation errors
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int compileStatus);
        if (compileStatus == 0)
        {
            var infoLog = _gl.GetShaderInfoLog(shader);
            var shaderTypeName = type == ShaderType.VertexShader ? "vertex" : "fragment";
            throw new InvalidOperationException($"{shaderTypeName} shader compilation failed: {infoLog}");
        }

        return shader;
    }

    private void GetUniformLocations()
    {
        _modelUniform = _gl!.GetUniformLocation(_shaderProgram, "uModel");
        _viewUniform = _gl.GetUniformLocation(_shaderProgram, "uView");
        _projectionUniform = _gl.GetUniformLocation(_shaderProgram, "uProjection");
        _colorUniform = _gl.GetUniformLocation(_shaderProgram, "uColor");
        _lightDirUniform = _gl.GetUniformLocation(_shaderProgram, "uLightDir");
        _lightColorUniform = _gl.GetUniformLocation(_shaderProgram, "uLightColor");
        _ambientColorUniform = _gl.GetUniformLocation(_shaderProgram, "uAmbientColor");
        _enableLightingUniform = _gl.GetUniformLocation(_shaderProgram, "uEnableLighting");
    }

    private void UpdateModelData(STLModel model)
    {
        var triangles = model.Triangles;
        var vertexList = new List<float>();
        var indexList = new List<uint>();

        uint vertexIndex = 0;
        foreach (var triangle in triangles)
        {
            // Add vertices with normals
            AddVertex(vertexList, triangle.Vertex1, triangle.Normal);
            AddVertex(vertexList, triangle.Vertex2, triangle.Normal);
            AddVertex(vertexList, triangle.Vertex3, triangle.Normal);

            // Add indices
            indexList.Add(vertexIndex++);
            indexList.Add(vertexIndex++);
            indexList.Add(vertexIndex++);
        }

        _vertices = vertexList.ToArray();
        _indices = indexList.ToArray();
        _indexCount = _indices.Length;

        // Upload to GPU
        _gl!.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)_vertices, BufferUsageARB.StaticDraw);

        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)_indices, BufferUsageARB.StaticDraw);
    }

    private static void AddVertex(List<float> vertexList, Vector3 position, Vector3 normal)
    {
        vertexList.Add(position.X);
        vertexList.Add(position.Y);
        vertexList.Add(position.Z);
        vertexList.Add(normal.X);
        vertexList.Add(normal.Y);
        vertexList.Add(normal.Z);
    }

    private void SetupRenderState(RenderSettings renderSettings)
    {
        // Set polygon mode
        _gl!.PolygonMode(GLEnum.FrontAndBack,
            renderSettings.Wireframe ? GLEnum.Line : GLEnum.Fill);

        // Set line width for wireframe mode
        if (renderSettings.Wireframe)
        {
            _gl.LineWidth(2.0f); // Make wireframe lines more visible
        }

        // Set depth testing
        if (renderSettings.DepthTesting)
        {
            _gl.Enable(EnableCap.DepthTest);
            _gl.DepthFunc(DepthFunction.Less);
        }
        else
        {
            _gl.Disable(EnableCap.DepthTest);
        }

        // Set transparency/alpha blending
        if (renderSettings.EnableTransparency && renderSettings.Material.Alpha < 1.0f)
        {
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // For transparent objects, we might want to disable depth writing
            // but keep depth testing to avoid sorting issues
            _gl.DepthMask(false);
        }
        else
        {
            _gl.Disable(EnableCap.Blend);
            _gl.DepthMask(true);
        }

        // Set backface culling
        if (renderSettings.BackfaceCulling)
        {
            _gl.Enable(EnableCap.CullFace);
            _gl.CullFace(GLEnum.Back);
        }
        else
        {
            _gl.Disable(EnableCap.CullFace);
        }

        // Enable anti-aliasing for smoother rendering
        if (renderSettings.AntiAliasing > 0)
        {
            _gl.Enable(EnableCap.Multisample);
            if (renderSettings.Wireframe)
            {
                _gl.Enable(EnableCap.LineSmooth);
                _gl.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
            }
        }
        else
        {
            _gl.Disable(EnableCap.Multisample);
            _gl.Disable(EnableCap.LineSmooth);
        }
    }

    private void SetUniforms(RenderSettings renderSettings)
    {
        _gl!.UseProgram(_shaderProgram);

        // Set matrices
        var model = Matrix4x4.Identity;
        var view = _camera!.ViewMatrix;
        var projection = _camera.ProjectionMatrix;

        SetMatrix4Uniform(_modelUniform, model);
        SetMatrix4Uniform(_viewUniform, view);
        SetMatrix4Uniform(_projectionUniform, projection);

        // Set material color with alpha from material
        var material = renderSettings.Material;
        _gl.Uniform4(_colorUniform, material.DiffuseColor.R, material.DiffuseColor.G,
            material.DiffuseColor.B, material.Alpha);

        // Set lighting parameters
        if (renderSettings.Lighting.Enabled)
        {
            _gl.Uniform1(_enableLightingUniform, 1);
            var lightDir = renderSettings.Lighting.LightDirection.Normalized();
            _gl.Uniform3(_lightDirUniform, lightDir.X, lightDir.Y, lightDir.Z);

            // Use material properties for lighting
            _gl.Uniform3(_lightColorUniform, material.DiffuseColor.R, material.DiffuseColor.G, material.DiffuseColor.B);
            _gl.Uniform3(_ambientColorUniform, material.AmbientColor.R, material.AmbientColor.G, material.AmbientColor.B);

            // Set view position for specular lighting (camera position)
            var viewPos = _camera.Position;
            _gl.Uniform3(_gl.GetUniformLocation(_shaderProgram, "uViewPos"), viewPos.X, viewPos.Y, viewPos.Z);

            // Set specular properties from material
            _gl.Uniform3(_gl.GetUniformLocation(_shaderProgram, "uSpecularColor"),
                material.SpecularColor.R, material.SpecularColor.G, material.SpecularColor.B);
            _gl.Uniform1(_gl.GetUniformLocation(_shaderProgram, "uShininess"), material.Shininess);
        }
        else
        {
            _gl.Uniform1(_enableLightingUniform, 0);
        }
    }

    private void SetMatrix4Uniform(int location, Matrix4x4 matrix)
    {
        var matrixArray = new float[16]
        {
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44
        };
        _gl!.UniformMatrix4(location, 1, false, matrixArray);
    }

    private void DrawModel(RenderSettings renderSettings)
    {
        _gl!.BindVertexArray(_vao);
        _gl.DrawElements(PrimitiveType.Triangles, (uint)_indexCount, DrawElementsType.UnsignedInt, 0);
        _gl.BindVertexArray(0);
    }

    private static string GetVertexShaderSource()
    {
        return @"
#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec3 FragPos;
out vec3 Normal;

void main()
{
    FragPos = vec3(uModel * vec4(aPos, 1.0));
    Normal = mat3(transpose(inverse(uModel))) * aNormal;

    gl_Position = uProjection * uView * vec4(FragPos, 1.0);
}
";
    }

    private static string GetFragmentShaderSource()
    {
        return @"
#version 330 core
in vec3 FragPos;
in vec3 Normal;

uniform vec4 uColor;
uniform vec3 uLightDir;
uniform vec3 uLightColor;
uniform vec3 uAmbientColor;
uniform vec3 uSpecularColor;
uniform vec3 uViewPos;
uniform float uShininess;
uniform bool uEnableLighting;

out vec4 FragColor;

void main()
{
    vec3 color = uColor.rgb;

    if (uEnableLighting)
    {
        vec3 norm = normalize(Normal);
        vec3 lightDir = normalize(-uLightDir);
        vec3 viewDir = normalize(uViewPos - FragPos);

        // Ambient lighting
        vec3 ambient = uAmbientColor;

        // Diffuse lighting
        float diff = max(dot(norm, lightDir), 0.0);
        vec3 diffuse = diff * uLightColor;

        // Specular lighting (Phong reflection model)
        vec3 reflectDir = reflect(-lightDir, norm);
        float spec = pow(max(dot(viewDir, reflectDir), 0.0), uShininess);
        vec3 specular = spec * uSpecularColor;

        // Combine all lighting components
        color = (ambient + diffuse + specular) * color;
    }

    FragColor = vec4(color, uColor.a);
}
";
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                if (_initialized && _gl != null)
                {
                    _gl.DeleteProgram(_shaderProgram);
                    _gl.DeleteVertexArray(_vao);
                    _gl.DeleteBuffer(_vbo);
                    _gl.DeleteBuffer(_ebo);
                }
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
