// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.ComponentModel;
using System.Runtime.CompilerServices;
using Chem4Word.Model2.Annotations;
using Chem4Word.Model2.Converters.CML;

namespace Chem4Word.Model2
{
    public class TextualProperty : INotifyPropertyChanged
    {
        public string Id { get; set; }

        public bool CanBeDeleted { get; set; }
        public bool CanBeEdited { get; private set; }
        public bool IsValid { get; private set; }

        private string _type;
        private string _value;


        public string Type
        {
            get => _type;
            set
            {
                _type = value;
                SetEditFlag();
                OnPropertyChanged(nameof(Type));
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                _value = value;
                SetEditFlag();
                IsValid = !string.IsNullOrEmpty(_value);
                OnPropertyChanged(nameof(Value));
            }
        }

        private void SetEditFlag()
        {
            CanBeEdited = !(Type.Equals(CMLConstants.AttributeValueChem4WordLabel)
                          || Type.Equals(CMLConstants.AttributeValueChem4WordFormula)
                          || Type.Equals(CMLConstants.AttributeValueChem4WordSynonym));
        }

        public override string ToString()
        {
            string result = string.Empty;

            if (!string.IsNullOrEmpty(Id))
            {
                result += $"{Id} ";
            }

            if (!string.IsNullOrEmpty(_type))
            {
                result += $"{_type} ";
            }

            if (!string.IsNullOrEmpty(_value))
            {
                result += $"{_value} ";
            }

            return result.Trim();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}