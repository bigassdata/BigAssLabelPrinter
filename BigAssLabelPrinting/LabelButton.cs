using System.Windows.Controls;

namespace BigAssLabelPrinting
{
    class LabelButton : Button

    {
        public string LabelInfo;

        public void SetLabelInfo(string InformationString)
        {
            LabelInfo = InformationString;
        }

        public string getInfo()
        {
            return LabelInfo;
        }

        public string Printer { get; set; }
        public string SerialNumber { get; set; }
        public string LotNumber { get; set; }
        public string LabelType { get; set; }
    }
}
