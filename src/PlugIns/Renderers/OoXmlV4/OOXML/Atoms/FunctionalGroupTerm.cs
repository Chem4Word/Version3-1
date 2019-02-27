// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace Chem4Word.Renderer.OoXmlV4.OOXML.Atoms
{

    public class FunctionalGroupTerm
    {
        public List<AtomLabelCharacter> Characters { get; set; }
        public bool IsAnchor { get; set; }

        public FunctionalGroupTerm()
        {
            Characters = new List<AtomLabelCharacter>();
        }
    }
}