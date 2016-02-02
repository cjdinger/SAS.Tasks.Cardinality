using System;
using System.Text;
using System.Xml;
using SAS.Shared.AddIns;
using SAS.Tasks.Toolkit;

namespace SAS.Tasks.DataCardinality
{
    /// <summary>
    /// Use this class to keep track of the 
    /// options that are set within your task.
    /// You must save and restore these settings when the user
    /// interacts with your task user interface,
    /// and when the task runs within the process flow.
    /// </summary>
    public class DataCardinalityTaskSettings
    {
        public DataCardinalityTaskSettings()
        {
            OutData = "WORK._OUTCARDS";
        }

        #region Properties, or task settings

        public string OutData { get; set; }

        #endregion

        #region Code to save/restore task settings
        public string ToXml()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement el = doc.CreateElement("DataCardinalityTask");
            el.SetAttribute("outdata", OutData);
            doc.AppendChild(el);
            return doc.OuterXml;
        }

        public void FromXml(string xml)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(xml);
                XmlElement el = doc["DataCardinalityTask"];
                OutData = el.GetAttribute("outdata");
            }
            catch (XmlException)
            {
                // couldn't read the XML content
            }
        }
        #endregion

        #region Routine to build a SAS program
        public string GetSasProgram(string inputData)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(SAS.Tasks.Toolkit.Helpers.UtilityFunctions.ReadFileFromAssembly("SAS.Tasks.DataCardinality.getcardinality.sas"));
            sb.AppendLine();
            sb.AppendFormat("%getcardinality({0}, {1}, 1);\n",
                inputData, OutData);
            return sb.ToString();
        }
        #endregion


    }
}
