using OctoGhast.Spatial;

namespace OctoGhast.UserInterface.Core.Messages.Interface
{
    public interface IMouseData
    {
        Vec Position { get; }
        Vec ScreenPosition { get; }
        MouseButton MouseButton { get; }
    }
}