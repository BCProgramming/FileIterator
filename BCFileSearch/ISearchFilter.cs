using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BCFileSearch
{
    

    public interface ISearchFilter
    {
        /// <summary>
        /// Method called when evaluating a Search Result for inclusion results compiled and retrieved by FileSearch. This method is called
        /// on all applicable ISearchFilter implementations in effect.
        /// if the Sum of Results is positive it will be displayed as a result; otherwise it will not be shown.
        /// </summary>
        /// <param name="fsr">FileSearchResult instance to evaluate.</param>
        /// <returns>integral value for weighting. Positive values indicate a positive result on this entry. Negative values indicate a negative result.</returns>
        int Filter(FileSearchResult fsr);

        int Recurse(FileSearchResult fsr);
    }
}
