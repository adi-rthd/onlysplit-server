namespace OnlySplit.Domain.Exceptions;

public sealed class NotFoundException : AppException
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}
