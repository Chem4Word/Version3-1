// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using System.Collections.Generic;

namespace Chem4Word.ACME.Drawing
{
    public class FunctionalGroupVisual : ChemicalVisual
    {
        private List<CustomTextSourceRun> ComponentRuns { get; }
        public FunctionalGroup ParentGroup { get; }

        public FunctionalGroupVisual(FunctionalGroup fg)
        {
            ParentGroup = fg;
            ComponentRuns = new List<CustomTextSourceRun>();
            BuildTextRuns();
        }

        private void BuildTextRuns()
        {
            //foreach (var VARIABLE in ParentGroup.)
            //{
            //    throw new NotImplementedException();
            //}
        }

        public override void Render()
        {
        }
    }

    public class CustomTextSourceRun
    {
        public string Text;
        public bool IsSuperscript;
        public bool IsEndParagraph;
        public int Length { get { return IsEndParagraph ? 1 : Text.Length; } }
    }
}