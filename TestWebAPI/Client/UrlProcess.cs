using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using TestWebAPI.Model;
using HtmlAgilityPack;

namespace TestWebAPI.Client
{
    public class UrlProcess
    {
        public async Task<UrlContentDto> GetURLDataAsync(string URL)
        {
            try
            {
                string html = "";
                HttpStatusCode oStatus = new HttpStatusCode();
                UrlContentDto urlContentDto = new UrlContentDto();
                oStatus = await GetUrlStatusAsync(URL);
                if (oStatus.ToString().ToLower() != "ok")
                {
                    return null;
                }

                Console.WriteLine("Troubleshoot1");
                Process currentProcess = Process.GetCurrentProcess();
                long totalBytesOfMemoryUsed = currentProcess.WorkingSet64;
                try
                {
                    Console.WriteLine("MB " + "Troubleshoot1" + " >> " + (totalBytesOfMemoryUsed / 1024) / 1024);
                    Console.WriteLine("Process ID " + Process.GetCurrentProcess().Id);
                }
                catch (Exception) { }

                try
                {
                    HttpResponseMessage response;
                    using (HttpClient httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Add("User-Agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36");
                        httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml");

                        response = await httpClient.GetAsync(URL);
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        using (HttpClient httpClient = new HttpClient())
                        {
                            httpClient.DefaultRequestHeaders.Add("User-Agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36");
                            response = await httpClient.GetAsync(URL);
                            if (response.IsSuccessStatusCode)
                            {
                                html = await response.Content.ReadAsStringAsync();
                            }
                        }
                        if (!string.IsNullOrEmpty(html))
                        {
                            urlContentDto.UrlDescription = Regex.Match(html, "<meta name=\"description\" content=\"(.+?)\" />", RegexOptions.IgnoreCase).Groups[1].Value;
                            try
                            {
                                if (string.IsNullOrEmpty(urlContentDto.UrlDescription))
                                {
                                    HtmlDocument htmlDoc = new HtmlDocument();
                                    htmlDoc.LoadHtml(html);

                                    var metaTag = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='description']");

                                    if (metaTag != null)
                                    {
                                        string description = metaTag.GetAttributeValue("content", "");
                                        Console.WriteLine($"Description: {description}");
                                        urlContentDto.UrlDescription = description;
                                    }
                                    else
                                    {
                                        Console.WriteLine("No meta tag with attribute name='description' found in the HTML document.");
                                    }
                                }
                            }
                            catch
                            {
                                //throw;
                            }
                            urlContentDto.UrlTitle = Regex.Match(html, @"\<title\b[^>]*\>\s*(?<Title>[\s\S]*?)\</title\>", RegexOptions.IgnoreCase).Groups["Title"].Value;
                            urlContentDto.UrlBody = ExtractBodyFromHtml(html);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Troubleshoot0 >> " + ex.ToString());
                    urlContentDto = new UrlContentDto();
                    try
                    {
                        using (HttpClient httpClient = new HttpClient())
                        {
                            HttpResponseMessage response = await httpClient.GetAsync(URL);

                            if (response.IsSuccessStatusCode)
                            {
                                Stream receiveStream = await response.Content.ReadAsStreamAsync();

                                using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                                {
                                    html = await readStream.ReadToEndAsync();
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(html))
                        {
                            urlContentDto.UrlDescription = Regex.Match(html, "<meta name=\"description\" content=\"(.+?)\" />", RegexOptions.IgnoreCase).Groups[1].Value;
                            try
                            {
                                if (string.IsNullOrEmpty(urlContentDto.UrlDescription))
                                {
                                    HtmlDocument htmlDoc = new HtmlDocument();
                                    htmlDoc.LoadHtml(html);

                                    var metaTag = htmlDoc.DocumentNode.SelectSingleNode("//meta[@name='description']");

                                    if (metaTag != null)
                                    {
                                        string description = metaTag.GetAttributeValue("content", "");
                                        Console.WriteLine($"Description: {description}");
                                        urlContentDto.UrlDescription = description;
                                    }
                                    else
                                    {
                                        Console.WriteLine("No meta tag with attribute name='description' found in the HTML document.");
                                    }
                                }
                            }
                            catch
                            {
                                //throw;
                            }

                            urlContentDto.UrlTitle = Regex.Match(html, @"\<title\b[^>]*\>\s*(?<Title>[\s\S]*?)\</title\>", RegexOptions.IgnoreCase).Groups["Title"].Value;
                            urlContentDto.UrlBody = ExtractBodyFromHtml(html);
                        }

                        return urlContentDto;
                    }
                    catch (Exception exc)
                    {
                        Console.WriteLine("### GetURLData Trouble Shoot 1 () ###" + URL + "###" + DateTime.Now.ToShortDateString());
                        Console.WriteLine(exc.StackTrace + exc.Message + "" + DateTime.Now.ToShortDateString() + Environment.NewLine + "==============================");
                        return null;
                    }
                }

                return urlContentDto;
            }
            catch (Exception ex)
            {
                Console.WriteLine("### GetURLData() ###" + URL + "###" + DateTime.Now.ToShortDateString());
                Console.WriteLine(ex.StackTrace + ex.Message + "" + DateTime.Now.ToShortDateString() + Environment.NewLine + "==============================");
                return null;
            }
        }

        public static async Task<HttpStatusCode> GetUrlStatusAsync(string url)
        {
            HttpStatusCode result = default(HttpStatusCode);

            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.106 Safari/537.36");
                httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml");

                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Head, url);
                    var response = await httpClient.SendAsync(request);
                    result = response.StatusCode;
                }
                catch (WebException hre)
                {
                    Console.WriteLine("### GetUrlStatus() ###" + url + "###" + DateTime.Now.ToShortDateString());
                    Console.WriteLine(hre.StackTrace + hre.Message + "" + DateTime.Now.ToShortDateString() + Environment.NewLine + "==============================");

                    if (hre.Response == null)
                        result = HttpStatusCode.BadRequest;

                    return result;
                }
            }

            return result;
        }

        public string ExtractBodyFromHtml(string htmltext)
        {
            try
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmltext);

                htmlDoc.DocumentNode.Descendants()
                .Where(n => n.Name.ToLower() == "script" || n.Name.ToLower() == "style")
                .ToList()
                .ForEach(n => n.Remove());

                HtmlNode bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//body");
                if (bodyNode != null)
                {
                    string url_body = bodyNode.InnerHtml;
                    url_body = Regex.Replace(url_body, @"\s{2,}", " ").Replace("\n", "").Replace("\t", "");
                    return url_body;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine("### ExtractBodyFromHtml() ###" + DateTime.Now.ToShortDateString());
                Console.WriteLine(ex.StackTrace + ex.Message + "" + DateTime.Now.ToShortDateString() + Environment.NewLine + "==============================");
                return string.Empty;
            }
        }
    }
}
