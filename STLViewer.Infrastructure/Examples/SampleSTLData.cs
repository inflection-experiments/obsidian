using System.Text;

namespace STLViewer.Infrastructure.Examples;

/// <summary>
/// Provides sample STL data for testing and demonstration purposes.
/// </summary>
public static class SampleSTLData
{
    /// <summary>
    /// Gets a simple ASCII STL file content representing a triangle.
    /// </summary>
    public static string SimpleAsciiSTL => @"solid Simple Triangle
  facet normal 0.0 0.0 1.0
    outer loop
      vertex 0.0 0.0 0.0
      vertex 1.0 0.0 0.0
      vertex 0.5 1.0 0.0
    endloop
  endfacet
endsolid Simple Triangle";

    /// <summary>
    /// Gets ASCII STL data as bytes.
    /// </summary>
    public static byte[] SimpleAsciiSTLBytes => Encoding.UTF8.GetBytes(SimpleAsciiSTL);

    /// <summary>
    /// Gets a simple binary STL file content representing a triangle.
    /// </summary>
    public static byte[] SimpleBinarySTL
    {
        get
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            // Header (80 bytes)
            var header = "Simple Binary STL Triangle".PadRight(80, '\0');
            writer.Write(Encoding.UTF8.GetBytes(header));

            // Triangle count
            writer.Write((uint)1);

            // Triangle data
            // Normal vector
            writer.Write(0.0f); // nx
            writer.Write(0.0f); // ny
            writer.Write(1.0f); // nz

            // Vertex 1
            writer.Write(0.0f); // x
            writer.Write(0.0f); // y
            writer.Write(0.0f); // z

            // Vertex 2
            writer.Write(1.0f); // x
            writer.Write(0.0f); // y
            writer.Write(0.0f); // z

            // Vertex 3
            writer.Write(0.5f); // x
            writer.Write(1.0f); // y
            writer.Write(0.0f); // z

            // Attribute byte count
            writer.Write((ushort)0);

            return stream.ToArray();
        }
    }

    /// <summary>
    /// Gets a more complex ASCII STL file content representing a cube.
    /// </summary>
    public static string CubeAsciiSTL => @"solid Cube
  facet normal 0.0 0.0 1.0
    outer loop
      vertex 0.0 0.0 1.0
      vertex 1.0 0.0 1.0
      vertex 1.0 1.0 1.0
    endloop
  endfacet
  facet normal 0.0 0.0 1.0
    outer loop
      vertex 0.0 0.0 1.0
      vertex 1.0 1.0 1.0
      vertex 0.0 1.0 1.0
    endloop
  endfacet
  facet normal 0.0 0.0 -1.0
    outer loop
      vertex 0.0 0.0 0.0
      vertex 1.0 1.0 0.0
      vertex 1.0 0.0 0.0
    endloop
  endfacet
  facet normal 0.0 0.0 -1.0
    outer loop
      vertex 0.0 0.0 0.0
      vertex 0.0 1.0 0.0
      vertex 1.0 1.0 0.0
    endloop
  endfacet
  facet normal 0.0 1.0 0.0
    outer loop
      vertex 0.0 1.0 0.0
      vertex 1.0 1.0 0.0
      vertex 1.0 1.0 1.0
    endloop
  endfacet
  facet normal 0.0 1.0 0.0
    outer loop
      vertex 0.0 1.0 0.0
      vertex 1.0 1.0 1.0
      vertex 0.0 1.0 1.0
    endloop
  endfacet
  facet normal 0.0 -1.0 0.0
    outer loop
      vertex 0.0 0.0 0.0
      vertex 1.0 0.0 1.0
      vertex 1.0 0.0 0.0
    endloop
  endfacet
  facet normal 0.0 -1.0 0.0
    outer loop
      vertex 0.0 0.0 0.0
      vertex 0.0 0.0 1.0
      vertex 1.0 0.0 1.0
    endloop
  endfacet
  facet normal 1.0 0.0 0.0
    outer loop
      vertex 1.0 0.0 0.0
      vertex 1.0 1.0 0.0
      vertex 1.0 1.0 1.0
    endloop
  endfacet
  facet normal 1.0 0.0 0.0
    outer loop
      vertex 1.0 0.0 0.0
      vertex 1.0 1.0 1.0
      vertex 1.0 0.0 1.0
    endloop
  endfacet
  facet normal -1.0 0.0 0.0
    outer loop
      vertex 0.0 0.0 0.0
      vertex 0.0 1.0 1.0
      vertex 0.0 1.0 0.0
    endloop
  endfacet
  facet normal -1.0 0.0 0.0
    outer loop
      vertex 0.0 0.0 0.0
      vertex 0.0 0.0 1.0
      vertex 0.0 1.0 1.0
    endloop
  endfacet
endsolid Cube";

    /// <summary>
    /// Gets cube ASCII STL data as bytes.
    /// </summary>
    public static byte[] CubeAsciiSTLBytes => Encoding.UTF8.GetBytes(CubeAsciiSTL);
}
