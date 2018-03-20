using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Chem4Word.Model;
using Chem4Word.Model.Enums;

namespace Chem4Word.ACME
{
    /// <summary>
    /// Rolls up Bond Stereo and Order into a single class to facilitate binding
    /// </summary>
    public class BondOption : DependencyObject
    {




        public String  Order
        {
            get { return (string)GetValue(OrderProperty); }
            set { SetValue(OrderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Order.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OrderProperty =
            DependencyProperty.Register("Order", typeof(string), typeof(BondOption), new PropertyMetadata(Bond.OrderPartial01));



        public BondStereo? Stereo
        {
            get { return (BondStereo?)GetValue(BondStereoEnumsProperty); }
            set { SetValue(BondStereoEnumsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BondStereoEnums.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BondStereoEnumsProperty =
            DependencyProperty.Register("Stereo", typeof(BondStereo?), typeof(BondOption), new PropertyMetadata(BondStereo.None));



        public Style DisplayStyle
        {
            get { return (Style)GetValue(DisplayStyleProperty); }
            set { SetValue(DisplayStyleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DisplayStyle.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayStyleProperty =
            DependencyProperty.Register("DisplayStyle", typeof(Style), typeof(BondOption), new PropertyMetadata(null));
    }
}
