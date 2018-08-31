using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chem4Word.Model
{
    internal class EdgeList :HashSet<Bond>
    {
        public static EdgeList operator ^(EdgeList a, EdgeList b)
        {
            var copy = new EdgeList();
            copy.UnionWith(a);
            copy.SymmetricExceptWith(b);
            return copy;
        }

        public static EdgeList operator + (EdgeList a, EdgeList b)
        {
            var copy = new EdgeList();
            copy.UnionWith(a);
            copy.UnionWith(b);
            return copy;
        }
    }
}
