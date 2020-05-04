using AzDeploy.Build.Models;
using JsonSettings;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AzDeploy.Build
{
    class Program
    {        
        static async Task Main(string[] args)
        {
            var script = GetDeployScript(args);

            VersionCheck check = await CheckNewVersionAsync(script);
            if (check.IsNew)
            {
                Console.WriteLine($"Building new installer version {check.Version}");
                string installerArgs = script.Installer.Arguments.Replace("%version%", check.Version.ToString());
                var process = Process.Start(script.Installer.Command, installerArgs);
                process.WaitForExit();
                if (process.ExitCode == 1)
                {
                    await UploadInstallerAsync(script, check);
                }                
            }            
        }

        private static async Task UploadInstallerAsync(DeployScript script, VersionCheck check)
        {
            Console.WriteLine($"Uploading version {check.Version} installer...");
            var uri = new Uri(GetBlobNameFromFilename(script.InstallerExe, script.StorageAccount));
            var blob = new CloudBlockBlob(uri, new StorageCredentials(script.StorageAccount.Name, script.StorageAccount.Key));            
            await blob.UploadFromFileAsync(script.InstallerExe);
            blob.Metadata[VersionCheck.VersionMetadata] = check.Version.ToString();
            await blob.SetMetadataAsync();
        }

        private static void BuildInstaller(DeployScript script, Version version)
        {
            throw new NotImplementedException();
        }

        private static async Task<VersionCheck> CheckNewVersionAsync(DeployScript script)
        {
            Console.WriteLine("Checking for updated local version...");

            var localVersion = GetLocalVersion(script.VersionSourceFile);

            var uri = new Uri(GetBlobNameFromFilename(script.InstallerExe, script.StorageAccount));
            var blob = new CloudBlockBlob(uri);

            try
            {
                var remoteVersion = (await blob.ExistsAsync()) ?
                    Version.Parse(blob.Metadata[VersionCheck.VersionMetadata]) :
                    Version.Parse("0.0.0.0");

                return new VersionCheck()
                {
                    IsNew = (localVersion > remoteVersion),
                    Version = localVersion
                };
            }
            catch 
            {
                return new VersionCheck()
                {
                    IsNew = true,
                    Version = localVersion
                };
            }
        }        

        private static Version GetLocalVersion(string fileName)
        {
            try
            {
                var fv = FileVersionInfo.GetVersionInfo(fileName);
                return new Version(fv.FileVersion);
            }
            catch (Exception exc)
            {
                throw new Exception($"Failed to get version info from {fileName}: {exc.Message}");
            }
        }

        private static string GetBlobNameFromFilename(string fileName, DeployScript.StorageAccountInfo storageAccount)
        {
            return $"https://{storageAccount.Name}.blob.core.windows.net:443/{storageAccount.Container}/{Path.GetFileName(fileName)}";
        }

        private static DeployScript GetDeployScript(string[] args)
        {
            try
            {
                string fileName = args[0];
                return JsonFile.Load<DeployScript>(fileName);
            }
            catch (Exception exc)
            {
                throw new Exception($"Couldn't open deploy script: {exc.Message}");
            }
        }
    }
}
