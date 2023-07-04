using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ChatBotFramework.Telegram;

interface IGroupedMessageService
{
    /// <summary>
    /// Collect multiple files/photos (passed by single message each) into one group and handle them together after a certain time interval (by default 150 ms).
    /// If user upload single file/photo, it will be handled immediately (mediaGroupId is empty).
    /// </summary>
    /// <param name="mediaGroupId">correlation Id for files/photo batch</param>
    Task ProcessOrAddToGroup(ITelegramBotClient bot, string? mediaGroupId, Update u, GroupedFileInfo file, string logContextPrefix);
}

[DebuggerDisplay("{Id}: {Size} bytes")]
sealed record GroupedFileInfo(string Id, int Size)
{
    public override string ToString() => $"{Id} ({Size} bytes)";
}