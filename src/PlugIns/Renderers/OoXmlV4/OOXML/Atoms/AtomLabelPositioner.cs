// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2;
using Chem4Word.Model2.Geometry;
using Chem4Word.Model2.Helpers;
using Chem4Word.Renderer.OoXmlV4.TTF;
using IChem4Word.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using Point = System.Windows.Point;

namespace Chem4Word.Renderer.OoXmlV4.OOXML.Atoms
{
    public class AtomLabelPositioner
    {
        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private Dictionary<char, TtfCharacter> _TtfCharacterSet;

        private List<AtomLabelCharacter> _AtomLabelCharacters;
        private IChem4WordTelemetry _telemetry;

        public AtomLabelPositioner(List<AtomLabelCharacter> atomLabelCharacters, Dictionary<char, TtfCharacter> characterset, IChem4WordTelemetry telemetry)
        {
            _AtomLabelCharacters = atomLabelCharacters;
            _TtfCharacterSet = characterset;
            _telemetry = telemetry;
        }

        public void CreateElementCharacters(Atom atom, Options options)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            //Point atomCentre = new Point((double)atom.X2, (double)atom.Y2);
            string atomLabel = atom.Element.Symbol;
            Rect labelBounds;

            // Get Charge and Isotope valuesfor use later on
            int iCharge = atom.FormalCharge ?? 0;
            int iAbsCharge = Math.Abs(iCharge);
            int isoValue = atom.IsotopeNumber ?? 0;

            // Get Implicit Hydrogen Count for use later on
            int implicitHCount = atom.ImplicitHydrogenCount;

            Point cursorPosition = atom.Position;
            Point chargeCursorPosition = atom.Position;
            Point isotopeCursorPosition = atom.Position;

            double lastOffset = 0;

            //Debug.WriteLine("  X: " + atom.X2 + " Y: " + atom.Y2 + " Implicit H Count: " + implicitHCount);

            int ringCount = atom.Rings.Count;
            int bondCount = atom.Bonds.ToList().Count;

            //var bv = atom.BalancingVector;
            //_telemetry.Write(module, "Debugging", $"Atom {atomLabel} [{atom.Id}] at {atom.Position} BalancingVector {bv} [{CoordinateTool.BearingOfVector(bv)}°]");

            #region Decide if atom label is to be displayed

            bool showLabel = true;
            if (atomLabel == "C")
            {
                if (!options.ShowCarbons)
                {
                    if (ringCount > 0 || bondCount > 1)
                    {
                        showLabel = false;
                    }

                    if (bondCount == 2)
                    {
                        Point p1 = atom.Bonds.ToList()[0].OtherAtom(atom).Position;
                        Point p2 = atom.Bonds.ToList()[1].OtherAtom(atom).Position;

                        double angle1 = Vector.AngleBetween(-(atom.Position - p1), atom.Position - p2);

                        if (Math.Abs(angle1) < 8)
                        {
                            showLabel = true;
                        }
                    }

                    // Force on if atom has charge
                    if (iAbsCharge > 0)
                    {
                        showLabel = true;
                    }
                    // Force on if atom has isotope value
                    if (isoValue > 0)
                    {
                        showLabel = true;
                    }
                }
            }

            #endregion Decide if atom label is to be displayed

            if (showLabel)
            {
                #region Set Up Atom Colours

                string atomColour = "000000";
                if (options.ColouredAtoms)
                {
                    if (atom.Element.Colour != null)
                    {
                        atomColour = atom.Element.Colour;
                        // Strip out # as OoXml does not use it
                        atomColour = atomColour.Replace("#", "");
                    }
                }

                #endregion Set Up Atom Colours

                #region Step 1 - Measure Bounding Box for all Characters of label

                double xMin = double.MaxValue;
                double yMin = double.MaxValue;
                double xMax = double.MinValue;
                double yMax = double.MinValue;

                Point thisCharacterPosition;
                for (int idx = 0; idx < atomLabel.Length; idx++)
                {
                    char chr = atomLabel[idx];
                    TtfCharacter c = _TtfCharacterSet[chr];
                    if (c != null)
                    {
                        thisCharacterPosition = GetCharacterPosition(cursorPosition, c);

                        xMin = Math.Min(xMin, thisCharacterPosition.X);
                        yMin = Math.Min(yMin, thisCharacterPosition.Y);
                        xMax = Math.Max(xMax, thisCharacterPosition.X + OoXmlHelper.ScaleCsTtfToCml(c.Width));
                        yMax = Math.Max(yMax, thisCharacterPosition.Y + OoXmlHelper.ScaleCsTtfToCml(c.Height));

                        // Uncomment the following to disable offsetting for terminal atoms
                        //if (bonds.Count == 1)
                        //{
                        //    break;
                        //}

                        if (idx < atomLabel.Length - 1)
                        {
                            // Move to next Character position
                            cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(c.IncrementX), 0);
                        }
                    }
                }

