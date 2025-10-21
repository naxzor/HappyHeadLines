using Contracts;
using Microsoft.AspNetCore.Mvc;

namespace NewsletterService.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class NewsletterController(IHttpClientFactory http, ILogger<NewsletterController> logger) : ControllerBase
{
    [HttpPost("daily")]
    public async Task<IActionResult> SendDaily([FromQuery] RegionScope region = RegionScope.GLOBAL, [FromQuery] int limit = 10)
    {
        var client = http.CreateClient("articles");
        var url = $"/v1/articles?regionScope={region}&limit={Math.Clamp(limit,1,200)}";

        var res = await client.GetAsync(url);
        if (!res.IsSuccessStatusCode)
            return StatusCode((int)res.StatusCode, await res.Content.ReadAsStringAsync());

        var json = await res.Content.ReadAsStringAsync();
        logger.LogInformation("Daily newsletter ({Region}): {Payload}", region, json);

        return Ok(new { sent = true, region, count = limit });
    }
}