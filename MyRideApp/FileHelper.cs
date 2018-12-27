using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;


namespace MyRide
{
    public class FileHelper
    {
        #region GlobalVariables
        public List<string> modifiedFiles { get; set; }
        public List<string> createdDirectories { get; set; }

        public DateTime lastUpdateTime { get; set; }
        public DateTime synchronizationStartTime { get; set; }
        public FilesMap filesMap { get; set; }
        public string name { get; set; }

        public long totalSize
        {
            get { return _totalSize ?? 0; }
        }

        private const string _dateFormat = "dd-MM-yyyy HH-mm-ss";

        private string _startDirectory;
        private long? _totalSize;
        private string _packageName;

        #endregion

        public FileHelper(DateTime lastUpdate, string startDirectory, string name)
        {
            this._startDirectory = startDirectory;
            lastUpdateTime = lastUpdate;
            modifiedFiles = new List<string>();
            createdDirectories = new List<string>();
            _totalSize = null;
            filesMap = new FilesMap();
            this.name = name;
        }


        #region FileExploration
        public int GetFiles()
        {
            DirSearch(_startDirectory);
            int modifiedFilesCount = FindModifiedFiles(_startDirectory);

            GetTotalSize();

            return modifiedFilesCount;
        }

        private bool IsDirectory(string filename)
        {
            FileInfo info = new FileInfo(filename);
            return info.Attributes == FileAttributes.Directory;
        }

        private void DirSearch(string dir)
        {
            try
            {
                foreach (string directory in Directory.GetDirectories(dir))
                {
                    DateTime creation = File.GetCreationTime(directory);
                    DateTime modification = File.GetLastWriteTime(directory);


                    if (creation > lastUpdateTime)
                    {
                        createdDirectories.Add(directory);
                    }
                    else
                    {
                        DirSearch(directory);
                        FindModifiedFiles(directory);

                    }



                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }

        private int FindModifiedFiles(string directory)
        {
            DateTime modification;
            int modifiedFilesCount = 0;
            foreach (string file in Directory.GetFiles(directory))
            {
                modification = File.GetLastWriteTime(file);

                if (modification > lastUpdateTime && !IsDirectory(file))
                {
                    modifiedFiles.Add(file);
                    modifiedFilesCount++;
                }
            }

            return modifiedFilesCount;
        }
        #endregion


        #region PackageCreation
        public string CreatePackage()
        {
            synchronizationStartTime = DateTime.Now;
            _packageName = addDateToName(_packageName);
            name = addDateToName(name);

            PrepareDirectoryForPackage();

            MapFiles();

            foreach (var file in filesMap.synchronizedFiles)
            {
                File.Copy(file.originalFilePath, (file.packageFilePath));
            }

            foreach (var directory in filesMap.synchronizedDirectories)
            {
                ZipFile.CreateFromDirectory(directory.originalFilePath, directory.packageFilePath + ".zip");
                //ZipFile.CreateFromDirectory(directory.originalFilePath, "F:\\Programowanie\\C#\\Test\\testzipa.zip");
            }

            AddSettingFile();

            ZipFile.CreateFromDirectory(_packageName, name + ".zip");

            Directory.Delete(_packageName, true);

            return name + ".zip";
        }

        public static string UnZipPackage(string packageName)
        {
            int index = packageName.IndexOf(".zip");
            string newName = packageName.Substring(0, index);
            ZipFile.ExtractToDirectory(packageName, newName);
            deleteFile(packageName);
            return newName;
        }

        public static (string, DateTime) GetPackageInfo(string packageName)
        {
            string[] splitted = packageName.Split('#');
            string dirName = splitted[0];
            DateTime synchronizationTime = DateTime.ParseExact(splitted[1], _dateFormat, CultureInfo.InvariantCulture);

            return (dirName, synchronizationTime);
        }

        public static void deleteFile(string filename)
        {
            File.Delete(filename);
        }

        private void MapFiles()
        {
            //Needed to create directories before adding files to them
            modifiedFiles.Sort();

            int filenumber = 0;

            foreach (var file in modifiedFiles)
            {
                SynchronizedFile record = CreateSynchronizedFile(ref filenumber, file);
                filesMap.synchronizedFiles.Add(record);
            }

            foreach (var directory in createdDirectories)
            {
                SynchronizedFile record = CreateSynchronizedFile(ref filenumber, directory);
                filesMap.synchronizedDirectories.Add(record);
            }
        }

        private void GetTotalSize()
        {
            long size = 0;
            foreach (string modifiedFile in modifiedFiles)
            {
                FileInfo info = new FileInfo(modifiedFile);
                if (info.Attributes != FileAttributes.Directory)
                {
                    size += info.Length;
                }
            }

            _totalSize = size;
        }

        private SynchronizedFile CreateSynchronizedFile(ref int filenumber, string file)
        {
            string filePath = file.Replace(_startDirectory, "");
            string fileName = GetFileName(filenumber, filePath);

            filenumber++;

            SynchronizedFile record = new SynchronizedFile(file, filePath, fileName, _packageName + "\\" + fileName);
            return record;
        }

        private static string GetFileName(int filenumber, string filePath)
        {
            return filePath.Substring(filePath.LastIndexOf('\\') + 1) + filenumber.ToString();
        }

        private void PrepareDirectoryForPackage()
        {
            try
            {
                bool exists = System.IO.Directory.Exists(_packageName);

                if (exists)
                    Directory.Delete(_packageName, true);

                Directory.CreateDirectory(_packageName);
            }
            catch (Exception e)
            {
                throw new NotImplementedException();
            }
        }

        private void AddSettingFile()
        {
            filesMap.synchronizationTime = synchronizationStartTime;
            Serializer.SerializeObject<FilesMap>(filesMap, _packageName + "\\settings.xml");
        }

        private string addDateToName(string name)
        {
            return name + "#" + synchronizationStartTime.ToString(_dateFormat);
        }

        #endregion

        #region PackageUnpacking

        public static void Unpack(string path, string packageName, DateTime packageSynchronizationTime)
        {
            try
            {
                FilesMap map = Serializer.DeSerializeObject<FilesMap>(packageName + "\\settings.xml");
                foreach (SynchronizedFile synchronizedFile in map.synchronizedFiles)
                {
                    string filePackagePath = packageName + "\\" + synchronizedFile.fileName;
                    string destinationPath = path + synchronizedFile.relativeFilePath;
                    if (!File.Exists(destinationPath) ||
                        File.GetLastWriteTime(destinationPath) < packageSynchronizationTime)
                    {
                        File.Delete(destinationPath);
                        File.Copy(filePackagePath, destinationPath);
                    }
                }

                foreach (SynchronizedFile synchronizedDirectory in map.synchronizedDirectories)
                {
                    string filePackagePath = packageName + "\\" + synchronizedDirectory.fileName + ".zip";
                    string destinationPath = path + synchronizedDirectory.relativeFilePath;

                    if (Directory.Exists(destinationPath) && Directory.GetLastWriteTime(destinationPath) < packageSynchronizationTime)
                    {
                        Directory.Delete(destinationPath, true);
                    }
                    if(!Directory.Exists(destinationPath))
                    {
                        ZipFile.ExtractToDirectory(filePackagePath, destinationPath);
                    }
                }

                Directory.Delete(packageName, true);
            }
            catch (Exception e)
            {
                throw new NotImplementedException();
            }
        }

        #endregion

    }

}
