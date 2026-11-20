using System.Numerics;
using Brutal.GlfwApi;

namespace Calisto.Framework.Input;

public readonly struct MouseState
{
    private readonly GlfwWindow _window;

    internal MouseState(GlfwWindow window) => _window = window;

    public Vector2 Position
    {
        get
        {
            var position = _window.CursorPos;
            return new Vector2((float)position.X, (float)position.Y);
        }
    }

    public bool LeftButtonDown => _window.GetMouseButton(GlfwMouseButton.Left) == GlfwButtonAction.Press;
}
