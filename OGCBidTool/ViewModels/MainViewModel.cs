using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using OGCBidTool.Services;
using System.Threading.Tasks;
using OGCBidTool.Models;
using GalaSoft.MvvmLight.Messaging;
using Loggly;

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
                this, (action) => RecieveMessage(action)
                );
        }

        private void RecieveMessage(GenericMessage pMessage)
        {
            AppendText(pMessage.Message);
        }

        private string fTitle;
        public string Title
        {
            get { return fTitle; }
            set
            {
                if ( value != fTitle )
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
                    fClearCommand = new RelayCommand(ClearLogs);
                }
                return fClearCommand;
            }
        }

        private void ClearLogs()
        {
            OutputConsole = string.Empty;
        }
    }
}