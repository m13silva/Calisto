using Brutal.GlfwApi;

namespace Calisto.Framework.Input;

sealed class KeyTransitions
{
    private readonly HashSet<GlfwKey> _pressed = [];
    private readonly HashSet<GlfwKey> _released = [];

    public void Record(GlfwKey key, GlfwKeyAction action)
    {
        switch (action)
        {
            case GlfwKeyAction.Press:
                _pressed.Add(key);
                _released.Remove(key);
                break;
            case GlfwKeyAction.Release:
                _released.Add(key);
                _pressed.Remove(key);
                break;
        }
    }

    public bool WasPressed(GlfwKey key) => _pressed.Contains(key);

    public bool WasReleased(GlfwKey key) => _released.Contains(key);

    public void EndFrame()
    {
        _pressed.Clear();
        _released.Clear();
    }
}
