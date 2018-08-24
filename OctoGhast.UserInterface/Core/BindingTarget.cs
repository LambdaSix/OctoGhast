using System;
using System.ComponentModel;
using System.Linq.Expressions;
using OctoGhast.UserInterface.Controls;

namespace OctoGhast.UserInterface.Core {
    public class BindingTarget {
        public Expression<Func<object>> Target { get; }
        private readonly Func<object> compiledTarget;
        private object _rootObject;
        public BindingMode BindMode { get; }

        public BindingTarget(Expression<Func<object>> target, BindingMode bindMode) {
            Target = target;
            BindMode = bindMode;
            compiledTarget = target.Compile();
        }

        /// <summary>
        /// Retrieves the value of the bound property
        /// </summary>
        /// <returns></returns>
        public object RetrieveValue() => compiledTarget.GetValue();

        /// <summary>
        /// Retrieve the owning object (binding site) of the property
        /// </summary>
        /// <returns></returns>
        public object RetrieveBindingSite() => _rootObject ?? (_rootObject = Target.GetRootObject());

        public INotifyPropertyChanged RetrieveBinding() => Target.GetRootObject() as INotifyPropertyChanged;

        public string RetrieveBindingName() => Target.GetProperty().Name;
    }
}