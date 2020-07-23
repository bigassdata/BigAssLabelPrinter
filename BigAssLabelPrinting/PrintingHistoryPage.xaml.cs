using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace BigAssLabelPrinting
{
    /// <summary>
    /// Interaction logic for PrintingHistoryPage.xaml
    /// </summary>
    public partial class PrintingHistoryPage : Page
    {
        public PrintingHistoryPage()
        {
            InitializeComponent();
        }

        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            updateGrid();
        }

        private void updateGrid()
        {
            dgv.ItemsSource = null;
            dgv.ColumnWidth = 250;
            SqlConnection connection = new SqlConnection(MainWindow.fansconnectionString);
            SqlDataAdapter da = new SqlDataAdapter();
            da.SelectCommand = new SqlCommand(@"SELECT username AS 'USER', date AS 'Date', serialnum AS 'Serial Number', lotnum AS 'Lot Number', printer AS 'Printer', labeltype AS 'Label Type', data AS 'Bartender String' FROM LabelReprintLogs Where date > (DATEADD(MONTH, -1,GETDATE())) ORDER BY date DESC", connection);
            DataTable dt = new DataTable();
            //da.Fill(dt);
            //dgv.UpdateDefaultStyle();
            
            //dgv.ItemsSource = dt.AsDataView();
            
        }

        private void Grid_GotFocus(object sender, RoutedEventArgs e){}
        private void dgv_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e){}
    }
}
