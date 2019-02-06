// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model2.Helpers;
using Chem4Word.Model2.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

namespace Chem4Word.Model2.Converters.CML
{
    // ReSharper disable once InconsistentNaming
    public class CMLConverter
    {
        public bool Compressed { get; set; }

        public Model Import(object data)
        {
            Model newModel = new Model();

            if (data != null)
            {
                XDocument modelDoc = XDocument.Parse((string)data);
                var root = modelDoc.Root;

                // Only import if not null
                var customXmlPartGuid = CMLHelper.GetCustomXmlPartGuid(root);
                if (customXmlPartGuid != null && !string.IsNullOrEmpty(customXmlPartGuid.Value))
                {
                    newModel.CustomXmlPartGuid = customXmlPartGuid.Value;
                }

                var moleculeElements = CMLHelper.GetMolecules(root);

                foreach (XElement meElement in moleculeElements)
                {
                    var newMol = GetMolecule(meElement);

                    AddMolecule(newModel, newMol);
                    newMol.Parent = (IChemistryContainer)newModel;
                }

                foreach (Molecule molecule in newModel.Molecules.Values)
                {
                    // Force ConciseFormula to be calculated
                    // molecule.ConciseFormula = molecule.CalculatedFormula();
                }
                newModel.Relabel(true);

                newModel.Refresh();
            }

            return newModel;
        }

        public string Export(Chem4Word.Model2.Model model)
        {
            XDocument xd = new XDocument();

            XElement root = new XElement(CMLNamespaces.cml + CMLConstants.TagCml,
                new XAttribute(XNamespace.Xmlns + CMLConstants.TagConventions, CMLNamespaces.conventions),
                new XAttribute(XNamespace.Xmlns + CMLConstants.TagCml, CMLNamespaces.cml),
                new XAttribute(XNamespace.Xmlns + CMLConstants.TagCmlDict, CMLNamespaces.cmlDict),
                new XAttribute(XNamespace.Xmlns + CMLConstants.TagNameDict, CMLNamespaces.nameDict),
                new XAttribute(XNamespace.Xmlns + CMLConstants.TagC4W, CMLNamespaces.c4w),
                new XAttribute(CMLConstants.TagConventions, CMLConstants.TagValConvetionMolecular)
                );

            // Only export if set
            if (!string.IsNullOrEmpty(model.CustomXmlPartGuid))
            {
                XElement customXmlPartGuid = new XElement(CMLNamespaces.c4w + CMLConstants.TagXMLPartGuid, model.CustomXmlPartGuid);
                root.Add(customXmlPartGuid);
            }

            bool relabelRequired = false;

            // Handle case where id's are null
            foreach (Molecule molecule in model.Molecules.Values)
            {
                if (molecule.Id == null)
                {
                    relabelRequired = true;
                    break;
                }

                foreach (Atom atom in molecule.Atoms.Values)
                {
                    if (atom.Id == null)
                    {
                        relabelRequired = true;
                        break;
                    }
                }

                foreach (Bond bond in molecule.Bonds)
                {
                    if (bond.Id == null)
                    {
                        relabelRequired = true;
                        break;
                    }
                }
            }

            if (relabelRequired)
            {
                model.Relabel(false);
            }

            foreach (Molecule molecule in model.Molecules.Values)
            {
                root.Add(GetMoleculeElement(molecule));
            }
            xd.Add(root);

            string cml = string.Empty;
            if (Compressed)
            {
                cml = xd.ToString(SaveOptions.DisableFormatting);
            }
            else
            {
                cml = xd.ToString();
            }

            return cml;
        }

        #region Export Helpers

        private XElement GetXElement(Formula f, string concise)
        {
            XElement result = new XElement(CMLNamespaces.cml + CMLConstants.TagFormula);

            if (f.Id != null)
            {
                result.Add(new XAttribute(CMLConstants.TagId, f.Id));
            }

            if (f.Convention != null)
            {
                result.Add(new XAttribute(CMLConstants.AttrConvention, f.Convention));
            }

            if (f.Inline != null)
            {
                result.Add(new XAttribute(CMLConstants.AttrInline, f.Inline));
            }

            if (concise != null)
            {
                result.Add(new XAttribute(CMLConstants.TagConcise, concise));
            }

            return result;
        }

