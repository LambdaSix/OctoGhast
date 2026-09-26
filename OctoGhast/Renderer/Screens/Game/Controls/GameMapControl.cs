using System;
using InfiniMap;
using OctoGhast.DataStructures.Map;
using OctoGhast.Renderer.View;
using OctoGhast.UserInterface.Controls;
using OctoGhast.UserInterface.Core;

namespace OctoGhast.Renderer.Screens.Game.Controls
{
    public class GameMapControlTemplate : PanelTemplate
    {
        public IGameViewModel Model { get; set; }

        public GameMapControlTemplate() {
            Size = new Size(80, 24);
        }
    }

    /// <summary>
    /// Handles the Drawing of the World to the screen.
    /// No handling of game logic is done here, that should be handled by WorldInstance and Systems
    /// </summary>
    public class GameMapControl : Panel
    {
        /// <summary>
        /// Access to the WorldInstance is handled via the ViewModel
        /// </summary>
        IGameViewModel Model { get; set; }

        public Map2D<ITile> Map => Model.World.Map;

        public GameMapControl(GameMapControlTemplate template) : base(template) {
            Model = template.Model;
            Size = template.CalculateSize();
        }

    }
}
