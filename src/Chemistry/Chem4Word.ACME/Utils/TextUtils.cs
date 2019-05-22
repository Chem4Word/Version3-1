using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Chem4Word.ACME.Utils
{
     public static class TextUtils
    {
        public static Dictionary<string, string> MatchNamedCaptures(this Regex regex, string input)
        {
            var namedCaptureDictionary = new Dictionary<string, string>();

            var match = regex.Match(input);

            GroupCollection groups = match.Groups;

            string[] groupNames = regex.GetGroupNames();
            foreach (string groupName in groupNames)
                if (groups[groupName].Captures.Count > 0)
                    namedCaptureDictionary.Add(groupName, groups[groupName].Value);
            return namedCaptureDictionary;
        }
    }
}
