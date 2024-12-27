using NeeLaboratory.Generators;
using NeeLaboratory.IO.Search.Files;
using NeeLaboratory.RealtimeSearch.Models;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace NeeLaboratory.RealtimeSearch.ViewModels
{
    [NotifyPropertyChanged]
    public partial class SettingSearchAreaViewModel : INotifyPropertyChanged
    {

        private CollectionViewSource? _collectionViewSource;
        private FileArea? _selectedArea;


        public SettingSearchAreaViewModel(AppSettings setting)
        {
            Setting = setting;
            UpdateCollectionViewSource();
        }


        public event PropertyChangedEventHandler? PropertyChanged;


        public AppSettings Setting { get; }


        public CollectionViewSource? CollectionViewSource
        {
            get { return _collectionViewSource; }
            set { SetProperty(ref _collectionViewSource, value); }
        }

        public FileArea? SelectedArea
        {
            get { return _selectedArea; }
            set { SetProperty(ref _selectedArea, value); }
        }


        private void UpdateCollectionViewSource()
        {
            var collectionViewSource = new CollectionViewSource
            {
                Source = Setting.SearchAreas
            };
            collectionViewSource.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(FileArea.Path), System.ComponentModel.ListSortDirection.Ascending));

            CollectionViewSource = collectionViewSource;
            SelectedArea = null;
        }

        public void AddSearchPath(string path)
        {
            if (!System.IO.Directory.Exists(path)) return;

            var area = new FileArea(path, true);
            var existArea = Setting.SearchAreas.FirstOrDefault(p => p.Path == area.Path);

            if (existArea != null)
            {
                SelectedArea = existArea;
                return;
            }

            Setting.SearchAreas.Add(area);
            SelectedArea = area;
        }

        public void RemoveSearchPath(FileArea area)
        {
            Setting.SearchAreas.Remove(area);
        }
    }
}
