using Application.Features.Orders.Commands.Add;
using Application.Features.Orders.Queries.GetAll;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

public class OrdersController : ApiControllerBase
{
    [HttpPost("Add")]
    public async Task<IActionResult> AddAsync(OrderAddCommand command)
    {
        return Ok(await Mediator.Send(command));
    }

    [HttpPost("GetAll")]
    public async Task<IActionResult> GetAllAsync(OrderGetAllQuery query)
    {
        return Ok(await Mediator.Send(query));
    }
}