        private XElement GetXElement(string concise, string molId)
        {
            XElement result = new XElement(CMLNamespaces.cml + CMLConstants.TagFormula);

            if (concise != null)
            {
                result.Add(new XAttribute(CMLConstants.TagId, $"{molId}.f0"));
                result.Add(new XAttribute(CMLConstants.TagConcise, concise));
            }

            return result;
        }

        private XElement GetXElement(ChemicalName name)
        {
            XElement result = new XElement(CMLNamespaces.cml + CMLConstants.TagName, name.Name);

            if (name.Id != null)
            {
                result.Add(new XAttribute(CMLConstants.TagId, name.Id));
            }

            if (name.DictRef != null)
            {
                result.Add(new XAttribute(CMLConstants.TagDictRef, name.DictRef));
            }

            return result;
        }

        private XElement GetStereoXElement(Bond bond)
        {
            XElement result = null;

            if (bond.Stereo != Globals.BondStereo.None)
            {
                if (bond.Stereo == Globals.BondStereo.Cis || bond.Stereo == Globals.BondStereo.Trans)
                {
                    Atom firstAtom = bond.StartAtom;
                    Atom lastAtom = bond.EndAtom;

                    // Hack: To find first and last atomRefs
                    foreach (var atomBond in bond.StartAtom.Bonds)
                    {
                        if (!bond.Id.Equals(atomBond.Id))
                        {
                            firstAtom = atomBond.OtherAtom(bond.StartAtom);
                            break;
                        }
                    }

                    foreach (var atomBond in bond.EndAtom.Bonds)
                    {
                        if (!bond.Id.Equals(atomBond.Id))
                        {
                            lastAtom = atomBond.OtherAtom(bond.EndAtom);
                            break;
                        }
                    }

                    result = new XElement(CMLNamespaces.cml + CMLConstants.TagBondStereo,
                        new XAttribute(CMLConstants.TagAtomRefs4,
                            $"{firstAtom.Id} {bond.StartAtom.Id} {bond.EndAtom.Id} {lastAtom.Id}"),
                        GetStereoString(bond.Stereo));
                }
                else
                {
                    result = new XElement(CMLNamespaces.cml + CMLConstants.TagBondStereo,
                        new XAttribute(CMLConstants.TagAtomRefs2, $"{bond.StartAtom.Id} {bond.EndAtom.Id}"),
                        GetStereoString(bond.Stereo));
                }
            }

            return result;
        }

        private string GetStereoString(Globals.BondStereo stereo)
        {
            switch (stereo)
            {
                case Globals.BondStereo.None:
                    return null;

                case Globals.BondStereo.Hatch:
                    return "H";

                case Globals.BondStereo.Wedge:
                    return "W";

                case Globals.BondStereo.Cis:
                    return "C";

                case Globals.BondStereo.Trans:
                    return "T";

                case Globals.BondStereo.Indeterminate:
                    return "S";

                default:
                    return null;
            }
        }

