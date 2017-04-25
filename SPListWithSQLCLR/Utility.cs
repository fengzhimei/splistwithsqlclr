using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Globalization;
using System.Data;
using System.Data.SqlClient;
using System.ComponentModel;
using System.Collections;

using SPListWithSQLCLR.ListService;

namespace SPListWithSQLCLR
{
    public class Utility
    {
        /// <summary>
        /// Get xml node attribute
        /// </summary>
        /// <param name="node"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetNodeAttribute(XmlNode node, string name)
        {
            XmlNode namedItem = node.Attributes.GetNamedItem(name);
            if (namedItem != null)
            {
                return namedItem.Value;
            }
            return null;
        }

        /// <summary>
        /// Check if SharePoint field allows blank
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool IsNullSPColumn(XmlNode node)
        {
            string attribute = GetNodeAttribute(node, "Required");
            if (!string.IsNullOrEmpty(attribute))
            {
                return !bool.Parse(attribute);
            }
            return true;
        }

        /// <summary>
        /// Check if SharePoint field is a computered column
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool IsComputeredSPColumn(XmlNode node)
        {
            string attribute = GetNodeAttribute(node, "ColName");
            return string.IsNullOrEmpty(attribute);
        }

        /// <summary>
        /// Check if SharePoint field is readonly
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool IsReadOnlySPColumn(XmlNode node)
        {
            string attribute = GetNodeAttribute(node, "ReadOnly");
            if (!string.IsNullOrEmpty(attribute))
            {
                return bool.Parse(attribute);
            }
            return false;
        }

        /// <summary>
        /// Check if SharePoint field is hidden
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool IsHiddenSPColumn(XmlNode node)
        {
            string attribute = GetNodeAttribute(node, "Hidden");
            if (!string.IsNullOrEmpty(attribute))
            {
                return bool.Parse(attribute);
            }
            return false;
        }

