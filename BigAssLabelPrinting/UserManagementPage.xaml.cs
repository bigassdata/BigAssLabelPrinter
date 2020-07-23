using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BigAssLabelPrinting
{
    /// <summary>
    /// Interaction logic for UserManagementPage.xaml
    /// </summary>
    public partial class UserManagementPage : Page
    {
        public UserManagementPage()
        {
            InitializeComponent();
        }

        private void dgv_Loaded(object sender, RoutedEventArgs e)
        {
            updateGrid();
            updatePrinterBox();
        }

        private void updateGrid()
        {
            
            removeUserComboBox.Items.Clear();
            dgv.ItemsSource = null;
            dgv.ColumnWidth = 250;
            SqlConnection connection = new SqlConnection(MainWindow.fansconnectionString);
            SqlDataAdapter da = new SqlDataAdapter();
            da.SelectCommand = new SqlCommand(@"SELECT username AS 'USER', lastlogin AS 'Last Login', usertype AS 'Administrator' FROM LabelReprintUsers ORDER BY username ASC", connection);
            DataTable dt = new DataTable();
            da.Fill(dt);
            dgv.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Source = dt });
            
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                if (dt.Rows.Count == 0)
                {
                    return;
                }
                removeUserComboBox.Items.Add(dt.Rows[i]["USER"]);
                adminComboBox.Items.Add(dt.Rows[i]["USER"]);
            }

        }

        private void Grid_GotFocus(object sender, RoutedEventArgs e)
        {
            //updateGrid();
        }

        private void removeUserButton_Click(object sender, RoutedEventArgs e)
        {
            string user = removeUserComboBox.Text;
            using (SqlConnection connection = new SqlConnection(MainWindow.fansconnectionString))
            using (SqlCommand command = connection.CreateCommand())
            {

                command.CommandText = "DELETE FROM LabelReprintUsers WHERE username = '" + user + "'";
               

                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
            }
            infoTextbox.Text += "User has been removed: " + user + "." + Environment.NewLine;
            updateGrid();
        }

        private bool IsAdmin(string user)
        {
            SqlConnection connection = new SqlConnection(MainWindow.fansconnectionString);
            SqlDataAdapter da = new SqlDataAdapter();
            da.SelectCommand = new SqlCommand(@"SELECT username AS 'USER', lastlogin AS 'Last Login', usertype AS 'Administrator' FROM LabelReprintUsers WHERE username = '"+user+"'", connection);
            DataTable dt = new DataTable();
            da.Fill(dt);
            if (dt.Rows.Count == 0)
            {
                return false;
            }
            return Convert.ToBoolean(dt.Rows[0]["Administrator"]);

        }

        private void adminButton_Click(object sender, RoutedEventArgs e)
        {
            string user = adminComboBox.Text;
            using (SqlConnection connection = new SqlConnection(MainWindow.fansconnectionString))
                using (SqlCommand command = connection.CreateCommand())
                {
                    if (IsAdmin(user))
                    {
                        command.CommandText = "UPDATE LabelReprintUsers set usertype = 0 WHERE username = '"+user + "'";
                        infoTextbox.Text += "Administrator priviledges removed from user " + user + "." + Environment.NewLine;
                    }
                    else
                    {
                        command.CommandText = "UPDATE LabelReprintUsers set usertype = 1 WHERE username = '" + user + "'";
                        infoTextbox.Text += "Administrator priviledges added to user " + user + "." + Environment.NewLine;
                    }
                   
                   connection.Open();
                   command.ExecuteNonQuery();
                   connection.Close();
               }
            updateGrid();
        }

        private void addUserButton_Click(object sender, RoutedEventArgs e)
        {
            string user = addUserTextBox.Text;
            using (SqlConnection connection = new SqlConnection(MainWindow.fansconnectionString))
            using (SqlCommand command = connection.CreateCommand())
            {

                command.CommandText = "INSERT INTO LabelReprintUsers (username, usertype) VALUES (@username, @usertype)";
                command.Parameters.AddWithValue("@username", user);
                command.Parameters.AddWithValue("@usertype", false);

                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
            }
            infoTextbox.Text += "User has been added: " + user + "." + Environment.NewLine;
            updateGrid();
        }


        private void updatePrinterBox()
        {
            removePrinterComboBox.Items.Clear();
            SqlDataAdapter da = new SqlDataAdapter();
            SqlConnection connection = new SqlConnection(MainWindow.fansconnectionString);
            da.SelectCommand = new SqlCommand(@"SELECT * FROM LabelReprintPrinters ORDER BY printer ASC", connection);
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            da.Fill(ds, "LabelReprintPrinters");
            dt = ds.Tables["LabelReprintPrinters"];
            
            for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
            {
                removePrinterComboBox.Items.Add(ds.Tables[0].Rows[i]["printer"]);
            }
        }

        private void addPrinterButton_Click(object sender, RoutedEventArgs e)
        {
            string printer = addPrinterTextBox.Text;
            if (printer == "")
            {
                return;
            }
            using (SqlConnection connection = new SqlConnection(MainWindow.fansconnectionString))
            using (SqlCommand command = connection.CreateCommand())
            {

                command.CommandText = "INSERT INTO LabelReprintPrinters (printer) VALUES (@printer)";
                command.Parameters.AddWithValue("@printer", printer);
                

                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
            }
            infoTextbox.Text += "Printer has been added: " + printer + "." + Environment.NewLine;
            infoTextbox.Text += "Please restart the program for changes to take effect." + Environment.NewLine;
        }

        private void removePrinterButton_Click(object sender, RoutedEventArgs e)
        {
            string printer = removePrinterComboBox.Text;
            using (SqlConnection connection = new SqlConnection(MainWindow.fansconnectionString))
            using (SqlCommand command = connection.CreateCommand())
            {

                command.CommandText = "DELETE FROM LabelReprintPrinters WHERE printer = '" + printer + "'";


                connection.Open();
                command.ExecuteNonQuery();
                connection.Close();
            }
            infoTextbox.Text += "Printer has been removed: " + printer + "." + Environment.NewLine;
            infoTextbox.Text += "Please restart the program for changes to take effect." + Environment.NewLine;
        }
    }
}
