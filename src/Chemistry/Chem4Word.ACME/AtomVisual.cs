using System.Windows;
using System.Windows.Media;
using Chem4Word.Model;
using Chem4Word.ViewModel;

namespace Chem4Word.ACME
{
    public class AtomVisual:  ChemicalVisual
    {
        public Atom ParentAtom { get;  }
        public AtomVisual(Atom atom)
        {
            ParentAtom = atom;
        }


        public override void Render()
        {
            Point centre = ParentAtom.Position;
            using (DrawingContext dc = RenderOpen())
            {
                dc.DrawEllipse(Brushes.Black, new Pen(Brushes.Black, 1.0), centre, 3.0, 3.0 );
                dc.Close();
            }
        }
    }
}
