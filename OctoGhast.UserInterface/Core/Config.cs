using System;

namespace OctoGhast.UserInterface.Core
{
    public class Config
    {
        public int Width { get { return WidthFunc(); } }
        public int Height { get { return HeightFunc(); } }

        public static Func<int> WidthFunc { get; set; } 
        public static Func<int> HeightFunc { get; set; } 
    }
}