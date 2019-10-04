// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.OOXML
{
    public class MoleculeExtents
    {
        private Rect _internalCharacterExtents;
        private Rect _moleculeBracketsExtents;
        private Rect _externalCharacterExtents;
        private Rect _groupBracketsExtents;

        public string Path { get; set; }

        /// <summary>
        /// BoundingBox of this molecule's Atom.Position(s)
        /// </summary>
        public Rect AtomExtents { get; }

        /// <summary>
        /// BoundingBox of Atom points and Atom label characters
        /// This will be a Union of AtomExtents and bounding box of each AtomLabelCharacter(s) belonging to this molecule
        /// </summary>
        public Rect InternalCharacterExtents
        {
            get => _internalCharacterExtents;
        }

        /// <summary>
        /// Where to draw Molecule Brackets
        /// If brackets are not being drawn this will be equal to InternalCharacterExtents
        /// If brackets are being drawn this will be equal to InternalCharacterExtents + margin
        /// </summary>
        public Rect MoleculeBracketsExtents
        {
            get => _moleculeBracketsExtents;
        }

        /// <summary>
        /// BoundingBox of molecule and it's external characters
        /// </summary>
        public Rect ExternalCharacterExtents
        {
            get => _externalCharacterExtents;
        }

        /// <summary>
        /// Where to draw Molecule Group Brackets
        /// If brackets are not being drawn this will be equal to ExternalCharacterExtents
        /// If brackets are being drawn this will be equal to ExternalCharacterExtents + margin
        /// </summary>
        public Rect GroupBracketsExtents
        {
            get => _groupBracketsExtents;
        }

        public MoleculeExtents(string path, Rect extents)
        {
            Path = path;

            AtomExtents = extents;

            _internalCharacterExtents = extents;
            _moleculeBracketsExtents = extents;
            _externalCharacterExtents = extents;
            _groupBracketsExtents = extents;
        }

        public override string ToString()
        {
            return $"{Path}, {AtomExtents}";
        }

        public void SetInternalCharacterExtents(Rect extents)
        {
            _internalCharacterExtents = extents;
            _moleculeBracketsExtents = extents;
            _externalCharacterExtents = extents;
            _groupBracketsExtents = extents;
        }

        public void SetMoleculeBracketExtents(Rect extents)
        {
            _moleculeBracketsExtents = extents;
            _externalCharacterExtents = extents;
            _groupBracketsExtents = extents;
        }

        public void SetExternalCharacterExtents(Rect extents)
        {
            _externalCharacterExtents = extents;
            _groupBracketsExtents = extents;
        }

        public void SetGroupBracketExtents(Rect extents)
        {
            _groupBracketsExtents = extents;
        }
    }
}