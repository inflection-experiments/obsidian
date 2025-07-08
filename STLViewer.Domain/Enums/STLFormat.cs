namespace STLViewer.Domain.Enums;

/// <summary>
/// Represents the format of an STL file.
/// </summary>
public enum STLFormat
{
    /// <summary>
    /// Unknown or undetected format.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// ASCII text format STL file.
    /// </summary>
    ASCII = 1,

    /// <summary>
    /// Binary format STL file.
    /// </summary>
    Binary = 2
}
