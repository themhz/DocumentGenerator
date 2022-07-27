using System;
using System.Collections.Generic;
using System.Collections;
using System.Data;
using System.IO;
using System.Linq;
using DevExpress.XtraRichEdit;
using DocumentGenerator.DXDocuments;
using Newtonsoft.Json.Linq;

namespace DocumentGenerator
{
    public class Processor
    {
        protected string templatePath;
        protected string savePath;
        protected string fieldsPath;
        protected string groupsPath;
        protected string includesPath;

        protected List<BindingTable> bindingTables;
        protected List<BindingInclude> bindingIncludes;
        protected List<BindingTable.Row> bindingStack;
        protected Dictionary<string, BindingField> fieldsIndex;
        protected Dictionary<string, BindingTable> tablesIndex;
        protected Dictionary<string, BindingInclude> includesIndex;
        protected IDataSource dataSource = null;

        public Processor(string _templatePath, string _fieldsPath, string _groupsPath, string _includesPath, string _savePath, IDataSource _dataSource = null)
        {
            templatePath = _templatePath;
            savePath = _savePath;
            fieldsPath = _fieldsPath;
            groupsPath = _groupsPath;
            includesPath = _includesPath;

            fieldsIndex = new Dictionary<string, BindingField>();
            tablesIndex = new Dictionary<string, BindingTable>();
            includesIndex = new Dictionary<string, BindingInclude>();

            bindingTables = new List<BindingTable>();
            bindingIncludes = new List<BindingInclude>();
            bindingStack = new List<BindingTable.Row>();
           
            dataSource = _dataSource;
        }

        #region "   PROCESS   "

        /// <summary>
        /// Ενεργοποίηση του manager κατασκευής εγγράφου. 
        /// </summary>
        public void start()
        {
            // 1. Φορτώνουμε τα Json από {{κάποιο path (fields.txt, parts.txt)}} και μαζεύουμε τα {aliases}

            // Ελέγχουμε αν υπάρχουν τα αρχεία
            checkFilePaths();

            // Προετοιμασία των δεδομένων
            onPrepareData();

            // Διαβάζουμε τα πεδία από τα json
            readFields();

            // Διαβάζουμε τα groups
            readGroups();

            // Διαβάζουμε τα includes
            readIncludes();

            // Έναρξη Manager
            startManager();
        }

        protected virtual void onPrepareData()
        {
        }

        protected virtual bool onFilterRow(BindingTable.Row row) {

            return true;
        }

        //protected virtual void onSetStyle(Range range) {
        //}

        /// <summary>
        /// Ελέγχει αν υπάρχουν τα βασικά αρχεία includes και fields
        /// </summary>
        public void checkFilePaths()
        {            
            if (!File.Exists(templatePath))
            {                
                Log.Error("Δεν βρέθηκε το αρχείο που περιέχει το κεντρικό τμήμα (main) του document");
                throw new Exception("Δεν βρέθηκε το αρχείο που περιέχει το κεντρικό τμήμα (main) του document");
            }

            if (!File.Exists(fieldsPath))
            {
                Log.Error("Δεν βρέθηκε το αρχείο που περιέχει τα fields του (main) document");
                throw new Exception("Δεν βρέθηκε το αρχείο που περιέχει τα fields του (main) document");
            }
        }

        /// <summary>
        /// Έναρξη του manager που θα φτιάξει το αρχείο. 
        /// </summary>
        public void startManager()
        {
            bindingStack.Clear();
            Manager manager = process(templatePath);
            if (manager != null)
            {
                string path = manager.SaveTemp();
                manager.Close();
                manager.Dispose();

                Manager.ShowFile(path);
            }
        }

