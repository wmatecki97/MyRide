using Google.Apis.Drive.v3.Data;
using MyRide;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;

namespace MyRideApp
{
    public static class AppManagement
    {
        public static Settings _settings;

        public static bool backgroundProcessing = false;

        private const string NothingToSynchronize = "Nothing to update.";
        private const string NoChangesFound = "No changes found.";

        static AppManagement()
        {
            var a = GoogleDriveHelper.GetAllProgramFiles();
            _settings = Serializer.DeSerializeObject<Settings>("settings.mr");

            if (_settings == null)
                _settings = new Settings(DateTime.MinValue);
        }

        public static void SaveSettings()
        {
            Serializer.SerializeObject(_settings, "settings.mr");
        }

        public static string Pull()
        {
            List<string> packagesToSynchronise = DownloadAllPackagesToSynchronize();

            foreach (var package in packagesToSynchronise)
            {
                var (name, packageSynchronizationDate) = FileHelper.GetPackageInfo(package);

                SynchronizedDirectory directory = _settings.synchronizedDirectories.FirstOrDefault(d => d.name == name);

                FileHelper.Unpack(directory.path, package, packageSynchronizationDate);
            }

            if (packagesToSynchronise.Count() > 0)
            {
                return "Synchronized " + packagesToSynchronise.Count() + " packages.";
            }
            else return NothingToSynchronize;
        }

        public static string Push()
        {
            string result = "";

            DateTime synchronizationStartTime = DateTime.Now;

            foreach (var directory in _settings.synchronizedDirectories)
            {
                FileHelper fh = new FileHelper(_settings.lastUpdateTime, directory.path, directory.name);
                int filesCount = fh.GetFiles();
                if (filesCount > 0)
                {
                    string package = fh.CreatePackage();
                    GoogleDriveHelper.UploadAndDeleteFile(package);
                    _settings.uploadedPackages.Add(package);

                    result += "Pushed: " + filesCount + " files from " + directory.name + " directory. ";
                }

                result += NoChangesFound;
            }

            _settings.lastUpdateTime = synchronizationStartTime;

            Serializer.SerializeObject(_settings, "settings.mr");

            return result;
        }

        private static List<string> DownloadAllPackagesToSynchronize()
        {
            List<string> packages = new List<string>();
            List<File> uploadedPackages = GoogleDriveHelper.GetAllProgramFiles();
            foreach (var uploadedPackage in uploadedPackages)
            {
                if (!_settings.uploadedPackages.Contains(uploadedPackage.Name))
                {
                    string name = GoogleDriveHelper.DownloadAndUnzipPackage(uploadedPackage.Id, uploadedPackage.Name);
                    packages.Add(name);
                    GoogleDriveHelper.DeleteFile(uploadedPackage.Id);
                }
            }

            return packages;
        }

        public static void AutoSynchronize(NotifyIcon notifyIcon)
        {
            backgroundProcessing = true;
            Thread t = new Thread(() => Synchronize(notifyIcon));
            t.Start();
        }

        private static void Synchronize(NotifyIcon notifyIcon)
        {
            while (backgroundProcessing)
            {
                Thread.Sleep(7200);

                string pushMessage = Push();
                pushMessage = pushMessage.Replace(NoChangesFound, "");

                if(pushMessage.Length > 0)
                {
                    notifyIcon.BalloonTipTitle = "Succesfully added files to drive";
                    notifyIcon.BalloonTipText = pushMessage;
                    notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                    notifyIcon.ShowBalloonTip(1000);
                }

                string pullMessage = Pull();

                if (!pullMessage.Contains(NothingToSynchronize))
                {
                    notifyIcon.BalloonTipTitle = "Succesfully updated files";
                    notifyIcon.BalloonTipText = pullMessage;
                    notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                    notifyIcon.ShowBalloonTip(1000);
                }
            }
        }

    }
}
