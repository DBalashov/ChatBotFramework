﻿using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ChatBotFramework.Telegram;

interface IChatBotMessageProcessor
{
    Task HandleMessageAsync(ITelegramBotClient bot, HandleMessageParams p);
}

[DebuggerDisplay("{Id}: From={From}, Chat={Chat}, {MessageText}, Files={Files.Length}")]
sealed record HandleMessageParams(int Id, string LogPrefix, User From, Chat Chat, string? MessageText, int? ReplyMessageId, ChatBotRequestFile[] Files)
{
    public string RequestId => Id.ToString("X");
}