using Brutal.GlfwApi;
using Brutal.Numerics;
using Brutal.OpenGlApi;
using Calisto.Framework.Core.Models;

namespace Calisto.Framework.Render;

public sealed class GraphicsDevice
{
    private readonly GlfwWindow _window;

    internal GraphicsDevice(GlfwWindow window)
    {
        _window = window;
        UpdateViewport(window.FramebufferSize);
        window.OnFramebufferSize += (_, size) => UpdateViewport(size);
        OpenGl.Enable(GlCapability.Blend);
        OpenGl.BlendFunc(OpenGl.GL_SRC_ALPHA, OpenGl.GL_ONE_MINUS_SRC_ALPHA);
    }

    public int2 ViewportSize => _window.FramebufferSize;

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

    private static void UpdateViewport(int2 size)
    {
        if (size.X > 0 && size.Y > 0)
            OpenGl.Viewport(0, 0, size.X, size.Y);
    }
}
