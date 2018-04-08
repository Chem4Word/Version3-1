using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Chem4Word.Model.Annotations;

namespace Chem4Word.ViewModel.Adorners
{
    public class LassoAdorner:Adorner
    {
        private StreamGeometry _outline;
           
        public LassoAdorner([NotNull] UIElement adornedElement) : base(adornedElement)
        {
            var myAdornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            myAdornerLayer.Add(this);
        }

        public LassoAdorner([NotNull] UIElement adornedElement, StreamGeometry outline) : this(adornedElement)
        {
            _outline = outline;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            SolidColorBrush brush = new SolidColorBrush(SystemColors.HighlightColor);
            brush.Opacity = 0.25;

            Pen dashPen = new Pen(SystemColors.HighlightBrush, 1);
            dashPen.DashStyle = DashStyles.Dash;

            drawingContext.DrawGeometry(brush,dashPen,_outline);
        }
    }
}
