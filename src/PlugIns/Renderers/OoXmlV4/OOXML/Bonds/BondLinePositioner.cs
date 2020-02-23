// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using Chem4Word.Renderer.OoXmlV4.Entities;
using Chem4Word.Renderer.OoXmlV4.Enums;

namespace Chem4Word.Renderer.OoXmlV4.OOXML.Bonds
{
    public class BondLinePositioner
    {
        private List<BondLine> _BondLines;
        private double _medianBondLength;
        private OoXmlV4Options _options;

        public BondLinePositioner(List<BondLine> bondLines, OoXmlV4Options opts, double medianBondLength)
        {
            _BondLines = bondLines;
            _medianBondLength = medianBondLength;
            _options = opts;
        }

        /// <summary>
        /// Creates the lines for a bond
        /// </summary>
        /// <param name="bond"></param>
        public void CreateLines(Bond bond)
        {
            IEnumerable<Ring> rings = bond.Rings;
            int ringCount = 0;
            foreach (Ring r in rings)
            {
                ringCount++;
            }

            Point bondStart = bond.StartAtom.Position;

            Point bondEnd = bond.EndAtom.Position;

            #region Create Bond Line objects

            switch (bond.Order)
            {
                case Globals.OrderZero:
                case "unknown":
                    _BondLines.Add(new BondLine(BondLineStyle.Dotted, bond));
                    break;

                case Globals.OrderPartial01:
                    _BondLines.Add(new BondLine(BondLineStyle.Dashed, bond));
                    break;

                case "1":
                case Globals.OrderSingle:
                    switch (bond.Stereo)
                    {
                        case Globals.BondStereo.None:
                            _BondLines.Add(new BondLine(BondLineStyle.Solid, bond));
                            break;

                        case Globals.BondStereo.Hatch:
                            _BondLines.Add(new BondLine(BondLineStyle.Hatch, bond));
                            break;

                        case Globals.BondStereo.Wedge:
                            _BondLines.Add(new BondLine(BondLineStyle.Wedge, bond));
                            break;

                        case Globals.BondStereo.Indeterminate:
                            _BondLines.Add(new BondLine(BondLineStyle.Wavy, bond));
                            break;

                        default:

                            _BondLines.Add(new BondLine(BondLineStyle.Solid, bond));
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
                            onePointFive = new BondLine(BondLineStyle.Solid, bond);
                            _BondLines.Add(onePointFive);
                            onePointFiveDotted = onePointFive.GetParallel(BondOffset());
                            onePointFiveStart = new Point(onePointFiveDotted.Start.X, onePointFiveDotted.Start.Y);
                            onePointFiveEnd = new Point(onePointFiveDotted.End.X, onePointFiveDotted.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref onePointFiveStart, ref onePointFiveEnd, -(BondOffset() / 1.75));
                            onePointFiveDotted = new BondLine(BondLineStyle.Dotted, onePointFiveStart, onePointFiveEnd, bond);
                            _BondLines.Add(onePointFiveDotted);
                            break;

                        case Globals.BondDirection.Anticlockwise:
                            onePointFive = new BondLine(BondLineStyle.Solid, bond);
                            _BondLines.Add(onePointFive);
                            onePointFiveDotted = onePointFive.GetParallel(-BondOffset());
                            onePointFiveStart = new Point(onePointFiveDotted.Start.X, onePointFiveDotted.Start.Y);
                            onePointFiveEnd = new Point(onePointFiveDotted.End.X, onePointFiveDotted.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref onePointFiveStart, ref onePointFiveEnd, -(BondOffset() / 1.75));
                            onePointFiveDotted = new BondLine(BondLineStyle.Dotted, onePointFiveStart, onePointFiveEnd, bond);
                            _BondLines.Add(onePointFiveDotted);
                            break;

                        case Globals.BondDirection.None:
                            onePointFive = new BondLine(BondLineStyle.Solid, bond);
                            _BondLines.Add(onePointFive.GetParallel(-(BondOffset() / 2)));
                            onePointFiveDotted = onePointFive.GetParallel(BondOffset() / 2);
                            onePointFiveDotted.SetLineStyle(BondLineStyle.Dotted);
                            _BondLines.Add(onePointFiveDotted);
                            break;
                    }
                    break;

                case "2":
                case Globals.OrderDouble:
                    if (bond.Stereo == Globals.BondStereo.Indeterminate) //crossing bonds
                    {
                        // Crossed lines
                        BondLine d = new BondLine(BondLineStyle.Solid, bondStart, bondEnd, bond);
                        BondLine d1 = d.GetParallel(-(BondOffset() / 2));
                        BondLine d2 = d.GetParallel(BondOffset() / 2);
                        _BondLines.Add(new BondLine(BondLineStyle.Solid, new Point(d1.Start.X, d1.Start.Y), new Point(d2.End.X, d2.End.Y), bond));
                        _BondLines.Add(new BondLine(BondLineStyle.Solid, new Point(d2.Start.X, d2.Start.Y), new Point(d1.End.X, d1.End.Y), bond));
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

                        if (bond.StartAtom.Degree == 1 || bond.EndAtom.Degree == 1)
                        {
                            shifted = true;
                        }

                        if (shifted)
                        {
                            BondLine d = new BondLine(BondLineStyle.Solid, bond);
                            _BondLines.Add(d.GetParallel(-(BondOffset() / 2)));
                            _BondLines.Add(d.GetParallel(BondOffset() / 2));
                        }
                        else
                        {
                            Point outIntersectP1;
                            Point outIntersectP2;
                            bool linesIntersect;
                            bool segmentsIntersect;
                            Point centre;

                            switch (bond.Placement)
                            {
                                case Globals.BondDirection.Anticlockwise:
                                    BondLine da = new BondLine(BondLineStyle.Solid, bond);
                                    _BondLines.Add(da);

                                    BondLine bla = da.GetParallel(-BondOffset());
                                    Point startPointa = bla.Start;
                                    Point endPointa = bla.End;

                                    if (bond.PrimaryRing != null)
                                    {
                                        centre = bond.PrimaryRing.Centroid.Value;

                                        if (_options.ShowBondClippingLines)
                                        {
                                            // Diagnostics
                                            _BondLines.Add(new BondLine(BondLineStyle.Dotted, bond.StartAtom.Position, centre));
                                            _BondLines.Add(new BondLine(BondLineStyle.Dotted, bond.EndAtom.Position, centre));
                                        }

                                        CoordinateTool.FindIntersection(startPointa, endPointa, bondStart, centre,
                                            out linesIntersect, out segmentsIntersect, out outIntersectP1);
                                        CoordinateTool.FindIntersection(startPointa, endPointa, bondEnd, centre,
                                            out linesIntersect, out segmentsIntersect, out outIntersectP2);

                                        _BondLines.Add(new BondLine(BondLineStyle.Solid, outIntersectP1, outIntersectP2, bond));
                                    }
                                    else
                                    {
                                        CoordinateTool.AdjustLineAboutMidpoint(ref startPointa, ref endPointa, -(BondOffset() / 1.75));
                                        _BondLines.Add(new BondLine(BondLineStyle.Solid, startPointa, endPointa, bond));
                                    }
                                    break;

                                case Globals.BondDirection.Clockwise:
                                    BondLine dc = new BondLine(BondLineStyle.Solid, bond);
                                    _BondLines.Add(dc);

                                    BondLine blc = dc.GetParallel(BondOffset());
                                    Point startPointc = blc.Start;
                                    Point endPointc = blc.End;

                                    if (bond.PrimaryRing != null)
                                    {
                                        centre = bond.PrimaryRing.Centroid.Value;
                                        if (_options.ShowBondClippingLines)
                                        {
                                            // Diagnostics
                                            _BondLines.Add(new BondLine(BondLineStyle.Dotted, bond.StartAtom.Position, centre));
                                            _BondLines.Add(new BondLine(BondLineStyle.Dotted, bond.EndAtom.Position, centre));
                                        }

                                        CoordinateTool.FindIntersection(startPointc, endPointc, bondStart, centre,
                                            out linesIntersect, out segmentsIntersect, out outIntersectP1);
                                        CoordinateTool.FindIntersection(startPointc, endPointc, bondEnd, centre,
                                            out linesIntersect, out segmentsIntersect, out outIntersectP2);

                                        _BondLines.Add(new BondLine(BondLineStyle.Solid, outIntersectP1, outIntersectP2, bond));
                                    }
                                    else
                                    {
                                        CoordinateTool.AdjustLineAboutMidpoint(ref startPointc, ref endPointc, -(BondOffset() / 1.75));
                                        _BondLines.Add(new BondLine(BondLineStyle.Solid, startPointc, endPointc, bond));
                                    }
                                    break;

                                default:
                                    switch (bond.Stereo)
                                    {
                                        case Globals.BondStereo.Cis:
                                            BondLine dcc = new BondLine(BondLineStyle.Solid, bond);
                                            _BondLines.Add(dcc);
                                            BondLine blnewc = dcc.GetParallel(BondOffset());
                                            Point startPointn = blnewc.Start;
                                            Point endPointn = blnewc.End;
                                            CoordinateTool.AdjustLineAboutMidpoint(ref startPointn, ref endPointn, -(BondOffset() / 1.75));
                                            _BondLines.Add(new BondLine(BondLineStyle.Solid, startPointn, endPointn, bond));
                                            break;

                                        case Globals.BondStereo.Trans:
                                            BondLine dtt = new BondLine(BondLineStyle.Solid, bond);
                                            _BondLines.Add(dtt);
                                            BondLine blnewt = dtt.GetParallel(BondOffset());
                                            Point startPointt = blnewt.Start;
                                            Point endPointt = blnewt.End;
                                            CoordinateTool.AdjustLineAboutMidpoint(ref startPointt, ref endPointt, -(BondOffset() / 1.75));
                                            _BondLines.Add(new BondLine(BondLineStyle.Solid, startPointt, endPointt, bond));
                                            break;

                                        default:
                                            BondLine dp = new BondLine(BondLineStyle.Solid, bond);
                                            _BondLines.Add(dp.GetParallel(-(BondOffset() / 2)));
                                            _BondLines.Add(dp.GetParallel(BondOffset() / 2));
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
                            twoPointFive = new BondLine(BondLineStyle.Solid, bond);
                            _BondLines.Add(twoPointFive);
                            // Solid bond line
                            twoPointFiveParallel = twoPointFive.GetParallel(-BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveParallel.Start.X, twoPointFiveParallel.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveParallel.End.X, twoPointFiveParallel.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / 1.75));
                            twoPointFiveParallel = new BondLine(BondLineStyle.Solid, twoPointFiveStart, twoPointFiveEnd, bond);
                            _BondLines.Add(twoPointFiveParallel);
                            // Dotted bond line
                            twoPointFiveDotted = twoPointFive.GetParallel(BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveDotted.Start.X, twoPointFiveDotted.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveDotted.End.X, twoPointFiveDotted.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / 1.75));
                            twoPointFiveDotted = new BondLine(BondLineStyle.Dotted, twoPointFiveStart, twoPointFiveEnd, bond);
                            _BondLines.Add(twoPointFiveDotted);
                            break;

                        case Globals.BondDirection.Anticlockwise:
                            // Central bond line
                            twoPointFive = new BondLine(BondLineStyle.Solid, bond);
                            _BondLines.Add(twoPointFive);
                            // Dotted bond line
                            twoPointFiveDotted = twoPointFive.GetParallel(-BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveDotted.Start.X, twoPointFiveDotted.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveDotted.End.X, twoPointFiveDotted.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / 1.75));
                            twoPointFiveDotted = new BondLine(BondLineStyle.Dotted, twoPointFiveStart, twoPointFiveEnd, bond);
                            _BondLines.Add(twoPointFiveDotted);
                            // Solid bond line
                            twoPointFiveParallel = twoPointFive.GetParallel(BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveParallel.Start.X, twoPointFiveParallel.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveParallel.End.X, twoPointFiveParallel.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / 1.75));
                            twoPointFiveParallel = new BondLine(BondLineStyle.Solid, twoPointFiveStart, twoPointFiveEnd, bond);
                            _BondLines.Add(twoPointFiveParallel);
                            break;

                        case Globals.BondDirection.None:
                            twoPointFive = new BondLine(BondLineStyle.Solid, bond);
                            _BondLines.Add(twoPointFive);
                            _BondLines.Add(twoPointFive.GetParallel(-BondOffset()));
                            twoPointFiveDotted = twoPointFive.GetParallel(BondOffset());
                            twoPointFiveDotted.SetLineStyle(BondLineStyle.Dotted);
                            _BondLines.Add(twoPointFiveDotted);
                            break;
                    }
                    break;

                case "3":
                case Globals.OrderTriple:
                    BondLine tripple = new BondLine(BondLineStyle.Solid, bond);
                    _BondLines.Add(tripple);
                    _BondLines.Add(tripple.GetParallel(BondOffset()));
                    _BondLines.Add(tripple.GetParallel(-BondOffset()));
                    break;

                default:
                    // Draw a single line, so that there is something to see
                    _BondLines.Add(new BondLine(BondLineStyle.Solid, bond));
                    break;
            }

            #endregion Create Bond Line objects
        }

        private double BondOffset()
        {
            return (_medianBondLength * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE);
        }
    }
}