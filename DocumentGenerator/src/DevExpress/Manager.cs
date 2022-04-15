using System;
using System.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using DevExpress.XtraRichEdit;
using dxDocument = DevExpress.XtraRichEdit.API.Native.Document;
using dxElement = DevExpress.XtraRichEdit.API.Native.DocumentElementBase;
using dxRange = DevExpress.XtraRichEdit.API.Native.DocumentRange;
using dxPosition = DevExpress.XtraRichEdit.API.Native.DocumentPosition;
using dxComment = DevExpress.XtraRichEdit.API.Native.Comment;
using dxTable = DevExpress.XtraRichEdit.API.Native.Table;
using dxTableRow = DevExpress.XtraRichEdit.API.Native.TableRow;
using dxTableCell = DevExpress.XtraRichEdit.API.Native.TableCell;
using dxSubDocument = DevExpress.XtraRichEdit.API.Native.SubDocument;
using System.IO;

namespace DocumentGenerator.DXDocuments
{
    internal class Manager
    {        
        private RichEditDocumentServer _wordProcessor = null;
        private List<Table> _tables;

        public Manager()
        {
            _tables = new List<Table>();
        }

        public void Open(string fileName) {
            try {
                _wordProcessor = new RichEditDocumentServer();
                _wordProcessor.Document.BeginUpdate();
                _wordProcessor.LoadDocument(fileName);
                
                _tables.Clear();

                foreach (dxTable table in _wordProcessor.Document.Tables) {

                    Table _table = new Table(table, new Range(table.Range));
                    InitializeTable(_table);
                    _tables.Add(_table);
                }
            }
            catch(Exception ex)
            {
                if (_wordProcessor != null) {
                    if (_wordProcessor.Document != null) {
                        _wordProcessor.EndUpdate();
                    }
                    _wordProcessor.Dispose();
                    _wordProcessor = null;
                }

                Console.WriteLine(ex.Message);
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

        public static void ShowFile(string fileName) {
            Process.Start(new ProcessStartInfo(fileName) { UseShellExecute = true });
        }

        /// <summary>
        /// Gets all the aliases from the document. 
        /// </summary>
        /// <param name="search">If this parameter is passed, then the function searches for a particular text</param>
        /// <param name="range">If this parameter is passed then the function searches in a particular range in the document</param>
        /// <returns>a dictionary object with the string that was found and the range in the document</returns>
        public List<Token> GetAliases(Range range = null, string search = "") {
            Regex r;
            dxRange[] result;
            List<Token> fields = new List<Token>(); 

            if (search != "") {
                r = new Regex("{" + search + "[^}]*}");
            } else {
                r = new Regex("{[^}]*}");
            }

            dxDocument document = _wordProcessor.Document;
            dxRange searchRange;
            if (range == null)
                searchRange = document.Range;
            else
                searchRange = range.Value;

            result = document.FindAll(r, searchRange);
            for (int i = 0; i < result.Length; i++) {
                dxRange aliasRange = result[i]; // ΕΙΝΑΙ ΛΑΘΟΣ: document.CreateRange(result[i].Start.ToInt(), result[i].End.ToInt());
                string text = document.GetText(aliasRange).Trim();
                if (text != string.Empty && !text.StartsWith("{!")) {
                    text = text.Replace("{", "");
                    text = text.Replace("}", "");
                    fields.Add(new Token(text, text, new Range(aliasRange), null));
                }
            }

            return fields;
        }

        /// <summary>
        /// Gets all the aliases from the document. 
        /// </summary>
        /// <param name="search">If this parameter is passed, then the function searches for a particular text</param>
        /// <param name="range">If this parameter is passed then the function searches in a particular range in the document</param>
        /// <returns>a dictionary object with the string that was found and the range in the document</returns>
        public void InitializeTable(Table table, string search = "") {
            Regex r;
            dxRange[] result;
            List<Token> fields = new List<Token>();

            if (search != "") {
                r = new Regex("{!" + search + "[^}]*}");
            } else {
                r = new Regex("{![^}]*}");
            }

            dxDocument document = _wordProcessor.Document;

            if (table != null) {
                int minRow = table.Element.Rows.Count;
                int maxRow = 0;

                for (int indexRow = 0; indexRow < table.Element.Rows.Count; indexRow++) {
                    dxTableRow dxRow = table.Element.Rows[indexRow];

                    result = document.FindAll(r, dxRow.Range);

                    if (result.Length > 0) {
                        TableRow row = new TableRow(table, dxRow, new Range(dxRow.Range), indexRow);
                        minRow = minRow > indexRow ? indexRow : minRow;
                        maxRow = maxRow < indexRow ? indexRow + 1 : maxRow;

                        for (int i = 0; i < result.Length; i++) {
                            dxRange aliasRange = result[i];
                            string text = document.GetText(aliasRange);
                            text = text.Trim();
                            text = text.Replace("{", "");
                            text = text.Replace("}", "");
                            if (text != string.Empty) {
                                if (text.StartsWith("!")) {
                                    fields.Add(new Token(text.Replace("!", ""), text, new Range(aliasRange), row));
                                } else {
                                    fields.Add(new Token(text.Replace("!", ""), text, new Range(aliasRange), null));
                                }
                            }
                        }
                    }
                }

                table.Tokens = fields;
                table.HeaderCount = minRow;
                table.BodyCount = maxRow - minRow;
                table.FooterCount = table.Element.Rows.Count - maxRow;
            }
            //else {
            //    for (int indexRow = 0; indexRow < table.Rows.Count; indexRow++) {
            //        dxTableRow dxRow = table.Rows[indexRow];
            //        result = document.FindAll(r, row.Range);

            //        result = _wordProcessor.Document.FindAll(r);
            //        for (int i = 0; i < result.Length; i++) {
            //            dxRange aliasRange = result[i];
            //            string text = document.GetText(aliasRange);
            //            text = text.Trim();
            //            text = text.Replace("{", "");
            //            text = text.Replace("}", "");
            //            if (text != string.Empty) {
            //                if (text.StartsWith("!")) {
            //                    fields.Add(new Token(text.Replace("!", ""), text, new Range(aliasRange), getTable(aliasRange)));
            //                } else {
            //                    fields.Add(new Token(text.Replace("!", ""), text, new Range(aliasRange), null));
            //                }
            //            }
            //        }
            //    }
            //}

            //return fields;
        }

        public List<Comment> GetComments() {
            List<Comment> comments = new List<Comment>();

            foreach (DevExpress.XtraRichEdit.API.Native.Comment comment in _wordProcessor.Document.Comments) {
                dxTableCell tableCell = _wordProcessor.Document.Tables.GetTableCell(comment.Range.Start);
                dxTable table = tableCell.Row.Table;

                //comments.Add(new Comment(comment, getTable(comment.Range), new Range(comment.Range), _wordProcessor.Document.GetText(comment.Range)));  //new Table(table, new Range(table.Range)), new Range(comment.Range)));
                comments.Add(new Comment(comment, getTable(comment.Range), new Range(comment.Range), getCommentText(comment)));  //new Table(table, new Range(table.Range)), new Range(comment.Range)));
            }

            // TODO: 1. Να φτιάξουμε μία array ταξινομημένη, με τα ranges των πινάκων, 2. Με δυαδική αναζήτηση να βρίσκουμε αν μία θέση ανήκει σε κάποιο από αυτά τα ranges

            return comments;
        }

        private Table getTable(dxRange range) {
            foreach (Table table in _tables) {
                if (table.Range.Value.Contains(range.Start)) {
                    return table;
                }
            }

            return null;
        }

        //private dxTable getTableByRange(dxRange range) {
        //    dxTableCell tableCell = _wordProcessor.Document.Tables.GetTableCell(range.Start);
        //    return tableCell.Row.Table;
        //}

        public void ReplaceRangeWithText(Range range, string targetText) {
            //this.mainWordProcessor.Document.BeginUpdate();
            if (range != null)
                _wordProcessor.Document.Replace(range.Value, targetText);
        }

        public void ReplaceRangeWithText(dxRange range, string targetText) {
            //this.mainWordProcessor.Document.BeginUpdate();
            if (range != null)
                _wordProcessor.Document.Replace(range, targetText);
        }

        public void ReplaceRangeWithContent(Manager manager, Range range /*, string file, string id, string foreightKey = "", RichEditDocumentServer wp = null*/) {
            // Delete alias range
            dxPosition start = range.Value.Start;
            _wordProcessor.Document.Delete(range.Value);

            // Insert the content
            dxDocument document = getManagerDocument(manager);
            _wordProcessor.Document.InsertDocumentContent(start, document.Range, DevExpress.XtraRichEdit.API.Native.InsertOptions.KeepSourceFormatting);
        }

        public void Delete(Comment comment) {
            _wordProcessor.Document.Delete(comment.Element.Range);
        }

        /// <summary>
        /// Populates a template table with data
        /// </summary>
        /// <param name="comment">A comment object that links to the document table</param>
        /// <param name="bindingTable">The table that contains the data used for the table population</param>        
        public void PopulateTable(Comment comment, BindingTable bindingTable, List<BindingTable.Row> contextStack) { // JObject jo, Comment comment, string id = "", RichEditDocumentServer wp = null) {

            Table table = comment.Table;
            DataTable dataTable = bindingTable.DataTable;

            // Delete comment
            _wordProcessor.Document.Delete(comment.Range.Value);

            // Insert <newline> after the table to create space for the new table                       
            _wordProcessor.Document.InsertText(table.Element.Range.End, DevExpress.Office.Characters.LineBreak.ToString());
            var newTableRange = _wordProcessor.Document.InsertText(table.Element.Range.End, "{{newTable}}");

            // Copy header
            var headerRange = getRowsRange(table.Element, 0, table.HeaderCount);
            _wordProcessor.Document.InsertDocumentContent(newTableRange.End, headerRange, DevExpress.XtraRichEdit.API.Native.InsertOptions.KeepSourceFormatting);

            // Copy body <n> times
            var bodyRange = getRowsRange(table.Element, table.HeaderCount, table.BodyCount);

            BindingTable.Row row;
            BindingTable.Enumerator enumerator = new BindingTable.Enumerator(bindingTable);
            dxRange lastRange = newTableRange;

            enumerator.Start();
            while ((row = enumerator.Next()) != null) { // TODO: use index for performance
                if (row.InContext(contextStack)) {
                    lastRange = _wordProcessor.Document.InsertDocumentContent(lastRange.End, bodyRange, DevExpress.XtraRichEdit.API.Native.InsertOptions.KeepSourceFormatting);

                    foreach (Token token in table.Tokens) {
                        //3.1.1 Αν είναι field:
                        BindingField field = bindingTable.BindingFields.Find((BindingField f) => token.Alias == f.Alias || token.Alias == f.FullName);

                        //3.1.1.1 Ελέγχουμε αν το alias είναι έγκυρο
                        if (field != null) {
                            //3.1.1.2 Βρίσκουμε την τιμή του field
                            string text = row.GetValue(field, contextStack);

                            //3.1.1.3 Αντικαθιστούμε το alias με το κείμενο
                            replaceTextInRange($"{{{token.Original}}}", text, lastRange);
                        } else {
                            // TODO: Log
                        }
                    }
                }
            }

            _wordProcessor.Document.ReplaceAll("{{newTable}}", " ", DevExpress.XtraRichEdit.API.Native.SearchOptions.None, _wordProcessor.Document.Range);
            _wordProcessor.Document.Delete(comment.Table.Element.Range);
        }

        /// <summary>
        /// Gets the row range
        /// </summary>
        /// <param name="table">the table object</param>
        /// <param name="rowIndex">the rowIndex that we will get the range</param>
        /// <param name="rowCount">how many rows</param>
        /// <returns></returns>
        protected dxRange getRowsRange(dxTable table, int rowIndex, int rowCount) {
            dxPosition start = table.Rows[rowIndex].Range.Start;
            dxPosition end = table.Rows[rowIndex + rowCount - 1].Range.End;
            int length = end.ToInt() - start.ToInt();

            return _wordProcessor.Document.CreateRange(start, length);
        }

        protected static dxDocument getManagerDocument(Manager manager) {
            return manager._wordProcessor.Document;
        }

        protected string getCommentText(dxComment comment) {
            dxSubDocument doc = comment.BeginUpdate();
            string commentText = doc.GetText(doc.Range);
            comment.EndUpdate(doc);

            return commentText;
        }

        protected void replaceTextInRange(string sourceText, string targetText, dxRange range) {
            //this.mainWordProcessor.Document.BeginUpdate();
            dxRange[] targetRanges = this.getTextRanges(sourceText, range);
            foreach(var targetRange in targetRanges) {
                _wordProcessor.Document.Replace(targetRange, targetText);
            }
        }

        protected dxRange[] getTextRanges(string search, dxRange range) {
            return _wordProcessor.Document.FindAll(new Regex(search), range);
        }

        public void ReplacePageBreakToken(bool ignore) {
            Regex r = new Regex("{PBR}");
            if (!ignore)
                _wordProcessor.Document.ReplaceAll(r, DevExpress.Office.Characters.PageBreak.ToString());
            else
                _wordProcessor.Document.ReplaceAll(r, string.Empty);
        }
    }
}
