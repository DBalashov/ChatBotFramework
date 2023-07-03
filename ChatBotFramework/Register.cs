using System.Reflection;

namespace ChatBotFramework;

public static class Register
{
    public static IServiceCollection AddChatBot<UID, MODEL, STYPE, PERSISTER>(this IServiceCollection s) where MODEL : ChatBotModelBase<STYPE>, new()
                                                                                                         where PERSISTER : class, IChatBotModelStorage<UID, MODEL, STYPE>
                                                                                                         where UID : notnull
                                                                                                         where STYPE : notnull
    {
        s.AddScoped<IChatBotModelStorage<UID, MODEL, STYPE>, PERSISTER>();
        s.AddScoped<IChatBotHandler<UID>, ChatBotHandler<UID, MODEL, STYPE>>();
        s.AddSingleton<IChatBotCommandCollection<UID, MODEL, STYPE>, ChatBotCommandCollection<UID, MODEL, STYPE>>();
        return s;
    }

    public static IServiceCollection AddChatBotCommand<UID, MODEL, STYPE, HANDLER>(this IServiceCollection s) where HANDLER : class, IChatBotCommand<UID, MODEL>
    {
        s.AddScoped<IChatBotCommand<UID, MODEL>, HANDLER>();

        var count = 0;

        foreach (var attCommand in typeof(HANDLER).GetCustomAttributes<ChatBotCommandAttribute>())
        {
            s.AddScoped<HANDLER>();
            s.AddSingleton(new ChatBotCommandWrapper<UID, MODEL>(attCommand.Command, typeof(HANDLER)));
            count++;
        }

        foreach (var attState in typeof(HANDLER).GetCustomAttributes<ChatBotStateAttribute<STYPE>>())
        {
            s.AddScoped<HANDLER>();
            s.AddSingleton(new ChatBotStateWrapper<UID, MODEL, STYPE>(attState.State, typeof(HANDLER)));
            count++;
        }

        if (count == 0)
            throw new InvalidDataException($"ChatBotCommandAttribute and/or ChatBotStateAttribute<{typeof(STYPE)}> not found on class {typeof(HANDLER).FullName}");

        return s;
    }
}