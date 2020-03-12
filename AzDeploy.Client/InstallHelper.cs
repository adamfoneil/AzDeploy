using AzDeploy.Build.Models;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AzDeploy.Client
{
    public abstract class InstallHelper
    {
        private readonly Uri _uri;
        private readonly Version _localVersion;
        private readonly string _installerExe;

        public InstallHelper(Version localVerison, string account, string container, string installerExe)
        {
            _localVersion = localVerison;
            _uri = GetBlobUri(account, container, installerExe);
            _installerExe = installerExe;
        }

        public async Task<VersionCheck> GetVersionCheckAsync()
        {
            var blob = new CloudBlockBlob(_uri);
            var remoteVersion = (await blob.ExistsAsync()) ?
                Version.Parse(blob.Metadata[VersionCheck.VersionMetadata]) :
                Version.Parse("0.0.0.0");

            return new VersionCheck()
            {
                IsNew = (remoteVersion > _localVersion),
                Version = remoteVersion
            };
        }

        public async Task AutoInstallAsync()
        {
            var check = await GetVersionCheckAsync();
            if (check.IsNew)
            {
                if (!PromptDownloadAndExit()) return;

                var localFile = await DownloadInstallerAsync();

                ProcessStartInfo psi = new ProcessStartInfo(localFile);
                Process.Start(psi);
                ExitApplication();
            }
        }

        public async Task<string> DownloadInstallerAsync()
        {
            string localFile = GetDownloadFilename();
            if (File.Exists(localFile)) File.Delete(localFile);

            var blob = new CloudBlockBlob(_uri);
            await blob.DownloadToFileAsync(localFile, FileMode.CreateNew);

            return localFile;
        }

        protected abstract bool PromptDownloadAndExit();

        protected abstract void ExitApplication();

        protected virtual string GetDownloadFilename()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), _installerExe);
        }

        private static Uri GetBlobUri(string account, string container, string fileName)
        {
            string blobName = $"https://{account}.blob.core.windows.net:443/{container}/{fileName}";
            return new Uri(blobName);
        }
    }
}
