// from http://sourcechord.hatenablog.com/entry/20130303/1362315081
using System;
using System.ComponentModel;

namespace NeeLaboratory.ComponentModel
{
    public static class PropertyChangedTools
    {
        /// <summary>
        /// 特定のプロパティ名に対応した受信ハンドルを作る
        /// </summary>
        public static PropertyChangedEventHandler CreateChangedEventHandler(string? propertyName, PropertyChangedEventHandler handler)
        {
            if (handler is null) throw new ArgumentNullException(nameof(handler));

            return new PropertyChangedEventHandler((s, e) =>
            {
                // NOTE: propertyNameが空の場合も実行
                if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == propertyName)
                {
                    handler.Invoke(s, e);
                }
            });
        }

        public static PropertyChangingEventHandler CreateChangingEventHandler(string? propertyName, PropertyChangingEventHandler handler)
        {
            if (handler is null) throw new ArgumentNullException(nameof(handler));

            return new PropertyChangingEventHandler((s, e) =>
            {
                // NOTE: propertyNameが空の場合も実行
                if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == propertyName)
                {
                    handler.Invoke(s, e);
                }
            });
        }
    }

}
