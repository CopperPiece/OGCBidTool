using HtmlAgilityPack;
using OGCBidTool.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OGCBidTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string LOG_FILE_LOCATION = string.Empty;
        private int LastLine = 0;
        private bool FirstTime = true;
        private List<MadeMan> vGuildRoster = new List<MadeMan>();

        public MainWindow()
        {
            InitializeComponent();
            var test = Properties.Settings.Default.LogFile;
            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.LogFile)) LogFileTextBox.Text = Properties.Settings.Default.LogFile;
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            LOG_FILE_LOCATION = LogFileTextBox.Text;
            await LogProcessing();
            await GetDKPInfo();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            
            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            
            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();
            
            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                LogFileTextBox.Text = filename;
            }
        }

        private Task GetDKPInfo()
        {
            return Task.Run(() =>
            {
                UpdateTextBox("Fetching the latest DKP information...");
                GetDKPInformation();
                if (vGuildRoster.Count == 0)
                {
                    UpdateTextBox("Was not able to get the latest DKP information....");
                }
                else
                {
                    UpdateTextBox("Loaded " + vGuildRoster.Count + " players DKP information (this includes PALTs)");
                }
            });
        }

        private Task LogProcessing()
        {
            return Task.Run(() =>
            {
                UpdateTextBox("Validating Log File");
                if (File.Exists(LOG_FILE_LOCATION))
                {
                    Properties.Settings.Default.LogFile = LOG_FILE_LOCATION;
                    Properties.Settings.Default.Save();
                    var DirectoryPath = System.IO.Path.GetDirectoryName(LOG_FILE_LOCATION);
                    var FileName = System.IO.Path.GetFileName(LOG_FILE_LOCATION);
                    UpdateTextBox("Starting Initial Log Parse");
                    onChanged(null, null);
                    UpdateTextBox("Finished. Monitoring now...");
                    var watch = new FileSystemWatcher();
                    watch.Path = DirectoryPath;
                    watch.Filter = FileName;
                    watch.NotifyFilter = NotifyFilters.LastWrite;
                    watch.Changed += new FileSystemEventHandler(onChanged);
                    watch.EnableRaisingEvents = true;
                }
                else
                {
                    UpdateTextBox("I don't think you entered a valid Log File, try again?");
                }
            });
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            BidTextBox.Clear();
        }

        private void onChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                var fs = new FileStream(LOG_FILE_LOCATION, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                string Line;
                int CurrentLine = 0;
                using (StreamReader sr = new StreamReader(fs))
                {
                    while ((Line = sr.ReadLine()) != null)
                    {
                        CurrentLine++;
                        if (CurrentLine > LastLine)
                        {
                            if (!FirstTime)
                            {
                                if (Line.ToLower().Contains("bid "))
                                {
                                    ParseBid(Line.ToLower());
                                }
                            }
                        }
                    }
                    LastLine = CurrentLine;
                    FirstTime = false;
                }
            }
            catch(Exception ex)
            {
                UpdateTextBox("Critical Error detected: "+ex);
            }
        }

        public void ParseBid(string bid)
        {
            string noTimestamp = bid.Substring(bid.IndexOf("]") + 2);
            string playerName = noTimestamp.Split(' ')[0];
            if ( playerName.Equals("you", StringComparison.OrdinalIgnoreCase) )
            {
                string vFileName = System.IO.Path.GetFileNameWithoutExtension(LOG_FILE_LOCATION);
                playerName = vFileName.Split('_')[1];
            }
            playerName = char.ToUpper(playerName[0]) + playerName.Substring(1);
            string playerBid = bid.Substring(bid.IndexOf("bid")); playerBid = playerBid.Remove(playerBid.Length - 1);
            var vTest = vGuildRoster.SingleOrDefault<MadeMan>(s => s.Name.Equals(playerName,StringComparison.OrdinalIgnoreCase));
            if ( vTest == null )
            {
                UpdateTextBox(playerName + " " + playerBid + " (no dkp info available)");
            } else
            {
                UpdateTextBox(string.Format("{0} {1} (RANK={2}, RA={3}, DKP={4})",playerName, playerBid, vTest.Rank, vTest.RA, vTest.DKP));
            }          
        }
        public delegate void UpdateTextCallback(string message);
        private void UpdateTextBox(string input)
        {
            BidTextBox.Dispatcher.Invoke(new UpdateTextCallback(this.UpdateText), new object[] { input });
        }
        private void UpdateText(string message)
        {
            BidTextBox.AppendText(message + Environment.NewLine);
            BidTextBox.ScrollToEnd();
        }

        private void GetDKPInformation()
        {
            string html = string.Empty;
            string url = @"http://modestman.club/dkp";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            try
            {
                try
                {
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        html = reader.ReadToEnd();
                        Properties.Settings.Default.DkpInfo = html;
                        Properties.Settings.Default.Save();
                        UpdateTextBox("Was able to get the latest DKP successfully");
                    }                
                }
                catch (Exception)
                {
                    UpdateTextBox("Was NOT able to get the latest DKP, using last known data on file");
                    html = Properties.Settings.Default.DkpInfo;
                }
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                HtmlNodeCollection playerList = htmlDoc.DocumentNode.SelectNodes("//table[@class=\"table fullwidth trcheckboxclick hptt colorswitch scrollable-x\"]/tr");

                foreach (HtmlNode player in playerList)
                {
                    if (player.ChildNodes.Count >= 9 && !player.ChildNodes[3].InnerText.Trim().Equals("Name"))
                    {
                        MadeMan vMadeMan = new MadeMan()
                        {
                            Name = player.ChildNodes[3].InnerText,
                            Rank = player.ChildNodes[5].InnerText,
                            DKP = player.ChildNodes[7].InnerText,
                            RA = player.ChildNodes[9].InnerText,
                        };
                        vGuildRoster.Add(vMadeMan);
                    }
                }
            }
            catch(Exception e)
            {
                //Typically bad practice to catch all, but fuck it
            }
        }
    }
}
