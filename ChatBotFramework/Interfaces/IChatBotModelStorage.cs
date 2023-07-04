namespace ChatBotFramework;

public interface IChatBotModelStorage<in UID, MODEL, in STYPE> where MODEL : class, IChatBotModel<STYPE>, new()
                                                               where STYPE : notnull
                                                               where UID : notnull
{
    Task Save(UID userId, MODEL model);

    Task<MODEL> Load(UID userId);
}