using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlySplit.Shared.Responses;
using OnlySplit.Application.Features.Expenses;
using OnlySplit.Application.Interfaces;

namespace OnlySplit.API.Controllers;

[ApiController]
[Authorize]
[Route("api/expenses")]
public sealed class ExpensesController(IExpenseService expenseService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ExpenseResponse>>> Create(CreateExpenseRequest request, CancellationToken cancellationToken)
    {
        var response = await expenseService.CreateAsync(request, cancellationToken);
        return Ok(ApiResponse<ExpenseResponse>.Ok(response, "Expense created successfully."));
    }

    [HttpGet("group/{groupId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ExpenseResponse>>>> GetByGroup(Guid groupId, CancellationToken cancellationToken)
    {
        var response = await expenseService.GetByGroupAsync(groupId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyCollection<ExpenseResponse>>.Ok(response));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ExpenseResponse>>> Update(Guid id, UpdateExpenseRequest request, CancellationToken cancellationToken)
    {
        var response = await expenseService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<ExpenseResponse>.Ok(response, "Expense updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await expenseService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(null, "Expense deleted successfully."));
    }
}
