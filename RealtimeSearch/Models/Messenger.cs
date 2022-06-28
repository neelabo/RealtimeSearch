using System;
using System.Collections.Generic;
using System.Linq;

namespace NeeLaboratory.RealtimeSearch
{
    public class Messenger
    {
        private Dictionary<Type, Delegate> _map = new Dictionary<Type, Delegate>();

        public void Register<TMessage>(Action<object?, TMessage> action)
        {
            _map.Add(typeof(TMessage), action);
        }

        public void Send<TMessage>(object? sender, TMessage message) where TMessage : notnull
        {
            if (_map.TryGetValue(typeof(TMessage), out var value))
            {
                var action = value as Action<object?, TMessage>;
                action?.Invoke(sender, message);
            }
        }
    }
}
