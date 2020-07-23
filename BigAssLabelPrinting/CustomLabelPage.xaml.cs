using System;
using System.Windows;
using System.Windows.Controls;

namespace BigAssLabelPrinting
{
    /// <summary>
    /// Interaction logic for CustomLabelPage.xaml
    /// </summary>
    public partial class CustomLabelPage : Page
    {
        public CustomLabelPage()
        {
            InitializeComponent();
        }

        private void sendToDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.SendToBartender(customCodeTextBox.Text);
            infoTextbox.Text += DateTime.Now.ToString() + " : Data was sent to Bartender : " + customCodeTextBox.Text + Environment.NewLine;
            customCodeTextBox.Text = "";
        }
    }
}
