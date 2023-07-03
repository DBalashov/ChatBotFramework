namespace ChatBotFramework;


sealed class ChatBotCommandCollection<UID, MODEL, STYPE> : IChatBotCommandCollection<UID, MODEL, STYPE> where STYPE : notnull
{
    readonly Dictionary<string, Type> commands = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<STYPE, Type>  states   = new();

    public ChatBotCommandCollection(IEnumerable<ChatBotCommandWrapper<UID, MODEL>> commands, IEnumerable<ChatBotStateWrapper<UID, MODEL, STYPE>> states)
    {
        foreach (var command in commands)
        {
            if (this.commands.TryGetValue(command.Command, out var existingHandler))
                throw new InvalidDataException($"Duplicate command handler for '{command.Command}': existing={existingHandler.FullName} and new={command.Handler.FullName}");
            this.commands.Add(command.Command, command.Handler);
        }

        foreach (var state in states)
        {
            if (this.states.TryGetValue(state.State, out var existingHandler))
                throw new InvalidDataException($"Duplicate state handler for '{state.State}': existing={existingHandler.FullName} and new={state.Handler.FullName}");
            this.states.Add(state.State, state.Handler);
        }
    }

    public IChatBotCommand<UID, MODEL>? GetCommandHandler(IServiceProvider serviceProvider, string command)
    {
        if (!commands.TryGetValue(command, out var handlerType)) return null;

        var h = serviceProvider.GetService(handlerType);
        if (h == null) return null;

        return h as IChatBotCommand<UID, MODEL> ?? throw new InvalidDataException($"Handler {handlerType.FullName} does not implement {typeof(IChatBotCommand<UID, MODEL>).FullName}");
    }

    public IChatBotCommand<UID, MODEL>? GetStateHandler(IServiceProvider serviceProvider, STYPE state)
    {
        if (!states.TryGetValue(state, out var handlerType)) return null;

        var h = serviceProvider.GetService(handlerType);
        if (h == null) return null;

        return h as IChatBotCommand<UID, MODEL> ?? throw new InvalidDataException($"Handler {handlerType.FullName} does not implement {typeof(IChatBotCommand<UID, MODEL>).FullName}");
    }
}