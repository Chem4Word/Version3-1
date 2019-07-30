using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Chem4Word.ACME.Annotations;
using Chem4Word.Model2;

namespace Chem4Word.ACME.Models
{
    public class NamesModel : INotifyPropertyChanged
    {
        private ObservableCollection<TextualProperty> _listOfNames;

        public ObservableCollection<TextualProperty> ListOfNames
        {
            get { return _listOfNames; }
            set
            {
                _listOfNames = value;
                OnPropertyChanged(nameof(ListOfNames));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}