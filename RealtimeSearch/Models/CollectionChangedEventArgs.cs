using System;

namespace NeeLaboratory.RealtimeSearch
{
    public class CollectionChangedEventArgs<T> : EventArgs
    {
        public CollectionChangedEventArgs(CollectionChangedAction action, T? item)
        {
            Action = action;
            Item = item;
        }

        public CollectionChangedAction Action { get; }
        public T? Item { get; } 
    }

    public enum CollectionChangedAction
    {
        Add,
        Remove,
    }

}
