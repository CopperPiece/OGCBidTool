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


                Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = "Starting Initial Log Parse" });
                onChanged(this, new FileSystemEventArgs(WatcherChangeTypes.All, DirectoryPath, FileName));
                Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = "Finished. Monitoring now..." });

                fFileWatcher.Path = DirectoryPath;
                fFileWatcher.Filter = FileName;
                fFileWatcher.NotifyFilter = NotifyFilters.LastWrite;
                fFileWatcher.Changed += new FileSystemEventHandler(onChanged);
                fFileWatcher.EnableRaisingEvents = true;

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
                        if (!FirstTime)
                        {
                            if (Line.Substring(27).StartsWith("**A Magic Die is rolled"))
                            {
                                string[] vRollTokens = Line.Split(' ');
                                string TimeStamp = vRollTokens[3];
                                string playerName = vRollTokens[11].Remove(vRollTokens[11].Length - 1);
                                playerName = char.ToUpper(playerName[0]) + playerName.Substring(1);

                                // verify that roll started from zero
                                string vRollMin = vRollTokens[19];
                                if (vRollMin != "0")
                                {
                                    Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = string.Format("Skipping roll by {0} since it started from {1}", playerName, vRollMin) });
                                    continue;
                                }

                                var vPlayerDKP = DKPService.Instance.GuildRoster.SingleOrDefault<MadeMan>(s => s.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));

                                Roller vRoller = new Roller();
                                vRoller.Name = playerName;
                                vRoller.RollMax = UInt32.Parse(vRollTokens[21].Remove(vRollTokens[21].Length - 1));
                                vRoller.Value = UInt32.Parse(vRollTokens[29].Remove(vRollTokens[29].Length - 1));

                                if (vPlayerDKP == null)
                                {
                                    vRoller.RA60 = 0;
                                    vRoller.Rank = "Unknown";
                                    vRoller.AdjustedValue = 0;
                                }
                                else
                                {
                                    vRoller.RA60 = UInt32.Parse(vPlayerDKP.RA60.Substring(0,vPlayerDKP.RA60.IndexOf("%")));
                                    vRoller.Rank = vPlayerDKP.Rank;
                                    vRoller.AdjustedValue = vRoller.RA60 * 10 * vRoller.Value / vRoller.RollMax;

                                }
                                Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = string.Format("{0}: {1} rolled {2} out of {3} (Adjusted Value = {4}, RANK = {5}, 60-day RA = {6})", TimeStamp, playerName, vRoller.Value, vRoller.RollMax, vRoller.AdjustedValue, vRoller.Rank, vRoller.RA60) });
                                Messenger.Default.Send<RollMessage>(new RollMessage() { Action = "add", Roller = vRoller });
                            }

                            /*
                            if (Line.Contains("'"))
                            {
                                string vPlayerMessage = Line.Substring(Line.IndexOf("'")); vPlayerMessage = vPlayerMessage.Trim('\'');
                                string[] vTokens = vPlayerMessage.Split(' ');
                                int vBid = 0;

                                foreach (string sToken in vTokens)
                                {
                                    string lowerToken = sToken.ToLower();
                                    if (lowerToken.EndsWith("k"))
                                    {
                                        lowerToken = lowerToken.Remove(lowerToken.Length - 1) + "000";
                                    }
                                    int.TryParse(lowerToken, out vBid);

                                }
                                if (vBid >= 1000)
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
                                    if (noTimestamp.ToLower().Contains("palt") && vTokens.Length > 2)
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
                                        Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = string.Format("{0} {1} (RANK = {2}, RA = {3}, DKP = {4})", playerName, playerBid, vPlayerDKP.Rank, vPlayerDKP.RA30, vPlayerDKP.DKP) });
                                    }
                                }
                            } */
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
