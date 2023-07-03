using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ChatBotFramework.Telegram;

static class TelegramExtenders
{
    public static ParseMode? GetParseMode(this ChatBotMessage msg) => msg.Html ? ParseMode.Html : null;

    public static string FormatFileName(this string? fileName) =>
        string.IsNullOrEmpty(fileName) ? "" : Path.GetFileName(fileName);

    public static string GetUserName(this User user)
    {
        var userName = user.Username ?? string.Join(" ", user.FirstName, user.LastName);
        return user.Id + (string.IsNullOrEmpty(userName) ? "" : "/" + userName.Trim());
    }

    public static ChatBotRequest ParseMessage(this string msg, ChatBotRequestFile[] files)
    {
        var idx = msg.IndexOf(' ');
        return new ChatBotRequest(msg,
                                  idx < 0 ? null : msg[..idx],
                                  idx < 0 ? null : msg[(idx + 1)..],
                                  files);
    }

    public static bool ReplyMessageDeleted(this ApiRequestException e) =>
        e.Message.IndexOf("replied message", StringComparison.Ordinal) != -1;

    public static InlineKeyboardMarkup? ToInlineKeyboardMarkup(this ChatBotButton[] buttons) =>
        !buttons.Any()
            ? null
            : new InlineKeyboardMarkup(buttons.Select(p => new InlineKeyboardButton(p.Text)
                                                           {
                                                               CallbackData = p.Command + (string.IsNullOrEmpty(p.Arguments) ? "" : " " + p.Arguments)
                                                           }));

    public static IAlbumInputMedia ConvertToMedia(this ChatBotMessageBase m) =>
        m switch
        {
            ChatBotImage image       => new InputMediaPhoto(new InputFileStream(image.Stream)),
            ChatBotImageUrl imageUrl => new InputMediaPhoto(new InputFileUrl(imageUrl.Url)),
            ChatBotVideo video       => new InputMediaVideo(new InputFileStream(video.Stream)),
            ChatBotVideoUrl videoUrl => new InputMediaVideo(new InputFileUrl(videoUrl.Url)),
            _                        => throw new NotSupportedException($"{nameof(ConvertToMedia)} not support {m.GetType()}")
        };

    public static InputFile ConvertToDocument(this ChatBotMessageBase m) =>
        m switch
        {
            ChatBotFile file       => new InputFileStream(file.Stream, file.FileName),
            ChatBotFileUrl fileUrl => new InputFileUrl(fileUrl.Url),
            _                      => throw new NotSupportedException($"{nameof(ConvertToDocument)} not support {m.GetType()}")
        };

    public static HandleMessageParams ConvertToParams(this Update u, params ChatBotRequestFile[] files) =>
        new(u.Id,
            $"{u.Message!.From!.GetUserName()}/{u.Type}",
            u.Message!.From!,
            u.Message.Chat,
            u.Message.Text,
            u.Message.MessageId,
            files);

    public static HandleMessageParams ConvertToParams(this CallbackQuery c, Update u) =>
        new(u.Id,
            $"{c.Message!.From!.GetUserName()}/{u.Type}",
            c.From,
            c.Message!.Chat,
            c.Data,
            c.Message!.MessageId,
            Array.Empty<ChatBotRequestFile>());

    public static async Task<ChatBotRequestFile?> TryDownloadFile(this ITelegramBotClient bot, ILogger logger, string fileId, string logContextPrefix)
    {
        using var stm = new MemoryStream();
        try
        {
            var file = await bot.GetInfoAndDownloadFileAsync(fileId, stm);
            return new ChatBotRequestFile(file.FilePath.FormatFileName(), stm.ToArray());
        }
        catch (Exception e)
        {
            logger.LogWarning("[{0}] Can't download file {1}: {2}", logContextPrefix, fileId, (e.InnerException ?? e).Message);
            return null;
        }
    }
}