// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Linq;
using Xunit;

namespace Chem4WordTests
{
    public class FunctionalGroups
    {
        [Fact]
        public void ShortcutListIsPopulated()
        {
            var message = "Expected at least one entry in FunctionalGroups shortcut list";
            Assert.True(Chem4Word.Model2.FunctionalGroups.ShortcutList.Any(), message);
        }

        [Fact]
        public void ShortcutListDoesNotContainNullKeys()
        {
            var message = "Did not expect any null keys";
            Assert.True(Chem4Word.Model2.FunctionalGroups.ShortcutList.All(entry => entry.Key != null), message);
        }

        [Fact]
        public void ShortcutListDoesNotContainNullValues()
        {
            var message = "Did not expect any null values";
            Assert.True(Chem4Word.Model2.FunctionalGroups.ShortcutList.All(entry => entry.Value != null), message);
        }

        [Theory]
        [InlineData("R1", 0)]
        [InlineData("Et", 29)]
        [InlineData("CH2CH2OH", 45)]
        [InlineData("TMS", 73)]
        public void AtomicWeightIsCalculated(string shortcut, double expectedAtomicWeight)
        {
            var functionalGroup = Chem4Word.Model2.FunctionalGroups.ShortcutList[shortcut];

            var actualAtomicWeight = functionalGroup.AtomicWeight;

            Assert.Equal(expectedAtomicWeight, actualAtomicWeight, 0);
        }

        [Theory]
        [InlineData("R1", false, "[R1]")]
        [InlineData("R9", false, "[R9]")]
        [InlineData("Et", false, "[Et]")]
        [InlineData("CH2", false, "[C]H2")]
        [InlineData("CH2", true, "[C]H2")]
        [InlineData("CH3", false, "[C]H3")]
        [InlineData("CH3", true, "[C]H3")]
        [InlineData("CH2CH2OH", false, "[CH2]CH2OH")]
        [InlineData("CH2CH2OH", true, "HOH2C[H2C]")]
        [InlineData("TMS", false, "[Si](CH3)3")]
        [InlineData("TMS", true, "(H3C)3[Si]")]
        [InlineData("CO2H", false, "[C]O2H")]
        [InlineData("CO2H", true, "[C]O2H")]
        public void Expansion(string shortcut, bool reverse, string expected)
        {
            var functionalGroup = Chem4Word.Model2.FunctionalGroups.ShortcutList[shortcut];

            var item = functionalGroup.Expand(reverse);

            Assert.Equal(expected, item);
        }
    }
}