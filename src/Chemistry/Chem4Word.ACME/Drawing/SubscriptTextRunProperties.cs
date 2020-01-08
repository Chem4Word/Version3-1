// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Media.TextFormatting;

namespace Chem4Word.ACME.Drawing
{
    public class SubscriptTextRunProperties : LabelTextRunProperties
    {
        public SubscriptTextRunProperties(string colour) : base(colour)
        {
        }

        public override double FontHintingEmSize
        {
            get { return GlyphText.ScriptSize; }
        }

        public override double FontRenderingEmSize
        {
            get { return GlyphText.ScriptSize; }
        }

        public override BaselineAlignment BaselineAlignment
        {
            get { return BaselineAlignment.Subscript; }
        }

        public override TextRunTypographyProperties TypographyProperties
        {
            get
            {
                return new SubscriptTextRunTypographyProperties();
            }
        }
    }
}