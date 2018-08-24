using System;
using System.ComponentModel;
using System.Linq.Expressions;
using OctoGhast.UserInterface.Controls;

namespace OctoGhast.UserInterface.Core {
    public class ConstBindingTarget<T> :BindingTarget{
        public ConstBindingTarget(T value) : base(() => value, BindingMode.OneWay) { }
    }
    public class BindingTarget {
        public Expression<Func<object>> Target { get; set; }
        public BindingMode BindMode { get; set; }
        public Func<object, object> Formatter { get; set; }


        private Func<object> _compiledTarget;
        private Func<object> CompiledTarget => _compiledTarget ?? (_compiledTarget = Target.Compile());
        private object _rootObject;

        public BindingTarget(Expression<Func<object>> target, BindingMode bindMode) {
            Target = target;
            BindMode = bindMode;
        }

        public BindingTarget() { }

        /// <summary>
        /// Retrieves the value of the bound property
        /// </summary>
        /// <returns></returns>
        public object RetrieveValue() => CompiledTarget.GetValue();

        /// <summary>
        /// Retrieve the owning object (binding site) of the property
        /// </summary>
        /// <returns></returns>
        public object RetrieveBindingSite() => _rootObject ?? (_rootObject = Target.GetRootObject());

        public INotifyPropertyChanged RetrieveBinding() => Target.GetRootObject() as INotifyPropertyChanged;

        public string RetrieveBindingName() => Target.GetProperty().Name;
    }
}