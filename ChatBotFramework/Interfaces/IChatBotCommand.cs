namespace ChatBotFramework;

public interface IChatBotCommand<in UID, in MODEL> where UID : notnull
                                                   where MODEL : class, new()
{
    Task<ChatBotResponse> Handle(UID userId, MODEL model, ChatBotRequest request);
}