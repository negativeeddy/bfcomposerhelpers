using AdaptiveExpressions.Properties;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NegativeEddy.Bots.Composer.Actions
{
    public class HelloWorld : Dialog
    {
        [JsonConstructor]
        public HelloWorld([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
          : base()
        {
            // enable instances of this command as debug break point
            RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        [JsonProperty("$kind")]
        public const string Kind = nameof(HelloWorld);

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            await dc.Context.SendActivityAsync("Hello World!");

            return await dc.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
