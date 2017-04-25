using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace SPListWithSQLCLR
{
    public class ColumnSchema
    {
        private string columnDisplayName;
        private string columnInternalName;
        private Type dataType;
        private bool allowNull;
        private int maxLength;
        private string dataTypeName;

        public ColumnSchema(string displaynName, string internalName, Type dataType, string dataTypeName, bool allowNull, int maxLength)
        {
            this.ColumnDisplayName = displaynName;
            this.ColumnInternalName = internalName;
            this.DataType = dataType;
            this.AllowNull = allowNull;
            this.MaxLength = maxLength;
            this.DataTypeName = dataTypeName;
        }

        public bool AllowNull
        {
            get
            {
                return this.allowNull;
            }
            set
            {
                this.allowNull = value;
            }
        }

        public string ColumnDisplayName
        {
            get
            {
                return this.columnDisplayName;
            }
            set
            {
                this.columnDisplayName = value;
            }
        }

        public string ColumnInternalName
        {
            get
            {
                return this.columnInternalName;
            }
            set
            {
                this.columnInternalName = value;
            }
        }

        public Type DataType
        {
            get
            {
                return this.dataType;
            }
            set
            {
                this.dataType = value;
            }
        }

        public string DataTypeName
        {
            get
            {
                return this.dataTypeName;
            }
            set
            {
                this.dataTypeName = value;
            }
        }

        public int MaxLength
        {
            get
            {
                return this.maxLength;
            }
            set
            {
                this.maxLength = value;
            }
        }
    }

    public class ColumnSchemaMatcher
    {
        string columnDisplayName;
        ColumnSchema schemaItem;

        public ColumnSchemaMatcher(string columnName)
        {
            this.columnDisplayName = columnName;
            this.schemaItem = null;
        }

        public ColumnSchemaMatcher(ColumnSchema schemaItem)
        {
            this.columnDisplayName = string.Empty;
            this.schemaItem = schemaItem;
        }

        public bool SchemaMatch(ColumnSchema schemaItem)
        {
            if (this.schemaItem != null)
            {
                return (string.Compare(this.schemaItem.ColumnDisplayName, schemaItem.ColumnDisplayName, true) == 0 &&
                    this.schemaItem.DataType == schemaItem.DataType &&
                    this.schemaItem.AllowNull == schemaItem.AllowNull);
            }
            return false;
        }

        public bool ColumnDisplayNameMatch(ColumnSchema schemaItem)
        {
            if (!string.IsNullOrEmpty(columnDisplayName))
            {
                return (string.Compare(columnDisplayName, schemaItem.ColumnDisplayName, true) == 0);
            }
            return false;
        }
    }
}
