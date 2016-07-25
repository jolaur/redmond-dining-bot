using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace msftbot
{
    [Serializable]
    public class CafeNotFoundDialog : IDialog<object>
    {
        protected int count = 1;

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public virtual async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;

            PromptDialog.Confirm(
                context,
                AfterResetAsync,
                "Cafe not found, would you like to see a list of all MSFT cafes (yes/no)?",
                promptStyle: PromptStyle.None);
        }

        public async Task AfterResetAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                this.count = 1;
                string cafes = await MessagesController.GetAllCafes();
                await context.PostAsync(cafes);
            }
        }

    }
}