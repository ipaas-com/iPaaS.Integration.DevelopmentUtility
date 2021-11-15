using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace IntegrationDevelopmentUtility.Utilities
{
    public class HookController
    {
        public static void SendHookAndListenForLogData(long externalSystemId, string scope, string externalId, string direction)
        {
            var trackingGuid = Guid.NewGuid();

            var success = SendHook(trackingGuid, externalSystemId, scope, externalId, direction);
            //If the sendHook call was not completed, it should return false.
            if (!success)
                return;

            var companyToken = StandardUtilities.ApiTokenForSystem(externalSystemId);

            DateTimeOffset? cutoffDTO = DateTimeOffset.MinValue;
            DateTimeOffset maxDateFound = DateTimeOffset.MinValue;

            bool offrampKeyFound = false;

            while (true)
            {
                if(Program.OperationCancelled)
                {
                    StandardUtilities.WriteToConsole($"Listener cancelled", StandardUtilities.Severity.LOCAL);
                    return;
                }

                var response = iPaaSCallWrapper.RetrieveLogDetail(trackingGuid, cutoffDTO, companyToken);

                //StandardUtilities.WriteToConsole($"Finished search with DTO {cutoffDTO.Value.ToString("yyyy/MM/dd HH:mm:ss.fff zzz")}", StandardUtilities.Severity.LOCAL);
                if (response != null && response.Count > 0)
                {
                    //Sort the list by act
                    response.Sort((x, y) => x.ActivityTimestamp.CompareTo(y.ActivityTimestamp));

                    foreach (var detail in response)
                    {
                        detail.PrintToConsole();
                        if (detail.ActivityTimestamp > maxDateFound)
                            maxDateFound = detail.ActivityTimestamp;
                        if (detail.Details.Contains($"Reporting hook complete {trackingGuid}")) //Indicates the end of a normal transaction
                            offrampKeyFound = false;
                        else if(detail.Details.Contains("Completed loading meta data"))//Indicates the end of a metadata transaction
                            offrampKeyFound = true;
                    }
                    cutoffDTO = maxDateFound;
                    //StandardUtilities.WriteToConsole($"Setting search to {startReqDTO.ToString("yyyy/MM/dd HH:mm:ss.fff zzz")}", StandardUtilities.Severity.LOCAL);
                }
                //else
                //    StandardUtilities.WriteToConsole($"No results found", StandardUtilities.Severity.LOCAL);

                //If we found the magical phrase that tells us the transfer is complete, then exit
                if (offrampKeyFound)
                    break;

                //If we go one minute without recieving any updated log data, exit.
                if (cutoffDTO.HasValue && cutoffDTO.Value != DateTimeOffset.MinValue && cutoffDTO.Value.AddMinutes(5) < DateTimeOffset.Now)
                {
                    StandardUtilities.WriteToConsole($"No new log data found for 5 minutes. Transfer is likely complete. Aborting log listener.", StandardUtilities.Severity.LOCAL);
                    break;
                }

                //If we have gone 5 minutes since launch, and there is no log data, we give up
                if (!cutoffDTO.HasValue && maxDateFound.AddMinutes(5) < DateTimeOffset.Now)
                {
                    StandardUtilities.WriteToConsole($"No log data found after 5 mintues of waiting. Aborting log listener.", StandardUtilities.Severity.LOCAL);
                    break;
                }

                Thread.Sleep(Settings.Instance.HookReadIntervalMS); //Sleep 5 seconds
            }

            Program.OperationCompleted = true;
            StandardUtilities.WriteToConsole($"Transfer is complete", StandardUtilities.Severity.LOCAL);
        }

        public static bool SendHook(Guid trackingGuid, long externalSystemId, string scope, string externalId, string direction)
        {
            iPaaSModels.WebhookRequest webhookRequest = new iPaaSModels.WebhookRequest();
            webhookRequest.Notifications = new System.Collections.Generic.List<iPaaSModels.Notification>();

            //if (!scope.EndsWith("/debug"))
            //    scope += "/debug";

            var notification = new iPaaSModels.Notification();
            notification.Destination = Convert.ToString(externalSystemId);
            notification.TrackingGuid = trackingGuid;
            notification.Scope = scope;
            if (direction == "FROM")
                notification.Id = externalId;
            else
                notification.ExternalId = externalId;

            webhookRequest.Notifications.Add(notification);

            var hookToken = StandardUtilities.HookTokenForSystem(externalSystemId);

            if (string.IsNullOrEmpty(hookToken))
            {
                StandardUtilities.WriteToConsole($"Unable to retrieve token for system {externalSystemId}. Hook will not be sent. Ensure this is a system your account has access to.", StandardUtilities.Severity.LOCAL_ERROR);
                Program.OperationCancelled = true;
                Program.OperationCompleted = true;
                return false;
            }

            StandardUtilities.WriteToConsole($"Sending hook: ExternalSystemId: {externalSystemId}, Scope: {scope}, ExternalId: {externalId}, Direction: {direction}, TrackingId: {trackingGuid}", StandardUtilities.Severity.LOCAL);

            iPaaSCallWrapper.SendHook(webhookRequest, direction, hookToken);

            StandardUtilities.WriteToConsole($"Send complete. Log output will appear below", StandardUtilities.Severity.LOCAL);

            return true;
        }
    }
}
