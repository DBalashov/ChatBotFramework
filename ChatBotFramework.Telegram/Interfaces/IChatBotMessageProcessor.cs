using Telegram.Bot;
using Telegram.Bot.Types;

namespace ChatBotFramework.Telegram;

interface IChatBotMessageProcessor
{
    Task HandleMessageAsync(ITelegramBotClient bot, HandleMessageParams p);
}

sealed record HandleMessageParams(int Id, string MessageType, User From, Chat Chat, string? MessageText, int? ReplyMessageId, ChatBotRequestFile[] Files)
{
    public string RequestId => Id.ToString("X");
}