using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Telegram.Bot;
using Telegram.Bot.Types;

#pragma warning disable CS4014

namespace ChatBotFramework.Telegram;

/// <summary> singleton </summary>
[DebuggerDisplay("MediaGroups: {mediaGroups.Count}")]
sealed class GroupedMessageService : IGroupedMessageService
{
    readonly ILogger                  logger;
    readonly IChatBotMessageProcessor messageProcessor;

    readonly TimeSpan                                    waitingInterval;
    readonly Dictionary<string, GroupedMessageContainer> mediaGroups     = new(StringComparer.Ordinal);
    readonly ReaderWriterLockSlim                        mediaGroupsLock = new();

    public GroupedMessageService(ILogger<GroupedMessageService> logger,
                                 ChatBotTelegramOptions         options,
                                 IChatBotMessageProcessor       messageProcessor)
    {
        this.logger           = logger;
        this.messageProcessor = messageProcessor;
        waitingInterval       = options.GroupedMessageInterval;
    }

    public async Task ProcessOrAddToGroup(ITelegramBotClient bot, string? mediaGroupId, Update u, GroupedFileInfo file, string logContextPrefix)
    {
        if (string.IsNullOrEmpty(mediaGroupId))
        {
            logger.LogDebug("[{0}] media group empty, handle immediately", logContextPrefix);
            var fileInfo = await bot.TryDownloadFile(logger, file, logContextPrefix);
            messageProcessor.HandleMessageAsync(bot, u.ConvertToParams(fileInfo != null ? new[] {fileInfo} : Array.Empty<ChatBotRequestFile>()));
            return;
        }

        mediaGroupsLock.EnterUpgradeableReadLock();
        if (!mediaGroups.TryGetValue(mediaGroupId, out var groupedMessage))
        {
            mediaGroupsLock.EnterWriteLock();
            if (!mediaGroups.TryGetValue(mediaGroupId, out groupedMessage))
            {
                logger.LogDebug("[{0}] media group not found, create one {1}", logContextPrefix, mediaGroupId);
                mediaGroups.Add(mediaGroupId, groupedMessage = new GroupedMessageContainer(bot, mediaGroupId, waitingInterval, u.ConvertToParams(), logContextPrefix, completeMediaGroup));
            }

            mediaGroupsLock.ExitWriteLock();
        }
        else
        {
            logger.LogDebug("[{0}] media group found {1}", logContextPrefix, mediaGroupId);
        }

        mediaGroupsLock.ExitUpgradeableReadLock();
        logger.LogDebug("[<{0}] media group {1}, add file {2}", logContextPrefix, mediaGroupId, file);
        groupedMessage.AddFile(file);
    }

    async Task completeMediaGroup(GroupedMessageContainer owner, ITelegramBotClient bot, HandleMessageParams p, GroupedFileInfo[] files, string logContextPrefix)
    {
        mediaGroupsLock.EnterWriteLock();
        var existing = mediaGroups.Remove(owner.MediaGroupId);
        logger.LogDebug("[{0}] media group {1} remove ({2})", logContextPrefix, owner.MediaGroupId, existing);
        mediaGroupsLock.ExitWriteLock();

        logger.LogDebug("[{0}] media group {1} complete, {2} files", logContextPrefix, owner.MediaGroupId, files.Length);

        var downloadedFiles = (await Task.WhenAll(files.Select(f => bot.TryDownloadFile(logger, f, logContextPrefix)).ToArray())).Where(p => p != null).ToArray();
        messageProcessor.HandleMessageAsync(bot, p with {Files = downloadedFiles!});
    }

    [DebuggerDisplay("{MediaGroupId}")]
    sealed class GroupedMessageContainer : IDisposable
    {
        readonly Subject<GroupedFileInfo> files = new();
        readonly IDisposable              subscription;

        int disposed;

        public string MediaGroupId { get; }

        public GroupedMessageContainer(ITelegramBotClient                bot,
                                       string                            mediaGroupId,
                                       TimeSpan                          interval,
                                       HandleMessageParams               p, string logContextPrefix,
                                       OnGroupedMessageCompletedDelegate collectionCompleted)
        {
            MediaGroupId = mediaGroupId;
            subscription = files.SubscribeOn(TaskPoolScheduler.Default)
                                .Buffer(interval, 10)
                                .Subscribe(msgs =>
                                           {
                                               var items = msgs.ToArray();
                                               Dispose();
                                               collectionCompleted(this, bot, p, items, logContextPrefix);
                                           });
        }

        public void AddFile(GroupedFileInfo fileId) => files.OnNext(fileId);

        public void Dispose()
        {
            var value = Interlocked.Exchange(ref disposed, 1);
            if (value == 1) return;
            subscription.Dispose();
            files.Dispose();
        }
    }

    delegate Task OnGroupedMessageCompletedDelegate(GroupedMessageContainer groupedMessage, ITelegramBotClient bot, HandleMessageParams p, GroupedFileInfo[] fileIds, string logContextPrefix);
}