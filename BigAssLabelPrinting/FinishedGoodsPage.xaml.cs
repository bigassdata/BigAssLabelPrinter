using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace BigAssLabelPrinting
{
    public partial class FinishedGoodsPage : Page
    {
        private enum SearchType { SerialNumber, Part, Lot }

        private const string DEFAULT_LABEL = "BAF_Retail.btw";
        private const string DEFAULT_LABEL_MY = "MY_BoxLabel.btw";

        public FinishedGoodsPage()
        {
            InitializeComponent();
            FadeLotGrid(false);
        }

        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            if (serialNumberTextBox.Text == "" && partNumberTextBox.Text == "")
            {
                MainWindow.Log("Please enter a serial or part number.");
            }
            else if (MainWindow.PrinterComboBox.Text == "")
            {
                MessageBox.Show("Please Select a printer");
            }
            else if (serialNumberTextBox.Text.Trim().ToLower() == MainWindow._BULKCOMMAND || partNumberTextBox.Text.Trim().ToLower() == MainWindow._BULKCOMMAND)
            {
                try
                {
                    string[] lines = File.ReadAllLines(MainWindow._BULKFILEPATH);

                    lines.ToList().ForEach(l => 
                    {
                        if (l.Trim().Length > 0)
                            FinishedGoodsSearch(l);
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
                FinishedGoodsSearch();
            }
        }

        private void FinishedGoodsSearch()
        {
            FinishedGoodsSearch("");
        }

        private void FinishedGoodsSearch(string serialPartLotNum)
        {
            SearchType searchType = SearchType.Part;
            string select = "";
            string selectSerialInventory = @"
                select s.SerialNumber, s.SNStatus, s.PartNum, p.PartDescription, p.UPCCode1, isnull(c.[Description], '') as Country, isnull(p.basPickCode_c, '') as basPickCode_c, p.basFGLabelOverride_c, 
                    isnull(pc.basFGLabel_c, '') as basFGLabel_c, p.LegalLabel_c, p.basdesctranslation_c
                from SerialNo s
                join Part p on p.Company = s.Company and p.PartNum = s.PartNum
                left join PartClass pc on pc.Company = p.Company and pc.ClassID = p.ClassID
                left join Erp.Country c on c.Company = p.Company and c.CountryNum = p.ISOrigCountry
                where s.Company = '{0}' and s.SerialNumber = '{1}'";
            string selectLotInventory = @"
                select j.PartNum, j.PartDescription, j.JobNum, p.basPickCode_c, cast(case when p.TrackSerialNum = 0 and p.TrackLots = 0 then 0 else 1 end as bit) as Tracked, p.basFGLabelOverride_c, 
                    isnull(pc.basFGLabel_c, '') as basFGLabel_c, isnull(c.Description, '') as Country, p.LegalLabel_c, p.basdesctranslation_c
                from JobHead j
                join Part p on p.Company = j.Company and p.PartNum = j.PartNum
                left join PartClass pc on pc.Company = p.Company and pc.ClassID = p.ClassID
                left join Erp.Country c on c.Company = p.Company and c.CountryNum = p.ISOrigCountry
                where j.Company = '{0}' and j.JobNum = '{1}'";
            string selectSerialWIP = @"
                select ud.Key1 as SerialNumber, ud.ShortChar02 as PartNum, p.PartDescription, p.UPCCode1, isnull(c.[Description], '') as Country, isnull(p.basPickCode_c, '') as basPickCode_c, 
                    p.basFGLabelOverride_c, isnull(pc.basFGLabel_c, '') as basFGLabel_c, LegalLabel_c, p.basdesctranslation_c
                from Ice.UD110 ud
                join Part p on p.Company = ud.Company and p.PartNum = ud.ShortChar02
                left join PartClass pc on pc.Company = p.Company and pc.ClassID = p.ClassID
                left join Erp.Country c on c.Company = p.Company and c.CountryNum = p.ISOrigCountry
                where ud.Company = '{0}' and ud.Key1 = '{1}'";
            string selectLotWIP = @""; // not implemented yet
            string selectPart = @"
                select isnull(s.SerialNumber, '{0}') as SerialNumber, p.PartNum, p.PartDescription, p.UPCCode1, isnull(c.[Description], '') as Country, isnull(p.basPickCode_c, '') as basPickCode_c, 
                    p.basFGLabelOverride_c, isnull(pc.basFGLabel_c, '') as basFGLabel_c, LegalLabel_c, p.basdesctranslation_c
                from Part p
                left join PartClass pc on pc.Company = p.Company and pc.ClassID = p.ClassID
                left join Erp.Country c on c.Company = p.Company and c.CountryNum = p.ISOrigCountry
                left join (
                    select Company, SerialNumber, rank() over(partition by PartNum order by newid()) as sRank
                    from SerialNo 
                    where Company = '{1}' and PartNum = '{2}'
                ) s on s.Company = p.Company and sRank = 1
                where p.Company = '{1}' and p.PartNum = '{2}'";
            string vantageSerialInventory = @"
                select s.serialnumber as SerialNumber, s.snstatus as SNStatus, s.partnum as PartNum, p.partdescription as PartDescription, p.shortchar02 as Country
                from serialno s
                join part p on p.company = s.company and p.partnum = s.partnum
                where s.company = '{0}' and s.serialnumber = '{1}'";
            string vantageLotInventory = @"
                select j.partnum as PartNum, j.partdescription as PartDescription, j.jobnum as JobNum, p.shortchar02 as Country
                from jobhead j
                join part p on p.company = j.company and p.partnum = j.partnum
                where j.company = '{0}' and j.jobnum = '{1}'";
            string vantageE10PartLookup = @"
                select p.PartNum, p.basFGLabelOverride_c, c.basFGLabel_c
                from Part p
                join PartClass c on c.Company = p.Company and c.ClassID = p.ClassID
                where p.Company = '{0}' and p.PartNum = '{1}'";

            switch (GetSearchType())
            {
                case MainWindow.SearchType.Inventory:
                    if (serialPartLotNum.Length == 0)
                        serialPartLotNum = serialNumberTextBox.Text;

                    if (serialLotSwitch.Value == 0)
                    {
                        searchType = SearchType.SerialNumber;

                        if (MainWindow.ERPSwitch.Value == 0)
                            select = string.Format(selectSerialInventory, MainWindow.CountryComboBox.Text, serialPartLotNum);
                        else
                            select = string.Format(vantageSerialInventory, MainWindow.CountryComboBox.Text, serialPartLotNum);
                    }
                    else
                    {
                        searchType = SearchType.Lot;

                        if (MainWindow.ERPSwitch.Value == 0)
                            select = string.Format(selectLotInventory, MainWindow.CountryComboBox.Text, serialPartLotNum);
                        else
                            select = string.Format(vantageLotInventory, MainWindow.CountryComboBox.Text, serialPartLotNum);
                    }

                    break;
                case MainWindow.SearchType.WIP:
                    if (serialPartLotNum.Length == 0)
                        serialPartLotNum = serialNumberTextBox.Text;

                    if (MainWindow.ERPSwitch.Value == 1)
                    {
                        MainWindow.Log("There is no WIP for Vantage.");
                        return;
                    }

                    if (serialLotSwitch.Value == 0)
                    {
                        searchType = SearchType.SerialNumber;

                        select = string.Format(selectSerialWIP, MainWindow.CountryComboBox.Text, serialPartLotNum);
                    }
                    else
                    {
                        searchType = SearchType.Lot;

                        //select = string.Format(selectLotWIP, MainWindow.CountryComboBox.Text, serialPartLotNum);
                        MainWindow.Log("Lot WIP is not currently implemented.");
                        return;
                    }

                    break;
                case MainWindow.SearchType.Part:
                    if (serialPartLotNum.Length == 0)
                        serialPartLotNum = partNumberTextBox.Text;

                    if (searchType == SearchType.Part)
                    {
                        
                    }

                    if (serialNumberTextBox.Text.Contains("'"))
                    {
                        serialNumberTextBox.Text = serialNumberTextBox.Text.Replace('\'', ' ');
                    }
                    

                    select = string.Format(selectPart, serialNumberTextBox.Text.Trim().Length > 0 ? serialNumberTextBox.Text : "Test-Serial-1234", MainWindow.CountryComboBox.Text, serialPartLotNum);

                    break;
            }

            using (DataTable dt = new DataTable())
            {
                using (SqlDataAdapter a = new SqlDataAdapter(select, MainWindow.ERPSwitch.Value == 0 ? MainWindow.GetE10ConnectionString() : MainWindow.VantageconnectionString))
                    a.Fill(dt);

                if (dt.Rows.Count == 0)
                {
                    MainWindow.Log("Serial or lot number not found.");
                }
                else
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        string buttonText = "";
                        string label;

                        string partNum = dr.Field<string>("PartNum");
                        string partDesc = dr.Field<string>("PartDescription");
                        string serialNum = dr.Table.Columns.Contains("SerialNumber") ? dr.Field<string>("SerialNumber") : "";
                        string upcCode = dr.Table.Columns.Contains("UPCCode1") ? dr.Field<string>("UPCCode1") : "";
                        string country = dr.Table.Columns.Contains("Country") ? dr.Field<string>("Country") : "";
                        string pickCode = dr.Table.Columns.Contains("basPickCode_c") ? dr.Field<string>("basPickCode_c") : "";
                        string jobNum = dr.Table.Columns.Contains("JobNum") ? dr.Field<string>("JobNum") : "";
                        string status = dr.Table.Columns.Contains("SNStatus") ? dr.Field<string>("SNStatus") : "";
                        bool tracked = dr.Table.Columns.Contains("Tracked") ? dr.Field<bool>("Tracked") : true;
                        string LegalLabel = dr.Table.Columns.Contains("LegalLabel_c") ? dr.Field<string>("LegalLabel_c") : "";
                        string TranslatedDesc = dr.Table.Columns.Contains("basdesctranslation_c") ? dr.Field<string>("basdesctranslation_c") : "";

                        if (MainWindow.ERPSwitch.Value == 1)
                        {
                            string vantageLabel = string.Format(vantageE10PartLookup, MainWindow.CountryComboBox.Text, partNum);

                            using (DataTable dtLabel = new DataTable())
                            {
                                string fgLabel = "";
                                string fgOverride = "";

                                using (SqlDataAdapter a = new SqlDataAdapter(vantageLabel, MainWindow.GetE10ConnectionString()))
                                    a.Fill(dtLabel);

                                dt.Columns.Add("basFGLabel_c", typeof(string));
                                dt.Columns.Add("basFGLabelOverride_c", typeof(string));

                                if (dtLabel.Rows.Count > 0)
                                {
                                    fgLabel = dtLabel.Rows[0].Field<string>("basFGLabel_c");
                                    fgOverride = dtLabel.Rows[0].Field<string>("basFGLabelOverride_c");
                                }

                                foreach (DataRow drFG in dt.Rows)
                                {
                                    drFG.SetField<string>("basFGLabel_c", fgLabel);
                                    drFG.SetField<string>("basFGLabelOverride_c", fgOverride);
                                }

                            }
                        }

                        if (dr.Field<string>("basFGLabelOverride_c").Length > 0)
                            label = dr.Field<string>("basFGLabelOverride_c");
                        else if (dr.Field<string>("basFGLabel_c").Length > 0)
                            label = dr.Field<string>("basFGLabel_c");
                        else if (MainWindow.CountryComboBox.Text != "MY")
                            label = DEFAULT_LABEL;
                        else
                            label = DEFAULT_LABEL_MY;

                        if (String.IsNullOrEmpty(jobNum))
                        {
                            if (!String.IsNullOrEmpty(this.serialNumberTextBox.Text))
                            {
                                jobNum = this.serialNumberTextBox.Text;
                            }
                        }

                        string printData = "";
                        string serialPartData = $"{partNum}|{partDesc}|{serialNum}|S||{pickCode}|{upcCode}|1|1|REPRINT|Product of {country}||{LegalLabel}|{TranslatedDesc}|";
                        string lotTrackedData = $"{partNum}|{partDesc}|{jobNum}|L||{pickCode}|{upcCode}|";
                        string lotNotTrackedData = $"{partNum}|{partDesc}|{jobNum}|J|{jobNum}|{pickCode}|{upcCode}|";
                        string vantageSerialData = $"{partNum}|{partDesc}|{serialNum}|S||{country}||1|1|REPRINT|";
                        string vantageLotTrackedData = $"{partNum}|{partDesc}|{jobNum}|L||{country}||";
                        string vantageLotNotTrackedData = $"{partNum}|{partDesc}||J|{jobNum}|{country}||";

                        if (searchType == SearchType.SerialNumber)
                        {
                            buttonText = $"{serialNum} - Part: {partNum} - {status}";

                            printData = MainWindow.ERPSwitch.Value == 0 ? serialPartData : vantageSerialData;
                        }
                        else if (searchType == SearchType.Lot)
                        {
                            buttonText = $"{jobNum} - LOT";

                            if (allLabelSwitch.Value == 1)
                            {
                                int count;
                                string allLinesTracked = "";
                                string allLinesNotTracked = "";
                                string allLinesTrackedVantage = "";
                                string allLinesNotTrackedVantage = "";

                                if (int.TryParse(howManyTextBox.Text, out count))
                                {
                                    for (int i = 0; i < count; i++)
                                    {
                                        allLinesTracked += lotTrackedData + $"{(i + 1).ToString()}|{count.ToString()}|REPRINT|" + $"Product of { country}||{LegalLabel}|{TranslatedDesc}|" + $"\r\n";
                                        allLinesNotTracked += lotNotTrackedData + $"{(i + 1).ToString()}|{count.ToString()}|REPRINT|" + $"Product of { country}||{LegalLabel}|{TranslatedDesc}| " + $"\r\n";
                                        allLinesTrackedVantage += vantageLotTrackedData + $"{(i + 1).ToString()}|{count.ToString()}|REPRINT|\r\n";
                                        allLinesNotTrackedVantage += vantageLotNotTrackedData + $"{(i + 1).ToString()}|{count.ToString()}|REPRINT|\r\n";
                                    }

                                    lotTrackedData = allLinesTracked;
                                    lotNotTrackedData = allLinesNotTracked;
                                    vantageLotTrackedData = allLinesTrackedVantage;
                                    vantageLotNotTrackedData = allLinesNotTrackedVantage;
                                }
                                else
                                {
                                    lotTrackedData += "1|1|REPRINT|" + $"Product of { country}|" + $"\r\n";
                                    lotNotTrackedData += "1|1|REPRINT|" + $"Product of { country}|" + $"\r\n";
                                    vantageLotTrackedData += "1|1|REPRINT|";
                                    vantageLotNotTrackedData += "1|1|REPRINT|";
                                }
                            }
                            else
                            {
                                lotTrackedData += $"{labelOne.Text}|{labelTwo.Text}|REPRINT|" + $"Product of { country}|";
                                lotNotTrackedData += $"{labelOne.Text}|{labelTwo.Text}|REPRINT|" + $"Product of { country}|" + $"\r\n";
                                vantageLotTrackedData += $"{labelOne.Text}|{labelTwo.Text}|REPRINT|";
                                vantageLotNotTrackedData += $"{labelOne.Text}|{labelTwo.Text}|REPRINT|";
                            }

                            if (tracked)
                                printData = MainWindow.ERPSwitch.Value == 0 ? lotTrackedData : vantageLotTrackedData;
                            else
                                printData = MainWindow.ERPSwitch.Value == 0 ? lotNotTrackedData : vantageLotNotTrackedData;
                        }
                        else
                        {
                            buttonText = partNum;

                            printData = lotNotTrackedData;
                        }

                        addButton(buttonText, string.Format(MainWindow.GetBartenderPrintString(), label, MainWindow.PrinterComboBox.Text) + printData, 
                            searchType == SearchType.SerialNumber ? serialNum : "", searchType == SearchType.Lot ? jobNum : "");
                    }
                }
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

//        private void e10partSearch(string part = "")
//        {
//            {
//                string connectorama, country, serial;
//                country = MainWindow.CountryComboBox.Text;

//                if (country == "MY")
//                { connectorama = MainWindow.MYE10connectionString; }
//                else
//                { connectorama = MainWindow.E10connectionString; }
//                SqlConnection connection = new SqlConnection(connectorama);
//                //SqlConnection connection = new SqlConnection(MainWindow.E10connectionString);
//                SqlDataAdapter da = new SqlDataAdapter();
//                //Start Find Serial Number*********************************************************************
                
//                da.SelectCommand = new SqlCommand(@"SELECT TOP 1 serialnumber AS 'Serial Number' FROM serialno WHERE PartNum = '" + part + "'", connection);

//                DataTable dt = new DataTable();
//                da.Fill(dt);
//                if (dt.Rows.Count < 1)
//                {
//                    MainWindow.Log("Serial number not found. Using None.");
//                    serial = "";
//                }
//                else
//                {
//                    if (serialNumberTextBox.Text != "" ){ serial = serialNumberTextBox.Text; }
//                    else { serial = "TEMP-Serial-1234"; }
//                }
//                //END Find Serial Number*******************************************************************
//                //Stat Find Part number********************************************************************

//                da.SelectCommand = new SqlCommand(@"declare @PartNum varchar(max) set @PartNum = '"+part+@"' select TOP 5 @PartNum +'|'+convert(varchar(max),p.partdescription)+'|'+'"+serial+@"'+'|S||'+ 
//isnull((select p.basPickCode_c from part p where p.partnum = @PartNum AND p.Company='"+country+@"'),'')+'|'+ isnull(p.UPCCode1, '') +'|1|1|REPRINT|' + isnull('Product of ' + c.Description, '') + '|' AS 'data' 
//from part p LEFT JOIN erp.Country c ON p.Company = c.Company AND p.ISOrigCountry = c.CountryNum where p.PartNum = @PartNum AND p.Company = '"+country+"'", connection);
                                                
//                dt = new DataTable();
//                da.Fill(dt);
//                if (dt.Rows.Count < 1)
//                {
//                    MainWindow.Log("Model number not found.");
//                    return;
//                }

//                //END Find Part number********************************************************************

//                string printcode = constructPrintCode(dt.Rows[0]["data"].ToString());
//                addButton(part.ToUpper(), printcode);
//            }
//        }

        //private void VantageLotSearchWIP(string serialnumber)
        //{
        //    MainWindow.Log("There are no items in the Vantage database in WIP state.");
        //}

        //private void VantageSerializedSearchWIP(string serialnumber)
        //{
        //    MainWindow.Log("There are no items in the Vantage database in WIP state.");
        //}

        //private void VantageLotSearchInventory(string serialnumber)
        //{
        //    string jobnum = serialnumber;
        //    SqlConnection connection = new SqlConnection(MainWindow.VantageconnectionString);
        //    SqlDataAdapter da = new SqlDataAdapter();
        //    if (IsNotTracked(jobnum))
        //    {
        //        da.SelectCommand = new SqlCommand(@"declare @jobnum varchar(max) set @jobnum = '" + jobnum + "' select j.partnum+'|'+convert(varchar(max),j.partdescription)+'|'+'|J|" + jobnum + "|'+ (select p.shortchar02 from part p where p.partnum = j.partnum)+'|'+ '|' AS 'data' from jobhead j where j.jobnum = @jobnum", connection);
        //    }
        //    else
        //    {
        //        da.SelectCommand = new SqlCommand(@"declare @jobnum varchar(max) set @jobnum = '" + jobnum + "' select j.partnum+'|'+convert(varchar(max),j.partdescription)+'|'+j.jobnum+'|L||'+ (select p.shortchar02 from part p where p.partnum = j.partnum)+'|'+ '|' AS 'data' from jobhead j where j.jobnum = @jobnum", connection);
        //    }

        //    DataTable dt = new DataTable();
        //    da.Fill(dt);
        //    string data = "";
        //    if (dt.Rows.Count < 1)
        //    {
        //        MainWindow.Log("Serial number not found.");
        //        return;
        //    }
        //    data = constructPrintCode(dt.Rows[0]["data"].ToString());

        //    addButton(serialnumber.ToUpper() + " - " + "LOT", data, "", serialnumber);
        //}

        //private void VantageSerializedSearchInventory(string serialnumber)
        //{
        //    string data;
        //    SqlConnection connection = new SqlConnection(MainWindow.VantageconnectionString);

        //    SqlDataAdapter da = new SqlDataAdapter();
        //    da.SelectCommand = new SqlCommand(@"declare @serialnum varchar(max) set @serialnum = '" + serialnumber + "' select snstatus, s.partnum+'|'+convert(varchar(max),p.partdescription)+'|'+s.serialnumber+'|S||'+(select p.shortchar02 from part p where p.partnum = s.partnum)+'|'+'|' AS 'data' from serialno s INNER JOIN part p ON (p.partnum = s.partnum AND p.company = s.company) where s.serialnumber = @serialnum", connection);
        //    DataTable dt = new DataTable();
        //    da.Fill(dt);
        //    if (dt.Rows.Count < 1)
        //    {
        //        MainWindow.Log("Serial number not found.");
        //        return;
        //    }
        //    data = constructPrintCode(dt.Rows[0]["data"].ToString());
        //    addButton(serialnumber.ToUpper() + " - " + dt.Rows[0]["snstatus"].ToString(), data, serialnumber);
        //}

        //private void e10LotSearchWIP(string serialnumber)
        //{
        //    //throw new NotImplementedException();
        //    MainWindow.Log("Not Implemented");
        //}

        //private void e10SerializedSearchWIP(string serialnumber)
        //{
        //    string data, connectorama, country;
        //    country = MainWindow.CountryComboBox.Text;

        //    if (country == "MY")
        //    { connectorama = MainWindow.MYE10connectionString;}
        //    else
        //    { connectorama = MainWindow.E10connectionString;}
        //    SqlConnection connection = new SqlConnection(connectorama);
            
        //    SqlDataAdapter da = new SqlDataAdapter();
        //    da.SelectCommand = new SqlCommand(@"declare @serialnum varchar(max) set @serialnum = '" + serialnumber + "' select TOP 10 s.shortchar02+'|'+convert(varchar(max),p.partdescription)+'|'+s.key1+'|S||'+ (select p.basPickCode_c from part p where p.partnum = s.shortchar02 AND p.Company='" + country + "')+'|'+ p.UPCCode1 +'|1|1|REPRINT|' + isnull('Product of ' + c.Description, '') + '|' AS 'data' from [Ice].UD110 s INNER JOIN part p ON (p.partnum = s.shortchar02) LEFT JOIN erp.Country c ON p.Company = c.Company AND p.ISOrigCountry = c.CountryNum where s.key1 = @serialnum"
        //    , connection);
        //    DataTable dt = new DataTable();
        //    da.Fill(dt);
        //    if (dt.Rows.Count < 1)
        //    {
        //        MainWindow.Log("Serial number not found.");
        //        return;
        //    }
        //    data = constructPrintCode(dt.Rows[0]["data"].ToString());
        //    addButton(serialnumber.ToUpper() + " - WIP", data, serialnumber);
        //}

        //private void e10LotSearchInventory(string serialnumber)
        //{            
        //    string jobnum = serialnumber;
        //    string data, connectorama, country;
        //    country = MainWindow.CountryComboBox.Text;

        //    if (country == "MY")
        //    { connectorama = MainWindow.MYE10connectionString; }
        //    else
        //    { connectorama = MainWindow.E10connectionString; }
        //    SqlConnection connection = new SqlConnection(connectorama);
            
        //    SqlDataAdapter da = new SqlDataAdapter();
        //    if (IsNotTracked(jobnum))
        //    {
        //        da.SelectCommand = new SqlCommand(@"declare @jobnum varchar(max) set @jobnum = '" + jobnum + "' select j.partnum+'|'+convert(varchar(max),j.partdescription)+'|'+'|J|" + jobnum + "|'+ (select p.basPickCode_c from part p where p.partnum = j.partnum and p.Company = '" + country + "')+'|'+ '|' AS 'data' from jobhead j where j.jobnum = @jobnum", connection);
        //    }
        //    else
        //    {
        //        da.SelectCommand = new SqlCommand(@"declare @jobnum varchar(max) set @jobnum = '" + jobnum + "' select j.partnum+'|'+convert(varchar(max),j.partdescription)+'|'+j.jobnum+'|L||'+ (select p.basPickCode_c from part p where p.partnum = j.partnum and p.Company = '" + country + "')+'|'+ '|' AS 'data' from jobhead j where j.jobnum = @jobnum", connection);
        //    }

        //    DataTable dt = new DataTable();
        //    da.Fill(dt);
        //    data = "";
        //    if (dt.Rows.Count < 1)
        //    {
        //        MainWindow.Log("Lot number not found.");
        //        return;
        //    }
        //    data = constructPrintCode(dt.Rows[0]["data"].ToString());
            
        //    addButton(serialnumber.ToUpper() + " - " + "LOT", data, "", serialnumber);
        //}

        //private void e10SerializedSearchInventory(string serialnum) /////////////  DO THINGS HERE ! ! ! ! ! ! ! ! 
        //{
        //    string data, connectorama;
            
        //    if (MainWindow.CountryComboBox.Text == "MY")
        //    { connectorama = MainWindow.MYE10connectionString; }
        //    else
        //    { connectorama = MainWindow.E10connectionString; }
        //    SqlConnection connection = new SqlConnection(connectorama);
            
        //    SqlDataAdapter da = new SqlDataAdapter();
        //    //da.SelectCommand = new SqlCommand(@"declare @serialnum varchar(max) set @serialnum = '" + serialnum + "' select snstatus, s.partnum+'|'+convert(varchar(max),p.partdescription)+'|'+s.serialnumber+'|S||'+(select p.basPickCode_c from part p where p.partnum = s.partnum)+'|'+ p.UPCCode1 +'|' AS 'data' from serialno s INNER JOIN part p ON (p.partnum = s.partnum AND p.company = s.company) where s.serialnumber = @serialnum", connection);
        //    da.SelectCommand = new SqlCommand(@"declare @serialnum varchar(max) set @serialnum = '" + serialnum + "' select snstatus, s.partnum, s.partnum + '|' + convert(varchar(max),p.partdescription) + '|'+s.serialnumber+'|S||'+ p.basPickCode_c +'|'+ p.UPCCode1 + '|1|1' + '|REPRINT|' + isnull('Product of ' + c.Description, '') + '|' AS 'data' from serialno s INNER JOIN part p ON  (p.partnum = s.partnum AND p.company = s.company) LEFT JOIN Erp.Country c ON (p.Company = c.Company AND p.ISOrigCountry = c.CountryNum) where s.serialnumber = @serialnum and s.Company = '" + MainWindow.CountryComboBox.Text + "'", connection);
        //    DataTable dt = new DataTable();
        //    da.Fill(dt);
        //    if (dt.Rows.Count < 1)
        //    {
        //        MainWindow.Log("Serial number not found.");
        //        return;
        //    }
        //    foreach (DataRow r in dt.Rows)
        //    {
        //        data = constructPrintCode(r["data"].ToString());
        //        addButton(serialnum.ToUpper() + " - Part:  " + r["partnum"].ToString() + " - " + r["snstatus"].ToString(), data, serialnum);
        //    }
        //}

        private void serialLotSwitch_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lotLabel.Foreground = MainWindow.buttonForeground;
            serialLabel.Foreground = MainWindow.buttonForeground ;

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

        //private bool IsNotTracked(string jobnum)
        //{
        //    bool result = true;
        //    string select = @"
        //        select p.TrackLots, p.TrackSerialNum
        //        from JobHead j
        //        join Part p on p.Company = j.Company and p.PartNum = j.PartNum 
        //        where j.Company = '{0}' and j.JobNum = '{1}'";

        //    using (DataTable dt = new DataTable())
        //    {
        //        using (SqlDataAdapter a = new SqlDataAdapter(string.Format(select, MainWindow.CountryComboBox.Text, jobnum), MainWindow.GetE10ConnectionString()))
        //            a.Fill(dt);

        //        if (dt.Rows.Count > 0)
        //            result = !(dt.Rows[0].Field<bool>("TrackLots") || dt.Rows[0].Field<bool>("TrackSerialNum"));
        //    }

        //    return result;
        //}

        //private string constructPrintCode(string code, int startlabel = 1, int endlabel = 1)
        //{
        //    string country = MainWindow.CountryComboBox.Text;
        //    string printer = MainWindow.PrinterComboBox.Text;
        //    string printcode, USMY, RetailLabel, NewConnection, basFGLabelOverride_c, serial, part;
            
        //    if (serialNumberTextBox.Text.Trim().ToLower() == MainWindow._BULKCOMMAND) { serial = _currentBulkLine; }
        //    else { serial = serialNumberTextBox.Text; }
        //    if (country == "MY")
        //    {
        //        RetailLabel = DEFAULT_LABEL_MY;
        //        USMY = "my";
        //        NewConnection = MainWindow.MYE10connectionString;
        //        basFGLabelOverride_c = "";
        //    }
        //    else
        //    {
        //        RetailLabel = DEFAULT_LABEL;
        //        USMY = "us";
        //        NewConnection = MainWindow.E10connectionString;
        //        basFGLabelOverride_c = "p.basFGLabelOverride_c,";
        //    }

        //    if (partNumberTextBox.Text != "") { part = partNumberTextBox.Text; }
        //    else
        //    {
        //        SqlConnection partconnection = new SqlConnection(NewConnection);
        //        SqlDataAdapter pda = new SqlDataAdapter();

        //        if (serialLotSwitch.Value == 0)
        //        {
        //            if (MainWindow.InventorySwitch.Value == 0)
        //            {
        //                pda.SelectCommand = new SqlCommand(@"SELECT PartNum from SerialNo where SerialNumber = '" + serial + "' and Company = '"+ country + "'", partconnection);
        //            }
        //            else
        //            {
        //                pda.SelectCommand = new SqlCommand(@"SELECT ChildKey1 AS PartNum from Ice.UD110A where Key1 = '" + serial + "'", partconnection);
        //            }
        //        }
        //        else
        //        {
        //            pda.SelectCommand = new SqlCommand(@"SELECT PartNum FROM Erp.PartLot Where LotNum = '" + serial + "'", partconnection);
        //        }
        //        DataTable pdt = new DataTable();
        //        pda.Fill(pdt);

        //        //if (pdt.Rows.Count > 0)
        //        //{
        //            DataRow parttemp = pdt.Rows[0];
        //            part = parttemp["PartNum"].ToString();
        //        //}
        //        //else
        //        //    MainWindow.Log("Lot not found.");
        //    }

        //    if (serialLotSwitch.Value == 0)
        //    {
        //        string test1="", test2="";
        //        SqlConnection connection = new SqlConnection(NewConnection);
        //        SqlDataAdapter da = new SqlDataAdapter();
        //        if (MainWindow.InventorySwitch.Value == 0)
        //        {
        //            da.SelectCommand = new SqlCommand(@"SELECT " + basFGLabelOverride_c + @" pc.basFGLabel_c, * FROM dbo.Part p INNER JOIN dbo.PartClass pc ON (p.ClassID = pc.ClassID) WHERE pc.Company='" + country + "' AND p.Company='" + country + "' AND p.PartNum='" + part + "'", connection);
        //        }
        //        else
        //        {
        //            da.SelectCommand = new SqlCommand(@"SELECT " + basFGLabelOverride_c + @" pc.basFGLabel_c FROM dbo.Part p INNER JOIN dbo.PartClass pc ON (p.ClassID = pc.ClassID) INNER JOIN dbo.SerialNo s ON (s.PartNum = p.PartNum) INNER JOIN [Ice].UD110 i ON (i.Company = p.Company AND p.PartNum = i.Shortchar02) WHERE i.Key1='" + serial + "'", connection);
        //        }
        //        DataTable dt = new DataTable();
        //        da.Fill(dt);
        //        if (dt.Rows.Count < 1)
        //        {
        //            return "";
        //        }
        //        DataRow temp = dt.Rows[0];
        //        if (dt.Rows[0].Table.Columns.Contains("basFGLabelOverride_c")) 
        //        { test1 = temp["basFGLabelOverride_c"].ToString(); }
        //        //temp = dt.Rows[1];
        //        test2 = temp["basFGLabel_c"].ToString();
        //        if (test1 != "")
        //        {
        //            RetailLabel = test1;
        //        }
        //        if (test2 != "" && test1 == "")
        //        {
        //            RetailLabel = test2;
        //        }
        //    }
        //    if (USMY != "MY")
        //    { printcode = "%BTW% /AF=\"\\\\E101-SQL\\Websites\\EpicorERP10\\Server\\reports\\" + RetailLabel + "\" /D=%Trigger File Name% /PRN=\"" + printer + "\" /R=3 /P /DD \r\n%END% \r\n"; }
        //    else
        //    { printcode = "%BTW% /AF=\"\\\\e10-" + USMY + "-app-1\\epicor10\\server\\reports\\" + RetailLabel + "\" /D=%Trigger File Name% /PRN=\"" + printer + "\" /R=3 /P /DD \r\n%END% \r\n"; }
            
        //    MainWindow.MainInfoTextBlock.Text = printcode;

        //    if(serialLotSwitch.Value == 1)
        //    {
        //        if (allLabelSwitch.Value == 1)
        //        {
        //            int amount = Convert.ToInt32(howManyTextBox.Text);
        //            for (int i = 0; i < amount; i++)
        //            { 
        //                printcode += code + (i + 1).ToString() + "|" + amount.ToString() + "|REPRINT|\r\n"; 
        //            }
        //        }
        //        else
        //        { printcode += code + labelOne.Text + "|" + labelTwo.Text + "|REPRINT|\r\n"; }
        //    }
        //    else 
        //    { printcode += code; }

        //    return printcode;
        //}

        private void partNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (partNumberTextBox.Text == "")
                PartWarningTxtBox.Visibility = System.Windows.Visibility.Hidden;
            else
                PartWarningTxtBox.Visibility = System.Windows.Visibility.Visible;
        }  
    }
}