        private XElement GetMoleculeElement(Molecule mol)
        {
            XElement molElement = new XElement(CMLNamespaces.cml + CMLConstants.TagMolecule, new XAttribute(CMLConstants.TagId, mol.Id));

            if (mol.Molecules.Any())
            {
                foreach (var childMolecule in mol.Molecules.Values)
                {
                    molElement.Add(GetMoleculeElement(childMolecule));
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(mol.ConciseFormula))
                {
                    molElement.Add(GetXElement(mol.ConciseFormula, mol.Id));
                }

                foreach (Formula formula in mol.Formulas)
                {
                    molElement.Add(GetXElement(formula, mol.ConciseFormula));
                }

                foreach (ChemicalName chemicalName in mol.Names)
                {
                    molElement.Add(GetXElement(chemicalName));
                }

                // Task 336
                if (mol.Atoms.Count > 0)
                {
                    // Add atomArray element, then add these to it
                    XElement aaElement = new XElement(CMLNamespaces.cml + CMLConstants.TagAtomArray);
                    foreach (Atom atom in mol.Atoms.Values)
                    {
                        aaElement.Add(GetXElement(atom));
                    }
                    molElement.Add(aaElement);
                }

                // Task 336
                if (mol.Bonds.Count > 0)
                {
                    XElement baElement = new XElement(CMLNamespaces.cml + CMLConstants.TagBondArray);
                    // Add bondArray element, then add these to it
                    foreach (Bond bond in mol.Bonds)
                    {
                        baElement.Add(GetXElement(bond));
                    }
                    molElement.Add(baElement);
                }
            }
            return molElement;
        }

        private XElement GetXElement(Bond bond)
        {
            XElement result;

            result = new XElement(CMLNamespaces.cml + CMLConstants.TagBond,
                new XAttribute(CMLConstants.TagId, bond.Id),
                new XAttribute(CMLConstants.TagAtomRefs2, $"{bond.StartAtom.Id} {bond.EndAtom.Id}"),
                new XAttribute(CMLConstants.TagOrder, bond.Order),
                GetStereoXElement(bond));

            if (bond.ExplicitPlacement != null)
            {
                result.Add(new XAttribute(CMLNamespaces.c4w + CMLConstants.TagPlacement, bond.ExplicitPlacement));
            }
            return result;
        }

        private XElement GetXElement(Atom atom)
        {
            XElement result = new XElement(CMLNamespaces.cml + CMLConstants.TagAtom,
                new XAttribute(CMLConstants.TagId, atom.Id),
                new XAttribute(CMLConstants.TagElementType, atom.Element.Symbol),
                new XAttribute(CMLConstants.TagX2, atom.Position.X),
                new XAttribute(CMLConstants.TagY2, atom.Position.Y)
            );

            if (atom.FormalCharge != null)
            {
                result.Add(new XAttribute(CMLConstants.TagFormalCharge, atom.FormalCharge.Value));
            }
            if (atom.IsotopeNumber != null)
            {
                result.Add(new XAttribute(CMLConstants.TagIsotopeNumber, atom.IsotopeNumber));
            }
            return result;
        }

        #endregion Export Helpers

        #region Import Helpers

        private static Molecule AddMolecule(Model newModel, Molecule newMol)
        {
            return newModel.AddMolecule(newMol);
        }

        public static Molecule GetMolecule(XElement cmlElement)
        {
            Molecule molecule = new Molecule();

            string idValue = cmlElement.Attribute("id")?.Value;
            if (idValue != null)
            {
                molecule.Id = idValue;
            }

            molecule.Errors = new List<string>();
            molecule.Warnings = new List<string>();

            List<XElement> childMolecules = CMLHelper.GetMolecules(cmlElement);

            List<XElement> atomElements = CMLHelper.GetAtoms(cmlElement);
            List<XElement> bondElements = CMLHelper.GetBonds(cmlElement);
            List<XElement> nameElements = CMLHelper.GetNames(cmlElement);
            List<XElement> formulaElements = CMLHelper.GetFormulas(cmlElement);

            foreach (XElement childElement in childMolecules)
            {
                Molecule newMol = GetMolecule(childElement);
                molecule.AddMolecule(newMol);
                newMol.Parent = molecule;
            }

            Dictionary<string, string> reverseAtomLookup = new Dictionary<string, string>();
            foreach (XElement atomElement in atomElements)
            {
                Atom newAtom = GetAtom(atomElement);
                if (newAtom.Messages.Count > 0)
                {
                    molecule.Errors.AddRange(newAtom.Messages);
                }

                molecule.AddAtom(newAtom);
                reverseAtomLookup[newAtom.Id] = newAtom.InternalId;
                newAtom.Parent = molecule;
            }

            foreach (XElement bondElement in bondElements)
            {
                Bond newBond = GetBond(bondElement, reverseAtomLookup);

                if (newBond.Messages.Count > 0)
                {
                    molecule.Errors.AddRange(newBond.Messages);
                }

                molecule.AddBond(newBond);
                newBond.Parent = molecule;
            }

            foreach (XElement formulaElement in formulaElements)
            {
                // Only import Concise Once
                if (string.IsNullOrEmpty(molecule.ConciseFormula))
                {
                    molecule.ConciseFormula = formulaElement.Attribute(CMLConstants.TagConcise)?.Value;
                }

                Formula formula = new Formula(formulaElement);
                if (formula.IsValid)
                {
                    molecule.Formulas.Add(formula);
                }
            }

            foreach (XElement nameElement in nameElements)
            {
                molecule.Names.Add(new ChemicalName(nameElement));
            }

            molecule.RebuildRings();

            return molecule;
        }