        public static bool IsIgnoredSPColumn(XmlNode node)
        {
            string[] ignoredColumns = new string[] { 
                "Attachments"
            };

            string attribute = GetNodeAttribute(node, "Name");
            if (!string.IsNullOrEmpty(attribute))
            {
                foreach (string str in ignoredColumns)
                {
                    if (string.Compare(str, attribute, true) == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsValidedColumn(XmlNode node)
        {
            return !(Utility.IsComputeredSPColumn(node) ||
                Utility.IsReadOnlySPColumn(node) ||
                Utility.IsHiddenSPColumn(node) ||
                Utility.IsIgnoredSPColumn(node));
        }

        /// <summary>
        /// Get name space table for XmlDocument
        /// </summary>
        /// <param name="nameTable"></param>
        /// <returns></returns>
        public static XmlNamespaceManager GetXmlNameSpaceManager()
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(new NameTable());
            nsmgr.AddNamespace("nsdef", "http://schemas.microsoft.com/sharepoint/soap/");
            nsmgr.AddNamespace("rs", "urn:schemas-microsoft-com:rowset");
            nsmgr.AddNamespace("z", "#RowsetSchema");

            return nsmgr;
        }

        /// <summary>
        /// Get column type base on sharepoint field type.
        /// Refer to http://msdn.microsoft.com/en-us/library/ms437580.aspx 
        /// for the whole list of definition of sharepoint field type.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static Type GetSPColumnDataType(string typeName)
        {
            switch (typeName)
            {
                // Bit
                case "AllDayEvent":
                case "Attachments":
                case "Boolean":
                case "CrossProjectLink":
                case "Recurrence":
                    return typeof(Boolean);
                // Variant
                case "Calculated":
                // VarChar
                case "Threading":
                // NVarChar
                case "Choice":
                case "Lookup":
                case "PageSeparator":
                case "Text":
                case "URL":
                case "User":
                case "WorkflowStatus":
                // NText
                case "Computed":
                case "GridChoice":
                case "LookupMulti":
                case "MultiChoice":
                case "MultiColumn":
                case "Note":
                case "UserMulti":
                    return typeof(String);
                // Varbinary
                case "ContentTypeId":
                case "ThreadIndex":
                    return typeof(Byte[]);
                // Int
                case "Counter":
                case "Integer":
                case "ModStat":
                case "WorkflowEventType":
                    return typeof(Int32);
                // Float
                case "Currency":
                case "Number":
                    return typeof(Double);
                // Datetime
                case "DateTime":
                    return typeof(DateTime);
                // Uniqueidentifier
                case "File":
                case "Guid":
                    return typeof(Guid);
            }
            return typeof(String);
        }

        /// <summary>
        /// Get column data length
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public static int GetSPColumnMaxLength(string typeName, XmlNode node)
        {
            if (typeName == "Calculated")
            {
                string resultType = GetNodeAttribute(node, "ResultType");
                if (!string.IsNullOrEmpty(resultType))
                {
                    if (resultType == "Text")
                    {
                        return 255;
                    }
                }
            }
            else if (
                typeName == "Choice" ||
                typeName == "Lookup" ||
                typeName == "PageSeparator" ||
                typeName == "User" ||
                typeName == "WorkflowStatus")
            {
                return 255;
            }
            else if (typeName == "Text")
            {
                string maxLength = GetNodeAttribute(node, "MaxLength");
                if (!string.IsNullOrEmpty(maxLength))
                {
                    return int.Parse(maxLength);
                }
                else
                {
                    return 255;
                }
            }
            else if (typeName == "URL")
            {
                // URL field actually is composed by two varchar(255) columns
                return 510;
            }
            else if (
                typeName == "Computed" ||
                typeName == "GridChoice" ||
                typeName == "LookupMulti" ||
                typeName == "MultiChoice" ||
                typeName == "MultiColumn" ||
                typeName == "Note" ||
                typeName == "UserMulti")
            {
                return 1073741823; // 2^30 - 1
            }
            else if (typeName == "ContentTypeId" || typeName == "ThreadIndex" || typeName == "Threading")
            {
                return 512;
            }

            return -1;
        }

        public static string GetListsServiceFullUrl(string serviceUrl)
        {
            Uri spsUri = new Uri(serviceUrl);
            if (!spsUri.AbsoluteUri.ToLower().EndsWith("lists.asmx"))
            {
                return spsUri.AbsoluteUri.TrimEnd('/') + "/_vti_bin/lists.asmx";
            }
            return serviceUrl;
        }

        public static string GetFolderUrl(string folderPath, string listName)
        {
            if (!string.IsNullOrEmpty(folderPath))
            {
                if (!folderPath.StartsWith("/Lists", true, CultureInfo.InvariantCulture))
                {
                    return "/Lists/" + listName + "/" + folderPath.TrimStart('/');
                }
            }
            return folderPath;
        }

        public static Lists GetListsServiceClient(string serviceUrl)
        {
            Lists listService = new Lists();
            listService.UseDefaultCredentials = true;
            listService.Url = serviceUrl;
            return listService;
        }

        /// <summary>
        /// Encode html content
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string HtmlEncode(string s)
        {
            if (s == null)
            {
                return null;
            }

            StringBuilder output = new StringBuilder();

            foreach (char c in s)
            {
                switch (c)
                {
                    case '&':
                        output.Append("&amp;");
                        break;
                    case '>':
                        output.Append("&gt;");
                        break;
                    case '<':
                        output.Append("&lt;");
                        break;
                    case '"':
                        output.Append("&quot;");
                        break;
                    default:
                        if (c > 159)
                        {
                            output.Append("&#");
                            output.Append(((int)c).ToString(CultureInfo.InvariantCulture));
                            output.Append(";");
                        }
                        else
                        {
                            output.Append(c);
                        }
                        break;
                }
            }
            return output.ToString();
        }
    }
}
