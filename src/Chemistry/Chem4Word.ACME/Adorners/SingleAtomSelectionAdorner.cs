// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.ACME.Controls;
using Chem4Word.Model2;


namespace Chem4Word.ACME.Adorners
{
    public class SingleAtomSelectionAdorner : Adorner
    {
        protected Thumb BigThumb; //this is the main grab area for the molecule

        protected readonly VisualCollection VisualChildren;

        //tracks the last operation performed
        protected Transform LastOperation;

        //status flag
        protected bool Dragging;

        //rendering variables
        protected readonly Pen BorderPen;

        protected readonly Brush RenderBrush;

        //tracks the amount of travel during drag operations
        protected double DragXTravel;

        protected double DragYTravel;

        public readonly EditViewModel CurrentModel;

        //where the dragging starts
        protected Point StartPos;
        private EditorCanvas _editorCanvas;

        public SingleAtomSelectionAdorner(UIElement adornedElement, Molecule molecule, EditViewModel currentModel)
            : base(adornedElement)
        {
            CurrentModel = currentModel;

            VisualChildren = new VisualCollection(this);

            BuildBigDragArea();

            AttachHandler();

            AdornedMolecule = molecule;

            BorderPen = (Pen)FindResource("GrabHandlePen");
            RenderBrush = (Brush)FindResource("BigThumbFillBrush");
            Focusable = false;
            IsHitTestVisible = true;

            _editorCanvas = (EditorCanvas) adornedElement;
            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);
        }

        protected void AttachHandler()
        {
            //wire up the event handling
            MouseLeftButtonDown += BigThumb_MouseLeftButtonDown;
            KeyDown += ThisAdorner_KeyDown;
        }

        private void BigThumb_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                RaiseEvent(e);
            }
        }

        protected void ThisAdorner_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Delete))
            {
                //bubble it up
                e.Handled = false;
            }
            else if ((Keyboard.IsKeyDown(Key.Z) | (Keyboard.IsKeyDown(Key.Y)) && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control))
            {
                e.Handled = false;
            }
            else if (Keyboard.IsKeyDown(Key.Escape))
            {
                e.Handled = true;
                if (IsWorking)
                {
                    AbortDragging();
                }
            }
        }

        protected virtual void AbortDragging()
        {
            Dragging = false;

            LastOperation = null;
            InvalidateVisual();
        }

        protected virtual void DragStarted(object sender, DragStartedEventArgs e)
        {
            Dragging = true;
            Keyboard.Focus(this);
            DragXTravel = 0.0d;
            DragYTravel = 0.0d;
        }

        private void SetBoundingBox()
        {
            //and work out the aspect ratio for later resizing
            //AdornedMolecule.ResetBoundingBox();
        }

        /// <summary>
        /// Creates the big thumb that allows dragging a molecule around the canvas
        /// </summary>
        private void BuildBigDragArea()
        {
            BigThumb = new Thumb();
            VisualChildren.Add(BigThumb);
            BigThumb.IsHitTestVisible = true;

            BigThumb.Style = (Style)FindResource("BigThumbStyle");
            BigThumb.Cursor = Cursors.Hand;
            BigThumb.DragStarted += _bigThumb_DragStarted;
            BigThumb.DragCompleted += _bigThumb_DragCompleted;
            BigThumb.DragDelta += _bigThumb_DragDelta;
            BigThumb.MouseLeftButtonDown += BigThumb_MouseLeftButtonDown;
        }

        /// <summary>
        /// Override this to
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            if (IsWorking)
            {
                object elem = AdornedElement;
                //identify which Molecule the atom belongs to

                //take a snapshot of the molecule

                var ghost = _editorCanvas.GhostMolecule(AdornedMolecule);
                //Debug.WriteLine(LastOperation.ToString());
                ghost.Transform = LastOperation;
                //drawingContext.DrawRectangle(_renderBrush, _renderPen, ghostImage.Bounds);
                drawingContext.DrawGeometry(RenderBrush, BorderPen, ghost);

                base.OnRender(drawingContext);
            }
        }

        protected bool IsWorking => Dragging;

        // Override the VisualChildrenCount and GetVisualChild properties to interface with
        // the adorner's visual collection.
        protected override int VisualChildrenCount => VisualChildren.Count;

        public Molecule AdornedMolecule { get; set; }

        protected override Visual GetVisualChild(int index) => VisualChildren[index];

        // Arrange the Adorners.
        protected override Size ArrangeOverride(Size finalSize)
        {
            // desiredWidth and desiredHeight are the width and height of the element that's being adorned.
            // These will be used to place the ResizingAdorner at the corners of the adorned element.
            var bbb = (AdornedElement as EditorCanvas).GetMoleculeBoundingBox(AdornedMolecule);

            if (LastOperation != null)
            {
                bbb = LastOperation.TransformBounds(bbb);
            }

            //put a box right around the entire shebang

            BigThumb.Arrange(bbb);
            Canvas.SetLeft(BigThumb, bbb.Left);
            Canvas.SetTop(BigThumb, bbb.Top);
            BigThumb.Height = bbb.Height;
            BigThumb.Width = bbb.Width;

            //add the rotator

            // Return the final size.
            //_boundingBox = bbb;
            return finalSize;
        }

        #region Events

        public event DragCompletedEventHandler DragCompleted;

        #endregion Events

        #region Dragging

        private void _bigThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            Dragging = true;

            DragXTravel = 0.0d;
            DragYTravel = 0.0d;

            StartPos = new Point(Canvas.GetLeft(BigThumb), Canvas.GetTop(BigThumb));
            LastOperation = new TranslateTransform();
        }

        private void _bigThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            DragXTravel += e.HorizontalChange;
            DragYTravel += e.VerticalChange;

            var lastTranslation = (TranslateTransform)LastOperation;

            lastTranslation.X = DragXTravel;
            lastTranslation.Y = DragYTravel;

            Canvas.SetLeft(BigThumb, StartPos.X + DragXTravel);
            Canvas.SetTop(BigThumb, StartPos.Y + DragYTravel);

            InvalidateVisual();
        }

        /// <summary>
        /// Handles all drag events from all thumbs.
        /// The actual transformation is set duing other code
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _bigThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            //wire up the event handling
            var lastTranslation = (TranslateTransform)LastOperation;
            lastTranslation.X = DragXTravel;
            lastTranslation.Y = DragYTravel;

            SetBoundingBox();
            InvalidateVisual();

            //move the molecule
            CurrentModel.DoOperation(LastOperation, AdornedMolecule.Atoms.Values.ToList());
            RaiseDRCompleted(sender, e);
            Dragging = false;
        }

        #endregion Dragging

        protected void RaiseDRCompleted(object sender, DragCompletedEventArgs dragCompletedEventArgs)
        {
            DragCompleted?.Invoke(this, dragCompletedEventArgs);
        }
    }
}