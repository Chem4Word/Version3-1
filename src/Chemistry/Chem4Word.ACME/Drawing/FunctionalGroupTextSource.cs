﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Windows.Media.TextFormatting;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Drawing
{
    partial class FunctionalGroupTextSource : TextSource
    {
        public List<LabelTextSourceRun> Runs = new List<LabelTextSourceRun>();

        public FunctionalGroupTextSource()
        {
        }

        public FunctionalGroupTextSource(FunctionalGroup parentGroup, bool isFlipped = false)
        {
            foreach (var term in parentGroup.ExpandIntoTerms(isFlipped))
            {
                foreach (var part in term.Parts)
                {
                    Runs.Add(new LabelTextSourceRun
                    {
                        IsAnchor = term.IsAnchor,
                        IsEndParagraph = false,
                        IsSubscript = part.Type == FunctionalGroupPartType.Subscript,
                        IsSuperscript = part.Type == FunctionalGroupPartType.Superscript,
                        Text = part.Text
                    });
                }
            }
        }

        public override TextRun GetTextRun(int textSourceCharacterIndex)
        {
            int pos = 0;
            foreach (var currentRun in Runs)
            {
                if (textSourceCharacterIndex < pos + currentRun.Length)
                {
                    if (currentRun.IsEndParagraph)
                    {
                        return new TextEndOfParagraph(1);
                    }

                    TextRunProperties props;
                    if (currentRun.IsSubscript)
                    {
                        props = new SubscriptTextRunProperties();
                    }
                    else if (currentRun.IsSuperscript)
                    {
                        props = new SuperscriptTextRunProperties();
                    }
                    else
                    {
                        props = new LabelTextRunProperties();
                    }

                    return new TextCharacters(
                        currentRun.Text,
                        textSourceCharacterIndex - pos,
                        currentRun.Length - (textSourceCharacterIndex - pos),
                        props);
                }
                pos += currentRun.Length;
            }

            // Return an end-of-paragraph if no more text source.
            return new TextEndOfParagraph(1);
        }

        public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit)
        {
            // Never called, but must be implemented
            throw new Exception("The method or operation is not implemented.");
        }

        public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex)
        {
            // Never called, but must be implemented
            throw new Exception("The method or operation is not implemented.");
        }
    }
}