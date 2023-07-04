using System.Diagnostics;

namespace ChatBotFramework;

interface IChatBotCommandCollection<in UID, in MODEL, in STYPE> where STYPE : notnull
                                                                where MODEL : class, IChatBotModel<STYPE>, new()
                                                                where UID : notnull
{
    IChatBotCommand<UID, MODEL>? GetCommandHandler(IServiceProvider serviceProvider, string command);

    IChatBotCommand<UID, MODEL>? GetStateHandler(IServiceProvider serviceProvider, STYPE state);
}

[DebuggerDisplay("{Command} -> {Handler}")]
sealed record ChatBotCommandWrapper<UID, MODEL>(string Command, Type Handler) where MODEL : class, new()
                                                                              where UID : notnull;

[DebuggerDisplay("{State} -> {Handler}")]
sealed record ChatBotStateWrapper<UID, MODEL, STYPE>(STYPE State, Type Handler) where MODEL : class, IChatBotModel<STYPE>, new()
                                                                                where STYPE : notnull
                                                                                where UID : notnull;