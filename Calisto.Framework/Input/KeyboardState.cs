using Brutal.GlfwApi;

namespace Calisto.Framework.Input;

public readonly struct KeyboardState
{
    private readonly GlfwWindow _window;

    internal KeyboardState(GlfwWindow window) => _window = window;

    public bool IsKeyDown(GlfwKey key) => _window.GetKey(key) == GlfwKeyAction.Press;
}
