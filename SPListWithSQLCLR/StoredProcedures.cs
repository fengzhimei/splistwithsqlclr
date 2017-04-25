using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Collections.Generic;
using System.Xml;
using System.Text;

using System.Xml.XPath;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Web.Services.Protocols;

using SPListWithSQLCLR;

public partial class StoredProcedures
{
    [SqlProcedure]
    public static int InsertListItems(string tableName, string webUrl, string listName, string folderPath, int batchSize, out string resultMsg)
    {
        int resultCode = 0;

        try
        {
            List<ColumnSchema> tableSchema = GetTableSchema(tableName);
            List<ColumnSchema> listSchema = GetSharePointListSchema(webUrl, listName);

            if (tableSchema.Count > 0 && listSchema.Count > 0)
            {
                bool schemaValided = IsSchemaMatch(tableSchema, listSchema, "Insert");

                if (schemaValided)
                {
                    DataTable tableData = GetTableData(tableName);
                    XmlElement insertCaml = GenerateInsertCAML(tableData, folderPath, listName, listSchema);

                    if (insertCaml.ChildNodes.Count > 0)
                    {
                        SPListDataSourceWriter spWriter = new SPListDataSourceWriter(webUrl, listName);
                        XmlNode xmlresult = spWriter.UpdateListItems(insertCaml, batchSize);

                        ReturnResult returnResult = ProccessReturnResult(xmlresult);

                        resultCode = (returnResult.IsSucceed) ? 1 : 0;
                        resultMsg = returnResult.ReturnMessage;
                    }
                    else
                    {
                        resultCode = 1;
                        resultMsg = "Success: There is no items to insert.";
                    }
                }
                else
                {
                    resultMsg = "Failure: SharePoint list schema doesn't match with table schema (Column Name, Column Data Type, Allow Nulls).";
                }
            }
            else
            {
                resultMsg = "Failure: SharePoint list schema or table schema is invalid.";
            }
        }
        catch (Exception ex)
        {
            resultCode = 0;
            resultMsg = "Unexpected Failure: Entire batch aborted, no items affected.\t" + ex.Message;
        }

        return resultCode;
    }

    [SqlProcedure]
    public static int UpdateListItems(string tableName, string webUrl, string listName, string keyColumnName, int batchSize, out string resultMsg)
    {
        int resultCode = 0;

        try
        {
            List<ColumnSchema> tableSchema = GetTableSchema(tableName);
            List<ColumnSchema> listSchema = GetSharePointListSchema(webUrl, listName);

            if (tableSchema.Count > 0 && listSchema.Count > 0)
            {
                bool schemaValided = IsSchemaMatch(tableSchema, listSchema, "Update");

                if (schemaValided)
                {
                    DataTable tableData = GetTableData(tableName);
                    XmlElement updateCaml = GenerateUpdateCAML(tableData, listSchema, keyColumnName, webUrl, listName);

                    if (updateCaml.ChildNodes.Count > 0)
                    {
                        SPListDataSourceWriter spWriter = new SPListDataSourceWriter(webUrl, listName);
                        XmlNode xmlresult = spWriter.UpdateListItems(updateCaml, batchSize);

                        ReturnResult returnResult = ProccessReturnResult(xmlresult);

                        resultCode = (returnResult.IsSucceed) ? 1 : 0;
                        resultMsg = returnResult.ReturnMessage;
                    }
                    else
                    {
                        resultCode = 1;
                        resultMsg = "Success: There is no items to update.";
                    }
                }
                else
                {
                    resultMsg = "Failure: SharePoint list schema doesn't match with table schema (Column Name, Column Data Type, Allow Nulls).";
                }
            }
            else
            {
                resultMsg = "Failure: SharePoint list schema or table schema is invalid.";
            }
        }
        catch (Exception ex)
        {
            resultCode = 0;
            resultMsg = "Unexpected Failure: Entire batch aborted, no items affected.\t" + ex.Message;
        }

        return resultCode;
    }

