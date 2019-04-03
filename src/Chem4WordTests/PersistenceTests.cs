// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Chem4Word.Model2;
using Chem4Word.Model2.Converters.CML;
using Chem4Word.Model2.Converters.MDL;

namespace Chem4WordTests
{
    [TestClass]
    public class PersistenceTests
    {
        [TestMethod]
        public void CmlImportNoAtoms()
        {
            CMLConverter mc = new CMLConverter();
            Model m = mc.Import(ResourceHelper.GetStringResource("NoAtoms.xml"));

            // Basic sanity checks
            Assert.IsTrue(m.Molecules.Count == 1, $"Expected 1 Molecule; Got {m.Molecules.Count}");
            Assert.IsTrue(m.TotalAtomsCount == 0, $"Expected 0 Atoms; Got {m.TotalAtomsCount}");
            Assert.IsTrue(m.TotalBondsCount == 0, $"Expected 0 Bonds; Got {m.TotalBondsCount}");

            // Check that names and formulae have not been trashed
            Assert.IsTrue(m.Molecules.Values.First().Names.Count == 1, $"Expected 1 Chemical Names; Got {m.Molecules.Values.First().Names.Count}");
        }

        [TestMethod]
        public void CmlImportBenzene()
        {
            CMLConverter mc = new CMLConverter();
            Model m = mc.Import(ResourceHelper.GetStringResource("Benzene.xml"));

            // Basic sanity checks
            Assert.IsTrue(m.Molecules.Count == 1, $"Expected 1 Molecule; Got {m.Molecules.Count}");
            Assert.IsTrue(m.TotalAtomsCount == 6, $"Expected 6 Atoms; Got {m.TotalAtomsCount}");
            Assert.IsTrue(m.TotalBondsCount == 6, $"Expected 6 Bonds; Got {m.TotalBondsCount}");

            // Check that names and formulae have not been trashed
            Assert.IsTrue(m.Molecules.Values.First().Names.Count == 3, $"Expected 3 Chemical Names; Got {m.Molecules.Values.First().Names.Count}");
            Assert.IsTrue(m.Molecules.Values.First().Formulas.Count == 2, $"Expected 2 Formulae; Got {m.Molecules.Values.First().Formulas.Count }");

            // Check that we have one ring
            Assert.IsTrue(m.Molecules.Values.First().Rings.Count == 1, $"Expected 1 Ring; Got {m.Molecules.Values.First().Rings.Count}");
        }

        [TestMethod]
        public void CmlImportTestosterone()
        {
            CMLConverter mc = new CMLConverter();
            Model m = mc.Import(ResourceHelper.GetStringResource("Testosterone.xml"));

            // Basic Sanity Checks
            Assert.IsTrue(m.Molecules.Count == 1, $"Expected 1 Molecule; Got {m.Molecules.Count}");
            Assert.IsTrue(m.TotalAtomsCount == 25, $"Expected 25 Atoms; Got {m.TotalAtomsCount}");
            Assert.IsTrue(m.TotalBondsCount == 28, $"Expected 28 Bonds; Got {m.TotalBondsCount}");

            // Check that names and formulae have not been trashed
            Assert.IsTrue(m.Molecules.Values.First().Names.Count == 4, $"Expected 4 Chemical Names; Got {m.Molecules.Values.First().Names.Count}");
            Assert.IsTrue(m.Molecules.Values.First().Formulas.Count == 2, $"Expected 2 Formulae; Got {m.Molecules.Values.First().Formulas.Count }");

            Assert.IsTrue(m.Molecules.Values.First().Rings.Count == 4, $"Expected 4 Rings; Got {m.Molecules.Values.First().Rings.Count}");

            var list = m.Molecules.Values.First().SortRingsForDBPlacement();
            Assert.IsTrue(list.Count == 4, $"Expected 4 Rings; Got {list.Count}");
        }

