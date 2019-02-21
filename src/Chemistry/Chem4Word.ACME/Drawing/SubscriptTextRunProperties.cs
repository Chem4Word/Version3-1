using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace Chem4Word.ACME.Drawing
{
   
        public class SubscriptTextRunProperties : LabelTextRunProperties
        {
           

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