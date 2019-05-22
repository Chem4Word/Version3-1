// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------

using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Controls
{
    public class FunctionalGroupBlock : TextBlock
    {
        // Using a DependencyProperty as the backing store for ParentGroup.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ParentGroupProperty =
            DependencyProperty.Register("ParentGroup", typeof(FunctionalGroup), typeof(FunctionalGroupBlock),
                                        new FrameworkPropertyMetadata(FunctionalGroupChanged));


        public FunctionalGroup ParentGroup
        {
            get => (FunctionalGroup) GetValue(ParentGroupProperty);
            set => SetValue(ParentGroupProperty, value);
        }

        private static void FunctionalGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var tb = (FunctionalGroupBlock) d;
            tb.BuildTextBlock((FunctionalGroup) e.NewValue);
        }


        public void BuildTextBlock(FunctionalGroup fg)
        {
            if (fg.ShowAsSymbol)
            {
                //search the symbol for superscripts:  there should be one pattern or zero
                var superstart = fg.Symbol.IndexOf('{');
                var superend = fg.Symbol.IndexOf('}');
                if ((superstart == -1) | (superend == -1))
                {
                    Inlines.Add(new Run(fg.Symbol));
                }
                else
                {
                    var textbefore = "";
                    var supertext = "";
                    var textafter = "";
                    var i = 0;
                    var before = true;

                    textbefore = fg.Symbol.Substring(0, superstart);
                    supertext = fg.Symbol.Substring(superstart + 1, superend - superstart - 1);
                    textafter = fg.Symbol.Substring(superend + 1);

                    if (textbefore != "")
                    {
                        Inlines.Add(new Run(textbefore));
                    }

                    if (supertext != "")
                    {
                        Inlines.Add(new Run(supertext)
                                    {
                                        Typography = {Variants = FontVariants.Subscript},
                                        BaselineAlignment = BaselineAlignment.Superscript,
                                        FontSize = FontSize * 0.6
                                    });
                    }

                    if (textafter != "")
                    {
                        Inlines.Add(new Run(textafter));
                    }
                }
            }
            else
            {
                foreach (var component in fg.Components)
                {
                    AddRuns(component);
                }
            }
        }

        private void AddRuns(Group group)
        {
            var ok = AtomHelpers.TryParse(group.Component, out var elem);
            if (ok)
            {
                if (elem is Element element)
                {
                    Inlines.Add(new Run(element.Symbol));

                    if (group.Count != 1)
                    {
                        Inlines.Add(new Run(group.Count.ToString())
                                    {
                                        Typography = {Variants = FontVariants.Subscript},
                                        BaselineAlignment = BaselineAlignment.Subscript,
                                        FontSize = FontSize * 0.6
                                    });
                    }
                }

                if (elem is FunctionalGroup fg)
                {
                    Inlines.Add(new Run("("));
                    if (fg.ShowAsSymbol)
                    {
                        Inlines.Add(fg.Symbol);
                    }
                    else
                    {
                        foreach (var fgc in fg.Components)
                        {
                            AddRuns(fgc);
                        }
                    }

                    if (group.Count != 1)
                    {
                        Inlines.Add(")");
                        Inlines.Add(new Run(group.Count.ToString())
                                    {
                                        Typography = {Variants = FontVariants.Subscript},
                                        BaselineAlignment = BaselineAlignment.Subscript,
                                        FontSize = FontSize * 0.6
                                    });
                    }
                }
            }
        }
    }
}