        private Manager process(string fileName, BindingTable.Row key = null)
        {
            Manager manager = null;
            List<Token> tokens;
            List<Comment> comments;

            try
            {
                //2. Δημιουργούμε ένα manager
                //2.1 Φορτώνουμε το master template 
                manager = new Manager();
                manager.Open(fileName);

                //2.2 Καταγράφουμε τα {aliases} που βρίσκονται μέσα στο έντυπο
                tokens = manager.GetAliases();

                //2.2 Καταγράφουμε τα <comments> των πινάκων που βρίσκονται μέσα στο έντυπο
                comments = manager.GetComments();

                //3. Ξεκινούμε την επεξεργασία του document(master template)

                //3.0 Προσθέτουμε το τρέχον ID στο context stack (μαζί με τον πίνακα) *
                if (key != null) bindingStack.Add(key);

                ParseTokens(fileName, manager, tokens);

                //3.1.3 Για κάθε πίνακα:
                ParseComments(manager, comments);

            }
            finally
            {
                //3.2 Αφαιρούμε το ID από το context stack *
                if (key != null) bindingStack.RemoveAt(bindingStack.Count - 1);
            }

            return manager;
        }

        private void ParseComments(Manager manager, List<Comment> comments)
        {
            foreach (Comment comment in comments)
            {
                //3.1.3.1 Καταγράφουμε τις παραμέτρους του comment                
                JObject tableName = ConvertCommentToJson(comment.Text);

                //3.1.3.2 Βρίσκουμε τον πίνακα που έχει τα δεδομένα                    
                if (tablesIndex.TryGetValue(tableName.GetValue("Table").ToString(), out BindingTable table))
                {
                    manager.PopulateTable(comment, table, bindingStack, tableName);                    
                }
                else
                {                    
                    Log.Warning("Τα σχόλια δεν περιέχουν δεδομένα για πίνακα ", comment, table, bindingStack);
                }
            }
        }

        private JObject ConvertCommentToJson(String text)
        {
            text = text.Replace("\r", "").Replace("\n", "");
            string json = "";
            string[] parts = text.Split(',');
            for (int i=0; i < parts.Length; i++){
                string part = parts[i].Trim();
                string[] keyValue = part.Split(':');
                if(i==0)
                    json += "\""+keyValue[0]+"\"" + ":\""+keyValue[1] + "\"";
                else
                    json += ",\"" + keyValue[0] + "\"" + ":\"" + keyValue[1] + "\"";
            }
            json = "{" + json + "}";

            JObject jsonObject = JObject.Parse(json);            
            return jsonObject;        
        }

        private void ParseTokens(string fileName, Manager manager, List<Token> tokens)
        {
            //3.1 Για κάθε token (που λέγετε και alias):
            foreach (Token token in tokens)
            {
                //Αν δεν αρχίζει με θαυμαστικό, γιατί αν αρχίζει τότε θα είναι τοκεν για πεδίο πίνακα που θα πρέπει να επαναλαμβάνεται
                if (!token.Alias.StartsWith("!"))
                {
                    //3.1.2 Αν δεν είναι include:
                    if (!token.Alias.StartsWith("include:"))
                    {
                        BindingField field;
                        //3.1.1 Αν είναι field:
                        //3.1.1.1 Ελέγχουμε αν το alias είναι έγκυρο
                        if (fieldsIndex.TryGetValue(token.Alias, out field))
                        {
                            ReplaceToken(manager, token, field);
                        } 
                        else
                        {
                            if (token.Alias!= "PBR" && checkValidJson("{"+token.Alias+"}")) 
                            {
                                JObject JsonToken = JObject.Parse("{" + token.Alias + "}");
                                if (fieldsIndex.TryGetValue(JsonToken.GetValue("name").ToString(), out field)) 
                                {
                                    ReplaceToken(manager, token, field, JsonToken);
                                }
                            }
                            
                        }
                    }
                    else
                    {
                        //3.1.2 Αν είναι include το αρχείο τότε θα παρσάρει τα include:                            
                        int start = token.Alias.IndexOf("\"") + 1;
                        int end = token.Alias.IndexOf("\"", start);
                        string alias = token.Alias.Substring(start, end - start);

                        if (includesIndex.TryGetValue(alias, out BindingInclude include))
                        {
                            // 3.1.2.1 Για κάθε row του πίνακα ή για την πρώτη row:
                            BindingTable bindingTable = include.Table;

                            if (include.Table != null)
                            {
                                ReplaceTokenWithTable(fileName, manager, token, include, bindingTable);
                            }
                            else
                            {
                                //3.1.2.1.1 Καλούμε την process() ->
                                ReplaceTokenWithTemplate(fileName, manager, token, include);
                            }
                        }
                        else
                        {
                            //TODO: Log
                            Log.Warning("Τα σχόλια δεν περιέχουν δεδομένα για πίνακα ", alias);
                        }
                    }
                }
            }
        }

       
        private bool checkValidJson(string jsonString) {            
            try {
                var tmpObj = JObject.Parse(jsonString);
                
                return true;
            } 
            catch (FormatException fex) 
            {
                return false;
            }
            catch (Exception ex) //some other exception
            {
                return false;
            }
        }
        private void ReplaceTokenWithTemplate(string fileName, Manager manager, Token token, BindingInclude include)
        {
            Manager subManager = process(getFilePath(fileName, include.File), null);

            if (subManager != null)
            {
                //3.1.2.1.2 Το παραγόμενο document το εισάγουμε στο τρέχον document
                manager.ReplaceRangeWithContent(subManager, token.Range);

                subManager.Close();
                //subManager.Save(savePath);
                subManager.Dispose();
            }
        }

