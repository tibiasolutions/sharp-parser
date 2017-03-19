using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Web;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TibiaSolutions.SharpParser
{
    public class Player
    {
        public Dictionary<string, dynamic> Data;

        public Player(string name)
        {
            Data = new Dictionary<string, dynamic>();
            Data.Add("exists", false);

            name = name.Trim().Replace(" ", "+");
            var url = $"https://secure.tibia.com/community/?subtopic=characters&name={name}";

            var web = new HtmlWeb();
            var document = web.Load(url);
            if (document.DocumentNode != null)
            {
                var info = document.DocumentNode.SelectNodes("//div[@class=\"BoxContent\"]/table[1]/tr[position() > 1]");
                if (info != null && !info.Nodes().First().InnerText.Contains("does not exist"))
                {
                    Data["exists"] = true;

                    foreach (var row in info)
                    {
                        var key = row.FirstChild.InnerText.Trim().Replace(":", "").Replace(" ", "_").ToLower();
                        if (key.Contains("guild"))
                            key = "guild";
                        else if (key.Contains("status"))
                            key = "account_status";

                        var stringWriter = new StringWriter();
                        var value = row.LastChild.InnerText.Trim();
                        HttpUtility.HtmlDecode(value, stringWriter);
                        value = stringWriter.ToString();

                        int numeric;
                        if (int.TryParse(value, out numeric))
                            Data.Add(key, numeric);
                        else
                            Data.Add(key, value);
                    }

                    if (Data.ContainsKey("name"))
                    {
                        var delimiters = new string[] { ", will " };
                        var explode = ((string)Data["name"]).Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                        Data["name"] = explode[0];
                    }

                    if (Data.ContainsKey("former_names"))
                    {
                        Data["former_names"] = string.Join(",", ((string)Data["former_names"]).Split(',').Select(s => s.Trim()).ToArray());
                    }

                    if (Data.ContainsKey("house"))
                    {
                        var delimiters = new string[] { " is paid until " };
                        var explode = ((string)Data["house"]).Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                        delimiters = new string[] { " (" };
                        var house_explode = explode[0].Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                        var house = new Dictionary<string, string>();
                        house.Add("name", house_explode[0]);
                        house.Add("city", house_explode[1].Replace(")", ""));

                        Data["house"] = house;
                    }

                    if (Data.ContainsKey("guild"))
                    {
                        var delimiters = new string[] { " of the " };
                        var explode = ((string)Data["guild"]).Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                        var guild = new Dictionary<string, string>();
                        guild.Add("rank", explode[0]);
                        guild.Add("name", explode[1]);

                        Data["guild"] = guild;
                    }

                    if (Data.ContainsKey("last_login"))
                    {
                        var date = ((string)Data["last_login"]).Replace("CEST", "").Replace("CET", "").Trim();
                        Data["last_login"] = DateTime.Parse(date);
                    }

                    var achievs = document.DocumentNode.SelectNodes("//b[text()=\"Account Achievements\"]/ancestor::table[1]//tr[position() > 1]");
                    if (achievs != null && !achievs.Nodes().First().InnerText.Contains("There are no achievements set to be displayed for this character."))
                    {
                        var achievements = new List<Tuple<int, string, bool>>();

                        foreach (var row in achievs)
                        {
                            var starsHtml = new HtmlAgilityPack.HtmlDocument();
                            starsHtml.LoadHtml(row.FirstChild.InnerHtml);
                            var starsNode = starsHtml.DocumentNode.SelectNodes("//img");
                            var stars = starsNode.Count();

                            var value = row.LastChild.InnerText.Trim();

                            var secretHtml = new HtmlAgilityPack.HtmlDocument();
                            secretHtml.LoadHtml(row.LastChild.InnerHtml);
                            var secretNodes = secretHtml.DocumentNode.SelectNodes("//img");
                            var secret = secretNodes != null && secretNodes.Count() > 0;

                            var achievement = new Tuple<int, string, bool>(stars, value, secret);
                            achievements.Add(achievement);
                        }

                        Data.Add("achievements", achievements);
                    }

                    var deathList = document.DocumentNode.SelectNodes("//b[text()=\"Character Deaths\"]/ancestor::table[1]//tr[position() > 1]");
                    if (deathList != null)
                    {
                        var deaths = new List<Tuple<int, string[], string[]>>();
                        foreach (var row in deathList)
                        {
                            var dateFormat = row.FirstChild.InnerText;
                            var stringWriter = new StringWriter();
                            HttpUtility.HtmlDecode(dateFormat, stringWriter);
                            dateFormat = stringWriter.ToString().Replace("CEST", "").Replace("CET", "").Trim();
                            var date = DateTime.Parse(dateFormat);

                            var value = row.LastChild.InnerText;
                            stringWriter = new StringWriter();
                            HttpUtility.HtmlDecode(value, stringWriter);
                            value = stringWriter.ToString().Trim();

                            var regex = new Regex(@"(\d+) by (.+)");
                            var match = regex.Match(value).Value.Replace(".", "");

                            var delimiters = new string[] { "Assisted by ", " by " };
                            var explode = match.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                            string[] assistants = new string[] { };
                            string[] killers = new string[] { };

                            var level = int.Parse(explode[0]);
                            if (explode.Count() == 3)
                            {
                                delimiters = new string[] { " and ", ", " };
                                assistants = explode[2].Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                                killers = explode[1].Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                            }
                            else
                            {
                                delimiters = new string[] { " and ", ", " };
                                killers = explode[1].Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                            }

                            var death = new Tuple<int, string[], string[]>(level, killers, assistants);
                            deaths.Add(death);
                        }

                        Data.Add("deaths", deaths);
                    }

                    var accountInfo = document.DocumentNode.SelectNodes("//b[text()=\"Account Information\"]/ancestor::table[1]//tr[position() > 1]");
                    if (accountInfo != null)
                    {
                        var accountInformation = new Dictionary<string, dynamic>();

                        foreach (var row in accountInfo)
                        {
                            var key = row.FirstChild.InnerText.Trim().Replace(":", "").Replace(" ", "_").ToLower();
                            var value = row.LastChild.InnerText;
                            var stringWriter = new StringWriter();
                            HttpUtility.HtmlDecode(value, stringWriter);
                            value = stringWriter.ToString().Trim();

                            if (key == "created")
                            {
                                var date = value.Replace("CEST", "").Replace("CET", "").Trim();
                                accountInformation.Add("created", DateTime.Parse(date));
                            }
                            else
                            {
                                accountInformation.Add(key, value);
                            }
                        }

                        Data.Add("account_information", accountInformation);
                    }
                }
            }
        }
    }
}
