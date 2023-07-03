namespace ChatBotFramework;

public interface IChatBotModel<STYPE> where STYPE : notnull
{
    /// <summary> Must be set to true if the state has been modified after request processed </summary>
    public bool Modified { get; set; }
    
    public STYPE State { get; set; }
}
