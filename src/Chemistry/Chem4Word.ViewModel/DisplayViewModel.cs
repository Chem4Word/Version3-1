// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using Chem4Word.Model;
using Chem4Word.Model.Annotations;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;

namespace Chem4Word.ViewModel
{
    public class DisplayViewModel : INotifyPropertyChanged
    {
        #region Properties

        public CompositeCollection AllObjects { get; set; }

        public ObservableCollection<Atom> AllAtoms { get; set; }

        public ObservableCollection<Bond> AllBonds { get; set; }

        public Model.Model Model { get; set; }

        private double? _bondThickness;
        public double BondThickness {
            get
            {
                if (_bondThickness == null)
                {
                    double h = BoundingBox.Height;
                    double w = BoundingBox.Width;
                    double n = Math.Max(h, w);
                    _bondThickness =  n / 100;
                }
                return _bondThickness.Value;
            }
        }

        public double HalfBondThickness
        {
            get
            {
                return BondThickness / 2;
            }
        }

        #region Layout

        //used to calculate the bounds of the atom
        public static double FontSize { get; set; }

        public Rect BoundingBox
        {
            get
            {
                try
                {
                    if (AllAtoms.Any())
                    {
                        var modelRect = AllAtoms[0].BoundingBox(FontSize);
                        for (int i = 1; i < AllAtoms.Count; i++)
                        {
                            var atom = AllAtoms[i];
                            modelRect.Union(atom.BoundingBox(FontSize));
                        }
                        return modelRect;
                    }
                    else
                    {
                        return new Rect(0, 0, Globals.DefaultFontSize, Globals.DefaultFontSize);
                    }
                }
                catch (System.NullReferenceException ex)
                {
                    return new Rect(0, 0, Globals.DefaultFontSize, Globals.DefaultFontSize);
                }
            }
        }

        #endregion Layout

        #endregion Properties

        #region Constructors

        public DisplayViewModel()
        {
        }

        public DisplayViewModel(Model.Model model) : this()
        {
            Model = model;
            FontSize = Globals.DefaultFontSize;
            if (model.AllBonds.Any())
            {
                FontSize = model.MeanBondLength * Globals.FontSizePercentageBond;
            }
            AllObjects = model.AllObjects;

            AllAtoms = model.AllAtoms;
            AllAtoms.CollectionChanged += AllAtoms_CollectionChanged;

            BindAtomChanges();

            AllBonds = model.AllBonds;
            AllBonds.CollectionChanged += AllBonds_CollectionChanged;
            OnPropertyChanged(nameof(BoundingBox));
        }

        ~DisplayViewModel()
        {
            UnbindAtomChanges();
            if (AllAtoms != null)
            {
                AllAtoms.CollectionChanged -= AllAtoms_CollectionChanged;
            }
            if (AllBonds != null)
            {
                AllBonds.CollectionChanged -= AllBonds_CollectionChanged;
            }
        }

        #endregion Constructors

        private void BindAtomChanges()
        {
            if (AllAtoms != null && AllAtoms.Any())
            {
                foreach (Atom allAtom in AllAtoms)
                {
                    allAtom.PropertyChanged += AllAtom_PropertyChanged;
                }
            }
        }

        private void UnbindAtomChanges()
        {
            if (AllAtoms != null && AllAtoms.Any())
            {
                foreach (Atom allAtom in AllAtoms)
                {
                    allAtom.PropertyChanged -= AllAtom_PropertyChanged;
                }
            }
        }

        private void AllAtom_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Atom.SymbolText))
            {
                OnPropertyChanged(nameof(BoundingBox));
            }
        }

        private void AllBonds_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(BoundingBox));
        }

        private void AllAtoms_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(BoundingBox));
        }

        [field: NonSerialized()]
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}