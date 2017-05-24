using Loggly;
using System;
using System.Windows;

namespace OGCBidTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ILogglyClient fLoggly = new LogglyClient();
        public App()
        {
            var LogEvent = new LogglyEvent();
            LogEvent.Data.Add("App Start detected", "{0}: Welcome new user!", DateTime.Now);
            fLoggly.Log(LogEvent);
        }
    }
}
