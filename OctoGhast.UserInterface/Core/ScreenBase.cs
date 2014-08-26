namespace OctoGhast.UserInterface.Core
{
    public abstract class ScreenBase : Manager
    {
        /// <summary>
        /// Navigate to <param name="screen"></param> on the next UI tick.
        /// </summary>
        /// <param name="screen">Screen to navigate to</param>
        public void NavigateTo(ScreenBase screen) {
            ParentWindow.EnqueueScreen(screen);
        }
    }
}