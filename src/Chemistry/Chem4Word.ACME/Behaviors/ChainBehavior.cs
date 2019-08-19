// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Chem4Word.ACME.Adorners;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;
using Chem4Word.Model2.Geometry;

namespace Chem4Word.ACME.Behaviors
{
    public class ChainBehavior : BaseEditBehavior
    {
        private ChainAdorner _currentAdorner;
        private Window _parent;
        public List<Point> Placements { get; private set; }

        public ChainAdorner CurrentAdorner
        {
            get => _currentAdorner;
            set
            {
                RemoveChainAdorner();
                _currentAdorner = value;
                if (_currentAdorner != null)
                {
                    _currentAdorner.MouseLeftButtonDown += CurrentEditor_MouseLeftButtonDown;
                    _currentAdorner.MouseLeftButtonUp += CurrentEditor_MouseLeftButtonUp;
                    _currentAdorner.PreviewKeyDown += CurrentEditor_PreviewKeyDown;
                }

                //local function
                void RemoveChainAdorner()
                {
                    if (_currentAdorner != null)
                    {
                        var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);
                        layer.Remove(_currentAdorner);
                        _currentAdorner.MouseLeftButtonDown -= CurrentEditor_MouseLeftButtonDown;
                        _currentAdorner.MouseLeftButtonUp -= CurrentEditor_MouseLeftButtonUp;
                        _currentAdorner.PreviewKeyDown -= CurrentEditor_PreviewKeyDown;
                        _currentAdorner = null;
                    }
                }
            }
        }

        private void CurrentEditor_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Abort();
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            EditViewModel.SelectedItems?.Clear();

            CurrentEditor = (EditorCanvas)AssociatedObject;

            _parent = Application.Current.MainWindow;

            CurrentEditor.MouseLeftButtonDown += CurrentEditor_MouseLeftButtonDown;
            CurrentEditor.MouseMove += CurrentEditor_MouseMove;
            CurrentEditor.MouseLeftButtonUp += CurrentEditor_MouseLeftButtonUp;
            CurrentEditor.PreviewKeyDown += CurrentEditor_PreviewKeyDown;

            AssociatedObject.IsHitTestVisible = true;

            if (_parent != null)
            {
                _parent.MouseLeftButtonDown += CurrentEditor_MouseLeftButtonDown;
            }

