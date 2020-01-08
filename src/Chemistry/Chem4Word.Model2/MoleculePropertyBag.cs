// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace Chem4Word.Model2
{
    /// <summary>
    /// Stashes the molecule properties as XML and restores them
    /// Used in editing operations
    /// </summary>
    public class MoleculePropertyBag
    {
        private string Names { get; set; }

        private string Formulas { get; set; }

        private string Labels { get; set; }

        public void Store(Molecule parent)
        {
            Names = JsonConvert.SerializeObject(parent.Names, Formatting.None);
            Formulas = JsonConvert.SerializeObject(parent.Formulas, Formatting.None);
            Labels = JsonConvert.SerializeObject(parent.Labels, Formatting.None);
        }

        public void Restore(Molecule parent)
        {
            parent.Names.Clear();
            parent.Names = JsonConvert.DeserializeObject<ObservableCollection<TextualProperty>>(Names);
            parent.Formulas.Clear();
            parent.Formulas = JsonConvert.DeserializeObject<ObservableCollection<TextualProperty>>(Formulas);
            parent.Labels.Clear();
            parent.Labels = JsonConvert.DeserializeObject<ObservableCollection<TextualProperty>>(Labels);
        }
    }
}