using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Chem4Word.Model;

namespace Chem4Word.ACME
{
    class AtomVisual :DrawingVisual
    {
        private Atom _parentAtom;

        public Atom ParentAtom
        {
            get => _parentAtom;
            set => _parentAtom = value;
        }

    }
}
