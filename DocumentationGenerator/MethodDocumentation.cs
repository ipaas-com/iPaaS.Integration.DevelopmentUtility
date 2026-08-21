using IntegrationDevelopmentUtility.iPaaSModels;
using IntegrationDevelopmentUtility.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

        /// <summary>
        /// The dynamic formula update is a full overwrite: every field on the request is written to the
        /// database, so a null erases whatever was there. We only know what the assembly tells us, which
        /// leaves two gaps:
        ///
        ///   - Format, MinLength and MaxLength are not derivable from reflection and are never populated
        ///     here, so an update would blank them on every run.
        ///   - Descriptions, examples and the returns text come from the XML documentation file. A method
        ///     with no doc comment, or a stale XML file, yields nulls that would wipe curated text.
        ///
        /// So where the code has nothing to say, keep what is already stored. Where the code does have a
        /// value it still wins - the point of the run is to make the database match the code.
        ///
        /// Deliberately NOT carried forward:
        ///   - Status, which is derived from the [Obsolete] attribute. The code is the source of truth for
        ///     whether a formula is deprecated, so an update should reset a manual status change.
        ///   - IsRequired, Name, DataType, Formula and IsAsync, which are always read from the assembly.
        /// </summary>
        private static void CarryForwardUnknownValues(DynamicFormulaRequest request, DynamicFormulaResponse existing)
        {
            if (string.IsNullOrEmpty(request.Description))
                request.Description = existing.Description;

            if (string.IsNullOrEmpty(request.Example))
                request.Example = existing.Example;

            if (request.ReturnParameter != null && existing.ReturnParameter != null
                && string.IsNullOrEmpty(request.ReturnParameter.Description))
                request.ReturnParameter.Description = existing.ReturnParameter.Description;

            if (request.Parameters == null || existing.Parameters == null)
                return;

            foreach (var requestParam in request.Parameters)
            {
                //Match on name. A renamed parameter is effectively a new one, so it correctly gets nothing.
                var existingParam = existing.Parameters.Find(x =>
                    string.Equals(x.Name, requestParam.Name, StringComparison.OrdinalIgnoreCase));
                if (existingParam == null)
                    continue;

                //These three are never set from reflection, so they would always be blanked without this.
                requestParam.Format = existingParam.Format;
                requestParam.MinLength = existingParam.MinLength;
                requestParam.MaxLength = existingParam.MaxLength;

                if (string.IsNullOrEmpty(requestParam.Description))
                    requestParam.Description = existingParam.Description;
            }
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

        /// <param name="updateOnly">
        /// When true, only update formulas that already exist. Returns false without calling the API if
        /// there is no match, so a run without an XML file cannot create anything.
        /// </param>
        /// <returns>True when the formula was created or updated, false when it was skipped.</returns>
        public async Task<bool> ToAPI(string systemTypeVersionId, FullToken fullToken, bool updateOnly = false)
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
                request.Parameters.Add(new DynamicFormulaParameterRequest() { Name = param.Name, Description = param.Description, DataType = param.Type, IsRequired = param.IsRequired });

            //We must build the formula.
            request.Formula = $"{this.Name}({string.Join(", ", this.Parameters.Select(p => p.Type + " " + p.Name))})"; //This field is required, so we need something here. 

            //We need to ensure that this formula does not already exist.
            var existingFormulas = iPaaSCallWrapper.DynamicFormulas(systemTypeVersionId, fullToken);
            long? existingId = null; 
            DynamicFormulaResponse existingFormula = null;
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
                        existingFormula = matchingName;
                        break; //we can stop looking
                    }
                }
            }

            //The update below is a full overwrite, so anything we leave null is erased. Carry forward the
            //values we cannot see from the assembly before we send it.
            if (existingFormula != null)
                CarryForwardUnknownValues(request, existingFormula);

            //Nothing to update and we are not allowed to create, so leave it alone. The caller reports these.
            if (updateOnly && !existingId.HasValue)
                return false;


            try
            {
                DynamicFormulaResponse response;
                if(existingId.HasValue)
                    response = iPaaSCallWrapper.DynamicFormulaUpdate(request, fullToken, existingId.Value);
                else
                    response = iPaaSCallWrapper.DynamicFormulaCreate(request, fullToken);
                
                StandardUtilities.WriteToConsole($"Successfully {(existingId.HasValue ? "updated" : "created")} {request.Name}", StandardUtilities.Severity.DETAIL);
                return true;
            }
            catch (Exception ex) 
            {
                StandardUtilities.WriteToConsole($"Unable to save Dynamic Formula named {this.Name}", StandardUtilities.Severity.ERROR);
                StandardUtilities.WriteToConsole(ex.Message, StandardUtilities.Severity.ERROR);
                return false;
            }
        }
    }

    public class MethodDocumentationParameter
    {
        public string Name;
        public string Description;
        public string Type;
        public int Order;
        /// <summary>
        /// False when the caller may omit this parameter - either it has a default value or it is a
        /// params array (which accepts zero arguments). See User Story 22028.
        /// </summary>
        public bool IsRequired;
    }
}
