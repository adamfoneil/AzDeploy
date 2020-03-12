using System;

namespace AzDeploy.Build.Models
{    
    public class VersionCheck
    {
        public bool IsNew { get; set; }
        public Version Version { get; set; }

        public const string VersionMetadata = "version";
    }
}