        [TestMethod]
        public void CmlImportTestosteroneThenRefresh()
        {
            CMLConverter mc = new CMLConverter();
            Model m = mc.Import(ResourceHelper.GetStringResource("Testosterone.xml"));

            // Basic Sanity Checks
            Assert.IsTrue(m.Molecules.Count == 1, $"Expected 1 Molecule; Got {m.Molecules.Count}");
            Assert.IsTrue(m.TotalAtomsCount == 25, $"Expected 25 Atoms; Got {m.TotalAtomsCount}");
            Assert.IsTrue(m.TotalBondsCount == 28, $"Expected 28 Bonds; Got {m.TotalBondsCount}");

            // Check that names and formulae have not been trashed
            Assert.IsTrue(m.Molecules.Values.First().Names.Count == 4, $"Expected 4 Chemical Names; Got {m.Molecules.Values.First().Names.Count}");
            Assert.IsTrue(m.Molecules.Values.First().Formulas.Count == 2, $"Expected 2 Formulae; Got {m.Molecules.Values.First().Formulas.Count }");

            Assert.IsTrue(m.Molecules.Values.First().Rings.Count == 4, $"Expected 4 Rings; Got {m.Molecules.Values.First().Rings.Count}");
            var list = m.Molecules.Values.First().SortRingsForDBPlacement();
            Assert.IsTrue(list.Count == 4, $"Expected 4 Rings; Got {list.Count}");
        }

        [TestMethod]
        public void CmlImportCopperPhthalocyanine()
        {
            CMLConverter mc = new CMLConverter();
            Model m = mc.Import(ResourceHelper.GetStringResource("CopperPhthalocyanine.xml"));

            // Basic Sanity Checks
            Assert.IsTrue(m.Molecules.Count == 1, $"Expected 1 Molecule; Got {m.Molecules.Count}");
            Assert.IsTrue(m.TotalAtomsCount == 57, $"Expected 57 Atoms; Got {m.TotalAtomsCount}");
            Assert.IsTrue(m.TotalBondsCount == 68, $"Expected 68 Bonds; Got {m.TotalBondsCount}");

            Assert.IsTrue(m.Molecules.Values.First().Rings.Count == 12, $"Expected 12 Rings; Got {m.Molecules.Values.First().Rings.Count}");
            var list = m.Molecules.Values.First().SortRingsForDBPlacement();
            Assert.IsTrue(list.Count == 12, $"Expected 12 Rings; Got {list.Count}");
        }

        [TestMethod]
        public void CmlImportCopperPhthalocyanineThenRefresh()
        {
            CMLConverter mc = new CMLConverter();
            Model m = mc.Import(ResourceHelper.GetStringResource("CopperPhthalocyanine.xml"));


            // Basic Sanity Checks
            Assert.IsTrue(m.Molecules.Count == 1, $"Expected 1 Molecule; Got {m.Molecules.Count}");
            Assert.IsTrue(m.TotalAtomsCount == 57, $"Expected 57 Atoms; Got {m.TotalAtomsCount}");
            Assert.IsTrue(m.TotalBondsCount == 68, $"Expected 68 Bonds; Got {m.TotalBondsCount}");

            Assert.IsTrue(m.Molecules.Values.First().Rings.Count == 12, $"Expected 12 Rings; Got {m.Molecules.Values.First().Rings.Count}");
            var list = m.Molecules.Values.First().SortRingsForDBPlacement();
            Assert.IsTrue(list.Count == 12, $"Expected 12 Rings; Got {list.Count}");
        }

        [TestMethod]
        public void CmlImportPhthalocyanine()
        {
            CMLConverter mc = new CMLConverter();
            Model m = mc.Import(ResourceHelper.GetStringResource("Phthalocyanine.xml"));

            // Basic Sanity Checks
            Assert.IsTrue(m.Molecules.Count == 1, $"Expected 1 Molecule; Got {m.Molecules.Count}");
            Assert.IsTrue(m.TotalAtomsCount == 58, $"Expected 58 Atoms; Got {m.TotalAtomsCount}");
            Assert.IsTrue(m.TotalBondsCount == 66, $"Expected 66 Bonds; Got {m.TotalBondsCount}");

            Assert.IsTrue(m.Molecules.Values.First().Rings.Count == 9, $"Expected 9 Rings; Got {m.Molecules.Values.First().Rings.Count}");
            var list = m.Molecules.Values.First().SortRingsForDBPlacement();
            Assert.IsTrue(list.Count == 8, $"Expected 8 Rings; Got {list.Count}");
        }

        [TestMethod]
        public void CmlImportPhthalocyanineThenRefresh()
        {
            CMLConverter mc = new CMLConverter();
            Model m = mc.Import(ResourceHelper.GetStringResource("Phthalocyanine.xml"));

            // Basic Sanity Checks
            Assert.IsTrue(m.Molecules.Count == 1, $"Expected 1 Molecule; Got {m.Molecules.Count}");
            Assert.IsTrue(m.TotalAtomsCount == 58, $"Expected 58 Atoms; Got {m.TotalAtomsCount}");
            Assert.IsTrue(m.TotalBondsCount == 66, $"Expected 66 Bonds; Got {m.TotalBondsCount}");

            Assert.IsTrue(m.Molecules.Values.First().Rings.Count == 9, $"Expected 9 Rings; Got {m.Molecules.Values.First().Rings.Count}");
            var list = m.Molecules.Values.First().SortRingsForDBPlacement();
            Assert.IsTrue(list.Count == 8, $"Expected 8 Rings; Got {list.Count}");
        }

