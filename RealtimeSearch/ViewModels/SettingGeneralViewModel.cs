using NeeLaboratory.Generators;
using NeeLaboratory.RealtimeSearch.Models;
using System.ComponentModel;

namespace NeeLaboratory.RealtimeSearch.ViewModels
{
    [NotifyPropertyChanged]
    public partial class SettingGeneralViewModel : INotifyPropertyChanged
    {
        public SettingGeneralViewModel(AppConfig setting)
        {
            Setting = setting;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public AppConfig Setting { get; }
    }
}
