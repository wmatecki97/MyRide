using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyRide
{
    [Serializable]
    public class SynchronizedFile
    {
        public string originalFilePath { get; set; }
        public string relativeFilePath { get; set; }
        public string fileName { get; set; }
        public string packageFilePath { get; set; }
        public DateTime lastWriteTime { get; set; }

        public SynchronizedFile(string originalFilePath, string relativeFilePath, string name, string packageFilePath, DateTime modified)
        {
            this.originalFilePath = originalFilePath;
            this.relativeFilePath = relativeFilePath;
            fileName = name;
            this.packageFilePath = packageFilePath;
            this.lastWriteTime = modified;
        }

        public SynchronizedFile()
        {
        }
    }
}
