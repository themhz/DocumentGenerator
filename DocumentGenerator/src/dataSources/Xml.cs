
using System;
using System.Collections.Generic;
using System.Xml;
using System.Data;
using System.Linq;
using System.Configuration;
using System.Globalization;

namespace DocumentGenerator {
    public class Xml : IDataSource {
        //private DataSet _dataSet;
        private Dictionary<string, DataColumn> columnsIndex;
        private Dictionary<string, DataTable> tablesIndex;
        private DataSet dsGroups = new DataSet();
       
        public Xml(List<DataSet> dataSets) {
            createIndexes(dataSets);
        }

        public string GetValue(BindingField field, List<BindingTable.Row> contextStack, BindingTable.Row currentRow) {
            BindingTable table = field.Table;

            if (currentRow != null && currentRow.Table == table) {
                return GetValue(field, currentRow);
            }

            for (int index = contextStack.Count - 1; index>= 0; index--) {
                BindingTable.Row row = contextStack[index];
                if (row.Table == field.Table) {
                    return GetValue(field, row);
                }
            }

            return GetValue(field, 0);
        }

        public string GetContextValue(BindingField field, int index, List<BindingTable.Row> contextStack) {
            int count = field.Table.Count;
            int currentIndex = -1;

            BindingTable.Row row;
            BindingTable bindingTable = field.Table;
            BindingTable.Enumerator enumerator = new BindingTable.Enumerator(bindingTable);

            enumerator.Start();
            while ((row = enumerator.Next()) != null) { // TODO: use index for performance
                if (row.InContext(contextStack)) {
                    currentIndex++;

                    if (currentIndex == index) {
                        return GetValue(field, row);
                    }
                }
            }

            return null;
        }

        public string GetValue(BindingField field, int index = 0) {
            DataColumn column = field.Column;
            DataTable table = column.Table;

            if (table.Rows.Count > index) {
                object value = table.Rows[index][column];

                return formatValue(value, field);
            }

            return null;
        }

        public string GetValue(BindingField field, BindingTable.Row row) {
            DataColumn column = field.Column;

            object value = row.DataRow[column];

            return formatValue(value, field);
        }

        public BindingTable GetTable(string tableName, string alias) {
            DataTable table = null;

            if (tablesIndex.TryGetValue(tableName, out table)) {
                DataColumn keyColumn;
                if (table.PrimaryKey != null && table.PrimaryKey.Length > 0)
                    keyColumn = table.PrimaryKey[0];
                else
                    keyColumn = table.Columns[0];

                return new BindingTable(tableName, alias, table, keyColumn, new List<BindingField>(), new List<BindingRelation>(), this);
            }

            return null;
        }

        public BindingTable GetGroup(BindingTable table, string groupName)
        {
            // Create Table
            DataTable dataTable = new DataTable(groupName);
            DataColumn columnId = new DataColumn("ID", typeof(Guid));
            dataTable.Columns.Add(columnId);
            dataTable.PrimaryKey = new DataColumn[] { columnId };

            // Add Table to DataSet
            dsGroups.Tables.Add(dataTable);

            // Create Binding Table
            BindingTable bindingGroup = new BindingTable(groupName, groupName, dataTable, dataTable.Columns[0], new List<BindingField>(), new List<BindingRelation>(), this);
            bindingGroup.BindingFields.Add(new BindingField(bindingGroup, columnId.ColumnName, columnId.ColumnName, columnId, "", ""));

            return bindingGroup;
        }

        public void GetRelations(Dictionary<string, BindingTable> tables, Dictionary<string, BindingField> fields) {
            foreach (var pair in tables) {
                BindingTable table = pair.Value;
                var dataRelations = table.DataTable.ParentRelations;

                foreach (DataRelation dataRelation in dataRelations) {
                    string parentTableName = dataRelation.ParentTable.TableName;
                    DataColumn column = dataRelation.ChildColumns[0];
                    BindingTable parentTable;
                    if (tables.TryGetValue(parentTableName, out parentTable)) {
                        BindingField foreignKey;
                        if (fields.TryGetValue($"{table.Name}.{column.ColumnName}", out foreignKey)) {
                            table.BindingRelations.Add(new BindingRelation(parentTable, foreignKey));
                        }
                        else {
                            table.BindingRelations.Add(new BindingRelation(parentTable, new BindingField(table, column.ColumnName, column.ColumnName, column, "", "")));
                        }
                    }
                }
            }
        }

        public BindingField GetField(BindingTable table, string fieldName, string alias, string formatString, string formatNull) {
            DataColumn column = null;
            
            if (columnsIndex.TryGetValue($"{table.Name}.{fieldName}", out column)) {
                return new BindingField(table, fieldName, alias, column, formatString.Trim(), formatNull);
            }

            return null;
        }      

        private void createIndexes(List<DataSet> dataSets) {

            tablesIndex = new Dictionary<string, DataTable>();
            columnsIndex = new Dictionary<string, DataColumn>();

            foreach (DataSet dataSet in dataSets) {
                foreach (DataTable table in dataSet.Tables) {
                    tablesIndex.Add(table.TableName, table);
                    foreach (DataColumn column in table.Columns) {
                        columnsIndex.Add(table.TableName + "." + column.ColumnName, column);
                    }
                }
            }
        }
        public List<BindingRelation> getTableRelations(List<DataSet> dataSets) {

            foreach (DataSet dataSet in dataSets) {
                foreach (DataTable table in dataSet.Tables) {

                }
            }
            return null;
        }        

        private string formatValue(object value, BindingField field) {

            if (Equals(value, DBNull.Value)) {
                return field.FormatNull;
            }

            if (value is int i) {
                if (field.FormatString == string.Empty) {
                    return i.ToString();
                } else {
                    return string.Format(field.FormatString, i); // TODO: formatNegative
                }
            }

            if (value is double d) {
                if (field.FormatString == string.Empty) {
                    return d.ToString(CultureInfo.CurrentUICulture);
                } else {
                    return string.Format(field.FormatString, d); // TODO: formatNegative
                }
            }

            if (value is decimal dc) {
                if (field.FormatString == string.Empty) {
                    return dc.ToString(CultureInfo.CurrentUICulture);
                } else {
                    return string.Format(field.FormatString, dc); // TODO: formatNegative
                }
            }

            if (value is string s) {
                if (field.FormatString == string.Empty) {
                    return s;
                } else {
                    return string.Format(field.FormatString, s);
                }
            }

            if (value is DateTime dt) {
                if (field.FormatString == string.Empty) {
                    return string.Format("{0:dd/MM/yyyy}");
                } else {
                    return string.Format(field.FormatString, dt);
                }
            }

            if (value is bool b) {
                if (b == true) {
                    return field.FormatString;
                } else {
                    return field.FormatNull; // TODO: formatNegative
                }
            }


            if ( value is byte[] by) {
                return Convert.ToBase64String(by, 0, by.Length);
            }

            return null;
        }
       
    }
}
