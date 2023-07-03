namespace ChatBotFramework;

/// <summary> scoped service </summary>
sealed class ChatBotHandler<UID, MODEL, STYPE> : IChatBotHandler<UID> where MODEL : ChatBotModelBase<STYPE>, new()
                                                                      where UID : notnull
                                                                      where STYPE : notnull
{
    readonly ILogger                                      logger;
    readonly IChatBotModelStorage<UID, MODEL, STYPE>      modelStorage;
    readonly IServiceProvider                             serviceProvider;
    readonly IChatBotCommandCollection<UID, MODEL, STYPE> commandCollection;

    public ChatBotHandler(ILogger<ChatBotHandler<UID, MODEL, STYPE>>   logger,
                          IChatBotModelStorage<UID, MODEL, STYPE>      modelStorage,
                          IServiceProvider                             serviceProvider,
                          IChatBotCommandCollection<UID, MODEL, STYPE> commandCollection)
    {
        this.logger            = logger;
        this.modelStorage      = modelStorage;
        this.serviceProvider   = serviceProvider;
        this.commandCollection = commandCollection;
    }

    #region Handle

    public async Task<ChatBotResponse> Handle(UID userId, ChatBotRequest request)
    {
        var historyLogger = serviceProvider.GetService<IChatBotHistoryLogger<UID>>();
        if (historyLogger != null)
            await historyLogger.Log(new ChatBotHistoryItem<UID>(userId, ChatBotHistoryAction.User, DateTime.UtcNow, request.OriginalMessage));

        logger.LogDebug("[{0}] Load model (command={1})", userId, request.Command);
        var model = await modelStorage.Load(userId);

        var handler = (request.Command != null
                           ? commandCollection.GetCommandHandler(serviceProvider, request.Command)
                           : commandCollection.GetStateHandler(serviceProvider, model.State)) ?? commandCollection.GetCommandHandler(serviceProvider, "*");
        
        if (handler == null)
        {
            logger.LogWarning("[{0}] Can't find handler for command={1} or state={2}", userId, request.Command, model.State);
            return ChatBotResponse.Text("Unknown command or state");
        }

        var response = await handler.Handle(userId, model, request);
        if (model.Modified)
        {
            model.Modified = false;
            logger.LogDebug("[{0}{1}] Model changed, save", userId, request.Command == null ? "" : $"/{request.Command}");
            await modelStorage.Save(userId, model);
        }

        await historyLog(historyLogger, userId, response);
        return response;
    }

    #endregion

    async Task historyLog(IChatBotHistoryLogger<UID>? historyLogger, UID userId, ChatBotResponse response)
    {
        if (!response.Messages.Any() || historyLogger == null) return;
        var dt         = DateTime.UtcNow;
        var filesCount = response.Messages.Count(p => p is not ChatBotMessage);
        foreach (var resp in response.Messages.OfType<ChatBotMessage>())
            await historyLogger.Log(new ChatBotHistoryItem<UID>(userId, ChatBotHistoryAction.Bot, dt, resp.Message, filesCount));
    }
}