// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;
using System.ComponentModel;

namespace Chem4Word.ACME.Models
{
    public class BondPropertiesModel : BaseDialogModel
    {
        public DoubleBondType DoubleBondChoice { get; set; }
        public SingleBondType SingleBondChoice { get; set; }

        public double Angle { get; set; }
        public bool IsSingle { get; set; }
        public bool IsDouble { get; set; }
        public bool ShowPlacementSettings { get; set; }

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
                    ShowPlacementSettings = value == 1.5 || value == 2 || value == 2.5;

                    OnPropertyChanged(nameof(IsSingle));
                    OnPropertyChanged(nameof(IsDouble));
                    OnPropertyChanged(nameof(ShowPlacementSettings));

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