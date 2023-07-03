using System.Diagnostics;

namespace ChatBotFramework;

[DebuggerDisplay("State={State} (Modified={Modified})")]
public abstract class ChatBotModelBase<STYPE> where STYPE : notnull
{
    /// <summary> Must be set to true if the state has been modified after request processed </summary>
    public bool Modified { get; set; }
    
    public STYPE State { get; set; } = default!;
}