    private static List<ColumnSchema> GetSharePointListSchema(string webUrl, string listName)
    {
        SPListDataSourceReader spReader = new SPListDataSourceReader(webUrl, listName);
        return spReader.GetDataSchema();
    }

    private static List<ColumnSchema> GetTableSchema(string tableName)
    {
        List<ColumnSchema> tblSchemas = new List<ColumnSchema>();

        using (SqlConnection conn = new SqlConnection("context connection=true"))
        {
            conn.Open();
            DBDataSourceReader sqlReader = new DBDataSourceReader(tableName);
            tblSchemas = sqlReader.GetDataSchema(conn);
            conn.Close();
        }
        return tblSchemas;
    }

    private static DataTable GetTableData(string tableName)
    {
        DataTable tableData = new DataTable(tableName);

        using (SqlConnection conn = new SqlConnection("context connection=true"))
        {
            conn.Open();
            DBDataSourceReader sqlReader = new DBDataSourceReader(tableName);
            tableData = sqlReader.GetDataTable(conn);
            conn.Close();
        }
        return tableData;
    }

    private static bool IsSchemaMatch(List<ColumnSchema> tableSchema, List<ColumnSchema> listSchema, string modifyType)
    {
        ColumnSchemaMatcher schemaMatcher;
        // Check if all columns in table are presented in list
        foreach (ColumnSchema schema in tableSchema)
        {
            schemaMatcher = new ColumnSchemaMatcher(schema);
            if (!listSchema.Exists(schemaMatcher.SchemaMatch))
            {
                return false;
            }
        }

        if (modifyType == "Insert")
        {
            // Check if all required column in list are presented in table
            List<ColumnSchema> requiredColumnInList = listSchema.FindAll(MatchNotNullColumn);

            foreach (ColumnSchema schema in requiredColumnInList)
            {
                schemaMatcher = new ColumnSchemaMatcher(schema);
                if (!tableSchema.Exists(schemaMatcher.SchemaMatch))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static XmlElement GenerateInsertCAML(DataTable tableData, string folderPath, string listName, List<ColumnSchema> listSchema)
    {
        List<ColumnSchema> mappingSchemas = new List<ColumnSchema>();
        ColumnSchemaMatcher schemaMatcher = null;
        ColumnSchema schemaItem = null;
        foreach (DataColumn dc in tableData.Columns)
        {
            schemaMatcher = new ColumnSchemaMatcher(dc.ColumnName);
            schemaItem = listSchema.Find(schemaMatcher.ColumnDisplayNameMatch);
            if (schemaItem != null)
            {
                mappingSchemas.Add(schemaItem);
            }
        }

        XmlDocument doc = new XmlDocument();
        XmlElement batch_element = doc.CreateElement("Batch");
        batch_element.SetAttribute("PreCalc", "True");
        batch_element.SetAttribute("RootFolder", Utility.GetFolderUrl(folderPath, listName));
        batch_element.SetAttribute("OnError", "Continue");
        StringBuilder batchXml = new StringBuilder();

        int i = 1;
        string fieldValue = "";
        foreach (DataRow dr in tableData.Rows)
        {
            batchXml.Append("<Method ID=\"" + i + "\" Cmd=\"New\">");
            batchXml.Append("<Field Name=\"ID\">New</Field>");

            foreach (ColumnSchema schema in mappingSchemas)
            {
                object columnValue = dr[schema.ColumnDisplayName];

                if (columnValue != DBNull.Value)
                {
                    if (schema.DataType == typeof(DateTime))
                    {
                        fieldValue = ((DateTime)columnValue).ToString("yyyy-MM-ddTHH:mm:ss.fff+00:00");
                    }
                    else if (schema.DataType == typeof(Guid))
                    {
                        fieldValue = ((Guid)columnValue).ToString("B");
                    }
                    else
                    {
                        fieldValue = columnValue.ToString();
                    }
                }
                else
                {
                    fieldValue = "";
                }

                batchXml.Append("<Field Name=\"" + schema.ColumnInternalName + "\">" + Utility.HtmlEncode(fieldValue) + "</Field>");
            }

            batchXml.Append("</Method>");
            i = i + 1;
        }

        batch_element.InnerXml = batchXml.ToString();

        return batch_element;
    }

    private static XmlElement GenerateUpdateCAML(DataTable tableData, List<ColumnSchema> listSchema, string keyColumnName, string serviceUrl, string listName)
    {
        keyColumnName = keyColumnName.Trim('[', ']');

        List<ColumnSchema> mappingSchemas = new List<ColumnSchema>();
        ColumnSchemaMatcher schemaMatcher = null;
        ColumnSchema schemaItem = null;
        ColumnSchema keyColumnSchemaItem = null;

        foreach (DataColumn dc in tableData.Columns)
        {
            schemaMatcher = new ColumnSchemaMatcher(dc.ColumnName);
            schemaItem = listSchema.Find(schemaMatcher.ColumnDisplayNameMatch);
            if (schemaItem != null)
            {
                if (string.Compare(schemaItem.ColumnDisplayName, keyColumnName, true) == 0)
                {
                    keyColumnSchemaItem = schemaItem;
                }
                else
                {
                    mappingSchemas.Add(schemaItem);
                }
            }
        }

        XmlDocument doc = new XmlDocument();
        XmlElement batch_element = doc.CreateElement("Batch");
        batch_element.SetAttribute("PreCalc", "True");
        batch_element.SetAttribute("OnError", "Continue");
        StringBuilder batchXml = new StringBuilder();

        if (tableData.Rows.Count > 0)
        {
            if (keyColumnSchemaItem != null)
            {
                string typeName = keyColumnSchemaItem.DataTypeName;

                SPListDataSourceReader spReader = new SPListDataSourceReader(serviceUrl, listName);
                XmlNamespaceManager nsMgr = Utility.GetXmlNameSpaceManager();
                string camlQueryToken = "<Eq><FieldRef Name=\"{0}\" /><Value Type=\"{1}\">{2}</Value></Eq>";
                StringBuilder sbCamlQuery = new StringBuilder();
                StringCollection orClauses = new StringCollection();

                foreach (DataRow dr in tableData.Rows)
                {
                    object pkColumnValue = dr[keyColumnName];
                    if (pkColumnValue != null)
                    {
                        if (pkColumnValue != DBNull.Value)
                        {
                            orClauses.Add(string.Format(camlQueryToken, keyColumnSchemaItem.ColumnInternalName, typeName, pkColumnValue.ToString()));
                        }
                    }
                }

                sbCamlQuery.Append("<Where>");
                sbCamlQuery.Append("<And>");

                if (orClauses.Count == 1)
                {
                    sbCamlQuery.Append(orClauses[0].ToString());
                }
                else if (orClauses.Count == 2)
                {
                    sbCamlQuery.Append("<Or>" + orClauses[0].ToString() + orClauses[1].ToString() + "</Or>");
                }
                else if (orClauses.Count > 2)
                {
                    for (int i = orClauses.Count - 1; i > 0; i--)
                    {
                        sbCamlQuery.Append("<Or>");
                    }

                    sbCamlQuery.Append(orClauses[0].ToString() + orClauses[1].ToString() + "</Or>");

                    for (int j = 2; j < orClauses.Count; j++)
                    {
                        sbCamlQuery.Append(orClauses[j].ToString() + "</Or>");
                    }
                }

                sbCamlQuery.Append("<Neq>");
                sbCamlQuery.Append("<FieldRef Name=\"FSObjType\"/>");
                sbCamlQuery.Append("<Value Type=\"Lookup\">1</Value>");
                sbCamlQuery.Append("</Neq>");
                sbCamlQuery.Append("</And>");
                sbCamlQuery.Append("</Where>");

                XmlNode resultNode = spReader.GetListItems(sbCamlQuery.ToString());
                XPathNavigator navigator = resultNode.CreateNavigator();
                XPathNodeIterator itemsNavigator = navigator.Select("/rs:data/z:row", nsMgr);

                int k = 1;
                string fieldValue = "";
                while (itemsNavigator.MoveNext())
                {
                    XPathNavigator currentNode = itemsNavigator.Current;
                    XPathNavigator idNode = currentNode.SelectSingleNode("./@ows_ID");
                    XPathNavigator keyColumnNode = currentNode.SelectSingleNode("./@ows_" + keyColumnSchemaItem.ColumnInternalName);
                    if (idNode != null && keyColumnNode != null)
                    {
                        string strExpression = string.Empty;

                        if (keyColumnSchemaItem.DataTypeName == "Text")
                        {
                            strExpression = "[" + keyColumnName + "]" + " = '" + keyColumnNode.Value + "'";
                        }
                        else
                        {
                            strExpression = "[" + keyColumnName + "]" + " = " + keyColumnNode.Value;
                        }

                        DataRow[] rows = tableData.Select(strExpression);
                        if (rows.Length == 1)
                        {
                            batchXml.Append("<Method ID=\"" + k + "\" Cmd=\"Update\">");
                            batchXml.Append("<Field Name=\"ID\">" + idNode.Value + "</Field>");

                            foreach (ColumnSchema schema in mappingSchemas)
                            {
                                object columnValue = rows[0][schema.ColumnDisplayName];

                                if (columnValue != DBNull.Value)
                                {
                                    if (schema.DataType == typeof(DateTime))
                                    {
                                        fieldValue = ((DateTime)columnValue).ToString("yyyy-MM-ddTHH:mm:ss.fff+00:00");
                                    }
                                    else if (schema.DataType == typeof(Guid))
                                    {
                                        fieldValue = ((Guid)columnValue).ToString("B");
                                    }
                                    else
                                    {
                                        fieldValue = columnValue.ToString();
                                    }
                                }
                                else
                                {
                                    fieldValue = "";
                                }

                                batchXml.Append("<Field Name=\"" + schema.ColumnInternalName + "\">" + Utility.HtmlEncode(fieldValue) + "</Field>");
                            }
                            batchXml.Append("</Method>");

                            k = k + 1;
                        }
                        else
                        {
                            throw new Exception(keyColumnName + " is not an identity column (or Primary Key) in the table.");
                        }
                    }
                }
            }

            batch_element.InnerXml = batchXml.ToString();
        }

        return batch_element;
    }

    private static bool MatchNotNullColumn(ColumnSchema schema)
    {
        return !schema.AllowNull;
    }

    private static ReturnResult ProccessReturnResult(XmlNode resultNode)
    {
        ReturnResult result = new ReturnResult();
        XmlDocument document = new XmlDocument();
        document.LoadXml(resultNode.OuterXml);
        XmlNamespaceManager nsmgr = Utility.GetXmlNameSpaceManager();

        XmlNodeList errorCodeNodes = document.SelectNodes("//nsdef:Results/nsdef:Result/nsdef:ErrorCode", nsmgr);
        int allRows = errorCodeNodes.Count;
        int failedRows = 0;

        foreach (XmlNode errorNode in errorCodeNodes)
        {
            if (errorNode.InnerText != "0x00000000")
            {
                failedRows = failedRows + 1;
            }
        }

        if (failedRows == 0)
        {
            result.IsSucceed = true;
            result.ReturnMessage = "Success: " + allRows + " of " + allRows + " items affected.";
        }
        else
        {
            result.IsSucceed = false;
            result.ReturnMessage = "Failure: " + (allRows - failedRows) + " of " + allRows + " items affected.\t" + resultNode.OuterXml;
        }

        return result;
    }

    private static string PrintSchema(List<ColumnSchema> schemas)
    {
        StringBuilder sbSchema = new StringBuilder();

        string printValueToken = "Column Name: {0} | Column Type: {1} | Column Data Length: {2} | Column Allows Null: {3}";
        foreach (ColumnSchema schema in schemas)
        {
            sbSchema.Append(string.Format(printValueToken, schema.ColumnDisplayName, schema.DataType.ToString(), schema.MaxLength.ToString(), schema.AllowNull.ToString()));
        }

        return sbSchema.ToString();
    }
};
