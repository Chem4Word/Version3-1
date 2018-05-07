// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.ACME.Utils;
using Chem4Word.View;

namespace Chem4Word.ACME.Behaviors
{
    public class DrawBehavior : BaseEditBehavior
    {
        private readonly TranslateTransform _transform = new TranslateTransform();
        private AtomShape _currentAtomShape;
        private bool _flag;
        private SnapGeometry _angleSnapper;

        public DrawBehavior()
        {
            
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            Window parent = Application.Current.MainWindow;
            AssociatedObject.RenderTransform = _transform;

            AssociatedObject.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            AssociatedObject.MouseLeftButtonUp += AssociatedObject_MouseLeftButtonUp;
            AssociatedObject.MouseMove += AssociatedObject_MouseMove;
        }

        private void AssociatedObject_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (Dragging(e))
            {
                
            }
        }

        private void AssociatedObject_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Dragging(e))
            {
                var lastAtom = GetAtomUnderCursor(e);

                if (lastAtom == _currentAtomShape) //both are the same atom or both are null
                {
                    ViewModel.DrawDefaultAtomChain(lastAtom.ParentAtom);
                }
            }
            _flag = false;
        }

        private void AssociatedObject_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _currentAtomShape = GetAtomUnderCursor(e);
            Mouse.Capture(AssociatedObject);
            _flag = true;

            _angleSnapper = new SnapGeometry(e.GetPosition(relativeTo:AssociatedObject));
        }
        private bool Dragging(MouseEventArgs e)
        {
            return e.LeftButton == MouseButtonState.Pressed & _flag;
        }
        private AtomShape GetAtomUnderCursor(MouseButtonEventArgs mouseButtonEventArgs)
        {

            var result = GetTarget(mouseButtonEventArgs);
            var id = ((GeometryHitTestResult)result).IntersectionDetail;

            var myShape = (result.VisualHit);

            if (myShape is AtomShape)
            {
                switch (id)
                {
                    case IntersectionDetail.FullyContains:
                    case IntersectionDetail.Intersects:
                    case IntersectionDetail.FullyInside:
                        return (AtomShape)myShape;

                    case IntersectionDetail.Empty:
                        return null;
                }
            }
            return null;

        }

        private HitTestResult GetTarget(MouseButtonEventArgs e)
        {
            return VisualTreeHelper.HitTest(AssociatedObject, e.GetPosition(AssociatedObject));
        }

       

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
            AssociatedObject.MouseLeftButtonUp -= AssociatedObject_MouseLeftButtonUp;
            AssociatedObject.MouseMove -= AssociatedObject_MouseMove;

        }
    }
}