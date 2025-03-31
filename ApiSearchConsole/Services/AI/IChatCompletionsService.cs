namespace ApiSearchConsole.Services.AI{
    public interface IChatCompletionsService
    {
        Task<string> GetChatCompletionsAsync(string prompt, string instruction,  object jsonSchema,CancellationToken cancellationToken);
    }
}