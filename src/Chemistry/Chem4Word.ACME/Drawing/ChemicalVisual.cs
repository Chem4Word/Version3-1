using System.Windows.Media;

namespace Chem4Word.ACME.Drawing
{
    public abstract class ChemicalVisual: DrawingVisual
    {
        public int RefCount { get; set; } //how many separate references are there to this visual within the model

        public ChemicalVisual()
        {

        }

        public abstract void Render();

    }
}