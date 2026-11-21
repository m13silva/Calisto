using Brutal.Numerics;
using Brutal.OpenGlApi;
using Calisto.Framework.Core.Models;
using System.Runtime.InteropServices;
using Color = Calisto.Framework.Core.Models.Color;

namespace Calisto.Framework.Render;

public sealed class SpriteBatch : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly GlVertexArray _vertexArray;
    private readonly GlBuffer _vertexBuffer;
    private readonly GlProgram _program;
    private readonly Texture2D _whiteTexture;
    private readonly List<Vertex> _vertices = [];
    private readonly List<DrawRange> _ranges = [];
    private Texture2D? _lastTexture;
    private float3x2 _transform = float3x2.Identity;
    private bool _begun;
    private bool _disposed;

    internal SpriteBatch(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _program = CreateProgram();
        _vertexArray = OpenGl.GenVertexArray().Bind();
        _vertexBuffer = OpenGl.GenBuffer();
        _vertexBuffer.Bind(GlBindTarget.ArrayBuffer);

        var stride = sizeof(float) * 8;
        OpenGl.VertexAttribute(new GlAttribute(0, 2, OpenGl.GL_FLOAT, GlScaling.Float, stride, 0));
        OpenGl.VertexAttribute(new GlAttribute(1, 2, OpenGl.GL_FLOAT, GlScaling.Float, stride, sizeof(float) * 2));
        OpenGl.VertexAttribute(new GlAttribute(2, 4, OpenGl.GL_FLOAT, GlScaling.Float, stride, sizeof(float) * 4));
        OpenGl.EnableVertexAttribArray(0);
        OpenGl.EnableVertexAttribArray(1);
        OpenGl.EnableVertexAttribArray(2);

        _whiteTexture = new Texture2D(graphicsDevice, 1, 1, [255, 255, 255, 255]);
    }

    public void Begin(Camera2D? camera = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_begun)
            throw new InvalidOperationException("SpriteBatch.Begin cannot be called while a batch is active.");

        var viewport = _graphicsDevice.ViewportSize;
        _transform = camera?.CreateTransform(viewport.X, viewport.Y) ?? float3x2.Identity;
        _vertices.Clear();
        _ranges.Clear();
        _lastTexture = null;
        _begun = true;
    }

    public void Draw(Texture2D texture, float2 position, Color tint)
    {
        ArgumentNullException.ThrowIfNull(texture);
        Draw(texture, new Rectangle(position.X, position.Y, texture.Width, texture.Height), tint);
    }

    public void Draw(Texture2D texture, Rectangle destination, Color tint)
    {
        ArgumentNullException.ThrowIfNull(texture);
        AddQuad(texture, destination, tint, 0, new Rectangle(0, 0, texture.Width, texture.Height), false);
    }

    public void Draw(Texture2D texture, Rectangle destination, Color tint, bool flipHorizontally)
    {
        ArgumentNullException.ThrowIfNull(texture);
        AddQuad(
            texture,
            destination,
            tint,
            0,
            new Rectangle(0, 0, texture.Width, texture.Height),
            flipHorizontally
        );
    }

    public void Draw(
        Texture2D texture,
        Rectangle source,
        Rectangle destination,
        Color tint,
        bool flipHorizontally = false
    )
    {
        ArgumentNullException.ThrowIfNull(texture);
        AddQuad(texture, destination, tint, 0, source, flipHorizontally);
    }

    public void DrawRectangle(Rectangle destination, Color color) =>
        AddQuad(_whiteTexture, destination, color, 0, new Rectangle(0, 0, 1, 1), false);

    public void DrawRectangle(Rectangle destination, Color color, float rotation) =>
        AddQuad(_whiteTexture, destination, color, rotation, new Rectangle(0, 0, 1, 1), false);

    public void DrawLine(float2 start, float2 end, float thickness, Color color)
    {
        var delta = end - start;
        DrawRectangle(
            new Rectangle(start.X, start.Y - thickness * 0.5f, delta.Length(), thickness),
            color,
            MathF.Atan2(delta.Y, delta.X)
        );
    }

    public void DrawCircle(float2 center, float radius, Color color, int segments = 16)
    {
        EnsureBegun();
        if (radius <= 0 || segments < 3)
            return;

        EnsureRange(_whiteTexture);
        var virtualSize = _graphicsDevice.VirtualSize;
        if (virtualSize.X <= 0 || virtualSize.Y <= 0)
            return;

        var clipCenter = ToClip(float2.Transform(center, _transform));
        var floatColor = ToFloatColor(color);
        for (var i = 0; i < segments; i++)
        {
            var angle0 = MathF.Tau * i / segments;
            var angle1 = MathF.Tau * (i + 1) / segments;
            var p0 = ToClip(
                float2.Transform(center + new float2(MathF.Cos(angle0), MathF.Sin(angle0)) * radius, _transform)
            );
            var p1 = ToClip(
                float2.Transform(center + new float2(MathF.Cos(angle1), MathF.Sin(angle1)) * radius, _transform)
            );
            AddTriangle(clipCenter, p0, p1, floatColor);
        }
    }

    public void End()
    {
        EnsureBegun();
        _begun = false;
        if (_vertices.Count == 0)
            return;

        _program.Use();
        _vertexArray.Bind();
        foreach (var range in _ranges)
        {
            OpenGl.BufferData(
                GlBindTarget.ArrayBuffer,
                CollectionsMarshal.AsSpan(_vertices).Slice(range.Start, range.Count),
                GlBufferUsage.StreamDraw
            );
            OpenGl.BindTexture(OpenGl.GL_TEXTURE_2D, range.Texture.Id);
            OpenGl.Draw(GlTopology.Triangles, range.Count, 1, 0, 0);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _whiteTexture.Dispose();
        _program.Dispose();
        OpenGl.DeleteBuffers([_vertexBuffer]);
        OpenGl.DeleteVertexArrays([_vertexArray]);
        _disposed = true;
    }

    private void AddQuad(
        Texture2D texture,
        Rectangle destination,
        Color tint,
        float rotation,
        Rectangle source,
        bool flipHorizontally
    )
    {
        EnsureBegun();
        if (
            destination.Width <= 0
            || destination.Height <= 0
            || source.Width <= 0
            || source.Height <= 0
        )
            return;

        if (
            source.X < 0
            || source.Y < 0
            || source.X + source.Width > texture.Width
            || source.Y + source.Height > texture.Height
        )
            throw new ArgumentOutOfRangeException(nameof(source), "Source rectangle must fit within the texture.");

        var virtualSize = _graphicsDevice.VirtualSize;
        if (virtualSize.X <= 0 || virtualSize.Y <= 0)
            return;

        EnsureRange(texture);
        var centerX = destination.X + destination.Width * 0.5f;
        var centerY = destination.Y + destination.Height * 0.5f;
        var cosine = MathF.Cos(rotation);
        var sine = MathF.Sin(rotation);
        var halfWidth = destination.Width * 0.5f;
        var halfHeight = destination.Height * 0.5f;
        var leftU = source.X / texture.Width;
        var rightU = (source.X + source.Width) / texture.Width;
        if (flipHorizontally)
            (leftU, rightU) = (rightU, leftU);
        var topV = source.Y / texture.Height;
        var bottomV = (source.Y + source.Height) / texture.Height;
        var topLeft = ToClip(float2.Transform(
            new float2(centerX - halfWidth * cosine + halfHeight * sine,
                centerY - halfWidth * sine - halfHeight * cosine), _transform));
        var topRight = ToClip(float2.Transform(
            new float2(centerX + halfWidth * cosine + halfHeight * sine,
                centerY + halfWidth * sine - halfHeight * cosine), _transform));
        var bottomRight = ToClip(float2.Transform(
            new float2(centerX + halfWidth * cosine - halfHeight * sine,
                centerY + halfWidth * sine + halfHeight * cosine), _transform));
        var bottomLeft = ToClip(float2.Transform(
            new float2(centerX - halfWidth * cosine - halfHeight * sine,
                centerY - halfWidth * sine + halfHeight * cosine), _transform));
        var color = ToFloatColor(tint);

        AddVertex(topLeft, leftU, topV, color);
        AddVertex(topRight, rightU, topV, color);
        AddVertex(bottomRight, rightU, bottomV, color);
        AddVertex(topLeft, leftU, topV, color);
        AddVertex(bottomRight, rightU, bottomV, color);
        AddVertex(bottomLeft, leftU, bottomV, color);
        AddToLastRange(6);
    }

    private void AddVertex(float x, float y, float u, float v, float4 color) =>
        _vertices.Add(new Vertex(new float2(x, y), new float2(u, v), color));

    private void AddVertex(float2 position, float u, float v, float4 color) =>
        AddVertex(position.X, position.Y, u, v, color);

    private void AddTriangle(float2 a, float2 b, float2 c, float4 color)
    {
        AddVertex(a, 0, 0, color);
        AddVertex(b, 0, 0, color);
        AddVertex(c, 0, 0, color);
        AddToLastRange(3);
    }

    private void EnsureRange(Texture2D texture)
    {
        if (_lastTexture == texture)
            return;

        _ranges.Add(new DrawRange(_vertices.Count, 0, texture));
        _lastTexture = texture;
    }

    private void AddToLastRange(int count)
    {
        var lastRange = _ranges[^1];
        _ranges[^1] = lastRange with { Count = lastRange.Count + count };
    }

    private float2 ToClip(float2 screenPosition)
    {
        var viewport = _graphicsDevice.ViewportSize;
        if (viewport.X <= 0 || viewport.Y <= 0)
            return float2.Zero;

        var position = _graphicsDevice.ToFramebuffer(screenPosition);
        return new float2(
            position.X / viewport.X * 2f - 1f,
            1f - position.Y / viewport.Y * 2f
        );
    }

    private static float4 ToFloatColor(Color color) =>
        new(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);

    private void EnsureBegun()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_begun)
            throw new InvalidOperationException("Call SpriteBatch.Begin before drawing.");
    }

    private static GlProgram CreateProgram()
    {
        using var vertexShader = OpenGl
            .CreateShader(GlShaderType.Vertex)
            .SetSource(
                """
                #version 410 core
                layout (location = 0) in vec2 aPosition;
                layout (location = 1) in vec2 aUv;
                layout (location = 2) in vec4 aColor;
                out vec2 uv;
                out vec4 color;
                void main()
                {
                    uv = aUv;
                    color = aColor;
                    gl_Position = vec4(aPosition, 0.0, 1.0);
                }
                """u8
            )
            .Compile();

        using var fragmentShader = OpenGl
            .CreateShader(GlShaderType.Fragment)
            .SetSource(
                """
                #version 410 core
                in vec2 uv;
                in vec4 color;
                uniform sampler2D tex;
                out vec4 fragColor;
                void main()
                {
                    fragColor = texture(tex, uv) * color;
                }
                """u8
            )
            .Compile();

        var program = OpenGl.CreateProgram().Attach(vertexShader, fragmentShader).Link();
        program.Uniform("tex"u8, 0);
        return program;
    }

    private readonly record struct Vertex(float2 Position, float2 Uv, float4 Color);
    private readonly record struct DrawRange(int Start, int Count, Texture2D Texture);
}
