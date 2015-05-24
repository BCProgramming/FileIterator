using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BCFileSearch
{
    public class SearchFilter : ISearchFilter 
    {
        private String _NameRegEx = ".*";
        private Regex NameRe = null;

        public SearchFilter(String pPattern)
        {
            _NameRegEx = pPattern;
            NameRe = new Regex(pPattern, RegexOptions.IgnoreCase);
        }

        public int Filter(FileSearchResult fsr)
        {
            return NameRe.IsMatch(fsr.FileName) ? 10 : -10;
        }

        public int Recurse(FileSearchResult fsr)
        {
            return 0;
        }
    }
}
