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

            public string GetValue(BindingField bindingField, List<Row> contextStack) {

                return Table._dataSource.GetValue(bindingField, contextStack, this);
            }

            public bool InContext(List<Row> contextStack) {
                foreach(var relation in Table.BindingRelations) {
                    // Έλεγχος αν κάποιος πίνακας από το context stack είναι Parent Table για αυτή τη σχέση
                    for (int index = contextStack.Count - 1; index >= 0; index--) {
                        Row row = contextStack[index];
                        if (row.Table == relation.Table) {
                            return Equals(row.DataRow[relation.Table.KeyColumn], DataRow[relation.ForeignKey.Column]);
                        }
                    }
                }

                return true;
            }
        }

        public class Enumerator {
            private int _count = 0;
            private BindingTable _table;

            public int Index { get { return _count - 1; } }
            public int Remaining { get { return _table.DataTable.Rows.Count - _count; } }

            public Enumerator(BindingTable table) {
                _table = table;
            }

            public void Start() {
                _count = 0;
            }

            public BindingTable.Row Next() {
                if (_count < _table.DataTable.Rows.Count) {
                    return new BindingTable.Row(_table, _table.DataTable.Rows[_count++]);
                } else {
                    return null;
                }
            }
        }

        public string Name { get; protected set; }
        public string Alias { get; protected set; }
        public DataTable DataTable { get; protected set; }
        public DataColumn KeyColumn { get; protected set; }
        public List<BindingField> BindingFields { get; protected set; }
        public List<BindingRelation> BindingRelations { get; protected set; }
        public int Count { get { return DataTable.Rows.Count; } }

        private IDataSource _dataSource;

        public BindingTable(string name, string alias, DataTable table, DataColumn keyColumn, List<BindingField> bindingFields, List<BindingRelation> bindingRelations, IDataSource dataSource) {
            Name = name;
            Alias = alias;
            DataTable = table;
            KeyColumn = keyColumn;
            BindingFields = bindingFields;
            BindingRelations = bindingRelations;

            _dataSource = dataSource;
        }

    }
}
