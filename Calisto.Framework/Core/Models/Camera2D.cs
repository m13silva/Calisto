using Brutal.Numerics;

namespace Calisto.Framework.Core.Models;

public sealed class Camera2D
{
    private float _zoom = 1;

    public float2 Position { get; set; }
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

    internal float3x2 CreateTransform(int viewportWidth, int viewportHeight) =>
        float3x2.CreateTranslation(-Position)
        * float3x2.CreateRotation(-Rotation)
        * float3x2.CreateScale(_zoom)
        * float3x2.CreateTranslation(
            viewportWidth * 0.5f,
            viewportHeight * 0.5f
        );
}
