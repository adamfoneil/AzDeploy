using Newtonsoft.Json;

namespace AzDeploy.Build.Models
{
    public class DeployScript
    {
        /// <summary>
        /// file that defines the app version (intended as the main build output of your project)
        /// </summary>
        [JsonProperty("versionSourceFile")]
        public string VersionSourceFile { get; set; }

        /// <summary>
        /// where do we upload the installer package?
        /// </summary>
        [JsonProperty("storageAccount")]
        public StorageAccountInfo StorageAccount { get; set; }

        [JsonProperty("installer")]
        public InstallerInfo Installer { get; set; }

        /// <summary>
        /// setup executable that is uploaded to blob storage
        /// </summary>
        [JsonProperty("installerExe")]
        public string InstallerExe { get; set; }

        public class StorageAccountInfo
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("key")]
            public string Key { get; set; }

            [JsonProperty("container")]
            public string Container { get; set; }
        }

        public class InstallerInfo
        {
            /// <summary>
            /// command to execute to rebuild installer package
            /// </summary>
            [JsonProperty("command")]
            public string Command { get; set; }

            /// <summary>
            /// any arguments (in my case DeployMaster .deploy script) required by InstallerCommand
            /// </summary>
            [JsonProperty("arguments")]
            public string Arguments { get; set; }
        }
    }
}
