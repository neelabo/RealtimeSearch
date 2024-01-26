using System.ComponentModel;

namespace NeeLaboratory.IO.Search.Files
{
    public interface ISearchContext : INotifyPropertyChanged
    {
        bool AllowFolder { get; set; }
    }
}
