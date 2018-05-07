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
using Chem4Word.Model;
using Chem4Word.View;
using Chem4Word.ViewModel.Adorners;

namespace Chem4Word.ACME.Behaviors
{
    public class DrawBehavior : BaseEditBehavior
    {
        private readonly TranslateTransform _transform = new TranslateTransform();
        private AtomShape _currentAtomShape;
        private bool _flag;
        private SnapGeometry _angleSnapper;
        private Window _parent;

        private DrawBondAdorner _dba;
        public DrawBehavior()
        {
            
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            _parent = Application.Current.MainWindow;
            Window parent = Application.Current.MainWindow;
            AssociatedObject.RenderTransform = _transform;

            AssociatedObject.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            AssociatedObject.MouseLeftButtonUp += AssociatedObject_MouseLeftButtonUp;
            AssociatedObject.MouseMove += AssociatedObject_MouseMove;

            AssociatedObject.IsHitTestVisible = true;
            if (_parent != null)
            {
                _parent.MouseLeftButtonDown += AssociatedObject_MouseLeftButtonDown;
            }
        }

        private void AssociatedObject_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Point lastPos = e.GetPosition(AssociatedObject);

            if (Dragging(e))
            {
                if (_dba == null)
                {
                    _dba=new DrawBondAdorner(AssociatedObject);

                    


                }

                var atomUnderCursor = GetAtomUnderCursor(e);
                if (atomUnderCursor != null)
                {
                    lastPos = atomUnderCursor.Position;
                }

                _dba.BondOrder = ViewModel.CurrentBondOrder;
                _dba.Stereo = ViewModel.CurrentStereo;
                _dba.StartPoint = _currentAtomShape.Position;
                _dba.EndPoint = lastPos;


            }
        }

        private void AssociatedObject_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AssociatedObject.ReleaseMouseCapture();
            

            var lastAtomShape = GetAtomUnderCursor(e);

            // ReSharper disable once PossibleUnintendedReferenceComparison
            if (lastAtomShape == null)
            {
                ViewModel.AddAtomChain(null, e.GetPosition(AssociatedObject));
            }
            else if (lastAtomShape!=null && lastAtomShape == _currentAtomShape) //both are the same atom or both are null
            {
                Point newAtomPos = GetNewChainEndPos(lastAtomShape);

                ViewModel.AddAtomChain(lastAtomShape.ParentAtom, newAtomPos);
            }
            else if (Dragging(e))
            {
            }

            _flag = false;
        }

        private Point GetNewChainEndPos(AtomShape lastAtomShape)
        {
            Atom lastAtom = lastAtomShape.ParentAtom;
            Vector newDirection = lastAtom.BalancingVector * ViewModel.Model.MeanBondLength;
            return lastAtom.Position + newDirection;

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
            return  (result?.VisualHit as AtomShape);


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
            AssociatedObject.IsHitTestVisible = false;
            if (_parent != null)
            {
                _parent.MouseLeftButtonDown -= AssociatedObject_MouseLeftButtonDown;
            }
        }
    }
}