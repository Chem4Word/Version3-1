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
            var textStore = new FunctionalGroupTextSource(expansion);

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

            Vector dragBackVector;
            IEnumerable<IndexedGlyphRun> glyphRuns;
            using (TextLine myTextLine = tc.FormatLine(textStore, textStorePosition, 60,
                paraprops,
                null))
            {
                //myTextLine.Draw( dc, startingPoint, InvertAxes.None);
                TextRun anchorRun;
                int startingPos;

                var textRunSpans = myTextLine.GetTextRunSpans();
                if (!Flipped)
                {
                    anchorRun = textRunSpans.First().Value;
                    startingPos = 0;
                }
                else
                {
                    anchorRun = textRunSpans[textRunSpans.Count-2].Value; //avoids the end of par
                    startingPos = myTextLine.Length - anchorRun.Length;
                }

                var textBounds = myTextLine.GetTextBounds(0, myTextLine.Length);
                
                var firstRect = textBounds[0].Rectangle;
                Point center = new Point((firstRect.Left+firstRect.Right)/2, (firstRect.Top+firstRect.Bottom)/2);
                dragBackVector = parentAtomPosition - center;
                var locus = new Point(0, 0) + dragBackVector;
                glyphRuns = myTextLine.GetIndexedGlyphRuns();
                myTextLine.Draw(dc, locus, InvertAxes.None);

                List<Point> outline = new List<Point>();

                foreach (IndexedGlyphRun igr in glyphRuns)
                {
                //https://docs.microsoft.com/en-us/dotnet/api/system.windows.media.glyphrun.glyphoffsets?f1url=https%3A%2F%2Fmsdn.microsoft.com%2Fquery%2Fdev15.query%3FappId%3DDev15IDEF1%26l%3DEN-US%26k%3Dk(System.Windows.Media.GlyphRun.GlyphOffsets);k(SolutionItemsProject);k(TargetFrameworkMoniker-.NETFramework,Version%3Dv4.6.2);k(DevLang-csharp)%26rd%3Dtrue&view=netframework-4.7.2
                // The glyph offset values are added to the nominal glyph origin to
                // generate the final origin for the glyph. The
                // AdvanceWidths property represents the values of the nominal
                // glyph origins for the GlyphRun.
                https://docs.microsoft.com/en-us/dotnet/api/system.windows.media.glyphrun.advancewidths?view=netframework-4.7.2
                    // Each item in the list of advance widths corresponds to the glyph values returned by the GlyphIndices property.
                    // The nominal origin of the nth glyph in the run (n>0) is the nominal origin of the n-1th
                    // glyph plus the n-1th advance width added along the runs advance vector.
                    // Base glyphs generally have a non-zero advance width, whereas combining glyphs generally have a zero advance width.

                    //Base glyphs generally have a glyph
                    // offset of(0, 0), combining glyphs generally have an offset that places them
                    // correctly on top of the nearest preceding base glyph.
                    var offsets = igr.GlyphRun.GlyphOffsets;
                    var advanceWidth = igr.GlyphRun.AdvanceWidths;
                    var runOutline = GlyphUtils.GetOutline(igr.GlyphRun, GlyphText.SymbolSize);
                    outline.AddRange(runOutline);
                }
                var sortedOutline = (from Point p in outline
                    orderby p.X ascending, p.Y descending
                    select p + dragBackVector + new Vector(0.0, myTextLine.Baseline)).ToList();

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