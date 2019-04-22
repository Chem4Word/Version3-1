// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Newtonsoft.Json;
namespace Chem4Word.Model2
{

    /// <summary>
    /// Stashes the molecule properties as XML and restores them
    /// Used in editing operations
    /// </summary>
    public class MoleculePropertyBag
    {
        private string _names;

        public string Names
        {
            get { return _names; }
            set { _names = value; }
        }

        private string _formulas;

        public string Formulas
        {
            get { return _formulas; }
            set { _formulas = value; }
        }

        public void Store(Molecule parent)
        {
            XmlSerializer namesSerializer= new XmlSerializer(typeof(ObservableCollection<ChemicalName>));
            XmlSerializer formulasSerializer = new XmlSerializer(typeof(ObservableCollection<Formula>));
            StringWriter sw1 = new StringWriter();
            StringWriter sw2 = new StringWriter();
            namesSerializer.Serialize(sw1, parent.Names);
            formulasSerializer.Serialize(sw2, parent.Formulas);
            Names = sw1.ToString();
            Formulas = sw2.ToString();
        }

        public void Restore(Molecule parent)
        {
            XmlSerializer namesSerializer = new XmlSerializer(typeof(ObservableCollection<ChemicalName>));
            XmlSerializer formulasSerializer = new XmlSerializer(typeof(ObservableCollection<Formula>));
            StringReader sr1 = new StringReader(Names);
            StringReader sr2 = new StringReader(Formulas);

            parent.Names.Clear();
            parent.Names = ((ObservableCollection<ChemicalName>) namesSerializer.Deserialize(sr1));
            parent.Formulas = ((ObservableCollection<Formula>)formulasSerializer.Deserialize(sr2));

        }
    }
}
