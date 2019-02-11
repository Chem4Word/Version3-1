// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Windows;

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
            startAtom.Position = new Point(5, 5);
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

            Assert.IsTrue(model.Molecules.Count == 1, $"Expected 1 Molecule; Got {model.Molecules.Count}");

            var a1 = model.Molecules.Values.First().Atoms.Values.First();
            Assert.IsTrue(Math.Abs(a1.Position.X - 5.0) < 0.001, $"Expected a1.X = 5; Got {a1.Position.X}");
            Assert.IsTrue(Math.Abs(a1.Position.Y - 5.0) < 0.001, $"Expected a1.Y = 5; Got {a1.Position.Y}");

            Model clone = model.Copy();

            Assert.IsTrue(model.Molecules.Count == 1, $"Expected 1 Molecule; Got {model.Molecules.Count}");
            Assert.IsTrue(clone.Molecules.Count == 1, $"Expected 1 Molecule; Got {clone.Molecules.Count}");

            var a2 = clone.Molecules.Values.First().Atoms.Values.First();
            Assert.IsTrue(Math.Abs(a2.Position.X - 5.0) < 0.001, $"Expected a2.X = 5; Got {a2.Position.X}");
            Assert.IsTrue(Math.Abs(a2.Position.Y - 5.0) < 0.001, $"Expected a2.Y = 5; Got {a2.Position.Y}");

            clone.ScaleToAverageBondLength(5);

            var a3 = model.Molecules.Values.First().Atoms.Values.First();
            Assert.IsTrue(Math.Abs(a3.Position.X - 5.0) < 0.001, $"Expected a3.X = 5; Got {a3.Position.X}");
            Assert.IsTrue(Math.Abs(a3.Position.Y - 5.0) < 0.001, $"Expected a3.Y = 5; Got {a3.Position.Y}");

            var a4 = clone.Molecules.Values.First().Atoms.Values.First();
            Assert.IsTrue(Math.Abs(a4.Position.X - 3.535) < 0.001, $"Expected a4.X = 3.535; Got {a4.Position.X}");
            Assert.IsTrue(Math.Abs(a4.Position.Y - 3.535) < 0.001, $"Expected a4.Y = 3.535; Got {a4.Position.Y}");
        }
    }
}