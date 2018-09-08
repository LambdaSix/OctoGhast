using System;

namespace OctoGhast.UserInterface.Controls {
    public class WeakFunc<TTarget, TResult> where TTarget : class {
        private readonly WeakReference<TTarget> _target;
        private readonly Func<TTarget, TResult> _weakFunc;

        public WeakFunc(TTarget target, Func<TTarget, TResult> weakFunc) {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            _target = new WeakReference<TTarget>(target);
            _weakFunc = weakFunc ?? throw new ArgumentNullException(nameof(weakFunc));
        }

        public TResult Invoke() {
            if (_target.TryGetTarget(out TTarget target))
                return _weakFunc(target);
            return default(TResult);
        }
    }
}