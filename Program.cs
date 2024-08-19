using Azure;
using IntegrationDevelopmentUtility.iPaaSModels;
using IntegrationDevelopmentUtility.Utilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Azure.Core.HttpHeader;

namespace IntegrationDevelopmentUtility
{
    class Program
    {
        public static bool OperationCancelled = false;
        public static bool OperationCompleted = false;
        public static Assembly AssemblyA;

        static async Task Main(string[] args)
        {
            //To prevent any crashed from unobserved exceptions (aka Joe Bugs), we add a handler for them.
            //For more details on this phenomenon, see https://medium.com/@anandgupta.08/curious-case-of-exceptions-in-async-methods-part-2-4a99841ae755
            var handler = new EventHandler<UnobservedTaskExceptionEventArgs>(UnobservedTaskExceptionHandler);
            TaskScheduler.UnobservedTaskException += handler;

            //TODO: Load from settings file
            //Settings.HookUrl = "https://devapi.ipaas.com/hookapi";
            //Settings.IntegrationUrl = "https://devapi.ipaas.com/integrations";
            //Settings.LoggerUrl = "https://devapi.ipaas.com/listener";
            //Settings.SSOUrl = "https://devapi.ipaas.com/sso";

            //Settings.AzureFileShareConnectionString = "DefaultEndpointsProtocol=https;AccountName=integrationdevshare;AccountKey=20Gy5XDKzIR8PkmpQGYQrFjIkbtfVwMM/yD1NF5z8Sfd9PH9JlGbdLz4F8t3b37eo/RkmM3VgZie20tQS70AJA==;EndpointSuffix=core.windows.net";

            //Load the default settings
            Settings.Instance = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(Directory.GetCurrentDirectory() + @"\appsettings.json"));

            if (!StandardUtilities.ValidateNotProduction())
            {
                StandardUtilities.WriteToConsole("Running this product against the Production environment is not supported.", StandardUtilities.Severity.LOCAL_ERROR);
                return;
            }
            ;

            //ZOMBIE TESTING!
            //load all the dlls before we add a resolution handler
            //AssemblyA = Assembly.LoadFrom(Settings.Instance.IntegrationFileLocation);
            //var assemblyList = new List<string>();
            //assemblyList.Add("Shopify.Data.EmbeddedAssemblies.GraphQL.Client.Abstractions.dll");
            //assemblyList.Add("Shopify.Data.EmbeddedAssemblies.GraphQL.Client.Abstractions.Websocket.dll");
            //assemblyList.Add("Shopify.Data.EmbeddedAssemblies.GraphQL.Client.dll");
            //assemblyList.Add("Shopify.Data.EmbeddedAssemblies.GraphQL.Client.Serializer.Newtonsoft.dll");
            //assemblyList.Add("Shopify.Data.EmbeddedAssemblies.GraphQL.dll");
            //assemblyList.Add("Shopify.Data.EmbeddedAssemblies.GraphQL.Primitives.dll");

            //foreach (var assemblyName2 in assemblyList)
            //{
            //    Console.WriteLine($"Loading {assemblyName2}");
            //    var assembly = Assembly.Load(assemblyName2);
            //    additional.Add(assembly.FullName, assembly);
            //    Console.WriteLine($"Complete {assemblyName2}");
            //}

            //now we can add our handler
            //AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            try
            {
                StandardUtilities.Login();
            }
            catch (Exception ex)
            {
                StandardUtilities.WriteToConsole("Unable to complete the startup process: " + ex.Message, StandardUtilities.Severity.LOCAL_ERROR);
                return;
            }

            if (Settings.Instance.SystemSettings != null && Settings.Instance.SystemSettings.Count > 0)
            {
                StandardUtilities.WriteToConsole("System settings specified in config file. Creating system 0.", StandardUtilities.Severity.LOCAL);
                StandardUtilities.CreateSystemZero();
            }

