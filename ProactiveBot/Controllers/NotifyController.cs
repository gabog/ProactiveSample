// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;

namespace ProactiveBot.Controllers
{
    [Route("api/notify")]
    [ApiController]
    public class NotifyController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly string _appId;
        private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferencesStore;

        public NotifyController(IBotFrameworkHttpAdapter adapter, IConfiguration configuration, ConcurrentDictionary<string, ConversationReference> conversationReferencesStore)
        {
            _adapter = adapter;
            _conversationReferencesStore = conversationReferencesStore;
            _appId = configuration["MicrosoftAppId"] ?? string.Empty;
        }

        public async Task<IActionResult> Get(string user, string message)
        {
            // Try to get a conversation reference for that user from the conversation reference store.
            _conversationReferencesStore.TryGetValue(user, out var continuationParameters);

            // Sample: this is how you would manually create a conversation reference for ACS to send an SMS to the user:
            // Note that this is only possible for SMS, for the other channels, the user needs to talk to the channel at least 
            // once so we can store a conversation reference for that user.
            //var continuationParameters = new ConversationReference
            //{
            //    User = new ChannelAccount(userSmsNumber),
            //    Bot = new ChannelAccount(config.GetSection("acsSmsAdapterSettings")["AcsPhoneNumber"], "bot"),
            //    Conversation = new ConversationAccount(false, null, userSmsNumber),
            //    ChannelId = "ACS_SMS"
            //};

            if (continuationParameters == null)
            {
                // We didn't find a conversation reference to the user.
                return new ContentResult
                {
                    Content = $"<html><body><h1>No messages sent</h1> <br/>There are no conversations registered to receive proactive messages for {user}.</body></html>",
                    ContentType = "text/html",
                    StatusCode = (int)HttpStatusCode.OK,
                };
            }

            Exception exception = null;
            try
            {
                // This anonymous function will be called from ContinueConversationAsync
                async Task ContinuationBotCallback(ITurnContext context, CancellationToken cancellationToken)
                {
                    await context.SendActivityAsync($"Got proactive message \"{message}\" for user: {user}", cancellationToken: cancellationToken);
                }

                // Continue the conversation with the proactive message
                await ((BotAdapter)_adapter).ContinueConversationAsync(_appId, continuationParameters, ContinuationBotCallback, default);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            // Let the caller know a proactive messages have been sent
            return new ContentResult
            {
                Content = $"<html><body><h1>Proactive messages have been sent</h1> <br/> Timestamp: {DateTime.Now} <br /> Exception: {exception}</body></html>",
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
            };
        }
    }
}