// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Xml.Linq;

namespace Chem4Word.Model2
{
    public class ChemicalName
    {
        public string Id { get; set; }

        public string DictRef { get; set; }

        public string Name { get; set; }

        public bool IsValid { get; set; }

        public ChemicalName()
        {
        }

        public ChemicalName(XElement cmlElement) : this()
        {
            ChemicalName n = new ChemicalName();

            Id = cmlElement.Attribute("id")?.Value;

            if (cmlElement.Attribute("dictRef") == null)
            {
                DictRef = "chem4word:Synonym";
            }
            else
            {
                DictRef = cmlElement.Attribute("dictRef")?.Value;
            }

            // Correct import from legacy Add-In
            if (string.IsNullOrEmpty(DictRef) || DictRef.Equals("nameDict:unknown"))
            {
                DictRef = "chem4word:Synonym";
            }

            Name = cmlElement.Value;
        }
    }
}