            CurrentStatus = "Draw a ring by clicking on a bond, atom or free space.";
        }

        private void CurrentEditor_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsDrawing)
            {
                EditViewModel.DrawChain(Placements, Target);
            }

            MouseIsDown = false;
            IsDrawing = false;
            CurrentEditor.ReleaseMouseCapture();
            CurrentAdorner = null;
            Placements = null;
        }

        private void CurrentEditor_MouseMove(object sender, MouseEventArgs e)
        {
            if (MouseIsDown)
            {
                IsDrawing = true;
            }

            if (IsDrawing)
            {
                CurrentStatus = "Drag to start sizing chain: [Esc] to cancel.";
                var endPoint = e.GetPosition(EditViewModel.CurrentEditor);

                MarkOutAtoms(endPoint, e);

                CurrentAdorner =
                    new ChainAdorner(FirstPoint, CurrentEditor, EditViewModel.EditBondThickness, Placements, endPoint, Target);
            }
        }

        private void CurrentEditor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Atom hitAtom = CurrentEditor.ActiveAtomVisual?.ParentAtom;
            Placements = new List<Point>();
            //save the current targeted visual

            Target = hitAtom;
            FirstPoint = (hitAtom?.Position) ?? e.GetPosition(CurrentEditor);

            if (Target == null)
            {
                Placements.Add(FirstPoint);
            }
            else
            {
                Placements.Add(Target.Position);
            }

            Mouse.Capture(CurrentEditor);
            Keyboard.Focus(CurrentEditor);
            MouseIsDown = true;
        }

        public bool IsDrawing { get; set; }

        public bool MouseIsDown { get; set; }

        public Atom Target { get; set; }

        public Point FirstPoint { get; set; }

        public override void Abort()
        {
            MouseIsDown = false;
            IsDrawing = false;
            CurrentEditor.ReleaseMouseCapture();
            CurrentAdorner = null;
            Placements = null;
        }

        public void MarkOutAtoms(Point endPoint, MouseEventArgs e)
        {
            Vector GetNewSprout(Vector lastBondvector, Vector vector)
            {
                double angle = 0d;
                if (Vector.AngleBetween(lastBondvector, vector) > 0)
                {
                    angle = 60;
                }
                else
                {
                    angle = -60;
                }

                Matrix rotator = new Matrix();
                rotator.Rotate(angle);
                Vector newvector = lastBondvector;
                newvector = BasicGeometry.SnapVectorToClock(newvector);
                newvector.Normalize();
                newvector *= EditViewModel.Model.XamlBondLength;
                newvector *= rotator;
                return newvector;
            }

            Vector displacement = endPoint - Placements.Last();
            bool movedABond = displacement.Length > EditViewModel.Model.XamlBondLength;

            if (Target != null) //we hit an atom on mouse-down
            {
                if (movedABond)
                {
                    if (Placements.Count > 1) //we already have two atoms added
                    {
                        var lastBondvector = Placements.Last() - Placements[Placements.Count - 2];
                        var newvector = GetNewSprout(lastBondvector, displacement);
                        Placements.Add(Placements.Last() + newvector);
                    }
                    else //placements.count == 1
                    {
                        if (Target.Singleton)
                        {
                            Snapper snapper = new Snapper(Placements.Last(), EditViewModel);
                            var newBondVector = snapper.SnapVector(0, displacement);
                            Placements.Add(Placements.Last() + newBondVector);
                        }
                        else if (Target.Degree == 1) //it has one bond going into the atom
                        {
                            Vector balancing = Target.BalancingVector();
                            var newvector = GetNewSprout(balancing, displacement);
                            Placements.Add(Placements.Last() + newvector);
                        }
                        else
                        {
                            //Just sprout a balancing vector
                            Vector balancing = Target.BalancingVector();
                            balancing *= EditViewModel.Model.XamlBondLength;
                            Placements.Add(Placements.Last() + balancing);
                        }
                    }
                }
            }
            else //we've just drawn a free chain
            {
                if (Placements.Count == 1)
                {
                    //just got one entry in the list
                    Snapper snapper = new Snapper(FirstPoint, EditViewModel);
                    Point newEnd = snapper.SnapBond(endPoint, e);
                    Placements.Add(newEnd);
                }
                else if (movedABond) //placements must have more than one entry
                {
                    var lastBondvector = Placements.Last() - Placements[Placements.Count - 2];
                    var newvector = GetNewSprout(lastBondvector, displacement);
                    Placements.Add(Placements.Last() + newvector);
                }
            }
        }

        public List<Point> MarkOutAtoms(Atom startAtom, Point endPoint)
        {
            var bondSize = EditViewModel.Model.XamlBondLength;
            var placements = new List<Point>();
            var startAtomPos = startAtom.Position;

            Snapper snapper = new Snapper(startAtomPos, EditViewModel);
            var dragVector = endPoint - startAtomPos;

            if (startAtom.Singleton)
            {
                dragVector.Normalize();
                dragVector = dragVector * bondSize;
                dragVector = snapper.SnapVector(0, dragVector);

                Point firstPos = startAtomPos + dragVector;
                placements.Add(startAtomPos);
                placements.Add(firstPos);
                placements.AddRange(MarkOutAtoms(startAtomPos, endPoint, dragVector));
            }
            else
            {
                Vector initialDisplacement;
                initialDisplacement = startAtom.BalancingVector() * bondSize;
                Point firstPos;

                if (startAtom.Degree == 1) //it's a terminal atom of a chain
                {
                    Matrix rotator = new Matrix();
                    //so guess where we're going to draw it
                    int altSign = Math.Sign(Vector.CrossProduct(startAtom.BalancingVector(), dragVector));
                    rotator.Rotate(altSign * 60);
                    initialDisplacement = initialDisplacement * rotator;
                }

                firstPos = startAtomPos + initialDisplacement;
                placements.Add(firstPos);
                placements.AddRange(MarkOutAtoms(firstPos, endPoint, initialDisplacement));
            }

            return placements;
        }

        public List<Point> MarkOutAtoms(Point start, Point endPoint, Vector initialDisplacement)
        {
            var bondSize = EditViewModel.Model.XamlBondLength;
            var placements = new List<Point>();
            Point branchPoint = start;
            placements.Add(branchPoint);

            var displacement = endPoint - start;
            var alternationSign = Math.Sign(Vector.CrossProduct(initialDisplacement, displacement));

            Matrix rotator = new Matrix();

            double angle = 60;
            while ((branchPoint - start).LengthSquared < (endPoint - start).LengthSquared)
            {
                rotator.Rotate(angle * alternationSign);
                branchPoint = branchPoint + initialDisplacement * rotator;
                placements.Add(branchPoint);
                alternationSign = -alternationSign;
            }

            return placements;
        }

        protected override void OnDetaching()
        {
            CurrentAdorner = null;
            CurrentEditor.MouseLeftButtonDown -= CurrentEditor_MouseLeftButtonDown;
            CurrentEditor.MouseMove -= CurrentEditor_MouseMove;
            CurrentEditor.MouseLeftButtonUp -= CurrentEditor_MouseLeftButtonUp;
            CurrentEditor.PreviewKeyDown -= CurrentEditor_PreviewKeyDown;
            //CurrentEditor = null;
            if (_parent != null)
            {
                _parent.MouseLeftButtonDown -= CurrentEditor_MouseLeftButtonDown;
            }

            _parent = null;
        }
    }
}