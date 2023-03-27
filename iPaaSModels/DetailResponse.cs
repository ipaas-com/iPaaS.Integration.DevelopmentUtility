using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDevelopmentUtility.iPaaSModels
{
    public class DetailResponse
    {
        [JsonProperty("application", Order = 10)]
        public string Application { get; set; }

        [JsonProperty("activity", Order = 15)]
        public string Activity { get; set; }

        [JsonProperty("status", Order = 20)]
        public string Status { get; set; }

        [JsonProperty("mapping_collection_type_id", Order = 25)]
        public int? MappingCollectionTypeId { get; set; }

        [JsonProperty("activity_timestamp", Order = 30)]
        public DateTimeOffset ActivityTimestamp { get; set; }

        [JsonProperty("details", Order = 35)]
        public string Details { get; set; }

        public void PrintToConsole()
        {
            var severity = Utilities.StandardUtilities.Severity.DETAIL;
            var severityPrint = "       ";
            if (Details.EndsWith("(INFO)"))
            {
                severity = Utilities.StandardUtilities.Severity.DETAIL;
                severityPrint = "INFO   ";
                Details = Details.Substring(0, Details.Length - 6);
            }
            else if (Details.EndsWith("(VERBOSE)"))
            {
                severity = Utilities.StandardUtilities.Severity.VERBOSE;
                severityPrint = "VERBOSE";
                Details = Details.Substring(0, Details.Length - 9);
            }
            else if (Details.EndsWith("(WARNING)"))
            {
                severity = Utilities.StandardUtilities.Severity.WARNING;
                severityPrint = "WARNING";
                Details = Details.Substring(0, Details.Length - 9);
            }
            else if (Details.EndsWith("(ERROR)"))
            {
                severity = Utilities.StandardUtilities.Severity.ERROR;
                severityPrint = "ERROR  ";
                Details = Details.Substring(0, Details.Length - 7);
            }

            //ToString("yyyy/MM/dd HH:mm:ss.fff zzz")
            var formattedDatetime = ActivityTimestamp.ToString("HH:mm:ss.fff zzz");
            var prefix = formattedDatetime + " " + severityPrint;

            //Utilities.StandardUtilities.WriteToConsole($"{ActivityTimestamp} {Activity} {Details}", severity);
            Utilities.StandardUtilities.WriteToConsole($"{Activity} {Details}", severity, prefix);
        }
    }
}
