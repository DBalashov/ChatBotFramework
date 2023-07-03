using System.Reflection;

namespace ChatBotFramework;

static class Extenders
{
    public static Task<ChatBotResponse> InvokeHandler<UID, MODEL, STYPE>(this IChatBotModelHandler<UID, MODEL, STYPE> instance,
                                                                         UID                                          userId,
                                                                         MODEL                                        model,
                                                                         ChatBotRequest                               request)
        where MODEL : ChatBotModelBase<STYPE>
        where UID : notnull
        where STYPE : notnull
    {
        var methodInfo = instance.GetType().getMethodInfo<ChatBotStateAttribute<STYPE>>(attr => model.State.Equals(attr.State));
        if (methodInfo != null)
            return (Task<ChatBotResponse>) methodInfo.Invoke(instance, new object[] {userId, model, request})!;

        if (request.Command != null)
        {
            methodInfo = instance.GetType().getMethodInfo<ChatBotCommandAttribute>(attr => string.Compare(request.Command, attr.Command, StringComparison.OrdinalIgnoreCase) == 0);
            if (methodInfo != null)
                return (Task<ChatBotResponse>) methodInfo.Invoke(instance, new object[] {userId, model, request})!;
        }

        return instance.Handle(userId, model, request);
    }

    static MethodInfo? getMethodInfo<ATTR>(this Type t, Func<ATTR, bool> filter) where ATTR : Attribute
    {
        foreach (var m in t.GetMethods())
        {
            var attrs = m.GetCustomAttributes<ATTR>();
            if (attrs.Any(filter)) return m;
        }

        return null;
    }
}