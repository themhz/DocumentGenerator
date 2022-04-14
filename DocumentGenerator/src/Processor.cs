using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using DevExpress.XtraRichEdit;
//using DevExpress.XtraRichEdit.API.Native;
using Newtonsoft.Json.Linq;

namespace DocumentGenerator
{
    class Processor
    {
        protected string templatePath;
        protected string savePath;
        protected string fieldsPath;
        protected string includesPath;

        protected List<BindingTable> bindingTables;
        protected List<BindingInclude> bindingIncludes;
        protected List<BindingTable.Row> bindingStack;
        protected Dictionary<string, BindingField> fieldsIndex;
        protected Dictionary<string, BindingTable> tablesIndex;
        protected Dictionary<string, BindingInclude> includesIndex;
        protected IDataSource dataSource = null;

        protected DXDocuments.Manager manager = null;

        public Processor(string _templatePath, string _fieldsPath, string _includesPath, string _savePath, IDataSource _dataSource = null)
        {
            templatePath = _templatePath;
            savePath = _savePath;
            fieldsPath = _fieldsPath;
            includesPath = _includesPath;

            fieldsIndex = new Dictionary<string, BindingField>();
            tablesIndex = new Dictionary<string, BindingTable>();
            includesIndex = new Dictionary<string, BindingInclude>();

            bindingTables = new List<BindingTable>();
            bindingIncludes = new List<BindingInclude>();
            bindingStack = new List<BindingTable.Row>();

            dataSource = _dataSource;
        }

        public void start()
        {
            if (!File.Exists(templatePath)) {
                throw new Exception("Δεν βρέθηκε το αρχείο που περιέχει το κεντρικό τμήμα (main) του document");
            }

            //1. Φορτώνουμε τα Json από {{κάποιο path (fields.txt, parts.txt)}} και μαζεύουμε τα {aliases}
            readFields();
            readIncludes();

            bindingStack.Clear();
            DXDocuments.Manager manager = process(templatePath);
            if (manager != null) {
                manager.Save(savePath);

                manager.Close();
                manager.Save(savePath);
                manager.Dispose();

                DXDocuments.Manager.ShowFile(savePath);
            }
        }

        private DXDocuments.Manager process(string fileName, BindingTable.Row key = null) {
            DXDocuments.Manager manager = null;
            List<Token> tokens;
            List<Comment> comments;

            try
            {
                //2. Δημιουργούμε ένα manager
                //2.1 Φορτώνουμε το master template 
                manager = new DXDocuments.Manager();
                manager.Open(fileName);

                //2.2 Καταγράφουμε τα {aliases} που βρίσκονται μέσα στο έντυπο
                tokens = manager.GetAliases();

                //2.2 Καταγράφουμε τα <comments> των πινάκων που βρίσκονται μέσα στο έντυπο
                comments = manager.GetComments();

                //3. Ξεκινούμε την επεξεργασία του document(master template)

                //3.0 Προσθέτουμε το τρέχον ID στο context stack (μαζί με τον πίνακα) *
                if (key != null) bindingStack.Add(key);

                //3.1 Για κάθε alias:
                foreach (Token token in tokens) {
                    if (!token.Alias.StartsWith("include:")) {
                        //3.1.1 Αν είναι field:
                        BindingField field;

                        //3.1.1.1 Ελέγχουμε αν το alias είναι έγκυρο
                        if (fieldsIndex.TryGetValue(token.Alias, out field)) {
                            //3.1.1.2 Αντικαθιστούμε το alias με το κείμενο
                            manager.ReplaceRangeWithText(token.Range, dataSource.GetValue(field));
                        }
                    }
                    else
                    {
                        if (!token.Alias.StartsWith("!")) {
                            //3.1.2 Αν είναι include:
                            BindingInclude include;
                            if (includesIndex.TryGetValue(token.Alias, out include)) {
                                // 3.1.2.1 Για κάθε row του πίνακα ή για την πρώτη row:
                                BindingTable bindingTable = include.Table;

                                if (include.Table != null) {
                                    BindingTable.Row row;
                                    bindingTable.Start();
                                    while ((row = bindingTable.Next()) != null) {
                                        //3.1.2.1.1 Καλούμε την process() ->
                                        DXDocuments.Manager subManager = process(include.File, row);

                                        //3.1.2.1.2 Το παραγόμενο document το εισάγουμε στο τρέχον document
                                        manager.ReplaceRangeWithContent(subManager, token.Range);
                                    }
                                } else {
                                    //3.1.2.1.1 Καλούμε την process() ->
                                    DXDocuments.Manager subManager = process(include.File, null);

                                    if (subManager != null) {
                                        //3.1.2.1.2 Το παραγόμενο document το εισάγουμε στο τρέχον document
                                        manager.ReplaceRangeWithContent(subManager, token.Range);

                                        subManager.Close();
                                        subManager.Save(savePath);
                                        subManager.Dispose();
                                    }
                                }
                            } else {
                                //TODO: Log
                            }
                        }
                    }
                }

                //3.1.3 Για κάθε πίνακα:
                foreach (Comment comment in comments) {
                    //3.1.3.1 Καταγράφουμε τις παραμέτρους του comment
                    string tableName = comment.Text.Replace("Table:", "");

                    //3.1.3.2 Βρίσκουμε τον πίνακα που έχει τα δεδομένα
                    BindingTable table;
                    if (tablesIndex.TryGetValue(tableName, out table)) {
                        //3.1.3.3 Παράγουμε τον πίνακα
                        manager.PopulateTable(comment, table);
                    } else {
                        // TODO: Log
                    }
                }

            } finally
            {
                //3.2 Αφαιρούμε το ID από το context stack *
                if (key != null) bindingStack.RemoveAt(bindingStack.Count);
            }

            return manager;
        }

