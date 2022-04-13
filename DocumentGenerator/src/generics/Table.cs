using System;
using System.Data;
using dxTable = DevExpress.XtraRichEdit.API.Native.Table;

namespace DocumentGenerator
{
    internal class Table
    {
        public dxTable Element { get; protected set; }
        public Range Range { get; protected set; }

        public Table(dxTable element, Range range) {
            Element = element;
            Range = range;
        }
    }
}
