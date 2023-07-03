# ChatBotFramework
## Installing


`Install-Package ChatBotFramework`

`Install-Package ChatBotFramework.Telegram`

## Implement some interfaces

### Persistable state model for your bot

```csharp
public class TestModel : IChatBotModel<string>
{
    public string Name     { get; set; }

    public bool   Modified { get; set; }
    public string State    { get; set; }
}
```

### Storage for model

`Int64` - user id type

`TestModel` - model type

`string` - state type (can be value or reference type)

```csharp

```csharp
class TestModelStorage : IChatBotModelStorage<Int64, TestModel, string>
{
    public Task Save(Int64 userId, TestModel model)
    {
        var fileName = getPath(userId);
        return File.WriteAllTextAsync(fileName, JsonSerializer.Serialize(model));
    }

    public async Task<TestModel> Load(Int64 userId)
    {
        var fileName = getPath(userId);
        return File.Exists(fileName) ? JsonSerializer.Deserialize<TestModel>(await File.ReadAllTextAsync(fileName)) ?? new TestModel() : new TestModel();
    }

    string getPath(Int64 userId) => Path.Combine(Path.GetTempPath(), userId + ".json");
}
```

### Implement bot commands

```csharp
[ChatBotCommand("set-name")] // command name
[ChatBotState<string>("set-name")]
public class SetNameCommand : IChatBotCommand<Int64, TestModel>
{
    public async Task<ChatBotResponse> Handle(long userId, TestModel model, ChatBotRequest request)
    {
        if (request.Command == null)
        {
            model.State    = "";
            model.Name     = request.OriginalMessage;
            model.Modified = true;
            return ChatBotResponse.Text("New name: " + model.Name);
        }
        else
        {
            model.State    = "set-name";
            model.Modified = true;
            return ChatBotResponse.Text("Enter new name:");
        }
    }
}

[ChatBotCommand("get-name")]
public class GetNameCommand : IChatBotCommand<Int64, TestModel>
{
    public async Task<ChatBotResponse> Handle(long userId, TestModel model, ChatBotRequest request)
    {
        return ChatBotResponse.Text("Current name: " + model.Name);
    }
}

[ChatBotDefaultHandler] // default handler for all unknown commands and requests from user
public class DefaultHandler : IChatBotCommand<Int64, TestModel>
{
    public async Task<ChatBotResponse> Handle(long userId, TestModel model, ChatBotRequest request)
    {
        return ChatBotResponse.Text("You can use commands: set-name, get-name");
    }
}
```

### Register bot classes in DI in ConfigureServices method

```csharp

public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddChatBot<Int64, TestModel, string, TestModelStorage>()
            .AddChatBotCommand<Int64, TestModel, string, SetNameCommand>()
            .AddChatBotCommand<Int64, TestModel, string, GetNameCommand>()
            .AddChatBotCommand<Int64, TestModel, string, DefaultHandler>()
            .AddSingleton(new ChatBotTelegramOptions() {Token = "yyyyyyyyy:xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"})
            .AddChatBotTelegram<TestModel, string>();
     ...    
 }

```

### Run

Run and enter something in chat with bot

### Notes

* all `IChatBotCommand<UID,MODEL>` handlers registered with `Scoped` lifetime and can be use all advantages and possibilities of DI
* all `IChatBotCommand<UID,MODEL>` handlers **must be registered** with `AddChatBotCommand<UID,MODEL,STYPE,HANDLER>` method as shown above
* one `IChatBotCommand<UID,MODEL>` can be handle multiple commands and states (multiple `ChatBotCommand` attributes allowed)
* your can set `Modified` property of state model in Handle method and state model will be saved in storage automatically
* bot state model must implement `IChatBotModel<STYPE>` interface and `STYPE` can be value or reference type
* bot state model storage must implement `IChatBotModelStorage<UID,MODEL,STYPE>` interface and return new/empty model for new users
