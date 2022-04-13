using System;
using System.Data;
using dxComment = DevExpress.XtraRichEdit.API.Native.Comment;

namespace DocumentGenerator
{
    internal class Comment
    {
        public dxComment Element { get; protected set; }
        public Table Table { get; protected set; }
        public Range Range { get; protected set; }

        public Comment(dxComment element, Table table, Range range) {
            Element = element;
            Table = table;
            Range = range;
        }
    }
}
