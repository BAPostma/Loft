using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.SimpleEmailEvents.Actions;
using static Amazon.Lambda.SimpleEmailEvents.SimpleEmailEvent<Amazon.Lambda.SimpleEmailEvents.Actions.S3ReceiptAction>;

namespace Loft.Function.Handlers
{
    public interface IHandler
    {
        IHandler SetNext(IHandler handler);

        Task Handle(SimpleEmailService<S3ReceiptAction> message, ILambdaContext context);
    }
}