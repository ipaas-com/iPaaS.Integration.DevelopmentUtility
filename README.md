# iPaaS.com Integration Development Utility
Provides a set of tools to assist in the development of third-party integrations using the iPaaS.com platform.

It is recommended to start with a new integration project.  If you have not done so yet, please review [iPaaS.com Integration Template](https://github.com/ipaas-com/iPaaS.Integration.Template) to get started.

- [iPaaSExternalIntegrationHelper](#ipaasexternalintegrationhelper)
  * [Overview](#overview)
  * [Upload a completed integration](#upload-a-completed-integration)
  * [Run local tests with your integration file](#run-local-tests-with-your-integration-file)
    + [Creating local test methods](#creating-local-test-methods)
  * [Simulate transfer hooks](#simulate-transfer-hooks)
  * [Configuration File](#configuration-file)
## Contents
 * [Overview](#overview)
 * [Upload a completed integration](#upload-a-completed-integration)
 * [Run local tests with your integration file](#run-local-tests-with-your-integration-file)
   + [Creating local test methods](#creating-local-test-methods)
 * [Simulate transfer hooks](#simulate-transfer-hooks)
 * [Configuration File](#configuration-file)

## Overview
This tool provides three main features for developers looking to add an integration to the iPaaS.com platform:
1. Upload a completed file to the iPaaS platform and run the requried steps to make the new file available for use.
1. Run local tests against your integration 
1. Run specific integration hooks and view the activity and technical log output as the hook is processed.
## Upload a completed integration
Upload a file to iPaaS and prepare it for use.
```
Usage: UPLOAD [Filename]
Parameters:
[Filename]    An optional parameter. If no Filename is specified, the value for integration_file_location in the configuration file will be used.
```
## Run local tests with your integration file
Execute a specified test procedure in your DevelopmentTests class. We will instantiate a connection object similar to the object you would recieve during a normal iPaaS transfer.
```
Usage: TEST MethodName [ExternalSystemId]
Parameters:
MethodName            The name of a method in the DevelopmentTests class of your DLL. The method must meet the specified requirements for a 
                      development test method. See the documentation for a full list of the requirements.
[ExternalSystemId]    The system id for the external system you will be testing with. You may need to execute your development tests prior 
                      to having a system available. In that case, use system ID 0 to indicate a dummy system using the settigns specified 
                      in the configuration file. See the documentation for full details on this feature.
```
### Creating local test methods
In your integration DLL, create a class called DevelopmentTests in the same namespace as your other Integration.Abstract classes. Create public static async methods that accept one parameter: an Integration.Abstract.Connection.
Example:
```
public static async Task GetCustomer(Integration.Abstract.Connection connection)
{
    var conn = (Connection)connection;
    var bcWrapper = conn.CallWrapper;
    var jennaWatkins = bcWrapper.Customer_GET(164);
    jennaWatkins.Company = "JennaTestCo";
    jennaWatkins.FormFields = null;
    bcWrapper.Customer_PUT(jennaWatkins);
}
```
To run that procedure, you would just use the command `TEST GetCustomer`. That would run the command on a pseudo-connection using your system_settings. To make the same call using the settings from a real system, just put the system id as the second parameter like `TEST GetCustomer 186`.
## Simulate transfer hooks
Send a transfer request hook. This allows you to trigger a data transfer between your external system and iPaaS and view the log output as the transfer occurs. These commands run remotely, so the file you are testing must be uploaded already.
```
Usage: HOOK "ExternalSystemId" "HookType" "ExternalId" "Direction"
ExternalSystemId    The system id for the external system you will be interacting with
HookType            The hook scope that you will be using. The value used may come from the list of iPaaS scopes or from the external 
                    system's list, depending on the direction of the transfer. Most scopes accept an appended /debug flag. This will 
                    trigger iPaaS to include more detailed technical information in the displayed log data.
ExternaId           The id for the data that you are transferring.
Direction           The direction of the transfer you are requesting. This value must be TO (for data being transferred to iPaaS) or 
                    FROM (for data being transferred from iPaaS).
```
All parameters must be in the order specified above and must be enclosed in double quotes. Embedded quotes inside a parameter should be slash-escaped (e.g. "{\"ITEM_NO\":\"ADM-TL2\"}").
## Configuration File
The file appsettings.json contains several settings that you will want to configure prior to running the integration helper tool.
* `username` and `password` - Your iPaaS login. These fields are optional and if not specified, you will be prompted for them when the program is run. These settings will determine which systems you have access to and what operations you will be allowed to perform.
* `integration_file_location` – If you are working on an integration DLL, this should be the full path to your local version of that file. This is used for two things: if you use the UPLOAD command without specifying a filename, we will use this filepath and, if you are running local test cases, we load this file.
* `customer_url`,  `giftcard_url`,  `product_url`,  `transaction_url`,  `integrator_url`, `hook_url`, `subscription_url`, `logger_url`, and `sso_ur`l –The URLs for each of the respective iPaaS.com environmental APIs used.  Only the staging environment will respond to the Integrator Development Utility.
* `hook_read_delay_interval_ms` – When running the HOOK command, how often should we ping the log endpoint. 
* `file_upload_delay_interval_secs` – When uploading a file, we restart C3PO so it can recognize the new file. This setting determines how long we wait for the restart to complete before sending the system\updated hook.
* `system_settings` – An optional set of settings for your dll to use when running local test cases. You may be testing a new dll that does not have an existing company (or has not even been loaded into iPaaS yet), so without these settings there would be nothing to test against. The key\value pairs you include should mirror the Presets that will be used by your integration. If specified, the settings are used to create a local pseudo-company with ID 0. These settings only apply to the local test case feature. 