        private void ReplaceTokenWithTable(string fileName, Manager manager, Token token, BindingInclude include, BindingTable bindingTable)
        {
            BindingTable.Row row;
            BindingTable.Enumerator enumerator = new BindingTable.Enumerator(bindingTable);

            enumerator.Start();
            while ((row = enumerator.Next()) != null)
            { // TODO: use index for performance
                if (row.InContext(bindingStack))
                {
                    bool exclude = false;
                    if (include.FilterTable != null)
                    {
                        int count = include.FilterTable.GetContextCount(bindingStack, row);
                        exclude = count == 0;
                    }

                    if(!exclude && onFilterRow(row))
                    {
                        //3.1.2.1.1 Καλούμε την process() ->
                        Manager subManager = process(getFilePath(fileName, include.File), row);

                        //3.1.2.1.2 Το παραγόμενο document το εισάγουμε στο τρέχον document
                        manager.ReplaceRangeWithContent(subManager, token.Range);
                        manager.ReplacePageBreakToken(enumerator.Remaining == 0);
                    }
                }
            }
        }

        private void ReplaceToken(Manager manager, Token token, BindingField field, JObject joToken = null)
        {
            //3.1.1.2 Αντικαθιστούμε το alias με το κείμενο
            int index = 0; // TODO: Να διαβάζει το index από το .docx
            if (field.Type.Name == "Byte[]")
            {
                manager.ReplaceTextWithImage(token.Range.Value, dataSource.GetContextValue(field, index, bindingStack), joToken);
            }
            else
            {
                manager.ReplaceRangeWithText(token.Range, dataSource.GetContextValue(field, index, bindingStack));
            }
        }

        protected string getFilePath(string currentFile, string fileName)
        {
            string dir = Path.GetFullPath(Path.GetDirectoryName(currentFile));
            string path = Path.Combine(dir, fileName);
            if (!File.Exists(path))
            {
                Log.Error($"File '{path}' doesn't exist", $"currentFile '{currentFile}'", $"fileName '{fileName}' ");
                throw new Exception($"File '{path}' doesn't exist");
            }
            return path;
        }

        #endregion

        #region "   TABLES   "

        /// <summary>
        /// Reads all the files from the fields.txt
        /// </summary>
        protected void readFields()
        {
            // Check the fields files
            var jsonTables = checkFieldsFile();

            // For each table in the file
            foreach (JObject jsonTable in jsonTables)
            {
                ParseJsonTables(jsonTable);
            }

            dataSource.GetRelations(tablesIndex, fieldsIndex);
        }

