using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        protected List<BindingKey> bindingKeys;
        protected Dictionary<string, BindingField> fieldsIndex;
        protected Dictionary<string, BindingTable> tablesIndex;
        protected Dictionary<string, BindingInclude> includesIndex;
        protected List<DataSet> dataSets = null;

        protected DXDocuments.Manager manager = null;

        public Processor(string _templatePath, string _fieldsPath, string _includesPath, string _savePath, List<DataSet> _data = null)
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
            bindingKeys = new List<BindingKey>();

            dataSets = _data;
        }

        public void start()
        {
            if (!File.Exists(templatePath)) {
                throw new Exception("Δεν βρέθηκε το αρχείο που περιέχει το κεντρικό τμήμα (main) του document");
            }

            //1. Φορτώνουμε τα Json από {{κάποιο path (fields.txt, parts.txt)}} και μαζεύουμε τα {aliases}
            readFields();
            readIncludes();

            process(templatePath);
        }

        private void process(string fileName, BindingKey key = null) {
            DXDocuments.Manager manager = null;
            try
            {
                //2. Δημιουργούμε ένα manager
                //2.1 Φορτώνουμε το master template 
                manager = new DXDocuments.Manager();
                manager.Open(fileName);

                //2.2 Καταγράφουμε τα {aliases} που βρίσκονται μέσα στο έντυπο
                List<Token> tokens = manager.getAliases();

                //2.2 Καταγράφουμε τα <comments> των πινάκων που βρίσκονται μέσα στο έντυπο
                List<Comment> comments = manager.getComments();

                //3. Ξεκινούμε την επεξεργασία του document(master template)

                //3.0 Προσθέτουμε το τρέχον ID στο context stack (μαζί με τον πίνακα) *


            } finally
            {
                if (manager != null) {
                    manager.Close();
                    manager.Save(savePath);
                    manager.Dispose();
                }
            }

            //3.1 Για κάθε alias:
            //3.1.1 Αν είναι field αντικαθιστούμε το κείμενο
            //3.1.2 Αν είναι include:
            //3.1.2.1 Φορτώνουμε το template:
            //3.1.2.2 Για όλα τα rows του πίνακα του include:
            //3.1.2.2.1 Κάνουμε κλώνο του template document
            //3.1.2.2.2 Καλούμε το βήμα 3 ->
            //3.1.2.2.3 Το παραγόμενο document το εισάγουμε στο τρέχον document

            //3.1.3 Για κάθε πίνακα:
            //3.1.3.1 Καταγράφουμε τις παραμέτρους του comment
            //3.1.3.2 Αφαιρούμε το comment
            //3.1.3.3 Παράγουμε τον πίνακα στη μνήμη
            //3.1.3.4 Διαγράφουμε τον αρχικό πίνακα και εισάγουμε τον παραχθέντα

            //3.2 Αφαιρούμε το ID από το context stack *
        }

        private void process(TableRow row, BindingKey key) {

        }

        private void readDocument(RichEditDocumentServer WordProcessor) {
           
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
                        DataTable dataTable = getDataTable(tableName);

                        if (dataTable == null)
                            throw new Exception($"Table '{tableName}' doesn't exist");

                        // Key column
                        DataColumn keyColumn = null;
                        // TODO: keyColumn = dataTable.PrimaryKey[0];

                        List<BindingField> bindingFields = new List<BindingField>();
                        BindingTable bindingTable = new BindingTable(tableName, tableAlias, dataTable, keyColumn, bindingFields);
                        bindingTables.Add(bindingTable);
                        tablesIndex.Add(tableAlias, bindingTable);
                        tablesIndex.Add(tableName, bindingTable);

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

                                // Field column
                                DataColumn fieldColumn = null;
                                // TODO: Get DataColumn from datasets

                                BindingField bindingField = new BindingField(bindingTable, fieldName, fieldAlias, fieldColumn, fieldFormat,fieldFormatNull);
                                bindingFields.Add(bindingField);

                                if (!fieldsIndex.ContainsKey(bindingField.Alias))
                                    fieldsIndex.Add(bindingField.Alias, bindingField);
                                else
                                    throw new Exception($"Το alias '{bindingField.Alias}' υπάρχει ήδη ως κλειδί");
                                
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

        protected DataTable getDataTable(string tableName) {
            
            foreach (DataSet dataSet in dataSets) {
                DataTable table = dataSet.Tables[tableName];
                if (table != null) return table;               
            }

            return null;
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
