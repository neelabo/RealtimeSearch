using NeeLaboratory.Generators;
using NeeLaboratory.RealtimeSearch.Models;
using NeeLaboratory.RealtimeSearch.TextResource;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace NeeLaboratory.RealtimeSearch.ViewModels
{
    [NotifyPropertyChanged]
    public partial class SettingGeneralViewModel : INotifyPropertyChanged
    {
        public SettingGeneralViewModel(AppSettings setting)
        {
            Setting = setting;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public AppSettings Setting { get; }
        
        public List<CultureItem> CultureList { get; } = TextResources.LanguageResource.Cultures.Select(e => new CultureItem(e.Name, e.NativeName)).ToList();
    }


    public record CultureItem(string Name, string NativeName);
}