        private void ParseJsonTables(JObject jsonTable)
        {
            string tableName = (string)jsonTable["table"];

            CheckTableName(tableName);

            // Get the alias from the fields.txt file
            string tableAlias = (string)jsonTable["alias"];

            // DataTable             
            BindingTable bindingTable = createBindingTable(tableName, tableAlias);

            // Fields
            ParseFields(jsonTable, tableName, bindingTable);
        }

        private static void CheckTableName(string tableName)
        {
            if (tableName == null || tableName == string.Empty)
            {
                Log.Error("Initialize fields: A table has no name or is empty");
                throw new Exception("Initialize fields: A table has no name or is empty");                
            }
        }

        private void ParseFields(JObject jsonTable, string tableName, BindingTable bindingTable)
        {
            var jsonFields = jsonTable["Fields"];

            if (jsonFields != null && jsonFields.Count() > 0)
            {
                foreach (var jsonField in jsonFields)
                {
                    // Name
                    string fieldName = jsonField["name"].Value<string>();

                    if (fieldName == null || fieldName == string.Empty)
                    {
                        Log.Error("Initialize fields: A table has no name or is empty");
                        throw new Exception("Initialize fields: A table has no name or is empty");
                    }

                    // Alias
                    string fieldAlias = (string)jsonField["alias"];

                    // Format
                    string fieldFormat = (string)jsonField["format"];

                    // FormatNull
                    string fieldFormatNull = (string)jsonField["formatNull"];

                    BindingField bindingField;

                    if (!fieldsIndex.ContainsKey(fieldAlias))
                    {
                        bindingField = dataSource.GetField(bindingTable, fieldName, fieldAlias, fieldFormat ?? string.Empty, fieldFormatNull ?? string.Empty);
                        if (bindingField == null)
                        {
                            Log.Error($"Το πεδίο '{bindingTable.Name}.{fieldName}' δεν βρέθηκε στους πίνακες της εφαρμογής");
                            //throw new Exception($"Το πεδίο '{bindingTable.Name}.{fieldName}' δεν βρέθηκε στους πίνακες της εφαρμογής");
                        }
                        else
                        {
                            fieldsIndex.Add(bindingField.Alias, bindingField);
                            fieldsIndex.Add($"{bindingTable.Name}.{fieldName}", bindingField);
                            bindingTable.Add(bindingField);
                        }
                    }
                    else
                    {
                        Log.Error($"Το alias '{fieldAlias}' υπάρχει ήδη ως κλειδί");
                        throw new Exception($"Το alias '{fieldAlias}' υπάρχει ήδη ως κλειδί");
                    }
                }
            }
            else
            {
                Log.Error(String.Format("Initialize fields: Fields are missing from table {0}", tableName));
                throw new Exception(String.Format("Initialize fields: Fields are missing from table {0}", tableName));
            }
        }

        private BindingTable createBindingTable(string tableName, string tableAlias)
        {
            BindingTable bindingTable = dataSource.GetTable(tableName, tableAlias);
            if (bindingTable == null)
            {
                Log.Error($"Table '{tableName}' doesn't exist");
                throw new Exception($"Table '{tableName}' doesn't exist");
            }
            else
            {
                bindingTables.Add(bindingTable);
                tablesIndex.Add(tableAlias, bindingTable);
                if (tableAlias != tableName)
                    tablesIndex.Add(tableName, bindingTable);
            }

            return bindingTable;
        }

