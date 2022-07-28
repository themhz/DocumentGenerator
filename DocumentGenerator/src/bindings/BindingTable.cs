using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentGenerator
{
    public class BindingTable {
        public class Row {
            public BindingTable Table { get; protected set; }
            public DataRow DataRow { get; protected set; }
            
            protected object[] extraValues;

            public Row(BindingTable _table, DataRow row) {
                Table = _table;
                DataRow = row;
                extraValues = new object[16];
            }

            public string GetValueAsString(BindingField bindingField, List<Row> contextStack)
            {
                if (bindingField.Column != null)
                {
                    return Table._dataSource.GetValue(bindingField, contextStack, this);
                }
                else
                {
                    Dictionary<Guid, object> values;
                    if (Table.extraColumns.TryGetValue(bindingField, out values))
                    {
                        object obj;
                        if (values.TryGetValue((Guid)DataRow[0], out obj))
                        {
                            return formatValue(values[(Guid)DataRow[0]], bindingField);
                        }
                    }

                    //int indexField;
                    //if (Table.extraFields.TryGetValue(bindingField, out indexField))
                    //{
                    //    return extraValues[indexField].ToString();
                    //}

                    return null;
                }
            }

            public object GetObject(BindingField bindingField)
            {
                if (bindingField.Column != null)
                    return DataRow[bindingField.Column];
                else
                {
                    Dictionary<Guid, object> values;
                    if (Table.extraColumns.TryGetValue(bindingField, out values))
                    {
                        object obj;
                        if (values.TryGetValue((Guid)DataRow[0], out obj))
                        {
                            return values[(Guid)DataRow[0]];
                        }
                    }

                    //int indexField;
                    //if (Table.extraFields.TryGetValue(bindingField, out indexField))
                    //{
                    //    return extraValues[indexField].ToString();
                    //}

                    return null;
                }
            }

            public void Set(BindingField bindingField, object obj)
            {
                int index = Table.IndexOf(bindingField);

                if (index < Table.DefaultCount)
                {
                    DataRow[index] = obj;
                }
                else
                {
                    Dictionary<Guid, object> values;
                    if (Table.extraColumns.TryGetValue(bindingField, out values))
                    {
                        values[(Guid)DataRow[0]] = obj;
                    }

                    //int indexField;
                    //if (Table.extraFields.TryGetValue(bindingField, out indexField))
                    //{
                    //    extraValues[indexField] = obj;
                    //}
                }
            }

            public void Set(int index, object obj)
            {
                if (index < Table.DefaultCount)
                {
                    DataRow[index] = obj;
                }
                else
                {
                    Dictionary<Guid, object> values = Table.extraColumns[Table.BindingFields[index]];
                    values[(Guid)DataRow[0]] = obj;
                }
            }

            public bool InContext(List<Row> contextStack) {
                if (contextStack.Count > 0){
                    for (int index = contextStack.Count - 1; index >= 0; index--)
                    {
                        Row contextRow = contextStack[index];

                        // Check current table
                        if (contextRow.Table == Table)
                        {
                            return Equals(contextRow.DataRow[contextRow.Table.KeyColumn], DataRow[Table.KeyColumn]);
                        }

                        // Check relations
                        foreach (var relation in Table.BindingRelations)
                        {
                            if (contextRow.Table == relation.Table)
                            {
                                if (relation.ForeignKey.Column != null)
                                {
                                    return Equals(contextRow.DataRow[relation.Table.KeyColumn], DataRow[relation.ForeignKey.Column]);
                                }
                                else
                                {
                                    Dictionary<Guid, object> values;
                                    if (Table.extraColumns.TryGetValue(relation.ForeignKey, out values))
                                    {
                                        object fKey = GetObject(relation.ForeignKey);
                                        if (fKey != null)
                                        {
                                            return Equals(fKey, contextRow.DataRow[0]);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //foreach (var relation in Table.BindingRelations)
                    //{
                    //    // Έλεγχος αν κάποιος πίνακας από το context stack είναι Parent Table για αυτό το row
                    //    // Εάν είναι, τότε πρέπει να ελέγξουμε αν η row έχει relation με τη γραμμή του Parent Table που βρίσκεται στην context stack
                    //    for (int index = contextStack.Count - 1; index >= 0; index--)
                    //    {
                    //        Row row = contextStack[index];

                    //        if (row.Table == relation.Table)
                    //        {
                    //            if (relation.ForeignKey.Column != null)
                    //            {
                    //                return Equals(row.DataRow[relation.Table.KeyColumn], DataRow[relation.ForeignKey.Column]);
                    //            }
                    //            else
                    //            {
                    //                Dictionary<Guid, object> values;
                    //                if (Table.extraColumns.TryGetValue(relation.ForeignKey, out values))
                    //                {
                    //                    object obj;
                    //                    if (values.TryGetValue((Guid)DataRow[0], out obj))
                    //                    {
                    //                        return Equals(row.DataRow[relation.Table.KeyColumn], obj);
                    //                    }
                    //                }

                    //                //int indexField;
                    //                //if (Table.extraFields.TryGetValue(relation.ForeignKey, out indexField))
                    //                //{
                    //                //    return Equals(row.DataRow[relation.Table.KeyColumn], extraValues[indexField]);
                    //                //}
                    //            }
                    //        }
                    //    }
                    //}

                    // Έλεγχος αν κάποιος πίνακας από το context stack είναι ο πίνακας αυτού του row
                    //for (int index = contextStack.Count - 1; index >= 0; index--)
                    //{
                    //    Row row = contextStack[index];

                    //    if (row.Table == Table)
                    //    {
                    //        if (relation.ForeignKey.Column != null)
                    //        {
                    //            return Equals(row.DataRow[relation.Table.KeyColumn], DataRow[relation.ForeignKey.Column]);
                    //        }
                    //        else
                    //        {
                    //            Dictionary<Guid, object> values;
                    //            if (Table.extraColumns.TryGetValue(relation.ForeignKey, out values))
                    //            {
                    //                object obj;
                    //                if (values.TryGetValue((Guid)DataRow[0], out obj))
                    //                {
                    //                    return Equals(row.DataRow[relation.Table.KeyColumn], obj);
                    //                }
                    //            }

                    //            //int indexField;
                    //            //if (Table.extraFields.TryGetValue(relation.ForeignKey, out indexField))
                    //            //{
                    //            //    return Equals(row.DataRow[relation.Table.KeyColumn], extraValues[indexField]);
                    //            //}
                    //        }
                    //    }
                    //}

                }

                return true;
            }

            private string formatValue(object value, BindingField field)
            {

                if (Equals(value, DBNull.Value))
                {
                    return field.FormatNull;
                }

                if (value is int i)
                {
                    if (field.FormatString == string.Empty)
                    {
                        return i.ToString();
                    }
                    else
                    {
                        return string.Format(field.FormatString, i); // TODO: formatNegative
                    }
                }

                if (value is double d)
                {
                    if (field.FormatString == string.Empty)
                    {
                        return d.ToString(CultureInfo.CurrentUICulture);
                    }
                    else
                    {
                        return string.Format(field.FormatString, d); // TODO: formatNegative
                    }
                }

                if (value is decimal dc)
                {
                    if (field.FormatString == string.Empty)
                    {
                        return dc.ToString(CultureInfo.CurrentUICulture);
                    }
                    else
                    {
                        return string.Format(field.FormatString, dc); // TODO: formatNegative
                    }
                }

                if (value is string s)
                {
                    if (field.FormatString == string.Empty)
                    {
                        return s;
                    }
                    else
                    {
                        return string.Format(field.FormatString, s);
                    }
                }

                if (value is DateTime dt)
                {
                    if (field.FormatString == string.Empty)
                    {
                        return string.Format("{0:dd/MM/yyyy}");
                    }
                    else
                    {
                        return string.Format(field.FormatString, dt);
                    }
                }

                if (value is bool b)
                {
                    if (b == true)
                    {
                        return field.FormatString;
                    }
                    else
                    {
                        return field.FormatNull; // TODO: formatNegative
                    }
                }


                if (value is byte[] by)
                {
                    return Convert.ToBase64String(by, 0, by.Length);
                }

                return null;
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
        public DataTable DataTable { get; set; }
        public DataTable GroupedDataTable { get; set; }
        public DataColumn KeyColumn { get; protected set; }
        public List<BindingField> BindingFields { get; protected set; }
        public List<BindingRelation> BindingRelations { get; protected set; }
        public int Count { get { return DataTable.Rows.Count; } }
        public int DefaultCount { get { return BindingFields.Count - extraColumns.Count; } }

        public IDataSource _dataSource;
        private Dictionary<BindingField, Dictionary<Guid, object>> extraColumns = new Dictionary<BindingField, Dictionary<Guid, object>>();
        //private Dictionary<BindingField, int> extraFields = new Dictionary<BindingField, int>();

        public BindingTable(string name, string alias, DataTable table, DataColumn keyColumn, List<BindingField> bindingFields, List<BindingRelation> bindingRelations, IDataSource dataSource) 
        {
            Name = name;
            Alias = alias;
            DataTable = table;
            KeyColumn = keyColumn;
            BindingFields = bindingFields;
            BindingRelations = bindingRelations;

            _dataSource = dataSource;
        }

        public void Add(BindingField field)
        {
            BindingFields.Add(field);
            if (field.Column == null)
            {
                extraColumns.Add(field, new Dictionary<Guid, object>(2 * DataTable.Rows.Count));
                //extraFields.Add(field, extraFields.Count);
            }
        }

        public Row NewRow(Guid id)
        {
            DataRow row = DataTable.NewRow();
            row[0] = id;
            DataTable.Rows.Add(row);
            return new Row(this, row);
        }

        public int IndexOf(BindingField bindingField)
        {
            return BindingFields.IndexOf(bindingField);
        }

        public BindingRelation GetRelation(BindingField bindingField)
        {
            foreach (BindingRelation relation in BindingRelations)
            {
                if (relation.ForeignKey.Name == bindingField.Name)
                    return relation;
            }

            return null;
        }

        public int GetContextCount(List<Row> contextStack, Row row)
        {
            List<Row> stack = new List<Row>();
            stack.AddRange(contextStack);
            stack.Add(row);

            BindingTable.Enumerator enumerator = new BindingTable.Enumerator(this);
            int count = 0;

            enumerator.Start();
            while ((row = enumerator.Next()) != null)
            { // TODO: use index for performance
                if (row.InContext(stack))
                {
                    count++;
                }
            }

            return count;
        }
        
        public Row[] Where(string filter, string sort = "")
        {
            List<Row> rows = new List<Row>();

            if (filter.Trim() == string.Empty)
            {
                for (int index = 0; index< DataTable.Rows.Count; index++)
                {
                    rows.Add(new Row(this, DataTable.Rows[index]));
                }
            }
            else
            {
                var dataRows = DataTable.Select(filter, sort.Replace("and",","));
                foreach(var dataRow in dataRows)
                {
                    rows.Add(new Row(this, dataRow));
                }
            }

            return rows.ToArray();
        }

        //public void FilterBindingTable()
        //{
        //    if (this.DataTable.TableName == "Annex4VerticalOpaqueElements")
        //    {
        //        //var test = this.DataTable.Select("GroupIndex=1");
        //        IEnumerable<DataRow> query = from all in this.DataTable.AsEnumerable()
        //                                     where all.Field<int>("GroupIndex") == 1
        //                                     select all;


        //        //this.DataTable = query.CopyToDataTable<DataRow>();
        //    }
        //}
    }
}
