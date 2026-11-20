using Calisto.Framework.Render;

namespace Calisto.Framework.Core.Manager;

public sealed class ContentManager : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly string _rootDirectory;
    private readonly Dictionary<string, Texture2D> _textures = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public ContentManager(GraphicsDevice graphicsDevice, string rootDirectory)
    {
        ArgumentNullException.ThrowIfNull(graphicsDevice);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);

        _graphicsDevice = graphicsDevice;
        _rootDirectory = Path.GetFullPath(rootDirectory);
    }

    public Texture2D LoadTexture(string assetName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(assetName);

        var path = Path.GetFullPath(Path.Combine(_rootDirectory, assetName));
        if (_textures.TryGetValue(path, out var texture))
            return texture;

        texture = Texture2D.FromFile(_graphicsDevice, path);
        _textures.Add(path, texture);
        return texture;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var texture in _textures.Values)
            texture.Dispose();
        _textures.Clear();
        _disposed = true;
    }
}
