using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TestWebAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly IAntiforgery _antiforgery;

        public HomeController(IAntiforgery antiforgery)
        {
            _antiforgery = antiforgery;
        }

        [HttpGet]
        [Route("api/GetAntiForgeryToken")]
        public IActionResult GetAntiForgeryToken()
        {
            var token = _antiforgery.GetAndStoreTokens(HttpContext);
            return Ok(token.RequestToken);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubmitForm([FromBody] MultipartFormDataContent formData)
        {
            // Validate the anti-forgery token
            _antiforgery.ValidateRequestAsync((HttpContext)Request.Headers);

            // Process the form data and return a response
            // ...

            return Ok();
        }
    }
}
