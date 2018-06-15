// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ViewModel.Adorners
{
    public class SingleAtomSelectionAdorner : Adorner
    {
        //static as they need to be set only when the adorner is first created
  
        private Point _canvasPos;
        private readonly Molecule _frag;

        //some things to grab hold of
        private readonly Thumb _topLeft; //these do the resizing

        private readonly Thumb _topRight; //these do the resizing
        private readonly Thumb _bottomLeft; //these do the resizing
        private readonly Thumb _bottomRight; //these do the resizing

        private Thumb _bigThumb; //this is the main grab area for the molecule

  
        private readonly VisualCollection _visualChildren;
        private Geometry ghostImage;
        private TranslateTransform _lastTranslation;

        private Rect _boundingBox;

        private bool _dragging;
     
        private Point _centroid;

        //private SnapGeometry _rotateSnapper;
        private readonly Brush _renderBrush;

        private readonly Pen _renderPen;
        private double _dragXTravel;
        private double _dragYTravel;

        public readonly EditViewModel CurrentModel;
        private Brush _bigBrush;
        private Point _startPos;

        public SingleAtomSelectionAdorner(UIElement adornedElement, Molecule molecule, EditViewModel currentModel)
            : base(adornedElement)
        {
            CurrentModel = currentModel;

            _visualChildren = new VisualCollection(this);
           
            BuildBigDragArea();

            AttachHandlers();

            _frag = molecule;

            _bigBrush = (Brush)FindResource("BigThumbFillBrush");
            _renderPen = (Pen)FindResource("GrabHandlePen");

            Focusable = false;
            IsHitTestVisible = true;
            SetBoundingBox();

            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);
        }

        private void AttachHandlers()
        {

            //wire up the event handling
            MouseLeftButtonDown += MoleculeSelectionAdorner_MouseLeftButtonDown;
            KeyDown += MoleculeAdorner_KeyDown;
        }

        private void MoleculeSelectionAdorner_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                RaiseEvent(e);
            }
        }

        private void MoleculeAdorner_KeyDown(object sender, KeyEventArgs e)
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

        private void AbortDragging()
        {
            _dragging = false;
      
            _lastTranslation = null;
            InvalidateVisual();
        }



        private void SetCentroid()
        {
            _centroid = _frag.Centroid;
            //create a snapper
            //_rotateSnapper = new SnapGeometry(_centroid, 15);
        }


        private void DragStarted(object sender, DragStartedEventArgs e)
        {
            _dragging = true;
            Keyboard.Focus(this);
            InitializeDragging();
        }

        private void InitializeDragging()
        {
            _dragXTravel = 0.0d;
            _dragYTravel = 0.0d;
        }

        private void SetBoundingBox()
        {
            //and work out the aspect ratio for later resizing
            _frag.ResetBoundingBox();
            _boundingBox = _frag.BoundingBox;
           
        }

        /// <summary>
        /// Creates the big thumb that allows dragging a molecule around the canvas
        /// </summary>
        private void BuildBigDragArea()
        {
            _bigThumb = new Thumb();
            _visualChildren.Add(_bigThumb);
            _bigThumb.IsHitTestVisible = true;

            _bigThumb.Style = (Style)FindResource("BigThumbStyle");
            _bigThumb.Cursor = Cursors.Hand;
            _bigThumb.DragStarted += _bigThumb_DragStarted;
            _bigThumb.DragCompleted += _bigThumb_DragCompleted;
            _bigThumb.DragDelta += _bigThumb_DragDelta;
            _bigThumb.MouseLeftButtonDown += MoleculeSelectionAdorner_MouseLeftButtonDown;
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

                var fragImage = _frag.Ghost();
                Debug.WriteLine(_lastTranslation.ToString());
                fragImage.Transform = _lastTranslation;
                //drawingContext.DrawRectangle(_renderBrush, _renderPen, ghostImage.Bounds);
                drawingContext.DrawGeometry(_renderBrush, _renderPen, fragImage);

                base.OnRender(drawingContext);
            }
        }

        private bool IsWorking => _dragging;


        // Override the VisualChildrenCount and GetVisualChild properties to interface with
        // the adorner's visual collection.
        protected override int VisualChildrenCount => _visualChildren.Count;

        public Molecule AdornedMolecule => _frag;

        protected override Visual GetVisualChild(int index) => _visualChildren[index];

        // Arrange the Adorners.
        protected override Size ArrangeOverride(Size finalSize)
        {
            // desiredWidth and desiredHeight are the width and height of the element that's being adorned.
            // These will be used to place the ResizingAdorner at the corners of the adorned element.
            var bbb = _frag.BoundingBox;

            if (_lastTranslation != null)
            {
                bbb = _lastTranslation.TransformBounds(bbb);
            }

       
            //put a box right around the entire shebang

            _bigThumb.Arrange(bbb);
            Canvas.SetLeft(_bigThumb, bbb.Left);
            Canvas.SetTop(_bigThumb, bbb.Top);
            _bigThumb.Height = bbb.Height;
            _bigThumb.Width = bbb.Width;

            //add the rotator
            
            // Return the final size.
            //_boundingBox = bbb;
            return finalSize;
        }

        #region Events

        public event DragCompletedEventHandler DragResizeCompleted;

        #endregion Events

        #region Dragging

        // Handler for resizing from the bottom-right.

        private void _bigThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            _dragging = true;
            InitializeDragging();
            _dragging = true;

            _startPos = new Point(Canvas.GetLeft(_bigThumb), Canvas.GetTop(_bigThumb));
            _lastTranslation = new TranslateTransform();
        }

        private void _bigThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            _dragXTravel += e.HorizontalChange;
            _dragYTravel += e.VerticalChange;

            _lastTranslation.X = _dragXTravel;
            _lastTranslation.Y = _dragYTravel;

            Point currentPos = new Point(_dragXTravel, _dragYTravel);

            Canvas.SetLeft(_bigThumb, _startPos.X + _dragXTravel);
            Canvas.SetTop(_bigThumb, _startPos.Y + _dragYTravel);



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

            _lastTranslation.X = e.HorizontalChange;
            _lastTranslation.Y = e.VerticalChange;


            SetBoundingBox();
            InvalidateVisual();

            //move the molecule
            CurrentModel.DoOperation(_lastTranslation, AdornedMolecule.Atoms.ToList());
            DragResizeCompleted?.Invoke(this, e);
            _dragging = false;
        }

        #endregion 
    }
}