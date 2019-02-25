// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using Chem4Word.Model2;
using Xunit;

namespace Chem4WordTests
{
    public class FunctionalGroupTests
    {
        [Fact]
        public void FunctionalGroupsShortcutListIsPopulated()
        {
            var message = "Expected at least one entry in FunctionalGroups shortcut list";
            Assert.True(FunctionalGroups.ShortcutList.Any(), message);
        }

        [Fact]
        public void FunctionalGroupsShortcutListDoesNotContainNullKeys()
        {
            var message = "Did not expect any null values";
            Assert.True(FunctionalGroups.ShortcutList.All(entry => entry.Key != null), message);
        }

        [Fact]
        public void FunctionalGroupsShortcutListDoesNotContainNullValues()
        {
            var message = "Did not expect any null values";
            Assert.True(FunctionalGroups.ShortcutList.All(entry => entry.Value != null), message);
        }

        [Fact]
        public void FunctionalGroupsShortcutListKeyEqualsSymbol()
        {
            foreach (var entry in FunctionalGroups.ShortcutList)
            {
                var actual = entry.Key;
                var expected = entry.Value.Symbol;
                Assert.Equal(expected, actual);
            }
        }

        [Theory]
        [InlineData("R1", 0)]
        [InlineData("Et", 29)]
        [InlineData("CH2CH2OH", 45)]
        [InlineData("TMS", 73)]
        public void FunctionalGroupsAtomicWeightIsCalculated(string shortcut, double expectedAtomicWeight)
        {
            var functionalGroup = FunctionalGroups.ShortcutList[shortcut];

            var actualAtomicWeight = functionalGroup.AtomicWeight;

            Assert.Equal(expectedAtomicWeight, actualAtomicWeight, 0);
        }

        //[Theory]
        //[InlineData("R1", false, "[R1]")]
        //[InlineData("R9", false, "[R9]")]
        //[InlineData("Et", false, "[Et]")]
        //[InlineData("CH2CH2OH", false, "[CH2]CH2OH")]
        //[InlineData("CH2CH2OH", true, "HOCH2[CH2]")]
        //[InlineData("TMS", false, "[Si](CH3)3")]
        //[InlineData("TMS", true, "(CH3)3[Si]")]
        //[InlineData("CO2H", false, "[C]O2H")]
        //[InlineData("CO2H", true, "[C]O2H")]
        //[InlineData("CH2CH2CH2OH", false, "[(CH2)3]OH")]
        //public void FunctionalGroupsExpansion(string shortcut, bool reverse, string expected)
        //{
        //    var functionalGroup = FunctionalGroups.ShortcutList[shortcut];

        //    var item = functionalGroup.Expand(reverse);

        //    Assert.Equal(expected, item);
        //}
    }
}
