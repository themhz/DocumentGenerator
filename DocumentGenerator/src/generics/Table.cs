using System;
using System.Data;
using System.Collections.Generic;
using dxTable = DevExpress.XtraRichEdit.API.Native.Table;

namespace DocumentGenerator
{
    internal class Table
    {
        public dxTable Element { get; protected set; }
        public Range Range { get; protected set; }
        public List<Token> Tokens { get; set; }
        public int HeaderCount { get; set; }
        public int BodyCount { get; set; }
        public int FooterCount { get; set; }

        public Table(dxTable element, Range range) {
            Element = element;
            Range = range;
        }
    }
}
