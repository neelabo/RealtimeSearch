using NeeLaboratory.Generators;
using NeeLaboratory.RealtimeSearch.Models;
using System.ComponentModel;
using System.Windows.Data;

namespace NeeLaboratory.RealtimeSearch.ViewModels
{
    [NotifyPropertyChanged]
    public partial class SettingExternalAppViewModel : INotifyPropertyChanged
    {
        private CollectionViewSource? _collectionViewSource;
        private ExternalProgram? _selectedItem;


        public SettingExternalAppViewModel(AppConfig setting)
        {
            Setting = setting;

            UpdateCollectionViewSource();
        }


        public event PropertyChangedEventHandler? PropertyChanged;


        public AppConfig Setting { get; }


        public CollectionViewSource? CollectionViewSource
        {
            get { return _collectionViewSource; }
            set { SetProperty(ref _collectionViewSource, value); }
        }

        public ExternalProgram? SelectedItem
        {
            get { return _selectedItem; }
            set { SetProperty(ref _selectedItem, value); }
        }


        private void UpdateCollectionViewSource()
        {
            var collectionViewSource = new CollectionViewSource
            {
                Source = Setting.ExternalPrograms
            };

            CollectionViewSource = collectionViewSource;
        }

        public ExternalProgram AddExternalProgram()
        {
            var item = new ExternalProgram();
            Setting.ExternalPrograms.Add(item);
            Setting.ValidateExternalProgramsIndex();
            return item;
        }

        public void DeleteExternalProgram(ExternalProgram item)
        {
            Setting.ExternalPrograms.Remove(item);
            Setting.ValidateExternalProgramsIndex();
        }
    }
}
