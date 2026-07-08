using System;
using System.Collections;
using System.Windows.Forms;

namespace myseq
{
    public sealed class SpawnListTag
    {
        public int ZoneSectionPriority { get; set; }
        public string ZoneSectionLabel { get; set; } = "";
    }

    // Compares two ListView items based on a selected column.
    // Compares two ListView items based on a selected column.
    public class ListViewComparer : IComparer
    {
        private readonly int ColumnNumber;
        private readonly SortOrder SortOrder;

        public ListViewComparer(int columnNumber, SortOrder sortOrder)
        {
            ColumnNumber = columnNumber;
            SortOrder = sortOrder;
        }

        // Compare two ListViewItems.
        public int Compare(object x, object y)
        {
            // Get the objects as ListViewItems.
            var item_x = x as ListViewItem;
            var item_y = y as ListViewItem;

            // Get the corresponding sub-item values.
            var string_x = item_x?.SubItems.Count > ColumnNumber ? item_x.SubItems[ColumnNumber].Text : "";
            var string_y = item_y?.SubItems.Count > ColumnNumber ? item_y.SubItems[ColumnNumber].Text : "";

            int priorityResult = CompareZoneSectionPriority(item_x, item_y);
            if (priorityResult != 0)
                return priorityResult;

            // Compare them.
            int result = CompareItems(string_x, string_y);

            // Return the correct result depending on the sort order.
            return SortOrder == SortOrder.Ascending ? result : -result;
        }

        private static int CompareItems(string string_x, string string_y)
        {
            // Compare as numbers
            if (double.TryParse(string_x, out var double_x) && double.TryParse(string_y, out var double_y))
                return double_x.CompareTo(double_y);

            // Compare as dates
            if (DateTime.TryParse(string_x, out var date_x) && DateTime.TryParse(string_y, out var date_y))
                return date_x.CompareTo(date_y);

            // Compare as strings
            return string.Compare(string_x, string_y);
        }

        private static int CompareZoneSectionPriority(ListViewItem item_x, ListViewItem item_y)
        {
            int priorityX = GetZoneSectionPriority(item_x);
            int priorityY = GetZoneSectionPriority(item_y);

            if (priorityX == priorityY)
                return 0;
            if (priorityX == 0)
                return 1;
            if (priorityY == 0)
                return -1;

            return priorityX.CompareTo(priorityY);
        }

        private static int GetZoneSectionPriority(ListViewItem item)
        {
            var tag = item?.Tag as SpawnListTag;
            return tag?.ZoneSectionPriority ?? 0;
        }
    }
}
