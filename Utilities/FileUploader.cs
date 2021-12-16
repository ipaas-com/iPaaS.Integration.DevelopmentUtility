using Azure;
using IntegrationDevelopmentUtility.iPaaSModels;
using IntegrationDevelopmentUtility.ValidationTester;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace IntegrationDevelopmentUtility.Utilities
{
    public class FileUploader
    {

        public static void UploadFile(string fullFilePath)
        {
            if (Settings.Instance.Systems.Count == 0)
            {
                StandardUtilities.WriteToConsole($"You must have access to at least one valid system to upload a file.", StandardUtilities.Severity.LOCAL_ERROR);
                Program.OperationCancelled = true;
                Program.OperationCompleted = true;
                return;
            }

            StandardUtilities.WriteToConsole($"Uploading file", StandardUtilities.Severity.LOCAL);

            //Ensure that the file is less than 10 MB
            long fileSize = new System.IO.FileInfo(fullFilePath).Length;
            if(fileSize > 10000000) //Ensure the file is less than 10MB
            {
                StandardUtilities.WriteToConsole("File must be less than 10MB", StandardUtilities.Severity.LOCAL_ERROR);
                Program.OperationCancelled = true;
                Program.OperationCompleted = true;
                return;
            }

            int systemTypeId, versionMajor, versionMinor;
            Integration.Abstract.MetaData metaData;

            SystemTypeVersionResponse versionResponse = null;
            try
            {
                //Load metadata
                metaData = LoadMetaData(fullFilePath);

                versionMajor = metaData.Info.VersionMajor;
                versionMinor = metaData.Info.VersionMinor;
                systemTypeId = metaData.Info.SystemTypeId;
            }
            catch (Exception ex)
            {
                StandardUtilities.WriteToConsole("Unable to upload file: Error reading MetaData.", StandardUtilities.Severity.LOCAL_ERROR);
                StandardUtilities.WriteToConsole(ex, StandardUtilities.Severity.LOCAL_ERROR);
                Program.OperationCancelled = true;
                Program.OperationCompleted = true;
                return;
            }

            //Find a system with same systemType. Users are only allowed to upload files for a system type they have access to
            var matchingSystem = Settings.Instance.Systems.Find(x => x.SystemTypeId == systemTypeId);
            if (matchingSystem == null && systemTypeId != 0)
            {
                StandardUtilities.WriteToConsole($"You must have access to at least one system of the requested system type to upload a file. (System Type {systemTypeId})", StandardUtilities.Severity.LOCAL_ERROR);
                Program.OperationCancelled = true;
                Program.OperationCompleted = true;
                return;
            }

            //Pull the token for this system
            var companyToken = StandardUtilities.ApiTokenForSystem(matchingSystem.Id);

            SettingResponse matchSystem;
            //Find the version, if there are any. It may be that there are no versions yet.
            if (versionResponse != null)
            {
                //System updated hooks need a valid system id for permission purposes. So we just grab the first system.
                matchSystem = Settings.Instance.Systems.Find(x => x.SystemTypeId == systemTypeId && x.SystemTypeVersionId == versionResponse.Id);
                if (matchSystem == null)
                {
                    StandardUtilities.WriteToConsole($"You must have access to at least one system of the requested system type version to upload a file. (System Type {systemTypeId}, Version Id {versionResponse.Id})", StandardUtilities.Severity.LOCAL_ERROR);
                    Program.OperationCancelled = true;
                    Program.OperationCompleted = true;
                    return;
                }
            }
            else
                matchSystem = matchingSystem;

            try
            {
                var request = new SystemTypeVersionRequest();

                //Use the retrieved metadata to create the systemtypeversion request
                request.Name = metaData.Info.Name;
                request.DllName = metaData.Info.IntegrationFilename;
                request.DllNamespace = metaData.Info.IntegrationNamespace;
                request.Status = "Test";
                request.VersionMajor = versionMajor;
                request.VersionMinor = versionMinor;

                versionResponse = iPaaSCallWrapper.UploadFile(systemTypeId, fullFilePath, request, companyToken.AcessToken);
                if (versionResponse == null)
                    throw new Exception("No response was returned from the Upload command");
            }
            catch (Exception ex)
            {
                StandardUtilities.WriteToConsole("Unable to upload file.", StandardUtilities.Severity.LOCAL_ERROR);
                StandardUtilities.WriteToConsole(ex, StandardUtilities.Severity.LOCAL_ERROR);
                Program.OperationCancelled = true;
                Program.OperationCompleted = true;
                return;
            }

            var fileName = Path.GetFileName(fullFilePath);

            //Send the system refresh hook, which will reboot the C3POs. We send this with a random GUID, since we don't need to track it.
            HookController.SendHook(Guid.NewGuid(), matchSystem.Id, "system/service/refresh", Convert.ToString(versionResponse.DllName), "FROM");

            StandardUtilities.WriteToConsole("Waiting for uploaded file to become available", StandardUtilities.Severity.LOCAL);

            //Sleep for 30 seconds to give C3PO time to restart
            Thread.Sleep(Settings.Instance.FileUploadDelayIntervalSecs * 1000);

            StandardUtilities.WriteToConsole("Processing system meta data", StandardUtilities.Severity.LOCAL);

            HookController.SendHookAndListenForLogData(matchSystem.Id, "system/type/created", Convert.ToString(versionResponse.DllName), "FROM");
        }

        private static Integration.Abstract.MetaData LoadMetaData(string fileLocation, string dllNamespace = null)
        {
            var assemblyHandler = new AssemblyHandler(fileLocation);

            if (string.IsNullOrEmpty(dllNamespace))
                assemblyHandler.DetermineNamespaceByClassName("MetaData");
            else
                assemblyHandler.ExternalNamespace = dllNamespace;

            var metaData = assemblyHandler.CreateInstance<Integration.Abstract.MetaData>("MetaData");
            metaData.LoadMetaData();

            return metaData;
        }
    }
}
