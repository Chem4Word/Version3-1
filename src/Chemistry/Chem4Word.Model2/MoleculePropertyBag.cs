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
            Names = JsonConvert.SerializeObject(parent.Names, Formatting.None);
            Formulas = JsonConvert.SerializeObject(parent.Formulas, Formatting.None);
        }

        public void Restore(Molecule parent)
        {
            parent.Names.Clear();
            parent.Names = JsonConvert.DeserializeObject<ObservableCollection<ChemicalName>>(Names);
            parent.Formulas = JsonConvert.DeserializeObject<ObservableCollection<Formula>>(Formulas);
        }
    }
}
