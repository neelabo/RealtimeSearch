using System.Windows;

namespace NeeLaboratory.RealtimeSearch
{
    public class ShowMessageBoxMessage
    {
        private const string _defaultCaption = "RealtimeSearch";

        public ShowMessageBoxMessage(string message)
            : this(message, null, MessageBoxButton.OK, MessageBoxImage.None)
        {
        }

        public ShowMessageBoxMessage(string message, MessageBoxButton button)
            : this(message, null, button, MessageBoxImage.None)
        {
        }

        public ShowMessageBoxMessage(string message, MessageBoxImage icon)
            : this(message, null, MessageBoxButton.OK, icon)
        {
        }

        public ShowMessageBoxMessage(string message, string? caption, MessageBoxButton button, MessageBoxImage icon)
        {
            Message = message;
            Caption = caption ?? _defaultCaption;
            Button = button;
            Icon = icon;
        }


        public string Message { get; }
        public string Caption { get; }
        public MessageBoxButton Button { get; }
        public MessageBoxImage Icon { get; }
        public MessageBoxResult Result { get; set; }
    }
}
