using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

#pragma warning disable CS1998
#pragma warning disable CS4014

namespace ChatBotFramework.Telegram;

sealed class ChatBotTelegramService<MODEL, STYPE> : IHostedService, IUpdateHandler where MODEL : IChatBotModel<STYPE>
                                                                                   where STYPE : notnull
{
    const int FILE_SIZE_LIMIT = 20 * 1024 * 1024;

    readonly ILogger                  logger;
    readonly IChatBotMessageProcessor messageProcessor;
    readonly IGroupedMessageService   groupedMessageService;
    readonly TelegramBotClient        bot;
    readonly string                   logPrefix;

    public ChatBotTelegramService(ILogger<ChatBotTelegramService<MODEL, STYPE>> logger,
                                  ChatBotTelegramOptions                        options,
                                  IChatBotMessageProcessor                      messageProcessor,
                                  IGroupedMessageService                        groupedMessageService)
    {
        this.logger                = logger;
        this.messageProcessor      = messageProcessor;
        this.groupedMessageService = groupedMessageService;
        bot                        = new TelegramBotClient(options.Token);

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
                var file = u.Message.Photo.Where(p => p.FileSize is <= FILE_SIZE_LIMIT).MaxBy(p => p.FileSize ?? 0); // file size for bots limited to 20 MB
                if (file != null)
                    await groupedMessageService.ProcessOrAddToGroup(bot, u.Message.MediaGroupId, u, file.Convert(), logContextPrefix);
                else
                {
                    logger.LogWarning("[{0}] Photo not found or file size not specified or oversized ({1} bytes max file size)", logContextPrefix, FILE_SIZE_LIMIT);
                    messageProcessor.HandleMessageAsync(bot, u.ConvertToParams());
                }

                return;

            case UpdateType.Message when u is {Type: UpdateType.Message, Message: {Type: MessageType.Document, From: not null, Document: not null}}:
                if (u.Message.Document.FileSize is <= FILE_SIZE_LIMIT) // file size for bots limited to 20 MB
                {
                    await groupedMessageService.ProcessOrAddToGroup(bot, u.Message.MediaGroupId, u, u.Message.Document.Convert(), logContextPrefix);
                }
                else
                {
                    logger.LogWarning("[{0}] Document file size not specified or oversized ({1} bytes max file size)", logContextPrefix, FILE_SIZE_LIMIT);
                    messageProcessor.HandleMessageAsync(bot, u.ConvertToParams());
                }

                return;
        }

        logger.LogWarning("[{0}] Unknown message: {1}", logContextPrefix, u.Message?.Text);
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError("[{0}] Error while polling: {1}", logPrefix, exception.Message);
        logger.LogDebug("[{0}] Error while polling: {1}", logPrefix, exception.StackTrace);
        logger.LogWarning("[{0}] Restart polling after 5 seconds...", logPrefix);
        await Task.Delay(5000, cancellationToken);
        StartAsync(cancellationToken); // restart polling, intent without awaiting
    }

    #endregion
}