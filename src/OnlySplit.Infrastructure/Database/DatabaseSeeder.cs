using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OnlySplit.Domain.Constants;
using OnlySplit.Domain.Entities;

namespace OnlySplit.Infrastructure.Database;

public static class DatabaseSeeder
{
    public static async Task SeedDevelopmentDataAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OnlySplitDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        if (!configuration.GetValue("SeedData:Enabled", false))
        {
            return;
        }

        if (await context.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var password = BCrypt.Net.BCrypt.HashPassword("DemoPass123!");
        var users = new[]
        {
            new User { FirstName = "Anika", LastName = "Rao", Email = "anika@onlysplit.dev", PasswordHash = password },
            new User { FirstName = "Kabir", LastName = "Mehta", Email = "kabir@onlysplit.dev", PasswordHash = password },
            new User { FirstName = "Mira", LastName = "Shah", Email = "mira@onlysplit.dev", PasswordHash = password }
        };

        var group = new Group
        {
            Name = "Goa Trip",
            CreatedBy = users[0].Id
        };

        foreach (var user in users)
        {
            group.Members.Add(new GroupMember { GroupId = group.Id, UserId = user.Id });
        }

        var expense = new Expense
        {
            GroupId = group.Id,
            PaidBy = users[0].Id,
            Title = "Villa booking",
            Description = "Weekend stay",
            Amount = 9000,
            Category = "travel",
            Splits = users.Select(user => new ExpenseSplit
            {
                UserId = user.Id,
                AmountOwed = 3000,
                SplitType = SplitTypes.Equal
            }).ToList()
        };

        var settlement = new Settlement
        {
            GroupId = group.Id,
            PayerId = users[1].Id,
            ReceiverId = users[0].Id,
            Amount = 3000
        };

        context.Users.AddRange(users);
        context.Groups.Add(group);
        context.Expenses.Add(expense);
        context.Settlements.Add(settlement);
        context.ActivityLogs.Add(new ActivityLog
        {
            UserId = users[0].Id,
            Type = ActivityTypes.GroupCreated,
            Metadata = JsonSerializer.Serialize(new { group.Id, group.Name })
        });

        await context.SaveChangesAsync(cancellationToken);
    }
}
