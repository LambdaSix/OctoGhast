﻿using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OctoGhast.Renderer.Screens {
    public abstract class ModelBase : INotifyPropertyChanged {
        public virtual event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}