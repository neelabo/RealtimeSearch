using CommunityToolkit.Mvvm.ComponentModel;
using NeeLaboratory.Generators;
using NeeLaboratory.RealtimeSearch.Models;
using System.ComponentModel;
using System.Windows.Data;

namespace NeeLaboratory.RealtimeSearch.ViewModels
{
    public partial class SettingExternalAppViewModel : ObservableObject
    {
        private CollectionViewSource? _collectionViewSource;
        private ExternalProgram? _selectedItem;


        public SettingExternalAppViewModel(AppSettings setting)
        {
            Setting = setting;

            UpdateCollectionViewSource();
        }


        public AppSettings Setting { get; }


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
            return item;
        }

        public void DeleteExternalProgram(ExternalProgram item)
        {
            Setting.ExternalPrograms.Remove(item);
        }

        public void MoveToUp(ExternalProgram item)
        {
            var index = Setting.ExternalPrograms.IndexOf(item);
            if (index <= 0) return;

            Setting.ExternalPrograms.Move(index, index - 1);
        }

        public void MoveToDown(ExternalProgram item)
        {
            var index = Setting.ExternalPrograms.IndexOf(item);
            if (index < 0 || index >= Setting.ExternalPrograms.Count - 1) return;

            Setting.ExternalPrograms.Move(index, index + 1);
        }
    }
}