        public static Atom GetAtom(XElement atomElement)
        {
            Atom atom = new Atom();

            atom.Messages = new List<string>();
            string message = "";
            string atomLabel = atomElement.Attribute(CMLConstants.TagId)?.Value;

            Point p = CMLHelper.GetPosn(atomElement, out message);
            if (!string.IsNullOrEmpty(message))
            {
                atom.Messages.Add(message);
            }

            atom.Id = atomLabel;
            atom.Position = p;

            ElementBase e = CMLHelper.GetChemicalElement(atomElement, out message);
            if (!string.IsNullOrEmpty(message))
            {
                atom.Messages.Add(message);
            }

            if (e != null)
            {
                atom.Element = e;
                atom.FormalCharge = CMLHelper.GetFormalCharge(atomElement);
                atom.IsotopeNumber = CMLHelper.GetIsotopeNumber(atomElement);
            }

            return atom;
        }

        public static Bond GetBond(XElement cmlElement, Dictionary<string, string> reverseAtomLookup)
        {
            Bond bond = new Bond();

            string[] atomRefs = cmlElement.Attribute(CMLConstants.TagAtomRefs2)?.Value.Split(' ');
            if (atomRefs?.Length == 2)
            {
                bond.StartAtomInternalId = reverseAtomLookup[atomRefs[0]];
                bond.EndAtomInternalId = reverseAtomLookup[atomRefs[1]];
            }
            var bondRef = cmlElement.Attribute(CMLConstants.TagId)?.Value;
            bond.Id = bondRef ?? bond.Id;
            bond.Order = cmlElement.Attribute(CMLConstants.TagOrder)?.Value;

            var stereoElems = CMLHelper.GetStereo(cmlElement);

            if (stereoElems.Any())
            {
                var stereo = stereoElems[0].Value;

                switch (stereo)
                {
                    case "N":
                        bond.Stereo = Globals.BondStereo.None;
                        break;

                    case "W":
                        bond.Stereo = Globals.BondStereo.Wedge;
                        break;

                    case "H":
                        bond.Stereo = Globals.BondStereo.Hatch;
                        break;

                    case "S":
                        bond.Stereo = Globals.BondStereo.Indeterminate;
                        break;

                    case "C":
                        bond.Stereo = Globals.BondStereo.Cis;
                        break;

                    case "T":
                        bond.Stereo = Globals.BondStereo.Trans;
                        break;

                    default:
                        bond.Stereo = Globals.BondStereo.None;
                        break;
                }
            }
            Globals.BondDirection? dir = null;

            var dirAttr = cmlElement.Attribute(CMLNamespaces.c4w + CMLConstants.TagPlacement);
            if (dirAttr != null)
            {
                Globals.BondDirection temp;

                if (Enum.TryParse(dirAttr.Value, out temp))
                {
                    dir = temp;
                }
            }

            if (dir != null)
            {
                bond.Placement = dir.Value;
            }

            return bond;
        }

        #endregion Import Helpers
    }
}