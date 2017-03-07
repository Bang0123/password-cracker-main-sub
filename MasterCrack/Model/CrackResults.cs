using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MasterCrack.Model
{
    public class CrackResults
    {
        public List<FullUser> Results { get; set; }
        public TimeSpan TimeElapsed { get; set; }

        public string TotalsString { get; set; }
        public string TimeString { get; set; }

        public CrackResults(List<FullUser> results, TimeSpan timeElapsed, string totalsString, string timeString)
        {
            Results = results;
            TimeElapsed = timeElapsed;
            TotalsString = totalsString;
            TimeString = timeString;
        }


    }
}
