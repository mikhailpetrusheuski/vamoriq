namespace Vamoriq.Services.Interfaces;

public interface ICommand<TRequest, TResponse>
{

    Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}

public interface ICommand<TRequest>
{

    Task ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}
