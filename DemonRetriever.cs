using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dx2_DiscordBot
{
    public class DemonRetriever : RetrieverBase
    {
        #region Properties

        private static List<Demon> Demons;

        private const int LEV_DISTANCE = 1;

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
            var demonsDt = await GetCSV("https://raw.githubusercontent.com/Alenael/Dx2DB/master/csv/SMT Dx2 Database - Demons.csv");
            //demons.PrimaryKey = new DataColumn[] { Demons.Columns["Name"] };

            var tempDemons = new List<Demon>();
            foreach(DataRow row in demonsDt.Rows)            
                tempDemons.Add(LoadDemon(row));            
            Demons = tempDemons;            
        }

        //Recieve Messages here
        public async override Task MessageReceivedAsync(SocketMessage message, string serverName, ulong channelId)
        {
            //Let Base Update
            await base.MessageReceivedAsync(message, serverName, channelId);

            if (message.Content.StartsWith(MainCommand))
            {
                var items = message.Content.Split(MainCommand);

                //Save demon to be searched for
                string searchedDemon = items[1].Trim().Replace("*", "☆").ToLower();

                //var test = Demons.FindAll(d =>                 
                //d.Name.Length >= 3 ?                 
                //Calculators.EditDistance(d.Name.ToLower(), items[1].Trim().Replace("*", "☆").ToLower()) <= 2 :
                //d.Name.ToLower() == items[1].Trim().Replace("*", "☆").ToLower());

                //                await Logger.LogAsync(items[1].Trim().Replace("*", "☆").ToLower() + " found " + test.Name);

                //Calculators.EditDistance(d.Name.ToLower(), items[1].Trim().Replace("*", "☆").ToLower())

                //Try to find demon
                var demon = Demons.Find(d => d.Name.ToLower() == searchedDemon); //.Rows.Find(items[1].Trim().Replace("*", "☆"));

                if (_client.GetChannel(channelId) is IMessageChannel chnl)
                {
                    //If exact demon not found
                    if (demon.Name == null)
                    {
                        //Find all similar demons
                        List<String> similarDemons = getSimilarDemons(searchedDemon, LEV_DISTANCE);

                        //If no similar demons found
                        if (similarDemons.Count == 0)
                        {
                            List<string> demonsStartingWith = new List<string>();

                            demonsStartingWith = findDemonsStartingWith(searchedDemon);
                            
                            if (demonsStartingWith.Count == 1)
                            {
                                demon = Demons.Find(x => x.Name.ToLower() == demonsStartingWith[0].ToLower());
                                if (demon.Name != null)
                                    await chnl.SendMessageAsync("", false, demon.WriteToDiscord());
                            }
                            else if(demonsStartingWith.Count > 1)
                            {
                                string answerString = "Could not find: " + searchedDemon + ". Did you mean: ";

                                foreach (string fuzzyDemon in demonsStartingWith)
                                {
                                    answerString += fuzzyDemon + ", ";
                                }

                                //Remove last space and comma
                                answerString = answerString.Remove(answerString.Length - 2);

                                answerString += "?";

                                await chnl.SendMessageAsync(answerString, false);
                            }
                            else
                            {
                                await chnl.SendMessageAsync("Could not find: " + searchedDemon, false);
                            }
                        }
                        //If exactly 1 demon found, return its Info
                        else if (similarDemons.Count == 1)
                        {
                            //Find exactly this demon
                            demon = Demons.Find(x => x.Name.ToLower() == similarDemons[0].ToLower());
                            if (demon.Name != null)
                                await chnl.SendMessageAsync("", false, demon.WriteToDiscord());
                        }
                        //If similar demons found
                        else
                        {
                            //Build answer string
                            string answerString = "Could not find: " + searchedDemon + ". Did you mean: ";

                            foreach (string fuzzyDemon in similarDemons)
                            {
                                answerString += fuzzyDemon + ", ";
                            }

                            //Remove last space and comma
                            answerString = answerString.Remove(answerString.Length - 2);

                            answerString += "?";

                            await chnl.SendMessageAsync(answerString, false);
                        }
                            
                    }
                    else                    
                        await chnl.SendMessageAsync("", false, demon.WriteToDiscord());                    
                }
            }
        }

        private List<string> findDemonsStartingWith(string searchedDemon)
        {
            List<string> demonSW = new List<string>();

            foreach(Demon demon in Demons)
            {
                if (demon.Name.ToLower().StartsWith(searchedDemon.ToLower()))
                    demonSW.Add(demon.Name);
            }

            return demonSW;
        }

        /// <summary>
        /// Returns List of demons whose name have a Levinshtein Distance of LEV_DISTANCE
        /// </summary>
        /// <param name="searchedDemon">Name of the Demon that is being compared agianst</param>
        /// <returns></returns>
        private List<string> getSimilarDemons(string searchedDemon, int levDist)
        {
            List<string> simDemons = new List<string>();

            foreach(Demon demon in Demons)
            {
                int levDistance = 999;

                try
                {
                    levDistance = LevenshteinDistance.EditDistance(demon.Name.ToLower(), searchedDemon);
                }
                catch (ArgumentNullException e)
                {
                    Logger.LogAsync("ArgumentNullException in getSimilarDemons: " + e.Message);
        
                }

                //Console.WriteLine("LevDistance between : " + demon.Name + " and " + searchedDemon + " : " + levDistance);

                //If only off by levDist characters, add to List
                if (levDistance <= levDist)
                {
                    simDemons.Add(demon.Name);
                }
            }
            
            return simDemons;
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
                foreach (var d in Demons)
                {
                    if (d.Name == newName)
                    {
                        newName = newName + " (Skill)";
                        return newName;
                    }
                }
            }

            return newName;
        }

        //Returns a list of demons that have a skill allows for expandability later on via different sets of data points
        public static Dictionary<string, List<Demon>> GetDemonsWithSkill(string skillName)
        {
            var fixedName = FixSkillsNamedAsDemons(skillName);

            var demons = new Dictionary<string, List<Demon>>();
            
            var transferableDemons = Demons.Where(
                d => d.Skill1 == fixedName ||
                d.GachaP == fixedName ||
                d.GachaR == fixedName ||
                d.GachaY == fixedName ||
                d.GachaT == fixedName).ToList();

            demons.Add("Transferrable", transferableDemons);

            return demons;
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

            var demon = new Demon();

            demon.Name = name;
            demon.Rarity = row["Rarity"] is DBNull ? "" : (string)row["Rarity"];
            demon.Race = row["Race"] is DBNull ? "" : (string)row["Race"];
            demon.Ai = row["Type"] is DBNull ? "" : (string)row["Type"];
            demon.Grade = row["Grade"] is DBNull ? "" : (string)row["Grade"];

            demon.Str = row["6★ Strength"] is DBNull ? 0 : Convert.ToInt32(row["6★ Strength"]);
            demon.Mag = row["6★ Magic"] is DBNull ? 0 : Convert.ToInt32(row["6★ Magic"]);
            demon.Vit = row["6★ Vitality"] is DBNull ? 0 : Convert.ToInt32(row["6★ Vitality"]);
            demon.Agi = row["6★ Agility"] is DBNull ? 0 : Convert.ToInt32(row["6★ Agility"]);
            demon.Luck = row["6★ Luck"] is DBNull ? 0 : Convert.ToInt32(row["6★ Luck"]);

            demon.DemonVersions = demonVersions;

            demon.Fire = LoadResist(row["Fire"] is DBNull ? "" : (string)row["Fire"]);
            demon.Dark = LoadResist(row["Dark"] is DBNull ? "" : (string)row["Dark"]);
            demon.Light = LoadResist(row["Light"] is DBNull ? "" : (string)row["Light"]);
            demon.Elec = LoadResist(row["Elec"] is DBNull ? "" : (string)row["Elec"]);
            demon.Ice = LoadResist(row["Ice"] is DBNull ? "" : (string)row["Ice"]);
            demon.Force = LoadResist(row["Force"] is DBNull ? "" : (string)row["Force"]);
            demon.Phys = LoadResist(row["Phys"] is DBNull ? "" : (string)row["Phys"]);

            demon.Skill1 = row["Skill 1"] is DBNull ? "" : (string)row["Skill 1"];
            demon.Skill2 = row["Skill 2"] is DBNull ? "" : (string)row["Skill 2"];
            demon.Skill3 = row["Skill 3"] is DBNull ? "" : (string)row["Skill 3"];

            demon.AwakenC = row["Clear Awaken"] is DBNull ? "" : (string)row["Clear Awaken"];
            demon.AwakenR = row["Red Awaken"] is DBNull ? "" : (string)row["Red Awaken"];
            demon.AwakenP = row["Purple Awaken"] is DBNull ? "" : (string)row["Purple Awaken"];
            demon.AwakenY = row["Yellow Awaken"] is DBNull ? "" : (string)row["Yellow Awaken"];
            demon.AwakenT = row["Teal Awaken"] is DBNull ? "" : (string)row["Teal Awaken"];

            demon.GachaR = row["Red Gacha"] is DBNull ? "" : (string)row["Red Gacha"];
            demon.GachaP = row["Purple Gacha"] is DBNull ? "" : (string)row["Purple Gacha"];
            demon.GachaY = row["Yellow Gacha"] is DBNull ? "" : (string)row["Yellow Gacha"];
            demon.GachaT = row["Teal Gacha"] is DBNull ? "" : (string)row["Teal Gacha"];

            demon.Panel1 = row["Panel 1"] is DBNull ? "" : (string)row["Panel 1"];
            demon.Panel2 = row["Panel 2"] is DBNull ? "" : (string)row["Panel 2"];
            demon.Panel3 = row["Panel 3"] is DBNull ? "" : (string)row["Panel 3"];

            demon.Panel1Stats = row["Panel 1 Stats"] is DBNull ? "" : (string)row["Panel 1 Stats"];
            demon.Panel2Stats = row["Panel 2 Stats"] is DBNull ? "" : (string)row["Panel 2 Stats"];
            demon.Panel3Stats = row["Panel 3 Stats"] is DBNull ? "" : (string)row["Panel 3 Stats"];

            return demon;
        }

        //Returns a value based on what its passed
        private static string LoadResist(string value)
        {
            if (value == "" || value == null)
                return "-";

            return value.First().ToString().ToUpper() + value.Substring(1);
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

        public string Panel1;
        public string Panel2;
        public string Panel3;

        public string Panel1Stats;
        public string Panel2Stats;
        public string Panel3Stats;

        public Embed WriteToDiscord()
        {
            var url = "https://dx2wiki.com/index.php/" + Uri.EscapeDataString(Name);
            var thumbnail = "https://teambuilder.dx2wiki.com/Images/Demons/" + Uri.EscapeDataString(Name.Replace("☆", "")) + ".jpg";
            
            var eb = new EmbedBuilder();
            eb.WithTitle(Name);

            var resist = "";

            //if (Phys != "")
                resist += "Phys: " + Phys + " ";

            //if (Fire != "")
                resist += " | Fire: " + Fire + " ";

            //if (Ice != "")
                resist += " | Ice: " + Ice + " ";

            //if (Elec != "")
                resist += " | Elec: " + Elec + " ";

            //if (Force != "")
                resist += " | Force: " + Force + " ";

            //if (Light != "")
                resist += " | Light: " + Light + " ";

            //if (Dark != "")
                resist += " | Dark: " + Dark;

            //if (resist.Length > 0)
                //resist = resist.Remove(0, 3);

            var panelInfo1 = "";
            var panelInfo2 = "";
            var panelInfo3 = "";

            if (Panel1 != "")
                panelInfo1 = "1: " + Panel1 + " " + Panel1Stats + "\n";

            if (Panel2 != "")
                panelInfo2 = "2: " + Panel2 + " " + Panel2Stats + "\n";

            if (Panel3 != "")
                panelInfo3 = "3: " + Panel3 + " " + Panel3Stats + "\n\n";

            var clear = "Clear: " + GenerateWikiLink(AwakenC) + "\n";
            var red = "Red: " + GenerateWikiLink(AwakenR) + " | " + GenerateWikiLink(GachaR) + "\n";
            var yellow = "Yellow: " + GenerateWikiLink(AwakenY) + " | " + GenerateWikiLink(GachaY) + "\n";
            var teal = "Teal: " + GenerateWikiLink(AwakenT) + " | " + GenerateWikiLink(GachaT) + "\n";
            var purple = "Purple: " + GenerateWikiLink(AwakenP) + " | " + GenerateWikiLink(GachaP) + "\n\n";
            
            if (red.EndsWith(" | \n"))
                red = red.Replace(" | ", "");

            if (yellow.EndsWith(" | \n"))
                yellow = yellow.Replace(" | ", "");

            if (teal.EndsWith(" | \n"))
                teal = teal.Replace(" | ", "");

            if (purple.EndsWith(" | \n\n"))
                purple = purple.Replace(" | ", "");

            var skill3 = "";

            if (Skill3 != "")
                skill3 = GenerateWikiLink(Skill3) + "\n";

            eb.WithDescription("[Lore..](https://dx2wiki.com/index.php/" + Uri.EscapeUriString(Name) + "/Lore)");

            eb.AddField("Skills:",
                 GenerateWikiLink(Skill1) + "\n" +
                 GenerateWikiLink(Skill2) + "\n" +
                 skill3 + "\n" +
                 clear +
                 red +
                 yellow +
                 teal +
                 purple, false);
            eb.AddField("Resists", resist, false);

            if (panelInfo1 != "")
                eb.AddField("Panels", panelInfo1 + panelInfo2 + panelInfo3, true);

            //Other Info
            eb.AddField("Race", Race, true);
            eb.AddField("Grade", Grade, true);
            eb.AddField("Rarity", Rarity, true);
            eb.AddField("Ai", Ai, true);

            //Stats
            eb.AddField("HP", HP, true);
            eb.AddField("Str", Str + " (PAtk: " + PAtk + ")", true);
            eb.AddField("Mag", Mag + " (MAtk: " + MAtk + ")", true);
            eb.AddField("Vit", Vit + " (PDef: " + PDef + " MDef: " + MDef + ")", true);
            eb.AddField("Agi", Agi, true);
            eb.AddField("Luck", Luck, true);    
            

            eb.WithUrl(url);
            eb.WithThumbnailUrl(thumbnail);
            return eb.Build();
        }

        private string GenerateWikiLink(string demon)
        {
            var newDemon = DemonRetriever.FixSkillsNamedAsDemons(demon);

            if (newDemon == "")
                return "";

            newDemon = "[" + newDemon + "](https://dx2wiki.com/index.php/" + Uri.EscapeUriString(newDemon) + ")";
            return newDemon;
        }
    }

    #endregion
}

