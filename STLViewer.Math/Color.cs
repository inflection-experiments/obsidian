namespace STLViewer.Math;

/// <summary>
/// Represents a color with red, green, blue, and alpha components.
/// </summary>
public readonly record struct Color
{
    /// <summary>
    /// The red component (0.0 to 1.0).
    /// </summary>
    public float R { get; }

    /// <summary>
    /// The green component (0.0 to 1.0).
    /// </summary>
    public float G { get; }

    /// <summary>
    /// The blue component (0.0 to 1.0).
    /// </summary>
    public float B { get; }

    /// <summary>
    /// The alpha component (0.0 to 1.0).
    /// </summary>
    public float A { get; }

    /// <summary>
    /// Initializes a new instance of the Color struct.
    /// </summary>
    /// <param name="r">The red component (0.0 to 1.0).</param>
    /// <param name="g">The green component (0.0 to 1.0).</param>
    /// <param name="b">The blue component (0.0 to 1.0).</param>
    /// <param name="a">The alpha component (0.0 to 1.0).</param>
    public Color(float r, float g, float b, float a = 1.0f)
    {
        R = System.Math.Clamp(r, 0.0f, 1.0f);
        G = System.Math.Clamp(g, 0.0f, 1.0f);
        B = System.Math.Clamp(b, 0.0f, 1.0f);
        A = System.Math.Clamp(a, 0.0f, 1.0f);
    }

    /// <summary>
    /// Creates a color from byte values (0-255).
    /// </summary>
    /// <param name="r">The red component (0-255).</param>
    /// <param name="g">The green component (0-255).</param>
    /// <param name="b">The blue component (0-255).</param>
    /// <param name="a">The alpha component (0-255).</param>
    /// <returns>A new Color instance.</returns>
    public static Color FromBytes(byte r, byte g, byte b, byte a = 255)
    {
        return new Color(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
    }

    /// <summary>
    /// Creates a color from a hexadecimal string (e.g., "#FF0000" for red).
    /// </summary>
    /// <param name="hex">The hexadecimal color string.</param>
    /// <returns>A new Color instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the hex string is invalid.</exception>
    public static Color FromHex(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            throw new ArgumentException("Hex string cannot be null or empty.", nameof(hex));

        hex = hex.TrimStart('#');

        if (hex.Length != 6 && hex.Length != 8)
            throw new ArgumentException("Hex string must be 6 or 8 characters long.", nameof(hex));

        try
        {
            var r = Convert.ToByte(hex.Substring(0, 2), 16);
            var g = Convert.ToByte(hex.Substring(2, 2), 16);
            var b = Convert.ToByte(hex.Substring(4, 2), 16);
            var a = hex.Length == 8 ? Convert.ToByte(hex.Substring(6, 2), 16) : (byte)255;

            return FromBytes(r, g, b, a);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid hex color string: {hex}", nameof(hex), ex);
        }
    }

    /// <summary>
    /// Creates a color from HSV (Hue, Saturation, Value) components.
    /// </summary>
    /// <param name="hue">The hue component (0.0 to 360.0).</param>
    /// <param name="saturation">The saturation component (0.0 to 1.0).</param>
    /// <param name="value">The value component (0.0 to 1.0).</param>
    /// <param name="alpha">The alpha component (0.0 to 1.0).</param>
    /// <returns>A new Color instance.</returns>
    public static Color FromHSV(float hue, float saturation, float value, float alpha = 1.0f)
    {
        hue = hue % 360.0f;
        if (hue < 0) hue += 360.0f;

        saturation = System.Math.Clamp(saturation, 0.0f, 1.0f);
        value = System.Math.Clamp(value, 0.0f, 1.0f);
        alpha = System.Math.Clamp(alpha, 0.0f, 1.0f);

        var c = value * saturation;
        var x = c * (1 - MathF.Abs((hue / 60.0f) % 2 - 1));
        var m = value - c;

        float r, g, b;

        if (hue < 60)
        {
            r = c; g = x; b = 0;
        }
        else if (hue < 120)
        {
            r = x; g = c; b = 0;
        }
        else if (hue < 180)
        {
            r = 0; g = c; b = x;
        }
        else if (hue < 240)
        {
            r = 0; g = x; b = c;
        }
        else if (hue < 300)
        {
            r = x; g = 0; b = c;
        }
        else
        {
            r = c; g = 0; b = x;
        }

        return new Color(r + m, g + m, b + m, alpha);
    }

    // Predefined colors
    public static Color Transparent => new(0, 0, 0, 0);
    public static Color Black => new(0, 0, 0);
    public static Color White => new(1, 1, 1);
    public static Color Red => new(1, 0, 0);
    public static Color Green => new(0, 1, 0);
    public static Color Blue => new(0, 0, 1);
    public static Color Yellow => new(1, 1, 0);
    public static Color Cyan => new(0, 1, 1);
    public static Color Magenta => new(1, 0, 1);
    public static Color Gray => new(0.5f, 0.5f, 0.5f);
    public static Color LightGray => new(0.75f, 0.75f, 0.75f);
    public static Color DarkGray => new(0.25f, 0.25f, 0.25f);

    /// <summary>
    /// Gets the red component as a byte value (0-255).
    /// </summary>
    public byte RByte => (byte)(R * 255);

    /// <summary>
    /// Gets the green component as a byte value (0-255).
    /// </summary>
    public byte GByte => (byte)(G * 255);

    /// <summary>
    /// Gets the blue component as a byte value (0-255).
    /// </summary>
    public byte BByte => (byte)(B * 255);

    /// <summary>
    /// Gets the alpha component as a byte value (0-255).
    /// </summary>
    public byte AByte => (byte)(A * 255);

    /// <summary>
    /// Converts this color to a hexadecimal string representation.
    /// </summary>
    /// <param name="includeAlpha">Whether to include the alpha component.</param>
    /// <returns>A hexadecimal color string.</returns>
    public string ToHex(bool includeAlpha = false)
    {
        if (includeAlpha)
            return $"#{RByte:X2}{GByte:X2}{BByte:X2}{AByte:X2}";
        else
            return $"#{RByte:X2}{GByte:X2}{BByte:X2}";
    }

    /// <summary>
    /// Converts this color to HSV components.
    /// </summary>
    /// <returns>A tuple containing hue, saturation, and value.</returns>
    public (float Hue, float Saturation, float Value) ToHSV()
    {
        var max = MathF.Max(R, MathF.Max(G, B));
        var min = MathF.Min(R, MathF.Min(G, B));
        var delta = max - min;

        // Value
        var value = max;

        // Saturation
        var saturation = max == 0 ? 0 : delta / max;

        // Hue
        float hue = 0;
        if (delta != 0)
        {
            if (max == R)
                hue = 60 * (((G - B) / delta) % 6);
            else if (max == G)
                hue = 60 * ((B - R) / delta + 2);
            else if (max == B)
                hue = 60 * ((R - G) / delta + 4);
        }

        if (hue < 0) hue += 360;

        return (hue, saturation, value);
    }

    /// <summary>
    /// Linearly interpolates between two colors.
    /// </summary>
    /// <param name="start">The start color.</param>
    /// <param name="end">The end color.</param>
    /// <param name="t">The interpolation factor (0.0 to 1.0).</param>
    /// <returns>The interpolated color.</returns>
    public static Color Lerp(Color start, Color end, float t)
    {
        t = System.Math.Clamp(t, 0.0f, 1.0f);
        return new Color(
            start.R + (end.R - start.R) * t,
            start.G + (end.G - start.G) * t,
            start.B + (end.B - start.B) * t,
            start.A + (end.A - start.A) * t
        );
    }

    /// <summary>
    /// Creates a color with the specified alpha value.
    /// </summary>
    /// <param name="alpha">The new alpha value (0.0 to 1.0).</param>
    /// <returns>A new color with the specified alpha.</returns>
    public Color WithAlpha(float alpha)
    {
        return new Color(R, G, B, alpha);
    }

    /// <summary>
    /// Creates a darker version of this color.
    /// </summary>
    /// <param name="factor">The darkening factor (0.0 to 1.0).</param>
    /// <returns>A darker color.</returns>
    public Color Darken(float factor = 0.2f)
    {
        factor = System.Math.Clamp(factor, 0.0f, 1.0f);
        var scale = 1.0f - factor;
        return new Color(R * scale, G * scale, B * scale, A);
    }

    /// <summary>
    /// Creates a lighter version of this color.
    /// </summary>
    /// <param name="factor">The lightening factor (0.0 to 1.0).</param>
    /// <returns>A lighter color.</returns>
    public Color Lighten(float factor = 0.2f)
    {
        factor = System.Math.Clamp(factor, 0.0f, 1.0f);
        return new Color(
            R + (1.0f - R) * factor,
            G + (1.0f - G) * factor,
            B + (1.0f - B) * factor,
            A
        );
    }

    public override string ToString()
    {
        return $"Color(R:{R:F2}, G:{G:F2}, B:{B:F2}, A:{A:F2})";
    }
}
