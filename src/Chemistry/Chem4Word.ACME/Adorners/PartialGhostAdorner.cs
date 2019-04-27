using Chem4Word.ACME.Controls;
using Chem4Word.Model2;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Adorners
{
    public class PartialGhostAdorner : Adorner
    {
        private Geometry _outline;
        private SolidColorBrush _ghostBrush;
        private Pen _ghostPen;
        public EditorCanvas CurrentEditor { get; }

        public PartialGhostAdorner(EditorCanvas currentEditor, IEnumerable<Atom> atomList, Transform shear) : base(currentEditor)
        {
            _ghostBrush = new SolidColorBrush(SystemColors.HighlightColor);
            _ghostBrush.Opacity = 0.25;
            
            _ghostPen = new Pen(SystemColors.HighlightBrush, Globals.BondThickness);
            var myAdornerLayer = AdornerLayer.GetAdornerLayer(currentEditor);
            Ghost = currentEditor.PartialGhost(atomList.ToList(), shear);
            myAdornerLayer.Add(this);
            PreviewMouseMove += PartialGhostAdorner_PreviewMouseMove;
            PreviewMouseUp += PartialGhostAdorner_PreviewMouseUp;
            CurrentEditor = currentEditor;

        }

        private void PartialGhostAdorner_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }


        private void PartialGhostAdorner_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            CurrentEditor.RaiseEvent(e);
        }

        public Geometry Ghost
        {
            get { return _outline; }
            set
            {
                _outline = value;
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawGeometry(_ghostBrush, _ghostPen, _outline);
        }
    }
}