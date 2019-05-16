// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Adorners;
using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing;
using Chem4Word.ACME.Enums;
using Chem4Word.ACME.Models;
using Chem4Word.ACME.Utils;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Chem4Word.ACME.Behaviors
{
    public class LassoBehaviour : BaseEditBehavior
    {
        //private bool _lassoVisible;
        private PointCollection _mouseTrack;

        private Point _startpoint;
        private Window _parent;
        public bool IsDragging { get; private set; }
        public bool ClickedOnAtomOrBond { get; set; }
        private TransformGroup _shift;
        private LassoAdorner _lassoAdorner;

        private PartialGhostAdorner _ghostAdorner;
        private List<Atom> _atomList;
        private object _initialTarget;

        private double _bondLength;

        //private MoleculeSelectionAdorner _molAdorner;
        private const string DefaultText = "Click to select; [Shift]-click to multselect; drag to select range; double-click to select molecule.";

        private const string ActiveSelText = "Set atoms/bonds using selectors; drag to reposition; [Delete] to remove.";

        protected override void OnAttached()
        {
            base.OnAttached();

            _parent = Application.Current.MainWindow;

            CurrentEditor = (EditorCanvas)AssociatedObject;

            CurrentEditor.PreviewMouseLeftButtonDown += CurrentEditor_PreviewMouseLeftButtonDown;
            CurrentEditor.PreviewMouseLeftButtonUp += CurrentEditor_PreviewMouseLeftButtonUp;
            CurrentEditor.PreviewMouseMove += CurrentEditor_PreviewMouseMove;
            CurrentEditor.MouseRightButtonDown += CurrentEditor_MouseRightButtonDown;
            CurrentEditor.MouseRightButtonUp += CurrentEditor_MouseRightButtonUp;
            CurrentEditor.PreviewMouseRightButtonUp += CurrentEditor_PreviewMouseRightButtonUp;
            CurrentEditor.IsHitTestVisible = true;

            _bondLength = CurrentEditor.Chemistry.Model.MeanBondLength;

            CurrentStatus = DefaultText;
        }

        private void CurrentEditor_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            DoPropertyEdit(e, CurrentEditor);
        }

        public static void DoPropertyEdit(MouseButtonEventArgs e, EditorCanvas currentEditor)
        {
            var pp = currentEditor.PointToScreen(e.GetPosition(currentEditor));

            EditViewModel evm;
            var activeVisual = currentEditor.GetTargetedVisual(e.GetPosition(currentEditor));

            if (activeVisual is AtomVisual av)
            {
                evm = (EditViewModel)((EditorCanvas)av.Parent).Chemistry;
                var mode = Application.Current.ShutdownMode;

                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                var atom = av.ParentAtom;
                var model = new AtomPropertiesModel
                            {
                                Centre = pp,
                                Path = atom.Path,
                                Element = atom.Element,
                                Charge = atom.FormalCharge??0,
                                Isotope = atom.IsotopeNumber.ToString(),
                                ShowSymbol = atom.ShowSymbol
                            };


                var pe = new AtomPropertyEditor(model, evm);
                var result = pe.ShowDialog();
                if (result ?? false)
                {
                    evm.SelectedElement = model.Element;
                }
                //var tcs = new TaskCompletionSource<bool>();

                //Application.Current.Dispatcher.Invoke(() =>
                //                                      {
                //                                          try
                //                                          {
                                                              
                //                                              var pe = new AtomPropertyEditor(model, evm);
                //                                              var result = pe.ShowDialog();
                //                                              if (result ?? false)
                //                                              {
                //                                                  evm.SelectedElement = model.Element;
                //                                              }
                //                                          }
                //                                          finally
                //                                          {
                //                                              tcs.TrySetResult(true);

                //                                          }
                //                                      });

                Application.Current.ShutdownMode = mode;

                if (model.Save)
                {
                    evm.UpdateAtom(atom, model);
                    evm.SelectedItems.Clear();
                    evm.AddToSelection(atom);
                }
            }

            if (activeVisual is BondVisual bv)
            {
                evm = (EditViewModel)((EditorCanvas)bv.Parent).Chemistry;
                var mode = Application.Current.ShutdownMode;

                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                var bond = bv.ParentBond;
                var model = new BondPropertiesModel
                            {
                                Centre = pp,
                                Path = bond.Path,
                                Angle = bond.Angle,
                                BondOrderValue = bond.OrderValue.Value,
                                IsSingle = bond.Order.Equals(Globals.OrderSingle),
                                IsDouble = bond.Order.Equals(Globals.OrderDouble)
                            };



                if (model.IsDouble)
                {
                    model.DoubleBondChoice = DoubleBondType.Auto;

                    if (bond.Stereo == Globals.BondStereo.Indeterminate)
                    {
                        model.DoubleBondChoice = DoubleBondType.Indeterminate;
                    }
                    else if (bond.ExplicitPlacement != null)
                    {
                        model.DoubleBondChoice = (DoubleBondType)bond.ExplicitPlacement.Value;
                    }
                }

                if (model.IsSingle)
                {
                    model.SingleBondChoice = SingleBondType.None;

                    switch (bond.Stereo)
                    {
                        case Globals.BondStereo.Wedge:
                            model.SingleBondChoice = SingleBondType.Wedge;
                            break;

                        case Globals.BondStereo.Hatch:
                            model.SingleBondChoice = SingleBondType.Hatch;
                            break;

                        case Globals.BondStereo.Indeterminate:
                            model.SingleBondChoice = SingleBondType.Indeterminate;
                            break;

                        default:
                            model.SingleBondChoice = SingleBondType.None;
                            break;
                    }
                }

                var tcs = new TaskCompletionSource<bool>();

                Application.Current.Dispatcher.Invoke(() =>
                                                      {
                                                          try
                                                          {
                                                              var pe = new BondPropertyEditor(model);
                                                              pe.ShowDialog();
                                                          }
                                                          finally
                                                          {
                                                              tcs.TrySetResult(true);
                                                          }
                                                      });

                Application.Current.ShutdownMode = mode;

                if (model.Save)
                {
                    evm.UpdateBond(bond, model);
                    bond.Order = Globals.OrderValueToOrder(model.BondOrderValue);
                }

                evm.SelectedItems.Clear();
                evm.AddToSelection(bond);
            }
        }

        public Point StartPoint { get; set; }

        private void DoSelectionClick(MouseButtonEventArgs e)
        {
            if (!(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
            {
                EditViewModel.SelectedItems.Clear();
            }

            _mouseTrack = new PointCollection();
            _startpoint = Mouse.GetPosition(CurrentEditor);

            Mouse.Capture(CurrentEditor);
            _mouseTrack.Add(_startpoint);

            if (e.ClickCount == 2 & EditViewModel.SelectionType == SelectionTypeCode.Molecule)
            {
                DoMolSelect(e);
                e.Handled = true;
            }

            if (e.ClickCount == 2)
            {
                DoMolSelect(e);
                e.Handled = true;
            }

            if (e.ClickCount == 1) //single click
            {
                ToggleSelect(e);
                //e.Handled = true;
            }
        }

        private void CurrentEditor_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dynamic currentObject = CurrentObject(e);

            if (IsDragging)
            {
                if (_atomList != null && _atomList.Any())
                {
                    EditViewModel.DoTransform(_shift, _atomList);
                    _atomList[0].Parent.ForceUpdates();
                }

                IsDragging = false;
                if (_ghostAdorner != null)
                {
                    RemoveAdorner(_ghostAdorner);
                    _ghostAdorner = null;
                }

                _atomList = null;
            }
            else
            {
                //did we go up on the target we went down on?
                if (currentObject != null & currentObject == _initialTarget)
                {
                    //select it
                    DoSelectionClick(e);
                }
                else if (_initialTarget != null && EditViewModel.SelectedItems.Contains(_initialTarget))
                {
                    DoSelectionClick(e);
                }

                if (EditViewModel.SelectedItems.Any())
                {
                    CurrentStatus = ActiveSelText;
                }

                if (_lassoAdorner != null)
                {
                    DisposeLasso();
                }
            }

            _initialTarget = null;
            _mouseTrack = null;

            CurrentEditor.ReleaseMouseCapture();
            CurrentStatus = DefaultText;
        }

        private object CurrentObject(MouseButtonEventArgs e)
        {
            var visual = CurrentEditor.GetTargetedVisual(e.GetPosition(CurrentEditor));
            //do a quick test to work out the
            object currentObject = null;
            if (visual is AtomVisual av)
            {
                currentObject = av.ParentAtom;
            }
            else if (visual is BondVisual bv)
            {
                currentObject = bv.ParentBond;
            }

            return currentObject;
        }

        private void CurrentEditor_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var pos = Mouse.GetPosition(CurrentEditor);

            if (MouseIsDown(e) & !IsDragging)
            {
                CurrentStatus = "Draw around atoms and bonds to select.";
                if (_initialTarget == null)
                {
                    if (_mouseTrack == null)
                    {
                        _mouseTrack = new PointCollection();
                    }
                    _mouseTrack.Add(pos);
                    var outline = GetPolyGeometry();

                    if (_lassoAdorner == null)
                    {
                        _lassoAdorner = new LassoAdorner(CurrentEditor, outline);
                    }

                    if (Mouse.Captured != (CurrentEditor))
                    {
                        Mouse.Capture(CurrentEditor);
                    }
                    _lassoAdorner.Outline = outline;

                    ModifySelection(outline);
                }
                else
                {
                    object target = CurrentObject(e);

                    if (_initialTarget != target)
                    {
                        IsDragging = true;
                    }
                }
            }
            //we're dragging an object around
            else if (MouseIsDown(e) & IsDragging)
            {
                if (_initialTarget is Bond b)
                {
                    CurrentStatus = "Drag bond to reposition";
                    _atomList = new List<Atom> { b.StartAtom, b.EndAtom };
                }
                else //we're dragging an atom
                {
                    RemoveGhost();
                    //this code is horrendous, apologies
                    //please don't modify it without good reason!
                    //if you must then READ THE COMMENTS FIRST, PLEASE!

                    Vector shift; //how much we want to shift the objects by

                    _atomList = EditViewModel.SelectedItems.OfType<Atom>().ToList();
                    List<Atom> immediateNeighbours = GetImmediateNeighbours(_atomList);
                    //we need to check to see whether we are moving an atom connected to the rest of the molecule by a single bond
                    //if we are then we can invoke the bond snapper to limit the movement
                    if (immediateNeighbours.Count == 1) //we are moving an atom attached by a single bond
                    {
                        CurrentStatus = "[Shift] = unlock length; [Ctrl] = unlock angle; [Alt]=rotate drag.";
                        //so invoke the snapper!
                        //grab the atom in the static fragment
                        Atom staticAtom = immediateNeighbours[0];

                        //now identify the connecting bond with the moving fragment
                        Bond connectingBond = null;
                        var staticAtomBonds = staticAtom.Bonds.ToArray();
                        for (int i = 0; i < staticAtomBonds.Count() & connectingBond == null; i++)
                        {
                            var bond = staticAtomBonds[i];
                            var otherAtom = bond.OtherAtom(staticAtom);
                            if (_atomList.Contains(otherAtom))
                            {
                                connectingBond = staticAtom.BondBetween(otherAtom);
                            }
                        }
                        //locate the static atom
                        Point staticPoint = staticAtom.Position;
                        //identify the moving atom
                        Atom movingAtom = connectingBond.OtherAtom(staticAtom);
                        //get the location of the neighbour of the static atom that is going to move
                        Point movingPoint = movingAtom.Position;
                        //now work out the separation between the current position and the moving atom
                        Vector fragmentSpan = StartPoint - movingPoint; //this gives us the span of the deforming fragment
                        Vector originalDistance = pos - staticPoint;
                        //now we need to work out how far away from the static atom the moving atom should be
                        Vector desiredDisplacement = originalDistance - fragmentSpan;
                        //then we snap it

                        Snapper bondSnapper = new Snapper(staticPoint, EditViewModel, bondLength: _bondLength, lockAngle: 10);

                        Vector snappedBondVector = bondSnapper.SnapVector(connectingBond.Angle, desiredDisplacement);
                        //Vector snappedBondVector = desiredDisplacement;
                        //subtract the original bond vector to get the actual desired, snapped shift
                        Vector bondVector = (movingPoint - staticPoint);
                        //now calculate the angle between the starting bond and the snapped vector
                        double rotation = Vector.AngleBetween(bondVector, snappedBondVector);

                        shift = snappedBondVector - bondVector;
                        //shift the atom and rotate the group around the new terminus
                        Point pivot = staticPoint + snappedBondVector;
                        RotateTransform rt;
                        if (KeyboardUtils.HoldingDownAlt())
                        {
                            rt = new RotateTransform(rotation, pivot.X, pivot.Y);
                        }
                        else
                        {
                            rt = new RotateTransform();
                        }

                        TransformGroup tg = new TransformGroup();
                        tg.Children.Add(new TranslateTransform(shift.X, shift.Y));
                        tg.Children.Add(rt);

                        _shift = tg;
                    }
                    else //moving an atom linked to two other neighbours
                    {
                        shift = pos - StartPoint;
                        CurrentStatus = "Drag atom to reposition";
                        TranslateTransform tt = new TranslateTransform(shift.X, shift.Y);
                        _shift = new TransformGroup();
                        _shift.Children.Add(tt);
                    }
                }

                RemoveGhost();
                _ghostAdorner = new PartialGhostAdorner(CurrentEditor, _atomList, _shift);
            }
        }

        private void RemoveGhost()
        {
            if (_ghostAdorner != null)
            {
                RemoveAdorner(_ghostAdorner);
                _ghostAdorner = null;
            }
        }

        private List<Atom> GetImmediateNeighbours(List<Atom> atomList)
        {
            var neighbours = from a in atomList
                             from n in a.Neighbours
                             where !atomList.Contains(n)
                             select n;
            return neighbours.ToList();
        }

        private object CurrentObject(MouseEventArgs e)
        {
            var visual = CurrentEditor.GetTargetedVisual(GetCurrentMouseLocation(e));
            //do a quick test to work out the
            object currentObject = null;
            if (visual is AtomVisual av)
            {
                currentObject = av.ParentAtom;
            }
            else if (visual is BondVisual bv)
            {
                currentObject = bv.ParentBond;
            }

            return currentObject;
        }

        private Point GetCurrentMouseLocation(MouseEventArgs e) => e.GetPosition(CurrentEditor);

        private void CurrentEditor_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var currentObject = CurrentObject(e);

            if (currentObject == null)
            {
                EditViewModel.SelectedItems.Clear();
            }

            //if (currentObject is Atom a)
            //{
            //    StartPoint = a.Position;
            //}
            //else
            //{
            StartPoint = e.GetPosition(CurrentEditor);
            //}

            _initialTarget = CurrentObject(e);
        }

        private void CurrentEditor_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void CurrentEditor_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("CurrentEditorOnMouseRightButtonDown");
            Mouse.Capture(CurrentEditor);
            if (e.ClickCount == 1)
            {
                var hitTestResult = GetTarget(e);
                if (hitTestResult.VisualHit is AtomVisual)
                {
                    var atom = (AtomVisual)hitTestResult.VisualHit;
                    Debug.WriteLine($"Right Click Atom {atom.ParentAtom.Id} at ({atom.Position.X},{atom.Position.Y})");
                }
                else if (hitTestResult.VisualHit is BondVisual)
                {
                    var bond = (BondVisual)hitTestResult.VisualHit;
                    var pos = e.GetPosition(CurrentEditor);
                    Debug.WriteLine($"Right Click Bond {bond.ParentBond.Id} at ({pos.X},{pos.Y})");
                }
            }
        }

        private void DisposeLasso()
        {
            RemoveAdorner(_lassoAdorner);
            _lassoAdorner = null;
        }

        private bool MouseIsDown(MouseEventArgs e)
        {
            return e.LeftButton == MouseButtonState.Pressed;
        }

        private void ModifySelection(StreamGeometry outline)
        {
            VisualTreeHelper.HitTest(CurrentEditor, null, HitTestCallback, new GeometryHitTestParameters(outline));
        }

        private void RemoveAdorner(Adorner adorner)
        {
            var layer = AdornerLayer.GetAdornerLayer(CurrentEditor);

            layer.Remove(adorner);
        }

        private StreamGeometry GetPolyGeometry()
        {
            if (_mouseTrack != null)
            {
                StreamGeometry geo = new StreamGeometry();
                using (StreamGeometryContext context = geo.Open())
                {
                    context.BeginFigure(_mouseTrack[0], true, true);

                    // Add the points after the first one.
                    context.PolyLineTo(_mouseTrack.Skip(1).ToArray(), true, false);
                }
                return geo;
            }
            else
            {
                return null;
            }
        }

        private void DoMolSelect(MouseButtonEventArgs e)
        {
            var activeVisual = CurrentEditor.ActiveVisual;

            switch (activeVisual)
            {
                case AtomVisual av:
                    {
                        var atom = av.ParentAtom;
                        //MessageBox.Show($"Hit Atom {atom.ParentAtom.Id} at ({atom.Position.X},{atom.Position.Y})");

                        EditViewModel.AddToSelection(atom);
                        CurrentStatus = ActiveSelText;
                        break;
                    }
                case BondVisual bv:
                    {
                        var bond = bv.ParentBond;
                        //MessageBox.Show($"Hit Bond {bond.ParentBond.Id} at ({e.GetPosition(CurrentEditor).X},{e.GetPosition(CurrentEditor).Y})");

                        EditViewModel.AddToSelection(bond);
                        CurrentStatus = ActiveSelText;
                        break;
                    }
                default:
                    EditViewModel.SelectedItems.Clear();
                    CurrentStatus = DefaultText;
                    break;
            }
        }

        private void ToggleSelect(MouseButtonEventArgs e)
        {
            var activeVisual = CurrentEditor.ActiveVisual;

            switch (activeVisual)
            {
                case AtomVisual av:
                    {
                        var atom = av.ParentAtom;
                        //MessageBox.Show($"Hit Atom {atom.ParentAtom.Id} at ({atom.Position.X},{atom.Position.Y})");
                        if (!EditViewModel.SelectedItems.Contains(atom))
                        {
                            EditViewModel.AddToSelection(atom);
                        }
                        else
                        {
                            EditViewModel.RemoveFromSelection(atom);
                        }
                        CurrentStatus = ActiveSelText;
                        break;
                    }
                case BondVisual bv:
                    {
                        var bond = bv.ParentBond;
                        //MessageBox.Show($"Hit Bond {bond.ParentBond.Id} at ({e.GetPosition(CurrentEditor).X},{e.GetPosition(CurrentEditor).Y})");
                        if (!EditViewModel.SelectedItems.Contains(bond))
                        {
                            EditViewModel.AddToSelection(bond);
                        }
                        else
                        {
                            EditViewModel.RemoveFromSelection(bond);
                        }
                        CurrentStatus = ActiveSelText;
                        break;
                    }
                default:
                    EditViewModel.SelectedItems.Clear();
                    CurrentStatus = DefaultText;
                    break;
            }
        }

        private HitTestResult GetTarget(MouseButtonEventArgs e)
        {
            return VisualTreeHelper.HitTest(CurrentEditor, e.GetPosition(CurrentEditor));
        }

        private HitTestResultBehavior HitTestCallback(HitTestResult result)
        {
            var id = ((GeometryHitTestResult)result).IntersectionDetail;

            var myShape = (result.VisualHit);
            if (myShape != null && (myShape is AtomVisual | myShape is BondVisual))
            {
                switch (id)
                {
                    case IntersectionDetail.FullyContains:
                    case IntersectionDetail.Intersects:
                    case IntersectionDetail.FullyInside:
                        var selAtom = ((myShape as AtomVisual)?.ParentAtom);
                        var selBond = ((myShape as BondVisual)?.ParentBond);

                        if (!(EditViewModel.SelectedItems.Contains(selAtom) || EditViewModel.SelectedItems.Contains(selBond)))
                        {
                            if (selAtom != null)
                            {
                                EditViewModel.AddToSelection(selAtom);
                            }
                            if (selBond != null)
                            {
                                EditViewModel.AddToSelection(selBond);
                            }
                        }
                        return HitTestResultBehavior.Continue;

                    case IntersectionDetail.Empty:
                        selAtom = ((myShape as AtomVisual)?.ParentAtom);
                        selBond = ((myShape as BondVisual)?.ParentBond);

                        if ((EditViewModel.SelectedItems.Contains(selAtom) || EditViewModel.SelectedItems.Contains(selBond)))
                        {
                            if (selAtom != null)
                            {
                                EditViewModel.RemoveFromSelection(selAtom);
                            }
                            if (selBond != null)
                            {
                                EditViewModel.RemoveFromSelection(selBond);
                            }
                        }
                        return HitTestResultBehavior.Continue;

                    default:
                        return HitTestResultBehavior.Stop;
                }
            }
            return HitTestResultBehavior.Continue;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            _parent = Application.Current.MainWindow;

            CurrentEditor.PreviewMouseLeftButtonDown -= CurrentEditor_PreviewMouseLeftButtonDown;
            CurrentEditor.MouseLeftButtonUp -= CurrentEditor_PreviewMouseLeftButtonUp;
            CurrentEditor.PreviewMouseMove -= CurrentEditor_PreviewMouseMove;
            CurrentEditor.MouseRightButtonDown -= CurrentEditor_MouseRightButtonDown;
            CurrentEditor.MouseRightButtonUp -= CurrentEditor_MouseRightButtonUp;
            CurrentEditor.PreviewMouseRightButtonUp -= CurrentEditor_PreviewMouseRightButtonUp;
            //CurrentEditor.IsHitTestVisible = false;

            _lassoAdorner = null;
        }
    }
}