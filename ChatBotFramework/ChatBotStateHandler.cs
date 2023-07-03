namespace ChatBotFramework;

public interface IChatBotModelHandler<in UID, in MODEL, in STYPE> where MODEL : ChatBotModelBase<STYPE>
                                                                  where STYPE : notnull
{
    /// <summary> default handler for unknown commands </summary>
    Task<ChatBotResponse> Handle(UID userId, MODEL model, ChatBotRequest request);
}