        protected void readFields()
        {
            string content = openFile(fieldsPath);

            if (content == null)
            {
                throw new Exception("Initialize fields: File is missing");
            }

            JObject json = this.jsonParse(content);

            if (json != null)
            {
                var jsonTables = json.GetValue("Tables");
                if (jsonTables != null)
                {
                    foreach (JObject jsonTable in jsonTables)
                    {
                        // Name
                        string tableName = (string)jsonTable["table"];

                        
                        if (tableName == null || tableName == string.Empty)
                        {
                            throw new Exception("Initialize fields: A table has no name or is empty");
                        }

                        // Alias
                        string tableAlias = (string)jsonTable["alias"];

                        // DataTable
                        //DataTable dataTable = getDataTable(tableName);

                        // Key column
                        //DataColumn keyColumn = null;
                        // TODO: keyColumn = dataTable.PrimaryKey[0];

                        //List<BindingField> bindingFields = new List<BindingField>();
                        //bindingTables.Add(bindingTable);
                        //new BindingTable(tableName, tableAlias, dataTable, keyColumn, bindingFields);

                        BindingTable bindingTable = dataSource.GetTable(tableName, tableAlias);
                        if (bindingTable == null) {
                            throw new Exception($"Table '{tableName}' doesn't exist");
                        }
                        else {
                            bindingTables.Add(bindingTable);
                            tablesIndex.Add(tableAlias, bindingTable);
                            tablesIndex.Add(tableName, bindingTable);
                        }

                        // Fields
                        var jsonFields = jsonTable["Fields"];

                        if (jsonFields != null && jsonFields.Count() > 0)
                        {
                            foreach (var jsonField in jsonFields)
                            {
                                // Name
                                string fieldName = jsonField["name"].Value<string>();

                                if (fieldName == null || fieldName == string.Empty)
                                {
                                    throw new Exception("Initialize fields: A table has no name or is empty");
                                }

                                // Alias
                                string fieldAlias = (string)jsonField["alias"];

                                // Format
                                string fieldFormat = (string)jsonField["format"];

                                // FormatNull
                                string fieldFormatNull = (string)jsonField["formatNull"];

                                BindingField bindingField;

                                if (!fieldsIndex.ContainsKey(fieldAlias)) {
                                    bindingField = dataSource.GetField(bindingTable, fieldName, fieldAlias, fieldFormat ?? string.Empty, fieldFormatNull ?? string.Empty);
                                    if (bindingField == null)
                                        throw new Exception($"Το πεδίο '{bindingTable.Name}.{fieldName}' δεν βρέθηκε στους πίνακες της εφαρμογής");
                                    else
                                        fieldsIndex.Add(bindingField.Alias, bindingField);
                                }
                                else {
                                    throw new Exception($"Το alias '{fieldAlias}' υπάρχει ήδη ως κλειδί");
                                }

                                // Field column
                                //DataColumn fieldColumn = null;
                                // TODO: Get DataColumn from datasets

                                //BindingField bindingField = new BindingField(bindingTable, fieldName, fieldAlias, fieldColumn, fieldFormat,fieldFormatNull);
                                //bindingFields.Add(bindingField);
                            }
                        }
                        else
                        {
                            throw new Exception(String.Format("Initialize fields: Fields are missing from table {0}", tableName));
                        }
                    }
                }
                else
                {
                    throw new Exception("Initialize fields: 'Tables' list is missing");
                }
            }
            else
            {
                throw new Exception("Initialize fields: File has not valid 'json' format");
            }
        }

        protected void readIncludes() {
            string content = openFile(includesPath);

            if (content == null) {
                throw new Exception("Initialize includes: File is missing");
            }

            JObject json = this.jsonParse(content);

            if (json != null) {
                var jsonIncludes = json.GetValue("Includes");
                if (jsonIncludes != null) {
                    foreach (JObject jsonInclude in jsonIncludes) {
                        // Alias
                        string includeAlias = (string)jsonInclude["alias"];

                        if (includeAlias == null || includeAlias == string.Empty) {
                            throw new Exception("Initialize includes: an alias is missing or is empty");
                        }

                        // File
                        string includeFile = (string)jsonInclude["file"];
                        string includeDir = Path.GetDirectoryName(templatePath);
                        string includePath = Path.Combine(includeDir, includeFile);
                        if (!File.Exists(includePath))
                            throw new Exception($"file '{includePath}' doesn't exist");

                        // Table
                        string includeTable = (string)jsonInclude["table"];

                        BindingTable bindingTable;
                        if (!tablesIndex.TryGetValue(includeTable, out bindingTable)) {
                            throw new Exception($"Initialize includes: for include '{includeAlias}' the table is missing or is empty");
                        }

                        BindingInclude bindingInclude = new BindingInclude(includeAlias, includeFile, bindingTable);
                        bindingIncludes.Add(bindingInclude);
                        includesIndex.Add(includeAlias, bindingInclude);
                    }
                } else {
                    throw new Exception("Initialize fields: 'Tables' list is missing");
                }
            } else {
                throw new Exception("Initialize fields: File has not valid 'json' format");
            }
        }

        protected string openFile(string path)
        {
            if (!File.Exists(path)) 
            {
                return null;
            }

            return File.ReadAllText(path);
        }

        protected JObject jsonParse(string text)
        {
            try 
            {
                return JObject.Parse(text);
            }
            catch(Exception) 
            {
                return null;
            }            
        }
        
    }
}
