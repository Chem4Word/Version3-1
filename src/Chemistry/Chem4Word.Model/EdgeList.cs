// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Chem4Word.Model
{
    public class EdgeList : HashSet<Bond>
    {
        public static EdgeList operator ^(EdgeList a, EdgeList b)
        {
            var copy = new EdgeList();
            copy.UnionWith(a);
            copy.SymmetricExceptWith(b);
            return copy;
        }

        public static EdgeList operator +(EdgeList a, EdgeList b)
        {
            var copy = new EdgeList();
            copy.UnionWith(a);
            copy.UnionWith(b);
            return copy;
        }

        public override string ToString()
        {
            var ids = this.Select(b => b.Id).ToArray();
            Array.Sort(ids);

            return "[" + string.Join(",", ids) + "]";
        }
    }

    public class EdgeListComparer : IEqualityComparer<EdgeList>
    {
        public bool Equals(EdgeList x, EdgeList y)
        {
            return x.ToString() == y.ToString();
        }

        public int GetHashCode(EdgeList obj)
        {
            return (obj?.ToString().GetHashCode()) ?? 0;
        }
    }
}