using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.TextFormatting;

namespace Chem4Word.ACME.Drawing
{
    partial class CustomTextSource : TextSource
    {
        public List<CustomTextSourceRun> Runs = new List<CustomTextSourceRun>();

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

                    var props =
                        new CustomTextRunProperties(currentRun.IsSubscript);

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

        class CustomTextRunTypographyProperties : TextRunTypographyProperties
        {
            private bool _subscript;

            public CustomTextRunTypographyProperties(bool subscript)
            {
                _subscript = subscript;
            }

            public override int AnnotationAlternates
            {
                get { return 0; }
            }

            public override bool CapitalSpacing
            {
                get { return false; }
            }

            public override System.Windows.FontCapitals Capitals
            {
                get { return FontCapitals.Normal; }
            }

            public override bool CaseSensitiveForms
            {
                get { return false; }
            }

            public override bool ContextualAlternates
            {
                get { return false; }
            }

            public override bool ContextualLigatures
            {
                get { return false; }
            }

            public override int ContextualSwashes
            {
                get { return 0; }
            }

            public override bool DiscretionaryLigatures
            {
                get { return false; }
            }

            public override bool EastAsianExpertForms
            {
                get { return false; }
            }

            public override System.Windows.FontEastAsianLanguage EastAsianLanguage
            {
                get { return FontEastAsianLanguage.Normal; }
            }

            public override System.Windows.FontEastAsianWidths EastAsianWidths
            {
                get { return FontEastAsianWidths.Normal; }
            }

            public override System.Windows.FontFraction Fraction
            {
                get { return FontFraction.Normal; }
            }

            public override bool HistoricalForms
            {
                get { return false; }
            }

            public override bool HistoricalLigatures
            {
                get { return false; }
            }

            public override bool Kerning
            {
                get { return true; }
            }

            public override bool MathematicalGreek
            {
                get { return false; }
            }

            public override System.Windows.FontNumeralAlignment NumeralAlignment
            {
                get { return FontNumeralAlignment.Normal; }
            }

            public override System.Windows.FontNumeralStyle NumeralStyle
            {
                get { return FontNumeralStyle.Normal; }
            }

            public override bool SlashedZero
            {
                get { return false; }
            }

            public override bool StandardLigatures
            {
                get { return false; }
            }

            public override int StandardSwashes
            {
                get { return 0; }
            }

            public override int StylisticAlternates
            {
                get { return 0; }
            }

            public override bool StylisticSet1
            {
                get { return false; }
            }

            public override bool StylisticSet10
            {
                get { return false; }
            }

            public override bool StylisticSet11
            {
                get { return false; }
            }

            public override bool StylisticSet12
            {
                get { return false; }
            }

            public override bool StylisticSet13
            {
                get { return false; }
            }

            public override bool StylisticSet14
            {
                get { return false; }
            }

            public override bool StylisticSet15
            {
                get { return false; }
            }

            public override bool StylisticSet16
            {
                get { return false; }
            }

            public override bool StylisticSet17
            {
                get { return false; }
            }

            public override bool StylisticSet18
            {
                get { return false; }
            }

            public override bool StylisticSet19
            {
                get { return false; }
            }

            public override bool StylisticSet2
            {
                get { return false; }
            }

            public override bool StylisticSet20
            {
                get { return false; }
            }

            public override bool StylisticSet3
            {
                get { return false; }
            }

            public override bool StylisticSet4
            {
                get { return false; }
            }

            public override bool StylisticSet5
            {
                get { return false; }
            }

            public override bool StylisticSet6
            {
                get { return false; }
            }

            public override bool StylisticSet7
            {
                get { return false; }
            }

            public override bool StylisticSet8
            {
                get { return false; }
            }

            public override bool StylisticSet9
            {
                get { return false; }
            }

            public override FontVariants Variants
            {
                get { return _subscript ? FontVariants.Subscript : FontVariants.Normal; }
            }
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