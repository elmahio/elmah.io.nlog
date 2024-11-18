using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Elmah.Io.NLog.AspNetCore90.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            // Simple information logging
            _logger.LogInformation("Calling Index");

            // Example of structured logging
            _logger.LogWarning("Index with {method}", Request.Method);

            try
            {
                var i = 0;
                var result = 10 / i;
            }
            catch (Exception e)
            {
                // Example of error logging with exception details
                _logger.LogError(e, "Error during index");
            }

        }
    }
}
