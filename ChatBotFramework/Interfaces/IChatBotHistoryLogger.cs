namespace ChatBotFramework;

public interface IChatBotHistoryLogger<UID> where UID : notnull
{
    Task Log(ChatBotHistoryItem<UID> item);
}

public enum ChatBotHistoryAction
{
    User = 0,
    Bot  = 1
}

/// <param name="DT">UTC</param>
public sealed record ChatBotHistoryItem<UID>(UID UserId, ChatBotHistoryAction Action, DateTime DT, string Message, int Files = 0) where UID : notnull;