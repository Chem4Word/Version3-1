using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Drawing
{
    public class CustomTextSourceRun
    {
        public string Text;
        public bool IsSuperscript;
        public bool IsEndParagraph;
        public int Length { get { return IsEndParagraph ? 1 : Text.Length; } }
    }

    public class FunctionalGroupVisual: ChemicalVisual
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
}
