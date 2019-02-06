// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

namespace Chem4Word.Model2.Converters.CML
{
    // ReSharper disable once InconsistentNaming
    public static class Helper
    {
        // ReSharper disable once InconsistentNaming
        public static XDocument LoadCML(string cml)
        {
            return XDocument.Parse(cml);
        }

        public static int? GetIsotopeNumber(XElement cmlElement)
        {
            int isotopeNumber;

            if (int.TryParse(cmlElement.Attribute(Constants.TagIsotopeNumber)?.Value, out isotopeNumber))
            {
                return isotopeNumber;
            }
            else
            {
                return null;
            }
        }

        internal static ElementBase GetChemicalElement(XElement cmlElement, out string message)
        {
            message = "";
            XAttribute xa = cmlElement.Attribute(Constants.TagElementType);
            if (xa != null)
            {
                string symbol = xa.Value;

                //try to return a chemical element from the Periodic Table
                if (Globals.PeriodicTable.HasElement(symbol))
                {
                    return Globals.PeriodicTable.Elements[symbol];
                }

                //if that fails, see if it's a functional group
                FunctionalGroup functionalGroup;
                if (FunctionalGroups.TryParse(symbol, out functionalGroup))
                {
                    return functionalGroup;
                }

                //if we got here then it went very wrong
                message = $"Unrecognised element '{symbol}' in {cmlElement}";
            }
            else
            {
                message = $"cml attribute 'elementType' missing from {cmlElement}";
            }

            return null;
        }

        internal static XElement GetCustomXmlPartGuid(XElement doc)
        {
            var id1 = from XElement xe in doc.Elements(Constants.TagXMLPartGuid) select xe;
            var id2 = from XElement xe in doc.Elements(Namespaces.c4w + Constants.TagXMLPartGuid) select xe;
            return id1.Union(id2).FirstOrDefault();
        }

        // ReSharper disable once InconsistentNaming
        internal static List<XElement> GetMolecules(XElement doc)
        {
            var mols = from XElement xe in doc.Elements(Constants.TagMolecule) select xe;
            var mols2 = from XElement xe2 in doc.Elements(Namespaces.cml + Constants.TagMolecule) select xe2;
            return mols.Union(mols2).ToList();
        }

        internal static List<XElement> GetAtoms(XElement mol)
        {
            // Task 336
            var aa1 = from a in mol.Elements(Constants.TagAtomArray) select a;
            var aa2 = from a in mol.Elements(Namespaces.cml + Constants.TagAtomArray) select a;
            var aa = aa1.Union(aa2);

            if (aa.Count() == 0)
            {
                // Bare Atoms without AtomArray
                var atoms1 = from a in mol.Elements(Constants.TagAtom) select a;
                var atoms2 = from a in mol.Elements(Namespaces.cml + Constants.TagAtom) select a;
                return atoms1.Union(atoms2).ToList();
            }
            else
            {
                // Atoms inside AtomArray
                var atoms1 = from a in aa.Elements(Constants.TagAtom) select a;
                var atoms2 = from a in aa.Elements(Namespaces.cml + Constants.TagAtom) select a;
                return atoms1.Union(atoms2).ToList();
            }
        }

        internal static List<XElement> GetBonds(XElement mol)
        {
            // Task 336
            var ba1 = from b in mol.Elements(Constants.TagBondArray) select b;
            var ba2 = from b in mol.Elements(Namespaces.cml + Constants.TagBondArray) select b;
            var ba = ba1.Union(ba2);

            if (ba.Count() == 0)
            {
                // Bare bonds without BondArray
                var bonds1 = from b in mol.Elements(Constants.TagBond) select b;
                var bonds2 = from b in mol.Elements(Namespaces.cml + Constants.TagBond) select b;
                return bonds1.Union(bonds2).ToList();
            }
            else
            {
                // Bonds inside BondArray
                var bonds1 = from b in ba.Elements(Constants.TagBond) select b;
                var bonds2 = from b in ba.Elements(Namespaces.cml + Constants.TagBond) select b;
                return bonds1.Union(bonds2).ToList();
            }
        }

        internal static List<XElement> GetStereo(XElement bond)
        {
            var stereo = from s in bond.Elements(Constants.TagBondStereo) select s;
            var stereo2 = from s in bond.Elements(Namespaces.cml + Constants.TagBondStereo) select s;
            return stereo.Union(stereo2).ToList();
        }

        internal static List<XElement> GetNames(XElement mol)
        {
            var names1 = from n1 in mol.Elements(Constants.TagName) select n1;
            var names2 = from n2 in mol.Elements(Namespaces.cml + Constants.TagName) select n2;
            return names1.Union(names2).ToList();
        }

        internal static List<XElement> GetFormulas(XElement mol)
        {
            var formulae1 = from f1 in mol.Elements(Constants.TagFormula) select f1;
            var formulae2 = from f2 in mol.Elements(Namespaces.cml + Constants.TagFormula) select f2;
            return formulae1.Union(formulae2).ToList();
        }

        internal static int? GetFormalCharge(XElement cmlElement)
        {
            int formalCharge;

            if (int.TryParse(cmlElement.Attribute(Constants.TagFormalCharge)?.Value, out formalCharge))
            {
                return formalCharge;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the cmlElement position from the CML
        /// </summary>
        /// <param name="cmlElement">XElement representing the cmlElement CML</param>
        /// <returns>Point containing the cmlElement coordinates</returns>
        internal static Point GetPosn(XElement cmlElement, out string message)
        {
            message = "";
            string symbol = cmlElement.Attribute(Constants.TagElementType)?.Value;
            string id = cmlElement.Attribute(Constants.TagId)?.Value;

            Point result = new Point();
            bool found = false;

            // Try first with 2D Co-ordinate scheme
            if (cmlElement.Attribute(Constants.TagX2) != null && cmlElement.Attribute(Constants.TagY2) != null)
            {
                result = new Point(
                    Double.Parse(cmlElement.Attribute(Constants.TagX2).Value, CultureInfo.InvariantCulture),
                    Double.Parse(cmlElement.Attribute(Constants.TagY2).Value, CultureInfo.InvariantCulture));
                found = true;
            }

            if (!found)
            {
                // Try again with 3D Co-ordinate scheme
                if (cmlElement.Attribute(Constants.TagX3) != null && cmlElement.Attribute(Constants.TagY3) != null)
                {
                    result = new Point(
                        Double.Parse(cmlElement.Attribute(Constants.TagY3).Value, CultureInfo.InvariantCulture),
                        Double.Parse(cmlElement.Attribute(Constants.TagY3).Value, CultureInfo.InvariantCulture));
                    found = true;
                }
            }

            if (!found)
            {
                message = $"No atom co-ordinates found for atom '{symbol}' with id of '{id}'.";
            }
            return result;
        }
    }
}