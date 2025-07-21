using IntegrationDevelopmentUtility.iPaaSModels;
using IntegrationDevelopmentUtility.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace IntegrationDevelopmentUtility.DocumentationGenerator
{
    public class MethodDocumentation
    {
        public string Name { get; set; }
        public string Summary { get; set; }
        public string Example { get; set; }
        public string Formula { get; set; }
        public bool IsAsync { get; set; }
        //Represents the value from TM_DynamicFormulaStatus (NONE=0, ACTIVE=1,DEPRECATED=2,REMOVED=3)
        public int Status { get; set; }
        public string ReturnsDescription { get; set; }
        public string ReturnType { get; set; }
        public string Remarks { get; set; }
        public List<MethodDocumentationParameter> Parameters = new List<MethodDocumentationParameter>();

        public void ToWikiString()
        {
            var callWithParams = Name + "(";
            foreach (var param in Parameters)
                callWithParams += $"{param.Type} {param.Name}, ";
            if (Parameters != null && Parameters.Count > 0) //Do not remove the last comma if there were no parameters
                callWithParams = callWithParams.Substring(0, callWithParams.Length - 2); //Remove the last comma
            callWithParams += ")";

            //| CoalesceToDateTime(params object[] list) | DateTime | Returns the first non-null element, converted to a DateTime | CoalesceToDateTime(PROF_DAT_1, PROF_DAT_2) |
            string retVal = $"| {HTMLEncode(callWithParams)} | {HTMLEncode(ReturnsDescription)} | {HTMLEncode(Summary)} | {HTMLEncode(Example)} |";

            Console.WriteLine(retVal);
        }

        private string HTMLEncode(string input)
        {
            if (input == null)
                return null;

            input = System.Net.WebUtility.HtmlDecode(input);
            input = System.Net.WebUtility.HtmlEncode(input);
            input = input.Replace("|", "&#124;");//Remove any pipe chars
            return input;
        }

        public void ToCsvString()
        {
            var callWithParams = Name + "(";
            foreach (var param in Parameters)
                callWithParams += $"{param.Type} {param.Name}, ";
            if (Parameters != null && Parameters.Count > 0) //Do not remove the last comma if there were no parameters
                callWithParams = callWithParams.Substring(0, callWithParams.Length - 2); //Remove the last comma
            callWithParams += ")";


            var output = $"{MakeCsvSafe(callWithParams)},{MakeCsvSafe(ReturnsDescription)},{MakeCsvSafe(Summary)},{MakeCsvSafe(Example)}";
            Console.WriteLine(output);
        }

        public string MakeCsvSafe(string input)
        {
            if (input == null)
                return null;

            input = input.Trim();

            if (input.Contains(",") || input.Contains("\"") || input.Contains("\n"))
            {
                input = input.Replace("\"", "\"\"");
                input = $"\"{input}\"";
            }
            return input;
        }

        public async void ToAPI(string systemTypeVersionId, FullToken fullToken)
        {
            var request = new DynamicFormulaRequest();
            request.Name = this.Name;
            request.Description = this.Summary;
            request.Example = this.Example;
            request.Formula = this.Formula;
            request.IsAsync = this.IsAsync;
            request.Status = (this.Status == 0 ?  1 : this.Status);
            request.SystemTypeVersionId = systemTypeVersionId;
            request.ReturnParameter = new ReturnParameterRequest() { Description = this.ReturnsDescription, DataType = this.ReturnType };
            request.Parameters = new List<DynamicFormulaParameterRequest>();
            foreach (var param in Parameters)
                request.Parameters.Add(new DynamicFormulaParameterRequest() { Name = param.Name, Description = param.Description, DataType = param.Type });

            //We must build the formula.
            request.Formula = $"{this.Name}({string.Join(", ", this.Parameters.Select(p => p.Type + " " + p.Name))})"; //This field is required, so we need something here. 

            //We need to ensure that this formula does not already exist.
            var existingFormulas = iPaaSCallWrapper.DynamicFormulas(systemTypeVersionId, fullToken);
            long? existingId = null; 
            if(existingFormulas != null)
            {
                //First match everything by name and param count
                foreach (var matchingName in existingFormulas.FindAll(x => x.Name == request.Name && (x.Parameters?.Count ?? 0) == (request.Parameters?.Count ?? 0)))
                {
                    int matchingParams = 0;
                    foreach (var requestParam in request.Parameters)
                    {
                        var paramMatch = matchingName.Parameters.Find(x => x.DataType.ToLower() == requestParam.DataType.ToLower());
                        if (paramMatch != null)
                            matchingParams++;
                    }

                    //If we had a match for every parameter, then we have a full match
                    if (matchingParams == request.Parameters.Count())
                    {
                        existingId = matchingName.Id;
                        break; //we can stop looking
                    }
                }
            }


            try
            {
                DynamicFormulaResponse response;
                if(existingId.HasValue)
                    response = iPaaSCallWrapper.DynamicFormulaUpdate(request, fullToken, existingId.Value);
                else
                    response = iPaaSCallWrapper.DynamicFormulaCreate(request, fullToken);
                
                StandardUtilities.WriteToConsole($"Successfully {(existingId.HasValue ? "updated" : "created")} {request.Name}", StandardUtilities.Severity.DETAIL);
            }
            catch (Exception ex) 
            {
                StandardUtilities.WriteToConsole($"Unable to save Dynamic Formula named {this.Name}", StandardUtilities.Severity.ERROR);
                StandardUtilities.WriteToConsole(ex.Message, StandardUtilities.Severity.ERROR);
            }
        }
    }

    public class MethodDocumentationParameter
    {
        public string Name;
        public string Description;
        public string Type;
        public int Order;
    }
}
