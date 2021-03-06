using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentGenerator {
    public interface IDataSource {
        BindingTable GetTable(string tableName, string alias);
        BindingTable GetGroup(BindingTable table, string groupName);
        BindingField GetField(BindingTable table, string fieldName, string alias, string formatString, string formatNull);
        string GetValue(BindingField field, List<BindingTable.Row> contextRows, BindingTable.Row currentRow);
        string GetValue(BindingField field, int index = 0);
        string GetValue(BindingField field, BindingTable.Row row);
        string GetContextValue(BindingField field, int index, List<BindingTable.Row> contextStack);
        void GetRelations(Dictionary<string, BindingTable> tables, Dictionary<string, BindingField> fields);
    }
}
