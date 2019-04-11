using System;
using System.IO;
using System.Threading.Tasks;

namespace Dx2_DiscordBot
{
    public class Logger
    {
        public static StreamWriter LogFile;

        //Entry Way
        public static void SetupLogger()
        {
            if (!File.Exists("log.txt"))
                File.Create("log.txt");
            Logger.LogFile = File.AppendText("log.txt");
        }


        //Performs some simple logging for our application
        public static Task LogAsync(string log)
        {
            LogFile.WriteLine(log);
            Console.WriteLine(log);
            return Task.CompletedTask;
        }

        //Performs some simple logging for our Discord CLient
        public static Task LogAsync(Discord.LogMessage log)
        {
            LogAsync(log.ToString());
            return Task.CompletedTask;
        }
    }
}