            while (true)
            {
                Console.WriteLine("Enter test command (UPLOAD, HOOK, TEST, BUILDMODELS):");
                var resp = Console.ReadLine();
                if (resp == null)
                    continue;

                resp = resp.Trim(); //remove any leading or trailing white space
                OperationCancelled = false;
                OperationCompleted = false;

                try
                {
                    var parsed = ParseCommandArguments(resp);
                    if (parsed == null || parsed.Length == 0)
                    {
                        Console.WriteLine("Invalid entry method");
                    }
                    else if (resp.ToUpper() == "UPLOAD /?" || resp.ToUpper() == "TEST /?" || resp.ToUpper() == "HOOK /?" || resp.ToUpper() == "BUILDMODELS /?")
                    {
                        switch (resp.ToUpper())
                        {
                            case "UPLOAD /?":
                                StandardUtilities.PrintUsageDetail("UPLOAD");
                                break;
                            case "TEST /?":
                                StandardUtilities.PrintUsageDetail("TEST");
                                break;
                            case "HOOK /?":
                                StandardUtilities.PrintUsageDetail("HOOK");
                                break;
                            case "BUILDMODELS /?":
                                StandardUtilities.PrintUsageDetail("BUILDMODELS");
                                break;
                        }
                    }
                    else if (parsed[0].ToUpper() == "UPLOAD")
                    {
                        string uploadFileName;
                        long integrationId;

                        //If no file name was specified, we use the default file in the settings
                        if (parsed.Length == 1)
                        {
                            if (string.IsNullOrEmpty(Settings.Instance.IntegrationFileLocation))
                            {
                                StandardUtilities.WriteToConsole("UPLOAD requires an integration id and filename, or a values specified in the config file. Run UPLOAD /? for more details", StandardUtilities.Severity.LOCAL);
                                continue;
                            }
                            else if (!Settings.Instance.IntegrationFileIntegrationId.HasValue)
                            {
                                StandardUtilities.WriteToConsole("UPLOAD requires an integration id and filename, or a values specified in the config file. Run UPLOAD /? for more details", StandardUtilities.Severity.LOCAL);
                                continue;
                            }
                            else
                            {
                                StandardUtilities.WriteToConsole($"Uploading the integration id/file specified in the configuration settings: {Settings.Instance.IntegrationFileIntegrationId}/{Settings.Instance.IntegrationFileLocation}", StandardUtilities.Severity.LOCAL);
                            }

                            uploadFileName = Settings.Instance.IntegrationFileLocation;
                            integrationId = Settings.Instance.IntegrationFileIntegrationId.Value;
                        }
                        else
                        {
                            //
                            if (parsed.Length != 3)
                            {
                                StandardUtilities.WriteToConsole("UPLOAD requires an integration id and filename, or a values specified in the config file. Run UPLOAD /? for more details", StandardUtilities.Severity.LOCAL);
                                continue;
                            }

                            //Strip quotes out, if specified
                            if (parsed[1].StartsWith("\"") && parsed[1].EndsWith("\""))
                                parsed[1] = parsed[1].Substring(1, parsed[1].Length - 2);

                            if (parsed[2].StartsWith("\"") && parsed[2].EndsWith("\""))
                                parsed[2] = parsed[2].Substring(1, parsed[2].Length - 2);

                            var integrationIdStr = parsed[1];
                            if (!long.TryParse(integrationIdStr, out integrationId))
                            {
                                StandardUtilities.WriteToConsole($"The IntegrationId supplied was not an integer. Value: {integrationIdStr}", StandardUtilities.Severity.LOCAL_ERROR);
                                continue;
                            }
                            uploadFileName = parsed[2];
                        }

                        //Clear the console so the hook will have clean output
                        //Console.Clear();

                        //Spawn the file uploader to a new thread
                        var thread = new Thread(() => FileUploader.UploadFile(integrationId, uploadFileName));
                        thread.Start();
                        ;

                        ConsoleKeyInfo? cki;
                        do
                        {
                            cki = StandardUtilities.ReadKeyWithTimeout();
                            // do something with each key press until escape key is pressed
                            if (OperationCompleted || OperationCancelled || (cki.HasValue && cki.Value.Key == ConsoleKey.Escape))
                            {
                                //Console.WriteLine("No longer listeing for escape");
                                break;
                            }
                            //Console.WriteLine("Listening for escape");
                        } while (true);

                        if (cki.HasValue && cki.Value.Key == ConsoleKey.Escape)
                        {
                            OperationCancelled = true;
                            //Console.WriteLine(" Cancelling log listener");
                            //Thread.Sleep(5 * 1000);
                        }
                    }
                    else if (parsed[0].ToUpper() == "HOOK")
                    {
                        if (parsed.Length != 5)
                        {
                            StandardUtilities.WriteToConsole("HOOK usage (all parameters should be enclosed in quotes): HOOK \"<System id>\" \"<Scope>\" \"<External Id>\" \"<Direction>\". Run HOOK /? for full usage details.", StandardUtilities.Severity.LOCAL);
                            continue;
                        }

                        //strip leading/trailing quotes
                        for (int i = 0; i < parsed.Length; i++)
                        {
                            if (parsed[i].StartsWith("\"") && parsed[i].EndsWith("\""))
                                parsed[i] = parsed[i].Substring(1, parsed[i].Length - 2);
                        }

                        //In order to be recieved properly, external ids have double quotes slash escaped. However, since we read that as a literal string, we need to manually
                        //  convert those escapes to just double quotes.
                        parsed[3] = parsed[3].Replace("\\\"", "\"");

                        //Clear the console so the hook will have clean output
                        Console.Clear();

                        RunHookLocal(parsed);

                        //var thread = new Thread(() => RunHookLocal(parsed));
                        //thread.Start();
                        //;

                        //ConsoleKeyInfo? cki;
                        //do
                        //{
                        //    cki = StandardUtilities.ReadKeyWithTimeout();
                        //    // do something with each key press until escape key is pressed
                        //    if (OperationCompleted || OperationCancelled || (cki.HasValue && cki.Value.Key == ConsoleKey.Escape))
                        //    {
                        //        //Console.WriteLine("No longer listeing for escape");
                        //        break;
                        //    }
                        //    //Console.WriteLine("Listening for escape");
                        //} while (true);

                        //if (cki.HasValue && cki.Value.Key == ConsoleKey.Escape)
                        //{
                        //    OperationCancelled = true;
                        //    //Console.WriteLine(" Cancelling log listener");
                        //    //Thread.Sleep(5 * 1000);
                        //}
                    }
                    else if (parsed[0].ToUpper() == "TEST")
                    {
                        if (parsed.Length != 3)
                        {
                            StandardUtilities.WriteToConsole("TEST usage: TEST <Method Name> <System Id>   Note: to use the configuration file settings, specifiy system 0. Run TEST /? for more details.", StandardUtilities.Severity.LOCAL);
                            continue;
                        }

                          await ValidationTester.DevelopmentTester.ExecuteTestCase(parsed[1], Int64.Parse(parsed[2]));
                    }
                    else if (parsed[0].ToUpper() == "APIKEYS")
                    {
                        if (Settings.Instance.AdminCompanies != null)
                            foreach (var company in Settings.Instance.AdminCompanies)
                            {
                                StandardUtilities.WriteToConsole($"AdminCompany {company.Name} {Environment.NewLine}Guid: {company.Id} {Environment.NewLine}Api Access Token (Valid until {company.CompanySpecificFullToken.AccessTokenExpiration.ToString("hh:mm:s tt")}): {company.CompanySpecificFullToken.AcessToken}", StandardUtilities.Severity.LOCAL);
                                //Create some space
                                StandardUtilities.WriteToConsole(Environment.NewLine + Environment.NewLine, StandardUtilities.Severity.LOCAL);
                            }

                        //this is an undocumented feature to list out all api keys that we have downlaoded. It allows for easy access to these values for API calls.
                        foreach (var company in Settings.Instance.Companies)
                        {
                            if (company.CompanySpecificFullToken == null)
                                StandardUtilities.WriteToConsole($"Company {company.Name} {Environment.NewLine}Guid: {company.Id} {Environment.NewLine} (No token available)", StandardUtilities.Severity.LOCAL);
                            else
                            {
                                //Calling ValidateFullToken will refresh the token, if we need to
                                iPaaSApiCall.ValidateFullToken(company.CompanySpecificFullToken);
                                StandardUtilities.WriteToConsole($"Company {company.Name} {Environment.NewLine}Guid: {company.Id} {Environment.NewLine}Api Access Token (Valid until {company.CompanySpecificFullToken.AccessTokenExpiration.ToString("hh:mm:s tt")}): {company.CompanySpecificFullToken.AcessToken}", StandardUtilities.Severity.LOCAL);
                            }

                            if (company.Systems == null)
                                continue;

                            foreach (var system in company.Systems)
                            {
                                var fullSystem = Settings.Instance.Systems.Find(x => x.Id == system.Id);
                                if (fullSystem != null)
                                    StandardUtilities.WriteToConsole($"     System {system.Name} ({system.Id}) Webhook Api Key {fullSystem.WebhookApiKey}", StandardUtilities.Severity.LOCAL);
                                else
                                    StandardUtilities.WriteToConsole($"     System {system.Name} ({system.Id}) (Full system details not found)", StandardUtilities.Severity.LOCAL);
                            }

                            //Create some space
                            StandardUtilities.WriteToConsole(Environment.NewLine + Environment.NewLine, StandardUtilities.Severity.LOCAL);
                        }
                    }
                    else if (parsed[0].ToUpper() == "INTEGRATORS")
                    {
                        if (Settings.Instance.IntegratorSystems != null)
                            foreach (var system in Settings.Instance.IntegratorSystems)
                            {
                                StandardUtilities.WriteToConsole($"IntegratorSystem {system.Name}, Version: {system.IntegrationVersionId} {Environment.NewLine}Guid: {system.Id} {Environment.NewLine}", StandardUtilities.Severity.LOCAL);
                                //Create some space
                                StandardUtilities.WriteToConsole(Environment.NewLine + Environment.NewLine, StandardUtilities.Severity.LOCAL);
                            }
                    }
                    else if (parsed[0].ToUpper() == "BUILDMODELS")
                    {
                        // Check that the required parameters were passed in
                        if (parsed.Length != 4)
                        {
                            StandardUtilities.WriteToConsole("BUILDMODELS requires a file path, API Name, and Namespace. Run BUILDMODELS /? for more details and examples", StandardUtilities.Severity.LOCAL);
                            continue;
                        }

                        //If the parameter is surrounded by quotes, remove them.
                        for (int i = 0; i < parsed.Length; i++)
                            if (parsed[i].StartsWith("\"") && parsed[i].EndsWith("\""))
                                parsed[i] = parsed[i].Substring(1, parsed[i].Length - 2);


                        string filePath = parsed[1];
                        string apiName = parsed[2];
                        string nameSpace = parsed[3];
                        string ipaasURL = string.Empty;
                        string swaggerPath = "/swagger/v2/swagger.json";

                        bool runAll = false;

                        string suffix = "";

                        // Combine appsettings API URL with swagger path 
                        switch (apiName.ToLower())
                        {
                            case "customer":
                                ipaasURL = Settings.Instance.CustomerURL + swaggerPath;
                                ValidateURL(ipaasURL, "customer_url");
                                suffix = "Customer";
                                break;
                            case "giftcard":
                                ipaasURL = Settings.Instance.GiftCardUrl + swaggerPath;
                                ValidateURL(ipaasURL, "giftcard_url");
                                suffix = "GiftCard";
                                break;
                            case "integrator":
                                ipaasURL = Settings.Instance.IntegratorUrl + swaggerPath;
                                ValidateURL(ipaasURL, "integrator_url");
                                suffix = "Integrator";
                                break;
                            case "product":
                                ipaasURL = Settings.Instance.ProductUrl + swaggerPath;
                                ValidateURL(ipaasURL, "product_url");
                                suffix = "Product";
                                break;
                            case "subscription":
                                ipaasURL = Settings.Instance.SubscriptionUrl + swaggerPath;
                                ValidateURL(ipaasURL, "subscription_url");
                                suffix = "Subscription";
                                break;
                            case "transaction":
                                ipaasURL = Settings.Instance.TransactionUrl + swaggerPath;
                                ValidateURL(ipaasURL, "transaction_url");
                                suffix = "Transaction";
                                break;
                            case "emplyoee":
                                ipaasURL = Settings.Instance.TransactionUrl + swaggerPath;
                                ValidateURL(ipaasURL, "employee_url");
                                suffix = "Employee";
                                break;
                            case "message":
                                ipaasURL = Settings.Instance.TransactionUrl + swaggerPath;
                                ValidateURL(ipaasURL, "message_url");
                                suffix = "Message";
                                break;
                            case "all":
                                //First validate the URLs
                                ValidateURL(Settings.Instance.CustomerURL, "customer_url");
                                ValidateURL(Settings.Instance.GiftCardUrl, "giftcard_url");
                                ValidateURL(Settings.Instance.IntegratorUrl, "integrator_url");
                                ValidateURL(Settings.Instance.ProductUrl, "product_url");
                                ValidateURL(Settings.Instance.SubscriptionUrl, "subscription_url");
                                ValidateURL(Settings.Instance.TransactionUrl, "transaction_url");
                                ValidateURL(Settings.Instance.MessageUrl, "message_url");
                                ValidateURL(Settings.Instance.EmployeeUrl, "employee_url");

                                //
                                Console.WriteLine("Generating models for the Customer API");
                                Utilities.ModelBuilder.BuildModels(Settings.Instance.CustomerURL + swaggerPath, filePath, nameSpace, "Customer");
                                Console.WriteLine("Generating models for the GiftCard API");
                                Utilities.ModelBuilder.BuildModels(Settings.Instance.GiftCardUrl + swaggerPath, filePath, nameSpace, "GiftCard");
                                Console.WriteLine("Generating models for the Integrators API");
                                Utilities.ModelBuilder.BuildModels(Settings.Instance.IntegratorUrl + swaggerPath, filePath, nameSpace, "Integrator");
                                Console.WriteLine("Generating models for the Product API");
                                Utilities.ModelBuilder.BuildModels(Settings.Instance.ProductUrl + swaggerPath, filePath, nameSpace, "Product");
                                Console.WriteLine("Generating models for the Subscription API");
                                Utilities.ModelBuilder.BuildModels(Settings.Instance.SubscriptionUrl + swaggerPath, filePath, nameSpace, "Subscription");
                                Console.WriteLine("Generating models for the Transaction API");
                                Utilities.ModelBuilder.BuildModels(Settings.Instance.TransactionUrl + swaggerPath, filePath, nameSpace, "Transaction");

                                // Set ipaasURL to null to prevent calling the build models method again 
                                ipaasURL = null;
                                runAll = true;

                                break;
                            default:
                                StandardUtilities.WriteToConsole("Invalid API specified: " + parsed[2], StandardUtilities.Severity.LOCAL_ERROR);
                                return;
                        }

                        // Check if ipaasUrl is null
                        if (!runAll)
                            Utilities.ModelBuilder.BuildModels(ipaasURL, filePath, nameSpace, suffix);
                    }
                    else
                        Console.WriteLine("Invalid entry method. Please use the UPLOAD, HOOK, TEST, or BUILDMODELS commands. Type a command followed by /? for more details.");
                }
                catch (Exception ex)
                {
                    StandardUtilities.WriteToConsole("An error occurred running the command " + resp, StandardUtilities.Severity.LOCAL_ERROR);
                    StandardUtilities.WriteToConsole(ex, StandardUtilities.Severity.LOCAL_ERROR);
                }
            }

