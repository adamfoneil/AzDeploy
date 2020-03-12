using Microsoft.Azure.Storage.Blob;
using System;

namespace AzDeploy.Cmd.Models
{
    internal class VersionCheck
    {
        public bool IsNew { get; set; }
        public Version Version { get; set; }        
    }
}
