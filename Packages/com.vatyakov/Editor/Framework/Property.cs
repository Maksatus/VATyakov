using System;
using System.Collections.Generic;

namespace VATyakov.Editor
{
    sealed class Property<T>
    {
        T _value;

        public Property(T value = default)
        {
            _value = value;
        }

        public event Action<T> Changed;

        public T Value
        {
            get => _value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(_value, value))
                    return;
                _value = value;
                Changed?.Invoke(value);
            }
        }
    }
}
