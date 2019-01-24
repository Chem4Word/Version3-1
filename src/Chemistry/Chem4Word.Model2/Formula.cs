// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Xml.Linq;

namespace Chem4Word.Model2
{
    public class Formula
    {
        public string Id { get; set; }

        public string Convention { get; set; }

        public string Inline { get; set; }

        public bool IsValid { get; set; }

        public Formula()
        {
        }

        public Formula(XElement cmlElement)
        {
            if (cmlElement.Attribute("id") != null)
            {
                Id = cmlElement.Attribute("id")?.Value;
            }

            if (cmlElement.Attribute("convention") == null)
            {
                Convention = "chem4word:Formula";
            }
            else
            {
                Convention = cmlElement.Attribute("convention")?.Value;
            }

            // Correct import from legacy Add-In
            if (string.IsNullOrEmpty(Convention))
            {
                Convention = "chem4word:Formula";
            }
            if (cmlElement.Attribute("inline") != null)
            {
                Inline = cmlElement.Attribute("inline")?.Value;
                IsValid = true;
            }
        }
    }
}