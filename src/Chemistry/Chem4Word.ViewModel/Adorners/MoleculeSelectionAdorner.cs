// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.Model;
using Chem4Word.Model.Annotations;

namespace Chem4Word.ViewModel.Adorners
{
    public class MoleculeSelectionAdorner : Adorner
    {

        private const int ThumbWidth = 10;
        private const int HalfThumbWidth = ThumbWidth / 2;

        private const int RotateThumbWidth = 30;
        private const int HalfRotateThumbWidth = ThumbWidth / 2;

        private Point _canvasPos;
        private readonly Molecule _frag;
        //some things to grab hold of
        private readonly Thumb _topLeft; //these do the resizing
        private readonly Thumb _topRight; //these do the resizing
        private readonly Thumb _bottomLeft; //these do the resizing
        private readonly Thumb _bottomRight; //these do the resizing


        private Thumb _bigThumb; //this is the main grab area for the molecule

        private Thumb _rotateThumb;  //Grab hold of this to rotate the molecule
        private readonly VisualCollection _visualChildren ;
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
        public MoleculeSelectionAdorner([NotNull] UIElement adornedElement, Molecule parent) : base(adornedElement)
        {


            _visualChildren = new VisualCollection(this);

            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);


            BuildBigDragArea();

            BuildAdornerCorner(ref _topLeft, Cursors.SizeNWSE);
            BuildAdornerCorner(ref _topRight, Cursors.SizeNESW);
            BuildAdornerCorner(ref _bottomLeft, Cursors.SizeNESW);
            BuildAdornerCorner(ref _bottomRight, Cursors.SizeNWSE);

            _topLeft.DragStarted += _thumb_DragStarted;
            _topRight.DragStarted += _thumb_DragStarted;
            _bottomLeft.DragStarted += _thumb_DragStarted;
            _bottomRight.DragStarted += _thumb_DragStarted;

            _topLeft.DragDelta += HandleTopLeft;
            _topRight.DragDelta += HandleTopRight;
            _bottomLeft.DragDelta += HandleBottomLeft;
            _bottomRight.DragDelta += HandleBottomRight;

            _bottomRight.DragCompleted += _bigThumb_DragCompleted;
            _topRight.DragCompleted += _bigThumb_DragCompleted;
            _topLeft.DragCompleted += _bigThumb_DragCompleted;
            _bottomLeft.DragCompleted += _bigThumb_DragCompleted;
            //wire up the event handling
            //this.PreviewMouseDown += MoleculeAdorner_PreviewMouseDown;
            //this.KeyDown += MoleculeAdorner_KeyDown;

          
            _frag = parent;

            _renderBrush = (Brush)FindResource("GrabHandleFillBrush");
            _renderPen = (Pen)FindResource("GrabHandlePen");


            Focusable = true;

            SetBoundingBox();
        }

