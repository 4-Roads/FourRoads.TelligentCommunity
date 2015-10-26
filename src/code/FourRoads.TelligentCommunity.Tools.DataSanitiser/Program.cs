using System;
using log4net;
using log4net.Config;

namespace FourRoads.TelligentCommunity.Tools.DataSanitiser
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            BasicConfigurator.Configure();
            var log = LogManager.GetLogger("Sanitiser");

            try
            {
                Sainitizer.Instance.Execute(log);
            }
            catch (Exception exception)
            {
                log.Fatal(exception.Message, exception);
            }

            Console.Write("Press any key...");
            Console.ReadLine();
        }
    }
}