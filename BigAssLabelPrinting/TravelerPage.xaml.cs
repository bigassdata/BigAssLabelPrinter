using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace BigAssLabelPrinting
{
    /// <summary>
    /// Interaction logic for TravelerPage.xaml
    /// </summary>
    public partial class TravelerPage : Page
    {
        private string HaikuTravelerlabel = "Baf_OperationTraveler-haiku.btw";
        private string LightsTravelerLabel = "Baf_OperationTraveler.btw";
        private string SMTTravelerlabel = "BAF_OP_PCBA.btw";
        public string testgroup = "60SM-15236-023-00|K3150-S0-PB-04-02-C|B64|FH-00208512|Haiku 60, Short Mount Ceiling Fan (<125W), Black, (US/CAN/MX), with SenseMe (WIFI, LED), (U), 0.019HP, 100-125/200-240 VAC, Single Phase|20F85EAE4C54|";

        public TravelerPage()
        {
            InitializeComponent();
            FadeLotGrid(false);
        }
        private void searchButton_Click(object sender, RoutedEventArgs e)
        {

            if (serialNumberTextBox.Text == "")
            {
                MessageBox.Show("Please enter a serial number");
                return;
            }
            if (MainWindow.PrinterComboBox.Text == "")
            {
                MessageBox.Show("Please Select a printer");
                return;
            }
            string serialnumber = serialNumberTextBox.Text;
            e10SerializedSearchInventory(serialnumber);
        }

        private void e10SerializedSearchInventory(string serialnum)
        {
            //YOU ARE WORKING HERE ! ! ! ! ! ! ! ! !
            string data, newdata;
            SqlConnection connection = new SqlConnection(MainWindow.E10connectionString);
            SqlDataAdapter da = new SqlDataAdapter();
            try
            {
                if (MainWindow.InventorySwitch.Value == 0)
                {
                    da.SelectCommand = new SqlCommand(@"DECLARE @serialnum varchar(40) set @serialnum = '" + serialnum + "' SELECT s.SerialNumber + '|' + s.partnum + '|' + p.basPickCode_c + '|' + s.JobNum + '|' + p.PartDescription + '|' + i.ChildKey2 +'|' AS 'data' from serialno s INNER JOIN part p ON (p.partnum = s.partnum AND p.company = s.company) INNER JOIN [Ice].UD110A i ON (s.SerialNumber = i.Key1) where s.serialnumber = @serialnum AND ChildKey3=0", connection);
                }
                else
                {
                    da.SelectCommand = new SqlCommand(@"DECLARE @serialnum varchar(max) set @serialnum = '" + serialnum + "' SELECT TOP 1 i.Key1 + '|' + p.PartNum + '|' + p.basPickCode_c + '|' + i.Key2 + '|' + p.PartDescription + '|' + i.Key2 +'|' AS 'data' from [Ice].UD110 i INNER JOIN Part p ON(p.Company = i.Company AND p.PartNum = i.ShortChar02) where i.Key1 = @serialnum", connection);
                }

                if (SMTButton.Foreground == MainWindow.activeBackground)
                {
                    da.SelectCommand = new SqlCommand(@"DECLARE @serialnum varchar(max) set @serialnum = '" + serialnum + "' SELECT Top 1 u.ChildKey2 + '||||||' AS 'data' from [ice].UD110A u where u.ChildKey2 = @serialnum", connection);
                }
                DataTable dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count < 1)
                {
                    MainWindow.Log("Serial number not found.");
                    return;
                }

                newdata = dt.Rows[0]["data"].ToString();
                data = constructPrintCode(dt.Rows[0]["data"].ToString());
                //data = constructPrintCode(testgroup);
                addButton(serialnum.ToUpper(), data, serialnum);
                //MainWindow.MainInfoTextBlock.Text = data;//dt.Rows[0]["data"].ToString();
            }
            catch
            {
                MainWindow.Log("Error finding Serial.  Try flipping the Inventoy / WIP switch.");
            }
        }
        
        private void VantageLotSearchWIP(string serialnumber)
        {
            MainWindow.Log("There are no items in the Vantage database in WIP state.");
        }

        private void VantageSerializedSearchWIP(string serialnumber)
        {
            MainWindow.Log("There are no items in the Vantage database in WIP state.");
        }
        
        private void e10LotSearchWIP(string serialnumber)
        {
            throw new NotImplementedException();
        }

        
        private void e10LotSearchInventory(string serialnumber)
        {
            string jobnum = serialnumber;
            SqlConnection connection = new SqlConnection(MainWindow.E10connectionString);
            SqlDataAdapter da = new SqlDataAdapter();
            if (IsNotTracked(jobnum))
            {
                da.SelectCommand = new SqlCommand(@"declare @jobnum varchar(max) set @jobnum = '" + jobnum + "' select j.partnum+'|'+convert(varchar(max),j.partdescription)+'|'+'|J|" + jobnum + "|'+ (select p.basPickCode_c from part p where p.partnum = j.partnum)+'|'+ '|' AS 'data' from jobhead j where j.jobnum = @jobnum", connection);
            }
            else
            {
                da.SelectCommand = new SqlCommand(@"declare @jobnum varchar(max) set @jobnum = '" + jobnum + "' select j.partnum+'|'+convert(varchar(max),j.partdescription)+'|'+j.jobnum+'|L||'+ (select p.basPickCode_c from part p where p.partnum = j.partnum)+'|'+ '|' AS 'data' from jobhead j where j.jobnum = @jobnum", connection);
            }

            DataTable dt = new DataTable();
            da.Fill(dt);
            string data = "";
            if (dt.Rows.Count < 1)
            {
                MainWindow.Log("Serial number not found.");
                return;
            }
            data = constructPrintCode(dt.Rows[0]["data"].ToString());


            addButton(serialnumber.ToUpper() + " - " + "LOT", data, "", serialnumber);
        }


        private void HaikuLightSwitch_Click(object sender, RoutedEventArgs e)
        {
            LightButton.Foreground = MainWindow.buttonForeground;
            HaikuButton.Foreground = MainWindow.buttonForeground;
            SMTButton.Foreground = MainWindow.buttonForeground;

            if (sender.ToString() == LightButton.ToString())
            {
                LightButton.Foreground = MainWindow.activeBackground;
                FadeLotGrid(true);
            }
            if (sender.ToString() == HaikuButton.ToString())
            {
                HaikuButton.Foreground = MainWindow.activeBackground;
                FadeLotGrid(false);
            }
            if (sender.ToString() == SMTButton.ToString())
            {
                SMTButton.Foreground = MainWindow.activeBackground;
                FadeLotGrid(false);
            }
        }

        private void FadeLotGrid(bool visible, int interval = 2)
        {
            double opacity = (visible == true) ? 1.0 : 0.2;
            DoubleAnimation ani = new DoubleAnimation(opacity, TimeSpan.FromSeconds(interval));
            LotGrid.BeginAnimation(Grid.OpacityProperty, ani);
            LotGrid.IsEnabled = visible;
        }

        private void allLabelSwitch_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            allOff.Foreground = MainWindow.buttonForeground;
            allOn.Foreground = MainWindow.buttonForeground;
            singleLabels.Foreground = MainWindow.buttonForeground;
            allLabels.Foreground = MainWindow.buttonForeground;
            labelOne.IsEnabled = true;
            howManyTextBox.IsEnabled = true;
            labelTwo.IsEnabled = true;

            if (allLabelSwitch.Value == 1)
            {
                labelOne.Text = "";
                allOn.Foreground = MainWindow.activeBackground;
                allLabels.Foreground = MainWindow.activeBackground;
                labelOne.IsEnabled = false;
                labelTwo.IsEnabled = false;

            }
            else
            {
                singleLabels.Foreground = MainWindow.activeBackground;
                allOff.Foreground = MainWindow.activeBackground;
                howManyTextBox.IsEnabled = false;
            }
        }



        private void addButton(string ButtonData, string LabelData, string serialnumber = "", string lotnumber = "")
        {
            LabelButton newbtn = new LabelButton();
            newbtn.Width = 1162;
            newbtn.Height = 40;
            newbtn.Foreground = MainWindow.buttonForeground;
            newbtn.Background = MainWindow.buttonBackground;
            newbtn.Content = "Printer: " + MainWindow.PrinterComboBox.Text + "  SN:  " + ButtonData;
            newbtn.LabelType = "FINISHED GOODS";
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

        private bool IsNotTracked(string jobnum)
        {
            SqlConnection connection = new SqlConnection(MainWindow.E10connectionString);
            SqlDataAdapter da = new SqlDataAdapter();
            da.SelectCommand = new SqlCommand(@"SELECT tracklots, trackserialnum FROM part WHERE partnum = (SELECT partnum FROM jobhead WHERE jobnum = '" + jobnum + "')", connection);
            DataTable dt = new DataTable();
            da.Fill(dt);
            try
            {
                if (Convert.ToInt32(dt.Rows[0]["tracklots"]) == 0 && Convert.ToInt32(dt.Rows[0]["trackserialnum"]) == 0)
                {
                    return true;
                }
            }
            catch
            {
                return true;
            }

            return false;
        }

        private string constructPrintCode(string code, int startlabel = 1, int endlabel = 1)
        {
            string printer = MainWindow.PrinterComboBox.Text;
            string printcode;
            string switchit = HaikuTravelerlabel;

            if (HaikuButton.Foreground == MainWindow.activeBackground)
            {
                switchit = HaikuTravelerlabel;
            }
            if (LightButton.Foreground == MainWindow.activeBackground)
            {
                switchit = LightsTravelerLabel;
            }
            if (SMTButton.Foreground == MainWindow.activeBackground)
            {
                switchit = SMTTravelerlabel;
            }

                printcode = "%BTW% /AF=\"\\\\erp-us-bartend\\Bartender\\Reports\\" + switchit + "\" /D=%Trigger File Name% /PRN=\"" + printer + "\" /R=3 /P /DD \r\n%END% \r\n";
            printcode += code + "1|1|REPRINT|\r\n";
            //printcode += "60SM-15236-023-00|K3150-S0-PB-04-02-C|B64|FH-00208512|Haiku 60, Short Mount Ceiling Fan (<125W), Black, (US/CAN/MX), with SenseMe (WIFI, LED), (U), 0.019HP, 100-125/200-240 VAC, Single Phase|20F85EAE4C54|" + "1|1|REPRINT|\r\n";
            return printcode;
        }
        /*
        private void e10SerializedSearchWIP(string serialnumber)
        {
            string data;
            SqlConnection connection = new SqlConnection(MainWindow.E10connectionString);
            SqlDataAdapter da = new SqlDataAdapter();
            da.SelectCommand = new SqlCommand(@"declare @serialnum varchar(max) set @serialnum = '" + serialnumber + "'select s.shortchar02+'|'+convert(varchar(max),p.partdescription)+'|'+s.key1+'|S||'+ (select p.basPickCode_c from part p where p.partnum = s.shortchar02)+'|'+ p.UPCCode1 +'|' AS 'data' from [Ice].UD110 s INNER JOIN part p ON (p.partnum = s.shortchar02) where s.key1 = @serialnum"
            , connection);
            DataTable dt = new DataTable();
            da.Fill(dt);
            if (dt.Rows.Count < 1)
            {
                MainWindow.Log("Serial number not found.");
                return;
            }
            data = constructPrintCode(dt.Rows[0]["data"].ToString());
            addButton(serialnumber.ToUpper() + " - WIP", data, serialnumber);
        }
        
        private void VantageSerializedSearchInventory(string serialnumber)
        {
            string data;
            SqlConnection connection = new SqlConnection(MainWindow.VantageconnectionString);
            SqlDataAdapter da = new SqlDataAdapter();
            da.SelectCommand = new SqlCommand(@"declare @serialnum varchar(max) set @serialnum = '" + serialnumber + "' select snstatus, s.partnum+'|'+convert(varchar(max),p.partdescription)+'|'+s.serialnumber+'|S||'+(select p.shortchar02 from part p where p.partnum = s.partnum)+'|'+'|' AS 'data' from serialno s INNER JOIN part p ON (p.partnum = s.partnum AND p.company = s.company) where s.serialnumber = @serialnum", connection);
            DataTable dt = new DataTable();
            da.Fill(dt);
            if (dt.Rows.Count < 1)
            {
                MainWindow.Log("Serial number not found.");
                return;
            }
            data = constructPrintCode(dt.Rows[0]["data"].ToString());
            addButton(serialnumber.ToUpper() + " - " + dt.Rows[0]["snstatus"].ToString(), data, serialnumber);
            MainWindow.MainInfoTextBlock.Text = data;
        } 
         
        private void VantageLotSearchInventory(string serialnumber)
        {
            string jobnum = serialnumber;
            SqlConnection connection = new SqlConnection(MainWindow.VantageconnectionString);
            SqlDataAdapter da = new SqlDataAdapter();
            if (IsNotTracked(jobnum))
            {
                da.SelectCommand = new SqlCommand(@"declare @jobnum varchar(max) set @jobnum = '" + jobnum + "' select j.partnum+'|'+convert(varchar(max),j.partdescription)+'|'+'|J|" + jobnum + "|'+ (select p.shortchar02 from part p where p.partnum = j.partnum)+'|'+ '|' AS 'data' from jobhead j where j.jobnum = @jobnum", connection);
            }
            else
            {
                da.SelectCommand = new SqlCommand(@"declare @jobnum varchar(max) set @jobnum = '" + jobnum + "' select j.partnum+'|'+convert(varchar(max),j.partdescription)+'|'+j.jobnum+'|L||'+ (select p.shortchar02 from part p where p.partnum = j.partnum)+'|'+ '|' AS 'data' from jobhead j where j.jobnum = @jobnum", connection);
            }

            DataTable dt = new DataTable();
            da.Fill(dt);
            string data = "";
            if (dt.Rows.Count < 1)
            {
                MainWindow.Log("Serial number not found.");
                return;
            }
            data = constructPrintCode(dt.Rows[0]["data"].ToString());


            addButton(serialnumber.ToUpper() + " - " + "LOT", data, "", serialnumber);
        }
         */
    }
}