        private void _thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            _resizing = true;
        }

        private void BuildBigDragArea()
        {

            _bigThumb = new Thumb();
            _visualChildren.Add(_bigThumb);
            //AddLogicalChild(_bigThumb);

            _bigThumb.Style = (Style)FindResource("MolAdornerStyle");
            _bigThumb.Cursor = Cursors.Hand;
            _bigThumb.DragStarted += _bigThumb_DragStarted;
            _bigThumb.DragCompleted += _bigThumb_DragCompleted;
            _bigThumb.DragDelta += _bigThumb_DragDelta;
        }

        private void _bigThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {

            Point currentPos = new Point(e.HorizontalChange, e.VerticalChange);
            Canvas.SetLeft(_bigThumb, Canvas.GetLeft(_bigThumb) + e.HorizontalChange);
            Canvas.SetTop(_bigThumb, Canvas.GetTop(_bigThumb) + e.VerticalChange);
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
                if (DragResizeCompleted != null) DragResizeCompleted(this, e);
                SetCentroid();
            }
            _dragging = false;
            _resizing = false;
        }

        private void SetBoundingBox()
        {
            //and work out the aspect ratio for later resizing
            _boundingBox = _frag.BoundingBox;
            _aspectRatio = _boundingBox.Width / _boundingBox.Height;
        }
        private void _bigThumb_DragStarted(object sender, DragStartedEventArgs e)
        {

            _dragging = true;

        }

        /// <summary>
        /// Override this to 
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            if (_dragging | _resizing | _rotating)
            {
                object elem = AdornedElement;
                //identify which Molecule the atom belongs to


                //take a snapshot of the molecule

                ghostImage = _frag.Ghost();
                Debug.WriteLine(_lastOperation.ToString());
                ghostImage.Transform = _lastOperation;
                //drawingContext.DrawRectangle(_renderBrush, _renderPen, ghostImage.Bounds);
                drawingContext.DrawGeometry(_renderBrush, _renderPen, ghostImage);

                base.OnRender(drawingContext);
            }

        }

        private void BuildAdornerCorner(ref Thumb cornerThumb, Cursor customizedCursor)
        {

            if (cornerThumb != null) return;

            cornerThumb = new Thumb();

            // Set some arbitrary visual characteristics.
            cornerThumb.Cursor = customizedCursor;
            cornerThumb.Style = (Style)FindResource("GrabHandleStyle");
            //cornerThumb.KeyDown += MoleculeAdorner_KeyDown;
            _visualChildren.Add(cornerThumb);
            //AddLogicalChild(cornerThumb);
        }

        // Override the VisualChildrenCount and GetVisualChild properties to interface with 
        // the adorner's visual collection.
        protected override int VisualChildrenCount => _visualChildren.Count;
        protected override Visual GetVisualChild(int index) => _visualChildren[index];

        // Arrange the Adorners.
        protected override Size ArrangeOverride(Size finalSize)
        {
            // desiredWidth and desiredHeight are the width and height of the element that's being adorned.
            // These will be used to place the ResizingAdorner at the corners of the adorned element.
            var bbb = _frag.BoundingBox;

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

            ////add the rotator
            //double xplacement, yplacement;
            //xplacement = (bbb.Left + bbb.Right) / 2 - _rotateThumb.Width / 2;
            //yplacement = bbb.Top - ThumbWidth * 3;

            //_rotateThumb.Arrange(new Rect(xplacement, yplacement, _rotateThumb.Width, _rotateThumb.Height));
            // Return the final size.
            return finalSize;
        }

        #region Events

        public event DragCompletedEventHandler DragResizeCompleted;

        #endregion


        // Handler for resizing from the bottom-right.
        private void HandleBottomRight(object sender, DragDeltaEventArgs args)
        {
            Thumb hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            var scaleFactor = GetScaleFactor(_boundingBox.Left,
                _boundingBox.Top,
                _boundingBox.Right + args.HorizontalChange,
                _boundingBox.Bottom + args.VerticalChange);

            _lastOperation = new ScaleTransform(scaleFactor, scaleFactor, _boundingBox.Left, _boundingBox.Top);

            InvalidateVisual();
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
        private void HandleTopRight(object sender, DragDeltaEventArgs args)
        {
            Thumb hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            var scaleFactor = GetScaleFactor(_boundingBox.Left,
             _boundingBox.Top + args.VerticalChange,
             _boundingBox.Right + args.HorizontalChange,
             _boundingBox.Bottom);

            _lastOperation = new ScaleTransform(scaleFactor, scaleFactor, _boundingBox.Left, _boundingBox.Bottom);

            InvalidateVisual();
        }

        // Handler for resizing from the top-left.
        private void HandleTopLeft(object sender, DragDeltaEventArgs args)
        {
            Thumb hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            var scaleFactor = GetScaleFactor(_boundingBox.Left + args.HorizontalChange,
            _boundingBox.Top + args.VerticalChange,
            _boundingBox.Right,
            _boundingBox.Bottom);

            _lastOperation = new ScaleTransform(scaleFactor, scaleFactor, _boundingBox.Right, _boundingBox.Bottom);

            InvalidateVisual();
        }

        // Handler for resizing from the bottom-left.
        private void HandleBottomLeft(object sender, DragDeltaEventArgs args)
        {
            Thumb hitThumb = sender as Thumb;

            if (hitThumb == null) return;

            var scaleFactor = GetScaleFactor(_boundingBox.Left + args.HorizontalChange,
            _boundingBox.Top + args.VerticalChange,
            _boundingBox.Right,
            _boundingBox.Bottom);

            _lastOperation = new ScaleTransform(scaleFactor, scaleFactor, _boundingBox.Right, _boundingBox.Top);

            InvalidateVisual();

        }

        private void SetCentroid()
        {
            _centroid = _frag.CentrePoint;
            //create a snapper
            //_rotateSnapper = new SnapGeometry(_centroid, 15);
        }
    }
}