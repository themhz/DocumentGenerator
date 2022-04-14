using System;
using System.Data;

namespace DocumentGenerator {
    public class BindingKey {
        public BindingRow Row { set; get; }
        public BindingTable Table { set; get; }

        public BindingKey(BindingRow row, BindingTable dataTable) {
            Row = row;
            Table = dataTable;
        }
    }
}