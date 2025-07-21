using IntegrationDevelopmentUtility.iPaaSModels;
using IntegrationDevelopmentUtility.Utilities;
using Microsoft.Extensions.Azure;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationDevelopmentUtility.ValidationTester
{
    public class CreateConnection
    {
        //This call is responsible for creating an empty, ready-to-use connection. That includes loading the external assembly, instantiating all the types, 
        //  and assigning delegate function references. 
        //It does NOT make any external calls. That should all be handled in the ConnectionManager.
        public static async Task<Tuple<Integration.Abstract.Connection, object>> Create(string assemblyPath, string dllNamespace, SubscriptionResponse settings, FullToken apiToken, long iPaaSSystemId)
        {
            // Use the file name to load the assembly into the current
            // application domain.
            var assemblyHandler = new AssemblyHandler(assemblyPath);

            Program.AssemblyA = assemblyHandler.a;

            // If we don't have the namespace supplied, guess at it. See the comments on this method for its (severe) limitations
            if (string.IsNullOrEmpty(dllNamespace))
                assemblyHandler.DetermineNamespaceByClassName("Connection");
            else
                assemblyHandler.ExternalNamespace = dllNamespace;

            // First create the connection
            var connection = assemblyHandler.CreateInstance<Integration.Abstract.Connection>("Connection");

            // First assign the basic external and internal settings
            connection.ExternalSystemId = settings.Id;
            connection.ExternalSystemType = settings.IntegrationId;
            connection.ExternalIntegrationVersionId = settings.IntegrationVersionId;
            connection.IPaaSApiToken = apiToken.AcessToken;
            connection.IPaaSSystemId = iPaaSSystemId;

            // Populate each of these objects: CallWrapper, Settings, TranslationUtilities, CustomFieldHandler
            connection.Settings = assemblyHandler.CreateInstance<Integration.Abstract.Settings>("Settings");
            foreach (var kvp in settings.Settings)
                connection.Settings.SettingsDictionary.TryAdd(kvp.Key, kvp.Value);

            // Dont be lazy and comment more please
            connection.TranslationUtilities = assemblyHandler.CreateInstance<Integration.Abstract.TranslationUtilities>("TranslationUtilities");

            // Populating the CustomFieldHandler
            connection.CustomFieldHandler = assemblyHandler.CreateInstance<Integration.Abstract.CustomFieldHandler>("CustomFieldHandler");

            // Create and initialize the callwrapper. Note that the EstablishConnection call is optionally implemented. The abstract class contains it as an empty function, so integrations
            // can choose to use it or not.
            connection.CallWrapper = assemblyHandler.CreateInstance<Integration.Abstract.CallWrapper>("CallWrapper");

            // Note that for ConversionFunctions, we only store the type, not an instance of the type. This is because the ConversionFunctions are all static methods (as required
            //  by FLEE)
            connection.ConversionFunctionType = assemblyHandler.GetType("ConversionFunctions");
            connection.TranslationUtilitiesType = assemblyHandler.GetType("TranslationUtilities");

            // Check for a persistent data class. This is actually optional in the external dll, so if does not exist, we will create an instance of the abstract version
            connection.Settings.PersistentData = assemblyHandler.CreateInstance<Integration.Abstract.Helpers.PersistentDataHandler>("PersistentDataHandler", isOptional: true);
            if (connection.Settings.PersistentData == null)
                connection.Settings.PersistentData = new Integration.Abstract.Helpers.PersistentDataHandler();

            //Convert any settings.PersistentData to the correct format
            // Load PersistentData
            if (settings.PersistentData != null)
                foreach (var persistentDatum in settings.PersistentData)
                    connection.Settings.PersistentData.Values.Add(new Integration.Abstract.Model.PersistentData() { Name = persistentDatum.Name, Value = persistentDatum.Value, ExpirationDateTime = persistentDatum.ExpirationDateTime });

            ApplySettings(connection.Settings, apiToken.AcessToken);

            //There are a couple of settings fields that we do not populate from the settings dictionary:
            connection.Settings.Id = settings.Id;
            connection.Settings.SystemTypeId = settings.IntegrationId;
            connection.Settings.Name = settings.Name;
            connection.Settings.WebhookApiKey = settings.WebhookApiKey;

            // Now we register the delegate functions
            connection.DataHandlerFunctionAsync = CreateConnection.ExternalDataHandlerAsync;
            connection.ClapbackTrackerFunctionAsync = RegisterClapback_Delegate;

            connection.Logger = new Integration.Abstract.Helpers.Logger();
            connection.Logger.Logger_Technical = LogTechEvent_Delegate;

            //We store the assemblyHandler so that we can call the Unload() method once we are done.
            connection.Assembly = assemblyHandler;

            // Establish a connection to the call wrapper
            dynamic dynamicConnection = connection;
            await dynamicConnection.CallWrapper.EstablishConnection(connection, connection.Settings);

            // Save Persistent Data (check to see if there is any data)
            if (connection.Settings.PersistentData != null && connection.Settings.PersistentData.Values != null && connection.Settings.PersistentData.Values.Count > 0)
                iPaaSCallWrapper.PersistentData(connection.ExternalSystemId, connection.Settings.PersistentData.Values, apiToken);

            //Now create a DevelopmentTests class
            var obj = assemblyHandler.CreateInstance<object>("DevelopmentTests", isOptional: true);

            return new Tuple<Integration.Abstract.Connection, object>(connection, obj);
        }

        /// <summary>
        /// Apply the context settings to the connection-specific abstract settings class. This allows external dlls to know the URLs for iPaaS API calls
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="context"></param>
        private static void ApplySettings(Integration.Abstract.Settings settings, string apiToken)
        {
            //settings.SettingsDictionary.TryAdd("Customers_URL", context.DatabaseSettings.ApiUrls.Tenant_URL);
            //settings.SettingsDictionary.TryAdd("Giftcards_URL", context.DatabaseSettings.ApiUrls.Krennic_URL);
            //settings.SettingsDictionary.TryAdd("Products_URL", context.DatabaseSettings.ApiUrls.Tagge_URL);
            //settings.SettingsDictionary.TryAdd("Transactions_URL", context.DatabaseSettings.ApiUrls.Motti_URL);
            if (settings.SettingsDictionary == null)
            {
                //settings.SettingsDictionary = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
                settings.SettingsDictionary = new Dictionary<string, string>();
            }


            settings.SettingsDictionary.TryAdd("Integrators_URL", Utilities.Settings.Instance.IntegratorUrl);
            settings.SettingsDictionary.TryAdd("Subscriptions_URL", Utilities.Settings.Instance.SubscriptionUrl);
            settings.SettingsDictionary.TryAdd("SSO_URL", Utilities.Settings.Instance.SSOUrl);
            settings.SettingsDictionary.TryAdd("IPaaSApi_Token", apiToken);
            settings.SettingsDictionary.TryAdd("Hook_URL", Utilities.Settings.Instance.HookUrl);

            settings.SettingsDictionary.TryAdd("Customers_URL", Utilities.Settings.Instance.CustomerURL);
            settings.SettingsDictionary.TryAdd("Giftcards_URL", Utilities.Settings.Instance.GiftCardUrl);
            settings.SettingsDictionary.TryAdd("Products_URL", Utilities.Settings.Instance.ProductUrl);
            settings.SettingsDictionary.TryAdd("Transactions_URL", Utilities.Settings.Instance.TransactionUrl);

            settings.SettingsDictionary.TryAdd("Employees_URL", Utilities.Settings.Instance.EmployeeUrl);
            settings.SettingsDictionary.TryAdd("Messages_URL", Utilities.Settings.Instance.MessageUrl);
        }

        public static async Task ExternalDataHandlerAsync(Integration.Abstract.Helpers.TransferRequest transferRequest)
        {
            ;
        }

        public static async Task RegisterClapback_Delegate(long systemId, int mappingCollectionTypeId, string id, int directionId)
        {
            ;
        }

        public static void LogTechEvent_Delegate(string severity, string location, string message)
        {
            Utilities.StandardUtilities.Severity level;
            switch (severity)
            {
                case "V":
                    level = Utilities.StandardUtilities.Severity.VERBOSE;
                    break;
                case "I":
                case "D":
                    level = Utilities.StandardUtilities.Severity.DETAIL;
                    break;
                case "W":
                    level = Utilities.StandardUtilities.Severity.WARNING;
                    break;
                case "E":
                    level = Utilities.StandardUtilities.Severity.ERROR;
                    break;
                default:
                    throw new Exception("Invalid severity level. Valid values are: V (Verbose), D (Detail), W (Warning), or E (Error)");
            }

            Utilities.StandardUtilities.WriteToConsole(location + " " + message, level);
        }
    }
}
