// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Windows;

namespace Chem4Word.Renderer.OoXmlV4.OOXML
{
    public class MoleculeBounds
    {
        public string Path { get; set; }

        public Rect BoundingBox { get; set; }

        public MoleculeBounds(string path, Rect boundingBox)
        {
            Path = path;
            BoundingBox = boundingBox;
        }

        public override string ToString()
        {
            return $"{Path}, {BoundingBox}";
        }
    }
}