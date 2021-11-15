using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationDevelopmentUtility.Utilities
{
    public class UsageDisplay
    {
        public static int DisplayWidth = 80;

        public string Description;
        public string UsageSummary;
        public string Example;
        public string PreparamInstruction;
        public List<UsageDisplayParameter> Parameters = new List<UsageDisplayParameter>();

        public void PrintToConsole()
        {
            var dispalyStr = AddStringWithOverflow(Description, 0) + Environment.NewLine;
            dispalyStr += Environment.NewLine + AddStringWithOverflow(UsageSummary, 0);
            dispalyStr += Environment.NewLine + Environment.NewLine + AddStringWithOverflow(new String(' ', 5) + Example, 22) + Environment.NewLine;
            if (!string.IsNullOrEmpty(PreparamInstruction))
                dispalyStr += Environment.NewLine + AddStringWithOverflow(PreparamInstruction, 0) + Environment.NewLine;

            foreach(var param in Parameters)
            {
                var paramDisplay = new String(' ', 5) + param.Name;
                if (paramDisplay.Length < 22)
                    paramDisplay += new String(' ', 22 - paramDisplay.Length);
                else
                    paramDisplay += " ";
                paramDisplay += param.Description;
                dispalyStr += Environment.NewLine + AddStringWithOverflow(paramDisplay, 22);
            }

            StandardUtilities.WriteToConsole(dispalyStr, StandardUtilities.Severity.LOCAL);
        }

        //Add a string to our write to console list, with the feature that any overflow text gets added at a specified position.
        //  EG we want our help to look like this:
        //       HookType         The hook scope that you will be using. The value used may
        //                        come from the list of iPaaS scopes or from the external
        //                        system's list, depending on the direction of the etc.
        //instead of this:
        //       HookType         The hook scope that you will be using. The value used may
        //come from the list of iPaaS scopes or from the external system's list, depending
        //on the direction of the transfer.
        private string AddStringWithOverflow(string value, int overflowPosition)
        {
            value = value.Replace("\t", new String(' ', 9)); //Replace tabs with 9 char strings so we can use .Length appropraitely

            //Nothing is required to do here, just return the value
            if (value.Length < DisplayWidth)
                return value;

            List<string> displayList = new List<string>();
            int i = 0; //This counter is a failsafe to prevent an infinite loop here. There shouldn't ever be a case where that happens, but better to be safe about it.
            while(value.Length > DisplayWidth && i < 5)
            {
                i++;
                var currentLine = value.Substring(0, DisplayWidth); //TODO: this cuts it off at the exact edge. Chagne it to find the last space
                value = value.Substring(currentLine.Length); //Remove the section we just queued.

                //If the last char of currentLine was a space, trim it off.
                if (currentLine.Substring(currentLine.Length - 1) == " ")
                    currentLine = currentLine.Substring(0, currentLine.Length -1);
                //If the first char of value is a space, trim it off
                else if (value.Substring(0, 1) == " ")
                    value = value.Substring(1);
                else if (currentLine.LastIndexOf(" ") == -1)
                    ; //If there are no spaces, then the mid-word trim is the best we can do.
                else
                {
                    var originalValue = currentLine;

                    //If it doesn't fit either of the cases above, we trimmed currentLine in the middle of a word.
                    //  So we need to look for the last space in currentLine, remove everything after it, and stick it onto value
                    var wordPart = currentLine.Substring(currentLine.LastIndexOf(" ") + 1);
                    currentLine = currentLine.Substring(0, currentLine.Length - wordPart.Length); //Remove the word part from currentLine
                    value = wordPart + value; //Stick the wordpart on value

                    //if the current line is white space, that means we have one long word that is longer than the space available to it.
                    //In that case, we just print it right-aligned and let the console overflow if it's still needed
                    if (currentLine.Trim() == "")
                        continue;
                }

                displayList.Add(currentLine);
                value = new String(' ', overflowPosition) + value; //Pad the string to the overflow position.
            }

            displayList.Add(value); //Add the last bit

            string retVal = "";
            foreach (var displayLine in displayList)
                retVal += displayLine + Environment.NewLine;

            retVal = retVal.Substring(0, retVal.Length - Environment.NewLine.Length); //Trim the last newline

            return retVal;
        }
    }

    public class UsageDisplayParameter
    {
        public string Name;
        public string Description;
    }
}
