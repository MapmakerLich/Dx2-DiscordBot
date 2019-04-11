using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace Dx2_DiscordBot
{
    public class DemonRetriever : RetrieverBase
    {
        #region Properties

        private static DataTable Demons;
        
        #endregion

        #region Constructor

        //Constructor
        public DemonRetriever(DiscordSocketClient client) : base(client)
        {
            MainCommand = "!dx2demon";
        }

        #endregion

        #region Overrides

        //Initialization
        public async override Task ReadyAsync()
        {
            Demons = await GetCSV("https://raw.githubusercontent.com/Alenael/Dx2DB/master/csv/SMT Dx2 Database - Demons.csv");
            Demons.PrimaryKey = new DataColumn[] { Demons.Columns["Name"] };
        }

        //Recieve Messages here
        public async override Task MessageReceivedAsync(SocketMessage message, string serverName, ulong channelId)
        {
            //Let Base Update
            await base.MessageReceivedAsync(message, serverName, channelId);

            if (message.Content.StartsWith(MainCommand))
            {
                var items = message.Content.Split(MainCommand);
                var dataRow = Demons.Rows.Find(items[1].Trim().Replace("*", "☆"));
                if (_client.GetChannel(channelId) is IMessageChannel chnl)
                {
                    if (dataRow == null)
                        await chnl.SendMessageAsync("Could not find: " + items[1].Trim().Replace("*", "☆"), false);
                    else
                    {
                        var demon = LoadDemon(dataRow);
                        await chnl.SendMessageAsync("", false, demon.WriteToDiscord());
                    }
                }
            }
        }

        //Returns the commands for this Retriever
        public override string GetCommands()
        {
            return "\n\nDemon Commands:" +
            "\n* " + MainCommand + " [Demon Name] - Search's for a demon with the name you provided as [Demon Name]. If nothing is found you will recieve a message back stating Demon was not found. Alternate demons can be found like.. Shiva A, Nekomata A. ☆ can be interperted as * when performing searches like... Nero*, V*";
        }

        #endregion

        #region Public Methods

        //Fixes an issue where skills can be the same name of a demon at times
        public static string FixSkillsNamedAsDemons(string name)
        {
            var newName = name;

            if (newName != "")
            {
                foreach (DataRow item in Demons.Rows as DataRowCollection)
                {
                    if ((string)item["Name"] == newName)
                    {
                        newName = newName + " (Skill)";
                        return newName;
                    }
                }
            }

            return newName;
        }

        //Creates a demon object from a data grid view row
        private static Demon LoadDemon(DataRow row)
        {
            var demonVersions = "";
            var name = row["Name"] is DBNull ? "" : (string)row["Name"];

            if (!(row["Alternate Name"] is DBNull))
            {
                var alternateDemon = (string)row["Alternate Name"];
                demonVersions = "{{DemonVersions|" + name + "|" + alternateDemon + "}}\r\n";
            }

            return new Demon
            {
                Name = name,
                Rarity = row["Rarity"] is DBNull ? "" : (string)row["Rarity"],
                Race = row["Race"] is DBNull ? "" : (string)row["Race"],
                Ai = row["Type"] is DBNull ? "" : (string)row["Type"],
                Grade = row["Grade"] is DBNull ? "" : (string)row["Grade"],

                Str = row["6★ Strength"] is DBNull ? 0 : Convert.ToInt32(row["6★ Strength"]),
                Mag = row["6★ Magic"] is DBNull ? 0 : Convert.ToInt32(row["6★ Magic"]),
                Vit = row["6★ Vitality"] is DBNull ? 0 : Convert.ToInt32(row["6★ Vitality"]),
                Agi = row["6★ Agility"] is DBNull ? 0 : Convert.ToInt32(row["6★ Agility"]),
                Luck = row["6★ Luck"] is DBNull ? 0 : Convert.ToInt32(row["6★ Luck"]),

                DemonVersions = demonVersions,

                Fire = LoadResist(row["Fire"] is DBNull ? "" : (string)row["Fire"]),
                Dark = LoadResist(row["Dark"] is DBNull ? "" : (string)row["Dark"]),
                Light = LoadResist(row["Light"] is DBNull ? "" : (string)row["Light"]),
                Elec = LoadResist(row["Elec"] is DBNull ? "" : (string)row["Elec"]),
                Ice = LoadResist(row["Ice"] is DBNull ? "" : (string)row["Ice"]),
                Force = LoadResist(row["Force"] is DBNull ? "" : (string)row["Force"]),
                Phys = LoadResist(row["Phys"] is DBNull ? "" : (string)row["Phys"]),

                Skill1 = row["Skill 1"] is DBNull ? "" : (string)row["Skill 1"],
                Skill2 = row["Skill 2"] is DBNull ? "" : (string)row["Skill 2"],
                Skill3 = row["Skill 3"] is DBNull ? "" : (string)row["Skill 3"],

                AwakenC = row["Clear Awaken"] is DBNull ? "" : (string)row["Clear Awaken"],
                AwakenR = row["Red Awaken"] is DBNull ? "" : (string)row["Red Awaken"],
                AwakenP = row["Purple Awaken"] is DBNull ? "" : (string)row["Purple Awaken"],
                AwakenY = row["Yellow Awaken"] is DBNull ? "" : (string)row["Yellow Awaken"],
                AwakenT = row["Teal Awaken"] is DBNull ? "" : (string)row["Teal Awaken"],

                GachaR = row["Red Gacha"] is DBNull ? "" : (string)row["Red Gacha"],
                GachaP = row["Purple Gacha"] is DBNull ? "" : (string)row["Purple Gacha"],
                GachaY = row["Yellow Gacha"] is DBNull ? "" : (string)row["Yellow Gacha"],
                GachaT = row["Teal Gacha"] is DBNull ? "" : (string)row["Teal Gacha"],
            };
        }

        //Returns a value based on what its passed
        private static string LoadResist(string value)
        {
            if (value == "" || value == null)
                return "  -   ";
            else
            {
                var type = "";

                switch (value)
                {
                    case "rs":
                        type = "Resist";
                        break;
                    case "rp":
                        type = "Repel";
                        break;
                    case "wk":
                        type = "Weak";
                        break;
                    case "nu":
                        type = "<Null>";
                        break;
                    case "ab":
                        type = "<Drain>";
                        break;
                }

                return type;
            }
        }

        #endregion
    }
    #region Structs

    //Object to hold Demon Data
    public struct Demon
    {
        public string Name;
        public string Race;
        public string Grade;
        public string Rarity;
        public string Ai;
        public int Str;
        public int Mag;
        public int Vit;
        public int Agi;
        public int Luck;
        public string Phys;
        public string Fire;
        public string Ice;
        public string Elec;
        public string Force;
        public string Light;
        public string Dark;
        public int HP { get { return (int)(Vit * 4.7 + 50 * 7.4); } }
        public int PAtk { get { return (int)(Str * 2.1 + 50 * 5.6 + 50); } }
        public int MAtk { get { return (int)(Mag * 2.1 + 50 * 5.6 + 50); } }
        public int PDef { get { return (int)(Vit * 1.1 + Str * 0.5 + 50 * 5.6 + 50); } }
        public int MDef { get { return (int)(Vit * 1.1 + Mag * 0.5 + 50 * 5.6 + 50); } }
        public string DemonVersions;

        public string Skill1;
        public string Skill2;
        public string Skill3;

        public string AwakenC;
        public string AwakenR;
        public string AwakenT;
        public string AwakenP;
        public string AwakenY;

        public string GachaR;
        public string GachaP;
        public string GachaT;
        public string GachaY;

        public string Awaken1;
        public string Awaken2;
        public string Awaken3;
        public string Awaken4;

        public string Awaken1Amount;
        public string Awaken2Amount;
        public string Awaken3Amount;
        public string Awaken4Amount;

        public Embed WriteToDiscord()
        {
            var url = "https://dx2wiki.com/index.php/" + Uri.EscapeDataString(Name);
            var thumbnail = "https://teambuilder.dx2wiki.com/Images/Demons/" + Uri.EscapeDataString(Name.Replace("☆", "")) + ".jpg";
            
            var eb = new EmbedBuilder();
            eb.WithTitle(Name);

            eb.WithDescription(        
                "Skills:" + "\n" +
                "Skill 1: " + GenerateWikiLink(Skill1) + "\n" +
                "Skill 2: " + GenerateWikiLink(Skill2) + "\n" +
                "Skill 3: " + GenerateWikiLink(Skill3) + "\n\n" +
                "Clear: " + GenerateWikiLink(AwakenC) + "\n" +
                "Red: " + GenerateWikiLink(AwakenR) + " | " + GenerateWikiLink(GachaR) + "\n" +
                "Yellow: " + GenerateWikiLink(AwakenY) + " | " + GenerateWikiLink(GachaY) + "\n" +
                "Teal: " + GenerateWikiLink(AwakenT) + " | " + GenerateWikiLink(GachaT) + "\n" +
                "Purple: " + GenerateWikiLink(AwakenP) + " | " + GenerateWikiLink(GachaP) + "\n\n");

            //Other Info
            eb.AddField("Race", Race, true);
            eb.AddField("Grade", Grade, true);
            eb.AddField("Rarity", Rarity, true);
            eb.AddField("Ai", Ai, true);

            //Stats
            eb.AddField("Str", Str, true);
            eb.AddField("Mag", Mag, true);
            eb.AddField("Vit", Vit, true);
            eb.AddField("Agi", Agi, true);
            eb.AddField("Luck", Luck, true);

            eb.WithUrl(url);
            eb.WithThumbnailUrl(thumbnail);
            return eb.Build();
        }

        private string GenerateWikiLink(string skill)
        {
            var newSkill = DemonRetriever.FixSkillsNamedAsDemons(skill);
            newSkill = "[" + newSkill + "](https://dx2wiki.com/index.php/" + Uri.EscapeUriString(newSkill) + ")";
            return newSkill;
        }
    }

    #endregion
}

