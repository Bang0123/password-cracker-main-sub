using static System.Console;

namespace MasterCrack
{
    public class Program
    {
        static void Main(string[] args)
        {
            var master = new Master();
            master.Invoke();

            ReadLine();
        }
    }
}
