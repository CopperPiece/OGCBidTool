using GalaSoft.MvvmLight.Messaging;
using HtmlAgilityPack;
using OGCBidTool.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OGCBidTool.Services
{
    class DKPService
    {
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
            string url = @"http://originalgangster.club/dkp";

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
                            DKP = Convert.ToUInt32(Double.Parse(player.ChildNodes[7].InnerText)),
                            RA30 = UInt32.Parse(player.ChildNodes[9].InnerText.Substring(0, player.ChildNodes[9].InnerText.IndexOf("%"))),
                            RA60 = UInt32.Parse(player.ChildNodes[11].InnerText.Substring(0, player.ChildNodes[11].InnerText.IndexOf("%"))),
                        };
                        fGuildRoster.Add(vMadeMan);
                    }
                }
            }
            catch (Exception e)
            {
                //Typically bad practice to catch all, but fuck it
                Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = "Critical Error in DKPService" });
            }
        }

        // auto-generated Json model
        public class DKPModel
        {
            public double CurrentDKP { get; set; }
            public string CharacterName { get; set; }
            public string CharacterClass { get; set; }
            public string CharacterRank { get; set; }
            public string CharacterStatus { get; set; }
            public double AttendedTicks_30 { get; set; }
            public double TotalTicks_30 { get; set; }
            public double Calculated_30 { get; set; }
            public double AttendedTicks_60 { get; set; }
            public double TotalTicks_60 { get; set; }
            public double Calculated_60 { get; set; }
            public double AttendedTicks_90 { get; set; }
            public double TotalTicks_90 { get; set; }
            public double Calculated_90 { get; set; }
            public double AttendedTicks_Life { get; set; }
            public double TotalTicks_Life { get; set; }
            public double Calculated_Life { get; set; }
        }

        public class DKPRootObject
        {
            public List<DKPModel> Models { get; set; }
            public DateTime AsOfDate { get; set; }
        }

        public void GetDKPInformationJson()
        {
            fGuildRoster.Clear();
            string html = string.Empty;
            string url = @"https://7gnjtigho4.execute-api.us-east-2.amazonaws.com/beta/dkp";

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
                    html = Properties.Settings.Default.DkpInfo;
                }


                DKPRootObject fDKPRoot = JsonConvert.DeserializeObject<DKPRootObject>(html);

                Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = string.Format("DKP site was last updated on: {0}", fDKPRoot.AsOfDate) });


                foreach (DKPModel model in fDKPRoot.Models)
                {
                    MadeMan vMadeMan = new MadeMan()
                    {
                        Name = model.CharacterName,
                        Rank = model.CharacterRank,
                        DKP = Convert.ToUInt32(model.CurrentDKP),
                        RA30 = Convert.ToUInt32(model.Calculated_30 * 100),
                        RA60 = Convert.ToUInt32(model.Calculated_60 * 100),
                    };
                    fGuildRoster.Add(vMadeMan);
                }
            }
            catch (Exception e)
            {
                //Typically bad practice to catch all, but fuck it
                Messenger.Default.Send<GenericMessage>(new GenericMessage() { Message = "Critical Error in DKPService" });
            }
        }

    }
}
