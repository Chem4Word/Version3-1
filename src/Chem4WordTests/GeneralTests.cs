using System.Diagnostics;
using System.Windows;
using Chem4Word.Model;
using Chem4Word.Model.Converters;
using Chem4Word.Model.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Chem4WordTests
{
    [TestClass]
    public class GeneralTests
    {
        [TestMethod]
        public void AddBond()
        {
            CMLConverter mc = new CMLConverter();
            Model m = mc.Import(ResourceHelper.GetStringResource("Carbon.xml"));

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

            Debug.WriteLine(a0.Bonds.Count);

            mol.Bonds.Remove(bb);
            mol.Atoms.Remove(aa);

            Debug.WriteLine(a0.Bonds.Count);
            Debug.WriteLine(".");
        }
    }
}