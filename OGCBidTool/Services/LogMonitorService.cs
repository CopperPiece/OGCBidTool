using GalaSoft.MvvmLight.Messaging;
using Loggly;
using OGCBidTool.Models;
using System;
using System.IO;
using System.Linq;

namespace OGCBidTool.Services
{
    public class LogMonitorService
    {
        private ILogglyClient fLoggly = new LogglyClient();
        private bool FirstTime = true;
        private FileSystemWatcher fFileWatcher = new FileSystemWatcher();
        private long Position = 0;

        public void MonitorLog(string pLogFilePath)
        {
            Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = "Validating Log File" });
            if (File.Exists(pLogFilePath))
            {
                var LogEvent = new LogglyEvent();
                LogEvent.Data.Add("Monitor Log", "{0}: valid Logfile={1}", DateTime.Now, pLogFilePath);
                fLoggly.Log(LogEvent);

                Properties.Settings.Default.LogFile = pLogFilePath;
                Properties.Settings.Default.Save();
                var DirectoryPath = Path.GetDirectoryName(pLogFilePath);
                var FileName = Path.GetFileName(pLogFilePath);

                fFileWatcher.Path = DirectoryPath;
                fFileWatcher.Filter = FileName;
                fFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
                fFileWatcher.Changed += new FileSystemEventHandler(onChanged);
                fFileWatcher.EnableRaisingEvents = true;

                Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = "Starting Initial Log Parse" });
                onChanged(this, new FileSystemEventArgs(WatcherChangeTypes.All, DirectoryPath, FileName));
                Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = "Finished. Monitoring now..." });
            }
            else
            {
                Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = "I don't think you entered a valid Log File, try again?" });
                var LogEvent = new LogglyEvent();
                LogEvent.Data.Add("Monitor Log", "{0}: Invalid Logfile={1}", DateTime.Now, pLogFilePath);
                fLoggly.Log(LogEvent);
            }
        }

        private void onChanged(object pSender, FileSystemEventArgs pFileSystemEventArgs)
        {
            string Line = string.Empty;
            try
            {
                var fs = new FileStream(pFileSystemEventArgs.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fs.Position = Position;
                using (StreamReader sr = new StreamReader(fs))
                {
                    while ((Line = sr.ReadLine()) != null)
                    {
                        if (!FirstTime && Line.Contains("'"))
                        {
                            string vPlayerMessage = Line.Substring(Line.IndexOf("'")); vPlayerMessage = vPlayerMessage.Trim('\'');
                            string[] vTokens = vPlayerMessage.Split(' ');
                            int vBid = 0;
                            if (vTokens != null && vTokens.Length > 0 && int.TryParse(vTokens[0], out vBid))
                            {
                                if (vBid >= 75)
                                {
                                    //Get Player Name
                                    string noTimestamp = Line.Substring(Line.IndexOf("]") + 2);
                                    string playerName = noTimestamp.Split(' ')[0];
                                    if (playerName.Equals("you", StringComparison.OrdinalIgnoreCase))
                                    {
                                        string vFileName = Path.GetFileNameWithoutExtension(pFileSystemEventArgs.FullPath);
                                        playerName = vFileName.Split('_')[1];
                                    }
                                   //Bidding for PALT?
                                    if ( noTimestamp.ToLower().Contains("palt") && vTokens.Length>2 )
                                    {
                                        playerName = vTokens[2];
                                    }

                                    playerName = char.ToUpper(playerName[0]) + playerName.Substring(1);
                                    string playerBid = vBid.ToString();
                                    var vPlayerDKP = DKPService.Instance.GuildRoster.SingleOrDefault<MadeMan>(s => s.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
                                    if (vPlayerDKP == null)
                                    {
                                        Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = string.Format("{0} {1} (no dkp info available)", playerName, vBid) });
                                    }
                                    else
                                    {
                                        Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = string.Format("{0} {1} (RANK = {2}, RA = {3}, DKP = {4})", playerName, playerBid, vPlayerDKP.Rank, vPlayerDKP.RA, vPlayerDKP.DKP) });
                                    }
                                }
                            }
                        }
                    }
                    Position = fs.Position;
                    FirstTime = false;
                }
            }
            catch (Exception ex)
            {
                Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = "Critical Error detected: " + Environment.NewLine + Line });
                var LogEvent = new LogglyEvent();
                LogEvent.Data.Add("Monitor Log Catch All", "{0}:{1}", DateTime.Now, ex);
                fLoggly.Log(LogEvent);
            }
        }
    }
}
