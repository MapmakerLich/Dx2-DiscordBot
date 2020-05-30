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

        Random rand = new Random();

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

                var moonTime = new DateTime().AddSeconds(upcomingMoon);
                var now = DateTime.Now.ToUniversalTime();

                var minusTime = 240000;
                var timeSpan = new TimeSpan();
                timeSpan = moonTime.Subtract(now);
                var max = Math.Max(timeSpan.TotalMilliseconds - minusTime, 0);

                if (max == 0)
                {
                    var nextFullMoon = GetNextFullMoon(upcomingMoon);
                    moonTime = new DateTime().AddSeconds(nextFullMoon);
                    timeSpan = moonTime.Subtract(now);
                }

                timer = new System.Timers.Timer(timeSpan.TotalMilliseconds - minusTime);
                timer.Elapsed += OnAlert;
                timer.Enabled = true;
            }
        }

        private List<string> botSpamChannelNames = new List<string>() { "bot-spam", "spam-bot" };
        private List<string> moonPhaseChannelNames = new List<string>() { "moon-phase", "phase-moon" };

        private void OnAlert(object sender, System.Timers.ElapsedEventArgs e)
        {
            var task = new Task(() => SendAlert());
            task.Start();
        }

        public async void SendAlert(bool forTesting = false)
        {
            timer.Enabled = false;
            timer = null;

            if (_client != null && _client.Guilds != null)
            {
                await Logger.LogAsync("SEND THE ALERT!");

                foreach (var g in _client.Guilds)
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

                    var role = g.Roles.FirstOrDefault(r => r.Name == "FullMoonCrew");
                    var randomPhrase = new[] { "Head to Aura Gate!", "Suit up!", "Theres evil afoot!", "DANGER, DANGER, DANGER!" };
                    int index = rand.Next(randomPhrase.Length);

                    if (!forTesting)
                    {
                        var message = "```Full Moon begins in three minutes. " + randomPhrase[index] + "\n!moonunsub to stop being notified of this event.\n!moonsub to begin receiving notifications!```";
                        if (moonPhase && moonPhaseChnlId != 0)
                        {
                            var chnl = _client.GetChannel(moonPhaseChnlId) as IMessageChannel;
                            if (role != null)
                                await chnl.SendMessageAsync(role.Mention + message);
                            else
                                await chnl.SendMessageAsync(message);

                            await Logger.LogAsync("Sending Alert to '" + g.Name + "' in channel '" + chnl.Name + "'");
                        }
                        else if (botSpam && botSpamChnlId != 0)
                        {
                            var chnl = _client.GetChannel(botSpamChnlId) as IMessageChannel;
                            if (role != null)
                                await chnl.SendMessageAsync(role.Mention + message);
                            else
                                await chnl.SendMessageAsync(message);

                            await Logger.LogAsync("Sending Alert to '" + g.Name + "' in channel '" + chnl.Name + "'");
                        }
                    }
                }
            }
            else
                await Logger.LogAsync("Could not send the Alert!");

            SetupTimer();
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
                        "```Below is the steps to set the bot up in your own server. Not all must be done so please read over them.\n" +
                        "1.) Create a channel called bot-spam or moon-phase that the bot has permission to write to them.\n" +
                        "2.) moon-phase will take priority over bot-spam as the bots output if you have both.\n" +
                        "3.) The bot will mention the role named 'FullMoonCrew' when sending the alert.\n" +
                        "4.) If you wish to allow your users to subscribe and unsubscribe from a role the bot manages give the bot permission to manage roles." +
                        "The bot will create a role called 'FullMoonCrew' and this role will used with the !moonsub and !moonunsub commands allowing users to set and un-set the role at will.\n" +
                        "5.) If you do not need the 'FullMoonCrew' role then instead you can decide not to give the bot role permissions and you will still receive alerts just not targeted to a specific role.\n\n" +
                        "If you have issues getting this working on your server, seeing an issue, or dislike this feature please message @Alenael.1801.```", false);
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
                else if (message.Content.StartsWith(MainCommand + "sub"))
                {
                    var roleExists = CreateRole(serverName, out SocketRole role);

                    if (roleExists)
                    {
                        var user = await chnl.GetUserAsync(message.Author.Id);
                        await (user as IGuildUser)?.AddRoleAsync(role);
                        await chnl.SendMessageAsync(message.Author.Mention + "```Subscribed to Full Moon Alert. Use !moonunsub to remove this subscription.```");
                    }
                    else
                        await chnl.SendMessageAsync("```Ask server owner to follow steps in !moonhelp in order to get this working.```");
                }
                else if (message.Content.StartsWith(MainCommand + "unsub"))
                {
                    var roleExists = CreateRole(serverName, out SocketRole role);

                    if (roleExists)
                    {
                        var user = await chnl.GetUserAsync(message.Author.Id);
                        await (user as IGuildUser)?.RemoveRoleAsync(role);
                        await chnl.SendMessageAsync(message.Author.Mention + "```Unsubscribed from Full Moon Alert.```");
                    }
                    else
                        await chnl.SendMessageAsync("```Ask server owner to follow steps in !moonhelp in order to get this working.```");
                }
                else if (message.Content.StartsWith(MainCommand + "test"))
                {
                    if (message.Author.ToString() == "Alenael#1801")
                        SendAlert(true);
                }
            }
        }

        //Returns the commands for this Retriever
        public override string GetCommands()
        {
            return "\n\nMoon Phases:" +
            "\n* " + MainCommand + "help: Prints instructions for setting up Moon Phases on your Discord Server." +
            "\n* " + MainCommand + "sub: Adds the FullMoonCrew role to you if the server has setup the bot with enough permissions to do so." +
            "\n* " + MainCommand + "unsub: Removes the FullMoonCrew role from you if the server has setup the bot with enough permissions to do so." +
            "\n* " + MainCommand + "next: Prints the next 3 upcoming full moon phase.";
        }

        #endregion

        #region Methods

        private double start_ref = new TimeSpan(new DateTime(2020, 4, 16, 8, 50, 0, DateTimeKind.Utc).Ticks).TotalSeconds;
        private const int moon_duration = 7 * 60;
        private const int full_moon_duration = 10 * 60;
        public const int next_moons_count = 20;

        public bool CreateRole(string serverName, out SocketRole role)
        {
            var guild = _client.Guilds.FirstOrDefault(g => g.Name == serverName);
            role = guild?.Roles.FirstOrDefault(r => r.Name == "FullMoonCrew");

            if (guild != null)
            {
                if (guild.CurrentUser.GuildPermissions.ManageRoles)
                {
                    if (role == null)
                    {
                        try
                        {
                            guild.CreateRoleAsync("FullMoonCrew", null, null, false, null);
                        }
                        catch(Exception e)
                        {
                            Logger.LogAsync("Error: " + e.StackTrace);
                        }                        
                        return true;
                    }
                    else
                        return true;
                }
                else
                    return false;
            }

            return false;
        }

        public long GetUpcomingMoon()
        {
            var nextFullMoon = Math.Floor(start_ref);

            while (true)
            {
                nextFullMoon = GetNextFullMoon(nextFullMoon);

                if (nextFullMoon < GetCurrentTime())
                    continue;
                else
                    break;
            }

            return (long) nextFullMoon;
        }

        //Gets next full moon by adding seconds to time provided
        public long GetNextFullMoon(double currentTime)
        {
            return (long) currentTime + (full_moon_duration * 2) + moon_duration * 14;
        }

        //Converts seconds to time with seconds beginning at 1/1/1970
        public string FormatNextTime(long seconds)
        {            
            var moonTime = new DateTime().AddSeconds(seconds);
            var now = DateTime.Now.ToUniversalTime();

            var timeSpan = moonTime.Subtract(now);

            return moonTime.ToString("h:mm tt UTC") + string.Format(" {0} Hour(s) and {1} Minute(s) Away", timeSpan.Hours, timeSpan.Minutes);
        }

        //Gets seconds since beginning of time
        public double GetCurrentTime()
        {
            var time = new TimeSpan(DateTime.Now.ToUniversalTime().Ticks);
            return Math.Floor(time.TotalSeconds+150);
        }
        
        #endregion
    }
}
