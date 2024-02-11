using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using TestWebAPI.Model;
using Document = TestWebAPI.Model.Document;
using System.Net.Http;
using System.Dynamic;

namespace TestWebAPI.Client
{
    public class OpenSearchClient
    {
        private readonly HttpClient _client;
        private readonly IConfiguration _configuration;
        private string _baseAddress;

        public OpenSearchClient(IConfiguration configuration)
        {
            _configuration = configuration;
            string username = _configuration.GetSection("AwsOpenSearchApi:UserName").Value;
            string password = _configuration.GetSection("AwsOpenSearchApi:Password").Value;
            _baseAddress = _configuration.GetSection("AwsOpenSearchApi:baseAddress").Value;
            _client = new HttpClient
            {
                BaseAddress = new Uri(_baseAddress),
                DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"))) }
            };
            
        }

        public async Task<HttpResponseMessage> CreateDocumentAsync(Document doc)
        {
            string type = "_doc"; // or your document type

            string requestUri = $"/{doc.index}/{type}/{doc.id_report}";

            var content = new StringContent(JsonConvert.SerializeObject(doc), Encoding.UTF8, "application/json");

            var response = await _client.PostAsync(requestUri, content);

            return response;
        }

        public async Task<HttpResponseMessage> SearchAsync(string index, string keyword)
        {
            //string index = "shipscience";
            string requestUri = $"{_baseAddress}{index}/_search";

            dynamic matchPhrase = new ExpandoObject();
            matchPhrase.query = new ExpandoObject();
            matchPhrase.query.match_phrase = new MatchPhrase()
            {
                url_content = new UrlContent()
                {
                    query = keyword
                }
            };

            string _ontent = JsonConvert.SerializeObject(matchPhrase);
            Console.WriteLine(_ontent);

            var content = new StringContent(JsonConvert.SerializeObject(matchPhrase), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(requestUri),
                Content = content,
            };

            var response = await _client.SendAsync(request);

            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseBody);

            return response;
        }
    }
}
