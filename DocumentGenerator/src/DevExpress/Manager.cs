using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using DevExpress.XtraRichEdit;
using dxTable = DevExpress.XtraRichEdit.API.Native.Table;
using dxTableRow = DevExpress.XtraRichEdit.API.Native.TableRow;
using dxTableCell = DevExpress.XtraRichEdit.API.Native.TableCell;
using dxRange = DevExpress.XtraRichEdit.API.Native.DocumentRange;

namespace DocumentGenerator.DXDocuments
{
    class Manager
    {
        private RichEditDocumentServer _wordProcessor = null;

        public Manager()
        {
        }

        public void Open(string fileName) {
            try {
                _wordProcessor = new RichEditDocumentServer();

                _wordProcessor.Document.BeginUpdate();
                _wordProcessor.LoadDocument(fileName);
            }
            finally
            {
                if (_wordProcessor != null) {
                    if (_wordProcessor.Document != null) {
                        _wordProcessor.EndUpdate();
                    }
                    _wordProcessor.Dispose();
                    _wordProcessor = null;
                }
            }
        }

        public void Close() {
            if (_wordProcessor != null) {
                if (_wordProcessor.Document != null) {
                    _wordProcessor.EndUpdate();
                }
                _wordProcessor.Dispose();
            }
        }

        public void Dispose() {
            if (_wordProcessor != null) {
                _wordProcessor.Dispose();
            }
        }

        public void Save(string fileName) {
            //Console.WriteLine("Saving file report");
            _wordProcessor.SaveDocument(fileName, DocumentFormat.OpenXml);
            //Console.WriteLine("Report save in :" + TemplatePathGenerated);
        }

        public void ShowFile(string fileName) {
            Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
        }

        /// <summary>
        /// Gets all the aliases from the document. 
        /// </summary>
        /// <param name="search">If this parameter is passed, then the function searches for a particular text</param>
        /// <param name="range">If this parameter is passed then the function searches in a particular range in the document</param>
        /// <returns>a dictionary object with the string that was found and the range in the document</returns>
        public List<Token> getAliases(string search = "", Range range = null) {
            Regex r;
            dxRange[] result = null;
            List<Token> fields = new List<Token>(); 

            if (search != "") {
                r = new Regex("{" + search + ".*?}");
            } else {
                r = new Regex("{");
            }

            if (range != null) {
                result = _wordProcessor.Document.FindAll(r, range.Value);                
                for (int i = 0; i < result.Length; i++) {
                    if (range.Value.Contains(result[i].Start) && range.Value.Contains(result[i].End)) {
                        dxRange aliasRange = _wordProcessor.Document.CreateRange(result[i].Start.ToInt(), result[i].End.ToInt());
                        fields.Add(new Token(result[i].ToString(), new Range(aliasRange)));
                    }
                }
            } else {
                result = _wordProcessor.Document.FindAll(r);
                for (int i = 0; i < result.Length; i++) {
                        dxRange aliasRange = _wordProcessor.Document.CreateRange(result[i].Start.ToInt(), result[i].End.ToInt());
                        fields.Add(new Token(result[i].ToString(), new Range(aliasRange)));
                }
            }

            return fields;
        }

        public List<Comment> getComments() {
            List<Comment> comments = new List<Comment>();

            foreach (DevExpress.XtraRichEdit.API.Native.Comment comment in _wordProcessor.Document.Comments) {
                dxTable table = getTableByRange(comment.Range);
                comments.Add(new Comment(comment, new Table(table, new Range(table.Range)), new Range(comment.Range)));
            }

            return comments;
        }

        private dxTable getTableByRange(dxRange range) {
            dxTableCell tableCell = _wordProcessor.Document.Tables.GetTableCell(range.Start);
            return tableCell.Row.Table;
        }
    }
}
