// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Chem4Word.ACME.Controls;
using Chem4Word.Model2.Annotations;

namespace Chem4Word.ACME.Models
{
    public class BondPropertiesModel : BaseDialogModel, INotifyPropertyChanged
    {
        //public string Stereo { get; set; }
        public DoubleBondType DoubleBondChoice { get; set; }
        public SingleBondType SingleBondChoice { get; set; }
        public double Angle { get; set; }
        public bool IsSingle { get; set; }
        public bool IsDouble { get; set; }

        private double _bondOrderValue;

        public double BondOrderValue
        {
            get { return _bondOrderValue; }
            set
            {
                if (value != _bondOrderValue)
                {
                    _bondOrderValue = value;
                    OnPropertyChanged();

                    IsSingle = value == 1;
                    IsDouble = value == 2;

                    OnPropertyChanged(nameof(IsSingle));
                    OnPropertyChanged(nameof(IsDouble));

                    if (IsSingle)
                    {
                        SingleBondChoice = SingleBondType.None;
                        OnPropertyChanged(nameof(SingleBondChoice));
                    }

                    if (IsDouble)
                    {
                        DoubleBondChoice = DoubleBondType.Auto;
                        OnPropertyChanged(nameof(DoubleBondChoice));
                    }
                }
            }
        }
    }
}