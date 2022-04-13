using DevExpress.XtraRichEdit.API.Native;
using System;
using System.Data;

namespace DocumentGenerator
{
    internal class Token
    {
        public string Alias { get; protected set; }
        public Range Range { get; protected set; }

        public Token(string alias, Range range) {
            Alias = alias;
            Range = range;
        }
    }
}
