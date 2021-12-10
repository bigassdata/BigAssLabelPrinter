using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace BigAssLabelPrinting
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public enum SearchType { Inventory, WIP, Part }

        public const string _BULKFILEPATH = "C:\\BulkPrint.txt";
        public const string _BULKCOMMAND = "bulk print";

        public static User CurrentUser;
        private static LoginPage loginpage = new LoginPage();
        private static FinishedGoodsPage finishedgoodspage = new FinishedGoodsPage();
        private static SpecLabelPage speclabelpage = new SpecLabelPage();
        private static PrintingHistoryPage printinghistorypage = new PrintingHistoryPage();
        private static CustomLabelPage customlabelpage = new CustomLabelPage();
        private static UserManagementPage usermanagementpage = new UserManagementPage();
        private static TwinLightPage twinlightpage = new TwinLightPage();
        private static DoePage doepage = new DoePage();
        private static TravelerPage travelerpage = new TravelerPage();

        public static Slider InventorySwitch;
        //public static Slider USMY_Switch;
        public static Slider ERPSwitch;
        public static TextBlock MainInfoTextBlock;

        public static string fansconnectionString = "server=mfg-db\\Vantage;database=bigassfans;uid=WebService;password=MKhZx0_N";
        public static string E10connectionString = "server=e101-SQL;database=Epicor10;uid=WebService;password=MKhZx0_N";
        public static string VantageconnectionString = "server=mfg-db\\Vantage;database=MfgSys803;uid=WebService;password=MKhZx0_N";
        public static string MYE10connectionString = "server=e10-my-SQL;database=EpicorERP10;uid=WebService;password=MKhZx0_N";
        public static string MYfansconnectionString = "server=mfg-db\\Vantage;database=bigassfans;uid=WebService;password=MKhZx0_N";
        
        public static string basLocation;
        public static string ActiveConnectionString = E10connectionString;

        private static Frame PrimaryFrame;
        private static Grid UpperGrid;
        public static Brush buttonBackground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF1B1B1C"));
        public static Brush activeBackground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFFFC60A"));
        public static Brush activeForeground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF020202"));
        public static Brush buttonForeground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFFBFBFB"));
        public static Brush remButtonColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFFF0000"));
        private static Button FGbutton;
        public static ComboBox PrinterComboBox;
        public static ComboBox CountryComboBox;
        public static Storyboard BlinkAnimation;

        private bool _loading = false;

        public MainWindow()
        {
            InitializeComponent();
            PrimaryFrame = primaryframe;
            UpperGrid = uppergrid;
            PrimaryFrame.Navigate(loginpage);
            FGbutton = finishedGoodsButton;
            PrinterComboBox = printerComboBox;
            CountryComboBox = countryComboBox;
            e10Label.Foreground = activeBackground;
            invLabel.Foreground = activeBackground;
            Country.Foreground = activeBackground;
            printerLabel.Foreground = activeBackground;            
            //USLabel.Foreground = activeBackground;
            changeButtonVisibility(false, 0);
            ERPSwitch = erpSwitch;
            InventorySwitch = invSwitch;
            //USMY_Switch = USMYSwitch;
            MainInfoTextBlock = maininfobox;
            BlinkAnimation = TryFindResource("blinkAnimation") as Storyboard;
            MainInfoTextBlock.Background = buttonBackground;
            MainInfoTextBlock.Foreground = activeBackground;
        }

        public static void Log(string Data)
        {
            MainInfoTextBlock.Text = DateTime.Now + " : " + Data + Environment.NewLine + MainInfoTextBlock.Text;
            //BlinkAnimation.Begin();
        }

        private static void changeButtonVisibility(bool visible, int interval = 2)
        {
            double opacity = (visible == true) ? 1.0 : 0.2;
            DoubleAnimation ani = new DoubleAnimation(opacity, TimeSpan.FromSeconds(interval));
            UpperGrid.BeginAnimation(Grid.OpacityProperty, ani);
        }

        public static void UserVerified()
        {
            PrimaryFrame.Navigate(finishedgoodspage);
            changeButtonVisibility(true);
            FGbutton.Background = activeBackground;
            FGbutton.Foreground = activeForeground;
        }

        private void UpperButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentUser == null)
            {
                return;
            }
            if (!CurrentUser.Verified)
            {
                return;
            }
            finishedGoodsButton.Background = buttonBackground;
            specLabelButton.Background = buttonBackground;
            twinLightButton.Background = buttonBackground;
            printingHistoryButton.Background = buttonBackground;
            customButton.Background = buttonBackground;
            doeButton.Background = buttonBackground;
            travelerButton.Background = buttonBackground;
            userManagementButton.Background = buttonBackground;
            finishedGoodsButton.Foreground = buttonForeground;
            specLabelButton.Foreground = buttonForeground;
            twinLightButton.Foreground = buttonForeground;
            printingHistoryButton.Foreground = buttonForeground;
            customButton.Foreground = buttonForeground;
            userManagementButton.Foreground = buttonForeground;
            doeButton.Foreground = buttonForeground;
            travelerButton.Foreground = buttonForeground;
            (sender as Button).Foreground = activeForeground;
            (sender as Button).Background = activeBackground;
            if (sender == finishedGoodsButton)
            {
                PrimaryFrame.Navigate(finishedgoodspage);
            }
            else if (sender == specLabelButton)
            {
                PrimaryFrame.Navigate(speclabelpage);
            }
            else if (sender == twinLightButton)
            {
                PrimaryFrame.Navigate(twinlightpage);
            }
            else if (sender == printingHistoryButton)
            {
                PrimaryFrame.Navigate(printinghistorypage);
            }
            else if (sender == customButton)
            {
                PrimaryFrame.Navigate(customlabelpage);
            }
            else if (sender == userManagementButton)
            {
                if(!CurrentUser.IsAdmin())
                {
                    finishedGoodsButton.Foreground = activeForeground;
                    finishedGoodsButton.Background = activeBackground;
                    (sender as Button).Foreground = buttonForeground;
                    (sender as Button).Background = buttonBackground;
                    PrimaryFrame.Navigate(finishedgoodspage);
                    MessageBox.Show("You are not authorized to manage user accounts.");
                    return;
                }
                PrimaryFrame.Navigate(usermanagementpage);
            }
            else if (sender == doeButton)
            {
                PrimaryFrame.Navigate(doepage);
            }
            else if (sender == travelerButton)
            {
                if (!CurrentUser.IsAdmin())
                {
                    /*
                    finishedGoodsButton.Foreground = activeForeground;
                    finishedGoodsButton.Background = activeBackground;
                    (sender as Button).Foreground = buttonForeground;
                    (sender as Button).Background = buttonBackground;
                    PrimaryFrame.Navigate(finishedgoodspage);
                    MessageBox.Show("You are not authorized to manage user accounts.");
                    return;
                    */
                }
                PrimaryFrame.Navigate(travelerpage);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _loading = true;

            FillPrinterBox();
            FillCountryBox();

            if (Properties.Settings.Default.LastPrinter.Length > 0 && printerComboBox.Items.Contains(Properties.Settings.Default.LastPrinter))
                printerComboBox.SelectedItem = Properties.Settings.Default.LastPrinter;

            if (Properties.Settings.Default.LastCompany.Length > 0 && countryComboBox.Items.Contains(Properties.Settings.Default.LastCompany))
                countryComboBox.SelectedItem = Properties.Settings.Default.LastCompany;

            _loading = false;
        }

        public static void SendToBartender(string data, string serialnum = "", string printer = "", string labeltype = "CUSTOM LABEL", string lotnum = "")
        {
            //string savepath = @"\\e10-us-app-1\EpicorData\Bartender";
            string savepath = @"\\e101-agent-1.bigassfan.local\EpicorData\Bartender"; //Live Folder
            //string savepath = @"\\e101-agent-1.bigassfan.local\EpicorData\"; // Test Folder

            string country = MainWindow.CountryComboBox.Text;
            if (country == "MY") { savepath = @"\\e10-my-app-1\EpicorData\Bartender"; data = data + "*"; }

            string thisDate = System.DateTime.Now.ToLongTimeString();
            thisDate = thisDate.Replace("/", "-").Replace(":", "_").Replace(" ","");
            string sender = savepath + @"\" + thisDate + "_reprint.txt";
            using (StreamWriter outfile = new StreamWriter(sender, true, Encoding.UTF8))
            {
                outfile.Write(data);
            }
            string queryString = @"INSERT INTO LabelReprintLogs (username, date, data, printer, labeltype, serialnum, lotnum) VALUES (@username, @date, @data, @printer, @labeltype, @serialnum, @lotnum)";
            using (SqlConnection connection = new SqlConnection(MainWindow.fansconnectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Parameters.AddWithValue("@username", CurrentUser.Username);
                command.Parameters.AddWithValue("@date", DateTime.Now);
                command.Parameters.AddWithValue("@data", data);
                command.Parameters.AddWithValue("@printer", printer);
                command.Parameters.AddWithValue("@labeltype", labeltype);
                command.Parameters.AddWithValue("@serialnum", serialnum);
                command.Parameters.AddWithValue("@lotnum", lotnum);
                //command.Connection.Open();
                //command.ExecuteNonQuery(); // Server where history is kept is down.
            }
        }

        private static void FillPrinterBox()
        {
            try
            {
                using (DataTable dt = new DataTable())
                {
                    using (SqlDataAdapter a = new SqlDataAdapter("SELECT CodeDesc FROM UDCodes WHERE CodeTypeID like 'LBL%' Group By CodeDesc", MainWindow.E10connectionString))
                        a.Fill(dt);
                    try
                    {
                        //using (SqlDataAdapter a = new SqlDataAdapter("SELECT CodeDesc FROM UDCodes WHERE CodeTypeID like 'LBL%' Group By CodeDesc", MainWindow.MYE10connectionString))
                            //a.Fill(dt);
                    }
                    catch (Exception e)
                    {
                        //MainInfoTextBlock.Text = "Error Connecting to MY server.";
                    }
                    DataView dv = new DataView(dt);
                    dv.Sort = "CodeDesc";
                    using (DataTable dt2 = dv.ToTable())
                    {
                        dv.Sort = "CodeDesc";
                        PrinterComboBox.ItemsSource = dt2.AsEnumerable().Select(r => r.Field<string>("CodeDesc"));
                    }
                }
            }
            catch (Exception e)
            {
                MainInfoTextBlock.Text = e.ToString();
            }
        }

        private static void FillCountryBox()
        {
            try
            {
                using (DataTable dt = new DataTable())
                {
                    using (SqlDataAdapter a = new SqlDataAdapter("select Company from Erp.Company where Company <> 'MY' order by Company", MainWindow.E10connectionString))
                        a.Fill(dt);

                    CountryComboBox.ItemsSource = dt.AsEnumerable().Select(r => r.Field<string>("Company"));
                }
            }
            catch (Exception e)
            {
                MainInfoTextBlock.Text = e.ToString();
            }
        }

        public static string GetE10ConnectionString()
        {
            return CountryComboBox.Text == "MY" ? MYE10connectionString : E10connectionString;
        }

        public static string GetBartenderPrintString()
        {
            string printStringElse = "%BTW% /AF=\"\\\\E101-SQL\\Websites\\EpicorERP10\\Server\\reports\\{0}\" /D=%Trigger File Name% /PRN=\"{1}\" /R=3 /P /DD \r\n%END% \r\n";
            string printStringMY = "%BTW% /AF=\"\\\\e10-my-app-1\\Epicor10\\Server\\reports\\{0}\" /D=%Trigger File Name% /PRN=\"{1}\" /R=3 /P /DD \r\n%END% \r\n";

            return CountryComboBox.Text == "MY" ? printStringMY : printStringElse;
        }

        private void erpSwitch_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            e10Label.Foreground = buttonForeground;
            vantageLabel.Foreground = buttonForeground;
            if (erpSwitch.Value == 1)
            {
                vantageLabel.Foreground = activeBackground;
            }
            else
            {
                e10Label.Foreground = activeBackground;
            }
        }

        private void invSwitch_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            invLabel.Foreground = buttonForeground;
            wipLabel.Foreground = buttonForeground;
            if (invSwitch.Value == 1)
            {
                wipLabel.Foreground = activeBackground;
            }
            else
            {
                invLabel.Foreground = activeBackground;
            }
        }

        private void printerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_loading)
            {
                ComboBox printer = (ComboBox)sender;

                Properties.Settings.Default.LastPrinter = printer.SelectedItem.ToString();
                Properties.Settings.Default.Save();
            }
        }

        private void countryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_loading)
            {
                ComboBox company = (ComboBox)sender;

                Properties.Settings.Default.LastCompany = company.SelectedItem.ToString();
                Properties.Settings.Default.Save();
            }
        }

        /*private void USMYSwitch_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            USLabel.Foreground = buttonForeground;
            MYLabel.Foreground = buttonForeground;
            if (USMYSwitch.Value == 1)
            {
                MYLabel.Foreground = activeBackground;
            }
            else
            {
                USLabel.Foreground = activeBackground;
            }
        }*/

    }
}
