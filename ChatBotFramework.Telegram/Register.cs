namespace ChatBotFramework.Telegram;

public static class Register
{
    public static IServiceCollection AddChatBotTelegram<MODEL, STYPE>(this IServiceCollection s) where MODEL : IChatBotModel<STYPE>
                                                                                                 where STYPE : notnull
    {
        s.AddSingleton<IChatBotMessageProcessor, ChatBotMessageProcessor>();
        s.AddSingleton<ChatBotTelegramService<MODEL, STYPE>>();
        s.AddHostedService<ChatBotTelegramService<MODEL, STYPE>>();
        return s;
    }
}