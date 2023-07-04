using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace ChatBotFramework.Telegram;

/// <summary> singleton </summary>
sealed class ChatBotMessageProcessor : IChatBotMessageProcessor
{
    const int MEDIA_CHUNK_SIZE = 10;

    readonly ILogger              logger;
    readonly IServiceScopeFactory serviceScopeFactory;

    public ChatBotMessageProcessor(ILogger<ChatBotMessageProcessor> logger,
                                   IServiceScopeFactory             serviceScopeFactory)
    {
        this.logger              = logger;
        this.serviceScopeFactory = serviceScopeFactory;
    }

    public async Task HandleMessageAsync(ITelegramBotClient bot, HandleMessageParams p)
    {
        var messageText = p.MessageText ?? "";

        logger.LogDebug("[<{0}] {1} {2} (files[{3}]={4})",
                        p.LogPrefix, p.RequestId, messageText, p.Files.Length, string.Join(',', p.Files.Select(f => f.FileName + "(" + f.Content.Length + " bytes)")));

        using var scope = serviceScopeFactory.CreateScope();
        try
        {
            var h = scope.ServiceProvider.GetRequiredService<IChatBotHandler<Int64>>();
            var response = await h.Handle(p.From.Id,
                                          messageText.StartsWith('/')
                                              ? messageText.Substring(1).ParseMessage(p.Files)
                                              : new ChatBotRequest(messageText, null, null, p.Files));
            await processResponse(bot, response, p);
        }
        catch (Exception e)
        {
            logger.LogWarning("[<{0}] {1} Can't process: {2}", p.LogPrefix, p.RequestId, (e.InnerException ?? e).Message);
            await processFail(bot, p);
        }
    }

    #region processFail / processResponse

    Task processFail(ITelegramBotClient bot, HandleMessageParams p)
    {
        try
        {
            return bot.SendTextMessageAsync(p.Chat, $"Server error ({p.RequestId})", replyToMessageId: p.ReplyMessageId);
        }
        catch (ApiRequestException apiexc) when (apiexc.ReplyMessageDeleted())
        {
            return bot.SendTextMessageAsync(p.Chat, $"Server error ({p.RequestId})");
        }
    }

    async Task processResponse(ITelegramBotClient bot, ChatBotResponse response, HandleMessageParams p)
    {
        var images           = new List<IAlbumInputMedia>();
        var messageIDs       = new List<int>();
        var replyToMessageId = p.ReplyMessageId;
        foreach (var respItem in response.Messages)
        {
            var buttons     = respItem.Buttons.Any() ? $" (buttons: {string.Join(',', respItem.Buttons.Select(x => x.Command + ":" + x.Text))})" : "";
            var replyMarkup = respItem.Buttons.ToInlineKeyboardMarkup();
            switch (respItem)
            {
                case ChatBotMessage msg:
                    logger.LogDebug("[>{0}] {1}{2}", p.LogPrefix, msg.Message, buttons);
                    try
                    {
                        var messageOut = await bot.SendTextMessageAsync(p.Chat, msg.Message, parseMode: msg.GetParseMode(), replyMarkup: replyMarkup, replyToMessageId: replyToMessageId);
                        messageIDs.Add(messageOut.MessageId);
                    }
                    catch (ApiRequestException e) when (e.ReplyMessageDeleted())
                    {
                        replyToMessageId = null;
                        var messageOut = await bot.SendTextMessageAsync(p.Chat, msg.Message, parseMode: msg.GetParseMode(), replyMarkup: replyMarkup);
                        messageIDs.Add(messageOut.MessageId);
                    }

                    break;

                case ChatBotFile:
                case ChatBotFileUrl:
                    logger.LogDebug("[>{0}] {1} {2}{3}", p.LogPrefix, p.RequestId, "File", buttons);
                    var documents = respItem.ConvertToDocument();
                    try
                    {
                        var messageOut = await bot.SendDocumentAsync(p.Chat, documents, replyMarkup: replyMarkup, replyToMessageId: replyToMessageId);
                        messageIDs.Add(messageOut.MessageId);
                    }
                    catch (ApiRequestException e) when (e.ReplyMessageDeleted())
                    {
                        replyToMessageId = null;
                        var messageOut = await bot.SendDocumentAsync(p.Chat, documents, replyMarkup: replyMarkup);
                        messageIDs.Add(messageOut.MessageId);
                    }
                    break;

                case ChatBotImage:
                case ChatBotImageUrl:
                case ChatBotVideo:
                case ChatBotVideoUrl:
                    images.Add(respItem.ConvertToMedia());
                    break;
                default:
                    logger.LogWarning("[>{0}] {1} Unknown message type: {2}", p.LogPrefix, p.RequestId, respItem.GetType().Name);
                    break;
            }
        }

        if (images.Any())
        {
            logger.LogDebug("[>{0}] {1} {2} ({3} images)", p.LogPrefix, p.RequestId, "Images", images.Count);
            foreach (var chunk in images.Chunk(MEDIA_CHUNK_SIZE))
                try
                {
                    var messagesOut = await bot.SendMediaGroupAsync(p.Chat, chunk, replyToMessageId: replyToMessageId);
                    messageIDs.AddRange(messagesOut.Select(p => p.MessageId));
                }
                catch (ApiRequestException e) when (e.ReplyMessageDeleted())
                {
                    replyToMessageId = null;
                    var messagesOut = await bot.SendMediaGroupAsync(p.Chat, chunk);
                    messageIDs.AddRange(messagesOut.Select(p => p.MessageId));
                }
        }

        logger.LogDebug("[>{0}] {1} Message IDs: {2}", p.LogPrefix, p.RequestId, string.Join(',', messageIDs));
    }

    #endregion
}