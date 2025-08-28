using MeuCrudCsharp.Features.Base;
using MeuCrudCsharp.Features.Refunds.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Refunds.Controllers
{
    /// <summary>
    /// Manages refund requests for the authenticated user.
    /// </summary>
    public class RefundsController : ApiControllerBase
    {
        private readonly RefundService _refundService;
        private readonly ILogger<RefundsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RefundsController"/> class.
        /// </summary>
        /// <param name="refundService">The service responsible for refund logic.</param>
        /// <param name="logger">The logger for recording events and errors.</param>
        public RefundsController(RefundService refundService, ILogger<RefundsController> logger)
        {
            _refundService = refundService;
            _logger = logger;
        }

        /// <summary>
        /// Initiates a refund request for the authenticated user's last eligible payment.
        /// </summary>
        /// <returns>A confirmation message upon success or an error message upon failure.</returns>
        /// <response code="200">Indicates the refund request was processed successfully.</response>
        /// <response code="400">If the request violates a business rule (e.g., refund period has expired).</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="500">If an unexpected internal server error occurs.</response>
        /// <response code="502">If there is a communication failure with the payment provider.</response>
        [HttpPost("request-refund")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status502BadGateway)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RequestRefund()
        {
            try
            {
                await _refundService.RequestUserRefundAsync();
                return Ok(new { message = "Refund request processed successfully." });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business rule violation while requesting a refund.");
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "Error communicating with the payment provider API during the refund process."
                );
                return StatusCode(
                    502,
                    new
                    {
                        message = "There was an error processing your refund with our payment provider. Please try again later.",
                    }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An unexpected error occurred while processing the refund request."
                );
                return StatusCode(
                    500,
                    new { message = "An internal error occurred. Our team has been notified." }
                );
            }
        }
    }
}
