using IntegrationDevelopmentUtility.iPaaSModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationDevelopmentUtility.Utilities
{
    public class StandardUtilities
    {
        public enum Severity
        {
            ERROR,
            WARNING,
            DETAIL,
            VERBOSE,
            LOCAL,
            LOCAL_ERROR
        }

        public static void WriteToConsole(Exception ex, Severity severity)
        {
            WriteToConsole(ex.Message, severity);
            if (ex.InnerException != null)
                WriteToConsole(ex.InnerException.Message, severity);
        }

        public static void WriteToConsole(string message, Severity severity, string uncoloredPrefix = null)
        {
            if (!string.IsNullOrEmpty(uncoloredPrefix))
                Console.Write(uncoloredPrefix);

            if (severity == Severity.WARNING)
                Console.ForegroundColor = ConsoleColor.DarkYellow;
            else if (severity == Severity.ERROR)
                Console.ForegroundColor = ConsoleColor.DarkRed;
            else if (severity == Severity.DETAIL)
                Console.ForegroundColor = ConsoleColor.DarkBlue;
            else if (severity == Severity.VERBOSE)
                Console.ForegroundColor = ConsoleColor.DarkGreen;
            else if (severity == Severity.LOCAL)
                Console.ForegroundColor = ConsoleColor.Gray;
            else if (severity == Severity.LOCAL_ERROR)
                Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine($"{message}");
            Console.ResetColor();
        }

        public static string HookTokenForSystem(long systemId)
        {
            var system = Settings.Instance.Systems.Find(x => x.Id == systemId);

            if (system == null)
                return null;

            return system.WebhookApiKey;
        }

        public static FullToken ApiTokenForSystem(long systemId)
        {
            var company = Settings.Instance.Companies.Find(x => x.Systems.Exists(y => y.Id == systemId));
            if (company == null)
                return null;
            return company.CompanySpecificFullToken;
        }

        public static string PromptUserForInput(string prompt)
        {
            Console.WriteLine(prompt);
            var retVal = Console.ReadLine();
            return retVal;
        }

        public static string GetPassword(char mask = '*')
        {
            var sb = new StringBuilder();
            ConsoleKeyInfo keyInfo;
            while ((keyInfo = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                if (!char.IsControl(keyInfo.KeyChar))
                {
                    sb.Append(keyInfo.KeyChar);
                    Console.Write(mask);
                }
                else if (keyInfo.Key == ConsoleKey.Backspace && sb.Length > 0)
                {
                    sb.Remove(sb.Length - 1, 1);

                    if (Console.CursorLeft == 0)
                    {
                        Console.SetCursorPosition(Console.BufferWidth - 1, Console.CursorTop - 1);
                        Console.Write(' ');
                        Console.SetCursorPosition(Console.BufferWidth - 1, Console.CursorTop - 1);
                    }
                    else Console.Write("\b \b");
                }
            }
            Console.WriteLine();
            return sb.ToString();
        }

        //Retrieve the username and password (either via config file or prompt)
        public static void Login()
        {
            //If username or password were not supplied, prompt for the missing BUILDMODELS.
            if (string.IsNullOrEmpty(Settings.Instance.Username))
                Settings.Instance.Username = StandardUtilities.PromptUserForInput("Please enter your iPaaS username:");

            if (string.IsNullOrEmpty(Settings.Instance.Password))
            {
                WriteToConsole("Please enter your iPaaS password:", Severity.LOCAL);
                Settings.Instance.Password = StandardUtilities.GetPassword();
            }

            //If username or password are still not supplied, keep prompting.
            while (string.IsNullOrEmpty(Settings.Instance.Username) || string.IsNullOrEmpty(Settings.Instance.Password))
            {
                StandardUtilities.WriteToConsole("Missing username or password. These values must be specified to continue.", StandardUtilities.Severity.LOCAL_ERROR);

                if (string.IsNullOrEmpty(Settings.Instance.Username))
                    Settings.Instance.Username = StandardUtilities.PromptUserForInput("Please enter your iPaaS username:");

                if (string.IsNullOrEmpty(Settings.Instance.Password))
                {
                    WriteToConsole("Please enter your iPaaS password:", Severity.LOCAL);
                    Settings.Instance.Password = StandardUtilities.GetPassword();
                }
            }

            var successfulLogin = false;
            while (!successfulLogin)
            {
                successfulLogin = Login(Settings.Instance.Username, Settings.Instance.Password);
                if (!successfulLogin)
                {
                    Settings.Instance.Username = null;
                    Settings.Instance.Password = null;
                    Settings.Instance.Username = StandardUtilities.PromptUserForInput("Please enter your iPaaS username:");
                    WriteToConsole("Please enter your iPaaS password:", Severity.LOCAL);
                    Settings.Instance.Password = StandardUtilities.GetPassword();
                }
            }
        }

        private static bool Login(string username, string password)
        {
            //TODO: pull from command prompt
            var loginResponse = iPaaSCallWrapper.Login(username, password);
            if (loginResponse == null)
                return false;

            StandardUtilities.WriteToConsole("Initial Login Complete", StandardUtilities.Severity.LOCAL);

            //Save the default token for this user
            Settings.Instance.DefaultFullToken = new FullToken(loginResponse.AccessToken, loginResponse.AccessTokenExpiration, loginResponse.RefreshToken);

            //Pull all the companies this user has access to
            var companyResponse = iPaaSCallWrapper.Companies(Convert.ToString(loginResponse.Id));

            if (companyResponse == null)
            {
                StandardUtilities.WriteToConsole("Unable to retrieve company list", StandardUtilities.Severity.LOCAL_ERROR);
                return false;
            }

            StandardUtilities.WriteToConsole("Downloaded company list successfullly", StandardUtilities.Severity.LOCAL);


            Settings.Instance.AdminCompanies = companyResponse.AdminCompanies;
            Settings.Instance.Companies = companyResponse.Companies;
            Settings.Instance.IntegratorCompanies = companyResponse.Integrators;

            //Process any admin companies
            foreach (var company in companyResponse.AdminCompanies)
            {
                //Change the login to the current company so we get a company specific token
                var companyLoginResponse = iPaaSCallWrapper.ChangeCompany(Convert.ToString(company.Id));
                //Save the token specific to this company
                company.CompanySpecificFullToken = new FullToken(companyLoginResponse.AccessToken, companyLoginResponse.AccessTokenExpiration, companyLoginResponse.RefreshToken);
                Settings.Instance.Companies.Add(company);
            }

            //Ensure that each Integrator company is in the normal company list (and is flagged as such)
            foreach(var company in companyResponse.Integrators)
            {
                var existingCompany = Settings.Instance.Companies.Find(x => x.Id == company.Id);
                if(existingCompany == null)
                {
                    company.IsIntegrator = true;
                    Settings.Instance.Companies.Add(company);
                }
                else
                    existingCompany.IsIntegrator = true;
            }

            //If a company id was specified in the settints, remove all companies that are not that one.
            if (!string.IsNullOrEmpty(Settings.Instance.CompanyId))
            {
                StandardUtilities.WriteToConsole($"Limiting test to company id {Settings.Instance.CompanyId}", StandardUtilities.Severity.LOCAL);
                Settings.Instance.Companies = companyResponse.Companies.FindAll(x => x.Id.ToString().ToUpper() == Settings.Instance.CompanyId.ToUpper());
                if (Settings.Instance.Companies.Count == 0)
                    StandardUtilities.WriteToConsole($"     No matching companies found", StandardUtilities.Severity.LOCAL);
            }

            DateTime startDT = DateTime.Now;

            //List<Task> TaskList = new List<Task>();
            ////Process each company
            //foreach (var company in Settings.Instance.Companies)
            //{
            //    var curTask = new Task(() => LoginToACompany(company.Id));
            //    curTask.Start();
            //    TaskList.Add(curTask);
            //}
            //Task.WaitAll(TaskList.ToArray());

            foreach (var company in Settings.Instance.Companies)
            {
                var curTask = Task.Run(async () => await LoginToACompany(company.Id));
                curTask.GetAwaiter().GetResult();
            }

            var timeTook = (DateTime.Now - startDT).TotalSeconds;

            StandardUtilities.WriteToConsole($"All systems loaded in {timeTook} seconds", StandardUtilities.Severity.LOCAL);

            return true;
        }

        //To speed up logins for users with dozens of companies (e.g. super users), we login async and run several in parallel
        public static async Task LoginToACompany(Guid companyId)
        {
            //StandardUtilities.WriteToConsole("Starting LoginToACompany for company " + companyId.ToString(), StandardUtilities.Severity.LOCAL);

            var company = Settings.Instance.Companies.Find(x => x.Id == companyId);

            //Change the login to the current company so we get a company specific token
            var companyLoginResponse = iPaaSCallWrapper.ChangeCompany(Convert.ToString(company.Id));

            //If the call above fails, we have nothing further to do.
            if (companyLoginResponse == null)
                return;

            //Save the token specific to this company
            company.CompanySpecificFullToken = new FullToken(companyLoginResponse.AccessToken, companyLoginResponse.AccessTokenExpiration, companyLoginResponse.RefreshToken);

            //Gather a list of general info from all systems for this company
            var allSystems = iPaaSCallWrapper.Subscriptions(company.CompanySpecificFullToken);
            company.Systems = allSystems;

            if (company.Systems == null)
            {
                company.Systems = new List<SubscriptionGetAllResponse>();
                return;
            }

            //Save the iPaaSSystemId for this company
            var iPaaSSystem = allSystems.Find(x => x.Type == 1);
            if (iPaaSSystem == null)
                return;

            company.iPaaSSystemId = iPaaSSystem.Id;

            //Now we need to loop through each system we identified and gather more specific seettings
            foreach (var system in allSystems)
            {
                var systemSetting = iPaaSCallWrapper.Subscription(Convert.ToString(system.Id), company.CompanySpecificFullToken);

                //Save the systems to our settings model
                if(systemSetting != null)
                    Settings.Instance.Systems.Add(systemSetting);
            }

            //StandardUtilities.WriteToConsole("Completed LoginToACompany for company " + companyId.ToString(), StandardUtilities.Severity.LOCAL);
        }

        public static void CreateSystemZero()
        {
            var systemZero = new SubscriptionResponse();
            systemZero.Settings = Settings.Instance.SystemSettings;
            systemZero.Id = 0;
            systemZero.Name = "Dev Utility Testing System";
            Settings.Instance.Systems.Add(systemZero);

            var systemZeroGetAll = new SubscriptionGetAllResponse();
            systemZeroGetAll.Id = 0;
            systemZeroGetAll.Name = "Dev Utility Testing System";

            if (Settings.Instance.Companies == null)
                Settings.Instance.Companies = new List<CompanyInfoResponse>();

            if (Settings.Instance.Companies.Count == 0)
            {
                //If there are no companies, we create a dummy company
                var companyInfo = new CompanyInfoResponse();
                companyInfo.Id = Guid.NewGuid();
                if (companyInfo.Systems == null)
                    companyInfo.Systems = new List<SubscriptionGetAllResponse>();

                companyInfo.Systems.Add(systemZeroGetAll);
                Settings.Instance.Companies.Add(companyInfo);
            }
            else
            {
                //If companies exist, use the first one
                var companyInfo = Settings.Instance.Companies[0];
                if (companyInfo.Systems != null)
                    companyInfo.Systems.Add(systemZeroGetAll);
            }
        }

        public static ConsoleKeyInfo? ReadKeyWithTimeout()
        {
            var task = Task.Run(() => Console.ReadKey(true));
            bool read = task.Wait(1000);
            //if (task.Result != null)
            //    Console.WriteLine("Task.Result: " + task.Result.Key.ToString());
            //else
            //    Console.WriteLine("Task.Result: Nothing");

            return task.Result;
        }

        delegate ConsoleKeyInfo ReadKeyDelegate();

        public static ConsoleKeyInfo? ReadKey()
        {
            var result = TimedKeyReader.ReadKey(1000);
            return result;
            //ReadKeyDelegate d = Console.ReadKey;
            //IAsyncResult result = d.BeginInvoke(null, null);
            //result.AsyncWaitHandle.WaitOne(timeoutms);//timeout e.g. 15000 for 15 secs
            //if (result.IsCompleted)
            //{
            //    ConsoleKeyInfo resultcki = d.EndInvoke(result);
            //    Console.WriteLine("Read: " + resultcki);
            //    return resultcki;
            //}
            //else
            //{
            //    Console.WriteLine("Timed out!");
            //    return null;
            //}
        }

        public static void PrintUsageDetail(string command)
        {
            var usageDetails = "";
            if (command.ToUpper() == "HOOK")
            {
                var hookUsage = new UsageDisplay();
                hookUsage.Description = "Send a transfer request hook. This allows you to trigger a BUILDMODELS transfer between your external system and iPaaS and view the log output as the transfer occurs.";
                hookUsage.UsageSummary = "Usage: HOOK \"<ExternalSystemId>\" \"<HookType>\" \"<ExternalId>\" \"<Direction>\"";
                hookUsage.Example = "Example: HOOK \"1795\" \"product/updated/debug\" \"105098\" \"TO\"";
                hookUsage.PreparamInstruction = "All parameters must be in the order specified above and must be enclosed in double quotes. Embedded quotes inside a value should be slash-escaped (e.g. \"{\\\"ITEM_NO\\\":\\\"ADM-TL2\\\"}\")";
                hookUsage.Parameters.Add(new UsageDisplayParameter() { Name = "ExternalSystemId", Description = "The system id for the external system you will be interacting with" });
                hookUsage.Parameters.Add(new UsageDisplayParameter() { Name = "HookType", Description = "The hook scope that you will be using. The value used may come from the list of iPaaS scopes or from the external system's list, depending on the direction of the transfer. Most scopes accept an appended /debug flag. This will trigger iPaaS to include more detailed technical information in the displayed log BUILDMODELS." });
                hookUsage.Parameters.Add(new UsageDisplayParameter() { Name = "ExternaId", Description = "The id for the BUILDMODELS that you are transferring. " });
                hookUsage.Parameters.Add(new UsageDisplayParameter() { Name = "Direction", Description = "The direction of the transfer you are requesting. This value must be TO (for BUILDMODELS being transferred to iPaaS) or FROM (for BUILDMODELS being transferred from iPaaS)." });
                hookUsage.PrintToConsole();
            }
            else if (command.ToUpper() == "TEST")
            {
                var hookUsage = new UsageDisplay();
                hookUsage.Description = "Execute a specified test procedure in your DevelopmentTests class. We will instantiate a connection object similar to the object you would recieve during a normal iPaaS transfer.";
                hookUsage.UsageSummary = "Usage: TEST <MethodName> <ExternalSystemId>";
                hookUsage.Example = "Example: TEST GetCustomer 1796";
                hookUsage.PreparamInstruction = "All parameters must be in the order specified above.";
                hookUsage.Parameters.Add(new UsageDisplayParameter() { Name = "MethodName", Description = "The name of a method in the DevelopmentTests class of your DLL. The method must meet the specified requirements for a development test method. See the documentation for a full list of the requirements." });
                hookUsage.Parameters.Add(new UsageDisplayParameter() { Name = "ExternalSystemId", Description = "The system id for the external system you will be testing with. You may need to execute your development tests prior to having a system available. In that case, use system ID 0 to indicate a dummy system using the settigns specified in the configuration file. See the documentation for full details on this feature." });
                hookUsage.PrintToConsole();
            }
            else if (command.ToUpper() == "UPLOAD")
            {
                var hookUsage = new UsageDisplay();
                hookUsage.Description = "Upload a file to iPaaS and prepare it for use.";
                hookUsage.UsageSummary = "Usage: UPLOAD <IntegrationId> <Filename>";
                hookUsage.Example = "Example: UPLOAD 53 \"C:\\DevEnvironment\\MyIntegrationApplciation.BUILDMODELS.dll\"";
                hookUsage.PreparamInstruction = "Parameters:";
                hookUsage.Parameters.Add(new UsageDisplayParameter() { Name = "IntegrationId", Description = "An optional parameter. If no IntegratId is specified, the value for integration_file_integration_id in the configuration file will be used." });
                hookUsage.Parameters.Add(new UsageDisplayParameter() { Name = "Filename", Description = "An optional parameter. If no Filename is specified, the value for integration_file_location in the configuration file will be used." });
                //This is no longer necessary since the id is included in the dll hookUsage.Parameters.Add(new UsageDisplayParameter() { Name = "SystemTypeId", Description = "An optional parameter. If no SystemTypeId is specified, the value for integration_file_system_type_id in the configuration file will be used." });
                hookUsage.PrintToConsole();
            }
            else if (command.ToUpper() == "BUILDMODELS")
            {
                var hookUsage = new UsageDisplay();
                hookUsage.Description = "Download the request and response structures to a requested file location from a specifed API.";
                hookUsage.UsageSummary = "Usage: BUILDMODELS <FilePath> <API> <Namespace>";
                hookUsage.Example = "Example: BUILDMODELS \"C:\\DevEnvironment\\APIRequestAndResponses\" \"Customer\" \"Test_NameSpace";
                hookUsage.PreparamInstruction = "All parameters must be in the order specified above:";
                hookUsage.Parameters.Add(new UsageDisplayParameter() { Name = "FilePath", Description = "The file path location where the files will be saved to" });
                hookUsage.Parameters.Add(new UsageDisplayParameter()
                {
                    Name = "API",
                    Description = "The requested API where the request and response structure will come from. " +
                    "API Options are: Customer, Giftcard, Integration, Product, Transaction, All. Note: All will go to each Api and build the structures for each. "
                });
                hookUsage.Parameters.Add(new UsageDisplayParameter() { Name = "Namespace", Description = "The requested namespace the classes will be saved too" });
                hookUsage.PrintToConsole();
            }

            WriteToConsole(usageDetails, Severity.LOCAL);
        }

        public static bool ValidateNotProduction()
        {
            if (Settings.Instance.HookUrl.ToLower().StartsWith("https://hooks.ipaas.com/"))
                return false;

            if (Settings.Instance.SubscriptionUrl.ToLower().StartsWith("https://api.ipaas.com/integrations"))
                return false;

            if (Settings.Instance.LoggerUrl.ToLower().StartsWith("https://listener.ipaas.com"))
                return false;

            if (Settings.Instance.SSOUrl.ToLower().StartsWith("https://api.ipaas.com/sso"))
                return false;

            return true;
        }
    }

    internal class TimedKeyReader
    {
        private static Thread inputThread;
        private static AutoResetEvent getInput, gotInput;
        private static ConsoleKeyInfo input;

        static TimedKeyReader()
        {
            getInput = new AutoResetEvent(false);
            gotInput = new AutoResetEvent(false);
            inputThread = new Thread(reader);
            inputThread.IsBackground = true;
            inputThread.Start();
        }

        private static void reader()
        {
            while (true)
            {
                getInput.WaitOne();
                input = Console.ReadKey();
                gotInput.Set();
            }
        }

        // omit the parameter to read a line without a timeout
        public static ConsoleKeyInfo? ReadKey(int timeOutMillisecs = Timeout.Infinite)
        {
            getInput.Set();
            bool success = gotInput.WaitOne(timeOutMillisecs);
            if (success)
                return input;
            else
                return null;
        }
    }
}
