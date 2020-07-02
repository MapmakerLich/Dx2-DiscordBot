using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Dx2_DiscordBot
{
    public class ResistsRetriever : RetrieverBase
    {
        #region Properties

        public static List<Demon> Demons;

        #endregion

        #region Constructor

        public ResistsRetriever(DiscordSocketClient client) : base(client)
        {
            MainCommand = "!dx2resist";
        }

        #endregion

        #region Overrides

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override string GetCommands()
        {
            return "\n\nResist Commands:" +
               "\n* " + MainCommand + " [Resist Type] [Element] - Returns a list of demons who have the matching type. Accepted [Resist Type]'s: Resist, Null, Repel, Drain, Weak. Acepted [Element]'s: Phys, Fire, Ice, Elec, Force, Light, Dark. Actual command example: !dx2resist Null Phys, !dx2resist Repel Fire, !dx2resist Drain Force, etc.";            
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public async override Task MessageReceivedAsync(SocketMessage message, string serverName, ulong channelId)
        {  
            //Let Base Update
            await base.MessageReceivedAsync(message, serverName, channelId);

            if (message.Content.StartsWith(MainCommand))
            {
                if (_client.GetChannel(channelId) is IMessageChannel chnl)
                {
                    var items = message.Content.Split(MainCommand + " ");

                    if (items.Length == 2)
                    {
                        var data = items[1].Split(" ");

                        if (data.Length == 2)
                        { 
                            if (SoftScanWords(data[0], data[1]))
                                await chnl.SendMessageAsync("", false, GetElementsOfType(data[0], data[1]));
                            else if(SoftScanWords(data[1], data[0]))
                                await chnl.SendMessageAsync("", false, GetElementsOfType(data[1], data[0]));
                        }
                        else
                            await chnl.SendMessageAsync("Could not parse request or incorrect commands were provided. Check !dx2help if you need more assistance.");
                    }
                }
            }
        }

        public bool SoftScanWords(String type, String element)
        {
            if (type.ToLower() == "null" || type.ToLower() == "resist" || type.ToLower() == "repel" || type.ToLower() == "drain" || type.ToLower() == "weak")
                if (element.ToLower() == "phys" || element.ToLower() == "fire" || element.ToLower() == "ice"
                     || element.ToLower() == "elec" || element.ToLower() == "force" || element.ToLower() == "light" || element.ToLower() == "dark")
                return true;

            return false;
        }


        public Embed GetElementsOfType(String type, String element)
        {
            var eb = new EmbedBuilder();

            //Variables to hold our data
            var stockList = "";
            var awakendList = "";
            var clearList = "";
            var redList = "";
            var tealList = "";
            var purpleList = "";
            var yellowList = "";
            var gachaList = "";

            var skill = char.ToUpper(type[0]) + type.Substring(1).ToLower() + " "
                + char.ToUpper(element[0]) + element.Substring(1).ToLower();

            eb.WithTitle(skill);

            //Generate our data
            foreach (var d in Demons)
            {
                var dLink = "";
                //Generate our link
                if (d.Rarity == "4" || d.Rarity == "5")
                    dLink = $"**{d.Name}**, ";
                else
                    dLink =  $"{d.Name}, ";

                //Check if demon has resist naturally
                stockList += CheckType(d, type, element);
                
                //Check each skill and if it matches add demont to the list and move on
                if (d.Skill1 == skill || d.Skill2 == skill || d.Skill3 == skill)
                    if (!stockList.Contains(dLink))
                        stockList += dLink;
                if (d.AwakenC == skill)
                    clearList += dLink;
                if (d.AwakenR == skill)
                    redList += dLink;
                if (d.AwakenT == skill)
                    tealList += dLink;
                if (d.AwakenP == skill)
                    purpleList += dLink;
                if (d.AwakenY == skill)
                    yellowList += dLink;
                if (d.GachaP == skill || d.GachaR == skill || d.GachaY == skill || d.GachaT == skill)
                    gachaList += dLink;
            }

            if (stockList != "")
                eb.AddField("Stock", stockList.Trim(' ').Trim(','));
            if (awakendList != "")
                eb.AddField("Awakened", awakendList.Trim(' ').Trim(','));
            if (clearList != "")
                eb.AddField("Clear Awakened", clearList.Trim(' ').Trim(','));
            if (redList != "")
                eb.AddField("Red Awakend", redList.Trim(' ').Trim(','));
            if (tealList != "")
                eb.AddField("Teal Awakened", tealList.Trim(' ').Trim(','));
            if (purpleList != "")
                eb.AddField("Purple Awakened", purpleList.Trim(' ').Trim(','));
            if (yellowList != "")
                eb.AddField("Yellow Awakened", yellowList.Trim(' ').Trim(','));
            if (gachaList != "")
                eb.AddField("Gacha", gachaList.Trim(' ').Trim(','));

            return eb.Build();
        }

        public string CheckType(Demon d, string type, string element)
        {
            var demonsList = "";
            var newType = TypeTranslator(type);

            if (newType != "")
            {
                var demonLink = d.Name + ", ";

                if (d.Phys == newType && element.ToLower() == "phys")
                    demonsList += demonLink;

                if (d.Fire == newType && element.ToLower() == "fire")
                    demonsList += demonLink;

                if (d.Ice == newType && element.ToLower() == "ice")
                    demonsList += demonLink;

                if (d.Elec == newType && element.ToLower() == "elec")
                    demonsList += demonLink;

                if (d.Force == newType && element.ToLower() == "force")
                    demonsList += demonLink;

                if (d.Light == newType && element.ToLower() == "light")
                    demonsList += demonLink;

                if (d.Dark == newType && element.ToLower() == "dark")
                    demonsList += demonLink;
            }

            return demonsList;
        }

        public string TypeTranslator(string type)
        {
            var newType = "";

            switch (type.ToLower())
            {
                case "null":
                    return "Nu";
                case "resist":
                    return "Rs";
                case "drain":                
                    return "Ab";
                case "repel":
                    return "Rp";
                case "weak":
                    return "Wk";
            }

            return newType;
        }

        public async override Task ReadyAsync()
        {
            var demonsDt = await GetCSV("https://raw.githubusercontent.com/Alenael/Dx2DB/master/csv/SMT Dx2 Database - Demons.csv");

            var tempDemons = new List<Demon>();
            foreach (DataRow row in demonsDt.Rows)
                tempDemons.Add(DemonRetriever.LoadDemon(row));
            Demons = tempDemons;
        }

        public override string ToString()
        {
            return base.ToString();
        }

        #endregion
    }
}
