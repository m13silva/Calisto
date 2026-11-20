using System.Diagnostics;
using Brutal.GlfwApi;
using Brutal.TextureApi;
using Calisto.Framework.Core.Models;
using Calisto.Framework.Render;

namespace Calisto.Framework.Core;

internal sealed class SplashScreen : IDisposable
{
    private const string LogoResourceName = "Calisto.Framework.LOGO_BRUTAL_WHT.png";
    private const float FadeInSeconds = 0.5f;
    private const float HoldSeconds = 1.5f;
    private const float FadeOutSeconds = 0.5f;
    private const float DurationSeconds = FadeInSeconds + HoldSeconds + FadeOutSeconds;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _logo;

    public SplashScreen(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = spriteBatch;

        using var stream =
            typeof(SplashScreen).Assembly.GetManifestResourceStream(LogoResourceName)
            ?? throw new InvalidOperationException(
                $"The embedded resource '{LogoResourceName}' was not found."
            );
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        _logo = Texture2D.FromMemory(graphicsDevice, buffer.ToArray(), TextureLoader.FormatType.Png);
    }

    public void Run(GlfwWindow window)
    {
        var timer = Stopwatch.StartNew();
        while (!window.ShouldClose)
        {
            var elapsed = (float)timer.Elapsed.TotalSeconds;
            if (elapsed >= DurationSeconds)
                break;

            Glfw.PollEvents();

            _graphicsDevice.Clear(Color.Black);
            _spriteBatch.Begin();
            DrawLogo(elapsed);
            _spriteBatch.End();
            window.SwapBuffers();
        }
    }

    public void Dispose() => _logo.Dispose();

    private void DrawLogo(float elapsed)
    {
        var viewport = _graphicsDevice.ViewportSize;
        if (viewport.X <= 0 || viewport.Y <= 0)
            return;

        var scale = viewport.X * 0.5f / _logo.Width;
        var width = _logo.Width * scale;
        var height = _logo.Height * scale;
        var destination = new Rectangle(
            (viewport.X - width) * 0.5f,
            (viewport.Y - height) * 0.5f,
            width,
            height
        );

        _spriteBatch.Draw(_logo, destination, new Color(255, 255, 255, Alpha(elapsed)));
    }

    private static byte Alpha(float elapsed)
    {
        var alpha = elapsed < FadeInSeconds
            ? elapsed / FadeInSeconds
            : elapsed < FadeInSeconds + HoldSeconds
                ? 1f
                : 1f - (elapsed - FadeInSeconds - HoldSeconds) / FadeOutSeconds;
        return (byte)(Math.Clamp(alpha, 0f, 1f) * 255);
    }
}
