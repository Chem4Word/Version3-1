// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;
using System.Windows.Media;
using Chem4Word.ACME.Controls;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.ACME.Adorners.Selectors
{
    public class AtomSelectionAdorner : SingleChemistryAdorner
    {
        #region Properties

        public Atom AdornedAtom => (Atom)AdornedChemistry;

        #endregion Properties

        #region Constructors

        public AtomSelectionAdorner(EditorCanvas currentEditor, Atom adornedAtom) : base(currentEditor, adornedAtom)
        {
            IsHitTestVisible = true;
        }

        #endregion Constructors

        #region Methods

        #region Overrides

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            double renderRadius = (EditViewModel.Model.XamlBondLength * Globals.FontSizePercentageBond) / 4;

            SolidColorBrush renderBrush = new SolidColorBrush(SystemColors.HighlightColor);
            renderBrush.Opacity = 0.25;
            if (AdornedAtom.SymbolText == "")
            {
                drawingContext.DrawEllipse(renderBrush, null, AdornedAtom.Position, renderRadius, renderRadius);
            }
            else
            {
                drawingContext.DrawGeometry(renderBrush, null, CurrentEditor.GetAtomVisual(AdornedAtom).HullGeometry);
            }
        }

        #endregion Overrides

        #endregion Methods
    }
}