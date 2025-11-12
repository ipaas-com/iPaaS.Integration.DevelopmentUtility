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

        public static async Task TurnXMLIntoOutput(string dllLocation, string XMLLocation, string destinationTypeStr, string outputTypeStr, string systemTypeVersionId)
        {
            //We need to validate and convert our parameters.

            //First ensure we have an xml file. We will ensure permission and actual content later
            if (!XMLLocation.ToUpper().EndsWith(".XML"))
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

            await TurnXMLIntoOutput(XMLLocation, destinationType, outputType, systemTypeVersionId, systemToken);
        }


        public static async Task TurnXMLIntoOutput(string XMLLocation, Type destinationType, OutputType outputType, string systemTypeVersionId = null, FullToken systemToken = null)
        {
            //string docuPath = dllPath.Substring(0, dllPath.LastIndexOf(".")) + ".XML";

            var _docuDoc = new XmlDocument();
            try
            {
                _docuDoc.Load(XMLLocation);
            }
            catch(Exception ex)
            {
                //Turn the exception into something more readable.
                throw new Exception($"Documentation.TurnXMLIntoOutput - Unable to load XML file {XMLLocation}: {ex.Message}", ex);
            }

            //Start with the methods as enumerated in the desired type. Note that we do not start with the XML file, since that will exclude methods
            //  without any comments, exclude parameters without comments, etc.
            var methods = destinationType.GetMethods(BindingFlags.Static | BindingFlags.Public).ToList().OrderBy(o => o.Name);
            foreach (var mi in methods)
            {
                //Ignore getters and setters
                if (mi.Name.StartsWith("get_") || mi.Name.StartsWith("set_"))
                    continue;

                string path = "M:" + mi.DeclaringType.FullName + "." + mi.Name;
                XmlNode xmlDocuOfMethod = _docuDoc.SelectSingleNode("//member[starts-with(@name, '" + path + "')]");

                var methodDoc = new MethodDocumentation();
                methodDoc.Name = mi.Name;

                //Load method parameters first
                var paramList = mi.GetParameters().ToList();
                foreach(var param in paramList)
                {
                    //Add the parameters from the method. Note that we add the Description field as we iterate the xml file below.
                    methodDoc.Parameters.Add(new MethodDocumentationParameter() { Name = param.Name, Order = param.Position, Type = GetSimplifiedTypeName(param.ParameterType) });
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

                //The DONOTEXPORT flag allows us to skip helper or internal-use methods
                if (string.IsNullOrEmpty(methodDoc.Remarks) || methodDoc.Remarks != "DONOTEXPORT")
                {
                    if (outputType == OutputType.Wiki)
                        methodDoc.ToWikiString();
                    else if (outputType == OutputType.CSV)
                        methodDoc.ToCsvString();
                    else if (outputType == OutputType.SendToDatabase)
                        methodDoc.ToAPI(systemTypeVersionId, systemToken);
                }
            }
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
