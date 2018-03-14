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
            Debug.WriteLine($"{fg1.AtomicWeight}");
            FunctionalGroup fg2 = FunctionalGroups.ShortcutList["Et"];
            Debug.WriteLine($"{fg2.AtomicWeight}");
            FunctionalGroup fg3 = FunctionalGroups.ShortcutList["CH2CH2OH"];
            Debug.WriteLine($"{fg3.AtomicWeight}");
            Assert.IsNotNull(fg1);
        }
    }
}