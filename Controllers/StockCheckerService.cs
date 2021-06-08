using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FlipkartProductTrackerBackend.Controllers
{
    public class StockCheckerService
    {
        //private readonly ILogger<WeatherForecastController> _logger;
        private readonly IConfiguration _config;

        //ILogger<WeatherForecastController> logger,
        public StockCheckerService(IConfiguration config)
        {
            //_logger = logger;
            _config = config;
        }

        public async Task<bool> GetProductStockStatus()
        {
            //_logger.Log(LogLevel.Information, $"Product URL: {_config.GetSection("PS5_PRODUCT_URL").Value}");
            string stock_status = "";
            const string path = @"C:\";
            const string fileName = "PS5Page.txt";

            //_logger.Log(LogLevel.Information, "Checking if path exists/cleanup");

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            else
            {
                if (System.IO.File.Exists(Path.Combine(path, fileName)))
                    System.IO.File.Delete(Path.Combine(path, fileName));
            }

            WebClient wb_cl = new WebClient();

            //_logger.Log(LogLevel.Information, "Adding headers to request");
            wb_cl.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4324.182 Safari/537.36 Edg/88.0.705.81");

            //_logger.Log(LogLevel.Information, "Begin call to FK to grab html page");

            using (Stream htmlData = wb_cl.OpenRead(_config.GetSection("PS5_PRODUCT_URL").Value))
            {
                //_logger.Log(LogLevel.Information, "Reading webpage");
                StreamReader sr = new StreamReader(htmlData);
                stock_status = sr.ReadToEnd();

                //_logger.Log(LogLevel.Information, "Storing webpage locally before performing any operations");
                using (FileStream fs = System.IO.File.Create(Path.Combine(path, fileName)))
                {
                    byte[] pageData = new UTF8Encoding(true).GetBytes(stock_status);

                    //_logger.Log(LogLevel.Information, "File stored successfully");
                    await fs.WriteAsync(pageData, 0, pageData.Length);
                }

                //_logger.Log(LogLevel.Information, "Begin html clean up");
                await CleanUpHTML(path, fileName);

                //_logger.Log(LogLevel.Information, "Check stock availability");
                return CheckAvailability(path, fileName);
            }
        }

        private async Task CleanUpHTML(string path, string fileName)
        {
            if (Directory.Exists(path))
            {
                if (System.IO.File.Exists(Path.Combine(path, fileName)))
                {
                    string HTMLData = "";
                    //_logger.Log(LogLevel.Information, "Begin reading file");
                    FileStream fileStream = new FileStream(Path.Combine(path, fileName), FileMode.Open, FileAccess.Read);

                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        HTMLData = reader.ReadToEnd();

                        //_logger.Log(LogLevel.Information, "Finding text between body tag");
                        int startIndex = HTMLData.IndexOf("<body>");
                        int endIndex = HTMLData.IndexOf("</body>", startIndex);

                        string bodyTagData = HTMLData.Substring(startIndex, endIndex + 7 - startIndex);

                        //_logger.Log(LogLevel.Information, "Begin removing script tag text");
                        while (bodyTagData.Contains("<script") || bodyTagData.Contains("</script>"))
                        {
                            int scriptSIndex = bodyTagData.IndexOf("<script");
                            int scriptEIndex = bodyTagData.IndexOf("</script>", scriptSIndex);

                            if (scriptSIndex > -1 && scriptEIndex > -1)
                            {
                                string scriptTagData = bodyTagData.Substring(scriptSIndex, scriptEIndex + 9 - scriptSIndex);
                                bodyTagData = bodyTagData.Replace(scriptTagData, string.Empty);
                            }
                        }

                        //_logger.Log(LogLevel.Information, "Begin removing svg tag text");
                        while (bodyTagData.Contains("<svg") || bodyTagData.Contains("</svg>"))
                        {
                            int svgSIndex = bodyTagData.IndexOf("<svg");
                            int svgEIndex = bodyTagData.IndexOf("</svg>", svgSIndex);

                            if (svgSIndex > -1 && svgEIndex > -1)
                            {
                                string svgTagData = bodyTagData.Substring(svgSIndex, svgEIndex + 6 - svgSIndex);
                                bodyTagData = bodyTagData.Replace(svgTagData, string.Empty);
                            }
                        }

                        //_logger.Log(LogLevel.Information, "Begin removing img tag text");
                        while (bodyTagData.Contains("<img"))
                        {
                            int imgSIndex = bodyTagData.IndexOf("<img");
                            int imgEIndex = bodyTagData.IndexOf("/>", imgSIndex);

                            if (imgSIndex > -1 && imgEIndex > -1)
                            {
                                string imgTagData = bodyTagData.Substring(imgSIndex, imgEIndex + 2 - imgSIndex);
                                bodyTagData = bodyTagData.Replace(imgTagData, string.Empty);
                            }
                        }

                        //_logger.Log(LogLevel.Information, "Begin removing footer tag text");
                        int footerSIndex = bodyTagData.IndexOf("<footer");
                        int footerEIndex = bodyTagData.IndexOf("</footer>", footerSIndex);

                        if (footerSIndex > -1 && footerEIndex > -1)
                        {
                            string footerTagData = bodyTagData.Substring(footerSIndex, footerEIndex + 9 - footerSIndex);
                            bodyTagData = bodyTagData.Replace(footerTagData, string.Empty);
                        }

                        //_logger.Log(LogLevel.Information, "Begin removing ul tag text");
                        while (bodyTagData.Contains("<ul") || bodyTagData.Contains("</ul>"))
                        {
                            int ulSIndex = bodyTagData.IndexOf("<ul");
                            int ulEIndex = bodyTagData.IndexOf("</ul>", ulSIndex);

                            if (ulSIndex > -1 && ulEIndex > -1)
                            {
                                string ulTagData = bodyTagData.Substring(ulSIndex, ulEIndex + 5 - ulSIndex);
                                if (ulTagData.Contains("ADD TO CART", System.StringComparison.InvariantCultureIgnoreCase) || ulTagData.Contains("BUY NOW", System.StringComparison.InvariantCultureIgnoreCase))
                                    break;
                                bodyTagData = bodyTagData.Replace(ulTagData, string.Empty);
                            }
                        }

                        //_logger.Log(LogLevel.Information, "Begin removing ratings/comment section");
                        int ratingSIndex = bodyTagData.IndexOf("Ratings &amp; Reviews");
                        int ratingsEIndex = bodyTagData.IndexOf("</body>", ratingSIndex);

                        if (ratingSIndex > -1 && ratingsEIndex > -1)
                        {
                            string ratingsSecData = bodyTagData.Substring(ratingSIndex, ratingsEIndex + 7 - ratingSIndex);
                            bodyTagData = bodyTagData.Replace(ratingsSecData, string.Empty);
                        }

                        HTMLData = bodyTagData;
                    }

                    System.IO.File.WriteAllText(Path.Combine(path, fileName), string.Empty);

                    //_logger.Log(LogLevel.Information, "Writing cleaned up text to file");
                    using (FileStream fs = System.IO.File.Open(Path.Combine(path, fileName), FileMode.Open))
                    {
                        byte[] pageData = new UTF8Encoding(true).GetBytes(HTMLData);

                        await fs.WriteAsync(pageData, 0, pageData.Length);
                    }
                }
            }
        }

        public bool CheckAvailability(string path, string fileName)
        {
            if (Directory.Exists(path))
            {
                if (System.IO.File.Exists(Path.Combine(path, fileName)))
                {
                    string HTMLData = "";
                    //_logger.Log(LogLevel.Information, "Begin reading file");
                    FileStream fileStream = new FileStream(Path.Combine(path, fileName), FileMode.Open, FileAccess.Read);

                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        HTMLData = reader.ReadToEnd();

                        if (HTMLData.Contains("NOTIFY ME", System.StringComparison.InvariantCultureIgnoreCase) || HTMLData.Contains("Get notified when this item comes back in stock.", System.StringComparison.InvariantCultureIgnoreCase))
                        {
                            return false;
                        }
                        else if (HTMLData.Contains("ADD TO CART", System.StringComparison.InvariantCultureIgnoreCase) || HTMLData.Contains("BUY NOW", System.StringComparison.InvariantCultureIgnoreCase))
                        {
                            return true;
                        }
                    }

                    return false;
                }
                else
                    return false;
            }
            else
                return false;
        }
    }
}