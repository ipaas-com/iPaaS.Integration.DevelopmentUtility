using IntegrationDevelopmentUtility.iPaaSModels;
using IntegrationDevelopmentUtility.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace IntegrationDevelopmentUtility.DocumentationGenerator
{
    public class DocumentationReader
    {
        public enum OutputType
        {
            Wiki,
            CSV,
            SendToDatabase
        }

        public static async Task TurnXMLIntoOutput(string dllLocation, string XMLLocation, string destinationTypeStr, string outputTypeStr, string systemTypeVersionId, bool updateOnly = false)
        {
            //We need to validate and convert our parameters.

            //An empty location means we are running from the assembly alone. Only descriptions and the
            //DONOTEXPORT flag come from the XML, so this is viable for refreshing values read from the
            //assembly (parameter names, types, IsRequired) - but only when we cannot create.
            if (updateOnly && string.IsNullOrEmpty(XMLLocation))
                XMLLocation = null;
            //First ensure we have an xml file. We will ensure permission and actual content later
            else if (!XMLLocation.ToUpper().EndsWith(".XML"))
                throw new Exception("XML Location must be an .xml file");

            //Now convert the destination type to a Type

            //first we need an assembly loader
            var assemblyHandler = new AssemblyHandler(dllLocation);
            assemblyHandler.DetermineNamespaceByClassName("Connection"); //Use the connection to determine our default namespace
            var destinationType = assemblyHandler.GetType(destinationTypeStr); //Use the assembly handler to convert our type name into a type

            var outputType = OutputType.SendToDatabase;
            if (outputTypeStr.ToUpper() == "CSV")
                outputType = OutputType.CSV;
            else if (outputTypeStr.ToUpper() == "WIKI")
                outputType = OutputType.Wiki;
            else if (outputTypeStr.ToUpper() == "UPLOAD")
                outputType = OutputType.SendToDatabase;
            else
                throw new Exception($"Invalid value specified for output type. Must be one of: CSV, WIKI, UPLOAD. Your value: {outputType}");

            FullToken systemToken = null;

            //We need to gather the version id and the token
            if (outputType == OutputType.SendToDatabase)
            {
                //TODO: Can we do better than this limitation?
                if (string.IsNullOrEmpty(Settings.Instance.CompanyId))
                    throw new Exception("CompanyId must be specified in the config file to use the UPLOAD command");
                
                //Find a system with same systemType. Users are only allowed to upload files for a system type they have access to
                var matchingCompany = Settings.Instance.Companies.Find(x => x.Id == Guid.Parse(Settings.Instance.CompanyId));
                if (matchingCompany == null)
                    throw new Exception($"Company specified in appsettings does not exist {Settings.Instance.CompanyId}");

                if(!matchingCompany.IsIntegrator)
                    throw new Exception($"Company specified must be an integrator {Settings.Instance.CompanyId}");

                //The version is required for an upload. Rather than failing on a null reference below, work out
                //what the caller could have meant: if there is exactly one candidate we use it, and if there are
                //several we list them so the user knows what to pass.
                if (string.IsNullOrEmpty(systemTypeVersionId))
                    systemTypeVersionId = ResolveSystemTypeVersionId(matchingCompany);

                //We need to find a system that matches our version type. Unfortunately we hvae to combine a few pieces of information to find this. Matchingcompany.systems will let us find everything for this company
                //by type, then we need to look through Settings.Instance.Systems to find one with the right version.
                var systemTypeId = systemTypeVersionId.Substring(0, systemTypeVersionId.LastIndexOf("|"));
                var matchingTypes = matchingCompany.Systems.FindAll(x => x.Type == long.Parse(systemTypeId));
                if(matchingTypes == null)
                    throw new Exception($"Company specified must include a system of type {systemTypeId}");

                SubscriptionResponse matchingSystem = null;
                foreach (var matchingType in matchingTypes)
                {
                    matchingSystem = Settings.Instance.Systems.Find(x => x.Id == matchingType.Id && x.IntegrationVersionId == systemTypeVersionId);
                    if (matchingSystem != null)
                        break;
                }

                if (matchingSystem == null)
                    throw new Exception($"Company specified must include a system of version {systemTypeVersionId}");

                //Pull the token for this system
                systemToken = StandardUtilities.ApiTokenForSystem(matchingSystem.Id);
            }

            await TurnXMLIntoOutput(XMLLocation, destinationType, outputType, systemTypeVersionId, systemToken, updateOnly);
        }


        public static async Task TurnXMLIntoOutput(string XMLLocation, Type destinationType, OutputType outputType, string systemTypeVersionId = null, FullToken systemToken = null, bool updateOnly = false)
        {
            //string docuPath = dllPath.Substring(0, dllPath.LastIndexOf(".")) + ".XML";

            //No location means no documentation: the assembly alone supplies the formulas. That is only
            //safe when we cannot create, since DONOTEXPORT lives in the XML.
            var hasDocumentation = !string.IsNullOrEmpty(XMLLocation);
            if (!hasDocumentation && !updateOnly)
                throw new Exception("An XML documentation file is required unless the run is update-only. "
                    + "Without it we cannot honor the DONOTEXPORT flag, so internal helper methods would be "
                    + "published as conversion functions.");

            var _docuDoc = new XmlDocument();
            if (hasDocumentation)
            {
                try
                {
                    _docuDoc.Load(XMLLocation);
                }
                catch(Exception ex)
                {
                    //Turn the exception into something more readable.
                    throw new Exception($"Documentation.TurnXMLIntoOutput - Unable to load XML file {XMLLocation}: {ex.Message}", ex);
                }
            }

            var uploaded = 0;
            var skipped = new List<string>();

            //Start with the methods as enumerated in the desired type. Note that we do not start with the XML file, since that will exclude methods
            //  without any comments, exclude parameters without comments, etc.
            var methods = destinationType.GetMethods(BindingFlags.Static | BindingFlags.Public).ToList().OrderBy(o => o.Name);
            foreach (var mi in methods)
            {
                //Ignore getters and setters
                if (mi.Name.StartsWith("get_") || mi.Name.StartsWith("set_"))
                    continue;

                string path = "M:" + mi.DeclaringType.FullName + "." + mi.Name;
                XmlNode xmlDocuOfMethod = hasDocumentation
                    ? _docuDoc.SelectSingleNode("//member[starts-with(@name, '" + path + "')]")
                    : null;

                var methodDoc = new MethodDocumentation();
                methodDoc.Name = mi.Name;

                //Load method parameters first
                var paramList = mi.GetParameters().ToList();
                foreach(var param in paramList)
                {
                    //Add the parameters from the method. Note that we add the Description field as we iterate the xml file below.
                    methodDoc.Parameters.Add(new MethodDocumentationParameter() { Name = param.Name, Order = param.Position, Type = GetSimplifiedTypeName(param.ParameterType), IsRequired = IsParameterRequired(param) });
                }

                //Now read the documentation and pull the XML data.
                if (xmlDocuOfMethod != null)
                {
                    foreach (XmlNode row in xmlDocuOfMethod.ChildNodes)
                    {
                        var cleanStr = Regex.Replace(row.InnerXml, @"\s+", " ").Trim();
                        if (row.Name == "summary")
                            methodDoc.Summary = cleanStr;
                        else if(row.Name == "example")
                            methodDoc.Example = cleanStr;
                        else if (row.Name == "remarks")
                            methodDoc.Remarks = cleanStr;
                        else if (row.Name == "returns")
                            methodDoc.ReturnsDescription = cleanStr;
                        else if(row.Name == "param")
                        {
                            //For params, we need to extract the name attribute. (The xml node looks like this: <param name="imageFilenameList">A list of file names</param>)
                            var paramName = row.Attributes["name"]?.Value;
                            var matchingParam = methodDoc.Parameters.Find(x => x.Name == paramName); //Look for an existing match.
                            if(matchingParam != null)
                                matchingParam.Description = cleanStr;
                            //Note that we do not add a new Parameter entry if we don't find a match. Since we add the params by method info, a lack
                            //of a matching record here would indicate we have a comment for a param that does not exist. We are better off excluding that.
                        }
                    }
                }

                methodDoc.Formula = GetMethodDeclaration(mi);

                //Determine if the method is async by checking if the return type is Task or Task<x>
                var returnType = mi.ReturnType;
                methodDoc.IsAsync = (returnType == typeof(Task) ||
                       (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)));

                methodDoc.ReturnType = GetSimplifiedTypeName(mi.ReturnType);

                var isObsolete = (mi.GetCustomAttribute<ObsoleteAttribute>() != null);
                if (isObsolete)
                    methodDoc.Status = 2; //Mark as obsolete
                else
                    methodDoc.Status = 1; //Mark as active

                //The DONOTEXPORT flag allows us to skip helper or internal-use methods. Without an XML file
                //Remarks is always null, so this cannot filter - update-only mode is what protects us there.
                if (string.IsNullOrEmpty(methodDoc.Remarks) || methodDoc.Remarks != "DONOTEXPORT")
                {
                    if (outputType == OutputType.Wiki)
                        methodDoc.ToWikiString();
                    else if (outputType == OutputType.CSV)
                        methodDoc.ToCsvString();
                    else if (outputType == OutputType.SendToDatabase)
                    {
                        //Note this is awaited. It used to be fire-and-forget (async void), which meant
                        //failures were unobservable and the process could exit mid-upload.
                        if (await methodDoc.ToAPI(systemTypeVersionId, systemToken, updateOnly))
                            uploaded++;
                        else
                            skipped.Add(methodDoc.Name);
                    }
                }
            }

            if (outputType == OutputType.SendToDatabase)
            {
                StandardUtilities.WriteToConsole($"Updated {uploaded} formula(s).", StandardUtilities.Severity.LOCAL);
                if (skipped.Count > 0)
                {
                    //In update-only mode these are the methods with no existing formula for this version.
                    //They need a run with the XML file present before they can be created.
                    StandardUtilities.WriteToConsole($"Skipped {skipped.Count} method(s) with no existing formula to update:", StandardUtilities.Severity.LOCAL);
                    foreach (var name in skipped)
                        StandardUtilities.WriteToConsole($"     {name}", StandardUtilities.Severity.LOCAL);
                }
            }
        }

        /// <summary>
        /// Determines whether a caller must supply a value for this parameter. A parameter is optional if it
        /// declares a default value, or if it is a params array - a params array accepts zero arguments, so
        /// omitting it is legal even though ParameterInfo.IsOptional reports false.
        /// </summary>
        public static bool IsParameterRequired(ParameterInfo parameter)
        {
            if (parameter.IsOptional || parameter.HasDefaultValue)
                return false;

            if (parameter.IsDefined(typeof(ParamArrayAttribute), false))
                return false;

            return true;
        }

        /// <summary>
        /// Work out which system type version an upload should target when the caller did not specify one.
        /// Only versions the company actually has a system for are candidates, since those are the only
        /// values that would pass the validation below. Returns the id when it is unambiguous, otherwise
        /// throws with the list of valid choices.
        /// </summary>
        private static string ResolveSystemTypeVersionId(CompanyInfoResponse matchingCompany)
        {
            //Match the company's systems up to the loaded subscription records so we can read their version ids.
            var candidates = new List<SubscriptionResponse>();
            foreach (var companySystem in matchingCompany.Systems ?? new List<SubscriptionGetAllResponse>())
            {
                var system = Settings.Instance.Systems.Find(x => x.Id == companySystem.Id);
                if (system != null && !string.IsNullOrEmpty(system.IntegrationVersionId)
                    && !candidates.Exists(x => x.IntegrationVersionId == system.IntegrationVersionId))
                    candidates.Add(system);
            }

            //We are uploading the conversion functions for one integration, so only that integration's versions
            //are relevant. Without this the list would include every unrelated system the company happens to own.
            if (Settings.Instance.IntegrationFileIntegrationId.HasValue && Settings.Instance.IntegrationFileIntegrationId.Value > 0)
            {
                var forThisIntegration = candidates.FindAll(x => x.IntegrationId == Settings.Instance.IntegrationFileIntegrationId.Value);

                //Only narrow if it actually leaves us something. If the configured integration has no systems we
                //are better off showing the full list than showing nothing.
                if (forThisIntegration.Count > 0)
                    candidates = forThisIntegration;
            }

            candidates.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            if (candidates.Count == 0)
                throw new Exception("systemTypeVersionId is required when uploading, and no version could be "
                    + $"determined automatically: company {Settings.Instance.CompanyId} has no systems with an "
                    + "integration version. Add a system for the integration you are uploading, then rerun with "
                    + "systemTypeVersionId=<SystemTypeVersionId>.");

            if (candidates.Count == 1)
            {
                StandardUtilities.WriteToConsole($"No systemTypeVersionId was specified. Using the only version "
                    + $"available to this company: {candidates[0].IntegrationVersionId}", StandardUtilities.Severity.DETAIL);
                return candidates[0].IntegrationVersionId;
            }

            //More than one, so we cannot pick for them. Describe each option, decorated with its version number
            //where we can retrieve it.
            throw new Exception("systemTypeVersionId is required when uploading, and this company has more than "
                + "one version available. Rerun with systemTypeVersionId=<SystemTypeVersionId> using one of: "
                + Environment.NewLine + DescribeVersions(candidates, matchingCompany));
        }

        /// <summary>
        /// Build a display list of the candidate versions, e.g. "2|11 v1.2.2". The version numbers come from the
        /// integration record; if that call fails we still list the ids, since the ids are the part the user needs.
        /// </summary>
        private static string DescribeVersions(List<SubscriptionResponse> candidates, CompanyInfoResponse matchingCompany)
        {
            //Cache by integration id so we make at most one call per integration rather than one per version.
            var versionsByIntegration = new Dictionary<long, List<VersionResponse>>();
            foreach (var candidate in candidates)
            {
                if (versionsByIntegration.ContainsKey(candidate.IntegrationId))
                    continue;

                try
                {
                    var integration = iPaaSCallWrapper.Integration(candidate.IntegrationId, matchingCompany.CompanySpecificFullToken);
                    versionsByIntegration[candidate.IntegrationId] = integration?.Versions ?? new List<VersionResponse>();
                }
                catch
                {
                    //Decorating the list is a convenience, not a requirement. Fall back to the bare id.
                    versionsByIntegration[candidate.IntegrationId] = new List<VersionResponse>();
                }
            }

            var lines = new List<string>();
            foreach (var candidate in candidates)
            {
                var display = new String(' ', 5) + candidate.IntegrationVersionId;

                var match = versionsByIntegration[candidate.IntegrationId]
                    .Find(x => x.Id == candidate.IntegrationVersionId);
                if (match != null)
                    display += $" v{match.VersionMajor}.{match.VersionMinor}.{match.VersionPatch}";

                if (!string.IsNullOrEmpty(candidate.Name))
                    display += $" ({candidate.Name})";

                lines.Add(display);
            }

            return string.Join(Environment.NewLine, lines);
        }

        public static string GetSimplifiedTypeName(Type type)
        {
            if (type.IsGenericType)
            {
                var typeName = type.Name;
                var backtickIndex = typeName.IndexOf('`');
                if (backtickIndex > 0)
                    typeName = typeName.Substring(0, backtickIndex);

                var genericArgs = type.GetGenericArguments()
                                      .Select(GetSimplifiedTypeName);
                return $"{typeName}<{string.Join(", ", genericArgs)}>";
            }
            else if (type.IsArray)
            {
                return $"{GetSimplifiedTypeName(type.GetElementType())}[]";
            }
            else
            {
                //simplify some of the types
                return type.Name switch
                {
                    "Int32" => "int",
                    "String" => "string",
                    "Boolean" => "bool",
                    "Object" => "object",
                    "Void" => "void",
                    "Task" => "Task",
                    _ => type.Name
                };
            }
        }

        public static string GetMethodDeclaration(MethodInfo method)
        {
            var methodName = method.Name;

            var parameters = method.GetParameters()
                .Select(p => $"{GetSimplifiedTypeName(p.ParameterType)} {p.Name}")
                .ToArray();

            return $"{methodName}({string.Join(", ", parameters)})";
        }
    }
}