            //SendHookAndListenForLogBUILDMODELS(1796, "product/updated", "275078", "FROM");

            //var fileLocation = @"C:\Users\vberisford\source\Workspaces\Red Rook\iPaaSIntegrations\Integrations\BigCommerce.v3.BUILDMODELS\bin\Debug\netcoreapp3.1\BigCommerce.v3.BUILDMODELS.dll";
            //var settings = new iPaaSModels.SettingResponse();

            //ValidationTester.DevelopmentTester.ExecuteTestCase("GetCustomer", 0);

            //var systemBC = Settings.Instance.Systems.Find(x => x.Id == 1796);

            ////UploadFile(fileLocation);
            //var x = ValidationTester.CreateConnection.Create(fileLocation, "", systemBC, Settings.Instance.Companies[0].CompanySpecificToken, Settings.Instance.Companies[0].iPaaSSystemId);
            //var y = x.TranslationUtilities.GetDestinationObject(x, 1);

            //var task1 = Task.Run(async () => await x.TranslationUtilities.ModelGetAsync(x, 8, "164"));
            //var z = task1.GetAwaiter().GetResult();
        }

        private static void ValidateURL(string iPaaSURL, string settingName)
        {
            if (string.IsNullOrEmpty(iPaaSURL))
                throw new Exception($"Unable to load the URL for the requested API. Ensure that your appsettings.json file includes a value for {settingName}");
        }

