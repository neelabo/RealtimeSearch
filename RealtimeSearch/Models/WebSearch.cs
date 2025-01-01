using System.Diagnostics;
using System.Text.RegularExpressions;

namespace NeeLaboratory.RealtimeSearch.Models
{
    public class WebSearch
    {
        private readonly AppSettings _settings;


        public WebSearch(AppSettings settings)
        {
            _settings = settings;
        }


        public void Search(string keyword)
        {
            //URLで使えない特殊文字。ひとまず変換なしで渡してみる
            //\　　'　　|　　`　　^　　"　　<　　>　　)　　(　　}　　{　　]　　[

            // キーワード整形。空白を"+"にする
            string query = keyword.Trim();
            if (string.IsNullOrEmpty(query)) return;
            query = query.Replace("%", "%25");
            query = query.Replace("+", "%2B");
            query = Regex.Replace(query, @"\s+", "+");

            string url = _settings.WebSearchFormat.Replace("$(query)", query);
            Debug.WriteLine(url);

            var startInfo = new ProcessStartInfo(url) { UseShellExecute = true };
            Process.Start(startInfo);
        }
    }

}
