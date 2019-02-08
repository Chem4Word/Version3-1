// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Diagnostics;
using System.Linq;
using System.Windows;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chem4WordTests.Model2
{
    [TestClass]
    public class General2
    {
        [TestMethod]
        public void TestClone()
        {
            Model model = new Model();

            Molecule molecule = new Molecule();
            molecule.Id = "m1";
            model.AddMolecule(molecule);
            molecule.Parent = model;

            Atom startAtom = new Atom();
            startAtom.Id = "a1";
            startAtom.Element = Globals.PeriodicTable.C;
            startAtom.Position = new Point(5,5);
            molecule.AddAtom(startAtom);
            startAtom.Parent = molecule;

            Atom endAtom = new Atom();
            endAtom.Id = "a2";
            endAtom.Element = Globals.PeriodicTable.C;
            endAtom.Position = new Point(10, 10);
            molecule.AddAtom(endAtom);
            endAtom.Parent = molecule;

            Bond bond = new Bond(startAtom, endAtom);
            bond.Id = "b1";
            bond.Order = Globals.OrderSingle;
            molecule.AddBond(bond);
            bond.Parent = molecule;

            Model clone = model.Clone();

            var a1 = model.Molecules.Values.First().Atoms.Values.First();
            var c1 = clone.Molecules.Values.First().Atoms.Values.First();
            Debug.WriteLine($"Atom a1 {a1} Atom {c1}");

            clone.ScaleToAverageBondLength(5);

            var aa1 = model.Molecules.Values.First().Atoms.Values.First();
            var ac1 = clone.Molecules.Values.First().Atoms.Values.First();
            Debug.WriteLine($"Atom a1 {aa1} Atom {ac1}");
        }
    }
}