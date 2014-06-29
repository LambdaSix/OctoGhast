using System;
using OctoGhast.UserInterface.Controls;
using OctoGhast.UserInterface.Theme;

namespace OctoGhast.UserInterface.Core
{
    public interface IApplication : IDisposable
    {
        /// <summary>
        /// Raised when the application sets up.
        /// This is raised after TCOD is initialized, so any code reliant on TCOD
        /// can be used within this event.
        /// This event is for non-standard usage, as most code will subclass Game
        /// and override Setup()
        /// </summary>
        event EventHandler SetupEventHandler;

        /// <summary>
        /// Raised each iteration of the main application loop.
        /// This event is for non-standard usage, as most code will subclass Game
        /// and override Update()
        /// </summary>
        event EventHandler UpdateEventHandler;

        bool IsQuitting { get; set; }
        PigmentMapping Pigments { get; }
        Window CurrentWindow { get; }
        void SetWindow(Window win);
    }
}