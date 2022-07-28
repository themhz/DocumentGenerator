using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentGenerator
{
    public class BindingInclude {
        public string Alias { get; protected set; }
        public string File { get; protected set; }
        public string Where { get; protected set; }
        public BindingTable Table { get; protected set; }
        public BindingTable FilterTable { get; protected set; }

        public BindingInclude(string alias, string file, BindingTable table, BindingTable filterTable, string where) {
            Where = where;
            Alias = alias;
            Table = table;
            File = file;
            FilterTable = filterTable;
        }
    }
}
