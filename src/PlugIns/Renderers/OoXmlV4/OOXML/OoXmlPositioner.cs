// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;
using Chem4Word.Renderer.OoXmlV4.Entities;
using Chem4Word.Renderer.OoXmlV4.Enums;
using Chem4Word.Renderer.OoXmlV4.TTF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using Point = System.Windows.Point;

namespace Chem4Word.Renderer.OoXmlV4.OOXML
{
    public class OoXmlPositioner
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private PositionerInputs Inputs { get; }
        private PositionerOutputs Outputs { get; } = new PositionerOutputs();

        private const double EPSILON = 1e-4;

        public OoXmlPositioner(PositionerInputs inputs) => Inputs = inputs;

        /// <summary>
        /// Carries out the following
        /// 1. Position Atom Label Characters
        /// 2. Position Bond Lines
        /// 3. Position Brackets
        /// 4. Position Molecule Label Characters
        /// 5. Shrink Bond Lines
        /// </summary>
        /// <returns>PositionerOutputs a class to hold all of the required output types</returns>
        public PositionerOutputs Position()
        {
            int moleculeNo = 0;
            foreach (Molecule mol in Inputs.Model.Molecules.Values)
            {
                // Steps 1 .. 4
                ProcessMolecule(mol, Inputs.Progress, ref moleculeNo);
            }

            // 5.   Shrink Bond Lines
            if (Inputs.Options.ClipLines)
            {
                #region Step 4 - Shrink bond lines

                ShrinkBondLinesPass1(Inputs.Progress);
                ShrinkBondLinesPass2(Inputs.Progress);

                #endregion Step 4 - Shrink bond lines
            }

            return Outputs;
        }

        private void ShrinkBondLinesPass1(Progress pb)
        {
            // so that they do not overlap label characters

            if (Outputs.ConvexHulls.Count > 1)
            {
                pb.Show();
            }
            pb.Message = "Clipping Bond Lines - Pass 1";
            pb.Value = 0;
            pb.Maximum = Outputs.ConvexHulls.Count;

            foreach (var hull in Outputs.ConvexHulls)
            {
                pb.Increment(1);

                // select lines which start or end with this atom
                var targeted = from l in Outputs.BondLines
                               where (l.StartAtomPath == hull.Key || l.EndAtomPath == hull.Key)
                               select l;

                foreach (BondLine bl in targeted.ToList())
                {
                    Point start = new Point(bl.Start.X, bl.Start.Y);
                    Point end = new Point(bl.End.X, bl.End.Y);

                    bool outside;
                    var r = GeometryTool.ClipLineWithPolygon(start, end, hull.Value, out outside);

                    switch (r.Length)
                    {
                        case 3:
                            if (outside)
                            {
                                bl.Start = new Point(r[0].X, r[0].Y);
                                bl.End = new Point(r[1].X, r[1].Y);
                            }
                            else
                            {
                                bl.Start = new Point(r[1].X, r[1].Y);
                                bl.End = new Point(r[2].X, r[2].Y);
                            }
                            break;

                        case 2:
                            if (!outside)
                            {
                                // This line is totally inside so remove it!
                                Outputs.BondLines.Remove(bl);
                            }
                            break;
                    }
                }
            }
        }

        private void ShrinkBondLinesPass2(Progress pb)
        {
            // so that they do not overlap label characters

            if (Outputs.AtomLabelCharacters.Count > 1)
            {
                pb.Show();
            }
            pb.Message = "Clipping Bond Lines - Pass 2";
            pb.Value = 0;
            pb.Maximum = Outputs.AtomLabelCharacters.Count;

            foreach (AtomLabelCharacter alc in Outputs.AtomLabelCharacters)
            {
                pb.Increment(1);

                double width = OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, Inputs.MeanBondLength);
                double height = OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, Inputs.MeanBondLength);

                if (alc.IsSubScript)
                {
                    // Shrink bounding box
                    width = width * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR;
                    height = height * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR;
                }

                // Create rectangle of the bounding box with a suitable clipping margin
                Rect cbb = new Rect(alc.Position.X - OoXmlHelper.CHARACTER_CLIPPING_MARGIN,
                    alc.Position.Y - OoXmlHelper.CHARACTER_CLIPPING_MARGIN,
                    width + (OoXmlHelper.CHARACTER_CLIPPING_MARGIN * 2),
                    height + (OoXmlHelper.CHARACTER_CLIPPING_MARGIN * 2));

                // Just in case we end up splitting a line into two
                List<BondLine> extraBondLines = new List<BondLine>();

                // Select Lines which may require trimming
                // By using LINQ to implement the following SQL
                // Where (L.Right Between Cbb.Left And Cbb.Right)
                //    Or (L.Left Between Cbb.Left And Cbb.Right)
                //    Or (L.Top Between Cbb.Top And Cbb.Botton)
                //    Or (L.Bottom Between Cbb.Top And Cbb.Botton)

                var targeted = from l in Outputs.BondLines
                               where (cbb.Left <= l.BoundingBox.Right && l.BoundingBox.Right <= cbb.Right)
                                     || (cbb.Left <= l.BoundingBox.Left && l.BoundingBox.Left <= cbb.Right)
                                     || (cbb.Top <= l.BoundingBox.Top && l.BoundingBox.Top <= cbb.Bottom)
                                     || (cbb.Top <= l.BoundingBox.Bottom && l.BoundingBox.Bottom <= cbb.Bottom)
                               select l;
                targeted = targeted.ToList();

                foreach (BondLine bl in targeted)
                {
                    Point start = new Point(bl.Start.X, bl.Start.Y);
                    Point end = new Point(bl.End.X, bl.End.Y);

                    int attempts = 0;
                    if (CohenSutherland.ClipLine(cbb, ref start, ref end, out attempts))
                    {
                        bool bClipped = false;

                        if (Math.Abs(bl.Start.X - start.X) < EPSILON && Math.Abs(bl.Start.Y - start.Y) < EPSILON)
                        {
                            bl.Start = new Point(end.X, end.Y);
                            bClipped = true;
                        }
                        if (Math.Abs(bl.End.X - end.X) < EPSILON && Math.Abs(bl.End.Y - end.Y) < EPSILON)
                        {
                            bl.End = new Point(start.X, start.Y);
                            bClipped = true;
                        }

                        if (!bClipped && bl.Bond != null)
                        {
                            // Only convert to two bond lines if not wedge or hatch
                            bool ignoreWedgeOrHatch = bl.Bond.Order == Globals.OrderSingle
                                                      && bl.Bond.Stereo == Globals.BondStereo.Wedge
                                                        || bl.Bond.Stereo == Globals.BondStereo.Hatch;
                            if (!ignoreWedgeOrHatch)
                            {
                                // Line was clipped at both ends
                                // 1. Generate new line
                                BondLine extraLine = new BondLine(bl.Style, new Point(end.X, end.Y), new Point(bl.End.X, bl.End.Y), bl.Bond);
                                extraBondLines.Add(extraLine);
                                // 2. Trim existing line
                                bl.End = new Point(start.X, start.Y);
                            }
                        }
                    }
                    if (attempts >= 15)
                    {
                        Debug.WriteLine("Clipping failed !");
                    }
                }