        protected JToken checkFieldsFile()
        {
            // Ανοίγουμε το αρχείο των fields και το διαβάζουμε
            string content = openFile(fieldsPath);

            // Αν το περιεχώμενο δεν υπάρχει προφανός έχουμε πρόβλημα
            if (content == null)
            {
                Log.Error("Initialize fields: File is missing", fieldsPath);
                throw new Exception("Initialize fields: File is missing");
            }

            // Παρσάρουμε το αρχείο ως json
            JObject json = this.jsonParse(content);
            if (json == null)
            {
                Log.Error("Initialize fields: content is missing");
                throw new Exception("Initialize fields: content is missing");
            }

            // Διαβάζουμε τους πίνακες
            var jsonTables = json.GetValue("Tables");
            if (jsonTables == null)
            {
                Log.Error("Initialize fields: Tables list is missing", json);
                throw new Exception("Initialize fields: 'Tables' list is missing");
            }

            return jsonTables;
        }

        #endregion

        #region "   GROUPS   "

        /// <summary>
        /// Reads all the records from the groups.txt
        /// </summary>
        protected void readGroups()
        {
            // Check the fields files
            var jsonGroups = checkGroupsFile();

            // For each table in the file
            foreach (JObject jsonGroup in jsonGroups)
            {
                ParseJsonGroups(jsonGroup);
            }
        }

        protected JToken checkGroupsFile()
        {
            // Ανοίγουμε το αρχείο των groups και το διαβάζουμε
            string content = openFile(groupsPath);

            // Αν το περιεχόμενο δεν υπάρχει, αναφορά λάθους
            if (content == null)
            {
                Log.Error("Initialize groups: File is missing", groupsPath);
                throw new Exception("Initialize groups: File is missing");
            }

            // Παρσάρουμε το αρχείο ως json
            JObject json = this.jsonParse(content);
            if (json == null)
            {
                Log.Error("Initialize groups: content is missing");
                throw new Exception("Initialize groups: content is missing");
            }

            // Διαβάζουμε τους πίνακες
            var jsonGroups = json.GetValue("Groups");
            if (jsonGroups == null)
            {
                Log.Error("Initialize groups: Groups list is missing", json);
                throw new Exception("Initialize groups: 'Groups' list is missing");
            }

            return jsonGroups;
        }

        private void ParseJsonGroups(JObject jsonGroup)
        {
            string tableName = (string)jsonGroup["table"];

            CheckTableName(tableName);

            BindingTable table;
            if (!tablesIndex.TryGetValue(tableName, out table))
            {
                Log.Error($"Initialize group fields: Table '{tableName}' was not found in tables index");
                throw new Exception($"Initialize group fields: Table '{tableName}' was not found in tables index");
            }

            // Get the alias from the fields.txt file
            string groupAlias = (string)jsonGroup["alias"];

            // DataTable             
            BindingTable bindingTable = createGroupTable(tableName, groupAlias);

            // Fields
            ParseGroupFields(jsonGroup, tableName, groupAlias, bindingTable, table);

            // Create a new field and a relation in the source table
            BindingField foreignKey = new BindingField(bindingTable, groupAlias + "Id", groupAlias + "Id", null, string.Empty, string.Empty);
            table.Add(foreignKey);
            table.BindingRelations.Add(new BindingRelation(bindingTable, foreignKey));

            // Scan table rows for distinct group values
            BindingTable.Enumerator enumerator = new BindingTable.Enumerator(table);
            Dictionary<GroupValue, Guid> values = new Dictionary<GroupValue, Guid>(2 * table.Count);

            // Get group field indexes
            List<BindingField> indexes = new List<BindingField>();

            for (int index = 1; index < bindingTable.BindingFields.Count; index++)
            {
                BindingField field = bindingTable.BindingFields[index];
                for (int indexSrc = 0; indexSrc < table.BindingFields.Count; indexSrc++)
                {
                    BindingField fieldSrc = table.BindingFields[indexSrc];
                    if (field.Name == fieldSrc.Name)
                        indexes.Add(fieldSrc);
                }
            }

            BindingTable.Row row;
            enumerator.Start();
            while ((row = enumerator.Next()) != null)
            {
                GroupValue groupValue = new GroupValue();

                // Get group value
                for (int index = 1; index < bindingTable.BindingFields.Count; index++)
                {
                    groupValue.Add(row.GetObject(indexes[index - 1]));
                }

                // Create a new row if necessary
                Guid id;
                BindingTable.Row bindingRow;
                if (!values.TryGetValue(groupValue, out id))
                {
                    id = Guid.NewGuid();
                    values.Add(groupValue, id);

                    bindingRow = bindingTable.NewRow(id);
                    bindingRow.Set(0, id);
                    for (int indexField = 0; indexField < groupValue.Count; indexField++)
                    {
                        bindingRow.Set(indexField + 1, groupValue[indexField]);
                    }
                }

                // Set foreign key in row
                row.Set(foreignKey, id);
            }
        }

