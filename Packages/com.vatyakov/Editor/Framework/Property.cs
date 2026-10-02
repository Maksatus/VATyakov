using System;
using System.Collections.Generic;

namespace VATyakov.Editor
{
    internal sealed class Property<T>
    {
        private T _value;

        public event Action<T> Changed;

        public T Value
        {
            get => _value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(_value, value))
                {
                    return;
                }

                _value = value;
                Changed?.Invoke(value);
            }
        }

        public Property(T value = default)
        {
            _value = value;
        }
    }
}
