namespace Vamoriq.Services.Interfaces
{
    public interface IGraphQLService
    {
        Task<TResponse?> ExecuteQueryAsync<TResponse>(string query, object? variables, CancellationToken ct = default);
    }

    public sealed class GraphQLResponse<T>
    {
        public T? Data { get; set; }
        public List<GraphQLError>? Errors { get; set; }
    }

    public sealed class GraphQLError
    {
        public string Message { get; set; } = string.Empty;
        public List<string>? Path { get; set; }
        public Dictionary<string, object>? Extensions { get; set; }
    }
}
