using System;
using System.Data;
using dxTableRow = DevExpress.XtraRichEdit.API.Native.TableRow;

namespace DocumentGenerator
{
    internal class TableRow
    {
        public Table Table { get; protected set; }
        public dxTableRow Element { get; protected set; }
        public Range Range { get; protected set; }
        public int Index { get; protected set; }

        public TableRow(Table table, dxTableRow element, Range range, int index) {
            Table = table;
            Element = element;
            Range = range;
            Index = index;
        }
    }
}
