namespace OnlySplit.Application.Features.BasicPage;

public sealed class LandingStatsResponse
{
    public int RegisteredUsers { get; set; }

    public int ActiveGroups { get; set; }

    public int ExpensesProcessed { get; set; }
}