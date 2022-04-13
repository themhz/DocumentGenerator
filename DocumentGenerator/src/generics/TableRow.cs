using System;
using System.Data;
using dxTableRow = DevExpress.XtraRichEdit.API.Native.TableRow;

namespace DocumentGenerator
{
    internal class TableRow
    {
        public dxTableRow Element { get; protected set; }
        public Range Range { get; protected set; }

        public TableRow(dxTableRow element, Range range) {
            Element = element;
            Range = range;
        }
    }
}
