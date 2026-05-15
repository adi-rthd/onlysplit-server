namespace OnlySplit.Domain.Exceptions;

public sealed class PaymentException : AppException
{
    public PaymentException(string message)
        : base(message)
    {
    }
}
