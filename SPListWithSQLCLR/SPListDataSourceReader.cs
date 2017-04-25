using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Xml;
using System.Security.Principal;
using Microsoft.SqlServer.Server;

using SPListWithSQLCLR.ListService;

namespace SPListWithSQLCLR
{
    public class SPListDataSourceReader
    {
        private string serviceUrl;
        private string listName;

        public SPListDataSourceReader(string serviceUrl, string listName)
        {
            this.serviceUrl = Utility.GetListsServiceFullUrl(serviceUrl);
            this.listName = listName;
        }

        public List<ColumnSchema> GetDataSchema()
        {
            List<ColumnSchema> schemas = new List<ColumnSchema>();
            XmlDocument document = new XmlDocument();
            XmlNode listSchema = null;

            WindowsIdentity newId = SqlContext.WindowsIdentity;
            WindowsImpersonationContext impersonatedUser = newId.Impersonate();

            try
            {
                Lists lists = Utility.GetListsServiceClient(this.serviceUrl);
                listSchema = lists.GetList(this.listName);
            }
            finally
            {
                impersonatedUser.Undo();
            }

            if (listSchema != null)
            {
                document.LoadXml(listSchema.OuterXml);
                XmlNamespaceManager nsmgr = Utility.GetXmlNameSpaceManager();

                foreach (XmlNode fieldNode in document.SelectNodes("//nsdef:List/nsdef:Fields/nsdef:Field", nsmgr))
                {
                    if (Utility.IsValidedColumn(fieldNode))
                    {
                        string displayName = Utility.GetNodeAttribute(fieldNode, "DisplayName");
                        string internalName = Utility.GetNodeAttribute(fieldNode, "Name");
                        string typeName = Utility.GetNodeAttribute(fieldNode, "Type");
                        Type colType = Utility.GetSPColumnDataType(typeName);
                        bool allowNull = Utility.IsNullSPColumn(fieldNode);
                        int maxLength = Utility.GetSPColumnMaxLength(typeName, fieldNode);
                        schemas.Add(new ColumnSchema(displayName, internalName, colType, typeName, allowNull, maxLength));
                    }
                }
            }

            return schemas;
        }

        public XmlNode GetListItems(string camlQuery)
        {
            WindowsIdentity newId = SqlContext.WindowsIdentity;
            WindowsImpersonationContext impersonatedUser = newId.Impersonate();

            XmlDocument xmlDoc = new XmlDocument();
            XmlNode ndQuery = xmlDoc.CreateNode(XmlNodeType.Element, "Query" ,"");
            XmlNode ndViewFields = xmlDoc.CreateNode(XmlNodeType.Element, "ViewFields", "");
            XmlNode ndQueryOptions = xmlDoc.CreateNode(XmlNodeType.Element, "QueryOptions", "");
            ndQuery.InnerXml = camlQuery;
            ndQueryOptions.InnerXml = "<ViewAttributes Scope=\"Recursive\" /><IncludeMandatoryColumns>False</IncludeMandatoryColumns>";

            try
            {
                Lists lists = Utility.GetListsServiceClient(this.serviceUrl);

                return lists.GetListItems(this.listName, null, ndQuery, ndViewFields, null, ndQueryOptions, null);
            }
            finally
            {
                impersonatedUser.Undo();
            }
        }
    }
}
