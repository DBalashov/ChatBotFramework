using System.Text.Json;

namespace ChatBotFramework;

/// <summary> scoped service </summary>
sealed class ChatBotHandler<UID, MODEL, STYPE> : IChatBotHandler<UID> where MODEL : ChatBotModelBase<STYPE>, new()
                                                                      where UID : notnull
                                                                      where STYPE : notnull
{
    readonly ILogger                                 logger;
    readonly IChatBotModelStorage<UID, MODEL, STYPE> modelStorage;
    readonly IServiceProvider                        serviceProvider;

    public ChatBotHandler(ILogger<ChatBotHandler<UID, MODEL, STYPE>> logger,
                          IChatBotModelStorage<UID, MODEL, STYPE>    modelStorage,
                          IServiceProvider                           serviceProvider)
    {
        this.logger          = logger;
        this.modelStorage    = modelStorage;
        this.serviceProvider = serviceProvider;
    }

    #region Handle

    public async Task<ChatBotResponse> Handle(UID userId, ChatBotRequest request)
    {
        var historyLogger = serviceProvider.GetService<IChatBotHistoryLogger<UID>>();
        if (historyLogger != null)
            await historyLogger.Log(new ChatBotHistoryItem<UID>(userId, ChatBotHistoryAction.User, DateTime.UtcNow, request.OriginalMessage));

        var handler = serviceProvider.GetRequiredService<IChatBotModelHandler<UID, MODEL, STYPE>>();
        logger.LogDebug("[{0}{1}] Load model", userId, request.Command == null ? "" : $"/{request.Command}");
        var model = await modelStorage.Load(userId);

        var response = await handler.InvokeHandler(userId, model, request);
        if (!model.Modified) return response;

        logger.LogDebug("[{0}{1}] Model changed, save", userId, request.Command == null ? "" : $"/{request.Command}");
        await modelStorage.Save(userId, model);

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