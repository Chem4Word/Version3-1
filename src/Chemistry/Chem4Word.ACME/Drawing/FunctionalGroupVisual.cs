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
        private List<CustomTextSourceRun> ComponentRuns { get; }
        public FunctionalGroup ParentGroup { get; }

        public Point Position { get; set; }
        public bool Flipped { get; set; }
        public FunctionalGroupVisual(object fg)
        {
            ParentGroup = (FunctionalGroup)fg;
            ComponentRuns = new List<CustomTextSourceRun>();
            
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
            string ReverseString(string input)
            {
                char[] charArray = input.ToCharArray();
                Array.Reverse(charArray);
                return new string(charArray);
            }

            //do the simple cases first
            if (parentGroup is FunctionalGroup fg && fg.ShowAsSymbol)
            {
                if (!Flipped)
                {
                    ComponentRuns.Add(new CustomTextSourceRun {Text = fg.Symbol});
                }
                else //it's flipped
                {
                    //gotta reverse the text or it will come out backwards
                    ComponentRuns.Add(new CustomTextSourceRun {Text = ReverseString(fg.Symbol)});
                }
            }
            else if (parentGroup is Element e)
            {
                if (!Flipped)
                {
                    ComponentRuns.Add(new CustomTextSourceRun { Text = e.Symbol });
                }
                else
                {
                    ComponentRuns.Add(new CustomTextSourceRun { Text = ReverseString(e.Symbol) });
                }
            }
            else if (parentGroup is FunctionalGroup fg2)
            {
                if(fg2.Flippable)
                {
                    for (int i = 0; i< fg2.Components.Count; i++)
                    {
                        var component = fg2.Components[i];
                        var group = component.Resolve();
                        int count = component.Count;

                        if (!Flipped)
                        {
                            if(count > 1 && group is FunctionalGroup fg3)
                            {
                                //need to draw brackets around it
                                ComponentRuns.Add(new CustomTextSourceRun { Text = "(" });
                                BuildTextRuns(fg3);
                                ComponentRuns.Add(new CustomTextSourceRun { Text = ")" });
                                ComponentRuns.Add(new CustomTextSourceRun { Text = count.ToString(), IsSubscript=true });

                            }
                            else if (count > 1 && group is Element e2)
                            {
                                BuildTextRuns(e2);
                                ComponentRuns.Add(new CustomTextSourceRun { Text = count.ToString(), IsSubscript = true });
                            }
                            else
                            {
                                BuildTextRuns(group);
                            }
                        }
                        else
                        {
                            if (count > 1 && group is FunctionalGroup fg3)
                            {
                                //assemble it backwards as we are drawing from right to left
                                ComponentRuns.Add(new CustomTextSourceRun { Text = count.ToString(), IsSubscript = true });
                                //need to draw brackets around it
                                ComponentRuns.Add(new CustomTextSourceRun { Text = ")" });
                                BuildTextRuns(fg3);
                                ComponentRuns.Add(new CustomTextSourceRun { Text = "(" });
                                

                            }
                            else if (count > 1 && group is Element e2)
                            {
                                ComponentRuns.Add(new CustomTextSourceRun {Text = count.ToString(), IsSubscript = true});
                                BuildTextRuns(e2);
                            }
                            else
                            {
                                BuildTextRuns(group);
                            }
                        }
                    }
                }
            }
        }

   
        public override void Render()
        {
            GetTextRuns();
            GlyphText firstGlyph = new GlyphText(ComponentRuns[0].Text[0].ToString(),GlyphUtils.SymbolTypeface, GlyphText.SymbolSize, PixelsPerDip());
            firstGlyph.MeasureAtCenter(Position);

            Point startingPoint = firstGlyph.TextMetrics.OffsetVector + Position;

            int textStorePosition = 0;

            var textStore = new CustomTextSource();
            textStore.Runs.AddRange(ComponentRuns);
         

            TextFormatter tc = TextFormatter.Create();
            using (DrawingContext dc = this.RenderOpen())
            {
                using (TextLine myTextLine = tc.FormatLine(textStore, textStorePosition, 96 * 6,
                    new CustomTextSource.GenericTextParagraphProperties(
                        Flipped ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                        TextAlignment.Left,
                        true,
                        false,
                        new CustomTextSource.CustomTextRunProperties(false),
                        TextWrapping.NoWrap,
                        GlyphText.SymbolSize,
                        0d),
                    null))
                {
                    myTextLine.Draw(dc, startingPoint, InvertAxes.None);
                    
                };

                dc.Close();
            }

            

            BuildHull();
        }

        private void BuildHull()
        {
            var outline = BasicGeometry.CreateGeometry(this.Drawing).GetFlattenedPathGeometry();
            Hull = new List<Point>();
            GlyphUtils.GetGeoPoints(outline, Hull);

            var sortedHull = (from Point p in Hull
                orderby p.X, p.Y descending
                select p).ToList();

            Hull = Geometry<Point>.GetHull(sortedHull, p => p);
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