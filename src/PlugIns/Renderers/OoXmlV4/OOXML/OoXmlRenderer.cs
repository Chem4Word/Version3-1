// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using Chem4Word.Core.Helpers;
using Chem4Word.Core.UI.Forms;
using Chem4Word.Model2;
using Chem4Word.Model2.Helpers;
using Chem4Word.Renderer.OoXmlV4.Enums;
using Chem4Word.Renderer.OoXmlV4.OOXML.Atoms;
using Chem4Word.Renderer.OoXmlV4.OOXML.Bonds;
using Chem4Word.Renderer.OoXmlV4.TTF;
using DocumentFormat.OpenXml;
using IChem4Word.Contracts;
using Newtonsoft.Json;
using A = DocumentFormat.OpenXml.Drawing;
using Drawing = DocumentFormat.OpenXml.Wordprocessing.Drawing;
using Point = System.Windows.Point;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using Wp = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using Wpg = DocumentFormat.OpenXml.Office2010.Word.DrawingGroup;
using Wps = DocumentFormat.OpenXml.Office2010.Word.DrawingShape;

namespace Chem4Word.Renderer.OoXmlV4.OOXML
{
    public class OoXmlRenderer
    {
        // DrawingML Units
        // https://startbigthinksmall.wordpress.com/2010/01/04/points-inches-and-emus-measuring-units-in-office-open-xml/
        // EMU Calculator
        // http://lcorneliussen.de/raw/dashboards/ooxml/

        private static string _product = Assembly.GetExecutingAssembly().FullName.Split(',')[0];
        private static string _class = MethodBase.GetCurrentMethod().DeclaringType?.Name;

        private Options _options;
        private IChem4WordTelemetry _telemetry;
        private Point _topLeft;

        private Model _chemistryModel;
        private Dictionary<char, TtfCharacter> _TtfCharacterSet;

        private long _ooxmlId = 1;
        private Rect _boundingBoxOfAllCharacters;

        private double _medianBondLength;
        private Rect _boundingBoxOfAllAtoms;

        private const double EPSILON = 1e-4;

        private List<AtomLabelCharacter> _atomLabelCharacters;
        private List<BondLine> _bondLines;

        private List<Rect> _boundingBoxesOfMoleculeAtoms = new List<Rect>();
        private List<Rect> _boundingBoxesOfMoleculesIncludingInternalCharacters = new List<Rect>();
        private List<MoleculeBounds> _boundingBoxesOfMolecules = new List<MoleculeBounds>();
        private List<MoleculeBounds> _boundingBoxesOfGroupedMolecules = new List<MoleculeBounds>();
        private List<Rect> _moleculeBrackets = new List<Rect>();
        private List<Point> _ringCentres = new List<Point>();
        private Dictionary<string, List<Point>> _convexHulls = new Dictionary<string, List<Point>>();

        public OoXmlRenderer(Model model, Options options, IChem4WordTelemetry telemetry, Point topLeft)
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";

            _options = options;
            _telemetry = telemetry;
            _topLeft = topLeft;

            _telemetry.Write(module, "Verbose", "Called");

            LoadFont();

            _chemistryModel = model;

            _boundingBoxOfAllAtoms = model.BoundingBoxOfCmlPoints;
        }

        private Rect CharacterExtents(Molecule mol, Rect existing)
        {
            var chars = _atomLabelCharacters.Where(m => m.ParentMolecule.StartsWith(mol.Path)).ToList();
            foreach (var c in chars)
            {
                if (c.IsSmaller)
                {
                    Rect r = new Rect(c.Position,
                                      new Size(OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, _medianBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR,
                                               OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, _medianBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR));
                    existing.Union(r);
                }
                else
                {
                    Rect r = new Rect(c.Position,
                                      new Size(OoXmlHelper.ScaleCsTtfToCml(c.Character.Width, _medianBondLength),
                                               OoXmlHelper.ScaleCsTtfToCml(c.Character.Height, _medianBondLength)));
                    existing.Union(r);
                }
            }

            return existing;
        }

        private Rect CharacterExtents(Molecule mol)
        {
            Rect result = mol.BoundingBox;

            return CharacterExtents(mol, result);
        }

        public Run GenerateRun()
        {
            string module = $"{_product}.{_class}.{MethodBase.GetCurrentMethod().Name}()";
            _telemetry.Write(module, "Verbose", "Called");

            //start off progress monitoring
            Progress pb = new Progress();
            pb.TopLeft = _topLeft;

            //lists of objects for drawing
            _atomLabelCharacters = new List<AtomLabelCharacter>();
            _bondLines = new List<BondLine>();

            Stopwatch swr = new Stopwatch();

            //Create a run
            Run run = new Run();

            swr.Start();

            //set the median bond length
            _medianBondLength = _chemistryModel.MeanBondLength;
            if (_chemistryModel.GetAllBonds().Count == 0)
            {
                _medianBondLength = _options.BondLength;
            }

            int moleculeNo = 0;
            foreach (Molecule mol in _chemistryModel.Molecules.Values)
            {
                ProcessMolecule(mol, ref moleculeNo);
            }

            SetCanvasSize();

            if (_options.ClipLines)
            {
                #region Step 4 - Shrink bond lines

                ShrinkBondLinesPass1(pb);
                ShrinkBondLinesPass2(pb);

                #endregion Step 4 - Shrink bond lines
            }

            #region Step 5 - Create main OoXml drawing objects

            Drawing drawing1 = new Drawing();
            A.Graphic graphic1 = CreateGraphic();
            A.GraphicData graphicData1 = CreateGraphicData();
            Wpg.WordprocessingGroup wordprocessingGroup1 = new Wpg.WordprocessingGroup();

            // Create Inline Drawing using canvas extents
            Wp.Inline inline1 = CreateInline(wordprocessingGroup1);

            #endregion Step 5 - Create main OoXml drawing objects

            #region Step 5.1 - Show Molecule and Group Brackets

            // Render molecule grouping brackets
            if (_options.ShowMoleculeGroups)
            {
                foreach (var group in _boundingBoxesOfGroupedMolecules)
                {
                    string bracketColour = _options.ColouredAtoms ? "2A579A" : "000000";
                    DrawGroupBrackets(wordprocessingGroup1, group.BoundingBox, _medianBondLength / 2, OoXmlHelper.ACS_LINE_WIDTH * 2, bracketColour);
                }
            }

            // Render molecule brackets
            foreach (var moleculeBracket in _moleculeBrackets)
            {
                DrawMoleculeBrackets(wordprocessingGroup1, moleculeBracket, _medianBondLength / 10, OoXmlHelper.ACS_LINE_WIDTH * 2, "000000");
            }

            #endregion Step 5.1 - Show Molecule and Group Brackets

            #region Step 5a - Diagnostics

            if (_options.ShowMoleculeBoundingBoxes)
            {
                foreach (var box in _boundingBoxesOfMoleculeAtoms)
                {
                    DrawBox(wordprocessingGroup1, box, "ff0000", .75);
                }

                foreach (var box in _boundingBoxesOfMoleculesIncludingInternalCharacters)
                {
                    DrawBox(wordprocessingGroup1, box, "00ff00", .25);
                }

                DrawBox(wordprocessingGroup1, _boundingBoxOfAllAtoms, "ff0000", .25);

                DrawBox(wordprocessingGroup1, _boundingBoxOfAllCharacters, "000000", .25);

                //Point centre = new Point(_boundingBoxOfAllAtoms.Left + _boundingBoxOfAllAtoms.Width / 2, _boundingBoxOfAllAtoms.Top + _boundingBoxOfAllAtoms.Height / 2);
                //DrawArrow(wordprocessingGroup1, centre, _boundingBoxOfAllAtoms.TopLeft, "000000", 0.25);
                //DrawArrow(wordprocessingGroup1, centre, _boundingBoxOfAllAtoms.TopRight, "000000", 0.25);
                //DrawArrow(wordprocessingGroup1, centre, _boundingBoxOfAllAtoms.BottomLeft, "000000", 0.25);
                //DrawArrow(wordprocessingGroup1, centre, _boundingBoxOfAllAtoms.BottomRight, "000000", 0.25);
            }

            if (_options.ShowCharacterBoundingBoxes)
            {
                foreach (var atom in _chemistryModel.GetAllAtoms())
                {
                    List<AtomLabelCharacter> chars = _atomLabelCharacters.FindAll(a => a.ParentAtom.Equals(atom.Path));
                    Rect atomCharsRect = Rect.Empty;
                    foreach (var alc in chars)
                    {
                        Rect thisBoundingBox = thisBoundingBox = new Rect(alc.Position,
                            new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, _medianBondLength),
                                OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, _medianBondLength)));
                        if (alc.IsSmaller)
                        {
                            thisBoundingBox = new Rect(alc.Position,
                                new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, _medianBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR,
                                    OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, _medianBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR));
                        }