                #endregion Step 1 - Measure Bounding Box for all Characters of label

                #region Step 2 - Reset Cursor such that the text is centered about the atom's co-ordinates

                double width = xMax - xMin;
                double height = yMax - yMin;
                cursorPosition = new Point(atom.Position.X - width / 2, atom.Position.Y + height / 2);
                chargeCursorPosition = new Point(cursorPosition.X, cursorPosition.Y);
                isotopeCursorPosition = new Point(cursorPosition.X, cursorPosition.Y);
                labelBounds = new Rect(cursorPosition, new Size(width, height));
                //_telemetry.Write(module, "Debugging", $"Atom {atomLabel} [{atom.Id}] Label Bounds {labelBounds}");

                #endregion Step 2 - Reset Cursor such that the text is centered about the atom's co-ordinates

                #region Step 3 - Place the characters

                foreach (char chr in atomLabel)
                {
                    TtfCharacter c = _TtfCharacterSet[chr];
                    if (c != null)
                    {
                        thisCharacterPosition = GetCharacterPosition(cursorPosition, c);
                        AtomLabelCharacter alc = new AtomLabelCharacter(thisCharacterPosition, c, atomColour, chr, atom.Path, atom.Parent.Path);
                        _AtomLabelCharacters.Add(alc);

                        // Move to next Character position
                        lastOffset = OoXmlHelper.ScaleCsTtfToCml(c.IncrementX);
                        cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(c.IncrementX), 0);
                        chargeCursorPosition = new Point(cursorPosition.X, cursorPosition.Y);
                    }
                }

                #endregion Step 3 - Place the characters

                #region Determine NESW

                double baFromNorth = Vector.AngleBetween(BasicGeometry.ScreenNorth, atom.BalancingVector);
                CompassPoints nesw = CompassPoints.East;

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
                    TtfCharacter hydrogenCharacter = _TtfCharacterSet['H'];

                    char sign = '.';
                    TtfCharacter chargeSignCharacter = null;
                    if (iCharge >= 1)
                    {
                        sign = '+';
                        chargeSignCharacter = _TtfCharacterSet['+'];
                    }
                    else if (iCharge <= 1)
                    {
                        sign = '-';
                        chargeSignCharacter = _TtfCharacterSet['-'];
                    }

