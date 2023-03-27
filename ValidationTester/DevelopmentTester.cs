using Integration.Abstract.Helpers;
using IntegrationDevelopmentUtility.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationDevelopmentUtility.ValidationTester
{
    public class DevelopmentTester
    {
        public static async Task ExecuteTestCase(string methodName, long systemId)
        {
            var system = Settings.Instance.Systems.Find(x => x.Id == systemId);
            if(system == null)
            {
                if (systemId == 0)
                    StandardUtilities.WriteToConsole($"System 0 specified but no system was specified in the configuration. See the documentaiton for more details on this feature.", StandardUtilities.Severity.LOCAL_ERROR);
                else
                    StandardUtilities.WriteToConsole($"You do not have access to the system specified: {systemId}", StandardUtilities.Severity.LOCAL_ERROR);

                Program.OperationCancelled = true;
                Program.OperationCompleted = true;
                return;
            }

            var company = Settings.Instance.Companies.Find(x => x.Systems.Exists(y => y.Id == systemId));

            if(string.IsNullOrEmpty(Settings.Instance.IntegrationFileLocation))
            {
                StandardUtilities.WriteToConsole("No filename specified. TEST requires a file specified in integration_file_location in the config file.", StandardUtilities.Severity.LOCAL_ERROR);
                Program.OperationCancelled = true;
                Program.OperationCompleted = true;
                return;

            }

            Integration.Abstract.Connection connection = null;
            object devTests = null;

            //UploadFile(fileLocation);
            try
            {
                var tupleResults = await ValidationTester.CreateConnection.Create(Settings.Instance.IntegrationFileLocation, "", system, company.CompanySpecificFullToken, company.iPaaSSystemId);
                connection = tupleResults.Item1;
                devTests = tupleResults.Item2;
            }
            catch(Exception ex)
            {
                StandardUtilities.WriteToConsole($"Unable to create a connection to the system specified: {systemId}", StandardUtilities.Severity.LOCAL_ERROR);
                StandardUtilities.WriteToConsole(ex, StandardUtilities.Severity.LOCAL_ERROR);
                Program.OperationCancelled = true;
                Program.OperationCompleted = true;
                return;
            }

            if(!connection.CallWrapper.Connected)
            {
                StandardUtilities.WriteToConsole($"Unable to connect to the system specified: {systemId}", StandardUtilities.Severity.LOCAL_ERROR);
                Program.OperationCancelled = true;
                Program.OperationCompleted = true;
                return;
            }

            if(devTests == null)
            {
                StandardUtilities.WriteToConsole($"The integration file does not include a class for named DevelopmentTests or it is not in the standard namespace. See the documentation for more details.", StandardUtilities.Severity.LOCAL_ERROR);
                Program.OperationCancelled = true;
                Program.OperationCompleted = true;
                return;
            }

            Type devTestType = devTests.GetType();
            MethodInfo theMethod = devTestType.GetMethod(methodName);
            if(theMethod == null)
            {
                Program.OperationCancelled = true;
                Program.OperationCompleted = true;
                StandardUtilities.WriteToConsole("The method name specified does not exist.", StandardUtilities.Severity.LOCAL_ERROR);
                return;
            }

            var theMethodParameters = theMethod.GetParameters();
            if(theMethodParameters.Length != 1)
            {
                Program.OperationCancelled = true;
                Program.OperationCompleted = true;
                StandardUtilities.WriteToConsole("The method must accept only one parameter.", StandardUtilities.Severity.LOCAL_ERROR);
                return;
            }

            if(theMethodParameters[0].ParameterType != typeof(Integration.Abstract.Connection))
            {
                Program.OperationCancelled = true;
                Program.OperationCompleted = true;
                StandardUtilities.WriteToConsole("The method must accept only one parameter of the type Integration.Abstract.Connection.", StandardUtilities.Severity.LOCAL_ERROR);
                return;
            }

            CallContext.SetData("Connection", connection);

            var methodParams = new object[1];
            methodParams[0] = connection;

            Task result = (Task)theMethod.Invoke(devTests, methodParams);
            result.GetAwaiter().GetResult();

            // Save Persistent Data (check to see if there is any data)
            if (connection.Settings.PersistentData != null && connection.Settings.PersistentData.Values != null && connection.Settings.PersistentData.Values.Count > 0)
                iPaaSCallWrapper.PersistentData(connection.ExternalSystemId, connection.Settings.PersistentData.Values, company.CompanySpecificFullToken);
        }
    }
}
