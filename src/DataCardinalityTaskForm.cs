using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SAS.Shared.AddIns;
using SAS.Tasks.Toolkit;
using SAS.Tasks.Toolkit.Controls;
using SAS.Tasks.Toolkit.Data;

namespace SAS.Tasks.DataCardinality
{
    /// <summary>
    /// This windows form inherits from the TaskForm class, which
    /// includes a bit of special handling for SAS Enterprise Guide.
    /// </summary>
    public partial class DataCardinalityTaskForm : SAS.Tasks.Toolkit.Controls.TaskForm
    {
        // internal classes to track info gathered for the list view
        internal class Cardinality
        {
            internal int levels = 0;
            internal double pct_unique =0.0;
        }

        internal class ColTag
        {
            internal SasColumn SasColumn = null;
            internal Cardinality Cardinality = null;
        }

        Dictionary<string, Cardinality> cardinalities = new Dictionary<string, Cardinality>();
        ImageList varTypes = new ImageList();

        public DataCardinalityTaskSettings Settings { get; set; }

        public DataCardinalityTaskForm(SAS.Shared.AddIns.ISASTaskConsumer3 consumer)
        {
            InitializeComponent();

            // provide a handle to the SAS Enterprise Guide application
            this.Consumer = consumer;

            varTypes = SasData.DataTypesImageListSmall;

            lvColumns.SmallImageList = varTypes;
            lvColumns.LargeImageList = varTypes;

        }

        // initialize the form with the values from the settings
        protected override void OnLoad(EventArgs e)
        {
            lblData.Text = string.Format("{0}.{1}", Consumer.ActiveData.Library, Consumer.ActiveData.Member);

            int rowCount = -1;
            if (Consumer.ActiveData.Accessor.Open())
            {
                 rowCount = Consumer.ActiveData.Accessor.RowCount;
                 Consumer.ActiveData.Accessor.Close();
            }

            lblRecords.Text = string.Format(Translate.RecordsLabelFmt, 
                rowCount>0 ? rowCount.ToString("#,###") : Translate.Unknown);

            lvColumns.ColumnClick += new ColumnClickEventHandler(lvColumns_ColumnClick);

            txtOutData.Text = Settings.OutData;

            CardinalityTask.AssignLocalLibraryIfNeeded(Consumer);
            PopulateDataView();
            base.OnLoad(e);
        }

        // Sort handlers for the list view columns
        private int sortColumn = -1;

        void lvColumns_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine whether the column is the same as the last column clicked.
            if (e.Column != sortColumn)
            {
                // Set the sort column to the new column.
                sortColumn = e.Column;
                // Set the sort order to ascending by default.
                lvColumns.Sorting = SortOrder.Ascending;
            }
            else
            {
                // Determine what the last sort order was and change it.
                if (lvColumns.Sorting == SortOrder.Ascending)
                    lvColumns.Sorting = SortOrder.Descending;
                else
                    lvColumns.Sorting = SortOrder.Ascending;
            }

            // Call the sort method to manually sort.
            lvColumns.Sort();
            // Set the ListViewItemSorter property to a new ListViewItemComparer
            // object.
            this.lvColumns.ListViewItemSorter = new DataItemComparer(e.Column,
                                                              lvColumns.Sorting);
        }

        internal enum eColIndex
        {
            Name = 0,
            Type = 1,
            Length = 2,
            Format = 3,
            Cardinality = 4,
            PctUnique = 5
        }

        private void PopulateDataView()
        {
            SasData d = new SasData(Consumer.AssignedServer, Consumer.ActiveData.Library, Consumer.ActiveData.Member.ToUpper());
            foreach (SasColumn c in d.GetColumns())
            {
                ListViewItem li = new ListViewItem(c.Name);
                
                li.SubItems.Add(c.Type.ToString());
                li.SubItems.Add(c.Length.ToString());
                li.SubItems.Add(c.Format);
                li.SubItems.Add(Translate.Unknown);
                li.SubItems.Add(Translate.Unknown);
                li.ImageIndex = (int)c.Category;
                li.Tag = new ColTag() { SasColumn = c, Cardinality=new Cardinality() };

                lvColumns.Items.Add(li);
            }

            SortAvailableList();
        }

        private void SortAvailableList()
        {
            lvColumns.Sorting = SortOrder.Ascending;
            // Call the sort method to manually sort.
            lvColumns.Sort();
            // Set the ListViewItemSorter property to a new ListViewItemComparer
            // object.
            this.lvColumns.ListViewItemSorter = new DataItemComparer(0, lvColumns.Sorting);
        }

