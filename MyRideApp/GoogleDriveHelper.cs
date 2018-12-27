using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using File = Google.Apis.Drive.v3.Data.File;
using System.Linq;
using System.Runtime.InteropServices;
using MyRide;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Download;

namespace MyRide
{
    public class GoogleDriveHelper
    {
        private static DriveService _service;
        private static string _programFolderId;

        static GoogleDriveHelper()
        {
            _service = Connect();
            _programFolderId = FindFile(f => f.Name == Settings.ProgramFolderName).Id;
        }

        private static DriveService Connect()
        {
            string[] scopes = new string[] { DriveService.Scope.Drive,
                DriveService.Scope.DriveFile};
            var clientId = "141606765449-ks44t3dfdqlletdgpjeq0v5hpra99ev5.apps.googleusercontent.com";      // From https://console.developers.google.com
            var clientSecret = "WbAmVi0lGRjco-3q8MaZLiJh";          // From https://console.developers.google.com
                                                                    // here is where we Request the user to give us access, or use the Refresh Token that was previously stored in %AppData%
            var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            },
                scopes,
                Environment.UserName,
                CancellationToken.None,
                new FileDataStore("MyAppsToken")).Result;
            //Once consent is received, your token will be stored locally on the AppData directory, so that next time you won't be prompted for consent. 

            DriveService service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "MyAppName",
            });
            service.HttpClient.Timeout = TimeSpan.FromMinutes(100);
            //Long Operations like file uploads might timeout. 100 is just precautionary value, can be set to any reasonable value depending on what you use your _service for.

            return service;
        }

        #region LocalActions

        // _service: Valid, authenticated Drive _service
        // fileName: Full path to the file to upload
        // _parent: ID of the parent directory to which the file should be uploaded

        private static List<File> FindFiles(Func<File, bool> lambda, string nextPageToken = null, bool stopWhenNextPageIsNull = false, bool firstPage = false)
        {
            List<File> result;
            result = GetFilesFromPage(lambda, ref nextPageToken);

            //result = files.FirstOrDefault(f => f.Name == Settings.ProgramFolderName);

            if (nextPageToken == null)
            {
                if (stopWhenNextPageIsNull || firstPage)
                {
                    return result;
                }
            }

            result = result.Concat(FindFiles(lambda, nextPageToken, true)).ToList();

            return result;
        }

        private static File FindFile(Func<File, bool> lambda, string nextPageToken = null, bool stopWhenNextPageIsNull = false)
        {
            File result;

            result = GetFilesFromPage(lambda, ref nextPageToken).FirstOrDefault();

            if (nextPageToken == null)
            {
                if (stopWhenNextPageIsNull)
                    return result;
            }

            return result ?? FindFile(lambda, nextPageToken, true);
        }

        private static List<File> GetFilesFromPage(Func<File, bool> lambda, ref string nextPageToken)
        {
            List<File> result;
            try
            {
                FilesResource.ListRequest listRequest = _service.Files.List();
                listRequest.PageSize = 1000;//TODO -zmiana na wieksza wartosc
                listRequest.Fields = "nextPageToken, files(id, name, parents)";
                if (nextPageToken != null)
                {
                    listRequest.PageToken = nextPageToken;
                }

                var response = listRequest.Execute();
                nextPageToken = response.NextPageToken;
                var files = response.Files;

                result = files.Where(lambda).ToList();
                Console.WriteLine(files[0].Name);
            }
            catch (Exception e)
            {
                throw new NotImplementedException();
            }

            return result;
        }

        public static List<File> GetAllProgramFiles()
        {
            bool allFilesReaded = false;

            string nextPageToken = null;

            var programFiles = new List<File>();

           

            return FindFiles(f => f.Parents != null && f.Parents.Any(p => p == _programFolderId), firstPage: true);
        }
        #endregion

        #region OnlineActions

        private static string GetMimeType(string fileName)
        {
            string mimeType = "application/unknown";
            string ext = System.IO.Path.GetExtension(fileName).ToLower();
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
            if (regKey != null && regKey.GetValue("Content Type") != null)
                mimeType = regKey.GetValue("Content Type").ToString();
            return mimeType;
        }

        public static void UploadFile(string filename)
        {

            // Define parameters of request.
            FilesResource.ListRequest listRequest = _service.Files.List();
            listRequest.PageSize = 10;
            listRequest.Fields = "nextPageToken, files(id, name)";

            // List files.
            IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute()
                .Files;

            string mainFolderName = "MyRaid";
            string mainFolderId = null;

            if (!files.Any(f => f.Name == mainFolderName))
            {
                File mainFolder = new File();
                mainFolder.Name = mainFolderName;
                mainFolder.MimeType = "application/vnd.google-apps.folder";

                // _service is an authorized Drive API _service instance
                File file = _service.Files.Create(mainFolder).Execute();

            }

            File parentFolder = files.FirstOrDefault(f => f.Name == mainFolderName);
            mainFolderId = parentFolder.Id;



            List<string> parents = new List<string>() { mainFolderId };
            UploadFile(filename, parents);
        }

        private static void UploadFile(string fileName, List<string> parents, string _descrp = "Uploaded with .NET!")
        {
            if (System.IO.File.Exists(fileName))
            {
                Google.Apis.Drive.v3.Data.File body = new Google.Apis.Drive.v3.Data.File();
                body.Name = System.IO.Path.GetFileName(fileName);
                body.Description = _descrp;
                body.MimeType = GetMimeType(fileName);
                body.Parents = parents;

                byte[] byteArray = System.IO.File.ReadAllBytes(fileName);
                System.IO.MemoryStream stream = new System.IO.MemoryStream(byteArray);
                try
                {
                    FilesResource.CreateMediaUpload request = _service.Files.Create(body, stream, GetMimeType(fileName));
                    request.Upload();
                    var winik = request.ResponseBody;
                }
                catch (Exception e)
                {
                    var a = e.Message;
                }
            }
            else
            {
                var b = "nie ma pliku";
            }

        }

        public static void UploadAndDeleteFile(string filename)
        {
            UploadFile(filename);
            FileHelper.deleteFile(filename);
        }

        public static string DownloadAndUnzipPackage(string fileId, string fileName)
        {
            DownloadFile(fileId, fileName);
            return FileHelper.UnZipPackage(fileName);
        }

        private static void DownloadFile(string fileId, string fileName)
        {

            var request = _service.Files.Get(fileId);
            var stream = new System.IO.MemoryStream();

            // Add a handler which will be notified on progress changes.
            // It will notify on each chunk download and when the
            // download is completed or failed.
            request.MediaDownloader.ProgressChanged += (Google.Apis.Download.IDownloadProgress progress) =>
            {
                switch (progress.Status)
                {
                    case Google.Apis.Download.DownloadStatus.Downloading:
                    {
                        Console.WriteLine(progress.BytesDownloaded);
                        break;
                    }
                    case Google.Apis.Download.DownloadStatus.Completed:
                    {
                        Console.WriteLine("Download complete.");
                        SaveStream(stream, fileName);
                        break;
                    }
                    case Google.Apis.Download.DownloadStatus.Failed:
                    {
                        Console.WriteLine("Download failed.");
                        break;
                    }
                }
            };
            request.Download(stream);

        }

        private static void SaveStream(System.IO.MemoryStream stream, string saveTo)
        {
            using (System.IO.FileStream file = new System.IO.FileStream(saveTo, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                stream.WriteTo(file);
            }
        }

        public static void DeleteFile(string fileId)
        {
            var request = _service.Files.Delete(fileId);
            request.Execute();
        }

        #endregion
    }
}