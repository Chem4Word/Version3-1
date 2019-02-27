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
    public static class CMLHelper
    {
        // ReSharper disable once InconsistentNaming
        public static XDocument LoadCML(string cml)
        {
            return XDocument.Parse(cml);
        }

        public static int? GetIsotopeNumber(XElement cmlElement)
        {
            int isotopeNumber;

            if (int.TryParse(cmlElement.Attribute(CMLConstants.TagIsotopeNumber)?.Value, out isotopeNumber))
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
            XAttribute xa = cmlElement.Attribute(CMLConstants.TagElementType);
            if (xa != null)
            {
                string symbol = xa.Value;
                ElementBase eb;
                AtomHelpers.TryParse(symbol, out eb);

                if (eb is Element element)
                {
                    return element;
                }

                if (eb is FunctionalGroup functionalGroup)
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
            var id1 = from XElement xe in doc.Elements(CMLConstants.TagXMLPartGuid) select xe;
            var id2 = from XElement xe in doc.Elements(CMLNamespaces.c4w + CMLConstants.TagXMLPartGuid) select xe;
            return id1.Union(id2).FirstOrDefault();
        }

        // ReSharper disable once InconsistentNaming
        internal static List<XElement> GetMolecules(XElement doc)
        {
            var mols = from XElement xe in doc.Elements(CMLConstants.TagMolecule) select xe;
            var mols2 = from XElement xe2 in doc.Elements(CMLNamespaces.cml + CMLConstants.TagMolecule) select xe2;
            return mols.Union(mols2).ToList();
        }

        internal static List<XElement> GetAtoms(XElement mol)
        {
            // Task 336
            var aa1 = from a in mol.Elements(CMLConstants.TagAtomArray) select a;
            var aa2 = from a in mol.Elements(CMLNamespaces.cml + CMLConstants.TagAtomArray) select a;
            var aa = aa1.Union(aa2);

            if (aa.Count() == 0)
            {
                // Bare Atoms without AtomArray
                var atoms1 = from a in mol.Elements(CMLConstants.TagAtom) select a;
                var atoms2 = from a in mol.Elements(CMLNamespaces.cml + CMLConstants.TagAtom) select a;
                return atoms1.Union(atoms2).ToList();
            }
            else
            {
                // Atoms inside AtomArray
                var atoms1 = from a in aa.Elements(CMLConstants.TagAtom) select a;
                var atoms2 = from a in aa.Elements(CMLNamespaces.cml + CMLConstants.TagAtom) select a;
                return atoms1.Union(atoms2).ToList();
            }
        }

        internal static List<XElement> GetBonds(XElement mol)
        {
            // Task 336
            var ba1 = from b in mol.Elements(CMLConstants.TagBondArray) select b;
            var ba2 = from b in mol.Elements(CMLNamespaces.cml + CMLConstants.TagBondArray) select b;
            var ba = ba1.Union(ba2);

            if (ba.Count() == 0)
            {
                // Bare bonds without BondArray
                var bonds1 = from b in mol.Elements(CMLConstants.TagBond) select b;
                var bonds2 = from b in mol.Elements(CMLNamespaces.cml + CMLConstants.TagBond) select b;
                return bonds1.Union(bonds2).ToList();
            }
            else
            {
                // Bonds inside BondArray
                var bonds1 = from b in ba.Elements(CMLConstants.TagBond) select b;
                var bonds2 = from b in ba.Elements(CMLNamespaces.cml + CMLConstants.TagBond) select b;
                return bonds1.Union(bonds2).ToList();
            }
        }

        internal static List<XElement> GetStereo(XElement bond)
        {
            var stereo = from s in bond.Elements(CMLConstants.TagBondStereo) select s;
            var stereo2 = from s in bond.Elements(CMLNamespaces.cml + CMLConstants.TagBondStereo) select s;
            return stereo.Union(stereo2).ToList();
        }

        internal static List<XElement> GetNames(XElement mol)
        {
            var names1 = from n1 in mol.Elements(CMLConstants.TagName) select n1;
            var names2 = from n2 in mol.Elements(CMLNamespaces.cml + CMLConstants.TagName) select n2;
            return names1.Union(names2).ToList();
        }

        internal static List<XElement> GetFormulas(XElement mol)
        {
            var formulae1 = from f1 in mol.Elements(CMLConstants.TagFormula) select f1;
            var formulae2 = from f2 in mol.Elements(CMLNamespaces.cml + CMLConstants.TagFormula) select f2;
            return formulae1.Union(formulae2).ToList();
        }

        internal static int? GetFormalCharge(XElement cmlElement)
        {
            int formalCharge;

            if (int.TryParse(cmlElement.Attribute(CMLConstants.TagFormalCharge)?.Value, out formalCharge))
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
            string symbol = cmlElement.Attribute(CMLConstants.TagElementType)?.Value;
            string id = cmlElement.Attribute(CMLConstants.TagId)?.Value;

            Point result = new Point();
            bool found = false;

            // Try first with 2D Co-ordinate scheme
            if (cmlElement.Attribute(CMLConstants.TagX2) != null && cmlElement.Attribute(CMLConstants.TagY2) != null)
            {
                result = new Point(
                    Double.Parse(cmlElement.Attribute(CMLConstants.TagX2).Value, CultureInfo.InvariantCulture),
                    Double.Parse(cmlElement.Attribute(CMLConstants.TagY2).Value, CultureInfo.InvariantCulture));
                found = true;
            }

            if (!found)
            {
                // Try again with 3D Co-ordinate scheme
                if (cmlElement.Attribute(CMLConstants.TagX3) != null && cmlElement.Attribute(CMLConstants.TagY3) != null)
                {
                    result = new Point(
                        Double.Parse(cmlElement.Attribute(CMLConstants.TagX3).Value, CultureInfo.InvariantCulture),
                        Double.Parse(cmlElement.Attribute(CMLConstants.TagY3).Value, CultureInfo.InvariantCulture));
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