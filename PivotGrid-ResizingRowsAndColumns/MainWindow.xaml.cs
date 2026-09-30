using Syncfusion.PivotAnalysis.Base;
using Syncfusion.Windows.Controls.Grid;
using System.Windows;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Store user-customized column widths by FieldMappingName or Column Key
        private readonly Dictionary<int, double> _customColumnWidths = new Dictionary<int, double>();
        private readonly Dictionary<int, double> _customRowHeights = new Dictionary<int, double>();
         
        public MainWindow()
        {
            InitializeComponent();
            pivotGrid.GridLayout = GridLayout.ExcelLikeLayout;
            pivotGrid.Loaded += pivotGrid_Loaded;
        }

        private void pivotGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (pivotGrid.InternalGrid != null)
            {
                pivotGrid.InternalGrid.ResizingColumns += InternalGrid_ResizingColumns;

                pivotGrid.InternalGrid.ResizingRows += InternalGrid_ResizingRows;
            }

            pivotGrid.PivotEngine.PivotSchemaChanged += PivotEngine_PivotSchemaChanged;
        }

        private void InternalGrid_ResizingColumns(object sender, GridResizingColumnsEventArgs args)
        {
            if (args.Reason == Syncfusion.Windows.Controls.Grid.GridResizeCellsReason.MouseUp)
            {
                for (int c = args.Columns.Left; c <= args.Columns.Right; c++)
                {
                    _customColumnWidths[c] = args.Width;
                }
            }
        }

        private void InternalGrid_ResizingRows(object sender, GridResizingRowsEventArgs args)
        {
            if (args.Reason == Syncfusion.Windows.Controls.Grid.GridResizeCellsReason.MouseUp)
            {
                for (int r = args.Rows.Top; r <= args.Rows.Bottom; r++)
                {
                    _customRowHeights[r] = args.Height;
                }
            }
        }

        private void PivotEngine_PivotSchemaChanged(object sender, Syncfusion.PivotAnalysis.Base.PivotSchemaChangedArgs e)
        {
            // Defer restoration until after the grid finishes its internal layout & ExcelLikeLayout reset
            Dispatcher.BeginInvoke(new Action(() =>
            {
                RestoreGridDimensions();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void RestoreGridDimensions()
        {
            if (pivotGrid.InternalGrid == null || pivotGrid.InternalGrid.Model == null)
                return;

            // Restore Column Widths
            foreach (var entry in _customColumnWidths)
            {
                if (entry.Key < pivotGrid.InternalGrid.Model.ColumnCount)
                {
                    pivotGrid.InternalGrid.Model.ColumnWidths[entry.Key] = entry.Value;
                }
            }

            // Restore Row Heights
            foreach (var entry in _customRowHeights)
            {
                if (entry.Key < pivotGrid.InternalGrid.Model.RowCount)
                {
                    pivotGrid.InternalGrid.Model.RowHeights[entry.Key] = entry.Value;
                }
            }

            pivotGrid.InternalGrid.InvalidateVisual();
        }
    }
}