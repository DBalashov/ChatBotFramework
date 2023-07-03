namespace ChatBotFramework;

public interface IChatBotCommand<in UID, in MODEL>
{
    Task<ChatBotResponse> Handle(UID userId, MODEL model, ChatBotRequest request);
}