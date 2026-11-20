namespace Calisto.Framework.Core.Models;

public readonly record struct Color(byte R, byte G, byte B, byte A = byte.MaxValue)
{
    public static Color Black => new(0, 0, 0);
    public static Color White => new(byte.MaxValue, byte.MaxValue, byte.MaxValue);
    public static Color CornflowerBlue => new(100, 149, 237);
    public static Color Red => new(byte.MaxValue, 0, 0);
    public static Color LimeGreen => new(50, 205, 50);
}
