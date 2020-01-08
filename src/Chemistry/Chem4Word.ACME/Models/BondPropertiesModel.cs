// ---------------------------------------------------------------------------
//  Copyright (c) 2020, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.ACME.Controls;

namespace Chem4Word.ACME.Models
{
    public class BondPropertiesModel : BaseDialogModel
    {
        public bool IsDirty { get; set; }

        private DoubleBondType _doubleBondChoice;

        public DoubleBondType DoubleBondChoice
        {
            get => _doubleBondChoice;
            set
            {
                _doubleBondChoice = value;
                IsDirty = true;
            }
        }

        private SingleBondType _singleBondChoice;

        public SingleBondType SingleBondChoice
        {
            get => _singleBondChoice;
            set
            {
                _singleBondChoice = value;
                IsDirty = true;
            }
        }

        public double Angle { get; set; }
        public string AngleString => $"{Angle:N2}";

        public bool IsSingle { get; set; }
        public bool IsDouble { get; set; }

        public bool Is1Point5 { get; set; }
        public bool Is2Point5 { get; set; }

        private double _bondOrderValue;

        public double BondOrderValue
        {
            get { return _bondOrderValue; }
            set
            {
                if (value != _bondOrderValue)
                {
                    _bondOrderValue = value;

                    IsSingle = value == 1;
                    IsDouble = value == 2;
                    Is1Point5 = value == 1.5;
                    Is2Point5 = value == 2.5;

                    OnPropertyChanged(nameof(IsSingle));
                    OnPropertyChanged(nameof(IsDouble));
                    OnPropertyChanged(nameof(Is1Point5));
                    OnPropertyChanged(nameof(Is2Point5));

                    if (IsSingle)
                    {
                        SingleBondChoice = SingleBondType.None;
                        OnPropertyChanged(nameof(SingleBondChoice));
                    }

                    if (IsDouble | Is1Point5 | Is2Point5)
                    {
                        DoubleBondChoice = DoubleBondType.Auto;
                        OnPropertyChanged(nameof(DoubleBondChoice));
                    }

                    OnPropertyChanged();
                    IsDirty = true;
                }
            }
        }
    }
}