        [TestMethod]
        public void CmlImportNested()
        {
            CMLConverter mc = new CMLConverter();
            Model model = mc.Import(ResourceHelper.GetStringResource("NestedMolecules.xml"));

            // Basic Sanity Checks
            Assert.IsTrue(model.Molecules.Count == 1, $"Expected 1 Molecule; Got {model.Molecules.Count}");
            // Check molecule m0 has 4 child molecules and no atoms
            Molecule molecule = model.Molecules.Values.First();
            Assert.IsTrue(molecule.Molecules.Count == 4, $"Expected 4 Molecule; Got {molecule.Molecules.Count}");
            Assert.IsTrue(molecule.Atoms.Count == 0, $"Expected 0 Atoms; Got {molecule.Atoms.Count}");
            // Check molecule m2 has no child molecules and 6 atoms
            molecule = model.Molecules.Values.First().Molecules.Values.ToList()[1];
            Assert.IsTrue(molecule.Molecules.Count == 0, $"Expected 0 Molecule; Got {molecule.Molecules.Count}");
            Assert.IsTrue(molecule.Atoms.Count == 6, $"Expected 6 Atoms; Got {molecule.Atoms.Count}");
            // Check molecule m1 has 1 child molecules and no atoms
            molecule = model.Molecules.Values.First().Molecules.Values.First();
            Assert.IsTrue(molecule.Molecules.Count == 1, $"Expected 1 Molecule; Got {molecule.Molecules.Count}");
            Assert.IsTrue(molecule.Atoms.Count == 0, $"Expected 0 Atoms; Got {molecule.Atoms.Count}");
            // Check molecule m5 has 1 child molecules and 6 atoms
            molecule = model.Molecules.Values.First().Molecules.Values.First().Molecules.Values.First();
            Assert.IsTrue(molecule.Molecules.Count == 0, $"Expected 0 Molecule; Got {molecule.Molecules.Count}");
            Assert.IsTrue(molecule.Atoms.Count == 6, $"Expected 6 Atoms; Got {molecule.Atoms.Count}");
        }

