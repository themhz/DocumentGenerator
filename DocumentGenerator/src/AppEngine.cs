using DevExpress.XtraRichEdit;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using Serilog;
using Serilog.Configuration;
using Serilog.Context;
using Serilog.Parsing;



namespace DocumentGenerator {
    internal class AppEngine {
        public AppEngine() {

        }

        internal void run() {
            

            //Φορτώνουμε τις διαδρομές για τα αρχεία που θα χρησιμοποιήσει το σύστημα
            string originalDocument = @"../../documents/Main.docx";
            string generatedDocument = @"../../documents/main_document_generated.docx";
            string fieldsPath = @"../../documents/fields.txt";
            string includesPath = @"../../documents/includes.txt";
            string dataSetPath = @"../../documents/datasets/dataEnergyBuilding.xml";
            string dataSetPathSchema = @"../../documents/datasets/dsBuildingHeatInsulation.xsd";

           // var log = new LoggerConfiguration()
           //.WriteTo.Console()
           //.WriteTo.File("logs.txt", rollingInterval: RollingInterval.Day)
           //.CreateLogger();

            //Φορτώνουμε το dataset
            List<DataSet> dataSets = new List<DataSet>();
            DataSet dataSet = new DataSet();
            dataSet.ReadXmlSchema(dataSetPathSchema);
            dataSet.ReadXml(dataSetPath);
            dataSets.Add(dataSet);

            IDataSource dataSource = new Xml(dataSets);

         


            //Ενεργοποιούμε τον επεξεργαστή εγγράφων
            Processor processor = new Processor(originalDocument, fieldsPath, includesPath, generatedDocument, dataSource);
            processor.start();

            //3.Καλούμε την επεξεργασία του document(master template)
            //3.0 Προσθέτουμε το τρέχον ID στο context stack (μαζί με τον πίνακα) *
            //3.1 Συλλέγουμε τα aliases από το document και τα επιβεβαιώνουμε αν είναι έγκυρα

            //3.2 Για κάθε alias:
            //3.2.1 Αν είναι field αντικαθιστούμε το κείμενο
            //3.2.2 Αν είναι include:
            //3.2.2.1 Φορτώνουμε το template:
            //3.2.2.2 Για όλα τα rows του πίνακα του include:
            //3.2.2.2.1 Κάνουμε κλώνο του template document
            //3.2.2.2.2 Καλούμε το βήμα 3 ->
            //3.2.2.2.3 Το παραγόμενο document το εισάγουμε στο τρέχον document

            //3.2.3 Αν είναι πίνακας, για κάθε πίνακα:
            //3.2.3.1 Καταγράφουμε τις παραμέτρους του alias (διαβάζουμε το comment)
            //3.2.3.2 Αφαιρούμε το comment
            //3.2.3.3 Παράγουμε τον πίνακα στη μνήμη
            //3.2.3.4 Διαγράφουμε τον αρχικό πίνακα και εισάγουμε τον παραχθέντα

            //3.3 Αφαιρούμε το ID από το context stack *
            //4.Γράφουμε το .docx στο δίσκο και επιστρέφουμε το path του
        }

        
    }
}