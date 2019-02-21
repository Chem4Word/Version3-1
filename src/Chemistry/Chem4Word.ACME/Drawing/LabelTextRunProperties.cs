using System.Globalization;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace Chem4Word.ACME.Drawing
{
   
        public class LabelTextRunProperties : TextRunProperties
        {
           
            public override System.Windows.Media.Brush BackgroundBrush
            {
                get { return null; }
            }

            public override CultureInfo CultureInfo
            {
                get { return CultureInfo.CurrentCulture; }
            }

            public override double FontHintingEmSize
            {
                get { return GlyphText.SymbolSize; }
            }

            public override double FontRenderingEmSize
            {
                get { return GlyphText.SymbolSize; }
            }

            public override Brush ForegroundBrush
            {
                get { return Brushes.Black; }
            }

            public override System.Windows.TextDecorationCollection TextDecorations
            {
                get { return new System.Windows.TextDecorationCollection(); }
            }

            public override System.Windows.Media.TextEffectCollection TextEffects
            {
                get { return new TextEffectCollection(); }
            }

            public override System.Windows.Media.Typeface Typeface
            {
                get { return GlyphUtils.SymbolTypeface; }
            }

            public override TextRunTypographyProperties TypographyProperties
            {
                get
                {
                    return new LabelTextRunTypographyProperties();
                }
            }

        }
    
}