using Microsoft.AspNetCore.Mvc;
using ShahiZayka.Application.DTOs;
using ShahiZayka.Application.Interfaces;

namespace ShahiZayka.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>
    /// Gets customer testimonials and reviews.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var reviews = await _reviewService.GetReviewsAsync();
        return Ok(reviews);
    }

    /// <summary>
    /// Submits a new review.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateReviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName) || string.IsNullOrWhiteSpace(request.Comment))
        {
            return BadRequest(new { message = "Customer name and comment are required." });
        }

        var created = await _reviewService.AddReviewAsync(request);
        return StatusCode(StatusCodes.Status201Created, created);
    }
}

[ApiController]
[Route("api/[controller]")]
public class NewsletterController : ControllerBase
{
    private readonly INewsletterService _newsletterService;

    public NewsletterController(INewsletterService newsletterService)
    {
        _newsletterService = newsletterService;
    }

    /// <summary>
    /// Subscribes an email to the Shahi Zayka culinary newsletter.
    /// </summary>
    [HttpPost("subscribe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
    {
        var result = await _newsletterService.SubscribeAsync(request.Email);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return StatusCode(result.StatusCode, new { message = "Successfully subscribed to the newsletter!" });
    }
}

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public HealthController(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// System health check endpoint for AWS ALB, Kubernetes liveness probes, and monitoring.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CheckHealth()
    {
        try
        {
            // Verify database connectivity
            var canConnect = _context.Products.Take(1).Any();
            return Ok(new
            {
                status = "Healthy",
                service = "ShahiZayka.Api",
                timestampUtc = DateTime.UtcNow,
                version = "1.0.0",
                database = canConnect ? "Connected" : "Reachable",
                uptime = Environment.TickCount64 / 1000 + " seconds"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unhealthy",
                error = ex.Message,
                timestampUtc = DateTime.UtcNow
            });
        }
    }
}
