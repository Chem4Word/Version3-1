using System.Windows;
using System.Windows.Media;
using Chem4Word.Model;

namespace Chem4Word.ACME.Drawing
{
    public class BondVisual: ChemicalVisual
    {
        public Bond ParentBond { get; }
        public BondVisual(Bond bond)
        {
            ParentBond = bond;
        }


        public override void Render()
        {
            Point startPoint, endPoint;

            startPoint = ParentBond.StartAtom.Position;
            endPoint = ParentBond.EndAtom.Position;

            using (DrawingContext dc = RenderOpen())
            {
                dc.DrawLine(new Pen(Brushes.Black, 1.0), startPoint,endPoint );
                dc.Close();
            }
        }
    }
}
