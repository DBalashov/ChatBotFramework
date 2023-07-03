namespace ChatBotFramework;

/// <summary> Must be compatible with <see cref="ChatBotStateHandlerDelegate{UID,MODEL}"/> </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ChatBotStateAttribute<STYPE> : Attribute
{
    public STYPE State { get; }

    public ChatBotStateAttribute(STYPE state) => State = state;
}