using System;
using libtcod;
using OctoGhast.Framework;
using OctoGhast.Spatial;

namespace OctoGhast
{
	internal class Program
	{
        static void Main(string[] args) {
	        var info = new GameInfo
	        {
	            Title = "UI Test",
	            ScreenSize = new Size(80,40),
	            Font = "celtic_garamond_10x10_gs_tc.png",
	        };

	        UserInterface.Core.Config.HeightFunc = () => info.ScreenSize.Height;
	        UserInterface.Core.Config.WidthFunc = () => info.ScreenSize.Width;

	        using (var application = new OctoghastGame()) {
	            application.Start(info);
	        }
	    }
	}
}