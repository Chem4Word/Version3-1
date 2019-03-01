// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using Chem4Word.Model2.Geometry;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace Chem4Word.ACME.Drawing
{
    public class FunctionalGroupVisual : AtomVisual
    {
        private List<LabelTextSourceRun> ComponentRuns { get; }
        public FunctionalGroup ParentGroup { get; }
        public AtomVisual ParentVisual { get; }
        public bool Flipped => ParentVisual.ParentAtom.BalancingVector.X < 0d;

        public FunctionalGroupVisual(object fg, AtomVisual parent)
        {
            ParentGroup = (FunctionalGroup)fg;
            ComponentRuns = new List<LabelTextSourceRun>();
            ParentVisual = parent;
        }

        public void Render(DrawingContext dc)
        {
            var parentAtomPosition = ParentVisual.ParentAtom.Position;

            int textStorePosition = 0;

            var textStore = new FunctionalGroupTextSource(ParentGroup, Flipped);

            //main textformatter - this does the writing of the visual
            TextFormatter textFormatter = TextFormatter.Create();

            //set up the default paragraph properties
            var paraprops = new FunctionalGroupTextSource.GenericTextParagraphProperties(
                FlowDirection.LeftToRight,
                TextAlignment.Left,
                true,
                false,
                new LabelTextRunProperties(),
                TextWrapping.NoWrap,
                GlyphText.SymbolSize,
                0d);

            //created purely to align the anchor -never writes to the screen
            TextFormatter anchorAligner = TextFormatter.Create();
            //set up a secondary text store simply to measure the anchor
            FunctionalGroup anchorGroup = new FunctionalGroup();
            anchorGroup.Components = new List<Group>();
            anchorGroup.Components.Add(ParentGroup.Components[0]); //add in the anchor
            //var anchorStore = new FunctionalGroupTextSource(anchorGroup, Flipped);
            string dummy = anchorGroup.Expand().TrimStart('[').TrimEnd(']');

            // Diag: Show Atom spot
            //dc.DrawEllipse(Brushes.Red, null, ParentVisual.ParentAtom.Position, 10, 10);
            //ParentVisual.ShowPoints(new List<Point> {startingPoint}, dc);

            Vector dispVector;
            IEnumerable<IndexedGlyphRun> glyphRuns;
            using (TextLine myTextLine = textFormatter.FormatLine(textStore, textStorePosition, 60,
                paraprops,
                null))
            {
                IList<TextBounds> textBounds;

                Rect firstRect = Rect.Empty;

                //dummy.Length is used to isolate the anchor group's characters' text bounding rectangle
                if (!Flipped)//isolate them at the beginning
                {
                    textBounds = myTextLine.GetTextBounds(0, dummy.Length);
                }
                else
                {
                    //isolate them at the end
                    textBounds = myTextLine.GetTextBounds(myTextLine.Length - 1 - dummy.Length, dummy.Length);
                }
                //add all the bounds together
                foreach (TextBounds anchorBound in textBounds)
                {
                    firstRect.Union(anchorBound.Rectangle);
                }

                //center will be position close to the origin 0,0
                Point center = new Point((firstRect.Left + firstRect.Right) / 2, (firstRect.Top + firstRect.Bottom) / 2);
                //the dispvector will be added to each relative coordinate for the glyphrun
                dispVector = parentAtomPosition - center;

                //locus is where the textline is drawn
                var locus = new Point(0, 0) + dispVector;

                //draw the line
                myTextLine.Draw(dc, locus, InvertAxes.None);

                glyphRuns = myTextLine.GetIndexedGlyphRuns();
                List<Point> outline = new List<Point>();
                double advanceWidths = 0d;

                //build up the convex hull from each glyph
                //you need to add in the advance widths for each
                //glyph run as they are traversed,
                //to the outline
                foreach (IndexedGlyphRun igr in glyphRuns)
                {
                    var currentRun = igr.GlyphRun;
                    var runOutline = GlyphUtils.GetOutline(currentRun);

                    for (int i = 0; i < runOutline.Count; i++)
                    {
                        var point = runOutline[i];
                        point.X += advanceWidths;
                        runOutline[i] = point;
                    }

                    outline.AddRange(runOutline);
                    advanceWidths += currentRun.AdvanceWidths.Sum();
                }
                var sortedOutline = (from Point p in outline
                                     orderby p.X ascending, p.Y descending
                                     select p + dispVector + new Vector(0.0, myTextLine.Baseline)).ToList();

                Hull = Geometry<Point>.GetHull(sortedOutline, p => p);
                StreamGeometry sg = BasicGeometry.BuildPolyPath(Hull);

                // Diag: Comment out to show hull and atom position
                //dc.DrawGeometry(null, new Pen(Brushes.Red, thickness: 1), sg);
                dc.DrawEllipse(Brushes.Red, null, ParentVisual.ParentAtom.Position, 5, 5);
                dc.Close();
            };
        }

        public override Geometry HullGeometry
        {
            get
            {
                //need to combine the actually filled atom area
                //with a stroked outline of it, to give a sufficient margin
                Geometry geo1 = BasicGeometry.BuildPolyPath(Hull);
                CombinedGeometry cg = new CombinedGeometry(geo1, geo1.GetWidenedPathGeometry(new Pen(Brushes.Black, Standoff)));
                return cg;
            }
        }

        public override Geometry WidenedHullGeometry
        {
            get
            {
                if (!string.IsNullOrEmpty(AtomSymbol))
                {
                    //Pen _widepen = new Pen(Brushes.Black, BondThickness);
                    return HullGeometry;
                }

                return null;
            }
        }
    }
}