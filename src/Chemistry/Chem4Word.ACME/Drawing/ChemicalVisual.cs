using System.Collections.Generic;
using System.Windows.Media;

namespace Chem4Word.ACME.Drawing
{
    public abstract class ChemicalVisual: DrawingVisual
    {
        public int RefCount { get; set; } //how many separate references are there to this visual within the model
        public Dictionary<object, DrawingVisual> ChemicalVisuals { get; set; }

        public ChemicalVisual()
        {

        }

        public abstract void Render();

    }
}