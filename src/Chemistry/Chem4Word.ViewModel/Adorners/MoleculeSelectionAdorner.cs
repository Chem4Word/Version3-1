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
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ViewModel.Adorners
{
    public class MoleculeSelectionAdorner : SingleAtomSelectionAdorner
    {
        //static as they need to be set only when the adorner is first created
        private static double? _thumbWidth;

        private static double _halfThumbWidth;
        private static double _rotateThumbWidth;

        //private Point _canvasPos;

        //some things to grab hold of
        protected readonly Thumb TopLeftHandle; //these do the resizing

        protected readonly Thumb TopRightHandle; //these do the resizing
        protected readonly Thumb BottomLeftHandle; //these do the resizing
        protected readonly Thumb BottomRightHandle; //these do the resizing
        protected readonly Thumb RotateHandle;  //Grab hold of this to rotate the molecule

        //flags

        protected bool Resizing;
        protected bool Rotating;

        private double _rotateAngle;
        private Point _centroid;

        //private SnapGeometry _rotateSnapper;
        //private readonly Brush _renderBrush;

        public MoleculeSelectionAdorner(UIElement adornedElement, Molecule molecule, EditViewModel currentModel)
            : base(adornedElement, molecule, currentModel)
        {
            if (_thumbWidth == null)
            {
                _thumbWidth = (int)CurrentModel.Model.XamlBondLength / 10;
                _halfThumbWidth = _thumbWidth.Value / 2;
                _rotateThumbWidth = CurrentModel.Model.XamlBondLength / 7.5;
            }

            BuildAdornerCorner(ref TopLeftHandle, Cursors.SizeNWSE);
            BuildAdornerCorner(ref TopRightHandle, Cursors.SizeNESW);
            BuildAdornerCorner(ref BottomLeftHandle, Cursors.SizeNESW);
            BuildAdornerCorner(ref BottomRightHandle, Cursors.SizeNWSE);

            BuildRotateThumb(ref RotateHandle, Cursors.Hand);

            AttachHandlers();

            //no need to add the adroner in at this point as the base has already done it
        }

        protected void AttachHandlers()
        {
            AttachHandler();
            //wire up the event handling

            TopLeftHandle.DragStarted += ResizeStarted;
            TopRightHandle.DragStarted += ResizeStarted;
            BottomLeftHandle.DragStarted += ResizeStarted;
            BottomRightHandle.DragStarted += ResizeStarted;

            TopLeftHandle.DragDelta += TopLeftHandleDragDelta;
            TopRightHandle.DragDelta += TopRightHandleDragDelta;
            BottomLeftHandle.DragDelta += BottomLeftHandleDragDelta;
            BottomRightHandle.DragDelta += BottomRightHandleDragDelta;

            TopLeftHandle.DragCompleted += HandleResizeCompleted;
            TopRightHandle.DragCompleted += HandleResizeCompleted; ;
            BottomLeftHandle.DragCompleted += HandleResizeCompleted;
            BottomRightHandle.DragCompleted += HandleResizeCompleted; ;
        }

        private void ResizeStarted(object sender, DragStartedEventArgs e)
        {
            Resizing = true;
            Dragging = false;
            Keyboard.Focus(this);
            BoundingBox = AdornedMolecule.BoundingBox;
            DragXTravel = 0.0d;
            DragYTravel = 0.0d;
        }

        protected override void AbortDragging()
        {
            Dragging = false;
            Resizing = false;
            base.AbortDragging();
        }

        private void BuildRotateThumb(ref Thumb rotateThumb, Cursor hand)
        {
            rotateThumb = new Thumb();

            RotateHandle.Width = _rotateThumbWidth;
            RotateHandle.Height = _rotateThumbWidth;
            rotateThumb.Style = (Style)FindResource("RotateThumb");
            rotateThumb.DragStarted += RotateThumb_DragStarted;
            rotateThumb.DragCompleted += RotateThumb_DragCompleted;
            //rotateThumb.DragDelta += RotateThumb_DragDelta;

            VisualChildren.Add(rotateThumb);
        }

        private void RotateThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            Rotating = true;
            if (_rotateAngle == 0.0d)
            {
                //we have not yet rotated anything
                //so take a snapshot of the centroid of the molecule
                SetCentroid();
            }
        }

        private void SetCentroid()
        {
            _centroid = AdornedMolecule.Centroid;
            //create a snapper
            //_rotateSnapper = new SnapGeometry(_centroid, 15);
        }

        private void RotateThumb_DragCompleted(object sender, DragCompletedEventArgs dragCompletedEventArgs)
        {
            Rotating = false;

            if (LastOperation != null && LastOperation is RotateTransform)
            {
                _rotateAngle = ((RotateTransform)LastOperation).Angle;

                AdornedMolecule.Move(LastOperation);
                SetBoundingBox();
                InvalidateVisual();
                DragCompleted?.Invoke(this, dragCompletedEventArgs);

                SetCentroid();
            }
        }

        public event DragCompletedEventHandler ResizeCompleted;

        private void HandleResizeCompleted(object sender, DragCompletedEventArgs dragCompletedEventArgs)
        {
            Resizing = false;

            if (LastOperation != null && LastOperation is ScaleTransform)
            {
                var atomList = AdornedMolecule.Atoms.ToList();
                CurrentModel.DoOperation(LastOperation, atomList);
                SetBoundingBox();
                ResizeCompleted?.Invoke(this, dragCompletedEventArgs);
                SetCentroid();
                InvalidateVisual();
            }
        }

        private void SetBoundingBox()
        {
            //and work out the aspect ratio for later resizing
            AdornedMolecule.ResetBoundingBox();
            BoundingBox = AdornedMolecule.BoundingBox;
            AspectRatio = BoundingBox.Width / BoundingBox.Height;
        }

        public double AspectRatio { get; set; }

        public Rect BoundingBox { get; set; }

        /// <summary>

        /// <summary>
        /// Override this to
        /// </summary>
        /// <param name="drawingContext"></param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (IsWorking)
            {
                object elem = AdornedElement;
                //identify which Molecule the atom belongs to

                //take a snapshot of the molecule

                var AdornedMoleculeImage = AdornedMolecule.Ghost();
                Debug.WriteLine(LastOperation.ToString());
                AdornedMoleculeImage.Transform = LastOperation;
                //drawingContext.DrawRectangle(_renderBrush, _renderPen, ghostImage.Bounds);
                drawingContext.DrawGeometry(RenderBrush, BorderPen, AdornedMoleculeImage);
            }
        }

        private bool IsWorking => Dragging | Resizing | Rotating;

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
            cornerThumb.KeyDown += ThisAdorner_KeyDown;
            VisualChildren.Add(cornerThumb);
        }

        // Override the VisualChildrenCount and GetVisualChild properties to interface with
        // the adorner's visual collection.
        protected override int VisualChildrenCount => VisualChildren.Count;

        protected override Visual GetVisualChild(int index) => VisualChildren[index];

        // Arrange the Adorners.
        protected override Size ArrangeOverride(Size finalSize)
        {
            // desiredWidth and desiredHeight are the width and height of the element that's being adorned.
            // These will be used to place the ResizingAdorner at the corners of the adorned element.
            var bbb = AdornedMolecule.BoundingBox;

            if (LastOperation != null)
            {
                bbb = LastOperation.TransformBounds(bbb);
            }

            TopLeftHandle.Arrange(new Rect(bbb.Left - _halfThumbWidth, bbb.Top - _halfThumbWidth, _thumbWidth.Value, _thumbWidth.Value));
            TopRightHandle.Arrange(new Rect(bbb.Left + bbb.Width - _halfThumbWidth, bbb.Top - _halfThumbWidth, _thumbWidth.Value,
                _thumbWidth.Value));
            BottomLeftHandle.Arrange(new Rect(bbb.Left - _halfThumbWidth, bbb.Top + bbb.Height - _halfThumbWidth,
                _thumbWidth.Value, _thumbWidth.Value));
            BottomRightHandle.Arrange(new Rect(bbb.Left + bbb.Width - _halfThumbWidth,
                bbb.Height + bbb.Top - _halfThumbWidth, _thumbWidth.Value, _thumbWidth.Value));

            //add the rotator
            double xplacement, yplacement;
            xplacement = (bbb.Left + bbb.Right) / 2 - RotateHandle.Width / 2;
            yplacement = bbb.Top - RotateHandle.Width;// - ThumbWidth * 3;

            RotateHandle.Arrange(new Rect(xplacement, yplacement, RotateHandle.Width, RotateHandle.Height));
            // Return the final size.
            //BoundingBox = bbb;
            base.ArrangeOverride(finalSize);
            return finalSize;
        }

        #region Events

        public new event DragCompletedEventHandler DragCompleted;

        #endregion Events

        #region Resizing

        // Handler for resizing from the bottom-right.

        private void IncrementDragging(DragDeltaEventArgs args)
        {
            DragXTravel += args.HorizontalChange;
            DragYTravel += args.VerticalChange;
        }

        private void ResizeAdornedMolecule(Molecule AdornedMolecule, double left, double top, double right, double bottom)
        {
            //work out the centroid of where we want to place the AdornedMoleculement
            var centreX = left + right / 2;
            var centreY = top + bottom / 2;
            Debug.WriteLine("CenterX={0}, CenterY ={1}, Right={2}, Left={3}, Top={4}, Bottom={5}", centreX, centreY, right, left, top, bottom);
            var scaleFactor = GetScaleFactor(left, top, right, bottom);

            LastOperation = new ScaleTransform(scaleFactor, scaleFactor, left, top);
        }

        private double GetScaleFactor(double left, double top, double right, double bottom)
        {
            double scaleFactor;
            var newAspectRatio = Math.Abs(right - left) / Math.Abs(bottom - top);
            if (newAspectRatio > AspectRatio) //it's wider now than it is deep
            {
                scaleFactor = Math.Abs(top - bottom) / BoundingBox.Height;
            }
            else //it's deeper than it's wide
            {
                scaleFactor = Math.Abs(right - left) / BoundingBox.Width;
            }
            return scaleFactor;
        }

        // Handler for resizing from the top-right.
        private void TopRightHandleDragDelta(object sender, DragDeltaEventArgs args)
        {
            IncrementDragging(args);

            if (NotDraggingBackwards())
            {
                var scaleFactor = GetScaleFactor(BoundingBox.Left,
                    BoundingBox.Top + DragYTravel,
                    BoundingBox.Right + DragYTravel,
                    BoundingBox.Bottom);

                LastOperation = new ScaleTransform(scaleFactor, scaleFactor, BoundingBox.Left, BoundingBox.Bottom);

                InvalidateVisual();
            }
        }

        private bool NotDraggingBackwards()
        {
            return BigThumb.Height >= 10 && BigThumb.Width >= 10;
        }

        // Handler for resizing from the top-left.
        private void TopLeftHandleDragDelta(object sender, DragDeltaEventArgs args)
        {
            Thumb hitThumb = sender as Thumb;

            IncrementDragging(args);
            if (NotDraggingBackwards())
            {
                var scaleFactor = GetScaleFactor(
                    BoundingBox.Left + DragXTravel,
                    BoundingBox.Top + DragYTravel,
                    BoundingBox.Right,
                    BoundingBox.Bottom);

                LastOperation = new ScaleTransform(scaleFactor, scaleFactor, BoundingBox.Right, BoundingBox.Bottom);

                InvalidateVisual();
            }
        }

        // Handler for resizing from the bottom-left.
        private void BottomLeftHandleDragDelta(object sender, DragDeltaEventArgs args)
        {
            IncrementDragging(args);
            if (NotDraggingBackwards())
            {
                var scaleFactor = GetScaleFactor(BoundingBox.Left + DragXTravel,
                    BoundingBox.Top + DragYTravel,
                    BoundingBox.Right,
                    BoundingBox.Bottom);

                LastOperation = new ScaleTransform(scaleFactor, scaleFactor, BoundingBox.Right, BoundingBox.Top);

                InvalidateVisual();
            }
        }

        private void BottomRightHandleDragDelta(object sender, DragDeltaEventArgs args)
        {
            IncrementDragging(args);
            if (NotDraggingBackwards())
            {
                var scaleFactor = GetScaleFactor(BoundingBox.Left,
                    BoundingBox.Top,
                    BoundingBox.Right + DragXTravel,
                    BoundingBox.Bottom + DragYTravel);

                LastOperation = new ScaleTransform(scaleFactor, scaleFactor, BoundingBox.Left, BoundingBox.Top);

                InvalidateVisual();
            }
        }

        #endregion Resizing
    }
}