using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ASPNETCoreWithServerCalls.Codes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace ASPNETCoreWithServerCalls.Pages
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
            using (ComProxy proxy = new ComProxy())
            {
                // query database(s)
                DatabaseResponse dbResponse = proxy.GetDatabases();
                foreach (KeyAndValueItem item in dbResponse.Items)
                {
                    _logger.LogInformation($"DbId: {item.Id}, Name: {item.Name}");
                    // query each db details
                    SPDatabaseDetailsResponse dbDetails = proxy.GetDatabaseDetails(new SPDatabaseDetailsRequest() { DatabaseId = item.Id });
                    _logger.LogInformation($"Currency: {dbDetails.Currency}");
                    _logger.LogInformation($"Population: {dbDetails.Population.ToString()}");
                    _logger.LogInformation($"Sample: {dbDetails.Sample.ToString()}");
                    _logger.LogInformation("Languages:");
                    foreach (KeyAndValueItem langItem in dbDetails.Languages)
                    {
                        _logger.LogInformation($"{langItem.Id}, name: {langItem.Name}");
                    }
                    _logger.LogInformation("-----------");
                }
            }
        }
    }
}
