using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Security.Principal;
using Microsoft.SqlServer.Server;
using System.Xml.XPath;

using SPListWithSQLCLR.ListService;

namespace SPListWithSQLCLR
{
    public class SPListDataSourceWriter
    {
        private string serviceUrl;
        private string listName;

        public SPListDataSourceWriter(string serviceUrl, string listName)
        {
            this.serviceUrl = Utility.GetListsServiceFullUrl(serviceUrl);
            this.listName = listName;
        }

        public XmlNode UpdateListItems(XmlElement batchXml, int batchSize)
        {
            WindowsIdentity newId = SqlContext.WindowsIdentity;
            WindowsImpersonationContext impersonatedUser = newId.Impersonate();

            try
            {
                Lists lists = Utility.GetListsServiceClient(this.serviceUrl);

                if (batchSize != -1)
                {
                    int methodCount = batchXml.ChildNodes.Count;
                    int batchCount = (int)Math.Ceiling((double)methodCount / (double)batchSize);
                    XmlDocument xmlDocResults = new XmlDocument();
                    XmlElement elementResults = xmlDocResults.CreateElement("Results", "http://schemas.microsoft.com/sharepoint/soap/");

                    for (int currentBatch = 0; currentBatch < batchCount; currentBatch++)
                    {
                        int methodStart = currentBatch * batchSize;
                        int methodEnd = Math.Min(methodStart + batchSize - 1, methodCount - 1);

                        XmlElement elementSmallBatch = (XmlElement)batchXml.CloneNode(false);

                        // for each method in the batch
                        for (int currentMethod = methodStart; currentMethod <= methodEnd; currentMethod++)
                        {
                            XmlElement element = (XmlElement)batchXml.ChildNodes[currentMethod];
                            elementSmallBatch.AppendChild(elementSmallBatch.OwnerDocument.ImportNode(element, true));
                        }

                        // execute the batch
                        XmlNode nodeBatchResult = lists.UpdateListItems(listName, elementSmallBatch);

                        // add the results of the batch into the results xml document
                        foreach (XmlElement elementResult in nodeBatchResult.ChildNodes)
                        {
                            elementResults.AppendChild(xmlDocResults.ImportNode(elementResult, true));
                        }

                        // clean up
                        elementSmallBatch.RemoveAll();

                    }

                    return elementResults;
                }
                else
                {
                    return lists.UpdateListItems(listName, batchXml);
                }
            }
            finally
            {
                impersonatedUser.Undo();
            }
        }
    }
}
