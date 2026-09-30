# How-to-preserve-resized-Width-and-Height-after-schema-designer-changes-in-WPF-PivotGridControl

## Description
This repository demonstrates how to preserve user-resized column widths and row heights in the Syncfusion [WPF PivotGridControl](https://help.syncfusion.com/wpf/pivotgrid/overview) when modifications are made to the pivot schema at runtime using the [PivotSchemaDesigner](https://help.syncfusion.com/wpf/pivot-grid/pivotschemadesigner-for-wpf).

## Overview
In the WPF PivotGridControl, users can manually resize columns and rows when `AllowResizeColumns` and `AllowResizeRows` are enabled. However, modifying fields via the `PivotSchemaDesigner` causes the `PivotGridControl` to recalculate its layout and reset column widths and row heights to their default values.

To retain customized dimensions across schema modifications:
1. Set `AutoSizeOption="None"` on `PivotGridControl` to disable automatic cell resizing.
2. Hook the `ResizingColumns` and `ResizingRows` events on `PivotGridControl.InternalGrid` to track user-resized dimensions in dictionaries keyed by row or column index.
3. Listen to the `PivotEngine.PivotSchemaChanged` event.
4. Reapply stored column widths and row heights asynchronously using `Dispatcher.BeginInvoke` with `DispatcherPriority.Loaded` after the control completes its layout recalculation.

## Project Structure
```plaintext
How-to-preserve-resized-Width-and-Height-after-schema-designer-changes-in-WPF-PivotGridControl/
├── README.md
└── PivotGrid-ResizingRowsAndColumns/
    ├── App.xaml                      # Application definition and resources
    ├── App.xaml.cs                   # Application startup logic
    ├── AssemblyInfo.cs               # Assembly attributes
    ├── MainWindow.xaml               # UI with PivotGridControl and PivotSchemaDesigner
    ├── MainWindow.xaml.cs            # Resizing logic and dimension restoration
    ├── ProductSales.cs               # Data model and sample sales data source
    ├── WpfApp1.csproj                # Project file targeting .NET 10.0 WPF
    └── WpfApp1.slnx                  # Solution file
```

## Implementation Details

### 1. XAML Configuration
Configure the `PivotGridControl` with row/column resizing enabled, `AutoSizeOption` set to `None`, and bind the `PivotSchemaDesigner` to the pivot control:

```xml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width=".35*"/>
    </Grid.ColumnDefinitions>

    <syncfusion:PivotGridControl Name="pivotGrid"
                                 HorizontalAlignment="Left" VerticalAlignment="Top"
                                 AllowSelection="True"
                                 EnableValueEditing="True"
                                 ItemSource="{Binding Source={StaticResource data}}"
                                 AllowResizeColumns="True"
                                 AllowResizeRows="True"
                                 AutoSizeOption="None">
        <syncfusion:PivotGridControl.PivotRows>
            <engine:PivotItem FieldMappingName="Product" FieldHeader="Product" TotalHeader="Total" />
            <engine:PivotItem FieldMappingName="Date" FieldHeader="Date" TotalHeader="Total"/>
        </syncfusion:PivotGridControl.PivotRows>
        <syncfusion:PivotGridControl.PivotColumns>
            <engine:PivotItem FieldMappingName="Country" FieldHeader="Country" TotalHeader="Total" />
            <engine:PivotItem FieldMappingName="State" FieldHeader="State" TotalHeader="Total"/>
        </syncfusion:PivotGridControl.PivotColumns>
        <syncfusion:PivotGridControl.PivotCalculations>
            <engine:PivotComputationInfo CalculationName="Total" FieldName="Amount" Format="C" SummaryType="DoubleTotalSum" />
            <engine:PivotComputationInfo CalculationName="Total" FieldName="Quantity" SummaryType="Count" />
        </syncfusion:PivotGridControl.PivotCalculations>
    </syncfusion:PivotGridControl>

    <!-- Schema Designer -->
    <syncfusion:PivotSchemaDesigner Grid.Column="1" Name="shemaDesginer" PivotControl="{Binding ElementName=pivotGrid}" />
</Grid>
```

### 2. C# Code-Behind
Capture resized row and column values on `MouseUp` and restore them when `PivotSchemaChanged` triggers:

```csharp
using Syncfusion.PivotAnalysis.Base;
using Syncfusion.Windows.Controls.Grid;
using System.Windows;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        // Store user-customized column widths and row heights
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
            if (args.Reason == GridResizeCellsReason.MouseUp)
            {
                for (int c = args.Columns.Left; c <= args.Columns.Right; c++)
                {
                    _customColumnWidths[c] = args.Width;
                }
            }
        }

        private void InternalGrid_ResizingRows(object sender, GridResizingRowsEventArgs args)
        {
            if (args.Reason == GridResizeCellsReason.MouseUp)
            {
                for (int r = args.Rows.Top; r <= args.Rows.Bottom; r++)
                {
                    _customRowHeights[r] = args.Height;
                }
            }
        }

        private void PivotEngine_PivotSchemaChanged(object sender, PivotSchemaChangedArgs e)
        {
            // Defer restoration until after the grid finishes its internal layout & layout reset
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
```

## Requirements to Run the Sample
- .NET 10.0 SDK or compatible runtime
- NuGet Package: `Syncfusion.PivotTable.Wpf`

## See Also
- [Syncfusion WPF PivotGrid - Resizing Documentation](https://help.syncfusion.com/wpf/pivot-grid/how-to/resizing-columns-and-rows-in-pivotgrid)
- [Syncfusion WPF PivotGrid - Schema Designer Documentation](https://help.syncfusion.com/wpf/pivot-grid/pivotschemadesigner-for-wpf)
 