using System.Windows;

namespace Chem4Word.ACME.Drawing
{
    public class SubscriptTextRunTypographyProperties : LabelTextRunTypographyProperties
    {
        public override FontVariants Variants
        {
            get { return FontVariants.Subscript; }
        }
    }
}