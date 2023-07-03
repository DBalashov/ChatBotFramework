namespace ChatBotFramework;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ChatBotStateAttribute<STYPE> : Attribute
{
    public STYPE State { get; }

    public ChatBotStateAttribute(STYPE state) => State = state;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ChatBotCommandAttribute : Attribute
{
    public string  Command     { get; }
    public string? Description { get; }

    public ChatBotCommandAttribute(string command, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(command);
        Command     = command;
        Description = description;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class ChatBotDefaultHandler : Attribute
{
}