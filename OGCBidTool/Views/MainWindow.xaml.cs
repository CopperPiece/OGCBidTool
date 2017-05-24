using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace OGCBidTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BidTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            BidTextBox.ScrollToEnd();
        }
    }
}
