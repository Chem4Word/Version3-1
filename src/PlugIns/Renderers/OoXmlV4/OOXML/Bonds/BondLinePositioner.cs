// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;

namespace Chem4Word.Renderer.OoXmlV4.OOXML.Bonds
{
    public class BondLinePositioner
    {
        private List<BondLine> m_BondLines;
        private double m_medianBondLength;

        public BondLinePositioner(List<BondLine> bondLines, double medianBondLength)
        {
            m_BondLines = bondLines;
            m_medianBondLength = medianBondLength;
        }

        /// <summary>
        /// Creates the lines for a bond
        /// </summary>
        /// <param name="wordprocessingGroup1">Where to add the bond lines</param>
        /// <param name="bond"></param>
        public void CreateLines(Bond bond)
        {
            //Debug.WriteLine("Bond: " + bond.Id);

            //IEnumerable<Atom> bondatoms = bond.GetAtoms();
            IEnumerable<Ring> rings = bond.Rings;
            int ringCount = 0;
            foreach (Ring r in rings)
            {
                ringCount++;
            }
            //Debug.WriteLine("  Ring Count: " + ringCount);

            Point bondStart = bond.StartAtom.Position;

            Point bondEnd = bond.EndAtom.Position;

            //IEnumerable<CmlBond> toAtomBonds = toAtom.GetLigandBonds();
            //int toAtomBondCount = toAtomBonds.ToArray<CmlBond>().Length;

            //Debug.WriteLine("    From : " + fromAtom.ElementType + " [" + fromAtom.Id + "] at " + bondStart.X + ", " + bondStart.Y);
            //Debug.WriteLine("      To : " + toAtom.ElementType + " [" + toAtom.Id + "] at " + bondEnd.X + ", " + bondEnd.Y);

            #region Create Bond Line objects

            switch (bond.Order)
            {
                case Globals.OrderZero:
                case "unknown":
                    m_BondLines.Add(new BondLine(bondStart, bondEnd, BondLineStyle.Dotted, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path));
                    break;

                case Globals.OrderPartial01:
                    m_BondLines.Add(new BondLine(bondStart, bondEnd, BondLineStyle.Dashed, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path));
                    break;

                case "1":
                case Globals.OrderSingle:
                    switch (bond.Stereo)
                    {
                        case Globals.BondStereo.None:
                            m_BondLines.Add(new BondLine(bondStart, bondEnd, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path));
                            break;

                        case Globals.BondStereo.Hatch:
                            m_BondLines.Add(new BondLine(bondStart, bondEnd, BondLineStyle.Hatch, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path));
                            break;

                        case Globals.BondStereo.Wedge:
                            m_BondLines.Add(new BondLine(bondStart, bondEnd, BondLineStyle.Wedge, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path));
                            break;

                        case Globals.BondStereo.Indeterminate:
                            m_BondLines.Add(new BondLine(bondStart, bondEnd, BondLineStyle.Wavy, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path));
                            break;

                        default:

                            m_BondLines.Add(new BondLine(bondStart, bondEnd, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path));
                            break;
                    }
                    break;

                case Globals.OrderPartial12:
                case Globals.OrderAromatic:
                    BondLine onePointFive;
                    BondLine onePointFiveDotted;
                    Point onePointFiveStart;
                    Point onePointFiveEnd;
                    switch (bond.Placement)
                    {
                        case Globals.BondDirection.Clockwise:
                            onePointFive = new BondLine(bondStart, bondEnd, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path);
                            m_BondLines.Add(onePointFive);
                            onePointFiveDotted = onePointFive.GetParallel(BondOffset());
                            onePointFiveStart = new Point(onePointFiveDotted.Start.X, onePointFiveDotted.Start.Y);
                            onePointFiveEnd = new Point(onePointFiveDotted.End.X, onePointFiveDotted.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref onePointFiveStart, ref onePointFiveEnd, -(BondOffset() / 1.75));
                            onePointFiveDotted = new BondLine(onePointFiveStart, onePointFiveEnd, BondLineStyle.Dotted, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path);
                            m_BondLines.Add(onePointFiveDotted);
                            break;
                        case Globals.BondDirection.Anticlockwise:
                            onePointFive = new BondLine(bondStart, bondEnd, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path);
                            m_BondLines.Add(onePointFive);
                            onePointFiveDotted = onePointFive.GetParallel(-BondOffset());
                            onePointFiveStart = new Point(onePointFiveDotted.Start.X, onePointFiveDotted.Start.Y);
                            onePointFiveEnd = new Point(onePointFiveDotted.End.X, onePointFiveDotted.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref onePointFiveStart, ref onePointFiveEnd, -(BondOffset() / 1.75));
                            onePointFiveDotted = new BondLine(onePointFiveStart, onePointFiveEnd, BondLineStyle.Dotted, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path);
                            m_BondLines.Add(onePointFiveDotted);
                            break;
                        case Globals.BondDirection.None:
                            onePointFive = new BondLine(bondStart, bondEnd, BondLineStyle.Solid, bond.Id, bond.Parent.Id, bond.StartAtom.Path, bond.EndAtom.Path);
                            m_BondLines.Add(onePointFive.GetParallel(-(BondOffset() / 2)));
                            onePointFiveDotted = onePointFive.GetParallel(BondOffset() / 2);
                            onePointFiveDotted.SetLineStyle(BondLineStyle.Dotted);
                            m_BondLines.Add(onePointFiveDotted);
                            break;
                    }
                    break;

                case "2":
                case Globals.OrderDouble:
                    if (bond.Stereo == Globals.BondStereo.Indeterminate) //crossing bonds
                    {
                        // Crossed lines
                        BondLine d = new BondLine(bondStart, bondEnd, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path);
                        BondLine d1 = d.GetParallel(-(BondOffset() / 2));
                        BondLine d2 = d.GetParallel(BondOffset() / 2);
                        m_BondLines.Add(new BondLine(new Point(d1.Start.X, d1.Start.Y), new Point(d2.End.X, d2.End.Y), BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path));
                        m_BondLines.Add(new BondLine(new Point(d2.Start.X, d2.Start.Y), new Point(d1.End.X, d1.End.Y), BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path));
                    }
                    else
                    {
                        bool shifted = false;
                        if (ringCount == 0)
                        {
                            if (bond.StartAtom.Element as Element == Globals.PeriodicTable.C && bond.EndAtom.Element as Element == Globals.PeriodicTable.C)
                            {
                                shifted = false;
                            }
                            else
                            {
                                shifted = true;
                            }
                        }

                        if (bond.StartAtom.Degree == 1 | bond.EndAtom.Degree == 1)
                        {
                            shifted = true;
                        }

                        if (shifted)
                        {
                            BondLine d = new BondLine(bondStart, bondEnd, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path);
                            m_BondLines.Add(d.GetParallel(-(BondOffset() / 2)));
                            m_BondLines.Add(d.GetParallel(BondOffset() / 2));
                        }
                        else
                        {
                            Debug.WriteLine($"bond.Placement {bond.Placement}");
                            Point outIntersectP1;
                            Point outIntersectP2;
                            bool linesIntersect;
                            bool segmentsIntersect;
                            Point centre;

                            switch (bond.Placement)
                            {
                                case Globals.BondDirection.Anticlockwise:
                                    BondLine da = new BondLine(bondStart, bondEnd, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path);
                                    m_BondLines.Add(da);

                                    BondLine bla = da.GetParallel(-BondOffset());
                                    Point startPointa = bla.Start;
                                    Point endPointa = bla.End;

                                    if (bond.PrimaryRing != null)
                                    {
                                        centre = bond.PrimaryRing.Centroid.Value;
                                        // Diagnostics
                                        //m_BondLines.Add(new BondLine(bondStart, centre, BondLineStyle.Dotted, null));
                                        //m_BondLines.Add(new BondLine(bondEnd, centre, BondLineStyle.Dotted, null));

                                        CoordinateTool.FindIntersection(startPointa, endPointa, bondStart, centre,
                                            out linesIntersect, out segmentsIntersect, out outIntersectP1);
                                        CoordinateTool.FindIntersection(startPointa, endPointa, bondEnd, centre,
                                            out linesIntersect, out segmentsIntersect, out outIntersectP2);

                                        m_BondLines.Add(new BondLine(outIntersectP1, outIntersectP2, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path));
                                    }
                                    else
                                    {
                                        CoordinateTool.AdjustLineAboutMidpoint(ref startPointa, ref endPointa, -(BondOffset() / 1.75));
                                        m_BondLines.Add(new BondLine(startPointa, endPointa, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path));
                                    }
                                    break;

                                case Globals.BondDirection.Clockwise:
                                    BondLine dc = new BondLine(bondStart, bondEnd, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path);
                                    m_BondLines.Add(dc);

                                    BondLine blc = dc.GetParallel(BondOffset());
                                    Point startPointc = blc.Start;
                                    Point endPointc = blc.End;

                                    if (bond.PrimaryRing != null)
                                    {
                                        centre = bond.PrimaryRing.Centroid.Value;
                                        // Diagnostics
                                        //m_BondLines.Add(new BondLine(bondStart, centre, BondLineStyle.Dotted, null));
                                        //m_BondLines.Add(new BondLine(bondEnd, centre, BondLineStyle.Dotted, null));

                                        CoordinateTool.FindIntersection(startPointc, endPointc, bondStart, centre,
                                            out linesIntersect, out segmentsIntersect, out outIntersectP1);
                                        CoordinateTool.FindIntersection(startPointc, endPointc, bondEnd, centre,
                                            out linesIntersect, out segmentsIntersect, out outIntersectP2);

                                        m_BondLines.Add(new BondLine(outIntersectP1, outIntersectP2, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path));
                                    }
                                    else
                                    {
                                        CoordinateTool.AdjustLineAboutMidpoint(ref startPointc, ref endPointc, -(BondOffset() / 1.75));
                                        m_BondLines.Add(new BondLine(startPointc, endPointc, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path));
                                    }
                                    break;

                                default:
                                    switch (bond.Stereo)
                                    {
                                        case Globals.BondStereo.Cis:
                                            BondLine dcc = new BondLine(bondStart, bondEnd, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path);
                                            m_BondLines.Add(dcc);
                                            BondLine blnewc = dcc.GetParallel(BondOffset());
                                            Point startPointn = blnewc.Start;
                                            Point endPointn = blnewc.End;
                                            CoordinateTool.AdjustLineAboutMidpoint(ref startPointn, ref endPointn, -(BondOffset() / 1.75));
                                            m_BondLines.Add(new BondLine(startPointn, endPointn, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path));
                                            break;

                                        case Globals.BondStereo.Trans:
                                            BondLine dtt = new BondLine(bondStart, bondEnd, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path);
                                            m_BondLines.Add(dtt);
                                            BondLine blnewt = dtt.GetParallel(BondOffset());
                                            Point startPointt = blnewt.Start;
                                            Point endPointt = blnewt.End;
                                            CoordinateTool.AdjustLineAboutMidpoint(ref startPointt, ref endPointt, -(BondOffset() / 1.75));
                                            m_BondLines.Add(new BondLine(startPointt, endPointt, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path));
                                            break;

                                        default:
                                            BondLine dp = new BondLine(bondStart, bondEnd, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path);
                                            m_BondLines.Add(dp.GetParallel(-(BondOffset() / 2)));
                                            m_BondLines.Add(dp.GetParallel(BondOffset() / 2));
                                            break;
                                    }
                                    break;
                            }
                        }
                    }
                    break;

                case Globals.OrderPartial23:
                    BondLine twoPointFive;
                    BondLine twoPointFiveDotted;
                    BondLine twoPointFiveParallel;
                    Point twoPointFiveStart;
                    Point twoPointFiveEnd;
                    switch (bond.Placement)
                    {
                        case Globals.BondDirection.Clockwise:
                            // Central bond line
                            twoPointFive = new BondLine(bondStart, bondEnd, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path);
                            m_BondLines.Add(twoPointFive);
                            // Solid bond line
                            twoPointFiveParallel = twoPointFive.GetParallel(-BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveParallel.Start.X, twoPointFiveParallel.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveParallel.End.X, twoPointFiveParallel.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / 1.75));
                            twoPointFiveParallel = new BondLine(twoPointFiveStart, twoPointFiveEnd, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path);
                            m_BondLines.Add(twoPointFiveParallel);
                            // Dotted bond line
                            twoPointFiveDotted = twoPointFive.GetParallel(BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveDotted.Start.X, twoPointFiveDotted.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveDotted.End.X, twoPointFiveDotted.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / 1.75));
                            twoPointFiveDotted = new BondLine(twoPointFiveStart, twoPointFiveEnd, BondLineStyle.Dotted, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path);
                            m_BondLines.Add(twoPointFiveDotted);
                            break;
                        case Globals.BondDirection.Anticlockwise:
                            // Central bond line
                            twoPointFive = new BondLine(bondStart, bondEnd, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path);
                            m_BondLines.Add(twoPointFive);
                            // Dotted bond line
                            twoPointFiveDotted = twoPointFive.GetParallel(-BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveDotted.Start.X, twoPointFiveDotted.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveDotted.End.X, twoPointFiveDotted.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / 1.75));
                            twoPointFiveDotted = new BondLine(twoPointFiveStart, twoPointFiveEnd, BondLineStyle.Dotted, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path);
                            m_BondLines.Add(twoPointFiveDotted);
                            // Solid bond line
                            twoPointFiveParallel = twoPointFive.GetParallel(BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveParallel.Start.X, twoPointFiveParallel.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveParallel.End.X, twoPointFiveParallel.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / 1.75));
                            twoPointFiveParallel = new BondLine(twoPointFiveStart, twoPointFiveEnd, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path);
                            m_BondLines.Add(twoPointFiveParallel);
                            break;
                        case Globals.BondDirection.None:
                            twoPointFive = new BondLine(bondStart, bondEnd, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path);
                            m_BondLines.Add(twoPointFive);
                            m_BondLines.Add(twoPointFive.GetParallel(-BondOffset()));
                            twoPointFiveDotted = twoPointFive.GetParallel(BondOffset());
                            twoPointFiveDotted.SetLineStyle(BondLineStyle.Dotted);
                            m_BondLines.Add(twoPointFiveDotted);
                            break;
                    }
                    break;

                case "3":
                case Globals.OrderTriple:
                    BondLine tripple = new BondLine(bondStart, bondEnd, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path);
                    m_BondLines.Add(tripple);
                    m_BondLines.Add(tripple.GetParallel(BondOffset()));
                    m_BondLines.Add(tripple.GetParallel(-BondOffset()));
                    break;


                default:
                    // Draw a single line, so that there is something to see
                    m_BondLines.Add(new BondLine(bondStart, bondEnd, BondLineStyle.Solid, bond.Path, bond.Parent.Path, bond.StartAtom.Path, bond.EndAtom.Path));
                    break;
            }

            #endregion Create Bond Line objects
        }

        private double BondOffset()
        {
            return (m_medianBondLength * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE);
        }

        private double BondOffsetPercent(Vector v)
        {
            double standardOffset = m_medianBondLength * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE;
            double ratio = standardOffset / v.Length;
            return ratio;
        }
    }
}