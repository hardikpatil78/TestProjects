using Microsoft.AspNetCore.Mvc;
using TestWebAPI.Client;
using TestWebAPI.Model;
using HttpGetAttribute = Microsoft.AspNetCore.Mvc.HttpGetAttribute;
using HttpPostAttribute = Microsoft.AspNetCore.Mvc.HttpPostAttribute;
using RouteAttribute = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace TestWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OpenSearch : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly OpenSearchClient _openSearchClient;
        public OpenSearch(IConfiguration _configuration, OpenSearchClient openSearchClient)
        {
            configuration = _configuration;
            _openSearchClient = openSearchClient;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return Ok();
        }

        [HttpPost]
        [Route("Createdocument")]
        public async Task<IActionResult> CreateDocument(string index,string url) 
        {
            UrlProcess urlProcess = new UrlProcess();
            var urlContentData = await urlProcess.GetURLDataAsync(url);

            Document document = new Document()
            {
                index = index,
                id_report = Guid.NewGuid().ToString(),
                url_content = urlContentData.UrlBody
            };

            var response = await _openSearchClient.CreateDocumentAsync(document);

            return Ok(response);
        }

        [HttpPost]
        [Route("searchkeyword")]
        public async Task<IActionResult> SearchKeyword(string index,string keyword)
        {
            var response = await _openSearchClient.SearchAsync(index,keyword);
            var responseBody = await response.Content.ReadAsStringAsync();
            return Ok(responseBody);
        }
    }
}
