using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BigAssLabelPrinting
{
    public partial class SpecLabelPage : Page
    {
        public SpecLabelPage()
        {
            InitializeComponent();
        }

        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.ERPSwitch.Value == 1)
            {
                MainWindow.Log("Spec labels do not support Vantage.");
            }
            else if (serialNumberTextBox.Text == "" && partNumberTextBox.Text == "")
            {
                MainWindow.Log("Please enter a serial or part number.");
            }
            else if (MainWindow.PrinterComboBox.Text == "")
            {
                MainWindow.Log("Please Select a printer");
            }
            else if (serialNumberTextBox.Text.Trim().ToLower() == MainWindow._BULKCOMMAND || partNumberTextBox.Text.Trim().ToLower() == MainWindow._BULKCOMMAND)
            {
                try
                {
                    string[] lines = File.ReadAllLines(MainWindow._BULKFILEPATH);

                    lines.ToList().ForEach(l =>
                    {
                        if (l.Trim().Length > 0)
                            SpecSearch(l);
                    });

                    File.WriteAllText(MainWindow._BULKFILEPATH, "");
                }
                catch (Exception ex)
                {
                    MainWindow.Log(ex.Message);
                }
            }
            else
            {
                SpecSearch();
            }
        }
 
        private MainWindow.SearchType GetSearchType()
        {
            MainWindow.SearchType result;

            if (partNumberTextBox.Text.Trim().Length > 0)
                result = MainWindow.SearchType.Part;
            else if (MainWindow.InventorySwitch.Value == 1)
                result = MainWindow.SearchType.WIP;
            else
                result = MainWindow.SearchType.Inventory;

            return result;
        }

        private void SpecSearch()
        {
            SpecSearch("");
        }

        private void SpecSearch(string serialOrPartNum)
        {
            string select = "";
            string selectInventory = @"
                select s.SerialNumber, p.PartNum, p.basModelNum_c, rtrim(ltrim(p.basSpecData_c)) as basSpecData_c, 
                    case when len(rtrim(ltrim(p.basSpecLabelOverride_c))) = 0 then rtrim(ltrim(c.basSpecLabel_c)) else rtrim(ltrim(p.basSpecLabelOverride_c)) end as basSpecLabel_c
                from SerialNo s
                join Part p on p.Company = s.Company and p.PartNum = s.PartNum
                join PartClass c on c.Company = p.Company and c.ClassID = p.ClassID 
                where s.Company = '{0}' and s.SerialNumber = '{1}'";
            string selectWIP = @"
                select s.Key1 as SerialNumber, p.PartNum, p.basModelNum_c, rtrim(ltrim(p.basSpecData_c)) as basSpecData_c, 
                    case when len(rtrim(ltrim(p.basSpecLabelOverride_c))) = 0 then rtrim(ltrim(c.basSpecLabel_c)) else rtrim(ltrim(p.basSpecLabelOverride_c)) end as basSpecLabel_c
                from Ice.UD110 s
                join Part p on p.Company = s.Company and p.PartNum = s.ShortChar02
                join PartClass c on c.Company = p.Company and c.ClassID = p.ClassID 
                where s.Company = '{0}' and s.Key1 = '{1}'";
            string selectPart = @"
                select 'Test-Serial-1234' as SerialNumber, p.PartNum, p.basModelNum_c, rtrim(ltrim(p.basSpecData_c)) as basSpecData_c, 
                    case when len(rtrim(ltrim(p.basSpecLabelOverride_c))) = 0 then rtrim(ltrim(c.basSpecLabel_c)) else rtrim(ltrim(p.basSpecLabelOverride_c)) end as basSpecLabel_c
                from Part p
                join PartClass c on c.Company = p.Company and c.ClassID = p.ClassID 
                where p.Company = '{0}' and p.PartNum = '{1}'";

            switch (GetSearchType())
            {
                case MainWindow.SearchType.Inventory:
                    if (serialOrPartNum.Length == 0)
                        serialOrPartNum = serialNumberTextBox.Text;

                    select = string.Format(selectInventory, MainWindow.CountryComboBox.Text, serialOrPartNum);

                    break;
                case MainWindow.SearchType.WIP:
                    if (serialOrPartNum.Length == 0)
                        serialOrPartNum = serialNumberTextBox.Text;

                    select = string.Format(selectWIP, MainWindow.CountryComboBox.Text, serialOrPartNum);

                    break;
                case MainWindow.SearchType.Part:
                    if (serialOrPartNum.Length == 0)
                        serialOrPartNum = partNumberTextBox.Text;

                    select = string.Format(selectPart, MainWindow.CountryComboBox.Text, serialOrPartNum);

                    break;
            }

            using (DataTable dt = new DataTable())
            {
                using (SqlDataAdapter a = new SqlDataAdapter(select, MainWindow.GetE10ConnectionString()))
                    a.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    MainWindow.Log("Serial number not found.");
                }
                else
                {
                    DataRow drSpec = dt.Select("basSpecData_c <> '' and basSpecLabel_c <> ''").FirstOrDefault();

                    if (drSpec == null)
                    {
                        MainWindow.Log("No spec printing data found.");
                    }
                    else
                    {
                        string specDataBase = drSpec.Field<string>("basSpecData_c");
                        string labelTypeBase = drSpec.Field<string>("basSpecLabel_c");

                        foreach (DataRow dr in dt.Rows)
                        {
                            string serialNum = dr.Field<string>("SerialNumber");
                            string partNum = dr.Field<string>("PartNum");
                            string modelNum = dr.Field<string>("basModelNum_c");
                            string specData = dr.Field<string>("basSpecData_c").Trim().Length == 0 ? specDataBase : dr.Field<string>("basSpecData_c");
                            string labelType = dr.Field<string>("basSpecLabel_c").Trim().Length == 0 ? labelTypeBase : dr.Field<string>("basSpecLabel_c");

                            addButton(serialNum + " " + partNum, string.Format($"{MainWindow.GetBartenderPrintString()}{{2}}|{{3}}|{{4}}|{{5}}|", labelType, MainWindow.PrinterComboBox.Text, serialNum, partNum, modelNum, specData), serialNum, "");
                        }
                    }
                }
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
            newbtn.LabelType = "SPEC";
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
    }
}