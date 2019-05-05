using NLog;
using System;
using System.Threading.Tasks;

namespace Dx2_DiscordBot
{
    public class Logger
    {
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        //Entry Way
        public static void SetupLogger()
        {
            var config = new NLog.Config.LoggingConfiguration();

            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "log.txt" };            
            config.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
            LogManager.Configuration = config;

            LogAsync("Starting new Session..");
        }

        //Performs some simple logging for our application
        public static Task LogAsync(string log)
        {
            logger.Debug(log);
            LogManager.Flush();

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
