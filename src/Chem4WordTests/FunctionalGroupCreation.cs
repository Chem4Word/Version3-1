using Chem4Word.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Chem4WordTests
{
    [TestClass]
    public class FunctionalGroupTests
    {
        [TestMethod]
        public void FgAutoLoad()
        {
            string temp = JsonConvert.SerializeObject(FunctionalGroups.ShortcutList, Formatting.Indented);

            FunctionalGroup fg1 = FunctionalGroups.ShortcutList["R1"];
            Assert.IsNotNull(fg1, "FunctionalGroup 'R1' not found");
            Assert.IsTrue(fg1.AtomicWeight == 0, $"Expected AtomicWeigt of 0; got AtomicWeight of {fg1.AtomicWeight}");

            FunctionalGroup fg2 = FunctionalGroups.ShortcutList["Et"];
            Assert.IsNotNull(fg2, "FunctionalGroup 'Et' not found");
            Assert.IsTrue(fg2.AtomicWeight > 29 && fg2.AtomicWeight < 30, $"Expected AtomicWeigt of 29; got AtomicWeight of {fg2.AtomicWeight}");

            FunctionalGroup fg3 = FunctionalGroups.ShortcutList["CH2CH2OH"];
            Assert.IsNotNull(fg3, "FunctionalGroup 'CH2CH2OH' not found");
            Assert.IsTrue(fg3.AtomicWeight > 45 && fg3.AtomicWeight < 46, $"Expected AtomicWeigt of 45; got AtomicWeight of {fg3.AtomicWeight}");
        }
    }
}