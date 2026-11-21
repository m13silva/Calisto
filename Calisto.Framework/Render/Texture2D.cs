using Brutal.OpenGlApi;
using Brutal.TextureApi;
using Brutal.TextureApi.Abstractions;

namespace Calisto.Framework.Render;

public sealed class Texture2D : IDisposable
{
    private readonly GlTexture _texture;
    private bool _disposed;

    public Texture2D(GraphicsDevice graphicsDevice, int width, int height, ReadOnlySpan<byte> rgbaData, bool smooth = false)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Texture dimensions must be positive.");
        if (rgbaData.Length != checked(width * height * 4))
            throw new ArgumentException("Expected four RGBA bytes per pixel.", nameof(rgbaData));

        Width = width;
        Height = height;
        Span<GlTexture> textures = stackalloc GlTexture[1];
        OpenGl.GenTextures(textures);
        _texture = textures[0];
        OpenGl.BindTexture(OpenGl.GL_TEXTURE_2D, _texture.Id);

        unsafe
        {
            fixed (byte* data = rgbaData)
            {
                OpenGl.TexImage2D(
                    OpenGl.GL_TEXTURE_2D,
                    0,
                    (int)OpenGl.GL_RGBA8,
                    width,
                    height,
                    0,
                    OpenGl.GL_RGBA,
                    OpenGl.GL_UNSIGNED_BYTE,
                    data
                );
            }
        }

        OpenGl.TexParameterFloat(OpenGl.GL_TEXTURE_2D, OpenGl.GL_TEXTURE_MIN_FILTER, (int)(smooth ? OpenGl.GL_LINEAR : OpenGl.GL_NEAREST));
        OpenGl.TexParameterFloat(OpenGl.GL_TEXTURE_2D, OpenGl.GL_TEXTURE_MAG_FILTER, (int)(smooth ? OpenGl.GL_LINEAR : OpenGl.GL_NEAREST));
        OpenGl.TexParameterFloat(OpenGl.GL_TEXTURE_2D, OpenGl.GL_TEXTURE_WRAP_S, (int)OpenGl.GL_CLAMP_TO_EDGE);
        OpenGl.TexParameterFloat(OpenGl.GL_TEXTURE_2D, OpenGl.GL_TEXTURE_WRAP_T, (int)OpenGl.GL_CLAMP_TO_EDGE);
    }

    public static Texture2D FromFile(GraphicsDevice graphicsDevice, string filePath)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var image = TextureLoader.LoadFromFile(
            filePath,
            Brutal.TextureApi.Stb.Loader.LoadSettings.ForceRgba8
        );
        try
        {
            if (image.Format != TextureFormat.R8G8B8A8_UNorm)
                throw new NotSupportedException(
                    $"Texture format {image.Format} is not supported; expected RGBA8."
                );

            return new Texture2D(
                graphicsDevice,
                image.Extent.X,
                image.Extent.Y,
                image.ImageData(0, 0, 0)
            );
        }
        finally
        {
            TextureLoader.Unload(image);
        }
    }

    public static Texture2D FromMemory(
        GraphicsDevice graphicsDevice,
        ReadOnlySpan<byte> data,
        TextureLoader.FormatType format
    )
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        if (data.IsEmpty)
            throw new ArgumentException("Image data cannot be empty.", nameof(data));

        var image = TextureLoader.LoadFromMemory(
            data,
            format,
            Brutal.TextureApi.Stb.Loader.LoadSettings.ForceRgba8
        );
        try
        {
            if (image.Format != TextureFormat.R8G8B8A8_UNorm)
                throw new NotSupportedException(
                    $"Texture format {image.Format} is not supported; expected RGBA8."
                );

            return new Texture2D(
                graphicsDevice,
                image.Extent.X,
                image.Extent.Y,
                image.ImageData(0, 0, 0)
            );
        }
        finally
        {
            TextureLoader.Unload(image);
        }
    }

    public int Width { get; }
    public int Height { get; }

    internal uint Id
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _texture.Id;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        OpenGl.DeleteTextures([_texture]);
        _disposed = true;
    }
}
