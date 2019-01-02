﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace Chem4Word.Model
{
    [TypeConverter(typeof(ElementConverter))]
    [Serializable]
    public abstract class ElementBase
    {
        public virtual double AtomicWeight { get; set; }

        public virtual string Symbol { get; set; }

        public virtual string Name { get; set; }

        public virtual string Colour { get; set; }
    }
}