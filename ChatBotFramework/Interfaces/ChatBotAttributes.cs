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
    public string Command { get; }

    public ChatBotCommandAttribute(string command)
    {
        ArgumentNullException.ThrowIfNull(command);
        Command = command;
    }
}