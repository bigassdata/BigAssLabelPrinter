using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace BigAssLabelPrinting
{
    /// <summary>
    /// Interaction logic for TwinLightPage.xaml
    /// </summary>
    public partial class TwinLightPage : Page
    {
        public TwinLightPage()
        {
            InitializeComponent();
        }

        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            if (kitpartTextBox.Text == "" || fixturepartTextBox.Text == "" || interconnectionpartTextBox.Text == "" || jobnumberTextBox.Text == "")
            {
                MainWindow.Log("Please enter all required information.");
                return;
            }
            if (MainWindow.PrinterComboBox.Text == "")
            {
                MainWindow.Log("Please select a printer");
                return;
            }

            string kitnum = kitpartTextBox.Text;
            string fixture = fixturepartTextBox.Text;
            string interconnection = interconnectionpartTextBox.Text;
            string jobnum = jobnumberTextBox.Text;
            string shortkit = kitnum.Substring(0, 17);
            int quantity = Convert.ToInt32(quantityTextBox.Text);

            string interconnectiondesc = getPartDescription(interconnection);
            //reprintcodeTextBox.Text = shortkit.Substring(9, 2) + "     " + shortkit.Substring(13, 2);
            string speclabelinfo = @"%BTW% /AF=""\\MFG-DB\Epicor\MfgSys803\Server\reports\TC On-Demand Labels\TC-004356-HBL.btw"" /D=%Trigger File Name% /PRN=""ControlsTwinSpec"" /R=3 /P /DD
              %END% " + Environment.NewLine;
            if (shortkit.Substring(9, 2) == "52" && shortkit.Substring(13, 2) == "01")
            {
                for (int i = 1; i < quantity; i++)
                {
                    speclabelinfo += "|" + kitnum + "|" + shortkit + "|4.30/1.92 A|120/277 VAC|60 Hz|512/502 W|NO|NO|YES|NO|" + fixture + "|"+ Environment.NewLine;
                }
            }
            else if (shortkit.Substring(9, 2) == "52" && shortkit.Substring(13, 2) == "02")
            {
                for (int i = 1; i < quantity; i++)
                {
                    speclabelinfo += "|" + kitnum + "|" + shortkit + "|1.44/1.10 A|347/480 VAC|60 Hz|490/494 W|NO|NO|YES|NO|" + fixture + "|"+ Environment.NewLine ;
                }
            }
            else if (shortkit.Substring(9, 2) == "40" && shortkit.Substring(13, 2) == "01")
            {
                for (int i = 1; i < quantity; i++)
                {
                    speclabelinfo += "|" + kitnum + "|" + shortkit + "|3.36/1.50 A|120/277 VAC|60 Hz|404/392 W|NO|NO|YES|NO|" + fixture + "|" + Environment.NewLine;
                }
            }
            else if (shortkit.Substring(9, 2) == "40" && shortkit.Substring(13, 2) == "02")
            {
                for (int i = 1; i < quantity; i++)
                {
                    speclabelinfo += "|" + kitnum + "|" + shortkit + "|1.18/0.92 A|347/480 VAC|60 Hz|396/398 W|NO|NO|YES|NO|" + fixture + "|" + Environment.NewLine;
                }
            }

            addButton(kitnum + " - " + jobnum + " - " + "Spec Label", speclabelinfo);

            string FGlabel = @"%BTW% /AF=""\\MFG-DB\Epicor\MfgSys803\Server\reports\BAF_Retail-twin.btw"" /D=%Trigger File Name% /PRN=""Controls"" /R=3 /P /DD
              %END%" + Environment.NewLine + interconnection + "|" + interconnectiondesc + "|" + jobnum + "|J||||1|1||" + kitnum + "|FOR USE WITH " + fixture+ Environment.NewLine;
            for (int i = 1; i < quantity; i++)
            {
                FGlabel += interconnection + "|" + interconnectiondesc + "|" + jobnum + "|J||||1|1||" + kitnum + "|FOR USE WITH " + fixture + Environment.NewLine;
            }

            addButton(kitnum + " - " + jobnum + " - " + "Finished Goods Label", FGlabel);
        }



        private void addButton(string ButtonData, string LabelData, string serialnumber = "", string lotnumber = "")
        {
            LabelButton newbtn = new LabelButton();
            newbtn.Width = 1162;
            newbtn.Height = 40;
            newbtn.Foreground = MainWindow.buttonForeground;
            newbtn.Background = MainWindow.buttonBackground;
            newbtn.Content = "Printer: " + MainWindow.PrinterComboBox.Text + "  SN:  " + ButtonData;
            newbtn.LabelType = "TWIN";
            newbtn.Printer = MainWindow.PrinterComboBox.Text;
            newbtn.SerialNumber = serialnumber;
            newbtn.LotNumber = lotnumber;

            newbtn.SetLabelInfo(LabelData);
            testGrid.Children.Add(newbtn);
            newbtn.Click += newbtn_Click;

            LabelButton rembtn = new LabelButton();
            rembtn.Width = 40;
            rembtn.Height = 40;
            rembtn.Foreground = MainWindow.remButtonColor;
            rembtn.Background = MainWindow.buttonBackground;
            rembtn.Content = " X ";
            rembtn.LabelType = "FINISHED GOODS";
            rembtn.Printer = MainWindow.PrinterComboBox.Text;
            rembtn.SerialNumber = serialnumber;
            rembtn.LotNumber = lotnumber;
            rembtn.SetLabelInfo(LabelData);
            remGrid.Children.Add(rembtn);
            rembtn.Click += rembtn_Click;
        }

        private void rembtn_Click(object sender, RoutedEventArgs e)
        {
            LabelButton btn = (sender as LabelButton);
            string info = btn.getInfo();
            int i = 0;
            foreach (LabelButton but in remGrid.Children)
            {
                if (but.getInfo() == info)
                {
                    testGrid.Children.RemoveAt(i);
                    break;
                }
                i += 1;
            }
            remGrid.Children.Remove((sender as LabelButton));
        }

        private void newbtn_Click(object sender, RoutedEventArgs e)
        {
            LabelButton btn = (sender as LabelButton);
            string info = btn.getInfo();
            MainWindow.SendToBartender(info, btn.SerialNumber, btn.Printer, btn.LabelType, btn.LotNumber);
            int i = 0;
            foreach (LabelButton but in testGrid.Children)
            {
                if (but.getInfo() == info)
                {
                    remGrid.Children.RemoveAt(i);
                    break;
                }
                i += 1;
            }
            testGrid.Children.Remove((sender as LabelButton));
        }

        private string getPartDescription(string partnum)
        {
            SqlConnection connection = new SqlConnection(MainWindow.E10connectionString);
            SqlDataAdapter da = new SqlDataAdapter();
            da.SelectCommand = new SqlCommand(@"SELECT partdescription FROM Part WHERE partnum = '" + partnum + "'", connection);
            DataTable dt = new DataTable();
            da.Fill(dt);
            string desc = dt.Rows[0]["partdescription"].ToString();
            return desc;
        }
    }
}
