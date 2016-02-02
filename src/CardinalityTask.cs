using System;
using System.Text;
using SAS.Shared.AddIns;
using SAS.Tasks.Toolkit;

namespace SAS.Tasks.DataCardinality
{
    // unique identifier for this task
    [ClassId("6453009C-FC4A-4D02-A295-6A4AEED34800")]
    // location of the task icon to show in the menu and process flow
    [IconLocation("SAS.Tasks.DataCardinality.task.ico")]
    [InputRequired(InputResourceType.Data)]
    public class CardinalityTask : SAS.Tasks.Toolkit.SasTask
    {
        #region private members

        private DataCardinalityTaskSettings settings;

        #endregion

        #region Initialization
        public CardinalityTask()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CardinalityTask));
            // 
            // DataCardinalityTask
            // 
            resources.ApplyResources(this, "$this");

        }
        #endregion

        #region overrides
        public override bool Initialize()
        {
            settings = new DataCardinalityTaskSettings();
            return true;
        }

        public override string GetXmlState()
        {
            return settings.ToXml();
        }

        public override void RestoreStateFromXml(string xmlState)
        {
            settings = new DataCardinalityTaskSettings();
            settings.FromXml(xmlState);
        }

        /// <summary>
        /// Show the task user interface
        /// </summary>
        /// <param name="Owner"></param>
        /// <returns>whether to cancel the task, or run now</returns>
        public override ShowResult Show(System.Windows.Forms.IWin32Window Owner)
        {
            DataCardinalityTaskForm dlg = new DataCardinalityTaskForm(this.Consumer);
            dlg.Settings = settings;
            if (System.Windows.Forms.DialogResult.OK == dlg.ShowDialog(Owner))
            {
                // gather settings values from the dialog
                settings = dlg.Settings;
                return ShowResult.RunNow;
            }
            else
                return ShowResult.Canceled;
        }

        /// <summary>
        /// Get the SAS program that this task should generate
        /// based on the options specified.
        /// </summary>
        /// <returns>a valid SAS program to run</returns>
        public override string GetSasCode()
        {
            string pref = SAS.Tasks.Toolkit.Helpers.UtilityFunctions.BuildSasTaskCodeHeader(this.TaskName, SAS.Tasks.Toolkit.Helpers.TaskDataHelpers.GetSasCodeReference(Consumer.ActiveData as ISASTaskData2),
                Consumer.AssignedServer);
            string body = settings.GetSasProgram(SAS.Tasks.Toolkit.Helpers.TaskDataHelpers.GetSasCodeReference(Consumer.ActiveData as ISASTaskData2));
            return pref + body;
        }

        public override System.Collections.Generic.List<SAS.Shared.AddIns.ISASTaskDataDescriptor> OutputDataDescriptorList
        {
            get
            {
                System.Collections.Generic.List<ISASTaskDataDescriptor> outList =
                    new System.Collections.Generic.List<ISASTaskDataDescriptor>();

                string[] parts;
                parts = settings.OutData.Split('.');
                if (parts.Length == 2)
                {
                    outList.Add(
                        // use this helper method to build the output descriptor
                        SAS.Shared.AddIns.SASTaskDataDescriptor.CreateLibrefDataDescriptor(
                            Consumer.AssignedServer, parts[0], parts[1], "Cardinalities")
                        );
                }
                return outList;
            }
        }

        #endregion

        /// <summary>
        /// In the special case where we have a local SAS data set file (sas7bdat),
        /// and a local SAS server, we have to make sure that there is a library
        /// assigned.  
        /// </summary>
        /// <param name="sd"></param>
        internal static void AssignLocalLibraryIfNeeded(ISASTaskConsumer3 consumer)
        {
            SAS.Tasks.Toolkit.Data.SasData sd = new SAS.Tasks.Toolkit.Data.SasData(consumer.ActiveData as ISASTaskData2);
            // get a SasServer object so we can see if it's the "Local" server
            SAS.Tasks.Toolkit.SasServer server = new SAS.Tasks.Toolkit.SasServer(sd.Server);
            // local server with local file, so we have to assign library
            if (server.IsLocal)
            {
                // see if the data reference is a file path ("c:\data\myfile.sas7bdat")
                if (!string.IsNullOrEmpty(consumer.ActiveData.File) &&
                    consumer.ActiveData.Source == SourceType.SasDataset &&
                    consumer.ActiveData.File.Contains("\\"))
                {
                    string path = System.IO.Path.GetDirectoryName(consumer.ActiveData.File);
                    SasSubmitter submitter = new SasSubmitter(sd.Server);
                    string log;
                    submitter.SubmitSasProgramAndWait(string.Format("libname {0} \"{1}\";\r\n", sd.Libref, path), out log);
                }
            }
        }
    }
}
