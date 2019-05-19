// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.Model2.Annotations;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace Chem4Word.ACME.Behaviors
{
    public class BaseEditBehavior : Behavior<Canvas>, INotifyPropertyChanged
    {
        public EditViewModel EditViewModel
        {
            get { return (EditViewModel)GetValue(EditViewModelProperty); }
            set { SetValue(EditViewModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EditViewModel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EditViewModelProperty =
            DependencyProperty.Register("EditViewModel", typeof(EditViewModel), typeof(BaseEditBehavior), new PropertyMetadata(null));

        private string _currentStatus;

        public EditorCanvas CurrentEditor { get; set; }

        public virtual string CurrentStatus
        {
            get
            {
                return _currentStatus;
            }
            protected set
            {
                _currentStatus = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}