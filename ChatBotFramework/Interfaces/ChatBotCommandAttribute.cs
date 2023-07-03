namespace ChatBotFramework;

/// <summary> Must be compatible with <see cref="ChatBotCommandHandlerDelegate{UID,MODEL}"/> </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ChatBotCommandAttribute : Attribute
{
    public string Command { get; }

    public ChatBotCommandAttribute(string command)
    {
        ArgumentNullException.ThrowIfNull(command);
        Command = command;
    }
}

public delegate Task<ChatBotResponse> ChatBotCommandHandlerDelegate<in UID, in MODEL>(UID userId, MODEL model, ChatBotRequest request);