        [TestMethod]
        public void CmlImportExportNested()
        {
            CMLConverter mc = new CMLConverter();
            Model model_1 = mc.Import(ResourceHelper.GetStringResource("NestedMolecules.xml"));

            // Basic Sanity Checks
            Assert.IsTrue(model_1.Molecules.Count == 1, $"Expected 1 Molecule; Got {model_1.Molecules.Count}");
            // Check molecule m0 has 4 child molecules and no atoms
            Molecule molecule_1 = model_1.Molecules.Values.First();
            Assert.IsTrue(molecule_1.Molecules.Count == 4, $"Expected 4 Molecule; Got {molecule_1.Molecules.Count}");
            Assert.IsTrue(molecule_1.Atoms.Count == 0, $"Expected 0 Atoms; Got {molecule_1.Atoms.Count}");
            // Check molecule m2 has no child molecules and 6 atoms
            molecule_1 = model_1.Molecules.Values.First().Molecules.Values.ToList()[1];
            Assert.IsTrue(molecule_1.Molecules.Count == 0, $"Expected 0 Molecule; Got {molecule_1.Molecules.Count}");
            Assert.IsTrue(molecule_1.Atoms.Count == 6, $"Expected 6 Atoms; Got {molecule_1.Atoms.Count}");
            // Check molecule m1 has 1 child molecules and no atoms
            molecule_1 = model_1.Molecules.Values.First().Molecules.Values.First();
            Assert.IsTrue(molecule_1.Molecules.Count == 1, $"Expected 1 Molecule; Got {molecule_1.Molecules.Count}");
            Assert.IsTrue(molecule_1.Atoms.Count == 0, $"Expected 0 Atoms; Got {molecule_1.Atoms.Count}");
            // Check molecule m5 has 1 child molecules and 6 atoms
            molecule_1 = model_1.Molecules.Values.First().Molecules.Values.First().Molecules.Values.First();
            Assert.IsTrue(molecule_1.Molecules.Count == 0, $"Expected 0 Molecule; Got {molecule_1.Molecules.Count}");
            Assert.IsTrue(molecule_1.Atoms.Count == 6, $"Expected 6 Atoms; Got {molecule_1.Atoms.Count}");

            var exported = mc.Export(model_1);
            Model model_2 = mc.Import(exported);

            // Basic Sanity Checks
            Assert.IsTrue(model_2.Molecules.Count == 1, $"Expected 1 Molecule; Got {model_2.Molecules.Count}");
            // Check molecule m0 has 4 child molecules and no atoms
            Molecule molecule_2 = model_2.Molecules.Values.First();
            Assert.IsTrue(molecule_2.Molecules.Count == 4, $"Expected 4 Molecule; Got {molecule_2.Molecules.Count}");
            Assert.IsTrue(molecule_2.Atoms.Count == 0, $"Expected 0 Atoms; Got {molecule_2.Atoms.Count}");
            // Check molecule m2 has no child molecules and 6 atoms
            molecule_2 = model_2.Molecules.Values.First().Molecules.Values.ToList()[1];
            Assert.IsTrue(molecule_2.Molecules.Count == 0, $"Expected 0 Molecule; Got {molecule_2.Molecules.Count}");
            Assert.IsTrue(molecule_2.Atoms.Count == 6, $"Expected 6 Atoms; Got {molecule_2.Atoms.Count}");
            // Check molecule m1 has 1 child molecules and no atoms
            molecule_2 = model_2.Molecules.Values.First().Molecules.Values.First();
            Assert.IsTrue(molecule_2.Molecules.Count == 1, $"Expected 1 Molecule; Got {molecule_2.Molecules.Count}");
            Assert.IsTrue(molecule_2.Atoms.Count == 0, $"Expected 0 Atoms; Got {molecule_2.Atoms.Count}");
            // Check molecule m5 has 1 child molecules and 6 atoms
            molecule_2 = model_2.Molecules.Values.First().Molecules.Values.First().Molecules.Values.First();
            Assert.IsTrue(molecule_2.Molecules.Count == 0, $"Expected 0 Molecule; Got {molecule_2.Molecules.Count}");
            Assert.IsTrue(molecule_2.Atoms.Count == 6, $"Expected 6 Atoms; Got {molecule_2.Atoms.Count}");
        }

        [TestMethod]
        public void SdfImportBenzene()
        {
            SdFileConverter mc = new SdFileConverter();
            Model m = mc.Import(ResourceHelper.GetStringResource("Benzene.txt"));

            // Basic sanity checks
            Assert.IsTrue(m.Molecules.Count == 1, $"Expected 1 Molecule; Got {m.Molecules.Count}");
            Assert.IsTrue(m.TotalAtomsCount == 6, $"Expected 6 Atoms; Got {m.TotalAtomsCount}");
            Assert.IsTrue(m.TotalBondsCount == 6, $"Expected 6 Bonds; Got {m.TotalBondsCount}");

            // Check that names and formulae have not been trashed
            Assert.IsTrue(m.Molecules.Values.First().Names.Count == 2, $"Expected 2 Chemical Names; Got {m.Molecules.Values.First().Names.Count}");
            Assert.IsTrue(m.Molecules.Values.First().Formulas.Count == 2, $"Expected 2 Formulae; Got {m.Molecules.Values.First().Formulas.Count }");

            // Check that we have one ring
            Assert.IsTrue(m.Molecules.Values.First().Rings.Count == 1, $"Expected 1 Ring; Got {m.Molecules.Values.First().Rings.Count}");
        }

        [TestMethod]
        public void SdfImportBasicParafuchsin()
        {
            SdFileConverter mc = new SdFileConverter();
            Model m = mc.Import(ResourceHelper.GetStringResource("BasicParafuchsin.txt"));

            // Basic sanity checks
            Assert.IsTrue(m.Molecules.Count == 1, $"Expected 2 Molecules; Got {m.Molecules.Count}");
            Assert.IsTrue(m.TotalAtomsCount == 41, $"Expected 41 Atoms; Got {m.TotalAtomsCount}");
            Assert.IsTrue(m.TotalBondsCount == 42, $"Expected 42 Bonds; Got {m.TotalBondsCount}");

            // Check that we got three rings
            Assert.IsTrue(m.Molecules.Values.First().Rings.Count == 3, $"Expected 3 Rings; Got {m.Molecules.Values.First().Rings.Count}");

            string molstring = mc.Export(m);
            mc = new SdFileConverter();
            Model m2 = mc.Import(molstring);
        }
    }
}