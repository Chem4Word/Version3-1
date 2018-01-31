using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using static Chem4Word.ViewModel.GlyphUtils;
// ReSharper disable once CheckNamespace
namespace Chem4Word.View
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

        public GlyphInfo GlyphInfo { get; protected set; }
        public AtomTextMetrics TextMetrics { get; protected set; }

        public Brush Fill { get; set; }
        public GlyphText(string text, Typeface typeface, double typesize, float pixelsPerDip)
        {
            if (!SymbolTypeface.TryGetGlyphTypeface(out _glyphTypeface))
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
            GlyphInfo = GetGlyphsAndInfo(Text, PixelsPerDip, out GlyphRun groupGlyphRun, center, _glyphTypeface, Typesize);
            Vector mainHOffset = GetOffsetVector(groupGlyphRun, SymbolSize);
            TextMetrics= new AtomTextMetrics
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
            GlyphInfo = GetGlyphsAndInfo(Text, PixelsPerDip, out GlyphRun groupGlyphRun, bottomLeft, _glyphTypeface, Typesize);
            TextMetrics = new AtomTextMetrics
            {
                BoundingBox = groupGlyphRun.GetBoundingBox(bottomLeft),
                Geocenter = bottomLeft,
                TotalBoundingBox = groupGlyphRun.GetBoundingBox(bottomLeft)
            };
        }

      

        public void DrawAtBottomLeft(Point bottomLeft, DrawingContext dc)
        {

            GlyphInfo = GetGlyphsAndInfo(Text, PixelsPerDip, out GlyphRun groupGlyphRun, bottomLeft, _glyphTypeface, Typesize);
            dc.DrawGlyphRun(Fill, groupGlyphRun);
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
        public MainLabelText(string text, float pixelsPerDip): base(text, SymbolTypeface, SymbolSize, pixelsPerDip)
        { }
    }


    public class SubLabelText : GlyphText
    {
        public SubLabelText(string text, float pixelsPerDip) : base(text, SymbolTypeface, ScriptSize, pixelsPerDip)
        { }
    }

    public class IsotopeLabelText : GlyphText
    {
        public IsotopeLabelText(string text, float pixelsPerDip) : base(text, SymbolTypeface, IsotopeSize, pixelsPerDip)
        { }
    }

    public class ChargeLabelText : GlyphText
    {
        public ChargeLabelText(string text, float pixelsPerDip) : base(text, SymbolTypeface, ScriptSize, pixelsPerDip)
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
