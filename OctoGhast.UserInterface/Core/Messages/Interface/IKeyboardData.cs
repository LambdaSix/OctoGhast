using libtcod;

namespace OctoGhast.UserInterface.Core.Messages.Interface
{
    public interface IKeyboardData
    {
        char Character { get; }
        TCODKeyCode KeyCode { get; }
        bool IsKeyDown { get; }
        ControlKeys ControlKeys { get; }
    }
}