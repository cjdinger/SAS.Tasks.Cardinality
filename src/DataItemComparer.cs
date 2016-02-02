using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SAS.Tasks.DataCardinality
{
    class DataItemComparer : IComparer
    {
            private int col;
            private SortOrder order;
            public DataItemComparer()
            {
                col = 0;
                order = SortOrder.Ascending;
            }
            public DataItemComparer(int column, SortOrder order)
            {
                col = column;
                this.order = order;
            }
            public int Compare(object x, object y)
            {
                int returnVal = -1;
                switch (( DataCardinalityTaskForm.eColIndex)col)
                {
                    case DataCardinalityTaskForm.eColIndex.Length:
                        int val1 = Convert.ToInt32(((ListViewItem)x).SubItems[col].Text);
                        int val2 = Convert.ToInt32(((ListViewItem)y).SubItems[col].Text);
                        returnVal = val1 > val2 ? 1 : -1;
                        break;

                    case DataCardinalityTaskForm.eColIndex.Cardinality:
                        returnVal = ((DataCardinalityTaskForm.ColTag)((ListViewItem)x).Tag).Cardinality.levels >
                            ((DataCardinalityTaskForm.ColTag)((ListViewItem)y).Tag).Cardinality.levels
                            ? 1 : -1;
                        break;

                    case DataCardinalityTaskForm.eColIndex.PctUnique:
                        returnVal = ((DataCardinalityTaskForm.ColTag)((ListViewItem)x).Tag).Cardinality.pct_unique >
                            ((DataCardinalityTaskForm.ColTag)((ListViewItem)y).Tag).Cardinality.pct_unique
                            ? 1 : -1;
                        break;

                    case DataCardinalityTaskForm.eColIndex.Name:
                    case DataCardinalityTaskForm.eColIndex.Type:
                    case DataCardinalityTaskForm.eColIndex.Format:
                        returnVal =
                        String.Compare(((ListViewItem)x).SubItems[col].Text,
                                        ((ListViewItem)y).SubItems[col].Text);
                        break;
                };

                // Determine whether the sort order is descending.
                if (order == SortOrder.Descending)
                    // Invert the value returned by String.Compare.
                    returnVal *= -1;
                return returnVal;
            }
    }
}
