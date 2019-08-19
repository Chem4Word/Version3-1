// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

/*General Comment:
 All selection adorners are 'immutable':  they are drawn once and subsequently destroyed
 rather than being visually modified.  This makes coding much easier.
When changing the visual appearance of a selection, create a new instance of the adorner
supplying the element to the constructor along with the editor canvas

 */
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.ACME.Controls;
using Chem4Word.Model2;
using Chem4Word.Model2.Annotations;


namespace Chem4Word.ACME.Adorners.Selectors
{
    public abstract class SelectionAdorner : Adorner
    {
        protected double AdornerOpacity;

        #region Shared Properties

        //the editor that the Adorner attaches to
        public EditorCanvas CurrentEditor => (EditorCanvas)AdornedElement;
        public EditViewModel EditViewModel => (CurrentEditor.Chemistry as EditViewModel);
        public SolidColorBrush RenderBrush { get; protected set; }
        [NotNull] public AdornerLayer AdornerLayer { get; private set; }

        public VisualCollection VisualChildren { get; set; }

        protected override int VisualChildrenCount => VisualChildren.Count;

        #endregion

        #region Constructors

        protected SelectionAdorner(EditorCanvas currentEditor) : base(currentEditor)
        {
            VisualChildren = new VisualCollection(this);
            DefaultSettings();
            AttachHandlers();
            BondToLayer();
            IsHitTestVisible = true;
            AdornerOpacity = 0.25;
        }


        protected override Visual GetVisualChild(int index)
        {
            return VisualChildren[index];
        }

        /// <summary>
        /// Bonds the adorner to the current editor
        /// </summary>
        private void BondToLayer()
        {
            AdornerLayer = AdornerLayer.GetAdornerLayer(CurrentEditor);
            AdornerLayer.Add(this);
        }

        ~SelectionAdorner()
        {
            DetachHandlers();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Attaches the default handlers to the adorner.
        /// These simply relay events to the CurrentEditor
        /// The CurrentEditor then forwards them to the attached Behavior
        /// </summary>
        protected void AttachHandlers()
        {
            MouseLeftButtonDown += SelectionAdorner_MouseLeftButtonDown;
            MouseLeftButtonDown += SelectionAdorner_MouseLeftButtonDown;
            MouseMove += SelectionAdorner_MouseMove;
            PreviewMouseMove += SelectionAdorner_PreviewMouseMove;
            PreviewMouseDown += SelectionAdorner_PreviewMouseDown;
            PreviewMouseLeftButtonUp += SelectionAdorner_PreviewMouseLeftButtonUp;
            MouseLeftButtonUp += SelectionAdorner_MouseLeftButtonUp;
            PreviewKeyDown += SelectionAdorner_PreviewKeyDown;
            KeyDown += SelectionAdorner_KeyDown;
        }

        private void SelectionAdorner_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
           CurrentEditor.RaiseEvent(e);
        }

        private void SelectionAdorner_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected virtual void SelectionAdorner_KeyDown(object sender, KeyEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected void DetachHandlers()
        {
            MouseLeftButtonDown -= SelectionAdorner_MouseLeftButtonDown;
            MouseLeftButtonDown -= SelectionAdorner_MouseLeftButtonDown;
            MouseMove -= SelectionAdorner_MouseMove;
            PreviewMouseLeftButtonUp -= SelectionAdorner_PreviewMouseLeftButtonUp;
            PreviewMouseMove -= SelectionAdorner_PreviewMouseMove;
            MouseLeftButtonUp -= SelectionAdorner_MouseLeftButtonUp;
            PreviewKeyDown -= SelectionAdorner_PreviewKeyDown;
            KeyDown -= SelectionAdorner_KeyDown;
        }

     

        protected void DefaultSettings()
        {
            IsHitTestVisible = false;
            RenderBrush = new SolidColorBrush(SystemColors.HighlightColor);
            RenderBrush.Opacity = AdornerOpacity;

        }


        #endregion

        #region Event Handlers


        //override these methods in derived classes to handle specific events
        //The forwarding chain for events is adorner -> CurrentEditor -> attached behaviour
        protected virtual void SelectionAdorner_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected virtual void SelectionAdorner_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }


        protected virtual void SelectionAdorner_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected virtual void SelectionAdorner_MouseMove(object sender, MouseEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected virtual void SelectionAdorner_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        protected virtual void SelectionAdorner_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        #endregion

    }
}
