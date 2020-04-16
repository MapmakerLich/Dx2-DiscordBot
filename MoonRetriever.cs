using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dx2_DiscordBot
{
    public class MoonRetriever : RetrieverBase
    {
        #region Properties

        System.Timers.Timer timer = null;

        #endregion

        #region Constructor

        //Constructor
        public MoonRetriever(DiscordSocketClient client) : base(client)
        {
            MainCommand = "!moon";
        }

        #endregion

        #region Overrides

        //Initialization
        public async override Task ReadyAsync()
        {
            SetupTimer();
        }

        public void SetupTimer()
        {
            if (timer == null)
            {
                var upcomingMoon = GetUpcomingMoon();

                var moonTime = new DateTime(1970, 1, 1).AddSeconds(upcomingMoon);
                var now = DateTime.Now.ToUniversalTime();

                var timeSpan = moonTime.Subtract(now);

                var oneMinute = 60000;
                timer = new System.Timers.Timer(timeSpan.TotalMilliseconds-oneMinute);
                timer.Elapsed += OnAlert;
                timer.Enabled = true;
            }
        }

        private List<string> botSpamChannelNames = new List<string>() { "bot-spam", "spam-bot" };
        private List<string> moonPhaseChannelNames = new List<string>() { "moon-phase", "phase-moon" };

        private void OnAlert(object sender, System.Timers.ElapsedEventArgs e)
        {
            var task = new Task(SendAlert);
            task.Start();
        }

        public async void SendAlert()
        {
            timer.Enabled = false;
            timer = null;

            await Logger.LogAsync("SEND THE ALERT! This is a test.");

            foreach (var g in _client.Guilds)
            {
                if (g.Name == "Children of Mara")
                {
                    var botSpam = false;
                    var moonPhase = false;

                    ulong botSpamChnlId = 0;
                    ulong moonPhaseChnlId = 0;

                    foreach (var c in g.Channels)
                    {
                        if (botSpam == false)
                        {
                            botSpam = botSpamChannelNames.Any(s => s == c.Name);
                            botSpamChnlId = c.Id;
                        }
                        if (moonPhase == false)
                        {
                            moonPhase = moonPhaseChannelNames.Any(s => s == c.Name);
                            moonPhaseChnlId = c.Id;
                        }
                    }

                    var role = g.Roles.First(r => r.Name == "FullMoonCrew");
                    var eb = new EmbedBuilder();
                    eb.WithDescription("Full Moon has started in Aura Gate!");

                    if (moonPhase && moonPhaseChnlId != 0)
                    {
                        var chnl = _client.GetChannel(moonPhaseChnlId) as IMessageChannel;
                        if (role != null && role.IsMentionable)
                            await chnl.SendMessageAsync(role.Mention);
                        await chnl.SendMessageAsync("", false, eb.Build());
                    }
                    else if (botSpam && botSpamChnlId != 0)
                    {
                        var chnl = _client.GetChannel(botSpamChnlId) as IMessageChannel;
                        if (role != null && role.IsMentionable)
                            await chnl.SendMessageAsync(role.Mention);
                        await chnl.SendMessageAsync("", false, eb.Build());
                    }
                }
            }

            var timeUntiNextMoon = 7080000;       
            timer = new System.Timers.Timer(timeUntiNextMoon);
            timer.Elapsed += OnAlert;
            timer.Enabled = true;
        }

        //Recieve Messages here
        public async override Task MessageReceivedAsync(SocketMessage message, string serverName, ulong channelId)
        {
            //Let Base Update
            await base.MessageReceivedAsync(message, serverName, channelId);

            if (_client.GetChannel(channelId) is IMessageChannel chnl)
            {
                if (message.Content.StartsWith(MainCommand + "help"))
                {
                    await chnl.SendMessageAsync(
                        "1.) Create a channel called bot-spam or moon-phase that the bot has permission to write to.\n" +
                        "2.) moon-phase will take priority over bot-spam as the bots output if you have both.\n" +
                        "3.) Bot will automatically write to this channel every time a full moon begins.\n" +
                        "The bot can also mention the role named 'FullMoonCrew' if you create and assign it for your users.", false);
                }
                else if (message.Content.StartsWith(MainCommand + "next"))
                {
                    var upcomingMoon = GetUpcomingMoon();
                    var upcomingMoon2 = GetNextFullMoon(upcomingMoon);
                    var upcomingMoon3 = GetNextFullMoon(upcomingMoon2);

                    var description = FormatNextTime(upcomingMoon) + "\n" + FormatNextTime(upcomingMoon2) + "\n" + FormatNextTime(upcomingMoon3);

                    var eb = new EmbedBuilder();
                    eb.WithDescription(description);
                    await chnl.SendMessageAsync("", false, eb.Build());
                }
            }
        }

        //Returns the commands for this Retriever
        public override string GetCommands()
        {
            return "\n\nMoon Phases:" +
            "\n* " + MainCommand + "help: Prints instructions for setting up Moon Phases on your Discord Server." +
            "\n* " + MainCommand + "next: Prints the next 3 upcoming full moon phase.";
        }

        #endregion

        #region Methods

        private const int start_ref = 1544848380;
        private const int moon_duration = 7 * 60;
        private const int full_moon_duration = 10 * 60;
        public const int next_moons_count = 20;

        public int GetUpcomingMoon()
        {
            var nextFullMoon = start_ref;
            var currentTime = GetCurrentTime();

            while (true)
            {
                nextFullMoon = GetNextFullMoon(nextFullMoon);

                if (nextFullMoon < currentTime)
                    continue;
                else
                    break;                    
            }

            return nextFullMoon;
        }

        //Gets next full moon by adding seconds to time provided
        public int GetNextFullMoon(int currentTime)
        {
            return currentTime + full_moon_duration * 2 + moon_duration * 14;
        }

        //Converts seconds to time with seconds beginning at 1/1/1970
        public string FormatNextTime(int seconds)
        {            
            var moonTime = new DateTime(1970, 1, 1).AddSeconds(seconds);
            var now = DateTime.Now.ToUniversalTime();

            var timeSpan = moonTime.Subtract(now);            

            return moonTime.ToString("H:mm tt UTC") + string.Format(" {0} Hour(s) and {1} Minute(s) Away", timeSpan.Hours, timeSpan.Minutes);
        }

        //Gets seconds since 1/1/1970
        public double GetCurrentTime()
        {
            var time = (DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1));
            return Math.Floor(time.TotalMilliseconds / 1000);
        }
        
        #endregion
    }
}
