using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentGenerator
{
    public class BindingTable {
        public class Row {
            public BindingTable Table { get; protected set; }
            public DataRow DataRow { get; protected set; }

            public Row(BindingTable _table, DataRow row) {
                Table = _table;
                DataRow = row;
            }

            public string GetValue(BindingField bindingField) {

                return Table._dataSource.GetValue(bindingField, this);
            }
        }

        public string Name { get; protected set; }
        public string Alias { get; protected set; }
        public DataTable DataTable { get; protected set; }
        public DataColumn KeyColumn { get; protected set; }
        public List<BindingField> BindingFields { get; protected set; }
        public int Count { get { return DataTable.Rows.Count; } }

        private IDataSource _dataSource;
        private int _count = 0;

        public BindingTable(string name, string alias, DataTable table, DataColumn keyColumn, List<BindingField> bindingFields, IDataSource dataSource) {
            Name = name;
            Alias = alias;
            DataTable = table;
            KeyColumn = keyColumn;
            BindingFields = bindingFields;

            _dataSource = dataSource;
        }

        public void Start() {
            _count = 0;
        }

        public BindingTable.Row Next() {
            if (_count < DataTable.Rows.Count) {
                return new BindingTable.Row(this, DataTable.Rows[_count++]);
            } else {
                return null;
            }
        }

        
    }
}
