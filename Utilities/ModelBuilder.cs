using Microsoft.OpenApi.Readers;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace IntegrationDevelopmentUtility.Utilities
{
    class ModelBuilder
    {
        /// <summary>
        /// Scrape the data for the models from the swagger url and build out the models
        /// </summary>
        /// <param name="swaggerURL">Swagger Page Url</param>
        /// <param name="destinationPath">Requested file path from user</param>
        /// <param name="nameSpace">Request Namespace</param>
        /// <returns></returns>
        public static void BuildModels(string swaggerURL, string destinationPath, string nameSpace, string suffix)
        {
            try
            {
                nameSpace += "." + suffix;

                //Split the URL 
                Uri uriAddress1 = new Uri(swaggerURL);
                string baseAddress = uriAddress1.GetLeftPart(UriPartial.Authority);
                string endAddress = uriAddress1.AbsolutePath;
                string endpointName = endAddress.Substring(1);
                endpointName = endpointName.Substring(0, endpointName.IndexOf("/"));

                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(baseAddress)
                };

                var stream = httpClient.GetStreamAsync(endAddress).Result;

                // Read the file
                Microsoft.OpenApi.Models.OpenApiDocument openApiDocument = new OpenApiStreamReader().Read(stream, out var diagnostic);

                try
                {
                    // Loop thru Component Schemas (Models)
                    foreach (var schema in openApiDocument.Components.Schemas)
                    {
                        // Create CCU for generating models
                        CodeCompileUnit compileUnit = new CodeCompileUnit();
                        CodeTypeDeclaration newClassModel = new CodeTypeDeclaration();

                        // Create class based on model name
                        string modelName = schema.Key;

                        // Skip over problem details 
                        if (modelName == "ProblemDetails")
                            continue;

                        newClassModel = CreateClass(compileUnit, modelName, nameSpace);

                        // Loop thru Properties
                        foreach (var property in schema.Value.Properties)
                        {
                            // Get the property name
                            string propertyName = property.Key;

                            // Get property type
                            //string type = ConvertType(property, modelName);

                            string type = null;
                            try
                            {
                                type = ConvertType(property, modelName);
                            }
                            catch(Exception ex2)
                            {
                                ;
                            }

                            //  Create Properties for class
                            AddProperty(newClassModel, type, propertyName);
                        }

                        // Save class file
                        GenerateFile(compileUnit, modelName, destinationPath, suffix);
                    }
                }
                catch(Exception ex1)
                {
                    StandardUtilities.WriteToConsole($"{ex1.Message} while processing the {endpointName} api", StandardUtilities.Severity.ERROR);
                }
            }
            catch (Exception ex)
            {
                StandardUtilities.WriteToConsole(ex.Message, StandardUtilities.Severity.ERROR);
            }
        }

        /// <summary>
        /// Create the class
        /// </summary>
        /// <param name="compileUnit"></param>
        /// <param name="className"></param>
        /// <returns></returns>
        public static CodeTypeDeclaration CreateClass(CodeCompileUnit compileUnit, string className, string nameSpace)
        {
            //     *******************   NOTE    *********************
            // There are two namespaces created in order to have the using statements in the properplace
            // Without adding the blank namespace, the using statements are generated inside the namespace
            // As opposed to above it. 

            CodeNamespace blankNamespace = new CodeNamespace();                                  // Create a namespace with no name 
            blankNamespace.Imports.Add(new CodeNamespaceImport("System"));                       // Import the system namespace
            blankNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));   // Import the generics namespace
            blankNamespace.Imports.Add(new CodeNamespaceImport("Newtonsoft.Json"));              // Import the Json namespace

            CodeNamespace newNamespace = new CodeNamespace(nameSpace);             // Create a namespace with the requested name (This is the one that will show in the class) 
            CodeTypeDeclaration classModel = new CodeTypeDeclaration(className);   // Intialize a new class with its name 
            newNamespace.Types.Add(classModel);                                    // Add class to the namespace 

            compileUnit.Namespaces.Add(blankNamespace);                            // Add the blank namespace to the compile unit (This won't show in the class, but it's using statements will) 
            compileUnit.Namespaces.Add(newNamespace);                              // Add the real namespace to the compile unit 

            classModel.TypeAttributes = System.Reflection.TypeAttributes.Public;  // Set class to public 

            return classModel;
        }

        /// <summary>
        /// Build the property and add it to the class 
        /// </summary>
        /// <param name="classModel">Current Class the properties are being added to</param>
        /// <param name="type">The type of property Ex: string or int </param>
        /// <param name="propertyName">Name of property</param>
        public static void AddProperty(CodeTypeDeclaration classModel, string type, string propertyName)
        {
            // JSON Property
            CodeSnippetTypeMember snippetJSON = new CodeSnippetTypeMember();            // Initalize a new code snippet type 
            snippetJSON.Text = $"        [JsonProperty(\"{ propertyName }\")]";                    // Build the JSON property.  Note: The spacing is needed in order to format the code correctly 
            classModel.Members.Add(snippetJSON);

            // Property
            // Format the property name correctly: Convert first letter to upper, and remove the underscores
            string formattedPropName = "";
            if (propertyName.Contains("_"))
            {
                var propNameArray = propertyName.Split("_");

                foreach (var singlePropName in propNameArray)
                {
                    formattedPropName = formattedPropName + FirstCharToUpper(singlePropName);
                }
            }
            else
            {
                formattedPropName = FirstCharToUpper(propertyName);
            }

            // Note: We used the CodeSnippet instead of CodeMemberProperty because that was the only way to format the empty getters and setters
            CodeSnippetTypeMember snippet = new CodeSnippetTypeMember();            // Initalize a new code snippet type 
            snippet.Text = $"        public {type} {formattedPropName}" + " { get; set; }"; // Build the property. Note: The spacing is needed in order to format the code correctly 
            classModel.Members.Add(snippet);                                        // Add the property to the class   
        }

        /// <summary>
        /// Convert the first char of a string to upper
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string FirstCharToUpper(string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default: return input.First().ToString().ToUpper() + input.Substring(1);
            }
        }

        /// <summary>
        /// Convert Type to a C# friendly term
        /// </summary>
        /// <param name="openAPIProperty"></param>
        /// <returns>string</returns>
        public static string ConvertType(KeyValuePair<string, Microsoft.OpenApi.Models.OpenApiSchema> openAPIProperty, string className)
        {
            string resultType = string.Empty;

            switch (openAPIProperty.Value.Type)
            {
                case ("integer"):
                    if (openAPIProperty.Value.Format != null && openAPIProperty.Value.Format == "int64")
                        resultType = "long";
                    else
                        resultType = "int";
                    break;
                case ("boolean"):
                    resultType = "bool";
                    break;
                case ("string"):
                    if (openAPIProperty.Value.Format != null && openAPIProperty.Value.Format == "uuid")
                        resultType = "Guid";
                    else if (openAPIProperty.Value.Format != null && openAPIProperty.Value.Format == "date-time")
                        resultType = "DateTime";
                    else
                        resultType = "string";
                    break;
                case ("array"):
                    if (openAPIProperty.Value.Items.Reference != null)
                    {
                        string subCollectionName = openAPIProperty.Value.Items.Reference.Id;
                        resultType = $"List<{subCollectionName}>";
                    }
                    else if(openAPIProperty.Value.Items.Type != null)
                        resultType = $"List<{openAPIProperty.Value.Items.Type}>";
                    else 
                        throw new Exception($"Unable to find Items.Reference for: {className} as type {openAPIProperty.Value.Type}");
                    break;
                case "number":
                    if (openAPIProperty.Value.Format != null && openAPIProperty.Value.Format == "double")
                        resultType = "double";
                    else
                        resultType = "decimal";
                    break;
                case ("object"):
                    // The OpenAPI property type defaults to "object" in some cases, such as when the property is referencing another schema
                    // If the property has a value for Reference 
                    if (openAPIProperty.Value.Reference != null)
                        resultType = openAPIProperty.Value.Reference.Id; // Set it to the reference Id 
                    // If the property has a value for additonal properties... then it's probably a dictionary
                    else if (openAPIProperty.Value.AdditionalProperties != null && openAPIProperty.Value.AdditionalProperties.Type == "string")
                        resultType = "Dictionary<string, string>"; // So far we only have dictionaries with type string, string. 
                                                                   // If we add more types of dictionaries we will need to adjust this 
                    else if (openAPIProperty.Value.Type != null)
                        resultType = openAPIProperty.Value.Type;
                    break;
                case (null):
                    // A generic "object" type will show up as null in the OpenAPI schema
                    resultType = "object";
                    break;
                default:
                    resultType = openAPIProperty.Value.Type;
                    break;
            }

            // Look for nullable fields
            // Do not set strings, lists, objects, or dictionaries as nullable value types (even though they may show "nullable" = true)
            if (openAPIProperty.Value.Nullable == true && resultType != "string" && !resultType.Contains("List") && !resultType.Contains("Dictionary") && !resultType.Contains("object"))
                resultType = resultType + "?";

            return resultType;
        }

        /// <summary>
        /// Generate file in the requested location 
        /// </summary>
        /// <param name="compileUnit"></param>
        /// <param name="className"></param>
        /// <param name="path">User Requested Path location</param>
        public static void GenerateFile(CodeCompileUnit compileUnit, string className, string path, string suffix)
        {
            string codeFileName = $"{path}\\{suffix}\\{className}.cs";

            //Ensure that the suffix path exists
            if(!System.IO.Directory.Exists($"{path}\\{suffix}"))
                System.IO.Directory.CreateDirectory($"{path}\\{suffix}");

            CodeDomProvider codeDomProvider = CodeDomProvider.CreateProvider("CSharp");
            IndentedTextWriter tw = new IndentedTextWriter(new System.IO.StreamWriter(codeFileName, false), "    ");
            codeDomProvider.GenerateCodeFromCompileUnit(compileUnit, tw, new CodeGeneratorOptions());
            tw.Close();
        }

    }
}
