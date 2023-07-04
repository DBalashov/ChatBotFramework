using System.Diagnostics;

namespace ChatBotFramework;

public interface IChatBotHandler<in UID> where UID : notnull
{
    Task<ChatBotResponse> Handle(UID userId, ChatBotRequest request);
}

/// <summary>
/// Parsed request from user.
/// If message contain command started from / - Command and Arguments will be set (/new-name argument value)
/// otherwise Command will be null and Arguments will be null
/// </summary>
/// <param name="OriginalMessage">Raw text message from user</param>
/// <param name="Files">files if passed (empty array if no files uploaded in message)</param>
[DebuggerDisplay("{OriginalMessage} ({Command}) {Arguments}")]
public sealed record ChatBotRequest(string OriginalMessage, string? Command, string? Arguments, ChatBotRequestFile[] Files);

[DebuggerDisplay("{FileName} ({Content.Length} bytes)")]
public sealed record ChatBotRequestFile(string FileName, byte[] Content);