using System.Numerics;
using Brutal.GlfwApi;
using Calisto;
using Calisto.Framework.Core;
using Calisto.Framework.Core.Models;
using Calisto.Framework.Render;

sealed class GrottoGame : Game
{
    private const float WorldWidth = 416;
    private const float WorldHeight = 240;
    private const float PlayerSpeed = 75;
    private const float Gravity = 500;
    private const float JumpStrength = 200;
    private static readonly Rectangle[] Platforms =
    [
        new(0, 208, WorldWidth, 32),
        new(48, 176, 80, 16),
        new(144, 144, 64, 16),
        new(240, 112, 80, 16),
        new(336, 160, 64, 16),
    ];

    private readonly Camera2D _camera = new()
    {
        Position = new Vector2(WorldWidth * 0.5f, WorldHeight * 0.5f),
        Zoom = 3,
    };
    private Texture2D[] _idleFrames = [];
    private Texture2D[] _runFrames = [];
    private Texture2D[] _jumpFrames = [];
    private Texture2D _tileSet = null!;
    private Texture2D _backLayer = null!;
    private Texture2D _farLayer = null!;
    private Texture2D _middleLayer = null!;
    private Texture2D _middleLayer2 = null!;
    private Texture2D _logo = null!;
    private Vector2 _playerPosition = new(24, 208);
    private float _verticalVelocity;
    private float _animationTime;
    private AnimationState _animationState;
    private bool _grounded = true;
    private bool _jumpWasDown;
    private int _facing = 1;

    protected override string WindowTitle => "Super Grotto Escape - Calisto";
    protected override int WindowWidth => 1248;
    protected override int WindowHeight => 720;

    protected override void LoadContent()
    {
        _idleFrames = LoadFrames("Player/Idle/player-idle", 6);
        _runFrames = LoadFrames("Player/Run/player-run", 6);
        _jumpFrames = LoadFrames("Player/Jump/player-jump", 2);
        _tileSet = Content.LoadTexture("World/tileset.png");
        _backLayer = Content.LoadTexture("World/back.png");
        _farLayer = Content.LoadTexture("World/far.png");
        _middleLayer = Content.LoadTexture("World/middle.png");
        _middleLayer2 = Content.LoadTexture("World/middle2.png");
        _logo = Content.LoadTexture("World/logo.png");
    }

    protected override void Update(GameTime gameTime)
    {
        var delta = Math.Clamp((float)gameTime.ElapsedGameTime.TotalSeconds, 0, 1f / 30f);
        var horizontal =
            (Keyboard.IsKeyDown(GlfwKey.D) || Keyboard.IsKeyDown(GlfwKey.Right) ? 1 : 0)
            - (Keyboard.IsKeyDown(GlfwKey.A) || Keyboard.IsKeyDown(GlfwKey.Left) ? 1 : 0);
        var jumpDown =
            Keyboard.IsKeyDown(GlfwKey.Space)
            || Keyboard.IsKeyDown(GlfwKey.W)
            || Keyboard.IsKeyDown(GlfwKey.Up);

        if (horizontal != 0)
            _facing = Math.Sign(horizontal);

        if (jumpDown && !_jumpWasDown && _grounded)
        {
            _verticalVelocity = -JumpStrength;
            _grounded = false;
        }
        _jumpWasDown = jumpDown;

        _playerPosition.X = Math.Clamp(
            _playerPosition.X + horizontal * PlayerSpeed * delta,
            8,
            WorldWidth - 8
        );

        var previousFootY = _playerPosition.Y;
        _verticalVelocity += Gravity * delta;
        _playerPosition.Y += _verticalVelocity * delta;
        _grounded = false;

        if (_verticalVelocity >= 0)
        {
            foreach (var platform in Platforms)
            {
                var overlapsHorizontally =
                    _playerPosition.X + 7 > platform.X
                    && _playerPosition.X - 7 < platform.X + platform.Width;
                if (!overlapsHorizontally || previousFootY > platform.Y || _playerPosition.Y < platform.Y)
                    continue;

                _playerPosition.Y = platform.Y;
                _verticalVelocity = 0;
                _grounded = true;
                break;
            }
        }

        if (_playerPosition.Y > WorldHeight + 32)
        {
            _playerPosition = new Vector2(24, 208);
            _verticalVelocity = 0;
            _grounded = true;
        }

        var nextState = !_grounded
            ? (_verticalVelocity < 0 ? AnimationState.Jump : AnimationState.Fall)
            : horizontal == 0 ? AnimationState.Idle : AnimationState.Run;
        if (nextState != _animationState)
        {
            _animationState = nextState;
            _animationTime = 0;
        }
        else
        {
            _animationTime += delta;
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(21, 17, 34));
        SpriteBatch.Begin(_camera);

        DrawBackgroundLayer(_farLayer, 56, 42);
        DrawBackgroundLayer(_backLayer, 56, 22);
        DrawBackgroundLayer(_middleLayer, -24, 76);
        DrawBackgroundLayer(_middleLayer2, 232, 24);
        DrawPlatforms();
        DrawPlayer();
        SpriteBatch.Draw(_logo, new Rectangle(8, 8, 142, 32), Color.White);

        SpriteBatch.End();
    }

    private Texture2D[] LoadFrames(string pathPrefix, int frameCount)
    {
        var frames = new Texture2D[frameCount];
        for (var index = 0; index < frameCount; index++)
            frames[index] = Content.LoadTexture($"{pathPrefix}{index + 1}.png");
        return frames;
    }

    private void DrawBackgroundLayer(Texture2D texture, float x, float y) =>
        SpriteBatch.Draw(texture, new Vector2(x, y), Color.White);

    private void DrawPlatforms()
    {
        foreach (var platform in Platforms)
        {
            var rows = platform.Height > 16 ? 2 : 1;
            var columns = (int)(platform.Width / 16);
            for (var row = 0; row < rows; row++)
                for (var column = 0; column < columns; column++)
                {
                    var source = new Rectangle(64, 112, 16, 16);
                    var destination = new Rectangle(
                        platform.X + column * 16,
                        platform.Y + row * 16,
                        16,
                        16
                    );
                    SpriteBatch.Draw(_tileSet, source, destination, Color.White);
                }
        }
    }

    private void DrawPlayer()
    {
        var frames = _animationState switch
        {
            AnimationState.Run => _runFrames,
            AnimationState.Jump or AnimationState.Fall => _jumpFrames,
            _ => _idleFrames,
        };
        var frameRate = _animationState switch
        {
            AnimationState.Run => 10,
            AnimationState.Idle => 7,
            _ => 1,
        };
        var frameIndex = _animationState switch
        {
            AnimationState.Jump => 0,
            AnimationState.Fall => 1,
            _ => (int)(_animationTime * frameRate) % frames.Length,
        };
        var frame = frames[frameIndex];
        var destination = new Rectangle(
            _playerPosition.X - frame.Width * 0.5f,
            _playerPosition.Y - frame.Height,
            frame.Width,
            frame.Height
        );
        SpriteBatch.Draw(frame, destination, Color.White, _facing < 0);
    }

    private enum AnimationState
    {
        Idle,
        Run,
        Jump,
        Fall,
    }
}
