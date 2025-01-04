using CommunityToolkit.Mvvm.ComponentModel;
using NeeLaboratory.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NeeLaboratory.RealtimeSearch.ComponentModel
{
    public static class ObservableObjectExtensions
    {
        /// <summary>
        /// プロパティの変更通知を購読。
        /// 購読解除するDisposableオブジェクトを返す。
        /// </summary>
        public static IDisposable SubscribePropertyChanged(this ObservableObject self, PropertyChangedEventHandler handler)
        {
            self.PropertyChanged += handler;
            return new AnonymousDisposable(() => self.PropertyChanged -= handler);
        }

        public static IDisposable SubscribePropertyChanged(this ObservableObject self, string? propertyName, PropertyChangedEventHandler handler)
        {
            return SubscribePropertyChanged(self, PropertyChangedTools.CreateChangedEventHandler(propertyName, handler));
        }
    }
}
