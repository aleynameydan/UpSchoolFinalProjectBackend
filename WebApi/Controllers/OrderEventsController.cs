using Application.Features.OrderEvents.Commands.Add;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

public class OrderEventsController : ApiControllerBase
{

    [HttpPost("Add")]
    public async Task<IActionResult> AddAsync(OrderEventAddCommand command)
    {
        return Ok(await Mediator.Send(command));
    }
}