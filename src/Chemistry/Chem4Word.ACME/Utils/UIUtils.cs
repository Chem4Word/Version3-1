// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using Chem4Word.ACME.Drawing;
using Chem4Word.ACME.Models;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace Chem4Word.ACME.Utils
{
    public static class UIUtils
    {
        public static bool? ShowDialog(Window dialog, object parent)
        {
            HwndSource source = (HwndSource)HwndSource.FromVisual((Visual)parent);
            if (source != null)
            {
                new WindowInteropHelper(dialog).Owner = source.Handle;
            }
            return dialog.ShowDialog();
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
                };

                if (atom.Element is Element)
                {
                    model.IsFunctionalGroup = false;
                    model.IsElement = true;

                    model.Charge = atom.FormalCharge ?? 0;
                    model.Isotope = atom.IsotopeNumber.ToString();
                    model.ShowSymbol = atom.ShowSymbol;
                }

                if (atom.Element is FunctionalGroup)
                {
                    model.IsElement = false;
                    model.IsFunctionalGroup = true;
                }

                model.MicroModel = new Model();

                Molecule m = new Molecule();
                model.MicroModel.AddMolecule(m);
                m.Parent = model.MicroModel;

                Atom a = new Atom();
                a.Element = atom.Element;
                a.Position = atom.Position;
                a.FormalCharge = atom.FormalCharge;
                a.IsotopeNumber = atom.IsotopeNumber;
                m.AddAtom(a);
                a.Parent = m;

                foreach (var bond in atom.Bonds)
                {
                    Atom ac = new Atom();
                    ac.Element = Globals.PeriodicTable.C;
                    ac.ShowSymbol = false;
                    ac.Position = bond.OtherAtom(atom).Position;
                    m.AddAtom(ac);
                    ac.Parent = m;
                    Bond b = new Bond(a, ac);
                    b.Order = bond.Order;
                    b.Stereo = bond.Stereo;
                    m.AddBond(b);
                    b.Parent = m;
                }
                model.MicroModel.ScaleToAverageBondLength(20);

                var pe = new AtomPropertyEditor(model);
                UIUtils.ShowDialog(pe, currentEditor);
                Application.Current.ShutdownMode = mode;

                if (model.Save)
                {
                    evm.UpdateAtom(atom, model);
                    evm.SelectedItems.Clear();
                    evm.AddToSelection(atom);
                    if (model.AddedElement != null)
                    {
                        if (model.IsElement)
                        {
                            var newOption = new AtomOption(model.AddedElement as Element);
                            evm.AtomOptions.Add(newOption);
                        }

                        if (model.IsFunctionalGroup)
                        {
                            var newOption = new AtomOption(model.AddedElement as FunctionalGroup);
                            evm.AtomOptions.Add(newOption);
                        }
                    }
                    evm.SelectedElement = model.Element;
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
                    IsDouble = bond.Order.Equals(Globals.OrderDouble),
                    ShowPlacementSettings = bond.Order.Equals(Globals.OrderPartial12) || bond.Order.Equals(Globals.OrderDouble) || bond.Order.Equals(Globals.OrderPartial23)
                };

                model.DoubleBondChoice = DoubleBondType.Auto;

                if (model.ShowPlacementSettings)
                {
                    if (bond.ExplicitPlacement != null)
                    {
                        model.DoubleBondChoice = (DoubleBondType)bond.ExplicitPlacement.Value;

                        if (model.IsDouble)
                        {
                            if (bond.Stereo == Globals.BondStereo.Indeterminate)
                            {
                                model.DoubleBondChoice = DoubleBondType.Indeterminate;
                            }
                        }
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

                var pe = new BondPropertyEditor(model);
                UIUtils.ShowDialog(pe, currentEditor);
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
    }
}