using System;
using System.Data;

namespace DocumentGenerator
{
    public class BindingRelation {
        public BindingTable Table { get; protected set; }
        public BindingField ForeignKey { get; protected set; }
       

        public BindingRelation(BindingTable table, BindingField foreignKey)
        {
            Table = table;
            ForeignKey = foreignKey;       
        }
    }
}