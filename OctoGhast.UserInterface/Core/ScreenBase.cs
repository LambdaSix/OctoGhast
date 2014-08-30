using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using OctoGhast.UserInterface.Core.Messages;

namespace OctoGhast.UserInterface.Core
{
    public abstract class ScreenBase : Manager
    {
        private ConcurrentDictionary<int, Action> actionMap { get; set; }

        protected ScreenBase() {
            actionMap = new ConcurrentDictionary<int, Action>();
        }

        /// <summary>
        /// Navigate to <param name="screen"></param> on the next UI tick.
        /// </summary>
        /// <param name="screen">Screen to navigate to</param>
        public void NavigateTo(ScreenBase screen) {
            ParentWindow.EnqueueScreen(screen);
        }

        public override void OnKeyBindingAction(int action) {
            base.OnKeyBindingAction(action);

            Action callback;
            if (actionMap.TryGetValue(action, out callback)) {
                callback();
            }
        }

        /// <summary>
        /// Register an ActionId to a callback.
        /// </summary>
        /// <param name="actionId"></param>
        /// <param name="action"></param>
        public void RegisterAction(int actionId, Action action) {
            actionMap.AddOrUpdate(actionId, v => action, (i, action1) => action);
        }

        public void UnregisterAction(int actionId) {
            Action unused;
            actionMap.TryRemove(actionId, out unused);
        }
    }
}