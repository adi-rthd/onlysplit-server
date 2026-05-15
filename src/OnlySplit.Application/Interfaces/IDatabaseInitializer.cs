namespace OnlySplit.Application.Interfaces;

public interface IDatabaseInitializer
{
    Task InitializeAsync(bool isDevelopment, CancellationToken cancellationToken = default);
}
