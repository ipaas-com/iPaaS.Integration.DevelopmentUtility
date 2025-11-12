using GraphQL.Types;
using Integration.Abstract.Helpers;
using IntegrationDevelopmentUtility.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
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

            //validate that the connection type matches the type specified in the file
            ;


            if (devTests == null)
            {
                StandardUtilities.WriteToConsole($"The integration file does not include a class for named DevelopmentTests or it is not in the standard namespace. See the documentation for more details.", StandardUtilities.Severity.LOCAL_ERROR);
                Program.OperationCancelled = true;
                Program.OperationCompleted = true;
                return;
            }

            Type devTestType = devTests.GetType();
            ParsedMethodDetails parsedMethodDetails = new ParsedMethodDetails(methodName);

            //Add all the parameters to a parameter array so we can search for the correct method instance
            var parameterArray = new object[parsedMethodDetails.Parameters.Count + 1];
            parameterArray[0] = connection;
            for(int i = 0; i < parsedMethodDetails.Parameters.Count; i++)
            {
                parameterArray[i + 1] = parsedMethodDetails.Parameters[i];
            }

            var theMethod = StandardUtilities.FindBestMethod(devTestType, parsedMethodDetails.Name, parameterArray);

            //MethodInfo theMethod = devTestType.GetMethod(parsedMethodDetails.Name);
            if (theMethod == null)
            {
                Program.OperationCancelled = true;
                Program.OperationCompleted = true;
                StandardUtilities.WriteToConsole($"The method name \"{methodName}\" does not exist.", StandardUtilities.Severity.LOCAL_ERROR);
                return;
            }

            var theMethodParameters = theMethod.GetParameters();
            if(theMethodParameters.Length != parsedMethodDetails.Parameters.Count + 1)
            {
                Program.OperationCancelled = true;
                Program.OperationCompleted = true;
                if(parsedMethodDetails.Parameters.Count == 0)
                    StandardUtilities.WriteToConsole($"The method must accept only one parameter of the type Integration.Abstract.Connection.", StandardUtilities.Severity.LOCAL_ERROR);
                else
                    StandardUtilities.WriteToConsole($"The method parameter count does not match. Expected {parsedMethodDetails.Parameters.Count + 1} parameters but found {theMethodParameters.Length}.", StandardUtilities.Severity.LOCAL_ERROR);
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

            var methodParamList = new List<object>();
            methodParamList.Add(connection); 
            methodParamList.AddRange(parsedMethodDetails.Parameters);

            var methodParams = methodParamList.ToArray();

            Task result = (Task)theMethod.Invoke(devTests, methodParams);
            result.GetAwaiter().GetResult();

            // Save Persistent Data (check to see if there is any data)
            if (connection.Settings.PersistentData != null && connection.Settings.PersistentData.Values != null && connection.Settings.PersistentData.Values.Count > 0)
                iPaaSCallWrapper.PersistentData(connection.ExternalSystemId, connection.Settings.PersistentData.Values, company.CompanySpecificFullToken);
        }
    }

    public class ParsedMethodDetails
    {
        public ParsedMethodDetails(string nameAndParameters)
        {
            if (string.IsNullOrEmpty(nameAndParameters))
                return;

            if(nameAndParameters.IndexOf("(") == -1)
            {
                Name = nameAndParameters;
                Parameters = new List<string>();
                return;
            }

            Name = nameAndParameters.Substring(0, nameAndParameters.IndexOf("("));
            var paramsString = nameAndParameters.Substring(nameAndParameters.IndexOf("(") + 1);
            paramsString = paramsString.Substring(0, paramsString.Length - 1); //Remove trailing )
            var splitParams = paramsString.Split(',');
            Parameters = splitParams.ToList();

            //For each parameter, trim whitespace and remove quotes if they exist
            for(int i = 0; i < Parameters.Count; i++)
            {
                Parameters[i] = Parameters[i].Trim();
                if(Parameters[i].StartsWith("\"") && Parameters[i].EndsWith("\""))
                {
                    Parameters[i] = Parameters[i].Substring(1, Parameters[i].Length - 2);
                }
            }

        }

        public string Name { get; set; }
        public List<string> Parameters { get; set; }
    }
}
