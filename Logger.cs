using System;
using System.Threading.Tasks;

namespace Dx2_DiscordBot
{
    public class Logger
    {
        //Performs some simple logging for our application
        public static Task LogAsync(string log)
        {
            Console.WriteLine(log);
            return Task.CompletedTask;
        }

        //Performs some simple logging for our Discord CLient
        public static Task LogAsync(Discord.LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }
    }
}
