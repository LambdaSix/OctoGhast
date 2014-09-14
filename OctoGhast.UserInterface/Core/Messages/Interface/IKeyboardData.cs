using Microsoft.Xna.Framework.Input;

namespace OctoGhast.UserInterface.Core.Messages.Interface
{
    public interface IKeyboardData
    {
        Keys KeyCode { get; }
        bool IsKeyDown { get; }
        ControlKeys ControlKeys { get; }
    }
}