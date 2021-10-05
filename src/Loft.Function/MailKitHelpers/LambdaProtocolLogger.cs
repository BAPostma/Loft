using System;
using System.IO;
using Loft.Function.Models;
using MailKit;

namespace Loft.Function.MailKitHelpers
{
    public class LambdaProtocolLogger : IProtocolLogger
    {
        private readonly Action<string> _log;

        public IAuthenticationSecretDetector AuthenticationSecretDetector { get; set; }

        public LambdaProtocolLogger(Action<string> logger) => _log = logger;

        public void Dispose() { }

        public void LogConnect(Uri uri) => _log($"Connected to IMAP server: {uri}");
        
        public void LogClient(byte[] buffer, int offset, int count) => LogFromBuffer(buffer, offset, count, "C: ");

        public void LogServer(byte[] buffer, int offset, int count) => LogFromBuffer(buffer, offset, count, "S: ");

        private void LogFromBuffer(byte[] buffer, int offset, int count, string prefix = null)
        {
            if(buffer == null || !Configuration.IMAPDebugLogging) return;

            using var ms = new MemoryStream(buffer, offset, count, false);
            using var sr = new StreamReader(ms);
            var message = sr.ReadToEnd();
            
            _log($"{prefix}{message}");
        }
    }
}