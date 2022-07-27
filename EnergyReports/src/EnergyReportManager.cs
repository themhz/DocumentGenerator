using System;
using System.Collections.Generic;
using System.Collections;
using System.Data;
using System.IO;
using System.Linq;
using DevExpress.XtraRichEdit;
using DocumentGenerator.DXDocuments;
using EnergyReports;

namespace DocumentGenerator
{
    class EnergyReportManager: Processor
    {
        public EnergyReportManager(string _templatePath, string _fieldsPath, string _groupsPath, string _includesPath, string _savePath, IDataSource _dataSource = null) : base(_templatePath, _fieldsPath, _groupsPath, _includesPath, _savePath, _dataSource)
        {
        }

        protected override void onPrepareData() {
            // Create new table from HorizontalLevels & Annex5HorizontalElements
            // Create relations
        }

        protected override bool onFilterRow(BindingTable.Row row)
        {         

            if (row.Table.Name == "Annex7Walls")
            {
                if (Math.Abs((double)row.DataRow["AreaOverGround"]) < 0.001)
                {
                    return false;
                }
            }

            if (row.Table.Name == "PageBLevels")
            {
                if (!Equals(row.DataRow["IsLevel"], true))
                {
                    return false;
                }

            }

        

            return true;
        }
    }
}
