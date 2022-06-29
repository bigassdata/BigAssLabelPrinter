using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace BigAssLabelPrinting
{
    /// <summary>
    /// Interaction logic for DoePage.xaml
    /// </summary>
    public partial class DoePage : Page
    {
        private string DOElabel = "HKU-LBLD-13-ENG-01.btw";

        public DoePage()
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
            /*if (serialLotSwitch.Value == 0 && MainWindow.InventorySwitch.Value == 0 && MainWindow.ERPSwitch.Value == 0)
            {
                e10SerializedSearchInventory(serialnumber);
            }
            else if (serialLotSwitch.Value == 1 && MainWindow.InventorySwitch.Value == 0 && MainWindow.ERPSwitch.Value == 0)
            {
                e10LotSearchInventory(serialnumber);
            }
            else if (serialLotSwitch.Value == 0 && MainWindow.InventorySwitch.Value == 1 && MainWindow.ERPSwitch.Value == 0)
            {
                e10SerializedSearchWIP(serialnumber);
            }
            else if (serialLotSwitch.Value == 1 && MainWindow.InventorySwitch.Value == 1 && MainWindow.ERPSwitch.Value == 0)
            {
                e10LotSearchWIP(serialnumber);
            }
            else if (serialLotSwitch.Value == 0 && MainWindow.InventorySwitch.Value == 0 && MainWindow.ERPSwitch.Value == 1)
            {
                VantageSerializedSearchInventory(serialnumber);
            }
            else if (serialLotSwitch.Value == 1 && MainWindow.InventorySwitch.Value == 0 && MainWindow.ERPSwitch.Value == 1)
            {
                VantageLotSearchInventory(serialnumber);
            }
            else if (serialLotSwitch.Value == 0 && MainWindow.InventorySwitch.Value == 1 && MainWindow.ERPSwitch.Value == 1)
            {
                VantageSerializedSearchWIP(serialnumber);
            }
            else if (serialLotSwitch.Value == 1 && MainWindow.InventorySwitch.Value == 1 && MainWindow.ERPSwitch.Value == 1)
            {
                VantageLotSearchWIP(serialnumber);
            }*/
        }

        private void VantageLotSearchWIP(string serialnumber)
        {
            MainWindow.Log("There are no items in the Vantage database in WIP state.");
        }

        private void VantageSerializedSearchWIP(string serialnumber)
        {
            MainWindow.Log("There are no items in the Vantage database in WIP state.");
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
        }

        private void e10LotSearchWIP(string serialnumber)
        {
            throw new NotImplementedException();
        }

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


        private void e10SerializedSearchInventory(string serialnum)
        {
            string data, newdata;
            SqlConnection connection = new SqlConnection(MainWindow.E10connectionString);
            SqlDataAdapter da = new SqlDataAdapter();
            //da.SelectCommand = new SqlCommand(@"SELECT basDOEInfo_c FROM dbo.Part WHERE PartNum = 'S3127-A2-BC-04-02-C'", connection);

            da.SelectCommand = new SqlCommand(@"declare @serialnum varchar(max) set @serialnum = '" + serialnum + "' select p.basDOEInfo_c AS 'data' from serialno s INNER JOIN part p ON (p.partnum = s.partnum AND p.company = s.company) where s.serialnumber = @serialnum", connection);
            DataTable dt = new DataTable();
            da.Fill(dt);
            if (dt.Rows.Count < 1)
            {
                MainWindow.Log("Serial number not found.");
                return;
            }
            data = constructPrintCode(dt.Rows[0]["data"].ToString());
            addButton(serialnum.ToUpper(), data, serialnum);
            /*newdata = "Haiku ";
            int count = 0;
            string temp = dt.Rows[0]["data"].ToString();
            foreach (char c in dt.Rows[0]["data"].ToString())
            {
                if (c == '|') 
                {
                    temp = temp.Remove(6, newdata.Count()-1);
                    foreach (char ch in temp)
                    {
                        count += 1;
                        if (ch == '|')
                        {
                            temp = temp.Remove(0, count-1);
                            break;
                        }
                    }
                    break; 
                }
                newdata += c;
            }
            newdata += temp.Remove(temp.Count() - 1);
            data = constructPrintCode(newdata);
            //MainWindow.MainInfoTextBlock.Text = newdata;//dt.Rows[0]["data"].ToString();
            */
        }

        private void serialLotSwitch_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lotLabel.Foreground = MainWindow.buttonForeground;
            serialLabel.Foreground = MainWindow.buttonForeground;

            if (serialLotSwitch.Value == 1)
            {
                serialLotLabel.Content = "LOT NUMBER";
                lotLabel.Foreground = MainWindow.activeBackground;
                FadeLotGrid(true);
            }
            else
            {
                serialLotLabel.Content = "SERIAL NUMBER";
                serialLabel.Foreground = MainWindow.activeBackground;
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
            //\\E101-SQL\\Websites\\EpicorERP10\\Server\\reports
            //printcode = "%BTW% /AF=\"\\\\e10-us-app-1\\epicor10\\server\\reports\\TC On-Demand Labels\\" + DOElabel + "\" /D=%Trigger File Name% /PRN=\"" + printer + "\" /R=3 /P /DD \r\n%END% \r\n";
            printcode = "%BTW% /AF=\"\\\\erp-us-bartend\\Bartender\\Reports\\TC On-Demand Labels\\" + DOElabel + "\" /D=%Trigger File Name% /PRN=\"" + printer + "\" /R=3 /P /DD \r\n%END% \r\n";
            
            if (allLabelSwitch.Value == 1 && serialLotSwitch.Value == 1)
            {
                int amount = Convert.ToInt32(howManyTextBox.Text);
                for (int i = 0; i < amount; i++)
                {
                    printcode += code + (i + 1).ToString() + "|" + amount.ToString() + "|REPRINT|\r\n";
                }
            }
            else //if (allLabelSwitch.Value == 0 && serialLotSwitch.Value == 1)
            {
                printcode += code + labelOne.Text + "|" + labelTwo.Text + "|REPRINT|\r\n";
            }

            return printcode;
        }
    }
}
