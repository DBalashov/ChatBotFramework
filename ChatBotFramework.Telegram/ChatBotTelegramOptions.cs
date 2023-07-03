#pragma warning disable CS8618
namespace ChatBotFramework.Telegram;

public sealed class ChatBotTelegramOptions
{
    public string   Token                  { get; set; }
    public TimeSpan GroupedMessageInterval { get; set; } = TimeSpan.FromMilliseconds(150);
}