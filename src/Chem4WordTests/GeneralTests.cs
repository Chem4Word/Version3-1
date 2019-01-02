// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Diagnostics;
using System.Windows;
using Chem4Word.Model;
using Chem4Word.Model.Converters.CML;
using Chem4Word.Model.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chem4WordTests
{
    using System.Runtime.InteropServices.ComTypes;

    [TestClass]
    public class GeneralTests
    {
        [TestMethod]
        public void AddBond()
        {
            CMLConverter mc = new CMLConverter();
            Model m = mc.Import(ResourceHelper.GetStringResource("Carbon.xml"));

            int atomBondsCount1 = m.Molecules[0].Atoms[0].Bonds.Count;
            Assert.AreEqual(0, atomBondsCount1, $"Expected 0; got {atomBondsCount1}");

            Molecule mol = m.Molecules[0];
            Atom a0 = mol.Atoms[0];

            var aa = new Atom
            {
                Element = Globals.PeriodicTable.H,
                Position = new Point(100,100)
            };

            var bb = new Bond
            {
                StartAtom = a0,
                EndAtom = aa,
                Stereo = BondStereo.None,
                Order = "S"
            };

            mol.Atoms.Add(aa);
            mol.Bonds.Add(bb);

            int atomBondsCount2 = m.Molecules[0].Atoms[0].Bonds.Count;
            Assert.AreEqual(1, atomBondsCount2, $"Expected 1; got {atomBondsCount2}");

            mol.Bonds[0].StartAtom = null;
            mol.Bonds[0].EndAtom = null;
            mol.Bonds.Remove(bb);
            mol.Atoms.Remove(aa);

            int atomBondsCount3 = m.Molecules[0].Atoms[0].Bonds.Count;
            Assert.AreEqual(0, atomBondsCount3, $"Expected 0; got {atomBondsCount3}");
        }

        [TestMethod]
        public void TestRPPath()
        {
            int ringcount, ringcount2, ringcount3;
            CMLConverter mc = new CMLConverter();
            Model m = mc.Import(ResourceHelper.GetStringResource("tworings.xml"));

            Debug.WriteLine("tworings");
            Debug.WriteLine("-------------------------");
            m.Molecules[0].RebuildRingsRPPath();
            ringcount = m.Molecules[0].Rings.Count;

            Debug.WriteLine($"Molecule tworings has {m.Molecules[0].Rings.Count} rings.");

            Debug.WriteLine("++++++++++++++++++++++++++");


            m = mc.Import(ResourceHelper.GetStringResource("C60.xml"));

            Debug.WriteLine("C60");
            Debug.WriteLine("-------------------------");
            m.Molecules[0].RebuildRingsRPPath();
            ringcount2 = m.Molecules[0].Rings.Count;

            //m = mc.Import(ResourceHelper.GetStringResource("Benzene.xml"));

            Debug.WriteLine($"Molecule c60 has {m.Molecules[0].Rings.Count} rings.");
            Debug.WriteLine("++++++++++++++++++++++++++");


            Debug.WriteLine("testosterone");
            Debug.WriteLine("-------------------------");
            m = mc.Import(ResourceHelper.GetStringResource("Testosterone.xml"));
            m.Molecules[0].ChemicalNames.Add(new ChemicalName { Name = "testosterone" });
            m.Molecules[0].RebuildRingsRPPath();
            ringcount3 = m.Molecules[0].Rings.Count;

            Debug.WriteLine($"Molecule testosterone has {m.Molecules[0].Rings.Count} rings.");
            Debug.WriteLine("++++++++++++++++++++++++++");


            //m = mc.Import(ResourceHelper.GetStringResource("ParafuchsinCarbol.xml"));
            //m.Molecules[0].ChemicalNames.Add(new ChemicalName { Name = "ParafuchsinCarbol" });
            //int  ringcount4 = m.Molecules[0].Rings.Count;
            //m.Molecules[0].RebuildRings();
            //Debug.WriteLine($"Molecule {m.Molecules[0].ChemicalNames[0].Name} has {m.Molecules[0].Rings.Count} rings.");

            Debug.WriteLine("Insulin");
            Debug.WriteLine("-------------------------");
            m = mc.Import(ResourceHelper.GetStringResource("Insulin.xml"));
            m.Molecules[0].ChemicalNames.Add(new ChemicalName { Name = "Insulin" });
            m.Molecules[0].RebuildRingsRPPath();
            int ringcount5 = m.Molecules[0].Rings.Count;
            Debug.WriteLine($"Molecule insulin  has {m.Molecules[0].Rings.Count} rings.");
            Debug.WriteLine("+++++++++++++++++++++++++");
        }
    }
}