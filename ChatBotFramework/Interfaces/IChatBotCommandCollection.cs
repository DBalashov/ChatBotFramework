namespace ChatBotFramework;

interface IChatBotCommandCollection<in UID, in MODEL, in STYPE> where STYPE : notnull
{
    IChatBotCommand<UID, MODEL>? GetCommandHandler(IServiceProvider serviceProvider, string command);

    IChatBotCommand<UID, MODEL>? GetStateHandler(IServiceProvider serviceProvider, STYPE state);
}