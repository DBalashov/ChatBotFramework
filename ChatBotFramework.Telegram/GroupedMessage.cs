using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Telegram.Bot.Types;

namespace ChatBotFramework.Telegram;

delegate Task OnGroupedMessageCompletedDelegate(GroupedMessage groupedMessage, HandleMessageParams p, string[] fileIds, string logContextPrefix);

sealed class GroupedMessage : IDisposable
{
    /// <summary> FileId -> FileName </summary>
    readonly Subject<string> fileIDs = new();

    readonly IDisposable subscription;

    int disposed;

    public readonly string MediaGroupId;

    public GroupedMessage(string mediaGroupId, TimeSpan interval, HandleMessageParams p, string logContextPrefix, OnGroupedMessageCompletedDelegate collectionCompleted)
    {
        MediaGroupId = mediaGroupId;
        subscription = fileIDs.SubscribeOn(TaskPoolScheduler.Default)
                              .Buffer(interval, 10)
                              .Subscribe(msgs =>
                                         {
                                             var items = msgs.ToArray();
                                             Dispose();
                                             collectionCompleted(this, p, items, logContextPrefix);
                                         });
    }

    public void AddFile(string fileId) => fileIDs.OnNext(fileId);

    public void Dispose()
    {
        var value = Interlocked.Exchange(ref disposed, 1);
        if (value == 1) return;
        subscription.Dispose();
        fileIDs.Dispose();
    }
}