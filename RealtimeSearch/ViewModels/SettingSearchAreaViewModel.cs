using CommunityToolkit.Mvvm.ComponentModel;
using NeeLaboratory.Generators;
using NeeLaboratory.IO.Search.Files;
using NeeLaboratory.RealtimeSearch.Models;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace NeeLaboratory.RealtimeSearch.ViewModels
{
    public partial class SettingSearchAreaViewModel : ObservableObject
    {

        private CollectionViewSource? _collectionViewSource;
        private FileArea? _selectedArea;


        public SettingSearchAreaViewModel(AppSettings setting)
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

            // insert in order by path
            int index;
            for (index = 0; index < Setting.SearchAreas.Count; index++)
            {
                if (Setting.SearchAreas[index].Path.CompareTo(area.Path) > 0)
                {
                    break;
                }
            }
            Setting.SearchAreas.Insert(index, area);

            SelectedArea = area;
        }

        public void RemoveSearchPath(FileArea area)
        {
            Setting.SearchAreas.Remove(area);
        }
    }
}
