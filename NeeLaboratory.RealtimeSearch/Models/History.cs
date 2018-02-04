using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace NeeLaboratory.RealtimeSearch
{

    /// <summary>
    /// 検索履歴
    /// </summary>
    public class History
    {
        public ObservableCollection<string> Collection { get; private set; }

        //
        public History()
        {
            Collection = new ObservableCollection<string>();
            BindingOperations.EnableCollectionSynchronization(Collection, new object());
        }


        //
        public void Add(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return;

            if (this.Collection.Count <= 0)
            {
                this.Collection.Add(keyword);
            }
            else if (Collection.First() != keyword)
            {
                int index = Collection.IndexOf(keyword);
                if (index > 0)
                {
                    Collection.Move(index, 0);
                }
                else
                {
                    this.Collection.Insert(0, keyword);
                }
            }

            while (Collection.Count > 6)
            {
                Collection.RemoveAt(Collection.Count - 1);
            }
        }
    }
}
