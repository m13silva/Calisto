using System.Numerics;

namespace Calisto.Framework.Core.Models;

public sealed class Camera2D
{
    private float _zoom = 1;

    public Vector2 Position { get; set; }
    public float Rotation { get; set; }

    public float Zoom
    {
        get => _zoom;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
            _zoom = value;
        }
    }

    internal Matrix3x2 CreateTransform(int viewportWidth, int viewportHeight) =>
        Matrix3x2.CreateTranslation(-Position)
        * Matrix3x2.CreateRotation(-Rotation)
        * Matrix3x2.CreateScale(_zoom)
        * Matrix3x2.CreateTranslation(viewportWidth * 0.5f, viewportHeight * 0.5f);
}