        private void ParseGroupFields(JObject jsonGroup, string tableName, string groupAlias, BindingTable bindingTable, BindingTable sourceTable)
        {
            var jsonFields = jsonGroup["Fields"];

            if (jsonFields != null && jsonFields.Count() > 0)
            {
                foreach (var jsonField in jsonFields)
                {
                    // Name
                    string fieldName = jsonField["name"].Value<string>();

                    if (fieldName == null || fieldName == string.Empty)
                    {
                        Log.Error("Initialize group fields: A field has no name or is empty");
                        throw new Exception("Initialize group fields: A field has no name or is empty");
                    }

                    // Order 
                    string fieldOrder = (string)jsonField["order"];

                    BindingField sourceField;

                    if (fieldsIndex.TryGetValue($"{tableName}.{fieldName}", out sourceField))
                    {
                        BindingField bindingField = new BindingField(bindingTable, fieldName, fieldName, null, string.Empty, string.Empty);
                        bindingTable.Add(bindingField); // TODO: να μπει και order
                        fieldsIndex.Add($"{bindingTable.Name}.{fieldName}", bindingField);

                        BindingField field = sourceTable.BindingFields.Find((f) => f.Name == fieldName);
                        if (field != null)
                        {
                            BindingRelation relation = sourceTable.GetRelation(bindingField);
                            if (relation != null)
                            {
                                // Add a relation to bindingTable
                                bindingTable.BindingRelations.Add(new BindingRelation(relation.Table, bindingField));
                            }
                        }
                    }
                    else
                    {
                        Log.Error($"Το πεδίο '{tableName}.{fieldName}' του group {groupAlias} δεν βρέθηκε σε κάποιον από τους υπάρχοντες πίνακες.");
                        throw new Exception($"Το πεδίο '{tableName}.{fieldName}' του group {groupAlias} δεν βρέθηκε.");
                    } 
                }
            }
            else
            {
                Log.Error(String.Format("Initialize fields: Fields are missing from group {0}", groupAlias));
                throw new Exception(String.Format("Initialize fields: Fields are missing from group {0}", groupAlias));
            }
        }

        private BindingTable createGroupTable(string tableName, string groupAlias)
        {
            BindingTable groupTable = null;
            BindingTable bindingTable = dataSource.GetTable(tableName, groupAlias);

            if (bindingTable == null)
            {
                Log.Error($"Table '{tableName}' for group '{groupAlias}' doesn't exist");
                throw new Exception($"Table '{tableName}' for group '{groupAlias}' doesn't exist");
            }
            else
            {
                groupTable = dataSource.GetGroup(bindingTable, groupAlias);
                bindingTables.Add(groupTable);
                tablesIndex.Add(groupAlias, groupTable);
            }

            return groupTable;
        }

        #endregion

        #region "   INCLUDES   "

        protected void readIncludes()
        {
            string content = openFile(includesPath);

            if (content == null)
            {
                Log.Error("Initialize includes: File is missing", includesPath);
                throw new Exception("Initialize includes: File is missing");
            }

            JObject json = this.jsonParse(content);

            if (json == null)
            {
                Log.Error("Initialize fields: File has not valid 'json' format", content);
                throw new Exception("Initialize fields: File has not valid 'json' format"); 
            }

            GetIncludes(json);
        }