                    if (iAbsCharge > 1)
                    {
                        string digits = iAbsCharge.ToString();
                        // Insert digits
                        foreach (char chr in digits)
                        {
                            TtfCharacter chargeValueCharacter = _TtfCharacterSet[chr];
                            thisCharacterPosition = GetCharacterPosition(chargeCursorPosition, chargeValueCharacter);

                            // Raise the superscript Character
                            thisCharacterPosition.Offset(0, -OoXmlHelper.ScaleCsTtfToCml(chargeValueCharacter.Height * OoXmlHelper.CS_SUPERSCRIPT_RAISE_FACTOR));

                            AtomLabelCharacter alcc = new AtomLabelCharacter(thisCharacterPosition, chargeValueCharacter, atomColour, chr, atom.Path, atom.Parent.Path);
                            alcc.IsSmaller = true;
                            alcc.IsSubScript = true;
                            _AtomLabelCharacters.Add(alcc);

                            // Move to next Character position
                            chargeCursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(chargeValueCharacter.IncrementX) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR, 0);
                            cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(chargeValueCharacter.IncrementX) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR, 0);
                        }
                    }

                    // Insert sign at raised position
                    thisCharacterPosition = GetCharacterPosition(chargeCursorPosition, chargeSignCharacter);
                    thisCharacterPosition.Offset(0, -OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Height * OoXmlHelper.CS_SUPERSCRIPT_RAISE_FACTOR));
                    thisCharacterPosition.Offset(0, -OoXmlHelper.ScaleCsTtfToCml(chargeSignCharacter.Height / 2 * OoXmlHelper.CS_SUPERSCRIPT_RAISE_FACTOR));

                    AtomLabelCharacter alcs = new AtomLabelCharacter(thisCharacterPosition, chargeSignCharacter, atomColour, sign, atom.Path, atom.Parent.Path);
                    alcs.IsSmaller = true;
                    alcs.IsSubScript = true;
                    _AtomLabelCharacters.Add(alcs);

                    if (iAbsCharge != 0)
                    {
                        cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(chargeSignCharacter.IncrementX) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR, 0);
                    }
                }

                #endregion Step 4 - Add Charge if required

                #region Step 5 - Add Implicit H if required

                if (options.ShowHydrogens && implicitHCount > 0)
                {
                    TtfCharacter hydrogenCharacter = _TtfCharacterSet['H'];
                    string numbers = "012345";
                    TtfCharacter implicitValueCharacter = _TtfCharacterSet[numbers[implicitHCount]];

                    #region Add H

                    if (hydrogenCharacter != null)
                    {
                        switch (nesw)
                        {
                            case CompassPoints.North:
                                if (atom.Bonds.ToList().Count > 1)
                                {
                                    cursorPosition.X = labelBounds.X + (labelBounds.Width / 2) - (OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Width) / 2);
                                    cursorPosition.Y = cursorPosition.Y +
                                                       OoXmlHelper.ScaleCsTtfToCml(-hydrogenCharacter.Height) -
                                                       OoXmlHelper.CHARACTER_CLIPPING_MARGIN;
                                    if (iCharge > 0)
                                    {
                                        if (implicitHCount > 1)
                                        {
                                            cursorPosition.Offset(0, OoXmlHelper.ScaleCsTtfToCml(-implicitValueCharacter.Height * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR / 2) - OoXmlHelper.CHARACTER_CLIPPING_MARGIN);
                                        }
                                    }
                                }
                                break;

                            case CompassPoints.East:
                                // Leave as is
                                break;

                            case CompassPoints.South:
                                if (atom.Bonds.ToList().Count > 1)
                                {
                                    cursorPosition.X = labelBounds.X + (labelBounds.Width / 2) - (OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Width) / 2);
                                    cursorPosition.Y = cursorPosition.Y +
                                                       OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Height) +
                                                       OoXmlHelper.CHARACTER_CLIPPING_MARGIN;
                                }
                                break;

                            case CompassPoints.West:
                                if (implicitHCount == 1)
                                {
                                    cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(-(hydrogenCharacter.IncrementX * 2)), 0);
                                }
                                else
                                {
                                    if (iAbsCharge == 0)
                                    {
                                        cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(-(hydrogenCharacter.IncrementX * 2.5)), 0);
                                    }
                                    else
                                    {
                                        cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(-((hydrogenCharacter.IncrementX * 2 + implicitValueCharacter.IncrementX * 1.5))), 0);
                                    }
                                }
                                break;
                        }

                        //_telemetry.Write(module, "Debugging", $"Adding H at {cursorPosition}");
                        thisCharacterPosition = GetCharacterPosition(cursorPosition, hydrogenCharacter);
                        AtomLabelCharacter alc = new AtomLabelCharacter(thisCharacterPosition, hydrogenCharacter, atomColour, 'H', atom.Path, atom.Parent.Path);
                        _AtomLabelCharacters.Add(alc);

                        // Move to next Character position
                        cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.IncrementX), 0);

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

                    if (implicitHCount > 1)
                    {
                        if (implicitValueCharacter != null)
                        {
                            thisCharacterPosition = GetCharacterPosition(cursorPosition, implicitValueCharacter);

                            // Drop the subscript Character
                            thisCharacterPosition.Offset(0, OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Width * OoXmlHelper.SUBSCRIPT_DROP_FACTOR));

                            AtomLabelCharacter alc = new AtomLabelCharacter(thisCharacterPosition, implicitValueCharacter, atomColour, numbers[implicitHCount], atom.Path, atom.Parent.Path);
                            alc.IsSmaller = true;
                            alc.IsSubScript = true;
                            _AtomLabelCharacters.Add(alc);

                            // Move to next Character position
                            cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(implicitValueCharacter.IncrementX) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR, 0);
                        }
                    }

                    #endregion Add number
                }

                #endregion Step 5 - Add Implicit H if required

                #region Step 6 Add IsoTope Number if required

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
                        TtfCharacter c = _TtfCharacterSet[chr];
                        thisCharacterPosition = GetCharacterPosition(isotopeCursorPosition, c);

                        // Raise the superscript Character
                        thisCharacterPosition.Offset(0, -OoXmlHelper.ScaleCsTtfToCml(c.Height * OoXmlHelper.CS_SUPERSCRIPT_RAISE_FACTOR));

                        xMin = Math.Min(xMin, thisCharacterPosition.X);
                        yMin = Math.Min(yMin, thisCharacterPosition.Y);
                        xMax = Math.Max(xMax, thisCharacterPosition.X + OoXmlHelper.ScaleCsTtfToCml(c.Width));
                        yMax = Math.Max(yMax, thisCharacterPosition.Y + OoXmlHelper.ScaleCsTtfToCml(c.Height));

                        // Move to next Character position
                        isotopeCursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(c.IncrementX) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR, 0);
                    }

                    // Re-position Isotope Cursor
                    width = xMax - xMin;
                    isotopeCursorPosition = new Point(isoOrigin.X - width, isoOrigin.Y);

                    // Insert digits
                    foreach (char chr in digits)
                    {
                        TtfCharacter c = _TtfCharacterSet[chr];
                        thisCharacterPosition = GetCharacterPosition(isotopeCursorPosition, c);

                        // Raise the superscript Character
                        thisCharacterPosition.Offset(0, -OoXmlHelper.ScaleCsTtfToCml(c.Height * OoXmlHelper.CS_SUPERSCRIPT_RAISE_FACTOR));

                        AtomLabelCharacter alcc = new AtomLabelCharacter(thisCharacterPosition, c, atomColour, chr, atom.Path, atom.Parent.Path);
                        alcc.IsSmaller = true;
                        alcc.IsSubScript = true;
                        _AtomLabelCharacters.Add(alcc);

                        // Move to next Character position
                        isotopeCursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(c.IncrementX) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR, 0);
                    }
                }

                #endregion Step 6 Add IsoTope Number if required
            }
        }

        private Point GetCharacterPosition(Point cursorPosition, TtfCharacter character)
        {
            // Add the (negative) OriginY to raise the character by it
            return new Point(cursorPosition.X, cursorPosition.Y + OoXmlHelper.ScaleCsTtfToCml(character.OriginY));
        }

        public void CreateFunctionalGroupCharacters(Atom atom, Options options)
        {
            FunctionalGroup fg = atom.Element as FunctionalGroup;
            double baFromNorth = Vector.AngleBetween(BasicGeometry.ScreenNorth, atom.BalancingVector);

            CompassPoints nesw = BasicGeometry.SnapTo2EW(baFromNorth);
            bool reverse = nesw == CompassPoints.West;

            #region Set Up Atom Colours

            string atomColour = "000000";
            if (options.ColouredAtoms)
            {
                atomColour = fg.Colour;
                // Strip out # as OoXml does not use it
                atomColour = atomColour.Replace("#", "");
            }

            #endregion Set Up Atom Colours

            List<FunctionalGroupTerm> terms = new List<FunctionalGroupTerm>();

            #region Step 1 - Expand Fg into terms

            if (fg.ShowAsSymbol)
            {
                var term = new FunctionalGroupTerm();
                terms.Add(term);
                term.IsAnchor = true;
                AddCharacters(fg.Symbol, term);
            }
            else
            {
                int i = 0;
                foreach (var component in fg.Components)
                {
                    var term = new FunctionalGroupTerm();
                    terms.Add(term);
                    term.IsAnchor = i == 0;

                    if (fg.ShowAsSymbol)
                    {
                        AddCharacters(fg.Symbol, term);
                    }
                    else
                    {
                        ExpandGroup(component, term);
                    }

                    i++;
                }
            }


            if (reverse)
            {
                terms.Reverse();
            }

            #endregion Step 1 - Expand Fg into terms

            #region Step 2 - Generate the characters and measure Bounding Boxes

            var cursorPosition = atom.Position;

            List<AtomLabelCharacter> fgCharacters = new List<AtomLabelCharacter>();
            TtfCharacter hydrogenCharacter = _TtfCharacterSet['H'];

            Rect fgBoundingBox = Rect.Empty;
            Rect anchorBoundingBox = Rect.Empty;

            foreach (var term in terms)
            {
                foreach (var alc in term.Characters)
                {
                    Rect thisBoundingBox;
                    if (alc.IsSmaller)
                    {
                        var thisCharacterPosition = cursorPosition;
                        // Start by assuming it's SubScript
                        thisCharacterPosition.Offset(0, OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Height * OoXmlHelper.SUBSCRIPT_DROP_FACTOR));
                        if (alc.IsSuperScript)
                        {
                            // Shift up by height of H to make it SuperScript
                            thisCharacterPosition.Offset(0, - OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Height));
                        }
                        alc.Position = thisCharacterPosition;

                        thisBoundingBox = new Rect(alc.Position,
                            new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR,
                                    OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR));

                        cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(alc.Character.IncrementX) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR, 0);
                    }
                    else
                    {
                        alc.Position = cursorPosition;

                        thisBoundingBox = new Rect(alc.Position,
                            new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width),
                                OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height)));

                        cursorPosition.Offset(OoXmlHelper.ScaleCsTtfToCml(alc.Character.IncrementX), 0);
                    }

                    fgCharacters.Add(alc);

                    if (fgBoundingBox.IsEmpty)
                    {
                        fgBoundingBox = thisBoundingBox;
                    }
                    else
                    {
                        fgBoundingBox.Union(thisBoundingBox);
                    }

                    if (term.IsAnchor)
                    {
                        if (anchorBoundingBox.IsEmpty)
                        {
                            anchorBoundingBox = thisBoundingBox;
                        }
                        else
                        {
                            anchorBoundingBox.Union(thisBoundingBox);
                        }
                    }
                }
            }

            #endregion Step 2 - Generate the characters and measure Bounding Boxes

            #region Step 3 - Move all characters such that the anchor term is centered on the atom position

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

            #endregion Step 3 - Move all characters such that the anchor term is centered on the atom position

            #region Step 4 - Transfer characters into main list

            foreach (var alc in fgCharacters)
            {
                _AtomLabelCharacters.Add(alc);
            }

            #endregion Step 4 - Transfer characters into main list

            #region Local Functions

            // Local function to support recursion
            void AddCharacters(string symbol, FunctionalGroupTerm term, bool isSubscript = false)
            {
                bool isSuperScript = false;
                foreach (var ch in symbol)
                {
                    char c = ch;

                    switch (c)
                    {
                        case '{':
                            isSuperScript = true;
                            break;
                        case '}':
                            isSuperScript = false;
                            break;
                        default:
                            var alc = new AtomLabelCharacter(atom.Position, _TtfCharacterSet[c], atomColour, c, atom.Path, atom.Parent.Path);
                            alc.IsSmaller = isSubscript || isSuperScript;
                            alc.IsSubScript = isSubscript;
                            alc.IsSuperScript = isSuperScript;
                            term.Characters.Add(alc);
                            break;
                    }
                }
            }

            // Local function to support recursion
            void ExpandGroup(Group componentGroup, FunctionalGroupTerm term)
            {
                ElementBase elementBase;
                var ok = AtomHelpers.TryParse(componentGroup.Component, out elementBase);
                if (ok)
                {
                    if (elementBase is Element element)
                    {
                        AddCharacters(element.Symbol, term);

                        if (componentGroup.Count != 1)
                        {
                            AddCharacters($"{componentGroup.Count}", term, true);
                        }
                    }

                    if (elementBase is FunctionalGroup functionalGroup)
                    {
                        if (componentGroup.Count != 1)
                        {
                            AddCharacters("(", term);
                        }

                        if (functionalGroup.ShowAsSymbol)
                        {
                            AddCharacters(functionalGroup.Symbol, term);
                        }
                        else
                        {
                            if (functionalGroup.Flippable && reverse)
                            {
                                for (int ii = functionalGroup.Components.Count - 1; ii >= 0; ii--)
                                {
                                    ExpandGroup(functionalGroup.Components[ii], term);
                                }
                            }
                            else
                            {
                                foreach (var fgc in functionalGroup.Components)
                                {
                                    ExpandGroup(fgc, term);
                                }
                            }
                        }

                        if (componentGroup.Count != 1)
                        {
                            AddCharacters(")", term);
                            AddCharacters($"{componentGroup.Count}", term, true);
                        }
                    }
                }
            }

            #endregion Local Functions
        }
    }
}