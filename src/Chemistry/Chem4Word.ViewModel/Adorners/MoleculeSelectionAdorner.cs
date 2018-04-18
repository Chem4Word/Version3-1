// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model;
using Chem4Word.Model.Annotations;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ViewModel.Adorners
{
    public class MoleculeSelectionAdorner: Adorner
    {
        private const int ThumbWidth = 10;
        private const int HalfThumbWidth = ThumbWidth / 2;

        private const int RotateThumbWidth = 20;


        private Point _canvasPos;
        private readonly Molecule _frag;
        //some things to grab hold of
        private readonly Thumb _topLeft; //these do the resizing
        private readonly Thumb _topRight; //these do the resizing
        private readonly Thumb _bottomLeft; //these do the resizing
        private readonly Thumb _bottomRight; //these do the resizing


        private Thumb _bigThumb; //this is the main grab area for the molecule

        private Thumb _rotateThumb;  //Grab hold of this to rotate the molecule
        private readonly VisualCollection _visualChildren;
        private System.Windows.Media.Geometry ghostImage = null;
        private Transform _lastOperation;
        private double _aspectRatio;
        private Rect _boundingBox;

        private bool _dragging = false;
        private bool _resizing = false;
        private bool _rotating = false;

        private double _rotateAngle = 0.0;
        private Point _centroid;
        //private SnapGeometry _rotateSnapper;
        private Brush _renderBrush;
        private Pen _renderPen;
        private double _dragXTravel;
        private double _dragYTravel;


        public MoleculeSelectionAdorner(UIElement adornedElement, Molecule molecule)
            : base(adornedElement)
        {
            _visualChildren = new VisualCollection(this);



            BuildBigDragArea();

            BuildAdornerCorner(ref _topLeft, Cursors.SizeNWSE);
            BuildAdornerCorner(ref _topRight, Cursors.SizeNESW);
            BuildAdornerCorner(ref _bottomLeft, Cursors.SizeNESW);
            BuildAdornerCorner(ref _bottomRight, Cursors.SizeNWSE);

            BuildRotateThumb(ref _rotateThumb, Cursors.Hand);

            _topLeft.DragStarted += DragStarted;
            _topRight.DragStarted += DragStarted;
            _bottomLeft.DragStarted += DragStarted;
            _bottomRight.DragStarted += DragStarted;

            _topLeft.DragDelta += _topLeft_DragDelta;
            _topRight.DragDelta += _topRight_DragDelta;
            _bottomLeft.DragDelta += _bottomLeft_DragDelta;
            _bottomRight.DragDelta += _bottomRight_DragDelta;

            _bottomRight.DragCompleted += _bigThumb_DragCompleted;
            _topRight.DragCompleted += _bigThumb_DragCompleted;
            _topLeft.DragCompleted += _bigThumb_DragCompleted;
            _bottomLeft.DragCompleted += _bigThumb_DragCompleted;
            //wire up the event handling
            this.MouseLeftButtonDown += MoleculeSelectionAdorner_MouseLeftButtonDown;
            this.KeyDown += MoleculeAdorner_KeyDown;
            
            _frag = molecule;

            _renderBrush = (Brush)FindResource("GrabHandleFillBrush");
            _renderPen = (Pen)FindResource("GrabHandlePen");


            Focusable = true;
            IsHitTestVisible = true;
            SetBoundingBox();

            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);
        }

        private void MoleculeSelectionAdorner_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                RaiseEvent(e);
            }
        }

        private void MoleculeAdorner_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Delete))
            {
                //_frag.Delete();
            }

            if (Keyboard.IsKeyDown(Key.Escape))
            {
                if (IsWorking)
                {
                    AbortDragging();
                }

                else
                {
                    
                }
            }
        }

        private void AbortDragging()
        {
            _dragging = false;
            _resizing = false;
            _rotating = false;
            _lastOperation = null;
            InvalidateVisual();
        }

        private void BuildRotateThumb(ref Thumb rotateThumb, Cursor hand)
        {


            rotateThumb = new Thumb();
            
            _rotateThumb.Width = RotateThumbWidth;
            _rotateThumb.Height = RotateThumbWidth;
            rotateThumb.Style = (Style)FindResource("RotateThumb");
            rotateThumb.DragStarted += RotateThumb_DragStarted;
            rotateThumb.DragCompleted += RotateThumb_DragCompleted;
            //rotateThumb.DragDelta += RotateThumb_DragDelta;
            rotateThumb.KeyDown += MoleculeAdorner_KeyDown;

            _visualChildren.Add(rotateThumb);
        }

        //private void RotateThumb_DragDelta(object sender, DragDeltaEventArgs e)
        //{
        //    double xOffset = Canvas.GetLeft(_bigThumb) + e.HorizontalChange - _centroid.X;
        //    double yOffset = Canvas.GetTop(_bigThumb) + e.VerticalChange - _centroid.Y;
        //    Vector orientation = new Vector(xOffset, yOffset);
        //    double tempAngle;
        //    tempAngle = _rotateSnapper.SnapAngle((int)Math.Floor(_rotateAngle), orientation, KeyboardUtils.HoldingDownControl());
        //    tempAngle = tempAngle / (2 * Math.PI) * 360;
        //    _lastOperation = new RotateTransform(tempAngle, _centroid.X, _centroid.Y);
        //    InvalidateVisual();
        //}

        private void RotateThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            _rotating = true;
            if (_rotateAngle == 0.0d)
            {
                //we have not yet rotated anything
                //so take a snapshot of the centroid of the molecule
                SetCentroid();
            }
        }

        private void SetCentroid()
        {
            _centroid = _frag.Centroid;
            //create a snapper
            //_rotateSnapper = new SnapGeometry(_centroid, 15);
        }

        private void RotateThumb_DragCompleted(object sender, DragCompletedEventArgs dragCompletedEventArgs)
        {
            _rotating = false;


            if (_lastOperation != null && _lastOperation is RotateTransform)
            {
                _rotateAngle = ((RotateTransform)_lastOperation).Angle;

                _frag.Move(_lastOperation);
                SetBoundingBox();
                InvalidateVisual();
                if (DragResizeCompleted != null)
                {
                    DragResizeCompleted(this, dragCompletedEventArgs);
                }

                SetCentroid();
            }


        }

        private void DragStarted(object sender, DragStartedEventArgs e)
        {
            _resizing = true;
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
            _boundingBox = _frag.BoundingBox;
            _aspectRatio = _boundingBox.Width / _boundingBox.Height;
        }

        /// <summary>
        /// Creates the big thumb that allows dragging a molecule around the canvas
        /// </summary>
        private void BuildBigDragArea()
        {

            _bigThumb = new Thumb();
            _visualChildren.Add(_bigThumb);
            _bigThumb.IsHitTestVisible = true;

            _bigThumb.Style = (Style)FindResource("GrabHandleStyle");
            _bigThumb.Cursor = Cursors.Hand;
            _bigThumb.DragStarted += _bigThumb_DragStarted;
            _bigThumb.DragCompleted += _bigThumb_DragCompleted;
            _bigThumb.DragDelta += _bigThumb_DragDelta;
            _bigThumb.MouseLeftButtonDown += MoleculeSelectionAdorner_MouseLeftButtonDown;
           
        }

        private void _bigThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            IncrementDragging(e);

            Point currentPos = new Point(_dragXTravel, _dragYTravel);

            Canvas.SetLeft(_bigThumb, Canvas.GetLeft(_bigThumb) + _dragXTravel);
            Canvas.SetTop(_bigThumb, Canvas.GetTop(_bigThumb) + _dragYTravel);

            _canvasPos = currentPos;
            _lastOperation = new TranslateTransform(_canvasPos.X, _canvasPos.Y);

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
            if (_lastOperation != null)
            {

                _frag.Move(_lastOperation);
                SetBoundingBox();
                InvalidateVisual();
                DragResizeCompleted?.Invoke(this, e);
                SetCentroid();
            }
            _dragging = false;
            _resizing = false;
        }

        private void _bigThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            InitializeDragging();
            _dragging = true;

        }


        

        private void MoleculeAdorner_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

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

                ghostImage = _frag?.Ghost();
                Debug.WriteLine(_lastOperation.ToString());
                ghostImage.Transform = _lastOperation;
                //drawingContext.DrawRectangle(_renderBrush, _renderPen, ghostImage.Bounds);
                drawingContext.DrawGeometry(_renderBrush, _renderPen, ghostImage);

                base.OnRender(drawingContext);
            }

        }

        private bool IsWorking
        {
            get { return _dragging | _resizing | _rotating; }
            
        }

        private void BuildAdornerCorner(ref Thumb cornerThumb, Cursor customizedCursor)
        {

            if (cornerThumb != null)
            {
                return;
            }

            cornerThumb = new Thumb();

            // Set some arbitrary visual characteristics.
            cornerThumb.Cursor = customizedCursor;
            cornerThumb.Style = (Style)FindResource("GrabHandleStyle");
            cornerThumb.KeyDown += MoleculeAdorner_KeyDown;
            _visualChildren.Add(cornerThumb);
        }

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

            if (_lastOperation != null)
            {
                bbb = _lastOperation.TransformBounds(bbb);
            }

            _topLeft.Arrange(new Rect(bbb.Left - HalfThumbWidth, bbb.Top - HalfThumbWidth, ThumbWidth, ThumbWidth));
            _topRight.Arrange(new Rect(bbb.Left + bbb.Width - HalfThumbWidth, bbb.Top - HalfThumbWidth, ThumbWidth,
                ThumbWidth));
            _bottomLeft.Arrange(new Rect(bbb.Left - HalfThumbWidth, bbb.Top + bbb.Height - HalfThumbWidth,
                ThumbWidth, ThumbWidth));
            _bottomRight.Arrange(new Rect(bbb.Left + bbb.Width - HalfThumbWidth,
                bbb.Height + bbb.Top - HalfThumbWidth, ThumbWidth, ThumbWidth));
            //put a box right around the entire shebang

            _bigThumb.Arrange(bbb);
            Canvas.SetLeft(_bigThumb, bbb.Left);
            Canvas.SetTop(_bigThumb, bbb.Top);
            _bigThumb.Height = bbb.Height;
            _bigThumb.Width = bbb.Width;

            //add the rotator
            double xplacement, yplacement;
            xplacement = (bbb.Left + bbb.Right) / 2 - _rotateThumb.Width / 2;
            yplacement = bbb.Top  - _rotateThumb.Width;// - ThumbWidth * 3;

            _rotateThumb.Arrange(new Rect(xplacement, yplacement  , _rotateThumb.Width, _rotateThumb.Height));
            // Return the final size.
            //_boundingBox = bbb;
            return finalSize;
        }

        #region Events

        public event DragCompletedEventHandler DragResizeCompleted;

        #endregion

        #region Resizing

        // Handler for resizing from the bottom-right.
       

        private void IncrementDragging(DragDeltaEventArgs args)
        {
            _dragXTravel += args.HorizontalChange;
            _dragYTravel += args.VerticalChange;
        }

        private void ResizeFrag(Molecule frag, double left, double top, double right, double bottom)
        {
            //work out the centroid of where we want to place the fragment
            var centreX = left + right / 2;
            var centreY = top + bottom / 2;
            Debug.WriteLine("CenterX={0}, CenterY ={1}, Right={2}, Left={3}, Top={4}, Bottom={5}", centreX, centreY, right, left, top, bottom);
            var scaleFactor = GetScaleFactor(left, top, right, bottom);

            _lastOperation = new ScaleTransform(scaleFactor, scaleFactor, left, top);
        }

        private double GetScaleFactor(double left, double top, double right, double bottom)
        {
            double scaleFactor;
            var newAspectRatio = Math.Abs(right - left) / Math.Abs(bottom - top);
            if (newAspectRatio > _aspectRatio) //it's wider now than it is deep
            {
                scaleFactor = Math.Abs(right - left) / _boundingBox.Width;
            }
            else //it's deeper than it's wide
            {
                scaleFactor = Math.Abs(right - left) / _boundingBox.Width;
            }
            return scaleFactor;
        }

        // Handler for resizing from the top-right.
        private void _topRight_DragDelta(object sender, DragDeltaEventArgs args)
        {
            Thumb hitThumb = sender as Thumb;

            if (hitThumb == null)
            {
                return;
            }

            
            IncrementDragging(args);

            if (NotDraggingBackwards())
            {
                var scaleFactor = GetScaleFactor(_boundingBox.Left,
                    _boundingBox.Top + _dragYTravel,
                    _boundingBox.Right + _dragXTravel,
                    _boundingBox.Bottom);

                _lastOperation = new ScaleTransform(scaleFactor, scaleFactor, _boundingBox.Left, _boundingBox.Bottom);

                InvalidateVisual();
            }
        }

        private bool NotDraggingBackwards()
        {
            return _boundingBox.Width + _dragXTravel > 10 && _boundingBox.Height + _dragYTravel > 10;
        }

        // Handler for resizing from the top-left.
        private void _topLeft_DragDelta(object sender, DragDeltaEventArgs args)
        {
            Thumb hitThumb = sender as Thumb;

            if (hitThumb == null)
            {
                return;
            }

            IncrementDragging(args);
            if (NotDraggingBackwards())
            {
                var scaleFactor = GetScaleFactor(_boundingBox.Left + _dragXTravel,
                    _boundingBox.Top + _dragYTravel,
                    _boundingBox.Right,
                    _boundingBox.Bottom);

                _lastOperation = new ScaleTransform(scaleFactor, scaleFactor, _boundingBox.Right, _boundingBox.Bottom);

                InvalidateVisual();
            }
        }

        // Handler for resizing from the bottom-left.
        private void _bottomLeft_DragDelta(object sender, DragDeltaEventArgs args)
        {
            Thumb hitThumb = sender as Thumb;

            if (hitThumb == null)
            {
                return;
            }

            IncrementDragging(args);
            if (NotDraggingBackwards())
            {
                var scaleFactor = GetScaleFactor(_boundingBox.Left + _dragXTravel,
                    _boundingBox.Top + _dragYTravel,
                    _boundingBox.Right,
                    _boundingBox.Bottom);

                _lastOperation = new ScaleTransform(scaleFactor, scaleFactor, _boundingBox.Right, _boundingBox.Top);

                InvalidateVisual();
            }

        }

        private void _bottomRight_DragDelta(object sender, DragDeltaEventArgs args)
        {
            Thumb hitThumb = sender as Thumb;

            if (hitThumb == null)
            {
                return;
            }

            IncrementDragging(args);
            if (NotDraggingBackwards())
            {
                var scaleFactor = GetScaleFactor(_boundingBox.Left,
                    _boundingBox.Top,
                    _boundingBox.Right + _dragXTravel,
                    _boundingBox.Bottom + _dragYTravel);

                _lastOperation = new ScaleTransform(scaleFactor, scaleFactor, _boundingBox.Left, _boundingBox.Top);

                InvalidateVisual();
            }
        }

        #endregion
    }
}