        private void GetIncludes(JObject json)
        {
            var jsonIncludes = json.GetValue("Includes");
            if (jsonIncludes != null)
            {
                foreach (JObject jsonInclude in jsonIncludes)
                {
                    // Alias
                    string includeAlias = GetIncludeAlias(jsonInclude);

                    // File
                    string includeFile = GetIncludeFile(jsonInclude);

                    // Table
                    BindingTable bindingTable = GetIncludeTable(jsonInclude);

                    // Filter table
                    BindingTable filterTable = GetFilterTable(jsonInclude);

                    BindAliasFileTable(includeAlias, includeFile, bindingTable, filterTable);
                }
            }
            else
            {
                Log.Error("Initialize fields: 'Tables' list is missing", json);
                throw new Exception("Initialize fields: 'Tables' list is missing");
            }
        }

        private void BindAliasFileTable(string includeAlias, string includeFile, BindingTable bindingTable, BindingTable filterTable)
        {
            BindingInclude bindingInclude = new BindingInclude(includeAlias, includeFile, bindingTable, filterTable);
            bindingIncludes.Add(bindingInclude);
            includesIndex.Add(includeAlias, bindingInclude);
        }

        private BindingTable GetIncludeTable(JObject jsonInclude)
        {
            string includeTable = (string)jsonInclude["table"];

            BindingTable bindingTable = null;
            //if (!tablesIndex.TryGetValue(includeTable, out bindingTable)) {

            if (includeTable != null && tablesIndex.ContainsKey(includeTable))
            {
                bindingTable = tablesIndex[includeTable];
            }

            return bindingTable;
        }

        private BindingTable GetFilterTable(JObject jsonInclude)
        {
            string includeTable = (string)jsonInclude["nonzero"];

            BindingTable bindingTable = null;
            //if (!tablesIndex.TryGetValue(includeTable, out bindingTable)) {

            if (includeTable != null && tablesIndex.ContainsKey(includeTable))
            {
                bindingTable = tablesIndex[includeTable];
            }

            return bindingTable;
        }

        private string GetIncludeFile(JObject jsonInclude)
        {
            string includeFile = (string)jsonInclude["file"];
            string includeDir = Path.GetDirectoryName(templatePath);
            string includePath = Path.Combine(includeDir, includeFile);
            if (!File.Exists(includePath))
            {
                Log.Error($"file '{includePath}' doesn't exist", jsonInclude);
                throw new Exception($"file '{includePath}' doesn't exist");
            }
            return includeFile;
        }

        private static string GetIncludeAlias(JObject jsonInclude)
        {
            string includeAlias = (string)jsonInclude["alias"];

            if (includeAlias == null || includeAlias == string.Empty)
            {
                Log.Error("Initialize includes: an alias is missing or is empty");
                throw new Exception("Initialize includes: an alias is missing or is empty");
            }

            return includeAlias;
        }

        #endregion

        #region "   MISCELLANEOUS   "

        protected string openFile(string path)
        {
            return File.ReadAllText(path);
        }

        protected JObject jsonParse(string text)
        {
            try
            {
                return JObject.Parse(text);
            }
            catch (Exception)
            {
                Log.Error($"text is not in json format {text}");
                return null;
            }
        }

        #endregion
    }
}

// (OK) 1. Διάβασμα των groups.txt
// (OK) 2. Δημιουργία βοηθητικών πινάκων και Dictionary με τα διακριτά groups
// (OK) 3. Δημιουργία επιπλέον σχέσεων
// (OK) 4. Προσθήκη επιπλέον πεδίων στους κανονικούς πίνακες (ίσως μέσω βοηθητικών δομών και όχι πάνω στους κανονικούς πίνακες)
// (OK) 5. Σκανάρισμα
// (OK) 6. Φιλτράρισμα των πινάκων με βάση τα groups
// 7. Events
// 8. Order By
