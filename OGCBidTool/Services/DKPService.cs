using GalaSoft.MvvmLight.Messaging;
using HtmlAgilityPack;
using Loggly;
using OGCBidTool.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace OGCBidTool.Services
{
    class DKPService
    {
        private ILogglyClient fLoggly = new LogglyClient();
        private static DKPService fInstance;

        public static DKPService Instance {
            get {
                if ( fInstance == null )
                {
                    fInstance = new DKPService();
                }
                return fInstance;
            }
        }

        private List<MadeMan> fGuildRoster = new List<MadeMan>();
        public List<MadeMan> GuildRoster
        {
            get
            {
                return fGuildRoster;
            }
        }

        public void GetDKPInformation()
        {
            fGuildRoster.Clear();
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
                        Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = "Was able to get the latest DKP successfully" });
                    }
                }
                catch (Exception)
                {
                    Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = "Was NOT able to get the latest DKP, using last known data on file" });
                    var LogEvent = new LogglyEvent();
                    LogEvent.Data.Add("Fetching DKP", "{0}: Error fetching latest DKP, site must be down", DateTime.Now );
                    fLoggly.Log(LogEvent);
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
                        fGuildRoster.Add(vMadeMan);
                    }
                }
            }
            catch (Exception e)
            {
                //Typically bad practice to catch all, but fuck it
                Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = "Critical Error in DKPService" });
                var LogEvent = new LogglyEvent();
                LogEvent.Data.Add("Catch All DKP", "{0}:{1}", DateTime.Now, e);
                fLoggly.Log(LogEvent);
            }
        }
    }
}
