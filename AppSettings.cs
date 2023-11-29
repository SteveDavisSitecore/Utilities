using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities
{
    public class AppSettings
    {
        public bool Live { get; set; }
        public string CatalogID { get; set; }
        public string IntegrationClientId { get; set; }
        public string IntegrationClientSecret { get; set; }
        public string ApiUrl { get; set; }
        public string AuthUrl { get; set; }
        public string BuyerID { get; set; }
        public string AnonUserID { get; set; }
        public string UserEmail { get; set; }
        public string SecurityProfileID { get; set; }
    }
}
