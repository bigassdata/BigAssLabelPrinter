using System;
using System.Data.SqlClient;
using System.DirectoryServices.AccountManagement;
using System.Globalization;
using System.IO;

namespace BigAssLabelPrinting
{
    public class User
    {
        public enum Type
        {
            StandardUser = 0,
            Administrator = 1
        }
        private string username;
        private string password;
        private Type accounttype;
        public string country;
        public bool Verified = false;

        public string Username
        {
            get { return username; }
        }

        public User(string Username, string Password)
        {
            username = Username;
            password = Password;
            accounttype = Type.StandardUser;
            country = RegionInfo.CurrentRegion.TwoLetterISORegionName.ToString();
        }
        public bool IsAdmin()
        {
            if (accounttype == Type.Administrator)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public string GetCountry()
        {
            string country = RegionInfo.CurrentRegion.TwoLetterISORegionName.ToString();
            return country;
        }

        public bool VerifyUser()
        {
            try
            {
                string country = GetCountry();
                using (PrincipalContext pc = new PrincipalContext(ContextType.Domain, "BIGASSFAN"))
                {
                    
                    //string time = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.ToString();
                    MainWindow.CountryComboBox.SelectedItem = country;                    
                    //time = "d/M/yyyy";

                    switch (country)
                    {
                        case "US":
                            if (pc.ValidateCredentials(username, password))
                            {
                                if (!checkUsername()) { return false; }
                                Verified = true;
                                //markLastLogin();  // Will Need to find a new way to Keep track of this.
                                return true;
                            }
                            else
                            {
                                MainWindow.MainInfoTextBlock.Text = $"Issue checking credentials\n{MainWindow.MainInfoTextBlock.Text}";
                                return false;
                            }
                        default:
                            Verified = true;
                            return checkUsername();
                    }
                }
            }
            catch (Exception ex)
            {
                using (StreamWriter write = new StreamWriter(@"\\bigassfan.local\Public\IT\BigAssLabelPrinting\PrintError.txt", true))
                {
                    write.WriteLine("Message: " + ex.Message + "<br/>" + Environment.NewLine + "Source: " + ex.Source.ToString());
                }
                MainWindow.MainInfoTextBlock.Text = "Failed to Verify";
                return false; 
            }
        }

        private void markLastLogin()
        {
            string queryString = @"UPDATE LabelReprintUsers SET lastlogin  = '" + DateTime.Now + "' WHERE username = '" + username + "'";
            using (SqlConnection connection = new SqlConnection(MainWindow.fansconnectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private bool checkUsername()
        {
            try
            {
                //SqlDataAdapter da = new SqlDataAdapter();
                //SqlConnection connection = new SqlConnection(MainWindow.E10connectionString);
                ////da.SelectCommand = new SqlCommand(@"SELECT * FROM LabelReprintUsers WHERE username = '" + username + "'", connection);
                //da.SelectCommand = new SqlCommand(@"SELECT * FROM LabelReprintUsers WHERE username = '" + username + "'", connection);
                                
                //DataSet ds = new DataSet();
                //DataTable dt = new DataTable();
                //da.Fill(ds, "LabelReprintUsers");
                //dt = ds.Tables["LabelReprintUsers"];
                ////string email = dt.Rows[0]["username"].ToString();
                //if (ds.Tables[0].Rows.Count < 1)
                //{
                //    return false;
                //}
                //else
                //{
                    //if (Convert.ToInt32(ds.Tables[0].Rows[0]["usertype"]) == 1)
                    if (Username == "jpinson" | username == "tfarlow" | username == "cdevers" | username == "rdrasch" )
                    {
                        accounttype = Type.Administrator;
                    }
                    else
                    {
                        accounttype = Type.StandardUser;
                    }
                    return true;

                //}
            }
            catch
            {
                MainWindow.MainInfoTextBlock.Text = "Failed to Check User credentials";
                return false;
            }
        }
    }
}
