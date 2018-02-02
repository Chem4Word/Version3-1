using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using Chem4Word.Model.Geometry;
using Chem4Word.View;

namespace Chem4Word.ViewModel
{
    /// <summary>
    /// wraps up some of the glyph handling into a handy class 
    /// Mostly stateful and uses properties to simplify the client code
    /// </summary>
    /// 
    /// 
    public class GlyphText
    {
        public string Text { get;  }
        public Typeface CurrentTypeface { get;  }

        public double Typesize { get; }

        public float PixelsPerDip { get; }

        private GlyphTypeface _glyphTypeface;

        public GlyphUtils.GlyphInfo GlyphInfo { get; protected set; }
        public AtomTextMetrics TextMetrics { get; protected set; }

        public GlyphRun TextRun { get; protected set; }
        public Brush Fill { get; set; }
        public Path Outline
        {
            get
            {
                var hull = Hull;
                return BasicGeometry.BuildPath(hull);
            }

        }

        public List<Point> Hull
        {
            get
            {
                var outline = GlyphUtils.GetOutline(TextRun);
                List<Point> hull = Geometry<Point>.GetHull(outline, p => p);
                return hull;
            }
        }

        public GlyphText(string text, Typeface typeface, double typesize, float pixelsPerDip)
        {
            if (!GlyphUtils.SymbolTypeface.TryGetGlyphTypeface(out _glyphTypeface))
            {
                throw new InvalidOperationException($"No glyph typeface found for the Windows Typeface '{typeface.FaceNames[XmlLanguage.GetLanguage("en-GB")]}'");
            }
            Text = text;
            CurrentTypeface = typeface;
            Typesize = typesize;
            PixelsPerDip = pixelsPerDip;

            TextMetrics = null;

        }


        public void MeasureAtCenter(Point center)
        {
            GlyphInfo = GlyphUtils.GetGlyphsAndInfo(Text, PixelsPerDip, out GlyphRun groupGlyphRun, center, _glyphTypeface, Typesize);
            Vector mainHOffset = GlyphUtils.GetOffsetVector(groupGlyphRun, GlyphUtils.SymbolSize);
            TextRun = groupGlyphRun;
            TextMetrics = new AtomTextMetrics
            {
                BoundingBox = groupGlyphRun.GetBoundingBox(center + mainHOffset),
                Geocenter = center,
                TotalBoundingBox = groupGlyphRun.GetBoundingBox(center + mainHOffset)
            };

        }

        public void Premeasure()
        {
            MeasureAtCenter(new Point(0d,0d));
        }

        public void MeasureAtBottomLeft(Point bottomLeft, float PixelsPerDip)
        {
            GlyphInfo = GlyphUtils.GetGlyphsAndInfo(Text, PixelsPerDip, out GlyphRun groupGlyphRun, bottomLeft, _glyphTypeface, Typesize);
            TextRun=groupGlyphRun;
            TextMetrics = new AtomTextMetrics
            {
                BoundingBox = groupGlyphRun.GetBoundingBox(bottomLeft),
                Geocenter = bottomLeft,
                TotalBoundingBox = groupGlyphRun.GetBoundingBox(bottomLeft)
            };
        }

      

        public void DrawAtBottomLeft(Point bottomLeft, DrawingContext dc)
        {

            GlyphInfo = GlyphUtils.GetGlyphsAndInfo(Text, PixelsPerDip, out GlyphRun groupGlyphRun, bottomLeft, _glyphTypeface, Typesize);
            dc.DrawGlyphRun(Fill, groupGlyphRun);
#if DEBUG
            //dc.DrawRectangle(null, new Pen(Brushes.Black, 0.5),  TextMetrics.BoundingBox );
#endif
            TextRun = groupGlyphRun;
        }

        public void Union(GlyphText gt)
        {
            Rect res = TextMetrics.BoundingBox;
            res.Union(gt.TextMetrics.TotalBoundingBox);
            TextMetrics.TotalBoundingBox = res;
        }

        public bool CollidesWith(params Rect[] occupiedAreas)
        {
            foreach (Rect area in occupiedAreas)
            {
                if (area.IntersectsWith(TextMetrics.TotalBoundingBox))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class MainLabelText : GlyphText
    {
        public MainLabelText(string text, float pixelsPerDip): base(text, GlyphUtils.SymbolTypeface, GlyphUtils.SymbolSize, pixelsPerDip)
        { }
    }


    public class SubLabelText : GlyphText
    {
        public SubLabelText(string text, float pixelsPerDip) : base(text, GlyphUtils.SymbolTypeface, GlyphUtils.ScriptSize, pixelsPerDip)
        { }
    }

    public class IsotopeLabelText : GlyphText
    {
        public IsotopeLabelText(string text, float pixelsPerDip) : base(text, GlyphUtils.SymbolTypeface, GlyphUtils.IsotopeSize, pixelsPerDip)
        { }
    }

    public class ChargeLabelText : GlyphText
    {
        public ChargeLabelText(string text, float pixelsPerDip) : base(text, GlyphUtils.SymbolTypeface, GlyphUtils.ScriptSize, pixelsPerDip)
        {
        }
    }

    /// <summary>
    /// Facilitates layout and positioning of text
    /// </summary>
    public class AtomTextMetrics: LabelMetrics
    {
       
        public Rect TotalBoundingBox; //surrounds ALL the text

        public AtomTextMetrics()
        {
            TotalBoundingBox = new Rect(0d,0d,0d,0d);
        }

        public override List<Point> Corners
        {
            get
            {
                List<Point> corners = new List<Point>();
                corners.Add(TotalBoundingBox.BottomLeft);
                corners.Add(TotalBoundingBox.BottomRight);
                corners.Add(TotalBoundingBox.TopLeft);
                corners.Add(TotalBoundingBox.TopRight);
                return corners;
            }
        }
    }

    public class LabelMetrics
    {
        public Point Geocenter;  //the center of the charge text
        public Rect BoundingBox; //the bounding box surrounds the text

        public LabelMetrics()
        {
            Geocenter = new Point(0d,0d);
            BoundingBox = new Rect(0d,0d,0d,0d);
        }
        public virtual List<Point> Corners
        {
            get
            {
                List<Point> corners = new List<Point>();
                corners.Add(BoundingBox.BottomLeft);
                corners.Add(BoundingBox.BottomRight);
                corners.Add(BoundingBox.TopLeft);
                corners.Add(BoundingBox.TopRight);
                return corners;
            }
        }
    }
}
