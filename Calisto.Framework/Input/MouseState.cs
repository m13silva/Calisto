using Brutal.GlfwApi;
using Brutal.Numerics;

namespace Calisto.Framework.Input;

public readonly struct MouseState
{
    private readonly GlfwWindow _window;

    internal MouseState(GlfwWindow window) => _window = window;

    public float2 Position
    {
        get
        {
            var position = _window.CursorPos;
            return new float2((float)position.X, (float)position.Y);
        }
    }

    public bool LeftButtonDown => _window.GetMouseButton(GlfwMouseButton.Left) == GlfwButtonAction.Press;
}
