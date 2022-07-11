using DevExpress.XtraRichEdit;
using System;
using System.Collections.Generic;
using System.Data;
using Serilog;
using Serilog.Configuration;
using Serilog.Context;
using Serilog.Parsing;

namespace DocumentGenerator
{
    internal class AppEngine
    {
        public AppEngine()
        {
        }

        internal void run()
        {
            // Φορτώνουμε τις διαδρομές για τα αρχεία που θα χρησιμοποιήσει το σύστημα
            string originalDocument = @"../../documents/Main.docx";
            string generatedDocument = @"../../documents/main_document_generated.docx";
            string fieldsPath = @"../../documents/fields.txt";
            string groupsPath = @"../../documents/groups.txt";
            string includesPath = @"../../documents/includes.txt";
            //string dataSetPath = @"../../documents/datasets/dataEnergyBuilding.xml";
            string dataSetPath = @"../../documents/datasets/dataHeatInsulation.xml";
            string dataSetPathSchema = @"../../documents/datasets/dsBuildingHeatInsulation.xsd";

            // Φορτώνουμε το dataset
            List<DataSet> dataSets = new List<DataSet>();
            DataSet dataSet = new DataSet();
            dataSet.ReadXmlSchema(dataSetPathSchema);

            dataSet.ReadXml(dataSetPath);
            dataSets.Add(dataSet);

            IDataSource dataSource = new Xml(dataSets);
            // Ενεργοποιούμε τον επεξεργαστή εγγράφων
            Processor processor = new Processor(originalDocument, fieldsPath, groupsPath, includesPath, generatedDocument, dataSource);
            processor.start();
        }
    }
}