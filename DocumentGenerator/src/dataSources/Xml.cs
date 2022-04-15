
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

        //public Xml(string _xmlPath) {
        //    columnsIndex = getDictionary(_xmlPath);
        //}

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
            }

            return null;
        }

        public string GetValue(BindingField field, BindingTable.Row row) {
            DataColumn column = field.Column;

            object value = row.DataRow[column];

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

            return null;
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

        //private Dictionary<string, DataColumn> getDictionary(string xmlPath) {
        //    var dictionary = new Dictionary<String, DataColumn>();

        //    _dataSet = new DataSet();
        //    _dataSet.ReadXmlSchema(xmlPath);
        //    _dataSet.ReadXml(xmlPath, XmlReadMode.ReadSchema);

           
        //    foreach (DataTable table in _dataSet.Tables) {
        //        foreach (DataColumn column in table.Columns) {
        //            dictionary.Add(table.TableName + "." + column.ColumnName, column);
        //        }
        //    }

        //    return dictionary;
        //}

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

        //Function to get any field by id. You need to specify parentnode.childnode in the field parameter, and the primary key ID
        //public string GetValueByID(string field, string id="") {

        //    string[] fields = field.Split('.');
        //    //var element = this.getList(fields[0] + "[ns:ID='" + id + "']");
        //    var element = this.getList(fields[0], "ID", id);
        //    return element[0][fields[1]].InnerText;
        //}

        //public XmlNodeList getList(string node, string field, string value) {

        //    XmlDocument xmlDoc = new XmlDocument();
        //    xmlDoc.Load(this.xmlPath);
        //    var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
        //    string selector = node;
        //    if (value == "") {
        //        selector += $"[ns:{field}]";
        //    } else {
        //        selector += $"[ns:{field}='" + value + "']";
        //    }

        //    nsmgr.AddNamespace("ns", "http://www.civilteam.gr/dsBuildingHeatInsulation.xsd");
        //    XmlNodeList nodeList = xmlDoc.DocumentElement.SelectNodes($"//ns:dsBuildingHeatInsulation//ns:{selector}", nsmgr);

        //    return nodeList;
        //}

        //public Dictionary<String, DataColumn> getXmlDictionary() {
        //    XmlTextReader reader = new XmlTextReader(this.xmlPath);
        //    while (reader.Read()) {
        //        // Do some work here on the data.
        //        Console.WriteLine(reader.Name);
        //    }
        //    Console.ReadLine();
        //    return null;
        //}

        // TODO Need to do some work
        //public object GetValueByLinq() {
        //    //https://www.c-sharpcorner.com/blogs/inner-join-and-outer-join-in-datatable-using-linq
        //    //Inner join
        //    DataTable PageA = _dataSet.Tables["PageA"];
        //    DataTable PageADetails = _dataSet.Tables["PageADetails"];

        //    var JoinResult = (from pageA in PageA.AsEnumerable()
        //                      join pageADetails in PageADetails.AsEnumerable()
        //                      on pageA.Field<string>("ID") equals pageADetails.Field<string>("PageADetailID")
        //                      select new {
        //                          id = pageA.Field<string>("ID"),
        //                          pageAName = pageA.Field<string>("Name"),
        //                          pageATypeName = pageA.Field<string>("TypeName"),
        //                          pageADetailsName = pageADetails.Field<string>("Name")
        //                      }).ToList();

        //    //Select where
        //    //DataTable SelectedTable = _dataSet.Tables["PageADetails"];            
        //    //IEnumerable<DataRow> filter = SelectedTable.AsEnumerable().
        //    //           Where(
        //    //               x => x.Field<string>("PageADetailID") == "77ff0e87-ea58-4e71-9fcb-78294125b76a"
        //    //           );

        //    //Select all 
        //    //SelectedTable = _dataSet.Tables["PageBLevelFaceElements"];
        //    //query = from table in SelectedTable.AsEnumerable() select table;

        //    Console.WriteLine("Start");
        //    foreach (var p in JoinResult) {
        //        //Console.WriteLine(((DataRow)p).Field<string>("ID"));
        //        Console.WriteLine(p.id + " | " + p.pageAName + " | " + p.pageATypeName + " | " + p.pageADetailsName);
        //    }

        //    return null;
        //}

    }
}
