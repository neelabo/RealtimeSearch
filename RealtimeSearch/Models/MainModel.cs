using NeeLaboratory.Generators;
using NeeLaboratory.IO.Search.Files;
using NeeLaboratory.RealtimeSearch.Clipboards;
using NeeLaboratory.Threading;
using NeeLaboratory.Windows.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace NeeLaboratory.RealtimeSearch.Models
{
    [NotifyPropertyChanged]
    public partial class MainModel : INotifyPropertyChanged
    {
        private readonly AppConfig _appConfig;
        private string _inputKeyword = "";
        private readonly DispatcherDelayValue<string> _keyword;
        private Search _search;
        private readonly WebSearch _webSearch;
        private readonly History _history;
        private string _resultMessage = "";
        private readonly DispatcherTimer _timer;


        private ClipboardSearch? _clipboardSearch;

        public MainModel(AppConfig appConfig)
        {
            _appConfig = appConfig;

            _keyword = new DispatcherDelayValue<string>("");
            _keyword.ValueChanged += async (s, e) => await SearchAsync(false);

            _search = new Search(appConfig);
            _search.SubscribePropertyChanged(nameof(Search.IsCollectBusy), Search_IsCollectBusyChanged);
            _search.SubscribePropertyChanged(nameof(Search.IsSearchBusy), Search_IsSearchBusyChanged);
            _search.SearchResultChanged += Search_SearchResultChanged;
            _search.SearchResultDecorator = new SearchResultDecorator();

            _webSearch = new WebSearch(appConfig);

            _history = new History();
            BindingOperations.EnableCollectionSynchronization(_history.Collection, new object());

            _timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(1000),
            };
            _timer.Tick += ProgressTimer_Tick;
        }



        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler? SearchResultChanged;


        public Search Search => _search;

        public string InputKeyword
        {
            get { return _inputKeyword; }
            set
            {
                if (SetProperty(ref _inputKeyword, value))
                {
                    SetKeywordDelay(_inputKeyword);
                }
            }
        }

        public History History => _history;

        public string ResultMessage
        {
            get { return _resultMessage; }
            set { SetProperty(ref _resultMessage, value); }
        }



        private void ProgressTimer_Tick(object? sender, EventArgs e)
        {
            _search.UpdateInformation();
        }

        public void Loaded()
        {
            var cache = _search.LoadCache();
            _search.ReIndex(cache);
        }

        public void Closed()
        {
            _search.SaveCache();
        }

        public void SetKeyword(string keyword)
        {
            _keyword.SetValue(keyword, 0, true);
        }

        public void SetKeywordDelay(string keyword)
        {
            _keyword.SetValue(keyword, 200);
        }

        public void StartClipboardWatch(Window window)
        {
            // クリップボード監視
            _clipboardSearch = new ClipboardSearch(_appConfig);
            _clipboardSearch.ClipboardChanged += ClipboardSearch_ClipboardChanged;
            _clipboardSearch.Start(window);
        }

        public async Task SearchAsync(bool isForce)
        {
            await _search.SearchAsync(_keyword.Value.Trim(), isForce);
        }

        public void WebSearch()
        {
            _webSearch.Search(_keyword.Value);
        }

        private void Search_IsCollectBusyChanged(object? sender, PropertyChangedEventArgs e)
        {
            UpdateInformationTimer();
        }

        private void Search_IsSearchBusyChanged(object? sender, PropertyChangedEventArgs e)
        {
            UpdateInformationTimer();
        }

        private void UpdateInformationTimer()
        {
            _timer.IsEnabled = _search.IsCollectBusy || _search.IsSearchBusy;
        }

        private void Search_SearchResultChanged(object? sender, EventArgs e)
        {
            SearchResultChanged?.Invoke(sender, EventArgs.Empty);

            if (_search.SearchResult != null && _search.SearchResult.Items.Count == 0)
            {
                ResultMessage = $"条件に一致する項目はありません。";
            }
            else
            {
                ResultMessage = "";
            }
        }

        public void StopClipboardWatch()
        {
            _clipboardSearch?.Stop();
        }

        private void ClipboardSearch_ClipboardChanged(object? sender, ClipboardChangedEventArgs e)
        {
            InputKeyword = e.Keyword;

            SetKeyword(e.Keyword);
            AddHistory();
        }

        public void AddHistory()
        {
            var keyword = _search.SearchResult?.Keyword ?? "";
            _history.Add(keyword);
        }

        public void CopyFilesToClipboard(List<FileContent> files)
        {
            ClipboardTools.SetFileDropList(files.Select(e => e.Path).ToArray());
        }

        public void CopyNameToClipboard(FileContent file)
        {
            var text = file.IsDirectory
                ? System.IO.Path.GetFileName(file.Path)
                : System.IO.Path.GetFileNameWithoutExtension(file.Path);
            ClipboardTools.SetText(text);
        }

        public void CopyNameToClipboard(List<FileContent> files)
        {
            var text = string.Join(Environment.NewLine, files.Select(e => e.IsDirectory ? System.IO.Path.GetFileName(e.Path) : System.IO.Path.GetFileNameWithoutExtension(e.Path)));
            ClipboardTools.SetText(text);
        }

        public void OpenPlace(List<FileContent> items)
        {
            foreach (var item in items)
            {
                if (System.IO.File.Exists(item.Path) || System.IO.Directory.Exists(item.Path))
                {
                    var startInfo = new ProcessStartInfo("explorer.exe", "/select,\"" + item.Path + "\"") { UseShellExecute = false };
                    Process.Start(startInfo);
                }
            }
        }

    }


    public class SearchResultDecorator : ISearchResultDecorator<FileContent>
    {
        public void Decorate(SearchResult<FileContent> searchResult)
        {
            BindingOperations.EnableCollectionSynchronization(searchResult.Items, new object());
        }
    }
}
