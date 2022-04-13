using DevExpress.XtraRichEdit.API.Native;
using System;
using System.Data;

namespace DocumentGenerator
{
    internal class Range
    {
        public DocumentRange Value { get; protected set; }

        public Range(DocumentRange value) {
            Value = value;
        }
    }
}
