using System;

namespace Loft.Function.Models
{
    public static class Configuration
    {
        public static string DynamoDbTableName {
            get => Environment.GetEnvironmentVariable(nameof(DynamoDbTableName));
        }
        
        public static string IMAPServer {
            get => Environment.GetEnvironmentVariable(nameof(IMAPServer));
        }

        public static string IMAPUsername {
            get => Environment.GetEnvironmentVariable(nameof(IMAPUsername));
        }

        public static string IMAPPassword {
            get => Environment.GetEnvironmentVariable(nameof(IMAPPassword));
        }

        public static string IMAPDestinationFolder {
            get => Environment.GetEnvironmentVariable(nameof(IMAPDestinationFolder));
        }

        private static bool? _imapMarkAsRead;
        public static bool IMAPMarkAsRead {
            get
            {
                if (!_imapMarkAsRead.HasValue) _imapMarkAsRead = Boolean.Parse(Environment.GetEnvironmentVariable(nameof(IMAPMarkAsRead)));
                return _imapMarkAsRead.Value;
            }
        }
        
        private static bool? _imapDebugLogging;
        public static bool IMAPDebugLogging {
            get
            {
                if (!_imapDebugLogging.HasValue) _imapDebugLogging = Boolean.Parse(Environment.GetEnvironmentVariable(nameof(IMAPDebugLogging)));
                return _imapDebugLogging.Value;
            }
        }
    }
}