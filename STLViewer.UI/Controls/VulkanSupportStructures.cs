using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;

namespace STLViewer.UI.Controls
{
    // Vertex Structure
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TexCoord;

        public static VertexInputBindingDescription GetBindingDescription()
        {
            return new VertexInputBindingDescription
            {
                Binding = 0,
                Stride = (uint)Marshal.SizeOf<Vertex>(),
                InputRate = VertexInputRate.Vertex
            };
        }

        public static VertexInputAttributeDescription[] GetAttributeDescriptions()
        {
            return new[]
            {
                new VertexInputAttributeDescription
                {
                    Binding = 0,
                    Location = 0,
                    Format = Format.R32G32B32Sfloat,
                    Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Position))
                },
                new VertexInputAttributeDescription
                {
                    Binding = 0,
                    Location = 1,
                    Format = Format.R32G32B32Sfloat,
                    Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Normal))
                },
                new VertexInputAttributeDescription
                {
                    Binding = 0,
                    Location = 2,
                    Format = Format.R32G32Sfloat,
                    Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(TexCoord))
                }
            };
        }
    }

    // Enhanced STL Loader with ASCII and Binary Support
    public static class STLLoader
    {
        public static (List<Vertex> Vertices, List<uint> Indices) LoadSTL(string filePath)
        {
            using var fileStream = File.OpenRead(filePath);
            using var reader = new BinaryReader(fileStream);

            // Check if it's ASCII or binary format
            fileStream.Seek(0, SeekOrigin.Begin);
            var header = new byte[80];
            fileStream.Read(header, 0, 80);
            var headerText = System.Text.Encoding.ASCII.GetString(header);

            if (headerText.StartsWith("solid") && !IsBinarySTL(filePath))
            {
                return LoadAsciiSTL(filePath);
            }
            else
            {
                return LoadBinarySTL(filePath);
            }
        }

        private static bool IsBinarySTL(string filePath)
        {
            try
            {
                using var reader = new BinaryReader(File.OpenRead(filePath));
                reader.ReadBytes(80); // Skip header
                uint triangleCount = reader.ReadUInt32();
                long expectedSize = 80 + 4 + (triangleCount * 50); // Header + count + triangles
                return new FileInfo(filePath).Length == expectedSize;
            }
            catch
            {
                return false;
            }
        }

        private static (List<Vertex> Vertices, List<uint> Indices) LoadBinarySTL(string filePath)
        {
            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            using var reader = new BinaryReader(File.OpenRead(filePath));

            // Skip header (80 bytes)
            reader.ReadBytes(80);

            // Read triangle count
            uint triangleCount = reader.ReadUInt32();

            uint vertexIndex = 0;
            for (uint i = 0; i < triangleCount; i++)
            {
                // Read normal vector
                var normal = new Vector3(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle()
                );

                // Read three vertices
                for (int j = 0; j < 3; j++)
                {
                    var position = new Vector3(
                        reader.ReadSingle(),
                        reader.ReadSingle(),
                        reader.ReadSingle()
                    );

                    vertices.Add(new Vertex
                    {
                        Position = position,
                        Normal = normal,
                        TexCoord = Vector2.Zero
                    });

                    indices.Add(vertexIndex++);
                }

                // Skip attribute byte count
                reader.ReadUInt16();
            }

            return (vertices, indices);
        }

        private static (List<Vertex> Vertices, List<uint> Indices) LoadAsciiSTL(string filePath)
        {
            var vertices = new List<Vertex>();
            var indices = new List<uint>();

            var lines = File.ReadAllLines(filePath);
            uint vertexIndex = 0;
            Vector3 currentNormal = Vector3.Zero;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();

                if (line.StartsWith("facet normal"))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 5)
                    {
                        currentNormal = new Vector3(
                            float.Parse(parts[2]),
                            float.Parse(parts[3]),
                            float.Parse(parts[4])
                        );
                    }
                }
                else if (line.StartsWith("vertex"))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4)
                    {
                        var position = new Vector3(
                            float.Parse(parts[1]),
                            float.Parse(parts[2]),
                            float.Parse(parts[3])
                        );

                        vertices.Add(new Vertex
                        {
                            Position = position,
                            Normal = currentNormal,
                            TexCoord = Vector2.Zero
                        });

                        indices.Add(vertexIndex++);
                    }
                }
            }

            return (vertices, indices);
        }
    }

    // Supporting Structures
    public struct QueueFamilyIndices
    {
        public uint? GraphicsFamily { get; set; }
        public uint? PresentFamily { get; set; }

        public bool IsComplete()
        {
            return GraphicsFamily.HasValue && PresentFamily.HasValue;
        }
    }

    public struct SwapChainSupportDetails
    {
        public SurfaceCapabilitiesKHR Capabilities;
        public SurfaceFormatKHR[] Formats;
        public PresentModeKHR[] PresentModes;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MVPMatrices
    {
        public Matrix4x4 Model;
        public Matrix4x4 View;
        public Matrix4x4 Projection;
    }

    // Renderer Info Structure
    public class RendererInfo
    {
        public string Name { get; set; } = "";
        public string Version { get; set; } = "";
        public string ApiVersion { get; set; } = "";
        public string DeviceName { get; set; } = "";
        public string VendorName { get; set; } = "";
        public Dictionary<string, object> Capabilities { get; set; } = new();
    }
}