                        atomCharsRect.Union(thisBoundingBox);
                    }

                    if (!atomCharsRect.IsEmpty)
                    {
                        DrawBox(wordprocessingGroup1, atomCharsRect, "FFA500", 0.5);
                    }
                }
            }

            if (_options.ShowRingCentres)
            {
                ShowRingCentres(wordprocessingGroup1);
            }

            if (_options.ShowAtomPositions)
            {
                ShowAtomCentres(wordprocessingGroup1);
            }

            if (_options.ShowHulls)
            {
                ShowConvexHulls(wordprocessingGroup1);
            }

            #endregion Step 5a - Diagnostics

            #region Step 6 - Create and append OoXml objects for all Bond Lines

            AppendBondOoxml(pb, wordprocessingGroup1);

            #endregion Step 6 - Create and append OoXml objects for all Bond Lines

            #region Step 7 - Create and append OoXml objects for Atom Labels

            AppendAtomLabelOoxml(pb, wordprocessingGroup1);

            #endregion Step 7 - Create and append OoXml objects for Atom Labels

            #region Step 8 - Append OoXml drawing objects to OoXml run object

            AppendAllOoXml(graphicData1, wordprocessingGroup1, graphic1, inline1, drawing1, run);

            #endregion Step 8 - Append OoXml drawing objects to OoXml run object

            double abl = _medianBondLength;
            _telemetry.Write(module, "Timing", $"Rendering {_chemistryModel.Molecules.Count} molecules with {_chemistryModel.TotalAtomsCount} atoms and {_chemistryModel.TotalBondsCount} bonds took {swr.ElapsedMilliseconds.ToString("##,##0")} ms; Average Bond Length: {abl.ToString("#0.00")}");

            ShutDownProgress(pb);

            return run;

            // Local Function
            void ProcessMolecule(Molecule mol, ref int molNumber)
            {
                molNumber++;
                // Step 1- gather the atom information together

                ProcessAtoms(mol, pb, molNumber);

                // Step 2- gather the bond information together

                ProcessBonds(mol, pb, molNumber);

                // Populate diagnostic data
                foreach (Ring ring in mol.Rings)
                {
                    if (ring.Centroid.HasValue)
                    {
                        _ringCentres.Add(ring.Centroid.Value);
                    }
                }

                // Recurse into any child molecules
                foreach (var child in mol.Molecules.Values)
                {
                    ProcessMolecule(child, ref molNumber);
                }

                Rect boundingBoxOfAtoms = mol.BoundingBox;
                _boundingBoxesOfMoleculeAtoms.Add(boundingBoxOfAtoms);

                Rect boundingBoxOfCharacters = CharacterExtents(mol);
                _boundingBoxesOfMoleculesIncludingInternalCharacters.Add(boundingBoxOfCharacters);

                bool showBrackets = mol.ShowMoleculeBrackets.HasValue && mol.ShowMoleculeBrackets.Value
                                    || mol.Count.HasValue
                                    || mol.FormalCharge.HasValue
                                    || mol.SpinMultiplicity.HasValue;

                Rect outside = Inflate(boundingBoxOfCharacters, OoXmlHelper.DRAWING_MARGIN);
                if (showBrackets)
                {
                    _moleculeBrackets.Add(outside);
                }

                var alp = new AtomLabelPositioner(_medianBondLength, _atomLabelCharacters, _convexHulls, _TtfCharacterSet, _telemetry);
                TtfCharacter hydrogenCharacter = _TtfCharacterSet['H'];

                // We will use the length of this later to offset the dots
                string chargeText = string.Empty;

                if (mol.FormalCharge.HasValue && mol.FormalCharge.Value != 0)
                {
                    // Draw FormalCharge at top right
                    int charge = mol.FormalCharge.Value;
                    int absCharge = Math.Abs(charge);

                    if (absCharge > 1)
                    {
                        chargeText = absCharge.ToString();
                    }

                    if (charge >= 1)
                    {
                        chargeText += "+";
                    }
                    else if (charge <= 1)
                    {
                        chargeText += "-";
                    }

                    var point = new Point(outside.Right
                                          + OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE * _medianBondLength,
                                          outside.Top
                                          + OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Height, _medianBondLength) / 2);
                    alp.PlaceString(chargeText, point, mol.Path);
                }

                if (mol.SpinMultiplicity.HasValue && mol.SpinMultiplicity.Value > 1)
                {
                    // Draw SpinMultiplicity at top right (after FormalCharge if present)
                    string dots = string.Empty;

                    switch (mol.SpinMultiplicity.Value)
                    {
                        case 2:
                            dots = "•";
                            break;

                        case 3:
                            dots = "••";
                            break;
                    }

                    var point = new Point(outside.Right
                                          + OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Height, _medianBondLength) * chargeText.Length
                                          + OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE * _medianBondLength,
                                          outside.Top
                                          + OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Height, _medianBondLength) / 2);
                    alp.PlaceString(dots, point, mol.Path);
                }

                if (mol.Count.HasValue && mol.Count.Value > 0)
                {
                    // Draw Count at bottom right
                    var point = new Point(outside.Right
                                          + OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE * _medianBondLength,
                                          outside.Bottom
                                          + OoXmlHelper.ScaleCsTtfToCml(hydrogenCharacter.Height, _medianBondLength) / 2);
                    alp.PlaceString($"{mol.Count}", point, mol.Path);
                }

                // Handle optional rendering of molecule labels (outside of any brackets)
                if (_options.ShowMoleculeLabels && mol.Labels.Any())
                {
                    var point = new Point(outside.Left
                                          + outside.Width / 2,
                                          outside.Bottom
                                          + _medianBondLength * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE / 2);
                    alp.AddMoleculeLabels(mol.Labels.ToList(), point, mol.Path);
                }

                if (mol.Count.HasValue
                    || mol.FormalCharge.HasValue
                    || mol.SpinMultiplicity.HasValue
                    || _options.ShowMoleculeLabels && mol.Labels.Any())
                {
                    // Recalculate as we may have just added extra characters
                    boundingBoxOfCharacters = CharacterExtents(mol, outside);
                }
                _boundingBoxesOfMolecules.Add(new MoleculeBounds(mol.Path, boundingBoxOfCharacters));

                if (mol.IsGrouped)
                {
                    Rect boundingBox = Rect.Empty;

                    var childMolecules = _boundingBoxesOfMolecules.Where(g => g.Path.StartsWith(mol.Path)).ToList();
                    foreach (var child in childMolecules)
                    {
                        boundingBox.Union(child.BoundingBox);
                    }

                    var childGroups = _boundingBoxesOfGroupedMolecules.Where(g => g.Path.StartsWith(mol.Path)).ToList();
                    foreach (var child in childGroups)
                    {
                        boundingBox.Union(child.BoundingBox);
                    }

                    if (boundingBox != Rect.Empty)
                    {
                        _boundingBoxesOfGroupedMolecules.Add(new MoleculeBounds(mol.Path, Inflate(boundingBox, OoXmlHelper.DRAWING_MARGIN)));
                    }
                }
            }
        }

        // Helper function to create a ne inflated Rect
        private Rect Inflate(Rect r, double x)
        {
            Rect r1 = r;
            r1.Inflate(x, x);
            return r1;
        }

        private void ShowConvexHulls(Wpg.WordprocessingGroup wordprocessingGroup1)
        {
            foreach (var hull in _convexHulls)
            {
                var points = hull.Value.ToList();
                DrawPolygon(wordprocessingGroup1, points, 0.25, "ff0000");
            }
        }

        private void ShowAtomCentres(Wpg.WordprocessingGroup wordprocessingGroup1)
        {
            double xx = _medianBondLength * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE / 6;

            foreach (var molecule in _chemistryModel.Molecules.Values)
            {
                foreach (var atom in molecule.Atoms.Values)
                {
                    DrawAtomCentre(atom);
                }

                foreach (var childMolecule in molecule.Molecules.Values)
                {
                    DrawAtoms(childMolecule);
                }
            }

            //Local Function
            void DrawAtoms(Molecule molecule)
            {
                foreach (var atom in molecule.Atoms.Values)
                {
                    DrawAtomCentre(atom);
                }

                foreach (var childMolecule in molecule.Molecules.Values)
                {
                    foreach (var atom in childMolecule.Atoms.Values)
                    {
                        DrawAtomCentre(atom);
                    }
                }
            }

            //Local Function
            void DrawAtomCentre(Atom atom)
            {
                Rect bb = new Rect(new Point(atom.Position.X - xx, atom.Position.Y - xx), new Point(atom.Position.X + xx, atom.Position.Y + xx));
                DrawShape(wordprocessingGroup1, bb, A.ShapeTypeValues.Ellipse, "ff0000");
            }
        }

        private static void ShutDownProgress(Progress pb)
        {
            pb.Value = 0;
            pb.Hide();
            pb.Close();
        }

        private static void AppendAllOoXml(A.GraphicData graphicData, Wpg.WordprocessingGroup wordprocessingGroup, A.Graphic graphic,
            Wp.Inline inline, Drawing drawing, Run run)
        {
            graphicData.Append(wordprocessingGroup);
            graphic.Append(graphicData);
            inline.Append(graphic);
            drawing.Append(inline);
            run.Append(drawing);
        }

        private void AppendAtomLabelOoxml(Progress pb, Wpg.WordprocessingGroup wordprocessingGroup1)
        {
            AtomLabelRenderer alr = new AtomLabelRenderer(_boundingBoxOfAllCharacters, ref _ooxmlId, _options, _medianBondLength);

            if (_chemistryModel.TotalAtomsCount > 1)
            {
                pb.Show();
            }
            pb.Message = "Rendering Atoms";
            pb.Value = 0;
            pb.Maximum = _atomLabelCharacters.Count;

            foreach (AtomLabelCharacter alc in _atomLabelCharacters)
            {
                pb.Increment(1);
                alr.DrawCharacter(wordprocessingGroup1, alc);
            }
        }

        private void AppendBondOoxml(Progress pb, Wpg.WordprocessingGroup wordprocessingGroup1)
        {
            BondLineRenderer blr = new BondLineRenderer(_boundingBoxOfAllCharacters, ref _ooxmlId, _medianBondLength);

            if (_chemistryModel.TotalBondsCount > 1)
            {
                pb.Show();
            }
            pb.Message = "Rendering Bonds";
            pb.Value = 0;
            pb.Maximum = _bondLines.Count;

            foreach (BondLine bl in _bondLines)
            {
                pb.Increment(1);
                switch (bl.Style)
                {
                    case BondLineStyle.Wedge:
                    case BondLineStyle.Hatch:
                        blr.DrawWedgeBond(wordprocessingGroup1, bl);
                        break;

                    default:
                        blr.DrawBondLine(wordprocessingGroup1, bl);
                        break;
                }
            }
        }

        private void ShowRingCentres(Wpg.WordprocessingGroup wordprocessingGroup1)
        {
            double xx = _medianBondLength * OoXmlHelper.MULTIPLE_BOND_OFFSET_PERCENTAGE / 3;

            foreach (var point in _ringCentres)
            {
                Rect bb = new Rect(new Point(point.X - xx, point.Y - xx), new Point(point.X + xx, point.Y + xx));
                DrawShape(wordprocessingGroup1, bb, A.ShapeTypeValues.Ellipse, "0000ff");
            }
        }

        private void ShrinkBondLinesPass1(Progress pb)
        {
            // so that they do not overlap label characters

            if (_convexHulls.Count > 1)
            {
                pb.Show();
            }
            pb.Message = "Clipping Bond Lines - Pass 1";
            pb.Value = 0;
            pb.Maximum = _convexHulls.Count;

            foreach (var hull in _convexHulls)
            {
                pb.Increment(1);

                // select lines which start or end with this atom
                var targeted = from l in _bondLines
                               where (l.StartAtomPath == hull.Key | l.EndAtomPath == hull.Key)
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
                                _bondLines.Remove(bl);
                            }
                            break;
                    }
                }
            }
        }

        private void ShrinkBondLinesPass2(Progress pb)
        {
            // so that they do not overlap label characters

            if (_atomLabelCharacters.Count > 1)
            {
                pb.Show();
            }
            pb.Message = "Clipping Bond Lines - Pass 2";
            pb.Value = 0;
            pb.Maximum = _atomLabelCharacters.Count;

            foreach (AtomLabelCharacter alc in _atomLabelCharacters)
            {
                pb.Increment(1);

                double width = OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, _medianBondLength);
                double height = OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, _medianBondLength);

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

                var targeted = from l in _bondLines
                               where (cbb.Left <= l.BoundingBox.Right & l.BoundingBox.Right <= cbb.Right)
                                     | (cbb.Left <= l.BoundingBox.Left & l.BoundingBox.Left <= cbb.Right)
                                     | (cbb.Top <= l.BoundingBox.Top & l.BoundingBox.Top <= cbb.Bottom)
                                     | (cbb.Top <= l.BoundingBox.Bottom & l.BoundingBox.Bottom <= cbb.Bottom)
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
                                                      && bl.Bond.Stereo == Globals.BondStereo.Wedge || bl.Bond.Stereo == Globals.BondStereo.Hatch;
                            if (!ignoreWedgeOrHatch)
                            {
                                // Line was clipped at both ends;
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
                    _bondLines.Add(bl);
                }
            }
        }

        /// <summary>
        /// Sets the canvas size to accomodate any extra space required by label characters
        /// </summary>
        private void SetCanvasSize()
        {
            _boundingBoxOfAllCharacters = _boundingBoxOfAllAtoms;

            foreach (AtomLabelCharacter alc in _atomLabelCharacters)
            {
                if (alc.IsSubScript)
                {
                    Rect r = new Rect(alc.Position,
                        new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, _medianBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR,
                                 OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, _medianBondLength) * OoXmlHelper.SUBSCRIPT_SCALE_FACTOR));
                    _boundingBoxOfAllCharacters.Union(r);
                }
                else
                {
                    Rect r = new Rect(alc.Position,
                                      new Size(OoXmlHelper.ScaleCsTtfToCml(alc.Character.Width, _medianBondLength),
                                               OoXmlHelper.ScaleCsTtfToCml(alc.Character.Height, _medianBondLength)));
                    _boundingBoxOfAllCharacters.Union(r);
                }
            }

            foreach (var group in _boundingBoxesOfGroupedMolecules)
            {
                _boundingBoxOfAllCharacters.Union(group.BoundingBox);
            }

            _boundingBoxOfAllCharacters.Inflate(OoXmlHelper.DRAWING_MARGIN, OoXmlHelper.DRAWING_MARGIN);
        }

        private void ProcessBonds(Molecule mol, Progress pb, int moleculeNo)
        {
            BondLinePositioner br = new BondLinePositioner(_bondLines, _medianBondLength);

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
                br.CreateLines(bond);
            }

            // Rendering molecular sketches for publication quality output
            // Alex M Clark
            // Implement beautification of semi open double bonds and double bonds touching rings

            // Obtain list of Double Bonds with Placement of BondDirection.None
            List<Bond> doubleBonds = mol.Bonds.Where(b => b.OrderValue.Value == 2 && b.Placement == Globals.BondDirection.None).ToList();
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
            if ((Element)atom.Element == Globals.PeriodicTable.C)
            {
                if (atom.Bonds.ToList().Count == 3)
                {
                    bool isInRing = atom.IsInRing;
                    List<BondLine> lines = _bondLines.Where(bl => bl.ParentBond.Equals(bondPath)).ToList();
                    if (lines.Any())
                    {
                        List<Bond> otherLines;
                        if (isInRing)
                        {
                            otherLines = atom.Bonds.Where(b => !b.Id.Equals(bondPath)).ToList();
                        }
                        else
                        {
                            otherLines = atom.Bonds.Where(b => !b.Id.Equals(bondPath) && b.Order.Equals(Globals.OrderSingle)).ToList();
                        }

                        if (lines.Count == 2 && otherLines.Count == 2)
                        {
                            BondLine line1 = _bondLines.First(bl => bl.ParentBond.Equals(otherLines[0].Path));
                            BondLine line2 = _bondLines.First(bl => bl.ParentBond.Equals(otherLines[1].Path));
                            TrimLines(lines, line1, line2, isInRing);
                        }
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
            CoordinateTool.AdjustLineAboutMidpoint(ref startLonger, ref endLonger, _medianBondLength / 5);

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

        private void ProcessAtoms(Molecule mol, Progress pb, int moleculeNo)
        {
            AtomLabelPositioner ar = new AtomLabelPositioner(_medianBondLength, _atomLabelCharacters, _convexHulls, _TtfCharacterSet, _telemetry);

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
                    ar.CreateElementCharacters(atom, _options);
                }

                if (atom.Element is FunctionalGroup)
                {
                    ar.CreateFunctionalGroupCharacters(atom, _options);
                }
            }
        }

        private void LoadFont()
        {
            // Use either Arial or Tahoma
            string json = ResourceHelper.GetStringResource(Assembly.GetExecutingAssembly(), "Arial.json");
            _TtfCharacterSet = JsonConvert.DeserializeObject<Dictionary<char, TtfCharacter>>(json);
        }

        private A.Graphic CreateGraphic()
        {
            A.Graphic graphic = new A.Graphic();
            graphic.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");
            return graphic;
        }

        private A.GraphicData CreateGraphicData()
        {
            return new A.GraphicData() { Uri = "http://schemas.microsoft.com/office/word/2010/wordprocessingGroup" };
        }

        private Wp.Inline CreateInline(Wpg.WordprocessingGroup wordprocessingGroup)
        {
            UInt32Value inlineId = UInt32Value.FromUInt32((uint)_ooxmlId++);

            Int64Value width = OoXmlHelper.ScaleCmlToEmu(_boundingBoxOfAllCharacters.Width);
            Int64Value height = OoXmlHelper.ScaleCmlToEmu(_boundingBoxOfAllCharacters.Height);

            Wp.Inline inline = new Wp.Inline() { DistanceFromTop = (UInt32Value)0U, DistanceFromBottom = (UInt32Value)0U, DistanceFromLeft = (UInt32Value)0U, DistanceFromRight = (UInt32Value)0U };
            Wp.Extent extent = new Wp.Extent() { Cx = width, Cy = height };

            Wp.EffectExtent effectExtent = new Wp.EffectExtent() { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L };
            Wp.DocProperties docProperties = new Wp.DocProperties() { Id = inlineId, Name = "moleculeGroup" };

            Wpg.NonVisualGroupDrawingShapeProperties nonVisualGroupDrawingShapeProperties = new Wpg.NonVisualGroupDrawingShapeProperties();

            Wpg.GroupShapeProperties groupShapeProperties = new Wpg.GroupShapeProperties();

            A.TransformGroup transformGroup = new A.TransformGroup();
            A.Offset offset = new A.Offset { X = 0L, Y = 0L };
            A.Extents extents = new A.Extents { Cx = width, Cy = height };
            A.ChildOffset childOffset = new A.ChildOffset() { X = 0L, Y = 0L };
            A.ChildExtents childExtents = new A.ChildExtents() { Cx = width, Cy = height };

            transformGroup.Append(offset);
            transformGroup.Append(extents);
            transformGroup.Append(childOffset);
            transformGroup.Append(childExtents);

            groupShapeProperties.Append(transformGroup);
            wordprocessingGroup.Append(nonVisualGroupDrawingShapeProperties);
            wordprocessingGroup.Append(groupShapeProperties);

            inline.Append(extent);
            inline.Append(effectExtent);
            inline.Append(docProperties);

            return inline;
        }

        private void DrawBox(Wpg.WordprocessingGroup wordprocessingGroup, Rect cmlExtents, string colour, double points)
        {
            UInt32Value bondLineId = UInt32Value.FromUInt32((uint)_ooxmlId++);
            string bondLineName = "box" + bondLineId;

            Int64Value width = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value height = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
            Int64Value top = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value left = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

            Point location = new Point(left, top);
            Size size = new Size(width, height);
            location.Offset(OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfAllCharacters.Left), OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfAllCharacters.Top));
            Rect boundingBox = new Rect(location, size);

            width = (Int64Value)boundingBox.Width;
            height = (Int64Value)boundingBox.Height;
            top = (Int64Value)boundingBox.Top;
            left = (Int64Value)boundingBox.Left;

            Wps.WordprocessingShape shape = new Wps.WordprocessingShape();
            Wps.NonVisualDrawingProperties nonVisualDrawingProperties = new Wps.NonVisualDrawingProperties()
            {
                Id = bondLineId,
                Name = bondLineName
            };
            Wps.NonVisualDrawingShapeProperties nonVisualDrawingShapeProperties = new Wps.NonVisualDrawingShapeProperties();

            Wps.ShapeProperties shapeProperties = new Wps.ShapeProperties();

            A.Transform2D transform2D = new A.Transform2D();
            A.Offset offset = new A.Offset { X = left, Y = top };
            A.Extents extents = new A.Extents { Cx = width, Cy = height };

            transform2D.Append(offset);
            transform2D.Append(extents);

            A.CustomGeometry customGeometry = new A.CustomGeometry();
            A.AdjustValueList adjustValueList = new A.AdjustValueList();
            A.Rectangle rectangle = new A.Rectangle { Left = "l", Top = "t", Right = "r", Bottom = "b" };

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = width, Height = height };

            // Starting Point
            A.MoveTo moveTo = new A.MoveTo();
            A.Point point1 = new A.Point { X = "0", Y = "0" };
            moveTo.Append(point1);

            // Mid Point
            A.LineTo lineTo1 = new A.LineTo();
            A.Point point2 = new A.Point { X = boundingBox.Width.ToString("0"), Y = "0" };
            lineTo1.Append(point2);

            // Mid Point
            A.LineTo lineTo2 = new A.LineTo();
            A.Point point3 = new A.Point { X = boundingBox.Width.ToString("0"), Y = boundingBox.Height.ToString("0") };
            lineTo2.Append(point3);

            // Last Point
            A.LineTo lineTo3 = new A.LineTo();
            A.Point point4 = new A.Point { X = "0", Y = boundingBox.Height.ToString("0") };
            lineTo3.Append(point4);

            // Back to Start Point
            A.LineTo lineTo4 = new A.LineTo();
            A.Point point5 = new A.Point { X = "0", Y = "0" };
            lineTo4.Append(point5);

            path.Append(moveTo);
            path.Append(lineTo1);
            path.Append(lineTo2);
            path.Append(lineTo3);
            path.Append(lineTo4);

            pathList.Append(path);

            customGeometry.Append(adjustValueList);
            customGeometry.Append(rectangle);
            customGeometry.Append(pathList);

            Int32Value emus = (Int32Value)(points * OoXmlHelper.EMUS_PER_WORD_POINT);
            A.Outline outline = new A.Outline { Width = emus, CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();

            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
            solidFill.Append(rgbColorModelHex);

            outline.Append(solidFill);

            shapeProperties.Append(transform2D);
            shapeProperties.Append(customGeometry);
            shapeProperties.Append(outline);

            Wps.ShapeStyle shapeStyle = new Wps.ShapeStyle();
            A.LineReference lineReference = new A.LineReference { Index = (UInt32Value)0U };
            A.FillReference fillReference = new A.FillReference { Index = (UInt32Value)0U };
            A.EffectReference effectReference = new A.EffectReference { Index = (UInt32Value)0U };
            A.FontReference fontReference = new A.FontReference { Index = A.FontCollectionIndexValues.Minor };

            shapeStyle.Append(lineReference);
            shapeStyle.Append(fillReference);
            shapeStyle.Append(effectReference);
            shapeStyle.Append(fontReference);

            shape.Append(nonVisualDrawingProperties);
            shape.Append(nonVisualDrawingShapeProperties);
            shape.Append(shapeProperties);
            shape.Append(shapeStyle);

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            shape.Append(textBodyProperties);

            wordprocessingGroup.Append(shape);
        }

        private void DrawGroupBrackets(Wpg.WordprocessingGroup wordprocessingGroup, Rect cmlExtents, double armLength, double points, string colour)
        {
            UInt32Value id = UInt32Value.FromUInt32((uint)_ooxmlId++);
            string bondLineName = "bracket" + id;

            Int64Value width = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value height = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
            Int64Value top = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value left = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

            Point location = new Point(left, top);
            Size size = new Size(width, height);
            location.Offset(OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfAllCharacters.Left), OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfAllCharacters.Top));
            Rect boundingBox = new Rect(location, size);
            Int64Value armLengthEmu = OoXmlHelper.ScaleCmlToEmu(armLength);

            width = (Int64Value)boundingBox.Width;
            height = (Int64Value)boundingBox.Height;
            top = (Int64Value)boundingBox.Top;
            left = (Int64Value)boundingBox.Left;

            Wps.WordprocessingShape shape = new Wps.WordprocessingShape();
            Wps.NonVisualDrawingProperties nonVisualDrawingProperties = new Wps.NonVisualDrawingProperties
            {
                Id = id,
                Name = bondLineName
            };
            Wps.NonVisualDrawingShapeProperties nonVisualDrawingShapeProperties = new Wps.NonVisualDrawingShapeProperties();

            Wps.ShapeProperties shapeProperties = new Wps.ShapeProperties();

            A.Transform2D transform2D = new A.Transform2D();
            A.Offset offset = new A.Offset { X = left, Y = top };
            A.Extents extents = new A.Extents { Cx = width, Cy = height };

            transform2D.Append(offset);
            transform2D.Append(extents);

            A.CustomGeometry customGeometry = new A.CustomGeometry();
            A.AdjustValueList adjustValueList = new A.AdjustValueList();
            A.Rectangle rectangle = new A.Rectangle { Left = "l", Top = "t", Right = "r", Bottom = "b" };

            A.PathList pathList = new A.PathList();

            pathList.Append(MakeCorner(boundingBox, "TopLeft", armLengthEmu));
            pathList.Append(MakeCorner(boundingBox, "TopRight", armLengthEmu));
            pathList.Append(MakeCorner(boundingBox, "BottomLeft", armLengthEmu));
            pathList.Append(MakeCorner(boundingBox, "BottomRight", armLengthEmu));

            customGeometry.Append(adjustValueList);
            customGeometry.Append(rectangle);
            customGeometry.Append(pathList);

            Int32Value emus = (Int32Value)(points * OoXmlHelper.EMUS_PER_WORD_POINT);
            A.Outline outline = new A.Outline { Width = emus, CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();

            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
            solidFill.Append(rgbColorModelHex);

            outline.Append(solidFill);

            shapeProperties.Append(transform2D);
            shapeProperties.Append(customGeometry);
            shapeProperties.Append(outline);

            Wps.ShapeStyle shapeStyle = new Wps.ShapeStyle();
            A.LineReference lineReference = new A.LineReference { Index = (UInt32Value)0U };
            A.FillReference fillReference = new A.FillReference { Index = (UInt32Value)0U };
            A.EffectReference effectReference = new A.EffectReference { Index = (UInt32Value)0U };
            A.FontReference fontReference = new A.FontReference { Index = A.FontCollectionIndexValues.Minor };

            shapeStyle.Append(lineReference);
            shapeStyle.Append(fillReference);
            shapeStyle.Append(effectReference);
            shapeStyle.Append(fontReference);

            shape.Append(nonVisualDrawingProperties);
            shape.Append(nonVisualDrawingShapeProperties);
            shape.Append(shapeProperties);
            shape.Append(shapeStyle);

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            shape.Append(textBodyProperties);

            wordprocessingGroup.Append(shape);

            // Local function
            A.Path MakeCorner(Rect bbRect, string corner, double armsSize)
            {
                var path = new A.Path { Width = (Int64Value)bbRect.Width, Height = (Int64Value)bbRect.Height };

                A.Point p0 = new A.Point();
                A.Point p1 = new A.Point();
                A.Point p2 = new A.Point();

                switch (corner)
                {
                    case "TopLeft":
                        p0 = new A.Point
                        {
                            X = armsSize.ToString("0"),
                            Y = "0"
                        };
                        p1 = new A.Point
                        {
                            X = "0",
                            Y = "0"
                        };
                        p2 = new A.Point
                        {
                            X = "0",
                            Y = armsSize.ToString("0")
                        };
                        break;

                    case "TopRight":
                        p0 = new A.Point
                        {
                            X = (bbRect.Width - armsSize).ToString("0"),
                            Y = "0"
                        };
                        p1 = new A.Point
                        {
                            X = bbRect.Width.ToString("0"),
                            Y = "0"
                        };
                        p2 = new A.Point
                        {
                            X = bbRect.Width.ToString("0"),
                            Y = armsSize.ToString("0")
                        };
                        break;

                    case "BottomLeft":
                        p0 = new A.Point
                        {
                            X = "0",
                            Y = (bbRect.Height - armsSize).ToString("0")
                        };
                        p1 = new A.Point
                        {
                            X = "0",
                            Y = bbRect.Height.ToString("0")
                        };
                        p2 = new A.Point
                        {
                            X = armsSize.ToString("0"),
                            Y = bbRect.Height.ToString("0")
                        };
                        break;

                    case "BottomRight":
                        p0 = new A.Point
                        {
                            X = bbRect.Width.ToString("0"),
                            Y = (bbRect.Height - armsSize).ToString("0")
                        };
                        p1 = new A.Point
                        {
                            X = bbRect.Width.ToString("0"),
                            Y = bbRect.Height.ToString("0")
                        };
                        p2 = new A.Point
                        {
                            X = (bbRect.Width - armsSize).ToString("0"),
                            Y = bbRect.Height.ToString("0")
                        };
                        break;
                }

                var moveTo = new A.MoveTo();
                moveTo.Append(p0);
                path.Append(moveTo);

                var lineTo1 = new A.LineTo();
                lineTo1.Append(p1);
                path.Append(lineTo1);

                var lineTo2 = new A.LineTo();
                lineTo2.Append(p2);
                path.Append(lineTo2);

                return path;
            }
        }

        private void DrawMoleculeBrackets(Wpg.WordprocessingGroup wordprocessingGroup, Rect cmlExtents, double armLength, double points, string colour)
        {
            UInt32Value id = UInt32Value.FromUInt32((uint)_ooxmlId++);
            string bondLineName = "bracket" + id;

            Int64Value width = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value height = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
            Int64Value top = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value left = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

            Point location = new Point(left, top);
            Size size = new Size(width, height);
            location.Offset(OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfAllCharacters.Left), OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfAllCharacters.Top));
            Rect boundingBox = new Rect(location, size);
            Int64Value armLengthEmu = OoXmlHelper.ScaleCmlToEmu(armLength);

            width = (Int64Value)boundingBox.Width;
            height = (Int64Value)boundingBox.Height;
            top = (Int64Value)boundingBox.Top;
            left = (Int64Value)boundingBox.Left;

            Wps.WordprocessingShape shape = new Wps.WordprocessingShape();
            Wps.NonVisualDrawingProperties nonVisualDrawingProperties = new Wps.NonVisualDrawingProperties()
            {
                Id = id,
                Name = bondLineName
            };
            Wps.NonVisualDrawingShapeProperties nonVisualDrawingShapeProperties = new Wps.NonVisualDrawingShapeProperties();

            Wps.ShapeProperties shapeProperties = new Wps.ShapeProperties();

            A.Transform2D transform2D = new A.Transform2D();
            A.Offset offset = new A.Offset { X = left, Y = top };
            A.Extents extents = new A.Extents { Cx = width, Cy = height };

            transform2D.Append(offset);
            transform2D.Append(extents);

            A.CustomGeometry customGeometry = new A.CustomGeometry();
            A.AdjustValueList adjustValueList = new A.AdjustValueList();
            A.Rectangle rectangle = new A.Rectangle { Left = "l", Top = "t", Right = "r", Bottom = "b" };

            A.PathList pathList = new A.PathList();

            double gap = boundingBox.Width * 0.8;
            double leftSide = (width - gap) / 2;
            double rightSide = width - leftSide;

            // Left Path
            A.Path path1 = new A.Path { Width = width, Height = height };

            A.MoveTo moveTo = new A.MoveTo();
            A.Point point1 = new A.Point { X = leftSide.ToString("0"), Y = "0" };
            moveTo.Append(point1);

            // Mid Point
            A.LineTo lineTo1 = new A.LineTo();
            A.Point point2 = new A.Point { X = "0", Y = "0" };
            lineTo1.Append(point2);

            // Last Point
            A.LineTo lineTo2 = new A.LineTo();
            A.Point point3 = new A.Point { X = "0", Y = boundingBox.Height.ToString("0") };
            lineTo2.Append(point3);

            // Mid Point
            A.LineTo lineTo3 = new A.LineTo();
            A.Point point4 = new A.Point { X = leftSide.ToString("0"), Y = boundingBox.Height.ToString("0") };
            lineTo3.Append(point4);

            path1.Append(moveTo);
            path1.Append(lineTo1);
            path1.Append(lineTo2);
            path1.Append(lineTo3);

            pathList.Append(path1);

            // Right Path
            A.Path path2 = new A.Path { Width = width, Height = height };

            A.MoveTo moveTo2 = new A.MoveTo();
            A.Point point5 = new A.Point { X = rightSide.ToString("0"), Y = "0" };
            moveTo2.Append(point5);

            // Mid Point
            A.LineTo lineTo4 = new A.LineTo();
            A.Point point6 = new A.Point { X = boundingBox.Width.ToString("0"), Y = "0" };
            lineTo4.Append(point6);

            // Last Point
            A.LineTo lineTo5 = new A.LineTo();
            A.Point point7 = new A.Point { X = boundingBox.Width.ToString("0"), Y = boundingBox.Height.ToString("0") };
            lineTo5.Append(point7);

            // Mid Point
            A.LineTo lineTo6 = new A.LineTo();
            A.Point point8 = new A.Point { X = rightSide.ToString("0"), Y = boundingBox.Height.ToString("0") };
            lineTo6.Append(point8);

            path2.Append(moveTo2);
            path2.Append(lineTo4);
            path2.Append(lineTo5);
            path2.Append(lineTo6);

            pathList.Append(path2);

            customGeometry.Append(adjustValueList);
            customGeometry.Append(rectangle);
            customGeometry.Append(pathList);

            Int32Value emus = (Int32Value)(points * OoXmlHelper.EMUS_PER_WORD_POINT);
            A.Outline outline = new A.Outline { Width = emus, CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();

            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
            solidFill.Append(rgbColorModelHex);

            outline.Append(solidFill);

            shapeProperties.Append(transform2D);
            shapeProperties.Append(customGeometry);
            shapeProperties.Append(outline);

            Wps.ShapeStyle shapeStyle = new Wps.ShapeStyle();
            A.LineReference lineReference = new A.LineReference { Index = (UInt32Value)0U };
            A.FillReference fillReference = new A.FillReference { Index = (UInt32Value)0U };
            A.EffectReference effectReference = new A.EffectReference { Index = (UInt32Value)0U };
            A.FontReference fontReference = new A.FontReference { Index = A.FontCollectionIndexValues.Minor };

            shapeStyle.Append(lineReference);
            shapeStyle.Append(fillReference);
            shapeStyle.Append(effectReference);
            shapeStyle.Append(fontReference);

            shape.Append(nonVisualDrawingProperties);
            shape.Append(nonVisualDrawingShapeProperties);
            shape.Append(shapeProperties);
            shape.Append(shapeStyle);

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            shape.Append(textBodyProperties);

            wordprocessingGroup.Append(shape);
        }

        private void DrawPolygon(Wpg.WordprocessingGroup wordprocessingGroup, List<Point> vertices, double points, string colour)
        {
            UInt32Value bondLineId = UInt32Value.FromUInt32((uint)_ooxmlId++);
            string bondLineName = "diag-polygon-" + bondLineId;

            Rect cmlExtents = new Rect(vertices[0], vertices[vertices.Count - 1]);

            for (int i = 0; i < vertices.Count - 1; i++)
            {
                cmlExtents.Union(new Rect(vertices[i], vertices[i + 1]));
            }

            // Move Extents to have 0,0 Top Left Reference
            cmlExtents.Offset(-_boundingBoxOfAllCharacters.Left, -_boundingBoxOfAllCharacters.Top);

            Int64Value width = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value height = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
            Int64Value top = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value left = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

            Wps.WordprocessingShape shape = new Wps.WordprocessingShape();
            Wps.NonVisualDrawingProperties nonVisualDrawingProperties = new Wps.NonVisualDrawingProperties() { Id = bondLineId, Name = bondLineName };
            Wps.NonVisualDrawingShapeProperties nonVisualDrawingShapeProperties = new Wps.NonVisualDrawingShapeProperties();

            Wps.ShapeProperties shapeProperties = new Wps.ShapeProperties();

            A.Transform2D transform2D = new A.Transform2D();
            A.Offset offset = new A.Offset { X = left, Y = top };
            A.Extents extents = new A.Extents { Cx = width, Cy = height };

            transform2D.Append(offset);
            transform2D.Append(extents);

            A.CustomGeometry customGeometry = new A.CustomGeometry();
            A.AdjustValueList adjustValueList = new A.AdjustValueList();
            A.Rectangle rectangle = new A.Rectangle { Left = "l", Top = "t", Right = "r", Bottom = "b" };

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = width, Height = height };

            // Local Function
            A.Point MakePoint(Point point)
            {
                Point startPoint = point;
                startPoint.Offset(-_boundingBoxOfAllCharacters.Left, -_boundingBoxOfAllCharacters.Top);
                startPoint.Offset(-cmlExtents.Left, -cmlExtents.Top);
                return new A.Point { X = OoXmlHelper.ScaleCmlToEmu(startPoint.X).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(startPoint.Y).ToString() };
            }

            // First point
            A.MoveTo moveTo = new A.MoveTo();
            moveTo.Append(MakePoint(vertices[0]));
            path.Append(moveTo);

            // Remaining points
            for (int i = 1; i < vertices.Count; i++)
            {
                A.LineTo lineTo = new A.LineTo();
                lineTo.Append(MakePoint(vertices[i]));
                path.Append(lineTo);
            }

            // Close the path
            A.CloseShapePath closeShapePath = new A.CloseShapePath();
            path.Append(closeShapePath);

            pathList.Append(path);

            customGeometry.Append(adjustValueList);
            customGeometry.Append(rectangle);
            customGeometry.Append(pathList);

            Int32Value emus = (Int32Value)(points * OoXmlHelper.EMUS_PER_WORD_POINT);
            A.Outline outline = new A.Outline { Width = emus, CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();

            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
            A.Alpha alpha = new A.Alpha() { Val = new Int32Value() { InnerText = "100%" } };

            rgbColorModelHex.Append(alpha);

            solidFill.Append(rgbColorModelHex);

            outline.Append(solidFill);

            shapeProperties.Append(transform2D);
            shapeProperties.Append(customGeometry);
            shapeProperties.Append(outline);

            OoXmlHelper.AppendShapeStyle(shape, nonVisualDrawingProperties, nonVisualDrawingShapeProperties, shapeProperties);
            wordprocessingGroup.Append(shape);
        }

        private void DrawLine(Wpg.WordprocessingGroup wordprocessingGroup, Point startPoint, Point endPoint, string colour, double points)
        {
            UInt32Value bondLineId = UInt32Value.FromUInt32((uint)_ooxmlId++);
            string bondLineName = "diag-line-" + bondLineId;

            Rect cmlExtents = new Rect(startPoint, endPoint);

            // Move Bond Line Extents and Points to have 0,0 Top Left Reference
            startPoint.Offset(-_boundingBoxOfAllCharacters.Left, -_boundingBoxOfAllCharacters.Top);
            endPoint.Offset(-_boundingBoxOfAllCharacters.Left, -_boundingBoxOfAllCharacters.Top);
            cmlExtents.Offset(-_boundingBoxOfAllCharacters.Left, -_boundingBoxOfAllCharacters.Top);

            // Move points into New Bond Line Extents
            startPoint.Offset(-cmlExtents.Left, -cmlExtents.Top);
            endPoint.Offset(-cmlExtents.Left, -cmlExtents.Top);

            Int64Value width = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value height = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
            Int64Value top = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value left = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

            Wps.WordprocessingShape wordprocessingShape = new Wps.WordprocessingShape();
            Wps.NonVisualDrawingProperties nonVisualDrawingProperties = new Wps.NonVisualDrawingProperties() { Id = bondLineId, Name = bondLineName };
            Wps.NonVisualDrawingShapeProperties nonVisualDrawingShapeProperties = new Wps.NonVisualDrawingShapeProperties();

            Wps.ShapeProperties shapeProperties = new Wps.ShapeProperties();

            A.Transform2D transform2D = new A.Transform2D();
            A.Offset offset = new A.Offset { X = left, Y = top };
            A.Extents extents = new A.Extents { Cx = width, Cy = height };

            transform2D.Append(offset);
            transform2D.Append(extents);

            A.CustomGeometry customGeometry = new A.CustomGeometry();
            A.AdjustValueList adjustValueList = new A.AdjustValueList();
            A.Rectangle rectangle = new A.Rectangle { Left = "l", Top = "t", Right = "r", Bottom = "b" };

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = width, Height = height };

            A.MoveTo moveTo = new A.MoveTo();
            A.Point point1 = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(startPoint.X).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(startPoint.Y).ToString() };
            moveTo.Append(point1);

            A.LineTo lineTo = new A.LineTo();
            A.Point point2 = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(endPoint.X).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(endPoint.Y).ToString() };
            lineTo.Append(point2);

            path.Append(moveTo);
            path.Append(lineTo);

            pathList.Append(path);

            customGeometry.Append(adjustValueList);
            customGeometry.Append(rectangle);
            customGeometry.Append(pathList);

            Int32Value emus = (Int32Value)(points * OoXmlHelper.EMUS_PER_WORD_POINT);
            A.Outline outline = new A.Outline { Width = emus, CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();

            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
            A.Alpha alpha = new A.Alpha() { Val = new Int32Value() { InnerText = "100%" } };

            rgbColorModelHex.Append(alpha);

            solidFill.Append(rgbColorModelHex);

            outline.Append(solidFill);

            shapeProperties.Append(transform2D);
            shapeProperties.Append(customGeometry);
            shapeProperties.Append(outline);

            Wps.ShapeStyle shapeStyle = new Wps.ShapeStyle();
            A.LineReference lineReference = new A.LineReference { Index = (UInt32Value)0U };
            A.FillReference fillReference = new A.FillReference { Index = (UInt32Value)0U };
            A.EffectReference effectReference = new A.EffectReference { Index = (UInt32Value)0U };
            A.FontReference fontReference = new A.FontReference { Index = A.FontCollectionIndexValues.Minor };

            shapeStyle.Append(lineReference);
            shapeStyle.Append(fillReference);
            shapeStyle.Append(effectReference);
            shapeStyle.Append(fontReference);

            wordprocessingShape.Append(nonVisualDrawingProperties);
            wordprocessingShape.Append(nonVisualDrawingShapeProperties);
            wordprocessingShape.Append(shapeProperties);
            wordprocessingShape.Append(shapeStyle);

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            wordprocessingGroup.Append(wordprocessingShape);
        }

        private void DrawArrow(Wpg.WordprocessingGroup wordprocessingGroup, Point startPoint, Point endPoint, string colour, double points)
        {
            UInt32Value id = UInt32Value.FromUInt32((uint)_ooxmlId++);
            string bondLineName = "arrow-" + id;

            Rect cmlExtents = new Rect(startPoint, endPoint);

            // Move Bond Line Extents and Points to have 0,0 Top Left Reference
            startPoint.Offset(-_boundingBoxOfAllCharacters.Left, -_boundingBoxOfAllCharacters.Top);
            endPoint.Offset(-_boundingBoxOfAllCharacters.Left, -_boundingBoxOfAllCharacters.Top);
            cmlExtents.Offset(-_boundingBoxOfAllCharacters.Left, -_boundingBoxOfAllCharacters.Top);

            // Move points into New Bond Line Extents
            startPoint.Offset(-cmlExtents.Left, -cmlExtents.Top);
            endPoint.Offset(-cmlExtents.Left, -cmlExtents.Top);

            Int64Value width = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value height = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
            Int64Value top = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value left = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

            Wps.WordprocessingShape shape = new Wps.WordprocessingShape();
            Wps.NonVisualDrawingProperties nonVisualDrawingProperties = new Wps.NonVisualDrawingProperties() { Id = id, Name = bondLineName };
            Wps.NonVisualDrawingShapeProperties nonVisualDrawingShapeProperties = new Wps.NonVisualDrawingShapeProperties();

            Wps.ShapeProperties shapeProperties = new Wps.ShapeProperties();

            A.Transform2D transform2D = new A.Transform2D();
            A.Offset offset = new A.Offset { X = left, Y = top };
            A.Extents extents = new A.Extents { Cx = width, Cy = height };

            transform2D.Append(offset);
            transform2D.Append(extents);

            A.CustomGeometry customGeometry = new A.CustomGeometry();
            A.AdjustValueList adjustValueList = new A.AdjustValueList();
            A.Rectangle rectangle = new A.Rectangle { Left = "l", Top = "t", Right = "r", Bottom = "b" };

            A.PathList pathList = new A.PathList();

            A.Path path = new A.Path { Width = width, Height = height };

            A.MoveTo moveTo = new A.MoveTo();
            A.Point point1 = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(startPoint.X).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(startPoint.Y).ToString() };
            moveTo.Append(point1);

            A.LineTo lineTo = new A.LineTo();
            A.Point point2 = new A.Point { X = OoXmlHelper.ScaleCmlToEmu(endPoint.X).ToString(), Y = OoXmlHelper.ScaleCmlToEmu(endPoint.Y).ToString() };
            lineTo.Append(point2);

            path.Append(moveTo);
            path.Append(lineTo);

            pathList.Append(path);

            customGeometry.Append(adjustValueList);
            customGeometry.Append(rectangle);
            customGeometry.Append(pathList);

            Int32Value emus = (Int32Value)(points * OoXmlHelper.EMUS_PER_WORD_POINT);
            A.Outline outline = new A.Outline { Width = emus, CapType = A.LineCapValues.Round };

            A.SolidFill solidFill = new A.SolidFill();

            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
            A.Alpha alpha = new A.Alpha() { Val = new Int32Value() { InnerText = "100%" } };

            rgbColorModelHex.Append(alpha);

            solidFill.Append(rgbColorModelHex);

            outline.Append(solidFill);

            A.TailEnd tailEnd = new A.TailEnd() { Type = A.LineEndValues.Arrow };
            outline.Append(tailEnd);

            shapeProperties.Append(transform2D);
            shapeProperties.Append(customGeometry);
            shapeProperties.Append(outline);

            Wps.ShapeStyle shapeStyle = new Wps.ShapeStyle();
            A.LineReference lineReference = new A.LineReference { Index = (UInt32Value)0U };
            A.FillReference fillReference = new A.FillReference { Index = (UInt32Value)0U };
            A.EffectReference effectReference = new A.EffectReference { Index = (UInt32Value)0U };
            A.FontReference fontReference = new A.FontReference { Index = A.FontCollectionIndexValues.Minor };

            shapeStyle.Append(lineReference);
            shapeStyle.Append(fillReference);
            shapeStyle.Append(effectReference);
            shapeStyle.Append(fontReference);

            shape.Append(nonVisualDrawingProperties);
            shape.Append(nonVisualDrawingShapeProperties);
            shape.Append(shapeProperties);
            shape.Append(shapeStyle);

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            shape.Append(textBodyProperties);

            wordprocessingGroup.Append(shape);
        }

        private void DrawShape(Wpg.WordprocessingGroup wordprocessingGroup, Rect cmlExtents, A.ShapeTypeValues shape, string colour)
        {
            UInt32Value id = UInt32Value.FromUInt32((uint)_ooxmlId++);
            string bondLineName = "shape" + id;

            Int64Value width = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Width);
            Int64Value height = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Height);
            Int64Value top = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Top);
            Int64Value left = OoXmlHelper.ScaleCmlToEmu(cmlExtents.Left);

            Point location = new Point(left, top);
            Size size = new Size(width, height);
            location.Offset(OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfAllCharacters.Left), OoXmlHelper.ScaleCmlToEmu(-_boundingBoxOfAllCharacters.Top));
            Rect boundingBox = new Rect(location, size);

            width = (Int64Value)boundingBox.Width;
            height = (Int64Value)boundingBox.Height;
            top = (Int64Value)boundingBox.Top;
            left = (Int64Value)boundingBox.Left;

            A.PresetGeometry presetGeometry = null;
            A.Extents extents = new A.Extents { Cx = width, Cy = height };
            presetGeometry = new A.PresetGeometry() { Preset = shape };

            Wps.WordprocessingShape wordprocessingShape = new Wps.WordprocessingShape();
            Wps.NonVisualDrawingProperties nonVisualDrawingProperties = new Wps.NonVisualDrawingProperties()
            {
                Id = id,
                Name = bondLineName
            };

            Wps.NonVisualDrawingShapeProperties nonVisualDrawingShapeProperties = new Wps.NonVisualDrawingShapeProperties();

            Wps.ShapeProperties shapeProperties = new Wps.ShapeProperties();

            A.Transform2D transform2D = new A.Transform2D();
            A.Offset offset = new A.Offset { X = left, Y = top };

            transform2D.Append(offset);
            transform2D.Append(extents);

            A.AdjustValueList adjustValueList = new A.AdjustValueList();

            presetGeometry.Append(adjustValueList);
            A.SolidFill solidFill = new A.SolidFill();

            A.RgbColorModelHex rgbColorModelHex = new A.RgbColorModelHex { Val = colour };
            solidFill.Append(rgbColorModelHex);

            shapeProperties.Append(transform2D);
            shapeProperties.Append(presetGeometry);
            shapeProperties.Append(solidFill);

            Wps.ShapeStyle shapeStyle = new Wps.ShapeStyle();
            A.LineReference lineReference = new A.LineReference { Index = (UInt32Value)0U };
            A.FillReference fillReference = new A.FillReference { Index = (UInt32Value)0U };
            A.EffectReference effectReference = new A.EffectReference { Index = (UInt32Value)0U };
            A.FontReference fontReference = new A.FontReference { Index = A.FontCollectionIndexValues.Minor };

            shapeStyle.Append(lineReference);
            shapeStyle.Append(fillReference);
            shapeStyle.Append(effectReference);
            shapeStyle.Append(fontReference);

            wordprocessingShape.Append(nonVisualDrawingProperties);
            wordprocessingShape.Append(nonVisualDrawingShapeProperties);
            wordprocessingShape.Append(shapeProperties);
            wordprocessingShape.Append(shapeStyle);

            Wps.TextBodyProperties textBodyProperties = new Wps.TextBodyProperties();
            wordprocessingShape.Append(textBodyProperties);

            wordprocessingGroup.Append(wordprocessingShape);
        }
    }
}