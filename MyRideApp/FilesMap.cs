using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MyRide
{
    [Serializable]
    public class FilesMap
    {
        [XmlArrayItem]
        public List<SynchronizedFile> synchronizedFiles { get; set; }

        [XmlArrayItem]
        public List<SynchronizedFile> synchronizedDirectories { get; set; }

        public DateTime synchronizationTime { get; set; }

        public FilesMap(DateTime synchronizationTime) : this()
        {
            this.synchronizationTime = synchronizationTime;
        }

        public FilesMap()
        {
            synchronizedFiles = new List<SynchronizedFile>();
            synchronizedDirectories = new List<SynchronizedFile>();
        }
    }
}
