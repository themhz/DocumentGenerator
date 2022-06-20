using System;
using System.Data;

namespace DocumentGenerator
{
    public class BindingField
    {
        public BindingTable Table { get; protected set; }
        public string Name { get; protected set; }
        public string FullName { get { return _fullName; } } private string _fullName;
        public string Alias { get; protected set; }
        public DataColumn Column { get; protected set; }
        public Type Type { get; protected set; }
        public string FormatString { get; protected set; }
        public string FormatNull { get; protected set; }

        public BindingField(BindingTable table, string name, string alias, DataColumn column, string formatString, string formatNull)
        {
            Table = table;
            Name = name;
            _fullName = string.Format("{0}.{1}", table.Name, name);
            Alias = alias;
            Column = column;
            Type = column == null ? typeof(Object) : column.DataType;
            FormatString = formatString;
            FormatNull = formatNull;
        }
    }
}