using System.Reflection;

namespace ChatBotFramework;

public static class Register
{
    public static IServiceCollection AddChatBotHandler<UID, MODEL, STYPE, HANDLER, PERSISTER>(this IServiceCollection s) where MODEL : ChatBotModelBase<STYPE>, new()
                                                                                                                         where HANDLER : class, IChatBotModelHandler<UID, MODEL, STYPE>
                                                                                                                         where PERSISTER : class, IChatBotModelStorage<UID, MODEL, STYPE>
                                                                                                                         where UID : notnull
                                                                                                                         where STYPE : notnull
    {
        validateStateHandler<UID, MODEL, STYPE, HANDLER>();

        s.AddScoped<IChatBotModelHandler<UID, MODEL, STYPE>, HANDLER>();
        s.AddScoped<IChatBotModelStorage<UID, MODEL, STYPE>, PERSISTER>();
        s.AddScoped<IChatBotHandler<UID>, ChatBotHandler<UID, MODEL, STYPE>>();
        return s;
    }

    static void validateStateHandler<UID, MODEL, STYPE, HANDLER>()
    {
        checkCompatibleSignature<ChatBotCommandHandlerDelegate<UID, MODEL>, HANDLER, ChatBotCommandAttribute>();
        checkCompatibleSignature<ChatBotCommandHandlerDelegate<UID, MODEL>, HANDLER, ChatBotStateAttribute<STYPE>>();
    }

    static void checkCompatibleSignature<DELEGATE, HANDLER, ATTR>() where ATTR : Attribute
    {
        var dlgate         = typeof(DELEGATE).GetMethod("Invoke")!;
        var delegateParams = dlgate.GetParameters();
        foreach (var m in typeof(HANDLER).GetMethods().Where(p => p.GetCustomAttributes<ATTR>().Any()))
        {
            var parameters = m.GetParameters();
            if (parameters.Length != delegateParams.Length)
                throw new InvalidOperationException($"Method {m.Name} must have {delegateParams.Length} parameters, but has {parameters.Length} parameters");

            for (var i = 0; i < delegateParams.Length; i++)
                if (parameters[i].ParameterType != delegateParams[i].ParameterType)
                    throw new InvalidOperationException($"Method {m.Name} must have first parameter of type {delegateParams[i].ParameterType}, but has {parameters[i].ParameterType}");

            if (m.ReturnType != dlgate.ReturnType)
                throw new InvalidOperationException($"Method {m.Name} must return {dlgate.ReturnType}, but returns {m.ReturnType}");
        }
    }
}