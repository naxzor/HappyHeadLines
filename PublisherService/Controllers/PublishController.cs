using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
namespace PublisherService.Controllers;
[ApiController]
[Route("[controller]")]
public class PublishController (IPublishEndpoint bus) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Publish([FromBody] ArticlePublished evt, CancellationToken ct)
    {
        await bus.Publish(evt, ct);
        return Accepted(new { evt.Id });
    }
}