                // Add any extra lines generated by this character into the List of Bond Lines
                foreach (BondLine bl in extraBondLines)
                {
                    Outputs.BondLines.Add(bl);
                }
            }
        }

        private void ProcessMolecule(Molecule mol, Progress pb, ref int molNumber)
        {
            molNumber++;

            // 1. Position Atom Label Characters
            ProcessAtoms(mol, pb, molNumber);

            // 2. Position Bond Lines
            ProcessBonds(mol, pb, molNumber);

            // Populate diagnostic data
            foreach (Ring ring in mol.Rings)
            {
                if (ring.Centroid.HasValue)
                {
                    var centre = ring.Centroid.Value;
                    Outputs.RingCenters.Add(centre);

                    var innerCircle = new InnerCircle();
                    // Traverse() obtains list of atoms in anti-clockwise direction around ring
                    innerCircle.Points.AddRange(ring.Traverse().Select(a => a.Position).ToList());
                    innerCircle.Centre = centre;
                    //Outputs.InnerCircles.Add(innerCircle);
                }
            }

            // Recurse into any child molecules
            foreach (var child in mol.Molecules.Values)
            {
                ProcessMolecule(child, pb, ref molNumber);
            }

            // 3 Determine Extents

            // Atoms <= InternalCharacters <= GroupBrackets <= MoleculesBrackets <= ExternalCharacters

            // 3.1. Atoms & InternalCharacters
            var thisMoleculeExtents = new MoleculeExtents(mol.Path, mol.BoundingBox);
            thisMoleculeExtents.SetInternalCharacterExtents(CharacterExtents(mol, thisMoleculeExtents.AtomExtents));
            Outputs.AllMoleculeExtents.Add(thisMoleculeExtents);

            // 3.2. Grouped Molecules
            if (mol.IsGrouped)
            {
                Rect boundingBox = Rect.Empty;

                var childGroups = Outputs.AllMoleculeExtents.Where(g => g.Path.StartsWith($"{mol.Path}/")).ToList();
                foreach (var child in childGroups)
                {
                    boundingBox.Union(child.ExternalCharacterExtents);
                }

                if (boundingBox != Rect.Empty)
                {
                    boundingBox.Union(thisMoleculeExtents.ExternalCharacterExtents);
                    if (Inputs.Options.ShowMoleculeGrouping)
                    {
                        boundingBox = Inflate(boundingBox, OoXmlHelper.BracketOffset(Inputs.MeanBondLength));
                        Outputs.GroupBrackets.Add(boundingBox);
                    }
                    thisMoleculeExtents.SetGroupBracketExtents(boundingBox);
                }
            }

            // 3.3 Add required Brackets
            bool showBrackets = mol.ShowMoleculeBrackets.HasValue && mol.ShowMoleculeBrackets.Value
                                || mol.Count.HasValue && mol.Count.Value > 0
                                || mol.FormalCharge.HasValue && mol.FormalCharge.Value != 0
                                || mol.SpinMultiplicity.HasValue && mol.SpinMultiplicity.Value > 1;

            var rect = thisMoleculeExtents.GroupBracketsExtents;
            var children = Outputs.AllMoleculeExtents.Where(g => g.Path.StartsWith($"{mol.Path}/")).ToList();
            foreach (var child in children)
            {
                rect.Union(child.GroupBracketsExtents);
            }

            if (showBrackets)
            {
                rect = Inflate(rect, OoXmlHelper.BracketOffset(Inputs.MeanBondLength));
                Outputs.MoleculeBrackets.Add(rect);
            }
            thisMoleculeExtents.SetMoleculeBracketExtents(rect);

            TtfCharacter hydrogenCharacter = Inputs.TtfCharacterSet['H'];

            string characters = string.Empty;

            if (mol.FormalCharge.HasValue && mol.FormalCharge.Value != 0)
            {
                // Add FormalCharge at top right
                int charge = mol.FormalCharge.Value;
                int absCharge = Math.Abs(charge);

                if (absCharge > 1)
                {
                    characters = absCharge.ToString();
                }

                if (charge >= 1)
                {
                    characters += "+";
                }
                else if (charge <= 1)
                {
                    characters += "-";
                }
            }

            if (mol.SpinMultiplicity.HasValue && mol.SpinMultiplicity.Value > 1)
            {
                // Append SpinMultiplicity
                switch (mol.SpinMultiplicity.Value)
                {
                    case 2:
                        characters += "•";
                        break;

                    case 3:
                        characters += "••";
                        break;
                }
            }

            if (!string.IsNullOrEmpty(characters))
            {
                // Draw characters at top right (outside of any brackets)
                var point = new Point(thisMoleculeExtents.MoleculeBracketsExtents.Right
                                      + OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE * Inputs.MeanBondLength,
                                      thisMoleculeExtents.MoleculeBracketsExtents.Top
                                      + OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Height, Inputs.MeanBondLength) / 2);
                PlaceString(characters, point, mol.Path);
            }

            if (mol.Count.HasValue && mol.Count.Value > 0)
            {
                // Draw Count at bottom right
                var point = new Point(thisMoleculeExtents.MoleculeBracketsExtents.Right
                                      + OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE * Inputs.MeanBondLength,
                                      thisMoleculeExtents.MoleculeBracketsExtents.Bottom
                                      + OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Height, Inputs.MeanBondLength) / 2);
                PlaceString($"{mol.Count}", point, mol.Path);
            }

            if (mol.Count.HasValue
                || mol.FormalCharge.HasValue
                || mol.SpinMultiplicity.HasValue)
            {
                // Recalculate as we have just added extra characters
                thisMoleculeExtents.SetExternalCharacterExtents(CharacterExtents(mol, thisMoleculeExtents.MoleculeBracketsExtents));
            }

            // 4. Position Molecule Label Characters
            // Handle optional rendering of molecule labels centered on brackets (if any) and below any molecule property characters
            if (Inputs.Options.ShowMoleculeLabels && mol.Labels.Any())
            {
                var point = new Point(thisMoleculeExtents.MoleculeBracketsExtents.Left
                                        + thisMoleculeExtents.MoleculeBracketsExtents.Width / 2,
                                      thisMoleculeExtents.ExternalCharacterExtents.Bottom
                                        + Inputs.MeanBondLength * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE / 2);

                AddMoleculeLabels(mol.Labels.ToList(), point, mol.Path);

                // Recalculate again as we have just added extra characters
                thisMoleculeExtents.SetExternalCharacterExtents(CharacterExtents(mol, thisMoleculeExtents.MoleculeBracketsExtents));

                // MAW Keep this code as it may be possible to bring back use of OoXmlString later on
                //AddMoleculeLabelsV2(mol.Labels.ToList(), point, mol.Path);
                //var revisedExtents = thisMoleculeExtents.ExternalCharacterExtents;
                //foreach (var ooXmlString in Outputs.MoleculeLabels.Where(p => p.ParentMolecule.Equals(mol.Path)))
                //{
                //    revisedExtents.Union(ooXmlString.Extents);
                //}

                //thisMoleculeExtents.SetExternalCharacterExtents(revisedExtents);
            }
        }

        private Rect Inflate(Rect r, double x)
        {
            Rect r1 = r;
            r1.Inflate(x, x);
            return r1;
        }

        private Rect CharacterExtents(Molecule mol, Rect existing)
        {
            var chars = Outputs.AtomLabelCharacters.Where(m => m.ParentMolecule.StartsWith(mol.Path)).ToList();
            foreach (var c in chars)
            {
                if (c.IsSmaller)
                {
                    Rect r = new Rect(c.Position,
                                      new Size(OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR,
                                               OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR));
                    existing.Union(r);
                }
                else
                {
                    Rect r = new Rect(c.Position,
                                      new Size(OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength),
                                               OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength)));
                    existing.Union(r);
                }
            }

            return existing;
        }

        private void ProcessAtoms(Molecule mol, Progress pb, int moleculeNo)
        {
            // Create Characters
            if (mol.Atoms.Count > 1)
            {
                pb.Show();
            }
            pb.Message = $"Processing Atoms in Molecule {moleculeNo}";
            pb.Value = 0;
            pb.Maximum = mol.Atoms.Count;

            foreach (Atom atom in mol.Atoms.Values)
            {
                pb.Increment(1);
                if (atom.Element is Element)
                {
                    CreateElementCharacters(atom);
                }

                if (atom.Element is FunctionalGroup)
                {
                    CreateFunctionalGroupCharacters(atom);
                }
            }
        }

        private void ProcessBonds(Molecule mol, Progress pb, int moleculeNo)
        {
            if (mol.Bonds.Count > 0)
            {
                pb.Show();
            }
            pb.Message = $"Processing Bonds in Molecule {moleculeNo}";
            pb.Value = 0;
            pb.Maximum = mol.Bonds.Count;

            foreach (Bond bond in mol.Bonds)
            {
                pb.Increment(1);
                CreateLines(bond);
            }

            // Rendering molecular sketches for publication quality output
            // Alex M Clark
            // Implement beautification of semi open double bonds and double bonds touching rings

            // Obtain list of Double Bonds with Placement of BondDirection.None
            List<Bond> doubleBonds = mol.Bonds.Where(b => b.OrderValue.HasValue && b.OrderValue.Value == 2 && b.Placement == Globals.BondDirection.None).ToList();
            if (doubleBonds.Count > 0)
            {
                pb.Message = $"Processing Double Bonds in Molecule {moleculeNo}";
                pb.Value = 0;
                pb.Maximum = doubleBonds.Count;

                foreach (Bond bond in doubleBonds)
                {
                    BeautifyLines(bond.StartAtom, bond.Path);
                    BeautifyLines(bond.EndAtom, bond.Path);
                }
            }
        }

        private void BeautifyLines(Atom atom, string bondPath)
        {
            if (atom.Element is Element element
                && element == Globals.PeriodicTable.C
                && atom.Bonds.ToList().Count == 3)
            {
                bool isInRing = atom.IsInRing;
                List<BondLine> lines = Outputs.BondLines.Where(bl => bl.BondPath.Equals(bondPath)).ToList();
                if (lines.Any())
                {
                    List<Bond> otherLines;
                    if (isInRing)
                    {
                        otherLines = atom.Bonds.Where(b => !b.Path.Equals(bondPath)).ToList();
                    }
                    else
                    {
                        otherLines = atom.Bonds.Where(b => !b.Path.Equals(bondPath) && b.Order.Equals(Globals.OrderSingle)).ToList();
                    }

                    if (lines.Count == 2 && otherLines.Count == 2)
                    {
                        BondLine line1 = Outputs.BondLines.First(bl => bl.BondPath.Equals(otherLines[0].Path));
                        BondLine line2 = Outputs.BondLines.First(bl => bl.BondPath.Equals(otherLines[1].Path));
                        TrimLines(lines, line1, line2, isInRing);
                    }
                }
            }
        }

        private void TrimLines(List<BondLine> mainPair, BondLine line1, BondLine line2, bool isInRing)
        {
            // Only two of these calls are expected to do anything
            if (!TrimLine(mainPair[0], line1, isInRing))
            {
                TrimLine(mainPair[0], line2, isInRing);
            }
            // Only two of these calls are expected to do anything
            if (!TrimLine(mainPair[1], line1, isInRing))
            {
                TrimLine(mainPair[1], line2, isInRing);
            }
        }

        private bool TrimLine(BondLine leftOrRight, BondLine line, bool isInRing)
        {
            bool dummy;
            bool intersect;
            Point intersection;

            // Make a longer version of the line
            Point startLonger = new Point(leftOrRight.Start.X, leftOrRight.Start.Y);
            Point endLonger = new Point(leftOrRight.End.X, leftOrRight.End.Y);
            CoordinateTool.AdjustLineAboutMidpoint(ref startLonger, ref endLonger, Inputs.MeanBondLength / 5);

            // See if they intersect at one end
            CoordinateTool.FindIntersection(startLonger, endLonger, line.Start, line.End,
                out dummy, out intersect, out intersection);

            // If they intersect update the main line
            if (intersect)
            {
                double l1 = CoordinateTool.DistanceBetween(intersection, leftOrRight.Start);
                double l2 = CoordinateTool.DistanceBetween(intersection, leftOrRight.End);
                if (l1 > l2)
                {
                    leftOrRight.End = new Point(intersection.X, intersection.Y);
                }
                else
                {
                    leftOrRight.Start = new Point(intersection.X, intersection.Y);
                }
                if (!isInRing)
                {
                    l1 = CoordinateTool.DistanceBetween(intersection, line.Start);
                    l2 = CoordinateTool.DistanceBetween(intersection, line.End);
                    if (l1 > l2)
                    {
                        line.End = new Point(intersection.X, intersection.Y);
                    }
                    else
                    {
                        line.Start = new Point(intersection.X, intersection.Y);
                    }
                }
            }

            return intersect;
        }

        private void CreateElementCharacters(Atom atom)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            string atomLabel = atom.Element.Symbol;
            Rect labelBounds;

            // Get Charge and Isotope values for use later on
            int iCharge = atom.FormalCharge ?? 0;
            int iAbsCharge = Math.Abs(iCharge);
            int isoValue = atom.IsotopeNumber ?? 0;

            // Get Implicit Hydrogen Count for use later on
            int implicitHCount = atom.ImplicitHydrogenCount;

            Point cursorPosition = atom.Position;

            int bondCount = atom.Bonds.ToList().Count;

            if (atom.ShowSymbol || Inputs.Options.ShowCarbons)
            {
                #region Set Up Atom Colour

                string atomColour = "000000";
                if (Inputs.Options.ColouredAtoms
                    && atom.Element.Colour != null)
                {
                    atomColour = atom.Element.Colour;
                    // Strip out # as OoXml does not use it
                    atomColour = atomColour.Replace("#", "");
                }

                #endregion Set Up Atom Colour

                #region Step 1 - Measure Bounding Box for all Characters of label

                double xMin = double.MaxValue;
                double yMin = double.MaxValue;
                double xMax = double.MinValue;
                double yMax = double.MinValue;

                Point thisCharacterPosition;
                for (int idx = 0; idx < atomLabel.Length; idx++)
                {
                    char chr = atomLabel[idx];
                    TtfCharacter c = Inputs.TtfCharacterSet[chr];
                    if (c != null)
                    {
                        thisCharacterPosition = GetCharacterPosition(cursorPosition, c);

                        xMin = Math.Min(xMin, thisCharacterPosition.X);
                        yMin = Math.Min(yMin, thisCharacterPosition.Y);
                        xMax = Math.Max(xMax, thisCharacterPosition.X + OoXmlHelper.ScaleCsTtfToCml(c.Width, Inputs.MeanBondLength));
                        yMax = Math.Max(yMax, thisCharacterPosition.Y + OoXmlHelper.ScaleCsTtfToCml(c.Height, Inputs.MeanBondLength));

                        if (idx < atomLabel.Length - 1)
                        {
                            // Move to next Character position
                            cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(c.IncrementX, Inputs.MeanBondLength), 0);
                        }
                    }
                }

                #endregion Step 1 - Measure Bounding Box for all Characters of label

                #region Step 2 - Reset Cursor such that the text is centered about the atom's co-ordinates

                double width = xMax - xMin;
                double height = yMax - yMin;
                cursorPosition = new Point(atom.Position.X - width / 2, atom.Position.Y + height / 2);
                var chargeCursorPosition = new Point(cursorPosition.X, cursorPosition.Y);
                var isotopeCursorPosition = new Point(cursorPosition.X, cursorPosition.Y);
                labelBounds = new Rect(cursorPosition, new Size(width, height));

                #endregion Step 2 - Reset Cursor such that the text is centered about the atom's co-ordinates

                #region Step 3 - Place the characters

                foreach (char chr in atomLabel)
                {
                    TtfCharacter c = Inputs.TtfCharacterSet[chr];
                    if (c != null)
                    {
                        thisCharacterPosition = GetCharacterPosition(cursorPosition, c);
                        AtomLabelCharacter alc = new AtomLabelCharacter(thisCharacterPosition, c, atomColour, atom.Path, atom.Parent.Path);
                        Outputs.AtomLabelCharacters.Add(alc);

                        // Move to next Character position
                        cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(c.IncrementX, Inputs.MeanBondLength), 0);
                        chargeCursorPosition = new Point(cursorPosition.X, cursorPosition.Y);
                    }
                }

                #endregion Step 3 - Place the characters

                #region Determine NESW

                double baFromNorth = Vector.AngleBetween(BasicGeometry.ScreenNorth, atom.BalancingVector(true));
                CompassPoints nesw;

                if (bondCount == 1)
                {
                    nesw = BasicGeometry.SnapTo2EW(baFromNorth);
                }
                else
                {
                    nesw = BasicGeometry.SnapTo4NESW(baFromNorth);
                }

                #endregion Determine NESW

                #region Step 4 - Add Charge if required

                if (iCharge != 0)
                {
                    TtfCharacter hydrogenCharacter = Inputs.TtfCharacterSet['H'];

                    TtfCharacter chargeSignCharacter = null;
                    if (iCharge >= 1)
                    {
                        chargeSignCharacter = Inputs.TtfCharacterSet['+'];
                    }
                    else if (iCharge <= 1)
                    {
                        chargeSignCharacter = Inputs.TtfCharacterSet['-'];
                    }

                    if (iAbsCharge > 1)
                    {
                        string digits = iAbsCharge.ToString();
                        // Insert digits
                        foreach (char chr in digits)
                        {
                            TtfCharacter chargeValueCharacter = Inputs.TtfCharacterSet[chr];
                            thisCharacterPosition = GetCharacterPosition(chargeCursorPosition, chargeValueCharacter);

                            // Raise the superscript Character
                            thisCharacterPosition.Offset(0, -OoXmlHelper.ScaleCsTtfToCml(chargeValueCharacter.Height * OoXmlHelper.CS_SUPERSCRIPT_RAISE_FACTOR, Inputs.MeanBondLength));

                            AtomLabelCharacter alcc = new AtomLabelCharacter(thisCharacterPosition, chargeValueCharacter, atomColour, atom.Path, atom.Parent.Path);
                            alcc.IsSmaller = true;
                            alcc.IsSubScript = true;
                            Outputs.AtomLabelCharacters.Add(alcc);

                            // Move to next Character position
                            chargeCursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(chargeValueCharacter.IncrementX, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR, 0);
                            cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(chargeValueCharacter.IncrementX, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR, 0);
                        }
                    }

                    // Insert sign at raised position
                    thisCharacterPosition = GetCharacterPosition(chargeCursorPosition, chargeSignCharacter);
                    thisCharacterPosition.Offset(0, -OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Height * OoXmlHelper.CS_SUPERSCRIPT_RAISE_FACTOR, Inputs.MeanBondLength));
                    thisCharacterPosition.Offset(0, -OoXmlHelper.ScaleCsTtfToCml(chargeSignCharacter.Height / 2 * OoXmlHelper.CS_SUPERSCRIPT_RAISE_FACTOR, Inputs.MeanBondLength));

                    AtomLabelCharacter alcs = new AtomLabelCharacter(thisCharacterPosition, chargeSignCharacter, atomColour, atom.Path, atom.Parent.Path);
                    alcs.IsSmaller = true;
                    alcs.IsSubScript = true;
                    Outputs.AtomLabelCharacters.Add(alcs);

                    if (iAbsCharge != 0)
                    {
                        cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(chargeSignCharacter.IncrementX, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR, 0);
                    }
                }

                #endregion Step 4 - Add Charge if required

                #region Step 5 - Add Implicit H if required

                if (Inputs.Options.ShowHydrogens && implicitHCount > 0)
                {
                    TtfCharacter hydrogenCharacter = Inputs.TtfCharacterSet['H'];
                    string numbers = "012345";
                    TtfCharacter implicitValueCharacter = Inputs.TtfCharacterSet[numbers[implicitHCount]];

                    #region Add H

                    if (hydrogenCharacter != null)
                    {
                        switch (nesw)
                        {
                            case CompassPoints.North:
                                if (atom.Bonds.ToList().Count > 1)
                                {
                                    cursorPosition.X = labelBounds.X
                                                       + (labelBounds.Width / 2)
                                                       - (OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Width, Inputs.MeanBondLength) / 2);
                                    cursorPosition.Y = cursorPosition.Y
                                                       + OoXmlHelper.ScaleCsTtfToCml(-hydrogenCharacter.Height, Inputs.MeanBondLength)
                                                       - OoXmlHelper.CHARACTER_VERTICAL_SPACING;
                                    if (iCharge > 0
                                        && implicitHCount > 1)
                                    {
                                        cursorPosition.Offset(0,
                                                              OoXmlHelper.ScaleCsTtfToCml(
                                                                  -implicitValueCharacter.Height *
                                                                  OoXmlHelper.SUBSCRIPT_SCALE_FACTOR / 2, Inputs.MeanBondLength)
                                                              - OoXmlHelper.CHARACTER_VERTICAL_SPACING);
                                    }
                                }
                                break;

                            case CompassPoints.East:
                                // Leave as is
                                break;

                            case CompassPoints.South:
                                if (atom.Bonds.ToList().Count > 1)
                                {
                                    cursorPosition.X = labelBounds.X + (labelBounds.Width / 2)
                                                       - (OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Width, Inputs.MeanBondLength) / 2);
                                    cursorPosition.Y = cursorPosition.Y
                                                       + OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Height, Inputs.MeanBondLength)
                                                       + OoXmlHelper.CHARACTER_VERTICAL_SPACING;
                                }
                                break;

                            case CompassPoints.West:
                                if (implicitHCount == 1)
                                {
                                    if (iAbsCharge == 0)
                                    {
                                        cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(-(hydrogenCharacter.IncrementX * 2), Inputs.MeanBondLength), 0);
                                    }
                                    else
                                    {
                                        cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(-(hydrogenCharacter.IncrementX * 2 + implicitValueCharacter.IncrementX * 1.25), Inputs.MeanBondLength), 0);
                                    }
                                }
                                else
                                {
                                    if (iAbsCharge == 0)
                                    {
                                        cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(-(hydrogenCharacter.IncrementX * 2.5), Inputs.MeanBondLength), 0);
                                    }
                                    else
                                    {
                                        cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(-(hydrogenCharacter.IncrementX * 2 + implicitValueCharacter.IncrementX * 1.25), Inputs.MeanBondLength), 0);
                                    }
                                }
                                break;
                        }

                        thisCharacterPosition = GetCharacterPosition(cursorPosition, hydrogenCharacter);
                        AtomLabelCharacter alc = new AtomLabelCharacter(thisCharacterPosition, hydrogenCharacter, atomColour, atom.Path, atom.Parent.Path);
                        Outputs.AtomLabelCharacters.Add(alc);

                        // Move to next Character position
                        cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.IncrementX, Inputs.MeanBondLength), 0);

                        if (nesw == CompassPoints.East)
                        {
                            chargeCursorPosition = new Point(cursorPosition.X, cursorPosition.Y);
                        }
                        if (nesw == CompassPoints.West)
                        {
                            isotopeCursorPosition = new Point(thisCharacterPosition.X, isotopeCursorPosition.Y);
                        }
                    }

                    #endregion Add H

                    #region Add number

                    if (implicitHCount > 1
                        && implicitValueCharacter != null)
                    {
                        thisCharacterPosition = GetCharacterPosition(cursorPosition, implicitValueCharacter);

                        // Drop the subscript Character
                        thisCharacterPosition.Offset(0, OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Width * OoXmlHelper.SUBSCRIPT_DROP_FACTOR, Inputs.MeanBondLength));

                        AtomLabelCharacter alc = new AtomLabelCharacter(thisCharacterPosition, implicitValueCharacter, atomColour, atom.Path, atom.Parent.Path);
                        alc.IsSmaller = true;
                        alc.IsSubScript = true;
                        Outputs.AtomLabelCharacters.Add(alc);

                        // Move to next Character position
                        cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(implicitValueCharacter.IncrementX, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR, 0);
                    }

                    #endregion Add number
                }

                #endregion Step 5 - Add Implicit H if required

                #region Step 6 - Add IsoTope Number if required

                if (isoValue > 0)
                {
                    string digits = isoValue.ToString();

                    xMin = double.MaxValue;
                    yMin = double.MaxValue;
                    xMax = double.MinValue;
                    yMax = double.MinValue;

                    Point isoOrigin = isotopeCursorPosition;

                    // Calculate width of digits
                    foreach (char chr in digits)
                    {
                        TtfCharacter c = Inputs.TtfCharacterSet[chr];
                        thisCharacterPosition = GetCharacterPosition(isotopeCursorPosition, c);

                        // Raise the superscript Character
                        thisCharacterPosition.Offset(0, -OoXmlHelper.ScaleCsTtfToCml(c.Height * OoXmlHelper.CS_SUPERSCRIPT_RAISE_FACTOR, Inputs.MeanBondLength));

                        xMin = Math.Min(xMin, thisCharacterPosition.X);
                        yMin = Math.Min(yMin, thisCharacterPosition.Y);
                        xMax = Math.Max(xMax, thisCharacterPosition.X + OoXmlHelper.ScaleCsTtfToCml(c.Width, Inputs.MeanBondLength));
                        yMax = Math.Max(yMax, thisCharacterPosition.Y + OoXmlHelper.ScaleCsTtfToCml(c.Height, Inputs.MeanBondLength));

                        // Move to next Character position
                        isotopeCursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(c.IncrementX, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR, 0);
                    }

                    // Re-position Isotope Cursor
                    width = xMax - xMin;
                    isotopeCursorPosition = new Point(isoOrigin.X - width, isoOrigin.Y);

                    // Insert digits
                    foreach (char chr in digits)
                    {
                        TtfCharacter c = Inputs.TtfCharacterSet[chr];
                        thisCharacterPosition = GetCharacterPosition(isotopeCursorPosition, c);

                        // Raise the superscript Character
                        thisCharacterPosition.Offset(0, -OoXmlHelper.ScaleCsTtfToCml(c.Height * OoXmlHelper.CS_SUPERSCRIPT_RAISE_FACTOR, Inputs.MeanBondLength));

                        AtomLabelCharacter alcc = new AtomLabelCharacter(thisCharacterPosition, c, atomColour, atom.Path, atom.Parent.Path);
                        alcc.IsSmaller = true;
                        alcc.IsSubScript = true;
                        Outputs.AtomLabelCharacters.Add(alcc);

                        // Move to next Character position
                        isotopeCursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(c.IncrementX, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR, 0);
                    }
                }

                #endregion Step 6 - Add IsoTope Number if required

                #region Step 7 - Create Convex Hull

                Outputs.ConvexHulls.Add(atom.Path, ConvexHull(atom.Path));

                #endregion Step 7 - Create Convex Hull
            }
        }

        private void CreateFunctionalGroupCharacters(Atom atom)
        {
            FunctionalGroup fg = atom.Element as FunctionalGroup;
            bool reverse = atom.FunctionalGroupPlacement == CompassPoints.West;

            #region Set Up Functional Group Colour

            string atomColour = "000000";
            if (Inputs.Options.ColouredAtoms
                && !string.IsNullOrEmpty(fg.Colour))
            {
                atomColour = fg.Colour;
                // Strip out # as OoXml does not use it
                atomColour = atomColour.Replace("#", "");
            }

            #endregion Set Up Functional Group Colour

            List<FunctionalGroupTerm> terms = fg.ExpandIntoTerms(reverse);

            #region Step 1 - Generate the characters and measure Bounding Boxes

            var cursorPosition = atom.Position;

            List<AtomLabelCharacter> fgCharacters = new List<AtomLabelCharacter>();
            TtfCharacter hydrogenCharacter = Inputs.TtfCharacterSet['H'];

            Rect fgBoundingBox = Rect.Empty;
            Rect anchorBoundingBox = Rect.Empty;

            foreach (var term in terms)
            {
                foreach (var part in term.Parts)
                {
                    foreach (char c in part.Text)
                    {
                        Rect bb = AddCharacter(c, part.Type);
                        fgBoundingBox.Union(bb);
                        if (term.IsAnchor)
                        {
                            anchorBoundingBox.Union(bb);
                        }
                    }
                }
            }

            #endregion Step 1 - Generate the characters and measure Bounding Boxes

            #region Step 2 - Move all characters such that the anchor term is centered on the atom position

            double offsetX;
            double offsetY;
            if (reverse)
            {
                offsetX = fgBoundingBox.Width - anchorBoundingBox.Width / 2;
                offsetY = anchorBoundingBox.Height / 2;
            }
            else
            {
                offsetX = anchorBoundingBox.Width / 2;
                offsetY = anchorBoundingBox.Height / 2;
            }

            offsetY = offsetY + anchorBoundingBox.Top - atom.Position.Y;

            foreach (var alc in fgCharacters)
            {
                alc.Position = new Point(alc.Position.X - offsetX, alc.Position.Y - offsetY);
            }

            #endregion Step 2 - Move all characters such that the anchor term is centered on the atom position

            #region Step 3 - Transfer characters into main list

            foreach (var alc in fgCharacters)
            {
                Outputs.AtomLabelCharacters.Add(alc);
            }

            #endregion Step 3 - Transfer characters into main list

            #region Step 4 - Convex Hull

            Outputs.ConvexHulls.Add(atom.Path, ConvexHull(atom.Path));

            #endregion Step 4 - Convex Hull

            // Local Function
            Rect AddCharacter(char c, FunctionalGroupPartType type)
            {
                TtfCharacter ttf = Inputs.TtfCharacterSet[c];
                var thisCharacterPosition = GetCharacterPosition(cursorPosition, ttf);
                var alc = new AtomLabelCharacter(thisCharacterPosition, ttf, atomColour, atom.Path, atom.Parent.Path);
                alc.IsSubScript = type == FunctionalGroupPartType.Subscript;
                alc.IsSuperScript = type == FunctionalGroupPartType.Superscript;
                alc.IsSmaller = alc.IsSubScript || alc.IsSuperScript;

                Rect thisBoundingBox;
                if (alc.IsSmaller)
                {
                    // Start by assuming it's SubScript
                    thisCharacterPosition.Offset(0, OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Height * OoXmlHelper.SUBSCRIPT_DROP_FACTOR, Inputs.MeanBondLength));
                    if (alc.IsSuperScript)
                    {
                        // Shift up by height of H to make it SuperScript
                        thisCharacterPosition.Offset(0, -OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Height, Inputs.MeanBondLength));
                    }

                    // Reset the character's position
                    alc.Position = thisCharacterPosition;

                    thisBoundingBox = new Rect(alc.Position,
                        new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR,
                                OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR));

                    cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(alc.Character.IncrementX, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR, 0);
                }
                else
                {
                    thisBoundingBox = new Rect(alc.Position,
                        new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, Inputs.MeanBondLength),
                            OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, Inputs.MeanBondLength)));

                    cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(alc.Character.IncrementX, Inputs.MeanBondLength), 0);
                }

                fgCharacters.Add(alc);

                return thisBoundingBox;
            }
        }

        private void AddMoleculeLabels(List<TextualProperty> labels, Point centrePoint, string moleculePath)
        {
            Point measure = new Point(centrePoint.X, centrePoint.Y);

            foreach (var label in labels)
            {
                // 1. Measure string
                var bb = MeasureString(label.Value, measure);

                // 2. Place string characters such that they are hanging below the "line"
                if (bb != Rect.Empty)
                {
                    Point place = new Point(measure.X - bb.Width / 2, measure.Y + (measure.Y - bb.Top));
                    PlaceString(label.Value, place, moleculePath);
                }

                // 3. Move to next line
                measure.Offset(0, bb.Height + Inputs.MeanBondLength * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE / 2);
            }
        }

        // MAW Keep this code as it may be possible to bring back use of OoXmlString later on
        private void AddMoleculeLabelsV2(List<TextualProperty> labels, Point centrePoint, string moleculePath)
        {
            Point measure = new Point(centrePoint.X, centrePoint.Y);

            foreach (var label in labels)
            {
                // 1. Measure string
                var bb = MeasureString(label.Value, measure);

                // 2. Place string characters such that they are hanging below the "line"
                if (bb != Rect.Empty)
                {
                    Point place = new Point(measure.X - bb.Width / 2, measure.Y);
                    Outputs.MoleculeLabels.Add(new OoXmlString(new Rect(place, bb.Size), label.Value, moleculePath));
                }

                // 3. Move to next line
                measure.Offset(0, bb.Height + Inputs.MeanBondLength * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE / 2);
            }
        }

        /// <summary>
        /// Creates the lines for a bond
        /// </summary>
        /// <param name="bond"></param>
        private void CreateLines(Bond bond)
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
                    Outputs.BondLines.Add(new BondLine(BondLineStyle.Zero, bond));
                    break;

                case Globals.OrderPartial01:
                    Outputs.BondLines.Add(new BondLine(BondLineStyle.Half, bond));
                    break;

                case "1":
                case Globals.OrderSingle:
                    switch (bond.Stereo)
                    {
                        case Globals.BondStereo.None:
                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, bond));
                            break;

                        case Globals.BondStereo.Hatch:
                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Hatch, bond));
                            break;

                        case Globals.BondStereo.Wedge:
                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Wedge, bond));
                            break;

                        case Globals.BondStereo.Indeterminate:
                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Wavy, bond));
                            break;

                        default:

                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, bond));
                            break;
                    }
                    break;

                case Globals.OrderPartial12:
                case Globals.OrderAromatic:

                    BondLine onePointFive;
                    BondLine onePointFiveDashed;
                    Point onePointFiveStart;
                    Point onePointFiveEnd;

                    switch (bond.Placement)
                    {
                        case Globals.BondDirection.Clockwise:
                            onePointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(onePointFive);
                            onePointFiveDashed = onePointFive.GetParallel(BondOffset());
                            onePointFiveStart = new Point(onePointFiveDashed.Start.X, onePointFiveDashed.Start.Y);
                            onePointFiveEnd = new Point(onePointFiveDashed.End.X, onePointFiveDashed.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref onePointFiveStart, ref onePointFiveEnd, -(BondOffset() / 1.75));
                            onePointFiveDashed = new BondLine(BondLineStyle.Half, onePointFiveStart, onePointFiveEnd, bond);
                            Outputs.BondLines.Add(onePointFiveDashed);
                            break;

                        case Globals.BondDirection.Anticlockwise:
                            onePointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(onePointFive);
                            onePointFiveDashed = onePointFive.GetParallel(-BondOffset());
                            onePointFiveStart = new Point(onePointFiveDashed.Start.X, onePointFiveDashed.Start.Y);
                            onePointFiveEnd = new Point(onePointFiveDashed.End.X, onePointFiveDashed.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref onePointFiveStart, ref onePointFiveEnd, -(BondOffset() / 1.75));
                            onePointFiveDashed = new BondLine(BondLineStyle.Half, onePointFiveStart, onePointFiveEnd, bond);
                            Outputs.BondLines.Add(onePointFiveDashed);
                            break;

                        case Globals.BondDirection.None:
                            onePointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(onePointFive.GetParallel(-(BondOffset() / 2)));
                            onePointFiveDashed = onePointFive.GetParallel(BondOffset() / 2);
                            onePointFiveDashed.SetLineStyle(BondLineStyle.Half);
                            Outputs.BondLines.Add(onePointFiveDashed);
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
                        Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, new Point(d1.Start.X, d1.Start.Y), new Point(d2.End.X, d2.End.Y), bond));
                        Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, new Point(d2.Start.X, d2.Start.Y), new Point(d1.End.X, d1.End.Y), bond));
                    }
                    else
                    {
                        switch (bond.Placement)
                        {
                            case Globals.BondDirection.Anticlockwise:
                                BondLine da = new BondLine(BondLineStyle.Solid, bond);
                                Outputs.BondLines.Add(da);
                                PlaceOtherLine(da, da.GetParallel(-BondOffset()));
                                break;

                            case Globals.BondDirection.Clockwise:
                                BondLine dc = new BondLine(BondLineStyle.Solid, bond);
                                Outputs.BondLines.Add(dc);
                                PlaceOtherLine(dc, dc.GetParallel(BondOffset()));
                                break;

                                // Local Function
                                void PlaceOtherLine(BondLine primaryLine, BondLine secondaryLine)
                                {
                                    var primaryMidpoint = CoordinateTool.GetMidPoint(primaryLine.Start, primaryLine.End);
                                    var secondaryMidpoint = CoordinateTool.GetMidPoint(secondaryLine.Start, secondaryLine.End);

                                    Point startPointa = secondaryLine.Start;
                                    Point endPointa = secondaryLine.End;

                                    Point? centre = null;

                                    bool clip = false;

                                    // Does bond have a primary ring?
                                    if (bond.PrimaryRing != null && bond.PrimaryRing.Centroid != null)
                                    {
                                        // Get angle between bond and vector to primary ring centre
                                        centre = bond.PrimaryRing.Centroid.Value;
                                        var primaryRingVector = primaryMidpoint - centre.Value;
                                        var angle = CoordinateTool.AngleBetween(bond.BondVector, primaryRingVector);

                                        // Does bond have a secondary ring?
                                        if (bond.SubsidiaryRing != null && bond.SubsidiaryRing.Centroid != null)
                                        {
                                            // Get angle between bond and vector to secondary ring centre
                                            var centre2 = bond.SubsidiaryRing.Centroid.Value;
                                            var secondaryRingVector = primaryMidpoint - centre2;
                                            var angle2 = CoordinateTool.AngleBetween(bond.BondVector, secondaryRingVector);

                                            // Get angle in which the offset line has moved with respect to the bond line
                                            var offsetVector = primaryMidpoint - secondaryMidpoint;
                                            var offsetAngle = CoordinateTool.AngleBetween(bond.BondVector, offsetVector);

                                            // If in the same direction as secondary ring centre, use it
                                            if (Math.Sign(angle2) == Math.Sign(offsetAngle))
                                            {
                                                centre = centre2;
                                            }
                                        }

                                        // Is projection to centre at right angles +/- 10 degrees
                                        if (Math.Abs(angle) > 80 && Math.Abs(angle) < 100)
                                        {
                                            clip = true;
                                        }

                                        // Is secondary line outside of the "selected" ring
                                        var distance1 = primaryRingVector.Length;
                                        var distance2 = (secondaryMidpoint - centre.Value).Length;
                                        if (distance2 > distance1)
                                        {
                                            clip = false;
                                        }
                                    }

                                    if (clip)
                                    {
                                        Point outIntersectP1;
                                        Point outIntersectP2;

                                        CoordinateTool.FindIntersection(startPointa, endPointa, bondStart, centre.Value,
                                                                        out _, out _, out outIntersectP1);
                                        CoordinateTool.FindIntersection(startPointa, endPointa, bondEnd, centre.Value,
                                                                        out _, out _, out outIntersectP2);

                                        Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, outIntersectP1, outIntersectP2, bond));

                                        if (Inputs.Options.ShowBondClippingLines)
                                        {
                                            // Diagnostics
                                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Zero, bond.StartAtom.Position, centre.Value, "ff0000"));
                                            Outputs.BondLines.Add(new BondLine(BondLineStyle.Zero, bond.EndAtom.Position, centre.Value, "ff0000"));
                                        }
                                    }
                                    else
                                    {
                                        CoordinateTool.AdjustLineAboutMidpoint(ref startPointa, ref endPointa, -(BondOffset() / 1.75));
                                        Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, startPointa, endPointa, bond));
                                    }
                                }

                            default:
                                switch (bond.Stereo)
                                {
                                    case Globals.BondStereo.Cis:
                                        BondLine dcc = new BondLine(BondLineStyle.Solid, bond);
                                        Outputs.BondLines.Add(dcc);
                                        BondLine blnewc = dcc.GetParallel(BondOffset());
                                        Point startPointn = blnewc.Start;
                                        Point endPointn = blnewc.End;
                                        CoordinateTool.AdjustLineAboutMidpoint(ref startPointn, ref endPointn, -(BondOffset() / 1.75));
                                        Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, startPointn, endPointn, bond));
                                        break;

                                    case Globals.BondStereo.Trans:
                                        BondLine dtt = new BondLine(BondLineStyle.Solid, bond);
                                        Outputs.BondLines.Add(dtt);
                                        BondLine blnewt = dtt.GetParallel(BondOffset());
                                        Point startPointt = blnewt.Start;
                                        Point endPointt = blnewt.End;
                                        CoordinateTool.AdjustLineAboutMidpoint(ref startPointt, ref endPointt, -(BondOffset() / 1.75));
                                        Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, startPointt, endPointt, bond));
                                        break;

                                    default:
                                        BondLine dp = new BondLine(BondLineStyle.Solid, bond);
                                        Outputs.BondLines.Add(dp.GetParallel(-(BondOffset() / 2)));
                                        Outputs.BondLines.Add(dp.GetParallel(BondOffset() / 2));
                                        break;
                                }
                                break;
                        }
                    }
                    break;

                case Globals.OrderPartial23:
                    BondLine twoPointFive;
                    BondLine twoPointFiveDashed;
                    BondLine twoPointFiveParallel;
                    Point twoPointFiveStart;
                    Point twoPointFiveEnd;
                    switch (bond.Placement)
                    {
                        case Globals.BondDirection.Clockwise:
                            // Central bond line
                            twoPointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(twoPointFive);
                            // Solid bond line
                            twoPointFiveParallel = twoPointFive.GetParallel(-BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveParallel.Start.X, twoPointFiveParallel.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveParallel.End.X, twoPointFiveParallel.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / 1.75));
                            twoPointFiveParallel = new BondLine(BondLineStyle.Solid, twoPointFiveStart, twoPointFiveEnd, bond);
                            Outputs.BondLines.Add(twoPointFiveParallel);
                            // Dashed bond line
                            twoPointFiveDashed = twoPointFive.GetParallel(BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveDashed.Start.X, twoPointFiveDashed.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveDashed.End.X, twoPointFiveDashed.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / 1.75));
                            twoPointFiveDashed = new BondLine(BondLineStyle.Half, twoPointFiveStart, twoPointFiveEnd, bond);
                            Outputs.BondLines.Add(twoPointFiveDashed);
                            break;

                        case Globals.BondDirection.Anticlockwise:
                            // Central bond line
                            twoPointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(twoPointFive);
                            // Dashed bond line
                            twoPointFiveDashed = twoPointFive.GetParallel(-BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveDashed.Start.X, twoPointFiveDashed.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveDashed.End.X, twoPointFiveDashed.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / 1.75));
                            twoPointFiveDashed = new BondLine(BondLineStyle.Half, twoPointFiveStart, twoPointFiveEnd, bond);
                            Outputs.BondLines.Add(twoPointFiveDashed);
                            // Solid bond line
                            twoPointFiveParallel = twoPointFive.GetParallel(BondOffset());
                            twoPointFiveStart = new Point(twoPointFiveParallel.Start.X, twoPointFiveParallel.Start.Y);
                            twoPointFiveEnd = new Point(twoPointFiveParallel.End.X, twoPointFiveParallel.End.Y);
                            CoordinateTool.AdjustLineAboutMidpoint(ref twoPointFiveStart, ref twoPointFiveEnd, -(BondOffset() / 1.75));
                            twoPointFiveParallel = new BondLine(BondLineStyle.Solid, twoPointFiveStart, twoPointFiveEnd, bond);
                            Outputs.BondLines.Add(twoPointFiveParallel);
                            break;

                        case Globals.BondDirection.None:
                            twoPointFive = new BondLine(BondLineStyle.Solid, bond);
                            Outputs.BondLines.Add(twoPointFive);
                            Outputs.BondLines.Add(twoPointFive.GetParallel(-BondOffset()));
                            twoPointFiveDashed = twoPointFive.GetParallel(BondOffset());
                            twoPointFiveDashed.SetLineStyle(BondLineStyle.Half);
                            Outputs.BondLines.Add(twoPointFiveDashed);
                            break;
                    }
                    break;

                case "3":
                case Globals.OrderTriple:
                    BondLine tripple = new BondLine(BondLineStyle.Solid, bond);
                    Outputs.BondLines.Add(tripple);
                    Outputs.BondLines.Add(tripple.GetParallel(BondOffset()));
                    Outputs.BondLines.Add(tripple.GetParallel(-BondOffset()));
                    break;

                default:
                    // Draw a single line, so that there is something to see
                    Outputs.BondLines.Add(new BondLine(BondLineStyle.Solid, bond));
                    break;
            }

            #endregion Create Bond Line objects
        }

        private double BondOffset()
        {
            return (Inputs.MeanBondLength * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE);
        }

        private Rect MeasureString(string text, Point startPoint)
        {
            Rect boundingBox = Rect.Empty;
            Point cursor = new Point(startPoint.X, startPoint.Y);

            TtfCharacter i = Inputs.TtfCharacterSet['i'];

            for (int idx = 0; idx < text.Length; idx++)
            {
                TtfCharacter c = Inputs.TtfCharacterSet['?'];
                char chr = text[idx];
                if (Inputs.TtfCharacterSet.ContainsKey(chr))
                {
                    c = Inputs.TtfCharacterSet[chr];
                }

                if (c != null)
                {
                    Point position = GetCharacterPosition(cursor, c);

                    Rect thisRect = new Rect(new Point(position.X, position.Y),
                                    new Size(OoXmlHelper.ScaleCsTtfToCml(c.Width, Inputs.MeanBondLength),
                                             OoXmlHelper.ScaleCsTtfToCml(c.Height, Inputs.MeanBondLength)));

                    boundingBox.Union(thisRect);

                    if (idx < text.Length - 1)
                    {
                        // Move to next Character position
                        // We ought to be able to use c.IncrementX, but this does not work with string such as "Bowl"
                        cursor.Offset(OoXmlHelper.ScaleCsTtfToCml(c.Width + i.Width, Inputs.MeanBondLength), 0);
                    }
                }
            }

            return boundingBox;
        }

        private void PlaceString(string text, Point startPoint, string path)
        {
            Point cursor = new Point(startPoint.X, startPoint.Y);

            TtfCharacter i = Inputs.TtfCharacterSet['i'];

            for (int idx = 0; idx < text.Length; idx++)
            {
                TtfCharacter c = Inputs.TtfCharacterSet['?'];
                char chr = text[idx];
                if (Inputs.TtfCharacterSet.ContainsKey(chr))
                {
                    c = Inputs.TtfCharacterSet[chr];
                }

                if (c != null)
                {
                    Point position = GetCharacterPosition(cursor, c);

                    AtomLabelCharacter alc = new AtomLabelCharacter(position, c, "000000", path, path);
                    Outputs.AtomLabelCharacters.Add(alc);

                    if (idx < text.Length - 1)
                    {
                        // Move to next Character position
                        // We ought to be able to use c.IncrementX, but this does not work with string such as "Bowl"
                        cursor.Offset(OoXmlHelper.ScaleCsTtfToCml(c.Width + i.Width, Inputs.MeanBondLength), 0);
                    }
                }
            }
        }

        private List<Point> ConvexHull(string atomPath)
        {
            List<Point> points = new List<Point>();

            var chars = Outputs.AtomLabelCharacters.Where(m => m.ParentAtom == atomPath);
            double margin = OoXmlHelper.CHARACTER_CLIPPING_MARGIN;
            foreach (var c in chars)
            {
                // Top Left --
                points.Add(new Point(c.Position.X - margin, c.Position.Y - margin));
                if (c.IsSmaller)
                {
                    points.Add(new Point(c.Position.X + OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR + margin,
                                        c.Position.Y - margin));
                    points.Add(new Point(c.Position.X + OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR + margin,
                                        c.Position.Y + OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR + margin));
                    points.Add(new Point(c.Position.X - margin,
                                        c.Position.Y + OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR + margin));
                }
                else
                {
                    points.Add(new Point(c.Position.X + OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength) + margin,
                                        c.Position.Y - margin));
                    points.Add(new Point(c.Position.X + OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, Inputs.MeanBondLength) + margin,
                                        c.Position.Y + OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength) + margin));
                    points.Add(new Point(c.Position.X - margin,
                                          c.Position.Y + OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, Inputs.MeanBondLength) + margin));
                }
            }

            return GeometryTool.MakeConvexHull(points);
        }

        private Point GetCharacterPosition(Point cursorPosition, TtfCharacter character)
        {
            // Add the (negative) OriginY to raise the character by it
            return new Point(cursorPosition.X, cursorPosition.Y + OoXmlHelper.ScaleCsTtfToCml(character.OriginY, Inputs.MeanBondLength));
        }
    }
}