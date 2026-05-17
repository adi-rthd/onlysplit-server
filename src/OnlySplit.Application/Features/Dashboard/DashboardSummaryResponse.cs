namespace OnlySplit.Application.Dashboard.DTOs;

public class DashboardSummaryResponse
{
    public int TotalGroups { get; set; }

    public int TotalMembers { get; set; }
    public string Currency { get; set; } = "INR";
    public decimal TotalSpending { get; set; }
    public List<RecentActivityResponse> RecentActivities { get; set; } = [];
    public decimal YouOwe { get; set; }

    public decimal YouAreOwed { get; set; }
}