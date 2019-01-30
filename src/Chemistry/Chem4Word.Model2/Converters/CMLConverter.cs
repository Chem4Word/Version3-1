// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Xml.Linq;
using Chem4Word.Model2.Interfaces;

namespace Chem4Word.Model2.Converters
{
    public class CMLConverter
    {
        public static string Description => "Chemical Markup Language";

        public static string[] Extensions => new[]
        {
            "*.CML",
            "*.XML"
        };

        public Model Import(object data)
        {
            Model newModel = new Model();

            if (data != null)
            {
                XDocument modelDoc = XDocument.Parse((string)data);
                var root = modelDoc.Root;

                // Only import if not null
                var customXmlPartGuid = CML.GetCustomXmlPartGuid(root);
                if (customXmlPartGuid != null && !string.IsNullOrEmpty(customXmlPartGuid.Value))
                {
                    newModel.CustomXmlPartGuid = customXmlPartGuid.Value;
                }

                var moleculeElements = CML.GetMolecules(root);

                foreach (XElement meElement in moleculeElements)
                {
                    var newMol = new Molecule(meElement);

                    AddMolecule(newModel, newMol);
                    newMol.Parent = (IChemistryContainer)newModel;
                }

                foreach (Molecule molecule in newModel.Molecules.Values)
                {
                    // Force ConciseFormula to be calculated
                    // molecule.ConciseFormula = molecule.CalculatedFormula();
                }
                //newModel.Relabel(true);

                newModel.Refresh();
            }

            return newModel;
        }

        private static Molecule AddMolecule(Model newModel, Molecule newMol)
        {
            return newModel.AddMolecule(newMol);
        }
    }
}