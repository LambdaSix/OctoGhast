using System;
using RenderLike;

namespace OctoGhast.UserInterface.Core
{
    public class Config
    {
        public int Width { get { return WidthFunc(); } }
        public int Height { get { return HeightFunc(); } }
        public RLConsole RootConsole { get { return RootConsoleFunc(); }}

        public static Func<RLConsole> RootConsoleFunc { get; set; }
        public static Func<int> WidthFunc { get; set; } 
        public static Func<int> HeightFunc { get; set; } 
    }
}