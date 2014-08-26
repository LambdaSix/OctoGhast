using System;
using System.Linq;
using System.Reflection;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Controls;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Core.Interface;
using OctoGhast.UserInterface.Templates;

namespace OctoGhast.Renderer.Screens
{
    public class AboutScreen : ScreenBase
    {
        public override void OnSettingUp() {
            base.OnSettingUp();

            var windowSize = ParentWindow.ParentApplication.CurrentWindow.Size;

            var versionNumber = (AssemblyFileVersionAttribute)
                    Assembly.GetExecutingAssembly()
                        .GetCustomAttributes(typeof (AssemblyFileVersionAttribute), true)
                        .Single();

            /* Control Templates */

            var asciiText = new[]
            {
                @"________          __          ________.__                    __   ",
                @"\_____  \   _____/  |_ ____  /  _____/|  |__ _____    ______/  |_ ",
                @" /   |   \_/ ___\   __/  _ \/   \  ___|  |  \\__  \  /  ___\   __\",
                @"/    |    \  \___|  |(  <_> \    \_\  |   Y  \/ __ \_\___ \ |  |  ",
                @"\_______  /\___  |__| \____/ \______  |___|  (____  /____  >|__|  ",
                @"        \/     \/                   \/     \/     \/     \/       "
            };

            var line1Template = new LabelTemplate
            {
                Label = asciiText[0],
                UpperLeftPos = new Vec(windowSize.Width / 2 - asciiText[0].Length/2,1),
            };

            var line2Template = new LabelTemplate
            {
                Label = asciiText[1],
            };
            line2Template.AlignTo(LayoutDirection.South, line1Template,-1);

            var line3Template = new LabelTemplate
            {
                Label = asciiText[2],
            };
            line3Template.AlignTo(LayoutDirection.South, line2Template, -1);

            var line4Template = new LabelTemplate
            {
                Label = asciiText[3],
            };
            line4Template.AlignTo(LayoutDirection.South, line3Template, -1);

            var line5Template = new LabelTemplate
            {
                Label = asciiText[4],
            };
            line5Template.AlignTo(LayoutDirection.South, line4Template, -1);

            var line6Template = new LabelTemplate
            {
                Label = asciiText[5],
            };
            line6Template.AlignTo(LayoutDirection.South, line5Template, -1);

            // Text
            var websiteUrlTemplate = new LabelTemplate
            {
                Label = "https://github.com/LambdaSix/OctoGhast"
            };
            websiteUrlTemplate.AlignTo(LayoutDirection.South, line6Template);

            /* Control widgets */
            var line1Label = new Label(line1Template);
            var line2Label = new Label(line2Template);
            var line3Label = new Label(line3Template);
            var line4Label = new Label(line4Template);
            var line5Label = new Label(line5Template);
            var line6Label = new Label(line6Template);
            var websiteUrl = new Label(websiteUrlTemplate);

            ParentWindow.AddControls(new ControlBase[]
            {
                line1Label, line2Label, line3Label, line4Label, line5Label, line6Label, websiteUrl
            });
        }
    }
}