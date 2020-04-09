// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;
using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class PositionerOutputs
    {
        public List<AtomLabelCharacter> AtomLabelCharacters { get; set; } = new List<AtomLabelCharacter>();
        public Dictionary<string, List<Point>> ConvexHulls { get; set; } = new Dictionary<string, List<Point>>();
        public List<BondLine> BondLines { get; set; } = new List<BondLine>();
        public List<Point> RingCenters { get; set; } = new List<Point>();
        public List<MoleculeExtents> AllMoleculeExtents { get; set; } = new List<MoleculeExtents>();
        public List<Rect> GroupBrackets { get; set; } = new List<Rect>();
        public List<Rect> MoleculeBrackets { get; set; } = new List<Rect>();
        public List<OoXmlString> MoleculeLabels { get; set; } = new List<OoXmlString>();
    }
}