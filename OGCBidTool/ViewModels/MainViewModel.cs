using System;
using System.Timers;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using OGCBidTool.Services;
using System.Threading.Tasks;
using OGCBidTool.Models;
using GalaSoft.MvvmLight.Messaging;

using System.Linq;
using System.ComponentModel;
using System.Windows.Data;

namespace OGCBidTool.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        DKPService fDKPService = DKPService.Instance;
        LogMonitorService fLogMonitoringService = new LogMonitorService();
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            Title = "Original Gangsters Club - DKP Tool";

            if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.LogFile))
                LogFile = Properties.Settings.Default.LogFile;
            else
                LogFile = "Enter Logfile Path here";

            Messenger.Default.Register<GenericMessage>(
                this, (action) => ReceiveGenericMessage(action)
                );

            Messenger.Default.Register<RollMessage>(
                this, (action) => ReceiveRollMessage(action)
                );

            Rollers = new ObservableCollection<Roller>();
            SortCommand = new RelayCommand<object>((txt)=>Sort(txt));
            InitTimer();
        }

        private string _sortColumn = "AdjustedValue";
        private ListSortDirection _sortDirection = ListSortDirection.Descending;

        public void Sort(object parameter)
        {
            string column = parameter as string;
            if (_sortColumn == column)
            {
                // Toggle sorting direction 
                _sortDirection = _sortDirection == ListSortDirection.Ascending ?
                                                   ListSortDirection.Descending :
                                                   ListSortDirection.Ascending;
            }
            else
            {
                _sortColumn = column;
                _sortDirection = ListSortDirection.Descending;
            }

            RedrawListView();
        }

        private void RedrawListView()
        {
            fRollersView.SortDescriptions.Clear();
            fRollersView.SortDescriptions.Add(
                                     new SortDescription(_sortColumn, _sortDirection));

        }

        private CollectionViewSource fRollersView;
        public ListCollectionView RollersView
        {
            get
            {
                return (ListCollectionView)fRollersView.View;
            }
        }

        private ObservableCollection<Roller> fRollers;
        public ObservableCollection<Roller> Rollers
        {
            get { return fRollers; }
            set
            {
                fRollers = value;
                fRollersView = new CollectionViewSource();
                fRollersView.Source = fRollers;
            }
        }

        private void ReceiveGenericMessage(GenericMessage pMessage)
        {
            AppendText(pMessage.Message);
        }

        private void ReceiveRollMessage(RollMessage pMessage)
        {
            if (pMessage.Action == "add")
            {
                if (fRollers == null)
                {
                    fRollers = new ObservableCollection<Roller>();
                }
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    AddRoller(pMessage.Roller);
                });
                RaisePropertyChanged("Rollers");
            }

            if (pMessage.Action == "clear")
            {
                App.Current.Dispatcher.Invoke((Action)delegate
                {
                    ClearRollers();
                });
                RaisePropertyChanged("Rollers");
            }

            if (pMessage.Action == "refresh")
            {
                if (fRollers != null)
                {
                    App.Current.Dispatcher.Invoke((Action)delegate
                    {
                        RefreshRollers();
                    });
                    RaisePropertyChanged("Rollers");
                }
            }
        }

        private string fTitle;
        public string Title
        {
            get { return fTitle; }
            set
            {
                if (value != fTitle)
                {
                    fTitle = value;
                    RaisePropertyChanged("Title");
                }
            }
        }

        private string fLogFile;
        public string LogFile
        {
            get { return fLogFile; }
            set
            {
                if (value != fLogFile)
                {
                    fLogFile = value;
                    RaisePropertyChanged("LogFile");
                }
            }
        }

        private string fOutputConsole;
        public string OutputConsole
        {
            get { return fOutputConsole; }
            set
            {
                if (value != fOutputConsole)
                {
                    fOutputConsole = value;
                    RaisePropertyChanged("OutputConsole");
                }
            }
        }

        private RelayCommand fBrowseCommand;
        public RelayCommand BrowseCommand
        {
            get
            {
                if (fBrowseCommand == null)
                {
                    fBrowseCommand = new RelayCommand(this.PopBrowseDialog);
                }
                return fBrowseCommand;
            }
        }

        private void PopBrowseDialog()
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
                LogFile = filename;
            }
        }

        public RelayCommand<object> SortCommand
        {
            get;
            private set;
        }

        private RelayCommand fMonitorLogCommand;
        public RelayCommand MonitorLogCommand
        {
            get
            {
                if (fMonitorLogCommand == null)
                {
                    fMonitorLogCommand = new RelayCommand(
                        async () =>
                        {
                            await StartLogMonitoring();
                            await GetDKPInfo();
                        });
                }
                return fMonitorLogCommand;
            }
        }

        private Task StartLogMonitoring()
        {
            return Task.Run(() =>
                {
                    fLogMonitoringService.MonitorLog(this.fLogFile);
                }
            );
        }

        private Task GetDKPInfo()
        {
            return Task.Run(() =>
            {
                AppendText("Fetching the latest DKP information...");
                DKPService.Instance.GetDKPInformation();
                if (DKPService.Instance.GuildRoster.Count == 0)
                {
                    AppendText("Was not able to get the latest DKP information....");
                }
                else
                {
                    AppendText("Loaded " + DKPService.Instance.GuildRoster.Count + " players DKP information (this includes PALTs)");
                    Messenger.Default.Send<RollMessage>(new RollMessage() { Action = "refresh" });
                }
            });
        }

        private void AppendText(string pStringToAppend)
        {
            OutputConsole += pStringToAppend + Environment.NewLine;
        }

        private RelayCommand fClearCommand;
        public RelayCommand ClearCommand
        {
            get
            {
                if (fClearCommand == null)
                {
                    fClearCommand = new RelayCommand(ClearRolls);
                }
                return fClearCommand;
            }
        }

        private void ClearRolls()
        {
            Messenger.Default.Send<RollMessage>(new RollMessage() { Action = "clear" });
        }

        private void ClearRollers()
        {
            aTimer.Stop();
            fRollers.Clear();
            _sortColumn = "AdjustedValue";
            _sortDirection = ListSortDirection.Descending;
            RedrawListView();
            Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = string.Format("Cleared current rolls view.") });
        }

        private void AddRoller(Roller roller)
        {
            var exRoller = fRollers.FirstOrDefault(r => r.Name == roller.Name);

            if (exRoller == null)
            {
                fRollers.Add(roller);
                RedrawListView();
                aTimer.Start();
            }
            else
            {
                Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = string.Format("Duplicate roll ignored from player {0}", exRoller.Name) });
            }
        }

        // update AdjustedValue
        private void RefreshRollers()
        {
            foreach (var vRoller in fRollers)
            {
                var vPlayerDKP = DKPService.Instance.GuildRoster.SingleOrDefault<MadeMan>(s => s.Name.Equals(vRoller.Name, StringComparison.OrdinalIgnoreCase));

                if (vPlayerDKP == null)
                {
                    Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = string.Format("Could not find DKP related information for roller {0}", vRoller.Name) });
                }
                else
                {
                    vRoller.RA60 = UInt32.Parse(vPlayerDKP.RA60.Substring(0, vPlayerDKP.RA60.IndexOf("%")));
                    vRoller.Rank = vPlayerDKP.Rank;
                    vRoller.AdjustedValue = vRoller.RA60 * 10 * vRoller.Value / vRoller.RollMax;
                    Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = string.Format("Updated DKP related information for roller {0}", vRoller.Name) });
                    RedrawListView();
                }
            }
        }

        private static System.Timers.Timer aTimer;

        private static void InitTimer()
        {
            // 7 minutes
            aTimer = new System.Timers.Timer(7 * 60 * 1000);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = false;
            aTimer.Enabled = false;
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Messenger.Default.Send<RollMessage>(new RollMessage() { Action = "clear" });
        }
    }
}