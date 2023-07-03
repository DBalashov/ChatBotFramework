using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

#pragma warning disable CS1998
#pragma warning disable CS4014

namespace ChatBotFramework.Telegram;

sealed class ChatBotTelegramService<MODEL, STYPE> : IHostedService, IUpdateHandler where MODEL : ChatBotModelBase<STYPE>
                                                                                   where STYPE : notnull
{
    readonly ILogger                  logger;
    readonly IChatBotMessageProcessor messageProcessor;
    readonly TelegramBotClient        bot;
    readonly string                   logPrefix;
    readonly TimeSpan                 groupedMessageInterval;

    readonly Dictionary<string, GroupedMessage> groupedMessages     = new(StringComparer.Ordinal);
    readonly ReaderWriterLockSlim               groupedMessagesLock = new();

    public ChatBotTelegramService(ILogger<ChatBotTelegramService<MODEL, STYPE>> logger, ChatBotTelegramOptions options, IChatBotMessageProcessor messageProcessor)
    {
        this.logger            = logger;
        this.messageProcessor  = messageProcessor;
        bot                    = new TelegramBotClient(options.Token);
        groupedMessageInterval = options.GroupedMessageInterval;

        var idx = options.Token.IndexOf(':');
        logPrefix = idx > 0 ? options.Token[..idx] : options.Token[..Math.Min(12, options.Token.Length)] + "xxx";
    }

    #region Start / Stop

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("[{0}] Starting...", logPrefix);

        await bot.DeleteWebhookAsync(true, cancellationToken);

        bot.StartReceiving(this,
                           cancellationToken: cancellationToken,
                           receiverOptions: new ReceiverOptions() {AllowedUpdates = new[] {UpdateType.Message, UpdateType.CallbackQuery}});
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("[{0}] Stopping...", logPrefix);
        await bot.CloseAsync(cancellationToken);
    }

    #endregion

    #region HandleUpdateAsync / HandlePollingErrorAsync

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update u, CancellationToken cancellationToken)
    {
        var logContextPrefix = $"<{(u.CallbackQuery?.Message ?? u.Message)?.From?.GetUserName()}/{u.Type}";

        switch (u.Type)
        {
            case UpdateType.CallbackQuery when u is {Type: UpdateType.CallbackQuery, CallbackQuery.From: not null}:
                messageProcessor.HandleMessageAsync(bot, u.CallbackQuery.ConvertToParams(u)); // intent without awaiting
                return;

            case UpdateType.Message when u is {Type: UpdateType.Message, Message: {Type: MessageType.Text, From: not null}} && !string.IsNullOrWhiteSpace(u.Message.Text):
                messageProcessor.HandleMessageAsync(bot, u.ConvertToParams()); // intent without awaiting
                return;

            case UpdateType.Message when u is {Type: UpdateType.Message, Message: {Type: MessageType.Photo, From: not null, Photo: not null}}:
                var fileId = u.Message.Photo.OrderByDescending(p => p.FileSize ?? 0).First().FileId;
                await processOrAddToGroup(u.Message.MediaGroupId, u, fileId, logContextPrefix);
                return;

            case UpdateType.Message when u is {Type: UpdateType.Message, Message: {Type: MessageType.Document, From: not null, Document: not null}}:
                await processOrAddToGroup(u.Message.MediaGroupId, u, u.Message.Document.FileId, logContextPrefix);
                return;
        }

        logger.LogWarning("[{0}] Unknown message: {1}", logContextPrefix, u.Message?.Text);
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError((exception.InnerException ?? exception).Message);
        logger.LogWarning("Restart polling after 5 seconds...");
        await Task.Delay(5000, cancellationToken);
        StartAsync(cancellationToken); // restart polling, intent without awaiting
    }

    #endregion

    async Task processOrAddToGroup(string? mediaGroupId, Update u, string fileId, string logContextPrefix)
    {
        if (string.IsNullOrEmpty(mediaGroupId))
        {
            logger.LogDebug("[{0}] media group empty, handle immediately", logContextPrefix);
            var file = await bot.TryDownloadFile(logger, fileId, logContextPrefix);
            messageProcessor.HandleMessageAsync(bot, u.ConvertToParams(file != null ? new[] {file} : Array.Empty<ChatBotRequestFile>()));
            return;
        }

        groupedMessagesLock.EnterUpgradeableReadLock();
        if (!groupedMessages.TryGetValue(mediaGroupId, out var groupedMessage))
        {
            groupedMessagesLock.EnterWriteLock();
            if (!groupedMessages.TryGetValue(mediaGroupId, out groupedMessage))
            {
                logger.LogDebug("[{0}] media group not found, create one {1}", logContextPrefix, mediaGroupId);
                groupedMessages.Add(mediaGroupId, groupedMessage = new GroupedMessage(mediaGroupId, groupedMessageInterval, u.ConvertToParams(), logContextPrefix, completeMediaGroup));
            }

            groupedMessagesLock.ExitWriteLock();
        }
        else
        {
            logger.LogDebug("[{0}] media group found {1}", logContextPrefix, mediaGroupId);
        }

        groupedMessagesLock.ExitUpgradeableReadLock();
        logger.LogDebug("[<{0}] media group {1}, add file {2}", logContextPrefix, mediaGroupId, fileId);
        groupedMessage.AddFile(fileId);
    }

    async Task completeMediaGroup(GroupedMessage owner, HandleMessageParams p, string[] fileIDs, string logContextPrefix)
    {
        groupedMessagesLock.EnterWriteLock();
        var existing = groupedMessages.Remove(owner.MediaGroupId);
        logger.LogDebug("[{0}] media group {1} remove ({2})", logContextPrefix, owner.MediaGroupId, existing);
        groupedMessagesLock.ExitWriteLock();

        logger.LogDebug("[{0}] media group {1} complete, {2} files", logContextPrefix, owner.MediaGroupId, fileIDs.Length);

        var files = (await Task.WhenAll(fileIDs.Select(fileId => bot.TryDownloadFile(logger, fileId, logContextPrefix)).ToArray())).Where(p => p != null).ToArray();
        messageProcessor.HandleMessageAsync(bot, p with {Files = files!});
    }
}