using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace testConsole
{
    public class PermonthPayment
    {
        public int SL { get; set; }
        public double BeginningBalance { get; set; }
        public double MonthlyInstallment { get; set; }
        public double Interest { get; set; }
        public double Principal { get; set; }
        public double Balance { get; set; }
    }
    public class TokenData
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(ExtractParagraph("SMS spoofing"));
            createFile();
            ReadXmlFromUrl("https://developer-docs.amazon.com/sp-api/docs/sp-api-errors-frequently-asked-questions").GetAwaiter().GetResult();
            Thread thread1 = new Thread(Method1)
            {
                //Thread becomes background thread
                IsBackground = true
            };

            Console.WriteLine($"Thread1 is a Background thread:  {thread1.IsBackground}");
            thread1.Start();
            //The control will come here and will exit 
            //the main thread or main application
            Console.WriteLine("Main Thread Exited");
            

            DateTime newdate = DateTime.UtcNow.AddSeconds(3600);
            DateTime now = DateTime.Today;
            double Principle = 0;
            double Payment = 0;
            double InterestRate = 0;
            int n = 0;
            int mont;
            string year = now.ToString("yyyy");
            int currentYear = Int16.Parse(year);
            string getMonthName(int month)
            {
                return CultureInfo.CurrentCulture.
                    DateTimeFormat.GetAbbreviatedMonthName
                    (month);
            }
            try
            {
                int sl = 1;
                Console.WriteLine("Enter The Principle Amount");
                Principle = Convert.ToDouble(Console.ReadLine());
                Console.WriteLine("Enter The Interest Rate");
                InterestRate = Convert.ToDouble(Console.ReadLine());
                Console.WriteLine("Enter the EMI duration in years");
                n = Convert.ToInt16(Console.ReadLine()) * 12;

                if (InterestRate > 1)
                {
                    InterestRate = Math.Round(InterestRate / 100, 2);
                }
                Payment = Math.Round((Principle * Math.Pow((InterestRate / 12) + 1,
                    (n)) * InterestRate / 12) / (Math.Pow(InterestRate / 12 + 1, (n)) - 1), 2);
                List<PermonthPayment> PaymentSchuld = new List<PermonthPayment>();
                CalculateEMI(Principle, InterestRate, Payment, sl, PaymentSchuld);
                Console.WriteLine("Year          " + "Month          " + "Principle      " + "Interest  " + "Total Payment");
                mont = 12;
                foreach (var item in PaymentSchuld)
                {
                    Console.WriteLine("{0}            {1}              {2}        {3}     {4}", currentYear, getMonthName(mont), item.Principal, Math.Round(item.Interest), item.MonthlyInstallment);
                    if (mont >= 12)
                    {
                        currentYear++;
                        mont = 0;
                    }
                    mont++;
                }
            }            
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        static void createFile()
        {
            string filePath = "TokenData.json";

            if (File.Exists(filePath))
            {
                Console.WriteLine("File exists.");
                var json = File.ReadAllText("TokenData.json");
                var token = JsonConvert.DeserializeObject<TokenData>(json);
                
                if(token != null)
                {
                    token.AccessToken = Guid.NewGuid().ToString();
                    var modifiedJson = JsonConvert.SerializeObject(token);
                    File.WriteAllText("TokenData.json", modifiedJson);
                }
                else
                {
                    var data = new TokenData()
                    {
                        AccessToken = Guid.NewGuid().ToString(),
                        RefreshToken = Guid.NewGuid().ToString(),
                    };
                    var modifiedJson = JsonConvert.SerializeObject(data);
                    File.WriteAllText("TokenData.json", modifiedJson);
                }

                Console.WriteLine("Access Token: " + token.AccessToken);
                Console.WriteLine("Refresh Token: " + token.RefreshToken);
                
            }
            else
            {
                var data = new TokenData()
                {
                    AccessToken = Guid.NewGuid().ToString(),
                    RefreshToken = Guid.NewGuid().ToString(),
                };
                var json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);

                File.WriteAllText("TokenData.json", json);

                Console.WriteLine("JSON file created successfully.");
            }
        }
        static void Method1()
        {
            Console.WriteLine("Method1 Started");
            for (int i = 0; i <= 5; i++)
            {
                Console.WriteLine("Method1 is in Progress!!");
                Thread.Sleep(1000);
            }
            Console.WriteLine("Method1 Exited");
            Console.WriteLine("Press any key to Exit.");
            //Console.ReadKey();
        }
        private static void CalculateEMI(double prin, double InterestRate, double Payment, int sl, List<PermonthPayment> PaymentSchuld)
        {


            PermonthPayment md = new PermonthPayment();

            md.MonthlyInstallment = Math.Round(Payment);
            md.Interest = Math.Round(prin * InterestRate / 12, 2);
            md.Principal = Math.Round(Payment - md.Interest);
            md.Balance = Math.Round(prin - md.Principal, 2);
            PaymentSchuld.Add(md);
            if (md.Balance > 0)
            {
                CalculateEMI(md.Balance, InterestRate, Payment, sl + 1, PaymentSchuld);
            }
            else
            {
                md.Balance = 0;
            }
        }

        public static async Task ReadXmlFromUrl(string url)
        {
            HttpClient client = new HttpClient();

            try
            {
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string xml = await response.Content.ReadAsStringAsync();

                    // Process the XML here
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xml);

                    // ... do something with the XML data

                    // Example: Get the value of an XML element
                    XmlNodeList nodes = xmlDoc.GetElementsByTagName("exampleElement");
                    if (nodes.Count > 0)
                    {
                        string elementValue = nodes[0].InnerText;
                        Console.WriteLine("Example Element Value: " + elementValue);
                    }
                }
                else if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    Console.WriteLine("503 Service Unavailable");
                    // Handle the 503 error here
                }
                else
                {
                    Console.WriteLine("Error: " + response.StatusCode);
                    // Handle other errors if needed
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                // Handle the exception
            }
            finally
            {
                client.Dispose();
            }
        }

        public static string ExtractParagraph(string word, string text="")
        {
            try
            {
                
                text = @"<section>
        <div class=""container"">
            <div class=""row justify-content-md-center"">
				<div class=""col-12 col-md-10 text-center"">
					<h1>How it Works  </h1>
					<p>Benevolist.org simplifies and speeds up the item donation process and helps to increase the number of donors for all your charitable item collection drives. Work with our merchant partners to easily create a wish list of items for your next donation drive. Share the link to the list with your donor network and they’ll be off and running - purchasing the items that you’ve specifically chosen and having them delivered to your desired location! </p>
					<p>
						You’ll be amazed at how Benevolist.org helps make your work so much easier and more efficient.  And your donors will love contributing to the success of all your charitable donation drives.
					</p>
					<h2 id=""fake-sender-ids"">Imitation sender IDs</h2>
					Fraudsters can alter the sender's name or number to fool recipients into thinking they are receiving messages from a trustworthy source. By disguising their identity, they can manipulate individuals into revealing sensitive information or performing specific actions.
					<h2 id=""harassment"">Harassment (Stalking, Pranks, Fake Emergencies, etc.)</h2>
					SMS spoofing can also be used for harmful activities like stalking, pranks, or creating panic. By pretending to be someone the recipient knows, the fraudster can cause distress or manipulate the person into behaving in ways they wouldn't normally.
					<h2 id=""how-to-identify-spoofing"">Recognizing spoofed messages</h2>
					Identifying spoofed messages can be tricky as fraudsters are becoming more sophisticated in their approach. However, there are some red flags to watch out for:
					Odd or unusual message content: Look out for unusual requests, spelling errors, or grammatical mistakes in the message. Authentic organizations typically maintain a professional tone and strive for error-free communication.
					Unexpected sender: Be cautious if you receive a message from someone unfamiliar or an unexpected sender. Always confirm the sender's identity through other means before responding or providing any personal information.
					Suspicious URLs or attachments: Be wary of messages that contain suspicious links or attachments. Avoid clicking on them unless you are certain of their legitimacy.
					<h2 id=""how-to-prevent-sms-spoofing"">Mitigating SMS Spoofing</h2>
					Though it's difficult to completely eliminate the risk of SMS spoofing, there are steps you can take to reduce your exposure:
					Stay vigilant with personal information: Refrain from sharing sensitive personal or financial details over text messages. Reputable organizations typically use secure channels or alternate methods for verification.
					Use reliable security software: Install trustworthy mobile security applications that can detect and protect against potential spoofing attempts.
					Educate yourself and your team: Stay informed about the latest spoofing techniques and share this information with your team. Train your team to identify potential spoofing attempts and follow cybersecurity best practices.
					<h2 id=""legal-uses-of-sms-spoofing"">Legal Utilization of SMS Spoofing in Business</h2>
					Although SMS spoofing is often linked with fraudulent activities, there are also legitimate uses for businesses. Here are a few examples:
					<h2 id=""bulk-sms-campaigns"">Bulk SMS Campaigns</h2>
					Businesses can use SMS spoofing to send bulk messages for promotional purposes, like marketing campaigns or essential updates to their customers. This facilitates effective communication and engagement with a large audience.
					<h2 id=""broadcast-official-messages"">Dissemination of Official Messages</h2>
					government agencies or institutions can use SMS spoofing to send official messages to citizens or employees, ensuring that the information is attributed to the appropriate authority.
					<h2 id=""preserve-anonymity"">Maintaining Anonymity</h2>
					In certain situations, businesses may need to remain anonymous while communicating with clients or customers. SMS spoofing can be employed to hide the sender's identity and maintain confidentiality.
					<h1 id=""conclusion"">Wrapping Up</h1>
					SMS spoofing is a growing concern in our increasingly digital world, offering an easy route for fraudsters to trick individuals and gather sensitive data. It's essential to stay alert and use preventive measures to protect yourself and your organization. While there are legitimate uses for SMS spoofing, it's critical to understand the potential risks and misuse. By staying informed and vigilant, we can ensure safe and secure digital communication.
				</div>
            </div>
        </div>
    </section>";
                string pattern = $@"\b\w*{word}\w*\b";
                Regex wordMatch = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
                Match match = wordMatch.Match(text);

                if (match.Success)
                {
                    int start = Math.Max(0, match.Index - 30);
                    int length = Math.Min(text.Length - start, 80);
                    return text.Substring(start, length);
                }

                return null;
            }
            catch (RegexMatchTimeoutException)
            {
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
            
            //try
            //{
            //    string expression = @"((^.{0,30}|\w*.{30})\b" + word + @"\b(.{50}\w*|.{0,50}$))";
            //    Regex wordMatch = new Regex(expression, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            //    Match m = wordMatch.Match(text);
            //    var matchData = m.Value;

            //    return matchData;
            //}
            //catch (Exception)
            //{
            //    return null;
            //}
        }
    }
}
