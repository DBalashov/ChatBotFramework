namespace ChatBotFramework;

public interface IChatBotModelStorage<in UID, MODEL, in STYPE> where MODEL : ChatBotModelBase<STYPE>, new()
                                                               where STYPE : notnull
{
    Task Save(UID userId, MODEL model);

    Task<MODEL> Load(UID userId);
}