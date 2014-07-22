using System;
using OctoGhast.UserInterface.Controls;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Theme;

namespace OctoGhast.Framework
{
    public interface IGame : IApplication
    {
        InputManager Input { get; }
        void Start(GameInfo info);
    }
}