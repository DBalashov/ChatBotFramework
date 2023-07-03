namespace ChatBotFramework;

public interface IChatBotHandler<in UID>
{
    Task<ChatBotResponse> Handle(UID userId, ChatBotRequest request);
}

public sealed record ChatBotRequest(string OriginalMessage, string? Command, string? Arguments, ChatBotRequestFile[] Files);

public sealed record ChatBotRequestFile(string FileName, byte[] Content);