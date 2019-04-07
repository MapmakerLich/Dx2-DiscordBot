using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace Dx2_DiscordBot
{
    public class SkillRetriever : RetrieverBase
    {
        #region Properties

        private static DataTable Skills;

        #endregion

        #region Constructor

        //Constructor
        public SkillRetriever(DiscordSocketClient client) : base(client)
        {
            MainCommand = "!dx2skill";
        }

        #endregion

        #region Overrides

        //Initialization
        public async override Task ReadyAsync()
        {
            Skills = await GetCSV("https://raw.githubusercontent.com/Alenael/Dx2DB/master/csv/SMT Dx2 Database - Skills.csv");
            Skills.PrimaryKey = new DataColumn[] { Skills.Columns["Name"] };
        }

        //Recieve Messages here
        public async override Task MessageReceivedAsync(SocketMessage message, string serverName, ulong channelId)
        {
            //Let Base Update
            await base.MessageReceivedAsync(message, serverName, channelId);
            
            if (message.Content.StartsWith(MainCommand))
            {
                var items = message.Content.Split(MainCommand);
                var dataRow = Skills.Rows.Find(items[1].Trim());
                var skill = LoadSkill(dataRow);
                if (_client.GetChannel(channelId) is IMessageChannel chnl)
                    await chnl.SendMessageAsync("", false, skill.WriteToDiscord());
            }
        }

        //Returns the commands for this Retriever
        public override string GetCommands()
        {
            return "\n\nSkill Commands:" +
            "\n* " + MainCommand + " [Skill Name] - Search's for a skill with the name you provided as [Skill Name]. If nothing is found you will recieve a message back stating Skill was not found.";
        }

        #endregion

        #region Private Methods

        //Loads our Skill from a DataGridRow
        private static Skill LoadSkill(DataRow row)
        {
            var name = row["Name"] is DBNull ? "" : (string)row["Name"];
            
            return new Skill
            {
                Name = name,
                Element = row["Element"] is DBNull ? "" : (string)row["Element"],
                Cost = row["Cost"] is DBNull ? "" : (string)row["Cost"],
                Description = row["Description"] is DBNull ? "" : (string)row["Description"],
                Target = row["Target"] is DBNull ? "" : (string)row["Target"],
                Sp = row["Skill Points"] is DBNull ? "" : (string)row["Skill Points"],
                ExtractExclusive = row["ExtractExclusive"] != null ? false : (bool)row["ExtractExclusive"],
                DuelExclusive = row["DuelExclusive"] != null ? false : (bool)row["DuelExclusive"],
                ExtractTransfer = row["ExtractTransfer"] != null ? false : (bool)row["ExtractTransfer"],
            };
        }

        #endregion
    }
    #region Structs

    //Struct to hold our Skill Data
    public struct Skill
    {
        public string Name;
        public string Element;
        public string Cost;
        public string Description;
        public string Target;
        public string Sp;
        public string LearnedBy;
        public string TransferableFrom;
        public bool ExtractExclusive;
        public bool DuelExclusive;
        public bool ExtractTransfer;

        public Embed WriteToDiscord()
        {
            //Perform some fixes on values before exporting

           Name = DemonRetriever.FixSkillsNamedAsDemons(Name);
            Element = char.ToUpper(Element[0]) + Element.Substring(1);

            if (Sp == "")
                Sp = "-";

            Description = Description.Replace("\\n", "\n");

            var url = "https://dx2wiki.com/index.php/" + Uri.EscapeDataString(Name);
            var thumbnail = "https://teambuilder.dx2wiki.com/Images/Spells/" + Uri.EscapeDataString(Element) + ".png";

            //Generate our embeded message and return it
            var eb = new EmbedBuilder();
            eb.WithTitle(Name);
            eb.AddField("Element: ", Element, true);
            eb.AddField("Cost: ", Cost, true);
            eb.AddField("Target: ", Target, true);
            eb.AddField("Sp: ", Sp, true);
            eb.WithDescription(Description);
            eb.WithUrl(url);
            eb.WithThumbnailUrl(thumbnail);
            return eb.Build();
        }
    }

    #endregion
}
