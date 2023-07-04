using System.Diagnostics;

namespace ChatBotFramework;

public abstract record ChatBotMessageBase(params ChatBotButton[] Buttons);

[DebuggerDisplay("{Command}: {Text}, Arguments: {Arguments}")]
public sealed record ChatBotButton(string Text, string Command, string? Arguments = null);

[DebuggerDisplay("Message: {Message}, Buttons: {Buttons.Length}")]
public sealed record ChatBotMessage(string Message, params ChatBotButton[] Buttons) : ChatBotMessageBase(Buttons)
{
    public bool Html { get; internal set; }
}

[DebuggerDisplay("File: {FileName}")]
public sealed record ChatBotFile(Stream Stream, string? FileName = null) : ChatBotMessageBase();

[DebuggerDisplay("FileUrl: {Url}")]
public sealed record ChatBotFileUrl(string Url) : ChatBotMessageBase;

public sealed record ChatBotImage(Stream Stream) : ChatBotMessageBase();

[DebuggerDisplay("ImageUrl: {Url}")]
public sealed record ChatBotImageUrl(string Url) : ChatBotMessageBase();

public sealed record ChatBotVideo(Stream Stream) : ChatBotMessageBase();

[DebuggerDisplay("VideoUrl: {Url}")]
public sealed record ChatBotVideoUrl(string Url) : ChatBotMessageBase();

[DebuggerDisplay("Messages: {Messages.Count}")]
public sealed class ChatBotResponse
{
    public List<ChatBotMessageBase> Messages { get; } = new();

    private ChatBotResponse()
    {
    }

    public ChatBotResponse(string message, bool asHtml = false, params ChatBotButton[] buttons)
    {
        ArgumentNullException.ThrowIfNull(message, nameof(message));
        AddMessage(new ChatBotMessage(message, buttons).AsHtml(asHtml));
    }

    public static ChatBotResponse Html(string htmlMessage, params ChatBotButton[] buttons) => new(htmlMessage, true, buttons);

    public static ChatBotResponse Text(string textMessage, params ChatBotButton[] buttons) => new(textMessage, false, buttons);

    #region AddMessage / AddFile / AddImage / AddVideo

    public ChatBotResponse AddMessage(params ChatBotMessage[] messages)
    {
        foreach (var m in messages)
            ArgumentNullException.ThrowIfNull(m.Message, nameof(m.Message));
        Messages.AddRange(messages);
        return this;
    }

    public ChatBotResponse AddFile(params ChatBotFile[] files)
    {
        foreach (var m in files)
            ArgumentNullException.ThrowIfNull(m.Stream, nameof(m.Stream));
        Messages.AddRange(files);
        return this;
    }

    public ChatBotResponse AddFileUrl(params ChatBotFileUrl[] files)
    {
        foreach (var m in files)
            ArgumentNullException.ThrowIfNull(m.Url, nameof(m.Url));
        Messages.AddRange(files);
        return this;
    }

    public ChatBotResponse AddImage(params ChatBotImage[] images)
    {
        foreach (var m in images)
            ArgumentNullException.ThrowIfNull(m.Stream, nameof(m.Stream));
        Messages.AddRange(images);
        return this;
    }

    public ChatBotResponse AddImageUrl(params ChatBotImageUrl[] images)
    {
        foreach (var m in images)
            ArgumentNullException.ThrowIfNull(m.Url, nameof(m.Url));
        Messages.AddRange(images);
        return this;
    }

    public ChatBotResponse AddVideo(params ChatBotVideo[] videos)
    {
        foreach (var m in videos)
            ArgumentNullException.ThrowIfNull(m.Stream, nameof(m.Stream));
        Messages.AddRange(videos);
        return this;
    }

    public ChatBotResponse AddVideo(params ChatBotVideoUrl[] videos)
    {
        foreach (var m in videos)
            ArgumentNullException.ThrowIfNull(m.Url, nameof(m.Url));
        Messages.AddRange(videos);
        return this;
    }

    #endregion
}

public static class ChatBotResponseExtenders
{
    public static ChatBotMessage AsHtml(this ChatBotMessage m, bool html = true)
    {
        m.Html = html;
        return m;
    }
}