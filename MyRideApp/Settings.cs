using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MyRide
{
    [Serializable]
    public class Settings
    {
 //       private static Dictionary<string, DateTime> lastUpdateTimes;

        public List<string> uploadedPackages { get; set; }

        public DateTime lastUpdateTime { get; set; }

        public List<SynchronizedDirectory> synchronizedDirectories { get; set; }

        public static string ProgramFolderName = "MyRaid";

        public bool autoSynchronizationEnabled { get; set; }

        public bool windowMinimized { get; set; }


        public Settings()
        {

        }

        public Settings(DateTime lastUpdate)
        {
            this.lastUpdateTime = lastUpdate;
            synchronizedDirectories = new List<SynchronizedDirectory>();
            uploadedPackages = new List<string>();
        }
//        {
//            get { return lastUpdateTimes[GetMacAddress()]; }
//        }

//        #region ToDelete
//
//        static Settings()
//        {
//            lastUpdateTimes = new Dictionary<string, DateTime>();
//            DateTime modified = new DateTime(2018,12, 19);
//            lastUpdateTimes.Add(GetMacAddress(), modified);
//        }
//            
//        #endregion

        public static string GetMacAddress()
        {
            var macAddr =
            (
                from nic in NetworkInterface.GetAllNetworkInterfaces()
                where nic.OperationalStatus == OperationalStatus.Up
                select nic.GetPhysicalAddress().ToString()
            ).FirstOrDefault();

            return macAddr;
        }
    }
}
