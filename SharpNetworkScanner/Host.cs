using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace MDScanner2
{
    class Host
    {
        public string ip;
        public string os;
        public List<int> ports = new List<int>();
        public string serverheader;
        public string portDetect;
        public string title;
        public Host(string ipString)
        {
            ip = ipString;
            os = "";
            ports = new List<int>();
            serverheader = "";
            title = "";
        }
        
        public string osDetection(string ipString)
        {
            string os = "";
            Ping pinger = null;
            int ttl = 0;
            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(ipString);
                bool pingable = reply.Status == IPStatus.Success;
                if (pingable == true)
                {
                    ttl = reply.Options.Ttl;
                    if (ttl == 32 || ttl == 128) { os = "Windows"; };
                    if (ttl == 200) { os = "MPE/IX (HP)"; };
                    if (ttl == 64) { os = "Linux"; };
                    if (ttl == 60) { os = "Stratus"; };
                }
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }
            return os;
        }
    }
}
