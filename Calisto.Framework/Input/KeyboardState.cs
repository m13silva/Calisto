using Brutal.GlfwApi;

namespace Calisto.Framework.Input;

public readonly struct KeyboardState
{
    private readonly GlfwWindow _window;
    private readonly KeyTransitions _transitions;

    internal KeyboardState(GlfwWindow window)
    {
        _window = window;
        var transitions = new KeyTransitions();
        _transitions = transitions;
        window.OnKey += (_, key, _, action, _) => transitions.Record(key, action);
    }

    public bool IsKeyDown(KeyboardKey key) => _window.GetKey((GlfwKey)key) == GlfwKeyAction.Press;

    public bool IsKeyUp(KeyboardKey key) => !IsKeyDown(key);

    public bool IsKeyPressed(KeyboardKey key) => _transitions.WasPressed((GlfwKey)key);

    public bool IsKeyReleased(KeyboardKey key) => _transitions.WasReleased((GlfwKey)key);

    public bool AreKeysDown(params KeyboardKey[] keys)
    {
        foreach (var key in keys)
        {
            if (!IsKeyDown(key))
                return false;
        }

        return true;
    }

    internal void Update() => _transitions.EndFrame();
}
