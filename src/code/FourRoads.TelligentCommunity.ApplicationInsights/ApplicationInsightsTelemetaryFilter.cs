using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace FourRoads.TelligentCommunity.ApplicationInsights
{

    public class ApplicationInsightsTelemetaryFilter : ITelemetryProcessor
    {
        private ITelemetryProcessor Next { get; set; }

        public ApplicationInsightsTelemetaryFilter(ITelemetryProcessor next)
        {
            Next = next;
        }

        public void Process(ITelemetry item)
        {
            if (!string.IsNullOrEmpty(item.Context.Operation.SyntheticSource))
            {
                return;
            }

            var request = item as RequestTelemetry;
            if (request != null)
            {
                // Determine tenant
                if (request.Url.PathAndQuery.ToLower().Contains("socket.ashx"))
                    return;
            }

            Next.Process(item);
        }
    }
}