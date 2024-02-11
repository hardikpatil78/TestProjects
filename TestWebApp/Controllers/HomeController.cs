using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using TestWebApp.Data;
using TestWebApp.Models;
using PuppeteerSharp;
using Microsoft.AspNetCore.Hosting;
using HtmlAgilityPack;
using System.Security.Policy;
//using NBoilerpipe.Extractors;
using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System.Net.Http;
using Microsoft.AspNetCore.Components.Forms;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Html;
using System.Xml.Linq;
using System.Text.Json;
using Amazon.OpenSearchService;
using OpenSearch.Client;
using OpenSearch.Net;
using Amazon.Runtime.Documents;
using Amazon.SimpleSystemsManagement.Model;

namespace TestWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly TestWebAppDbContext _testWebAppDbContext;
        private readonly IWebHostEnvironment _hostEnvironment;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, TestWebAppDbContext testWebAppDbContext, IWebHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _configuration = configuration;
            _testWebAppDbContext = testWebAppDbContext;
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Manage Google Drive Page
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult ManageDrive()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                return View();
            }
        }

        public ActionResult ConnectDrive()
        {
            var uri = $"https://accounts.google.com/o/oauth2/auth?" +
                        $"scope={Uri.EscapeUriString("https://www.googleapis.com/auth/drive https://www.googleapis.com/auth/userinfo.email https://www.googleapis.com/auth/userinfo.profile")}" +
                        $"&access_type=offline" +
                        $"&prompt=consent" +
                        $"&redirect_uri={Uri.EscapeUriString($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}/setting/ConnectDriveResponse")}" +
                        $"&response_type=code" +
                        $"&client_id={_configuration.GetSection("GoogleDrive:GoogleClientId").Value}";

            return Redirect(uri);
        }

        public async Task<ActionResult> ConnectDriveResponse(string code = null, string scope = null)
        {
            var redirectUri = "";
            try
            {
                if (!string.IsNullOrEmpty(code))
                {
                    using (var httpClient = new HttpClient())
                    {
                        redirectUri = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host.Value + "/setting/ConnectDriveResponse";
                        var dict = new Dictionary<string, string>
                        {
                            { "code", code },
                            { "client_id", _configuration.GetSection("GoogleDrive:GoogleClientId").Value },
                            { "client_secret", _configuration.GetSection("GoogleDrive:GoogleClientSecret").Value },
                            { "scope", scope },
                            { "redirect_uri", Uri.EscapeUriString(redirectUri) },
                            { "grant_type", "authorization_code" }
                        };

                        var content = new FormUrlEncodedContent(dict);
                        var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token", content);

                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            dynamic resultContent = JObject.Parse(await response.Content.ReadAsStringAsync());

                            var access_token = resultContent.access_token;
                            var refresh_token = resultContent.refresh_token;

                            string url = "https://www.googleapis.com/oauth2/v1/userinfo?alt=json&access_token=" + access_token;
                            var res_pro = await httpClient.GetAsync(url);
                            var profile_pic = "";
                            var email = "";
                            if (res_pro.StatusCode == HttpStatusCode.OK)
                            {
                                dynamic result_Content = JObject.Parse(await res_pro.Content.ReadAsStringAsync());
                                profile_pic = result_Content.picture;
                                email = result_Content.email;
                            }

                            //var UserId = Response.GetAuthCookie().UserID;

                            
                            tblDrive drive = _testWebAppDbContext.tblDrive.Where(a => a.DriveAccount == email).FirstOrDefault();

                            if (drive != null)
                            {
                                drive.Token = refresh_token;

                                _testWebAppDbContext.tblDrive.Attach(drive);
                                _testWebAppDbContext.Entry(drive).State = EntityState.Modified;
                            }
                            else
                            {
                                drive = new tblDrive();

                                drive.DriveAccount = email;
                                drive.Token = refresh_token;
                                drive.CreatedAt = DateTime.Now;
                                //drive.CreatedBy = UserId;
                                drive.IsActive = true;

                                _testWebAppDbContext.tblDrive.Add(drive);
                            }

                            _testWebAppDbContext.SaveChanges();
                            
                        }
                        else
                        {
                            ViewData["errorMessage"] = "Oops! Something went wrong with google authentication";
                        }
                    }
                }
                return RedirectToAction("ManageDrive");
            }
            catch (Exception ex)
            {
                return RedirectToAction("ManageDrive");
            }
        }

        public string GetMimeMapping(string filePath)
        {
            var provider = new FileExtensionContentTypeProvider();
            string contentType;
            if (provider.TryGetContentType(filePath, out contentType))
            {
                // Use the contentType as needed
            }
            else
            {
                // Default MIME type for unknown file extensions
                contentType = "application/octet-stream";
            }
            return contentType;
        }

        [HttpPost]
        public ActionResult UploadFile(tblDriveViewModel DriveView)
        {
            try
            {
                if (DriveView.DriveId != 0) //upload to google drive
                {
                    tblDrive drive = new tblDrive();
                    
                    drive = _testWebAppDbContext.tblDrive.Where(a => a.DriveId == DriveView.DriveId).FirstOrDefault();
                    
                    if (DriveView.SelectFile != null && DriveView.SelectFile.Length > 0)
                    {
                        var valid_token = drive.Token;
                        var token = new Google.Apis.Auth.OAuth2.Responses.TokenResponse()
                        {
                            ExpiresInSeconds = 9999999,
                            IssuedUtc = DateTime.Now,
                            RefreshToken = valid_token
                        };

                        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                        {
                            ClientSecrets = new ClientSecrets
                            {
                                ClientId = _configuration.GetSection("GoogleDrive:GoogleClientId").Value,
                                ClientSecret = _configuration.GetSection("GoogleDrive:GoogleClientSecret").Value
                            }
                        });

                        UserCredential credential = new UserCredential(flow, "Your Email Address", token);
                        var serviceInitializer = new BaseClientService.Initializer()
                        {
                            ApplicationName = "Application Name",
                            HttpClientInitializer = credential
                        };

                        DriveService service = new(serviceInitializer);

                        string path = Path.Combine(_hostEnvironment.WebRootPath + "/Content/GoogleDriveFiles", Path.GetFileName(DriveView.SelectFile.FileName));

                        if (!Directory.Exists(_hostEnvironment.WebRootPath + "/Content/GoogleDriveFiles"))
                        {
                            Directory.CreateDirectory(_hostEnvironment.WebRootPath + "/Content/GoogleDriveFiles");
                        }

                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            DriveView.SelectFile.CopyTo(stream);
                        }

                        var FileMetaData = new Google.Apis.Drive.v3.Data.File()
                        {
                            Name = DriveView.FileName != null ? DriveView.FileName + Path.GetExtension(DriveView.SelectFile.FileName) : Path.GetFileName(DriveView.SelectFile.FileName),
                            MimeType = GetMimeMapping(path)
                        };

                        FilesResource.CreateMediaUpload request;
                        bool FileUploadedFlag = false;
                        using (var stream = new System.IO.FileStream(path,
                        System.IO.FileMode.Open))
                        {
                            request = service.Files.Create(FileMetaData, stream, FileMetaData.MimeType);
                            request.Fields = "*";
                            var result = request.Upload();

                            if (result.Status == Google.Apis.Upload.UploadStatus.Completed)
                            {
                                FileUploadedFlag = true;

                                var file1 = request.ResponseBody;

                                //add file info to database
                                
                                tblFile file = new tblFile();

                                file.DriveId = DriveView.DriveId;
                                file.FileName = file1.OriginalFilename;
                                file.FileLocalUrl = path;
                                file.IsUplodedOnDrive = true;
                                file.IsActive = true;
                                file.CreatedAt = DateTime.Now;
                                file.ProjectId = DriveView.ProjectId;
                                file.Id = file1.Id;
                                file.FileDriveUrl = file1.WebViewLink;

                                _testWebAppDbContext.tblFile.Add(file);

                                _testWebAppDbContext.SaveChanges();
                                
                            }
                        }

                        System.IO.File.Delete(path);

                        if (FileUploadedFlag)
                        {
                            return Json(new { status = true });
                        }
                    }
                    else
                    {
                        bool FileUploadedFlag = false;
                        string path = "";

                        foreach (var file in DriveView.SelectFiles)
                        {
                            var valid_token = drive.Token;
                            var token = new Google.Apis.Auth.OAuth2.Responses.TokenResponse()
                            {
                                ExpiresInSeconds = 9999999,
                                IssuedUtc = DateTime.Now,
                                RefreshToken = valid_token
                            };

                            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                            {
                                ClientSecrets = new ClientSecrets
                                {
                                    ClientId = _configuration.GetSection("GoogleDrive:GoogleClientId").Value,
                                    ClientSecret = _configuration.GetSection("GoogleDrive:GoogleClientSecret").Value
                                }
                            });

                            UserCredential credential = new(flow, "Your Email Address", token);
                            var serviceInitializer = new BaseClientService.Initializer()
                            {
                                ApplicationName = "Application Name",
                                HttpClientInitializer = credential
                            };

                            DriveService service = new DriveService(serviceInitializer);

                            path = Path.Combine(_hostEnvironment.WebRootPath + "/Content/GoogleDriveFiles", Path.GetFileName(file.FileName));

                            if (!Directory.Exists(_hostEnvironment.WebRootPath + "/Content/GoogleDriveFiles"))
                            {
                                Directory.CreateDirectory(_hostEnvironment.WebRootPath + "/Content/GoogleDriveFiles");
                            }

                            using (var stream = new FileStream(path, FileMode.Create))
                            {
                                file.CopyTo(stream);
                            }

                            var FileMetaData = new Google.Apis.Drive.v3.Data.File()
                            {
                                Name = Path.GetFileName(file.FileName),
                                MimeType = GetMimeMapping(path)
                            };

                            FilesResource.CreateMediaUpload request;
                            using (var stream = new System.IO.FileStream(path,
                            System.IO.FileMode.Open))
                            {
                                request = service.Files.Create(FileMetaData, stream, FileMetaData.MimeType);
                                request.Fields = "*";
                                var result = request.Upload();

                                if (result.Status == Google.Apis.Upload.UploadStatus.Completed)
                                {
                                    FileUploadedFlag = true;

                                    var file1 = request.ResponseBody;

                                    //add file info to database
                                    
                                    tblFile filedata = new tblFile();

                                    filedata.DriveId = DriveView.DriveId;
                                    filedata.FileName = file1.OriginalFilename;
                                    filedata.FileLocalUrl = path;
                                    filedata.IsUplodedOnDrive = true;
                                    filedata.IsActive = true;
                                    filedata.CreatedAt = DateTime.Now;
                                    filedata.Id = file1.Id;
                                    filedata.FileDriveUrl = file1.WebViewLink;

                                    _testWebAppDbContext.tblFile.Add(filedata);

                                    _testWebAppDbContext.SaveChanges();
                                    
                                }
                                else
                                {
                                    FileUploadedFlag = false;

                                    break;
                                }
                            }

                            System.IO.File.Delete(path);
                        }

                        System.IO.File.Delete(path);

                        if (FileUploadedFlag)
                        {
                            return Json(new { status = true });
                        }
                    }
                }
                else //upload to FTP
                {
                    string path;

                    if (DriveView.SelectFile != null && DriveView.SelectFile.Length > 0)
                    {
                        path = Path.Combine(_hostEnvironment.WebRootPath + "/Content/GoogleDriveFiles", DriveView.FileName != null ? DriveView.FileName + Path.GetExtension(DriveView.SelectFile.FileName) : Path.GetFileName(DriveView.SelectFile.FileName));

                        if (!Directory.Exists(_hostEnvironment.WebRootPath + "/Content/GoogleDriveFiles"))
                        {
                            Directory.CreateDirectory(_hostEnvironment.WebRootPath + "/Content/GoogleDriveFiles");
                        }

                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            DriveView.SelectFile.CopyTo(stream);
                        }

                        var host = _configuration.GetSection("FTPSettings:FTPHost").Value;
                        var un = _configuration.GetSection("FTPSettings:FTPUserName").Value;
                        var password = _configuration.GetSection("FTPSettings:FTPUserName").Value;

                        if (!DirectoryExistOnFTP("ftp://" + host + "/ProjectFiles/Project" + DriveView.ProjectId + "/"))
                        {
                            WebRequest request = WebRequest.Create("ftp://" + host + "/ProjectFiles/Project" + DriveView.ProjectId + "/");
                            request.Method = WebRequestMethods.Ftp.MakeDirectory;
                            request.Credentials = new NetworkCredential(un, password);

                            var resp = (FtpWebResponse)request.GetResponse();
                        }

                        var FileName = DriveView.FileName != null ? DriveView.FileName + Path.GetExtension(DriveView.SelectFile.FileName) : Path.GetFileName(DriveView.SelectFile.FileName);
                        WebRequest requestFile = WebRequest.Create("ftp://" + host + "/ProjectFiles/Project" + DriveView.ProjectId + "/" + FileName);
                        requestFile.Method = WebRequestMethods.Ftp.UploadFile;
                        requestFile.Credentials = new NetworkCredential(un, password);

                        byte[] bytes = System.IO.File.ReadAllBytes(path);

                        using (Stream requestStream = requestFile.GetRequestStream())
                        {
                            requestStream.Write(bytes, 0, bytes.Length);
                        }

                        var respFile = (FtpWebResponse)requestFile.GetResponse();

                        if (respFile.StatusCode == FtpStatusCode.ClosingData)
                        {
                            //add file info to database
                            
                            tblFile file = new tblFile();

                            tblFile File1 = new tblFile();
                            File1 = _testWebAppDbContext.tblFile.Where(a => a.FileName == FileName && a.IsUplodedOnDrive == false && a.IsActive == true && a.ProjectId == DriveView.ProjectId).FirstOrDefault();
                            if (File1 != null)
                            {
                                file = File1;

                                file.FileLocalUrl = path;
                                file.IsUplodedOnDrive = false;
                                file.IsActive = true;
                                file.CreatedAt = DateTime.Now;
                                _testWebAppDbContext.tblFile.Attach(file);
                                _testWebAppDbContext.Entry(file).State = EntityState.Modified;
                                _testWebAppDbContext.SaveChanges();
                            }
                            else
                            {
                                file.FileName = FileName;
                                file.FileLocalUrl = path;
                                file.IsUplodedOnDrive = false;
                                file.IsActive = true;
                                file.CreatedAt = DateTime.Now;
                                _testWebAppDbContext.tblFile.Add(file);
                                _testWebAppDbContext.SaveChanges();
                            }
                            
                        }

                        System.IO.File.Delete(path);

                        return Json(new { status = true });
                    }
                    else
                    {
                        bool fileUploadFlag = false;

                        foreach (var file in DriveView.SelectFiles)
                        {
                            path = Path.Combine(_hostEnvironment.WebRootPath + "/Content/GoogleDriveFiles", Path.GetFileName(file.FileName));

                            if (!Directory.Exists(_hostEnvironment.WebRootPath + "/Content/GoogleDriveFiles"))
                            {
                                Directory.CreateDirectory(_hostEnvironment.WebRootPath + "/Content/GoogleDriveFiles");
                            }

                            using (var stream = new FileStream(path, FileMode.Create))
                            {
                                file.CopyTo(stream);
                            }

                            var host = _configuration.GetSection("FTPSettings:FTPHost").Value;
                            var un = _configuration.GetSection("FTPSettings:FTPUserName").Value;
                            var password = _configuration.GetSection("FTPSettings:FTPUserName").Value;

                            if (!DirectoryExistOnFTP("ftp://" + host + "/ProjectFiles/Project" + DriveView.ProjectId + "/"))
                            {
                                WebRequest request = WebRequest.Create("ftp://" + host + "/ProjectFiles/Project" + DriveView.ProjectId + "/");
                                request.Method = WebRequestMethods.Ftp.MakeDirectory;
                                request.Credentials = new NetworkCredential(un, password);

                                var resp = (FtpWebResponse)request.GetResponse();
                            }

                            var FileName = Path.GetFileName(file.FileName);
                            WebRequest requestFile = WebRequest.Create("ftp://" + host + "/ProjectFiles/Project" + DriveView.ProjectId + "/" + FileName);
                            requestFile.Method = WebRequestMethods.Ftp.UploadFile;
                            requestFile.Credentials = new NetworkCredential(un, password);

                            byte[] bytes = System.IO.File.ReadAllBytes(path);

                            using (Stream requestStream = requestFile.GetRequestStream())
                            {
                                requestStream.Write(bytes, 0, bytes.Length);
                            }

                            var respFile = (FtpWebResponse)requestFile.GetResponse();

                            if (respFile.StatusCode == FtpStatusCode.ClosingData)
                            {
                                fileUploadFlag = true;

                                //add file info to database
                                
                                tblFile filedata = new tblFile();

                                tblFile File1 = new tblFile();
                                File1 = _testWebAppDbContext.tblFile.Where(a => a.FileName == FileName && a.IsUplodedOnDrive == false && a.IsActive == true && a.ProjectId == DriveView.ProjectId).FirstOrDefault();
                                if (File1 != null)
                                {
                                    filedata = File1;

                                    filedata.FileLocalUrl = path;
                                    filedata.IsUplodedOnDrive = false;
                                    filedata.IsActive = true;
                                    filedata.ProjectId = DriveView.ProjectId;
                                    filedata.CreatedAt = DateTime.Now;

                                    _testWebAppDbContext.tblFile.Attach(filedata);
                                    _testWebAppDbContext.Entry(filedata).State = EntityState.Modified;

                                    _testWebAppDbContext.SaveChanges();
                                }
                                else
                                {
                                    filedata.FileName = FileName;
                                    filedata.FileLocalUrl = path;
                                    filedata.IsUplodedOnDrive = false;
                                    filedata.IsActive = true;
                                    filedata.CreatedAt = DateTime.Now;
                                    filedata.ProjectId = DriveView.ProjectId;

                                    _testWebAppDbContext.tblFile.Add(filedata);
                                    _testWebAppDbContext.SaveChanges();
                                }   
                            }
                            else
                            {
                                fileUploadFlag = false;

                                break;
                            }

                            System.IO.File.Delete(path);
                        }

                        if (fileUploadFlag)
                        {
                            return Json(new { status = true });
                        }
                    }
                }

                return Json(new { status = false });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.ToString() });
            }
        }

        private bool DirectoryExistOnFTP(string path)
        {
            try
            {
                WebRequest requestCheckDirectory = WebRequest.Create(path);
                requestCheckDirectory.Method = WebRequestMethods.Ftp.ListDirectory;
                requestCheckDirectory.Credentials = new NetworkCredential("UserName", "Password");
                using (var resp = (FtpWebResponse)requestCheckDirectory.GetResponse())
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public ActionResult DeleteFile(string GoogleDriveFileId, int FileId, int? ProjectId, string FileName)
        {
            try
            {
                if (GoogleDriveFileId != null)
                {
                    tblDrive drive = new tblDrive();
                    tblFile file = new tblFile();
                    
                    file = _testWebAppDbContext.tblFile.Where(a => a.FileId == FileId).FirstOrDefault();

                    drive = _testWebAppDbContext.tblDrive.Where(a => a.DriveId == file.DriveId).FirstOrDefault();
                    

                    var valid_token = drive.Token;
                    var token = new Google.Apis.Auth.OAuth2.Responses.TokenResponse()
                    {
                        ExpiresInSeconds = 9999999,
                        IssuedUtc = DateTime.Now,
                        RefreshToken = valid_token
                    };

                    var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                    {
                        ClientSecrets = new ClientSecrets
                        {
                            ClientId = _configuration.GetSection("GoogleDrive:GoogleClientId").Value,
                            ClientSecret = _configuration.GetSection("GoogleDrive:GoogleClientSecret").Value
                        }
                    });

                    UserCredential credential = new UserCredential(flow, "EmailAddress", token);
                    var serviceInitializer = new BaseClientService.Initializer()
                    {
                        ApplicationName = "Application Name",
                        HttpClientInitializer = credential
                    };

                    DriveService service = new DriveService(serviceInitializer);

                    service.Files.Delete(GoogleDriveFileId).Execute();

                    
                    tblFile file1 = new tblFile();

                    file1 = _testWebAppDbContext.tblFile.Where(a => a.Id == GoogleDriveFileId).FirstOrDefault();

                    if (file1 != null)
                    {
                        file1.IsActive = false;

                        _testWebAppDbContext.tblFile.Attach(file1);
                        _testWebAppDbContext.Entry(file1).State = EntityState.Modified;

                        _testWebAppDbContext.SaveChanges();
                    }
                    
                    return Json(new { status = true });
                }
                else
                {
                    var host = _configuration.GetSection("FTPSettings:FTPHost").Value;
                    WebRequest requestFile = WebRequest.Create("ftp://" + host + "/ProjectFiles/Project" + ProjectId + "/" + FileName);
                    requestFile.Method = WebRequestMethods.Ftp.DeleteFile;
                    requestFile.Credentials = new NetworkCredential("UserNAme", "Password");

                    var respFile = (FtpWebResponse)requestFile.GetResponse();

                    if (respFile.StatusCode == FtpStatusCode.FileActionOK)
                    {
                        //add file info to database
                        
                        tblFile file1 = new tblFile();

                        file1 = _testWebAppDbContext.tblFile.Where(a => a.Id == GoogleDriveFileId).FirstOrDefault();

                        if (file1 != null)
                        {
                            file1.IsActive = false;

                            _testWebAppDbContext.tblFile.Attach(file1);
                            _testWebAppDbContext.Entry(file1).State = EntityState.Modified;

                            _testWebAppDbContext.SaveChanges();
                        }
                        
                    }

                    return Json(new { status = true });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.ToString() });
            }
        }

        [HttpGet]
        public ActionResult DownloadFileFromFTP(string ProjectId_FileName)
        {
            try
            {
                int ProjectId = Convert.ToInt32(Utils.Encryption.DecryptText(ProjectId_FileName).Split('|')[0]);
                string FileName = Utils.Encryption.DecryptText(ProjectId_FileName).Split('|')[1];

                var host = _configuration.GetSection("FTPSettings:FTPHost").Value;
                var un = _configuration.GetSection("FTPSettings:FTPUserName").Value;
                var password = _configuration.GetSection("FTPSettings:FTPUserName").Value;

                WebRequest request = WebRequest.Create("ftp://" + host + "/ProjectFiles/Project" + ProjectId + "/" + FileName);
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential(un, password);

                Stream ftpStream = request.GetResponse().GetResponseStream();

                string contentType = GetMimeMapping(FileName);

                return File(ftpStream, contentType, FileName);
            }
            catch (Exception ex)
            {
                throw;
            }

            return null;
        }

        [HttpGet]
        public ActionResult ConvertHtml()
        {
            try
            {
                return View(new List<GenerateHtml>()); // Pass an empty list instead of a single instance
            }
            catch (Exception ex)
            {
                return View(new List<GenerateHtml>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> ConvertHtmlAsync(string GenerateUrl,string keywordString = "")
        {
            try
            {
                List<string> keywordList = JsonSerializer.Deserialize<List<string>>(keywordString);
                IEnumerable<GenerateHtml> htmlText = await ExtractParagraphAsync(GenerateUrl, keywordList);
                
                return View(htmlText);
            }
            catch (Exception ex)
            {
                return View(null);
            }
        }

        public async Task<List<GenerateHtml>> ExtractParagraphAsync(string GenerateUrl, List<string> Keywords = null)
        {
            try
            {
                string htmlContent = string.Empty;
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:64.0)");

                    var response = await httpClient.GetAsync(GenerateUrl); // get Content from the Web page
                    response.EnsureSuccessStatusCode();

                    htmlContent = await response.Content.ReadAsStringAsync();

                }

                // Remove unnecessery spaces and script tags
                string result = Regex.Replace(htmlContent, @"^\s*$[\r\n]*", "", RegexOptions.Multiline);
                result = Regex.Replace(result, @"(&[^;]+;)|(\d+mm)", "");
                result = Regex.Replace(result, @"\s{2,}", " ");
                result = Regex.Replace(result, @"[\n\r]+", "\n").Trim();
                result = Regex.Replace(result, @"<script[^>]*>[\s\S]*?</script>", string.Empty);
                result = Regex.Replace(result, @"<style[^>]*>[\s\S]*?</style>", string.Empty);


                // Load the HTML content into HtmlDocument
                var doc = new HtmlDocument();
                doc.LoadHtml(result);
                var bodyElement = doc.DocumentNode.SelectSingleNode("//body");
                
                var paragraphs = GetParag(bodyElement, bodyElement, Keywords); // Pass Keyword to get snippet of Keyword
                return paragraphs;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<List<GenerateHtml>> ExtractParagraphsByKeywordAsync(string htmlContent)
        {
            var paragraphs = new List<GenerateHtml>();
            var keyword = "SMS spoofing";
            
            {
                string result = Regex.Replace(htmlContent, @"^\s*$[\r\n]*", "", RegexOptions.Multiline);
                result = Regex.Replace(result, @"(&[^;]+;)|(\d+mm)", "");
                result = Regex.Replace(result, @"\s{2,}", " ");
                result = Regex.Replace(result, @"[\n\r]+", "\n").Trim();
                result = Regex.Replace(result, @"<script[^>]*>[\s\S]*?</script>", string.Empty);
                result = Regex.Replace(result, @"<style[^>]*>[\s\S]*?</style>", string.Empty);


                // Load the HTML content into HtmlDocument
                var doc = new HtmlDocument();
                doc.LoadHtml(result);
                var bodyElement = doc.DocumentNode.SelectSingleNode("//body");
                List<string> Keywords = new List<string>() {
                    "SMS spoofing"
                };
                paragraphs = GetParag(bodyElement, bodyElement, Keywords); // Get Paragraph from Text Content

                return paragraphs;
            }
        }

        public List<GenerateHtml> GetParag(HtmlNode node,HtmlNode parent, List<string> Keywords, List<GenerateHtml> paragraphs = null)
        {
            
            //var paragraphs = new List<string>();

            if (node.NodeType == HtmlNodeType.Text)
            {
                var nodeTextContent = node.OuterHtml;
                if (!string.IsNullOrEmpty(nodeTextContent))
                {
                    var replacedText = nodeTextContent;

                    foreach(var word in Keywords)
                    {
                        //string pattern = $@"\b\w*{word}\w*\b";
                        string pattern = $@"\b{word}\b";
                        Regex wordMatch = new(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
                        Match match = wordMatch.Match(replacedText);
                        if (match.Success)
                        {
                            replacedText = replacedText.Replace("\n", "").Replace("\t", "");
                            GenerateHtml generateHtml = new()
                            {
                                HtmlText = replacedText,
                                Text = word
                            };
                            paragraphs.Add(generateHtml);
                        }
                    }
                }
            }
            else if(node.NodeType == HtmlNodeType.Element)
            {
                string styleAttributeValue = node.GetAttributeValue("style", "");
                bool isDisplayNone = styleAttributeValue.Contains("display:none");
                string tagName = node.OriginalName.ToLower();
                if (tagName != "a" && tagName != "h1" && tagName != "h2" && !isDisplayNone)
                {
                    var childNodes = node.ChildNodes;
                    foreach(var childNode in childNodes)
                    {
                        paragraphs ??= new List<GenerateHtml>();
                        GetParag(childNode, node, Keywords, paragraphs);
                    }
                }
            }
            return paragraphs;
        }
        public async Task<string> ExtractContentByUrl(string Url)
        {
            try
            {
                
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:64.0)");

                    var response = await httpClient.GetAsync(Url);
                    response.EnsureSuccessStatusCode();

                    var htmlContent = await response.Content.ReadAsStringAsync();

                    var document = new HtmlDocument();
                    document.LoadHtml(htmlContent);

                    var content = document.DocumentNode.InnerText.Trim();
                    string result = Regex.Replace(content, @"^\s*$[\r\n]*", "", RegexOptions.Multiline);
                    result = Regex.Replace(result, @"(&[^;]+;)|(\d+mm)", "");
                    result = Regex.Replace(result, @"\s{2,}", " ");
                    result = Regex.Replace(result, @"[\n\r]+", "\n").Trim();
                    return result;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public string ExtractContent(string html)
        {
            try
            {
                var document = new HtmlDocument();
                document.LoadHtml(html);

                var content = document.DocumentNode.InnerText.Trim();
                string result = Regex.Replace(content, @"^\s*$[\r\n]*", "", RegexOptions.Multiline);
                return content;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public string ConvertHtmlToText(string html)
        {
            try
            {
                var textBuilder = new StringBuilder(html.Length);
                var tagWhiteSpaceRegex = new Regex(@"(>|$)(\W|\n|\r)+<", RegexOptions.Multiline);
                var stripFormattingRegex = new Regex(@"<[^>]*(>|$)", RegexOptions.Multiline);
                var lineBreakRegex = new Regex(@"<(br|BR)\s{0,1}\/{0,1}>", RegexOptions.Multiline);

                // Decode html specific characters
                html = System.Net.WebUtility.HtmlDecode(html);
                // Remove tag whitespace/line breaks
                html = tagWhiteSpaceRegex.Replace(html, "><");
                // Replace <br /> with line breaks
                html = lineBreakRegex.Replace(html, Environment.NewLine);
                // Strip formatting
                html = stripFormattingRegex.Replace(html, string.Empty);

                textBuilder.Append(html);
                return textBuilder.ToString();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public bool LogHtml(string htmlText)
        {
            try
            {
                string fileName = "HtmlFiles/" + DateTime.Now.ToString("yyyyMMMddhhmm_ss") + ".html"; // Specify the desired file name
                string filePath = Path.Combine(_hostEnvironment.WebRootPath, fileName);

                // Check if the file already exists
                if (!System.IO.File.Exists(filePath))
                {
                    // Create the file if it doesn't exist
                    using (StreamWriter sw = System.IO.File.CreateText(filePath))
                    {
                        sw.WriteLine(htmlText);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public ActionResult ConnectEbay()
        {
            var uri = $"https://accounts.google.com/o/oauth2/auth?" +
                        $"scope={Uri.EscapeUriString("https://www.googleapis.com/auth/drive https://www.googleapis.com/auth/userinfo.email https://www.googleapis.com/auth/userinfo.profile")}" +
                        $"&access_type=offline" +
                        $"&prompt=consent" +
                        $"&redirect_uri={Uri.EscapeUriString($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}/setting/ConnectDriveResponse")}" +
                        $"&response_type=code" +
                        $"&client_id=YourClientId";

            return Redirect(uri);
        }

        public ActionResult ConnectUps()
        {
            var uri = $"https://wwwcie.ups.com/security/v1/oauth/validate-client?" + 
                $"client_id=YourClientId" + 
                $"&redirect_uri={Uri.EscapeUriString($"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}/setting/ConnectDriveResponse")}";
            return Redirect(uri);
        }

        public ActionResult ConnectUpsClient(string code = null, string scope = null)
        {
            BasicAuthenticationCredentials credentials = new BasicAuthenticationCredentials();
            credentials.Username = "UserName";
            credentials.Password = SecureStrings.CreateSecureString("Password");

            

            //OpenSearchClient openSearchClient = new OpenSearchClient("linkbot-zybsfb5bm7btbyqbzpzbjp5lvi", credentials);
            //openSearchClient.CreateDocument();
          
            return NotFound();
        }
    }
}