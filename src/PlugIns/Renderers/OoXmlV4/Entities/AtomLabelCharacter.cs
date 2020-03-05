// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Windows;
using Chem4Word.Renderer.OoXmlV4.TTF;

namespace Chem4Word.Renderer.OoXmlV4.Entities
{
    public class AtomLabelCharacter
    {
        public string ParentMolecule { get; set; }
        public string ParentAtom { get; set; }
        public Point Position { get; set; }
        public TtfCharacter Character { get; set; }
        public String Colour { get; set; }
        public bool IsSmaller { get; set; }
        public bool IsSubScript { get; set; }
        public bool IsSuperScript { get; set; }

        public AtomLabelCharacter(Point position, TtfCharacter character, String colour, string parentAtom, string parentMolecule)
        {
            Position = position;
            Character = character;
            Colour = colour;
            ParentAtom = parentAtom;
            ParentMolecule = parentMolecule;
        }

        public override string ToString()
        {
            return $"{Character.Character} of {ParentAtom} @ {Position.X:#,##0.000},{Position.Y:#,##0.000}";
        }
    }
}