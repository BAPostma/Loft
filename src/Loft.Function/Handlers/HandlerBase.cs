using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SimpleEmailEvents.Actions;
using static Amazon.Lambda.SimpleEmailEvents.SimpleEmailEvent<Amazon.Lambda.SimpleEmailEvents.Actions.S3ReceiptAction>;

namespace Loft.Function.Handlers
{
    public abstract class HandlerBase : IHandler
    {
        protected IHandler NextHandler { get; private set; }
        protected static string LogPrefix { get; set;}

        public virtual IHandler SetNext(IHandler handler) => NextHandler = handler; // returns assigned handler

        public abstract Task Handle(SimpleEmailService<S3ReceiptAction> message, ILambdaContext context);

        public virtual async Task Next(SimpleEmailService<S3ReceiptAction> message, ILambdaContext context)
        {
            if(NextHandler == null) return;
            await NextHandler.Handle(message, context);
        }

        public virtual void Log(string message) => LambdaLogger.Log($"{LogPrefix}[{this.GetType().Name.Replace("Handler", string.Empty)}] {message}");
    }
}