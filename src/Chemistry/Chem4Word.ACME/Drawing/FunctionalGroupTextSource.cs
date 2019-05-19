// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media.TextFormatting;
using Group = Chem4Word.Model2.Group;

namespace Chem4Word.ACME.Drawing
{
    partial class FunctionalGroupTextSource : TextSource
    {
        private static Regex _superscriptRegEx = new Regex("(?<normal>[^{}]+)|(?<super>\\{[^{}]+\\})");
        public List<LabelTextSourceRun> Runs = new List<LabelTextSourceRun>();

        public FunctionalGroupTextSource(FunctionalGroup parentGroup, bool isFlipped = false)
        {
            Expand(parentGroup, isFlipped);
        }

        private void Expand(FunctionalGroup parentGroup, bool isFlipped)
        {
            if (parentGroup.ShowAsSymbol)
            {
                var super = _superscriptRegEx.Matches(parentGroup.Symbol);

                foreach (Match match in super)
                {
                    if (match.Value.Contains("{"))//it's a superscript
                    {
                        Runs.Add(new LabelTextSourceRun() { IsAnchor = true, IsSuperscript = true, IsEndParagraph = false, Text = match.Value.TrimStart('{').TrimEnd('}') });
                    }
                    else
                    {
                        Runs.Add(new LabelTextSourceRun() { IsAnchor = true, IsEndParagraph = false, Text = match.Value });
                    }
                }
            }
            else
            {
                if (isFlipped && parentGroup.Flippable)
                {
                    for (int i = parentGroup.Components.Count - 1; i >= 0; i--)
                    {
                        Append(parentGroup.Components[i], i == 0);
                    }
                }
                else
                {
                    int ii = 0;
                    foreach (var component in parentGroup.Components)
                    {
                        Append(component, ii == 0);
                        ii++;
                    }
                }
            }

            // Local Function
            void Append(Group component, bool isAnchor)
            {
                ElementBase eb;
                if (AtomHelpers.TryParse(component.Component, out eb))
                {
                    if (eb is Element)
                    {
                        Runs.Add(new LabelTextSourceRun
                        {
                            IsAnchor = isAnchor,
                            IsEndParagraph = false,
                            Text = component.Component
                        });
                        if (component.Count > 1)
                        {
                            Runs.Add(new LabelTextSourceRun
                            {
                                IsAnchor = isAnchor,
                                IsSubscript = true,
                                IsEndParagraph = false,
                                Text = component.Count.ToString()
                            });
                        }
                    }
                    else if (eb is FunctionalGroup fg)
                    {
                        if (component.Count > 1)
                        {
                            Runs.Add(new LabelTextSourceRun()
                            {
                                IsAnchor = false,
                                IsEndParagraph = false,
                                IsSubscript = false,
                                Text = "("
                            });
                        }

                        if (fg.ShowAsSymbol)
                        {
                            Runs.Add(new LabelTextSourceRun
                            {
                                IsAnchor = isAnchor,
                                IsEndParagraph = false,
                                Text = component.Component
                            });
                        }
                        else
                        {
                            Expand(fg, false);
                        }

                        if (component.Count > 1)
                        {
                            Runs.Add(new LabelTextSourceRun()
                            {
                                IsAnchor = false,
                                IsEndParagraph = false,
                                IsSubscript = false,
                                Text = ")"
                            });
                            Runs.Add(new LabelTextSourceRun
                            { IsAnchor = isAnchor, IsSubscript = true, IsEndParagraph = false, Text = component.Count.ToString() });
                        }
                    }

                    //
                }
                else
                {
                    Runs.Add(new LabelTextSourceRun
                    {
                        IsAnchor = isAnchor,
                        IsEndParagraph = false,
                        Text = "??" //WTF!
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
            throw new Exception("The method or operation is not implemented.");
        }

        public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public int Length
        {
            get
            {
                int r = 0;
                foreach (var currentRun in Runs)
                {
                    r += currentRun.Length;
                }
                return r;
            }
        }
    }
}