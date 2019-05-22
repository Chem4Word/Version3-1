using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Drawing
{
    /// <summary>
    /// use for simple rendering in the UI as
    /// part of other components such as combo box
    /// items, lists etc
    /// </summary>
    public class FunctionalGroupShape : Shape
    {
        public FunctionalGroup ParentGroup { get; }

        public FunctionalGroupVisual ActiveVisual { get; set; }

        protected override Geometry DefiningGeometry
        {
            get { return null; }
        }


        public FunctionalGroupShape(FunctionalGroup fg)
        {
            ParentGroup = fg;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            FunctionalGroupVisual visual = new FunctionalGroupVisual(ParentGroup);
            visual.Render(new Point(Globals.DefaultFontSize / 3, Globals.DefaultFontSize/2), drawingContext);
            ActiveVisual = visual;
            InvalidateMeasure();

        }


        protected override Size MeasureOverride(Size constraint)
        {
            Size desiredSize = Size.Empty;

            if (ActiveVisual?.HullGeometry != null)
            {
               desiredSize= ActiveVisual.HullGeometry.Bounds.Size;
            }
            return desiredSize;
        }
    }
}