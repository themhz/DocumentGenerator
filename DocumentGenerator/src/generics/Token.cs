using DevExpress.XtraRichEdit.API.Native;
using System;
using System.Data;

namespace DocumentGenerator
{
    internal class Token
    {
        public string Alias { get; protected set; }
        public string Original { get; protected set; }
        public Range Range { get; protected set; }
        public TableRow Row { get; protected set; }

        public Token(string alias, string original, Range range, TableRow row) {
            Alias = alias;
            Original = original;
            Range = range;
            Row = row;
        }
    }
}
