using UnityEngine;

namespace Windpost.Input
{
    public interface IInputSource
    {
        float Rudder { get; }
        float Sail { get; }
        bool InteractPressed { get; }
        bool MenuPressed { get; }
        Vector2 Look { get; }
        Vector2 Turn { get; }
    }
}

