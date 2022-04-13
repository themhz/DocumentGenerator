using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentGenerator
{
    class BindingTable {
        public string Name { get; protected set; }
        public string Alias { get; protected set; }
        public DataTable Table { get; protected set; }
        public DataColumn KeyColumn { get; protected set; }
        public List<BindingField> BindingFields { get; protected set; }

        public BindingTable(string _name, string _alias, DataTable _table, DataColumn _keyColumn, List<BindingField> _bindingFields) {
            Name = _name;
            Alias = _alias;
            Table = _table;
            KeyColumn = _keyColumn;
            BindingFields = _bindingFields;
        }

    }
}
