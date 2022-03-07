using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainCrack.Model
{
    public class CrackResults
    {
        public List<FullUser> Results { get; set; }
        public TimeSpan TimeElapsed { get; set; }
        public string TotalsString { get; set; }
        public string TimeString { get; set; }
        public int Hashes { get; set; }

        public CrackResults()
        {
            
        }

        public CrackResults(List<FullUser> results, TimeSpan timeElapsed, string totalsString, string timeString, int hashes)
        {
            Results = results;
            TimeElapsed = timeElapsed;
            TotalsString = totalsString;
            TimeString = timeString;
            Hashes = hashes;
        }


    }
}
