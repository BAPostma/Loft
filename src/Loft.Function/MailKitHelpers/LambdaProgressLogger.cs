using System;
using Humanizer;
using Loft.Function.Models;
using MailKit;

namespace Loft.Function.MailKitHelpers
{
    public class LambdaProgressLogger : ITransferProgress
    {
        private readonly Action<string> _log;

        public LambdaProgressLogger(Action<string> logger) => _log = logger;

        public void Report(long bytesTransferred, long totalSize)
        {
            if(!Configuration.IMAPDebugLogging) return;

            float progress = (float)bytesTransferred / (float)totalSize;
            _log($"Transferred {bytesTransferred}/{totalSize} bytes ({progress:P1})");
        }

        public void Report(long bytesTransferred) 
        {
            if(!Configuration.IMAPDebugLogging) return;
            
            _log($"Transferred {bytesTransferred.Bytes().Humanize()}");
        }
    }
}