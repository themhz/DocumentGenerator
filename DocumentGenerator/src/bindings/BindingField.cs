using System;
using System.Data;

namespace DocumentGenerator
{
    internal class BindingField
    {
        public BindingTable Table { get; protected set; }
        public string Name { get; protected set; }
        public string Alias { get; protected set; }
        public DataColumn Column { get; protected set; }
        public Type Type { get; protected set; }
        public string FormatString { get; protected set; }
        public string FormatNull { get; protected set; }

        public BindingField(BindingTable table, string _name, string _alias, DataColumn _column, string _formatString, string _formatNull)
        {
            Table = table;
            Name = _name;
            Alias = _alias;
            Column = _column;
            Type = _column == null ? typeof(Object) : _column.DataType;
            FormatString = _formatString;
            FormatNull = _formatNull;
        }
    }
}