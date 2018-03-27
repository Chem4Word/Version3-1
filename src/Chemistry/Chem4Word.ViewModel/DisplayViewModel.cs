using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Chem4Word.Model;
using Chem4Word.ViewModel.Commands;

namespace Chem4Word.ViewModel
{
    public class DisplayViewModel
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

     
        #endregion Properties



        #region constructors

        public DisplayViewModel()
        {
             FontSize = 23;
        }


        public DisplayViewModel(Model.Model model)
        {
            

            AllObjects = model.AllObjects;

            AllAtoms = model.AllAtoms;
            AllAtoms.CollectionChanged += AllAtoms_CollectionChanged;

            AllBonds = model.AllBonds;
            AllBonds.CollectionChanged += AllBonds_CollectionChanged;

        }


        ~DisplayViewModel()
        {
            AllAtoms.CollectionChanged -= AllAtoms_CollectionChanged;
            AllBonds.CollectionChanged -= AllBonds_CollectionChanged;
        }
#endregion

        private void AllBonds_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void AllAtoms_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }


        #endregion constructors
    }
}
