// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Chem4Word.Model2.Geometry;

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

        private void GetTextRuns()
        {
            ComponentRuns.Clear();
            BuildTextRuns(ParentGroup);
        }
        /// <summary>
        /// Recursively builds up a set of text runs from a functional group
        /// Allows you to reverse the order of the subgroups inside the group
        /// </summary>
        /// <param name="parentGroup">Functional group definition</param>
        private void BuildTextRuns(ElementBase parentGroup)
        {

        }

   
        public  void Render(DrawingContext dc)
        {


            var parentAtomPosition = ParentVisual.ParentAtom.Position;

            //Point startingPoint =
            //    parentAtomPosition + firstGlyph.TextMetrics.TextFormatterOffset;
            ;// - new Vector(0d, GlyphText.SymbolSize);

            int textStorePosition = 0;
            //dc.DrawLine(new Pen(Brushes.Gray, 1 ),parentAtomPosition,startingPoint );

            string expansion = this.ParentGroup.Expand(Flipped);
            var textStore = new FunctionalGroupTextSource(ParentGroup, Flipped);

            //GlyphText firstGlyph = new GlyphText(ComponentRuns[0].Text[0].ToString(), GlyphUtils.SymbolTypeface, GlyphText.SymbolSize, PixelsPerDip());

            //firstGlyph.MeasureAtCenter(parentAtomPosition);

            //textStore.Runs.AddRange(ComponentRuns);
            //textStore.Runs.Add(new LabelTextSourceRun());

            TextFormatter tc = TextFormatter.Create();
       
            //dc.DrawEllipse(Brushes.Red, null, ParentVisual.ParentAtom.Position, 20, 20);
            //ParentVisual.ShowPoints(new List<Point> {startingPoint}, dc);

            var paraprops = new FunctionalGroupTextSource.GenericTextParagraphProperties(
                FlowDirection.LeftToRight,
                TextAlignment.Left,
                true,
                false,
                new LabelTextRunProperties(), 
                TextWrapping.NoWrap,
                GlyphText.SymbolSize,
                0d);

            Vector dispVector;
            IEnumerable<IndexedGlyphRun> glyphRuns;
            using (TextLine myTextLine = tc.FormatLine(textStore, textStorePosition, 60,
                paraprops,
                null))
            {
                //myTextLine.Draw( dc, startingPoint, InvertAxes.None);
                TextRun anchorRun;
                int startingPos;

                var textRunSpans = myTextLine.GetTextRunSpans();
                //if (!Flipped)
                //{
                //    anchorRun = textRunSpans.First().Value;
                //    startingPos = 0;
                //}
                //else
                //{
                //    anchorRun = textRunSpans[textRunSpans.Count-2].Value; //avoids the end of par
                //    startingPos = myTextLine.Length - anchorRun.Length;
                //}

                IList<TextBounds> textBounds;
               
               

                Rect firstRect;

                if (!Flipped)
                {
                    textBounds = myTextLine.GetTextBounds(0, 1);
                }
                else
                {
                    //length-2 because otherwise you grab the CR/LF character midpoint!
                    textBounds = myTextLine.GetTextBounds(myTextLine.Length-2, 1);  
                  
                }

                firstRect = textBounds.First().Rectangle;

                //center will be position close to the origin 0,0
                Point center = new Point((firstRect.Left+firstRect.Right)/2, (firstRect.Top+firstRect.Bottom)/2);
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
                    var runOutline = GlyphUtils.GetOutline(currentRun, GlyphText.SymbolSize);

                    for (int i = 0; i <runOutline.Count; i++)
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
                dc.DrawGeometry(null, new Pen(Brushes.Red, thickness: 1), sg);
                dc.Close();
                var d = this.Drawing;
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