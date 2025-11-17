using BeC.OpenId.Connect.Infrastructure.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BeC.OpenId.Connect.Features.Payments.Controllers;

/// <summary>
/// Stripe webhook endpoint for receiving payment event notifications
/// This endpoint is called by Stripe when payment events occur (payment succeeded, failed, refunded, etc.)
/// </summary>
[ApiController]
[Route("api/webhooks/stripe")]
[AllowAnonymous] // Webhooks use signature verification instead of bearer tokens
public class StripeWebhookController : ControllerBase
{
    private readonly IStripePaymentService _paymentService;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        IStripePaymentService paymentService,
        ILogger<StripeWebhookController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Handle Stripe webhook events
    /// </summary>
    /// <remarks>
    /// This endpoint receives notifications from Stripe about payment events.
    /// Events include: payment_intent.succeeded, payment_intent.payment_failed,
    /// charge.refunded, payout.paid, payout.failed, etc.
    ///
    /// Configure this webhook URL in your Stripe Dashboard:
    /// https://dashboard.stripe.com/webhooks
    ///
    /// Example webhook URL: https://your-domain.com/api/webhooks/stripe
    /// </remarks>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleWebhook()
    {
        try
        {
            // Read raw request body
            using var reader = new StreamReader(Request.Body);
            var json = await reader.ReadToEndAsync();

            // Get Stripe signature header
            var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("Webhook received without Stripe-Signature header");
                return BadRequest(new { error = "Missing Stripe-Signature header" });
            }

            // Verify webhook signature for security
            if (!_paymentService.VerifyWebhookSignature(json, signature))
            {
                _logger.LogWarning("Webhook signature verification failed");
                return BadRequest(new { error = "Invalid signature" });
            }

            // Process the webhook event
            await _paymentService.HandleWebhookEventAsync(json);

            _logger.LogInformation("Webhook processed successfully");

            return Ok(new { received = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Health check endpoint for webhook configuration
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            service = "stripe_webhook",
            timestamp = DateTime.UtcNow
        });
    }
}
