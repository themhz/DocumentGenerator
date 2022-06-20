using System;
using System.Data;
using System.Collections.Generic;

namespace DocumentGenerator
{
    public class GroupValue
    {
        private List<object> _values;
        public int HashCode { get; protected set; } = 0;
        public int Count { get { return _values.Count; } }

        public GroupValue()
        {
            _values = new List<object>();
        }

        public object this[int index]
        {
            get { return _values[index]; }
        }

        public void Add(object value)
        {
            _values.Add(value);            
            HashCode = value == null ? 0 : value.GetHashCode();                        
        }

        public override bool Equals(object obj)
        {
            if (obj is GroupValue group)
            {
                if (group.Count != Count)
                    return false;

                for (int index = 0; index < _values.Count; index++)
                {
                    if (!Equals(_values[index], group._values[index]))
                    {
                        return false;
                    }
                }

                return true;
            }    

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode;
        }
    }
}
