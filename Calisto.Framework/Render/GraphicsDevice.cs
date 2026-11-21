using Brutal.GlfwApi;
using Brutal.Numerics;
using Brutal.OpenGlApi;
using Calisto.Framework.Core.Models;

namespace Calisto.Framework.Render;

public sealed class GraphicsDevice
{
    private readonly GlfwWindow _window;
    private int2 _virtualSize;
    private float _presentationScale = 1f;
    private float2 _presentationOffset;

    internal GraphicsDevice(GlfwWindow window)
    {
        _window = window;
        _virtualSize = window.Size;
        UpdateViewport(window.FramebufferSize);
        window.OnFramebufferSize += (_, size) => UpdateViewport(size);
        OpenGl.Enable(GlCapability.Blend);
        OpenGl.BlendFunc(OpenGl.GL_SRC_ALPHA, OpenGl.GL_ONE_MINUS_SRC_ALPHA);
    }

    public int2 ViewportSize => _window.FramebufferSize;

    public int2 VirtualSize => _virtualSize;

    public void SetVirtualResolution(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        _virtualSize = new int2(width, height);
        UpdatePresentation(_window.FramebufferSize);
    }

    public void Clear(Calisto.Framework.Core.Models.Color color)
    {
        OpenGl.ClearColor(
            new float4(
                color.R / 255f,
                color.G / 255f,
                color.B / 255f,
                color.A / 255f
            )
        );
        OpenGl.Clear(GlClearBit.Color);
    }

    internal float2 ToFramebuffer(float2 position) =>
        position * _presentationScale + _presentationOffset;

    private void UpdateViewport(int2 size)
    {
        if (size.X <= 0 || size.Y <= 0)
            return;

        OpenGl.Viewport(0, 0, size.X, size.Y);
        UpdatePresentation(size);
    }

    private void UpdatePresentation(int2 framebufferSize)
    {
        if (framebufferSize.X <= 0 || framebufferSize.Y <= 0)
            return;

        _presentationScale = MathF.Min(
            framebufferSize.X / (float)_virtualSize.X,
            framebufferSize.Y / (float)_virtualSize.Y
        );
        _presentationOffset = new float2(
            (framebufferSize.X - _virtualSize.X * _presentationScale) * 0.5f,
            (framebufferSize.Y - _virtualSize.Y * _presentationScale) * 0.5f
        );
    }
}
