namespace OnlySplit.Domain.Utils;

public static class MoneyMath
{
    public static decimal Round(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

    public static int ToPaise(decimal amount) => checked((int)(Round(amount) * 100));
}