        public static string[] ParseCommandArguments(string input)
        {
            var parsedInput = Regex.Split(input, "(?<=^[^\"]*(?:\"[^\"]*\"[^\"]*)*) (?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
            return parsedInput;
        }

        //So that we can spawn a new thread, we put this call in a separate method.
        public static void RunHookLocal(string[] parsed)
        {
            Int64 externalSystemId = 0;
            if (!Int64.TryParse(parsed[1], out externalSystemId))
            {
                StandardUtilities.WriteToConsole("Invalid External System Id specified: " + parsed[1], StandardUtilities.Severity.LOCAL_ERROR);
                OperationCancelled = true;
                OperationCompleted = true;
                return;
            }
            else if (parsed[4].ToUpper() != "TO" && parsed[4].ToUpper() != "FROM")
            {
                StandardUtilities.WriteToConsole("Invalid direction specified. The value must be TO or FROM", StandardUtilities.Severity.LOCAL_ERROR);
                OperationCancelled = true;
                OperationCompleted = true;
                return;
            }

            HookController.SendHookAndListenForLogData(Int64.Parse(parsed[1]), parsed[2], parsed[3], parsed[4]);
        }

        public static void UnobservedTaskExceptionHandler(object o, UnobservedTaskExceptionEventArgs e)
        {
            StandardUtilities.WriteToConsole("UnobservedTaskExceptionHandler has been reached", StandardUtilities.Severity.ERROR);
            StandardUtilities.WriteToConsole(e.Exception, StandardUtilities.Severity.ERROR);

            //Console.WriteLine("UnobservedTaskExceptionHandler has been reached.");

            e.SetObserved(); // We must set the exception as observed so that it isn't re-thrown
        }

        private static IDictionary<string, Assembly> additional = new Dictionary<string, Assembly>();

        //VB:05.10.23 - This is just a test! I am trying to see if we can load an assembly without having the packages available here (or in c3po!)
        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Console.WriteLine($"RequestFor: {args.Name} RequestingAssembly {args.RequestingAssembly.FullName}");

            var requestName = args.Name;
            Assembly a1 = Assembly.GetExecutingAssembly();
            if (Program.AssemblyA != null)
            {
                requestName = requestName.Substring(0, requestName.IndexOf(","));
                requestName = "Shopify.Data.EmbeddedAssemblies." + requestName + ".dll";
                a1 = Program.AssemblyA;
            }
            Stream s = a1.GetManifestResourceStream(requestName);
            if(s == null)
            {
                Console.WriteLine($"Unable to find {requestName} in {a1.FullName}");
                return null;
            }
            byte[] block = new byte[s.Length];
            s.Read(block, 0, block.Length);
            Assembly a2 = Assembly.Load(block);
            return a2;

            //Assembly res;
            //additional.TryGetValue(args.Name, out res);
            //return res;

            if (additional.ContainsKey(args.Name))
                return additional[args.Name];   

            var assemblyList = new List<string>();
            assemblyList.Add("Shopify.Data.EmbeddedAssemblies.GraphQL.Client.Abstractions.dll");
            assemblyList.Add("Shopify.Data.EmbeddedAssemblies.GraphQL.Client.Abstractions.Websocket.dll");
            assemblyList.Add("Shopify.Data.EmbeddedAssemblies.GraphQL.Client.dll");
            assemblyList.Add("Shopify.Data.EmbeddedAssemblies.GraphQL.Client.Serializer.Newtonsoft.dll");
            assemblyList.Add("Shopify.Data.EmbeddedAssemblies.GraphQL.dll");
            assemblyList.Add("Shopify.Data.EmbeddedAssemblies.GraphQL.Primitives.dll");

            var argNameClean = args.Name.Substring(0, args.Name.IndexOf(","));
            argNameClean += ".dll";

            if (additional.ContainsKey(argNameClean))
            {
                return additional[argNameClean];
            }

            var myAssembly = args.RequestingAssembly;
            if (Program.AssemblyA != null)
                myAssembly = Program.AssemblyA;

            var assemblyName = assemblyList.Find(x => x.EndsWith(argNameClean));
            if (assemblyName == null)
                return null;

            //foreach (var resource in myAssembly.GetManifestResourceNames())
            //    Console.WriteLine("IDU:: Resource: " + resource);

            using (var stream = myAssembly.GetManifestResourceStream(assemblyName))
            {
                if (stream == null)
                {
                    Console.WriteLine($"Unable to load assembly with path: {assemblyName} - RequestingAssembly {args.RequestingAssembly.FullName}, UsedAssembly: {myAssembly.FullName}");
                    return null;
                    //continue;
                }
                else
                    Console.WriteLine($"Loaded assembly with path: {assemblyName} - RequestingAssembly {args.RequestingAssembly.FullName}, UsedAssembly: {myAssembly.FullName}");

                var assemblyData = new Byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);
                //if (assemblyList[assemblyList.Count - 1] == assemblyName)
                return Assembly.Load(assemblyData); //If we are on the last entry, return it
                                                    //else
                                                    //  Assembly.Load(assemblyData);
            }



            //var assemblyList = new List<string>();
            //                  Shopify.Data.EmbeddedAssemblies.GraphQL.Client.Abstractions.dll
            //assemblyList.Add("Shopify.Data.EmbeddedAssemblies.GraphQL.Client.Abstactions.dll");
            //assemblyList.Add("Shopify.Data.EmbeddedAssemblies.GraphQL.Client.Abstactions.Websocket.dll");
            //assemblyList.Add("Shopify.Data.EmbeddedAssemblies.GraphQL.Client.dll");
            //assemblyList.Add("Shopify.Data.EmbeddedAssemblies.GraphQL.Client.Serializer.Newtonsoft.dll");
            //assemblyList.Add("Shopify.Data.EmbeddedAssemblies.GraphQL.dll");
            //                  Shopify.Data.EmbeddedAssemblies.GraphQL.Primitives.dll
            //assemblyList.Add("Shopify.Data.EmbeddedAssemblies.GraphQL.Primatives.dll");

            //foreach (var assemblyName in assemblyList)
            //{
            //}

            return null;
        }
    }
}
