using System;
using System.Runtime.Remoting.Channels;
using OctoGhast.Spatial;
using OctoGhast.UserInterface.Controls;
using OctoGhast.UserInterface.Core;
using OctoGhast.UserInterface.Templates;

namespace OctoGhast.Renderer.Screens {
    public static class UIHelper {

        /// <summary>
        /// Display a Yes/No prompt to the user
        /// </summary>
        /// <param name="window"></param>
        /// <param name="queryMsg"></param>
        /// <param name="callback"></param>
        public static void QueryYN(Window window, string queryMsg, Action<bool> callback) {
            // Deactivate everything else first.
            foreach (var control in window.Controls) { control.IsActive = false; }

            // Center the query in the center of the screen
            var topLeft = new Vec((window.Size.Width / 2) - (queryMsg.Length / 2), window.Size.Height / 2);
            var width = (window.Size.Width / 2) - (queryMsg.Length / 2);

            // Attempt to roughly put them in the center with a small gap between them
            var yesPosition = topLeft.Offset(width - 3, 2);
            var noPosition = topLeft.Offset(width + 3, 2);

            var panel = new Panel(new PanelTemplate()
            {
                UpperLeftPos = topLeft,
                HasFrameBorder = true,
                Size = new Size(queryMsg.Length + 2, 6)
            });

            var queryLabel_t = new LabelTemplate
            {
                UpperLeftPos = topLeft.Offset(1, 1), Label = queryMsg
            };
            var queryLabel = new Label(queryLabel_t);

            var queryYes_t = new ButtonTemplate {HasFrameBorder = true, Label = "Yes", UpperLeftPos = yesPosition};
            var queryYes = new Button(queryYes_t);

            var queryNo_t = new ButtonTemplate {HasFrameBorder = true, Label = "No", UpperLeftPos = noPosition};
            var queryNo = new Button(queryNo_t);


            window.AddControls(panel, queryLabel, queryYes, queryNo);

            queryYes.ButtonClick += (sender, args) => {
                callback(true);
                CleanUp();
            };

            queryNo.ButtonClick += (sender, args) => {
                callback(false);
                CleanUp();
            };

            // Remove the added controls and reactivate all the existing controls to give user back control
            void CleanUp() {
                window.RemoveControls(panel, queryLabel, queryYes, queryNo);
                foreach (var windowControl in window.Controls) {
                    windowControl.IsActive = true;
                }
            }
        }
    }
}