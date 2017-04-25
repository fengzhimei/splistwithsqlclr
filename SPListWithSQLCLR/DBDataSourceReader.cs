using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using System.Xml;

namespace SPListWithSQLCLR
{
    public class DBDataSourceReader
    {
        private string tableName;

        public DBDataSourceReader(string tableName)
        {
            this.tableName = tableName;
        }

        public List<ColumnSchema> GetDataSchema(SqlConnection connection)
        {
            List<ColumnSchema> schemas = new List<ColumnSchema>();

            string sqlQuery = string.Format("Select * From {0} where 1 = 0", this.tableName);

            DataTable dataTable = new DataTable(this.tableName);
            using (SqlDataAdapter adapter = new SqlDataAdapter(sqlQuery, connection))
            {
                adapter.FillSchema(dataTable, SchemaType.Source);
            }

            foreach (DataColumn column in dataTable.Columns)
            {
                schemas.Add(new ColumnSchema(column.ColumnName, XmlConvert.EncodeName(column.ColumnName), column.DataType, Type.GetTypeCode(column.DataType).ToString(), column.AllowDBNull, column.MaxLength));
            }

            return schemas;
        }

        public DataTable GetDataTable(SqlConnection connection)
        {
            string sqlQuery = string.Format("Select * From {0}", this.tableName);

            DataTable dataTable = new DataTable(this.tableName);
            using (SqlDataAdapter adapter = new SqlDataAdapter(sqlQuery, connection))
            {
                adapter.Fill(dataTable);
            }

            return dataTable;
        }
    }
}
