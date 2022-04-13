using System;
using System.Data;

namespace DocumentGenerator {
    internal class BindingKey {
        public Guid Id { set; get; }
        public DataRow DataRow { set; get; }
        public DataTable DataTable { set; get; }

        public BindingKey(Guid id, DataRow dataRow, DataTable dataTable) {
            Id = id;
            DataRow = dataRow;
            DataTable = dataTable;
        }
    }
}