        // save any values from the dialog into the settings class
        protected override void OnClosing(CancelEventArgs e)
        {
            if (this.DialogResult == DialogResult.OK)
            {
                if (!isValidOutput(txtOutData.Text))
                {
                    e.Cancel = true;
                    MessageBox.Show(string.Format(Translate.MsgInvalidName, txtOutData.Text));
                }
                else
                    Settings.OutData = txtOutData.Text;
            }

            base.OnClosing(e);
        }

        #region Use SasSubmitter to calculate cardinality

        // to hold current cursor while processing, showing WaitCursor
        Cursor _savedCursor = null;
        // SAS job ID in case we need to cancel
        int sasJobId = -1;
        // Progress window with Cancel button
        SubmitProgressForm progressdlg = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCalc_Click(object sender, EventArgs e)
        {            
            string code = SAS.Tasks.Toolkit.Helpers.UtilityFunctions.ReadFileFromAssembly("SAS.Tasks.DataCardinality.getcardinality.sas");
            code += 
                string.Format("%getcardinality({0}, {1},0);\n",
                SAS.Tasks.Toolkit.Helpers.TaskDataHelpers.GetSasCodeReference(Consumer.ActiveData as ISASTaskData2),
                "work._ds_cardinalities"
                );
            SasSubmitter s = new SasSubmitter(Consumer.AssignedServer);
            if (!s.IsServerBusy())
            {
                _savedCursor = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
                s.SubmitSasProgramComplete += handle_SubmitSasProgramComplete;
                sasJobId = s.SubmitSasProgram(code);
                progressdlg = new SubmitProgressForm();
                if (progressdlg.ShowDialog(this) == DialogResult.Cancel)
                {
                    s.CancelJob(sasJobId);
                    progressdlg = null;
                }
            }
            else
                MessageBox.Show(string.Format("The server {0} is busy; cannot process cardinalities.", Consumer.AssignedServer));
        }

        void handle_SubmitSasProgramComplete(object sender, SubmitCompleteEventArgs args)
        {
            BeginInvoke(new MethodInvoker(delegate()
                {
                    if (progressdlg != null && progressdlg.Visible)
                    {
                        progressdlg.Close();
                        progressdlg = null;
                    }
                    sasJobId = -1;
                    if (args.Success)
                    {
                        AddCardinalities();
                    }
                    Cursor.Current = _savedCursor;
                }
                ));
        }

        private void AddCardinalities()
        {
            SasServer s = new SasServer(Consumer.AssignedServer);
            using (OleDbConnection conn = s.GetOleDbConnection())
            {
                try
                {
                    //----- make provider connection
                    conn.Open();

                    //----- Read values from query command
                    string sql = @"select * from work._ds_cardinalities";
                    OleDbCommand cmdDB = new OleDbCommand(sql, conn);
                    OleDbDataReader dataReader = cmdDB.ExecuteReader(CommandBehavior.CloseConnection);
                    while (dataReader.Read())
                    {
                        cardinalities.Add(
                            dataReader["name"].ToString(),
                            new Cardinality()
                            {
                                levels = Convert.ToInt32(dataReader["nlevels"]),
                                pct_unique = Convert.ToDouble(dataReader["pct_unique"])
                            }
                            );
                    }

                    lvColumns.BeginUpdate();
                    foreach (ListViewItem li in lvColumns.Items)
                    {
                        Cardinality c = cardinalities[li.Text];
                        if (c != null)
                        {
                            li.SubItems[(int)eColIndex.Cardinality].Text = c.levels.ToString();
                            li.SubItems[(int)eColIndex.PctUnique].Text = c.pct_unique.ToString("#0.00%");
                            ((ColTag)li.Tag).Cardinality = c;
                        }
                    }
                    lvColumns.EndUpdate();
                }
                catch { }
                finally
                {
                   conn.Close();
                }
            }            
        }
        #endregion

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            string cookie = "";
            ISASTaskDataName dn = null;
            dn = Consumer.ShowOutputDataSelector(this, SAS.Shared.AddIns.ServerAccessMode.OneServer,
                Consumer.AssignedServer, "", "", ref cookie);
            if (dn != null)
            {
                txtOutData.Text = string.Format("{0}.{1}", dn.Library, dn.Member);
            }
        }

        private bool isValidOutput(string output)
        {
            // validate dataset name - some crude validation
            // a regular expression would be better
            string[] parts = output.Trim().Split(new char[] { '.' });
            if (output.Trim().Contains(" ") ||
                parts.Length != 2 ||
                !SAS.Shared.UtilityFunctions.IsValidSASName(parts[0], 8) ||
                !SAS.Shared.UtilityFunctions.IsValidSASName(parts[1], 32))
            {
                return false;
            }
            return true;
        }

        private void txtOutData_TextChanged(object sender, EventArgs e)
        {

        }

    }
}
