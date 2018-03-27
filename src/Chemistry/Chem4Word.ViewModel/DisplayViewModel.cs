using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Chem4Word.Model;
using Chem4Word.Model.Annotations;
using Chem4Word.ViewModel.Commands;

namespace Chem4Word.ViewModel
{
    public class DisplayViewModel : INotifyPropertyChanged
    {


        #region Properties

        public CompositeCollection AllObjects { get; set; }

        public ObservableCollection<Atom> AllAtoms { get; set; }

        public ObservableCollection<Bond> AllBonds { get; set; }
    

        public Model.Model Model { get; set; }
        
        #region Layout

        //used to calculate the bounds of the atom
        public static double FontSize { get; set; }

        public Rect BoundingBox
        {
            get
            {
                var modelRect = AllAtoms[0].BoundingBox(FontSize);
                for (int i = 1; i < AllAtoms.Count; i++)
                {
                    var atom = AllAtoms[i];
                    modelRect.Union(atom.BoundingBox(FontSize));
                }
                return modelRect;
            }
        }
        #endregion Layout

        #endregion Properties



        #region constructors

        public DisplayViewModel()
        {
             FontSize = 23;
        }


        public DisplayViewModel(Model.Model model):this()
        {
            

            AllObjects = model.AllObjects;

            AllAtoms = model.AllAtoms;
            AllAtoms.CollectionChanged += AllAtoms_CollectionChanged;

            BindAtomChanges();

            AllBonds = model.AllBonds;
            AllBonds.CollectionChanged += AllBonds_CollectionChanged;
            OnPropertyChanged(nameof(BoundingBox));
        }

        private void BindAtomChanges()
        {
            foreach (Atom allAtom in AllAtoms)
            {
                allAtom.PropertyChanged += AllAtom_PropertyChanged;
            }
        }
        private void UnbindAtomChanges()
        {
            foreach (Atom allAtom in AllAtoms)
            {
                allAtom.PropertyChanged -= AllAtom_PropertyChanged;
            }
        }
        private void AllAtom_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Atom.SymbolText))
            {
                OnPropertyChanged(nameof(BoundingBox));
            }
        }

        ~DisplayViewModel()
        {
            UnbindAtomChanges();
            AllAtoms.CollectionChanged -= AllAtoms_CollectionChanged;
            AllBonds.CollectionChanged -= AllBonds_CollectionChanged;
        }

    

        #endregion

        private void AllBonds_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(BoundingBox));
        }

        private void AllAtoms_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
           OnPropertyChanged(nameof(BoundingBox));
        }



        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
