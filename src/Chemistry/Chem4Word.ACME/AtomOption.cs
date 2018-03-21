using Chem4Word.Model;
using System.Windows;
using System.Windows.Controls;

namespace Chem4Word.ACME
{
    public class AtomOption : ComboBoxItem
    {
        
        public ElementBase Element
        {
            get => (ElementBase)GetValue(ElementProperty);
            set => SetValue(ElementProperty, value);
        }

        // Using a DependencyProperty as the backing store for Element.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ElementProperty =
            DependencyProperty.Register("Element", typeof(ElementBase), typeof(AtomOption), new PropertyMetadata(default(ElementBase)));



        public Style DisplayStyle
        {
            get => (Style)GetValue(DisplayStyleProperty);
            set => SetValue(DisplayStyleProperty, value);
        }

        // Using a DependencyProperty as the backing store for DisplayStyle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayStyleProperty =
            DependencyProperty.Register("DisplayStyle", typeof(Style), typeof(AtomOption), new PropertyMetadata(default(Style)));

    }
}
