using System.Diagnostics;
using Brutal;
using Brutal.GlfwApi;
using Brutal.Numerics;
using Brutal.OpenGlApi;
using Calisto.Framework.Core.Manager;
using Calisto.Framework.Render;
using Calisto.Framework.Input;

namespace Calisto.Framework.Core;

public abstract class Game
{
    private bool _exitRequested;

    protected virtual string WindowTitle => "Game";
    protected virtual int WindowWidth => 1280;
    protected virtual int WindowHeight => 720;
    protected virtual bool VerticalSync => true;

    protected GraphicsDevice GraphicsDevice { get; private set; } = null!;
    protected SpriteBatch SpriteBatch { get; private set; } = null!;
    protected ContentManager Content { get; private set; } = null!;
    protected KeyboardState Keyboard { get; private set; }
    protected MouseState Mouse { get; private set; }

    public void Run()
    {
        if (!Glfw.Init())
        {
            var errorCode = Glfw.GetError(out var errorMessage);
            throw new InvalidOperationException($"Failed to initialize GLFW: {errorCode}, {errorMessage}");
        }

        try
        {
            RunGame();
        }
        finally
        {
            Glfw.Terminate();
        }
    }

    public void Exit() => _exitRequested = true;

    protected virtual void LoadContent() { }
    protected virtual void UnloadContent() { }
    protected abstract void Update(GameTime gameTime);
    protected abstract void Draw(GameTime gameTime);

    private void RunGame()
    {
        Glfw.WindowHint(GlfwWindowHint.ContextVersionMajor, 4);
        Glfw.WindowHint(GlfwWindowHint.ContextVersionMinor, 1);
        Glfw.WindowHint(GlfwWindowHint.OpenGlProfile, Glfw.Constants.GLFW_OPENGL_CORE_PROFILE);
        if (Platform.IsMacOS)
            Glfw.WindowHint(GlfwWindowHint.OpenGlForwardCompat, 1);

        using var window = Glfw.CreateWindow(
            new GlfwWindow.CreateInfo
            {
                Title = WindowTitle,
                Size = new int2(WindowWidth, WindowHeight),
                Monitor = GlfwMonitor.Null,
                Share = null,
            }
        );
        window.MakeContextCurrent();
        Glfw.SwapInterval(VerticalSync ? 1 : 0);

        unsafe
        {
            if (!Glad.Initialize(Glfw.PInvoke.GetProcAddress))
                throw new InvalidOperationException("Failed to load OpenGL functions with GLAD.");
        }

        window.OnKey += (handle, key, _, action, _) =>
        {
            if (key == GlfwKey.Escape && action == GlfwKeyAction.Press)
                handle.ShouldClose = true;
        };

        GraphicsDevice = new GraphicsDevice(window);
        SpriteBatch = new SpriteBatch(GraphicsDevice);
        Content = new ContentManager(GraphicsDevice, Path.Combine(AppContext.BaseDirectory, "Content"));
        Keyboard = new KeyboardState(window);
        Mouse = new MouseState(window);

        try
        {
            LoadContent();
            var timer = Stopwatch.StartNew();
            var previousTime = timer.Elapsed;

            while (!window.ShouldClose && !_exitRequested)
            {
                Glfw.PollEvents();
                var now = timer.Elapsed;
                var gameTime = new GameTime(now, now - previousTime);
                previousTime = now;

                Update(gameTime);
                if (_exitRequested || window.ShouldClose)
                    break;

                Draw(gameTime);
                window.SwapBuffers();
            }
        }
        finally
        {
            try
            {
                UnloadContent();
            }
            finally
            {
                try
                {
                    Content.Dispose();
                }
                finally
                {
                    SpriteBatch.Dispose();
                }
            }
        }
    }
}
