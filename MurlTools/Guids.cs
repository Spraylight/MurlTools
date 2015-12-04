// Guids.cs
// MUST match guids.h
using System;

namespace Spraylight.MurlTools
{
    static class GuidList
    {
        public const string guidMurlToolsPkgString = "20c3d528-1bd2-4d75-972b-410659112b6c";
        public const string guidDuplicateFileCmdSetString = "ffb543ee-2689-4b81-9b2b-1039f445c3a3";
        public const string guidRefreshCmdSetString = "6367E3B5-0D8B-4611-B42E-01DA8D19390C";
        public const string guidShowHelpCmdSetString = "8C069995-DA2F-4C6E-9807-ABA2FDC58253";

        public static readonly Guid guidDuplicateFileCmdSet = new Guid(guidDuplicateFileCmdSetString);
        public static readonly Guid guidRefreshCmdSet = new Guid(guidRefreshCmdSetString);
        public static readonly Guid guidShowHelpCmdSet = new Guid(guidShowHelpCmdSetString);
    };
}