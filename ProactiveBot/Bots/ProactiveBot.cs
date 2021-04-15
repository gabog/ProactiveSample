// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace ProactiveBot.Bots
{
    public class ProactiveBot : ActivityHandler
    {
        // Message to send to users when the bot receives a Conversation Update event
        private const string WelcomeMessage = "Welcome to the Proactive Bot sample";
        private const string NotifyMessage = "Navigate to {0}api/notify?user={1}&message=Test to proactively message the user.";

        // Dependency injected dictionary for storing ConversationReference objects used in NotifyController to proactively message users
        private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferencesStore;

        private readonly Uri _serverUrl;

        public ProactiveBot(IHttpContextAccessor httpContextAccessor, ConcurrentDictionary<string, ConversationReference> conversationReferencesStore)
        {
            _conversationReferencesStore = conversationReferencesStore;
            _serverUrl = new Uri($"{httpContextAccessor.HttpContext.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host.Value}");
        }

        protected override Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            AddConversationReference(turnContext.Activity as Activity);

            return base.OnConversationUpdateActivityAsync(turnContext, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                // Greet anyone that was not the target (recipient) of this message.
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    // Remember the conversation reference so we can send proactive messages to the user.
                    AddConversationReference(turnContext.Activity as Activity);

                    // Greet the user and render message with continuation link.
                    await turnContext.SendActivityAsync(MessageFactory.Text(WelcomeMessage), cancellationToken);
                    await turnContext.SendActivityAsync(MessageFactory.Text(string.Format(NotifyMessage, _serverUrl, turnContext.Activity.From.Id)), cancellationToken);
                }
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Echo back what the user said and render message with continuation link.
            await turnContext.SendActivityAsync(MessageFactory.Text($"You sent '{turnContext.Activity.Text}'"), cancellationToken);
            await turnContext.SendActivityAsync(MessageFactory.Text(string.Format(NotifyMessage, _serverUrl, turnContext.Activity.From.Id)), cancellationToken);
        }

        /// <summary>
        /// Store the conversation reference for that user so we send a proactive message. 
        /// </summary>
        private void AddConversationReference(Activity activity)
        {
            var conversationReference = activity.GetConversationReference();
            _conversationReferencesStore.AddOrUpdate(conversationReference.User.Id, conversationReference, (key, newValue) => conversationReference);
        }
    }
}