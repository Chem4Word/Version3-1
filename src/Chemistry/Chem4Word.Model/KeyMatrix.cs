using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Chem4Word.Model
{
    /// <summary>
    /// This provides 2-dimensional disctionary support
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    internal class KeyMatrix<TKey, TValue>
    {
        private readonly Dictionary<(TKey, TKey), TValue> internalMatrix;

        public KeyMatrix()
        {
            internalMatrix = new Dictionary<(TKey, TKey), TValue>();


        }

        public TValue this[TKey key1, TKey key2]
        {
            get => internalMatrix[(key1, key2)];
            set => internalMatrix[(key1, key2)] = value;
        }
    }
}
