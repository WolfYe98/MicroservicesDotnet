using System;

namespace Play.Common.Settings
{
    public class ServiceCommunicationSettings
    {
        public string Url { get; set; }
        public int TimeoutSecs { get; set; }
        public Uri CommunicationUri
        {
            get
            {
                return new Uri(Url);
            }
        }
    }
}