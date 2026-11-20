using Brutal.GlfwApi;

namespace Calisto.Framework.Input;

public readonly struct KeyboardState
{
    private readonly GlfwWindow _window;

    internal KeyboardState(GlfwWindow window) => _window = window;

    public bool IsKeyDown(KeyboardKey key) => _window.GetKey((GlfwKey)key) == GlfwKeyAction.Press;

    public bool IsKeyUp(KeyboardKey key) => !IsKeyDown(key);

    public bool AreKeysDown(params KeyboardKey[] keys)
    {
        foreach (var key in keys)
        {
            if (!IsKeyDown(key))
                return false;
        }

        return true;
    }
}
