// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Linq;
using System.Xml.Linq;
using Chem4Word.Model2.Helpers;
using Chem4Word.Model2.Interfaces;

namespace Chem4Word.Model2.Converters
{
    public class CMLConverter
    {
        private const string TagConventions = "conventions";
        private const string TagCml = "cml";
        private const string TagCmlDict = "cmlDict";
        private const string TagNameDict = "nameDict";
        private const string TagC4W = "c4w";
        private const string TagValConvetionMolecular = "convention:molecular";
        private const string TagFormula = "formula";
        private const string TagXMLPartGuid = "customXmlPartGuid";
        private const string TagId = "id";
        private const string AttrConvention = "convention";
        private const string AttrInline = "inline";
        private const string TagConcise = "concise";
        private const string TagName = "name";
        private const string TagDictRef = "dictRef";
        private const string TagBondStereo = "bondStereo";
        private const string TagAtomRefs4 = "atomRefs4";
        private const string TagAtomRefs2 = "atomRefs2";
        private const string TagMolecule = "molecule";
        private const string TagAtomArray = "atomArray";
        private const string TagBondArray = "bondArray";
        private const string TagBond = "bond";
        private const string TagOrder = "order";
        private const string TagPlacement = "placement";
        private const string TagAtom = "atom";
        private const string TagElementType = "elementType";
        private const string TagX2 = "x2";
        private const string TagY2 = "y2";
        private const string TagFormalCharge = "formalCharge";
        private const string TagIsotopeNumber = "isotopeNumber";

        public static string Description => "Chemical Markup Language";
        public bool CanImport => true;
        public bool Compressed { get; set; }
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
                newModel.Relabel(true);

                newModel.Refresh();
            }

            return newModel;
        }

        public string Export(Chem4Word.Model2.Model model)
        {
            XDocument xd = new XDocument();
            
            XElement root = new XElement(CML.cml + TagCml,
                new XAttribute(XNamespace.Xmlns + TagConventions, CML.conventions),
                new XAttribute(XNamespace.Xmlns + TagCml, CML.cml),
                new XAttribute(XNamespace.Xmlns + TagCmlDict, CML.cmlDict),
                new XAttribute(XNamespace.Xmlns + TagNameDict, CML.nameDict),
                new XAttribute(XNamespace.Xmlns + TagC4W, CML.c4w),
                new XAttribute(TagConventions, TagValConvetionMolecular)
                );

            // Only export if set
            if (!string.IsNullOrEmpty(model.CustomXmlPartGuid))
            {
                XElement customXmlPartGuid = new XElement(Converters.CML.c4w + TagXMLPartGuid, model.CustomXmlPartGuid);
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

        public XElement GetXElement(Formula f, string concise)
        {
            XElement result = new XElement(Converters.CML.cml + TagFormula);

            if (f.Id != null)
            {
                result.Add(new XAttribute(TagId, f.Id));
            }

            if (f.Convention != null)
            {
                result.Add(new XAttribute(AttrConvention, f.Convention));
            }

            if (f.Inline != null)
            {
                result.Add(new XAttribute(AttrInline, f.Inline));
            }

            if (concise != null)
            {
                result.Add(new XAttribute(TagConcise, concise));
            }

            return result;
        }
        public XElement GetXElement(string concise, string molId)
        {
            XElement result = new XElement(Converters.CML.cml + TagFormula);

            if (concise != null)
            {
                result.Add(new XAttribute(TagId, $"{molId}.f0"));
                result.Add(new XAttribute(TagConcise, concise));
            }

            return result;
        }

        public XElement GetXElement(ChemicalName name)
        {
            XElement result = new XElement(Converters.CML.cml + TagName, name.Name);

            if (name.Id != null)
            {
                result.Add(new XAttribute(TagId, name.Id));
            }

            if (name.DictRef != null)
            {
                result.Add(new XAttribute(TagDictRef, name.DictRef));
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

                    result = new XElement(Converters.CML.cml + TagBondStereo,
                        new XAttribute(TagAtomRefs4,
                            $"{firstAtom.Id} {bond.StartAtom.Id} {bond.EndAtom.Id} {lastAtom.Id}"),
                        GetStereoString(bond.Stereo));
                }
                else
                {
                    result = new XElement(Converters.CML.cml + TagBondStereo,
                        new XAttribute(TagAtomRefs2, $"{bond.StartAtom.Id} {bond.EndAtom.Id}"),
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


        public XElement GetMoleculeElement(Molecule mol)
        {
            XElement molElement = new XElement(Converters.CML.cml + TagMolecule, new XAttribute(TagId, mol.Id));

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
                    XElement aaElement = new XElement(Converters.CML.cml + TagAtomArray);
                    foreach (Atom atom in mol.Atoms.Values)
                    {
                        aaElement.Add(GetXElement(atom));
                    }
                    molElement.Add(aaElement);
                }

                // Task 336
                if (mol.Bonds.Count > 0)
                {
                    XElement baElement = new XElement(Converters.CML.cml + TagBondArray);
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

        public XElement GetXElement(Bond bond)
        {
            XElement result;

            result = new XElement(Converters.CML.cml + TagBond,
                new XAttribute(TagId, bond.Id),
                new XAttribute(TagAtomRefs2, $"{bond.StartAtomInternalId} {bond.EndAtomInternalId}"),
                new XAttribute(TagOrder, bond.Order),
                GetStereoXElement(bond));

            if (bond.ExplicitPlacement != null)
            {
                result.Add(new XAttribute(Converters.CML.c4w + TagPlacement, bond.ExplicitPlacement));
            }
            return result;
        }

        public XElement GetXElement(Atom atom)
        {
            XElement result = new XElement(Converters.CML.cml + TagAtom,
                new XAttribute(TagId, atom.Id),
                new XAttribute(TagElementType, atom.Element.Symbol),
                new XAttribute(TagX2, atom.Position.X),
                new XAttribute(TagY2, atom.Position.Y)
            );

            if (atom.FormalCharge != null)
            {
                result.Add(new XAttribute(TagFormalCharge, atom.FormalCharge.Value));
            }
            if (atom.IsotopeNumber != null)
            {
                result.Add(new XAttribute(TagIsotopeNumber, atom.IsotopeNumber));
            }
            return result;
        }

        public bool CanExport => true;

        private static Molecule AddMolecule(Model newModel, Molecule newMol)
        {
            return newModel.AddMolecule(newMol);
        }
    }
}