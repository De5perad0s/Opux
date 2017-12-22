﻿using ByteSizeLib;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Opux.JsonClasses;

namespace Opux
{
    internal class Functions
    {
        internal static bool avaliable = false;
        internal static bool running = false;
        static readonly HttpClient _httpClient = new HttpClient();

        //Timer is setup here
        #region Timer stuff
        public async static void RunTick(Object stateInfo)
        {
            try
            {
                if (!running && avaliable)
                {
                    running = true;
                    Async_Tick(stateInfo).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                await Client_Log(new LogMessage(LogSeverity.Error, "Aync_Tick", ex.Message, ex));
            }
        }

        private async static Task Async_Tick(object args)
        {
            try
            {
                running = false;
            }
            catch (Exception ex)
            {
                await Client_Log(new LogMessage(LogSeverity.Error, "Aync_Tick", ex.Message, ex));
                running = false;
            }
        }
        #endregion

        //Needs logging to a file added
        #region Logger
        internal async static Task Client_Log(LogMessage arg)
        {
            try
            {

                var path = Path.Combine(AppContext.BaseDirectory, "logs");
                var file = Path.Combine(path, $"{arg.Source}.log");

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                if (!File.Exists(file))
                {
                    File.Create(file);
                }

                var cc = Console.ForegroundColor;

                switch (arg.Severity)
                {
                    case LogSeverity.Critical:
                    case LogSeverity.Error:
                        Console.ForegroundColor = ConsoleColor.Red;

                        break;
                    case LogSeverity.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LogSeverity.Info:
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case LogSeverity.Verbose:
                    case LogSeverity.Debug:
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        break;
                }

                using (StreamWriter logFile = new StreamWriter(File.Open(file, FileMode.Append, FileAccess.Write, FileShare.Write), Encoding.UTF8))
                {
                    await logFile.WriteLineAsync($"{DateTime.Now,-19} [{arg.Severity,8}]: {arg.Message}");
                }

                Console.WriteLine($"{DateTime.Now,-19} [{arg.Severity,8}] [{arg.Source}]: {arg.Message}");
                if (arg.Exception != null)
                {
                    Console.WriteLine(arg.Exception?.StackTrace);
                }
                Console.ForegroundColor = cc;
                await Task.CompletedTask;
            }
            catch { }
        }
        #endregion

        //Events are attached here
        #region EVENTS
        internal async static Task Event_Ready()
        {
            avaliable = true;
            var user = (ISelfUser) Program.Client.CurrentUser;
            await user.ModifyAsync(x => x.Username = Program.Settings.GetSection("config")["name"]);
            await Task.CompletedTask;
        }

        internal static Task Event_Disconnected(Exception arg)
        {
            avaliable = false;
            return Task.CompletedTask;
        }

        internal static Task Event_Connected()
        {
            return Task.CompletedTask;
        }

        internal static Task Event_LoggedIn()
        {
            return Task.CompletedTask;
        }

        internal static Task Event_LoggedOut()
        {
            avaliable = false;
            return Task.CompletedTask;
        }
        #endregion

        //Complete
        #region Pricecheck
        internal async static Task PriceCheck(ICommandContext context, string String, string system)
        {
            var channel = context.Message.Channel;

            try
            {
                HttpResponseMessage ItemID = new HttpResponseMessage();
                if (String.EndsWith('*'))
                {
                    ItemID = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/search/?categories=inventory_type&datasource=tranquility&language=en-us&search={String.TrimEnd('*')}&strict=true");
                }
                else

                {
                    ItemID = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/search/?categories=inventory_type&datasource=tranquility&language=en-us&search={String}&strict=false");
                }

                if (!ItemID.IsSuccessStatusCode)
                {
                    await channel.SendMessageAsync($"{context.Message.Author.Mention} ESI Failure ItemID please try again later");
                    await Task.CompletedTask;
                }


                var ItemIDResult = await ItemID.Content.ReadAsStringAsync();

                var ItemIDResults = JsonConvert.DeserializeObject<SearchInventoryType>(ItemIDResult);

                if (ItemIDResults.inventory_type == null || string.IsNullOrWhiteSpace(ItemIDResults.inventory_type.ToString()))
                {
                    await channel.SendMessageAsync($"{context.Message.Author.Mention} Item {String} does not exist please try again");
                }
                else if (ItemIDResults.inventory_type.Count() > 1)
                {
                    await channel.SendMessageAsync("Multiple results found see DM");

                    channel = await context.Message.Author.GetOrCreateDMChannelAsync();

                    var tmp = JsonConvert.SerializeObject(ItemIDResults.inventory_type);
                    var httpContent = new StringContent(tmp);

                    var ItemName = await Program._httpClient.PostAsync($"https://esi.tech.ccp.is/latest/universe/names/?datasource=tranquility", httpContent);

                    if (!ItemName.IsSuccessStatusCode)
                    {
                        await channel.SendMessageAsync($"{context.Message.Author.Mention} ESI Failure ItemName please try again later");
                        await Task.CompletedTask;
                    }

                    var ItemNameResult = await ItemName.Content.ReadAsStringAsync();

                    var ItemNameResults = JsonConvert.DeserializeObject<List<SearchName>>(ItemNameResult);

                    await Client_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));
                    var builder = new EmbedBuilder()
                        .WithColor(new Color(0x00D000))
                        .WithAuthor(author =>
                        {
                            author
                                .WithName($"Multiple Items found use * for exact search:")
                                .WithIconUrl("https://just4dns2.co.uk/shipexplosion.png");
                        })
                        .WithDescription("Example: Hyperion*");
                    var count = 0;
                    foreach (var inventory_type in ItemIDResults.inventory_type)
                    {
                        if (count < 25)
                        {
                            builder.AddField($"{ItemNameResults.FirstOrDefault(x => x.id == inventory_type).name}", "\u200b");
                        }
                        else
                        {
                            var embed2 = builder.Build();

                            await channel.SendMessageAsync($"", false, embed2).ConfigureAwait(false);

                            builder.Fields.Clear();
                            count = 0;
                        }
                        count++;
                    }

                    var embed = builder.Build();

                    await channel.SendMessageAsync($"", false, embed).ConfigureAwait(false);
                }
                else
                {
                    try
                    {
                        var httpContent = new StringContent($"[ { ItemIDResults.inventory_type[0] } ]");

                        var ItemName = await Program._httpClient.PostAsync($"https://esi.tech.ccp.is/latest/universe/names/?datasource=tranquility", httpContent);

                        if (!ItemName.IsSuccessStatusCode)
                        {
                            await channel.SendMessageAsync($"{context.Message.Author.Mention} ESI Failure ItemName please try again later");
                            await Task.CompletedTask;
                        }

                        var ItemNameResult = await ItemName.Content.ReadAsStringAsync();

                        var ItemNameResults = JsonConvert.DeserializeObject<List<SearchName>>(ItemNameResult)[0];

                        var url = "https://api.evemarketer.com/ec";
                        if (system == "")
                        {

                            var eveCentralReply = await Program._httpClient.GetStringAsync($"{url}/marketstat/json?typeid={ItemIDResults.inventory_type[0]}");
                            var centralreply = JsonConvert.DeserializeObject<List<Items>>(eveCentralReply)[0];

                            await Client_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));
                            var builder = new EmbedBuilder()
                                .WithColor(new Color(0x00D000))
                                .WithThumbnailUrl($"https://image.eveonline.com/Type/{ItemNameResults.id}_64.png")
                                .WithAuthor(author =>
                                {
                                    author
                                        .WithName($"Item: {ItemNameResults.name}")
                                        .WithUrl($"https://www.fuzzwork.co.uk/info/?typeid={ItemNameResults.id}/")
                                        .WithIconUrl("https://just4dns2.co.uk/shipexplosion.png");
                                })
                                .WithDescription($"Global Prices")
                                .AddInlineField("Buy", $"Low: {centralreply.buy.min.ToString("N2")}{Environment.NewLine}" +
                                $"Avg: {centralreply.buy.avg.ToString("N2")}{Environment.NewLine}" +
                                $"High: {centralreply.buy.max.ToString("N2")}")
                                .AddInlineField("Sell", $"Low: {centralreply.sell.min.ToString("N2")}{Environment.NewLine}" +
                                $"Avg: {centralreply.sell.avg.ToString("N2")}{Environment.NewLine}" +
                                $"High: {centralreply.sell.max.ToString("N2")}")
                                .AddField($"Extra Data", $"\u200b")
                                .AddInlineField("Buy", $"5%: {centralreply.buy.fivePercent.ToString("N2")}{Environment.NewLine}" +
                                $"Volume: {centralreply.buy.volume}")
                                .AddInlineField("Sell", $"5%: {centralreply.sell.fivePercent.ToString("N2")}{Environment.NewLine}" +
                                $"Volume: {centralreply.sell.volume.ToString("N0")}");
                            var embed = builder.Build();

                            await channel.SendMessageAsync($"", false, embed).ConfigureAwait(false);
                        }
                        if (system == "jita")
                        {
                            var eveCentralReply = await Program._httpClient.GetStringAsync($"{url}/marketstat/json?typeid={ItemIDResults.inventory_type[0]}&usesystem=30000142");
                            var centralreply = JsonConvert.DeserializeObject<List<Items>>(eveCentralReply)[0];

                            await Client_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));

                            var builder = new EmbedBuilder()
                                .WithColor(new Color(0x00D000))
                                .WithThumbnailUrl($"https://image.eveonline.com/Type/{ItemNameResults.id}_64.png")
                                .WithAuthor(author =>
                                {
                                    author
                                        .WithName($"Item: {ItemNameResults.name}")
                                        .WithUrl($"https://www.fuzzwork.co.uk/info/?typeid={ItemNameResults.id}/")
                                        .WithIconUrl("https://just4dns2.co.uk/shipexplosion.png");
                                })
                                .WithDescription($"Prices from Jita")
                                .AddInlineField("Buy", $"Low: {centralreply.buy.min.ToString("N2")}{Environment.NewLine}" +
                                $"Avg: {centralreply.buy.avg.ToString("N2")}{Environment.NewLine}" +
                                $"High: {centralreply.buy.max.ToString("N2")}")
                                .AddInlineField("Sell", $"Low: {centralreply.sell.min.ToString("N2")}{Environment.NewLine}" +
                                $"Avg: {centralreply.sell.avg.ToString("N2")}{Environment.NewLine}" +
                                $"High: {centralreply.sell.max.ToString("N2")}")
                                .AddField($"Extra Data", $"\u200b")
                                .AddInlineField("Buy", $"5%: {centralreply.buy.fivePercent.ToString("N2")}{Environment.NewLine}" +
                                $"Volume: {centralreply.buy.volume}")
                                .AddInlineField("Sell", $"5%: {centralreply.sell.fivePercent.ToString("N2")}{Environment.NewLine}" +
                                $"Volume: {centralreply.sell.volume.ToString("N0")}");
                            var embed = builder.Build();

                            await channel.SendMessageAsync($"", false, embed).ConfigureAwait(false);
                        }
                        if (system == "amarr")
                        {
                            var eveCentralReply = await Program._httpClient.GetStringAsync($"{url}/marketstat/json?typeid={ItemIDResults.inventory_type[0]}&usesystem=30002187");
                            var centralreply = JsonConvert.DeserializeObject<List<Items>>(eveCentralReply)[0];

                            await Client_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));

                            var builder = new EmbedBuilder()
                                .WithColor(new Color(0x00D000))
                                .WithThumbnailUrl($"https://image.eveonline.com/Type/{ItemNameResults.id}_64.png")
                                .WithAuthor(author =>
                                {
                                    author
                                        .WithName($"Item: {ItemNameResults.name}")
                                        .WithUrl($"https://www.fuzzwork.co.uk/info/?typeid={ItemNameResults.id}/")
                                        .WithIconUrl("https://just4dns2.co.uk/shipexplosion.png");
                                })
                                .WithDescription($"Prices from Amarr")
                                .AddInlineField("Buy", $"Low: {centralreply.buy.min.ToString("N2")}{Environment.NewLine}" +
                                $"Avg: {centralreply.buy.avg.ToString("N2")}{Environment.NewLine}" +
                                $"High: {centralreply.buy.max.ToString("N2")}")
                                .AddInlineField("Sell", $"Low: {centralreply.sell.min.ToString("N2")}{Environment.NewLine}" +
                                $"Avg: {centralreply.sell.avg.ToString("N2")}{Environment.NewLine}" +
                                $"High: {centralreply.sell.max.ToString("N2")}")
                                .AddField($"Extra Data", $"\u200b")
                                .AddInlineField("Buy", $"5%: {centralreply.buy.fivePercent.ToString("N2")}{Environment.NewLine}" +
                                $"Volume: {centralreply.buy.volume}")
                                .AddInlineField("Sell", $"5%: {centralreply.sell.fivePercent.ToString("N2")}{Environment.NewLine}" +
                                $"Volume: {centralreply.sell.volume.ToString("N0")}");
                            var embed = builder.Build();

                            await channel.SendMessageAsync($"", false, embed).ConfigureAwait(false);
                        }
                        if (system == "rens")
                        {
                            var eveCentralReply = await Program._httpClient.GetStringAsync($"{url}/marketstat/json?typeid={ItemIDResults.inventory_type[0]}&usesystem=30002510");
                            var centralreply = JsonConvert.DeserializeObject<List<Items>>(eveCentralReply)[0];

                            await Client_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));

                            var builder = new EmbedBuilder()
                                .WithColor(new Color(0x00D000))
                                .WithThumbnailUrl($"https://image.eveonline.com/Type/{ItemNameResults.id}_64.png")
                                .WithAuthor(author =>
                                {
                                    author
                                        .WithName($"Item: {ItemNameResults.name}")
                                        .WithUrl($"https://www.fuzzwork.co.uk/info/?typeid={ItemNameResults.id}/")
                                        .WithIconUrl("https://just4dns2.co.uk/shipexplosion.png");
                                })
                                .WithDescription($"Prices from Rens")
                                .AddInlineField("Buy", $"Low: {centralreply.buy.min.ToString("N2")}{Environment.NewLine}" +
                                $"Avg: {centralreply.buy.avg.ToString("N2")}{Environment.NewLine}" +
                                $"High: {centralreply.buy.max.ToString("N2")}")
                                .AddInlineField("Sell", $"Low: {centralreply.sell.min.ToString("N2")}{Environment.NewLine}" +
                                $"Avg: {centralreply.sell.avg.ToString("N2")}{Environment.NewLine}" +
                                $"High: {centralreply.sell.max.ToString("N2")}")
                                .AddField($"Extra Data", $"\u200b")
                                .AddInlineField("Buy", $"5%: {centralreply.buy.fivePercent.ToString("N2")}{Environment.NewLine}" +
                                $"Volume: {centralreply.buy.volume}")
                                .AddInlineField("Sell", $"5%: {centralreply.sell.fivePercent.ToString("N2")}{Environment.NewLine}" +
                                $"Volume: {centralreply.sell.volume.ToString("N0")}");
                            var embed = builder.Build();

                            await channel.SendMessageAsync($"", false, embed).ConfigureAwait(false);
                        }
                        if (system == "dodixie")
                        {
                            var eveCentralReply = await Program._httpClient.GetStringAsync($"{url}/marketstat/json?typeid={ItemIDResults.inventory_type[0]}&usesystem=30002659");
                            var centralreply = JsonConvert.DeserializeObject<List<Items>>(eveCentralReply)[0];

                            await Client_Log(new LogMessage(LogSeverity.Info, "PCheck", $"Sending {context.Message.Author}'s Price check to {channel.Name}"));

                            var builder = new EmbedBuilder()
                                .WithColor(new Color(0x00D000))
                                .WithThumbnailUrl($"https://image.eveonline.com/Type/{ItemNameResults.id}_64.png")
                                .WithAuthor(author =>
                                {
                                    author
                                        .WithName($"Item: {ItemNameResults.name}")
                                        .WithUrl($"https://www.fuzzwork.co.uk/info/?typeid={ItemNameResults.id}/")
                                        .WithIconUrl("https://just4dns2.co.uk/shipexplosion.png");
                                })
                                .WithDescription($"Prices from Dodixie")
                                .AddInlineField("Buy", $"Low: {centralreply.buy.min.ToString("N2")}{Environment.NewLine}" +
                                $"Avg: {centralreply.buy.avg.ToString("N2")}{Environment.NewLine}" +
                                $"High: {centralreply.buy.max.ToString("N2")}")
                                .AddInlineField("Sell", $"Low: {centralreply.sell.min.ToString("N2")}{Environment.NewLine}" +
                                $"Avg: {centralreply.sell.avg.ToString("N2")}{Environment.NewLine}" +
                                $"High: {centralreply.sell.max.ToString("N2")}")
                                .AddField($"Extra Data", $"\u200b")
                                .AddInlineField("Buy", $"5%: {centralreply.buy.fivePercent.ToString("N2")}{Environment.NewLine}" +
                                $"Volume: {centralreply.buy.volume}")
                                .AddInlineField("Sell", $"5%: {centralreply.sell.fivePercent.ToString("N2")}{Environment.NewLine}" +
                                $"Volume: {centralreply.sell.volume.ToString("N0")}");
                            var embed = builder.Build();

                            await channel.SendMessageAsync($"", false, embed).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        await Client_Log(new LogMessage(LogSeverity.Error, "PC", ex.Message, ex));
                    }
                }
            }
            catch (Exception ex)
            {
                await channel.SendMessageAsync($"{context.Message.Author.Mention}, ERROR Please inform Discord/Bot Owner");
                await Client_Log(new LogMessage(LogSeverity.Error, "PC", ex.Message, ex));
            }         
        }
        #endregion

        //About
        #region About
        internal async static Task About(ICommandContext context)
        {
            var channel = (dynamic)context.Channel;
            var botid = Program.Client.CurrentUser.Id;
            var MemoryUsed = ByteSize.FromBytes(Process.GetCurrentProcess().WorkingSet64);
            var RunTime = DateTime.Now - Process.GetCurrentProcess().StartTime;
            var Guilds = Program.Client.Guilds.Count;
            var TotalUsers = 0;
            foreach (var guild in Program.Client.Guilds)
            {
                TotalUsers = TotalUsers + guild.Users.Count;
            }

            channel.SendMessageAsync($"{context.User.Mention},{Environment.NewLine}{Environment.NewLine}" +
                $"```Developer: Jimmy06 (In-game Name: Jimmy06){Environment.NewLine}{Environment.NewLine}" +
                $"Bot ID: {botid}{Environment.NewLine}{Environment.NewLine}" +
                $"Run Time: {RunTime.Days} Days {RunTime.Hours} Hours {RunTime.Minutes} Minutes {RunTime.Seconds} Seconds{Environment.NewLine}{Environment.NewLine}" +
                $"Statistics:{Environment.NewLine}" +
                $"Memory Used: {Math.Round(MemoryUsed.LargestWholeNumberValue, 2)} {MemoryUsed.LargestWholeNumberSymbol}{Environment.NewLine}" +
                $"Total Connected Guilds: {Guilds}{Environment.NewLine}" +
                $"Total Users Seen: {TotalUsers}```" +
                $"Invite URL: <https://discordapp.com/oauth2/authorize?&client_id={botid}&scope=bot>");


            await Task.CompletedTask;
        }
        #endregion

        //Char
        #region Char
        internal async static Task Char(ICommandContext context, string x)
        {
            var channel = context.Channel;
            var responce = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/search/?categories=character&datasource=tranquility&language=en-us&search={x}&strict=true");
            CharacterID characterID = new CharacterID();
            if (!responce.IsSuccessStatusCode)
            {
                await channel.SendMessageAsync($"{context.User.Mention}, Character ESI Failure, Please try again later");
            }
            else
            {
                characterID = JsonConvert.DeserializeObject<CharacterID>(await responce.Content.ReadAsStringAsync());

                if (characterID.character == null)
                {
                    await channel.SendMessageAsync($"{context.User.Mention}, Char not found please try again");
                }
                else
                {
                    responce = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/characters/{characterID.character[0]}/?datasource=tranquility");
                    CharacterData characterData = new CharacterData();
                    if (responce.IsSuccessStatusCode)
                    {
                        characterData = JsonConvert.DeserializeObject<CharacterData>(await responce.Content.ReadAsStringAsync());
                    }
                    responce = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/corporations/{characterData.corporation_id}/?datasource=tranquility");
                    CorporationData corporationData = new CorporationData();
                    if (responce.IsSuccessStatusCode)
                    {
                        corporationData = JsonConvert.DeserializeObject<CorporationData>(await responce.Content.ReadAsStringAsync());
                    }
                    responce = await Program._httpClient.GetAsync($"https://zkillboard.com/api/kills/characterID/{characterID.character[0]}/");

                    List<Kill> zkillContent = new List<Kill>();
                    if (responce.IsSuccessStatusCode)
                    {
                        zkillContent = JsonConvert.DeserializeObject<List<Kill>>(await responce.Content.ReadAsStringAsync());
                    }
                    Kill zkillLast = zkillContent.Count > 0 ? zkillContent[0] : new Kill();
                    SystemData systemData = new SystemData();
                    ShipType lastShip = new ShipType();
                    AllianceData allianceData = new AllianceData();
                    try
                    {
                        responce = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/universe/systems/{zkillLast.solar_system_id}/?datasource=tranquility&language=en-us");
                        if (responce.IsSuccessStatusCode)
                        {
                            systemData = JsonConvert.DeserializeObject<SystemData>(await responce.Content.ReadAsStringAsync());
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        await Client_Log(new LogMessage(LogSeverity.Error, "char", ex.Message, ex));
                    }

                    var lastShipType = "Unknown";

                    if (zkillLast.victim != null && zkillLast.victim.character_id == characterID.character.FirstOrDefault())
                    {
                        lastShipType = zkillLast.victim.ship_type_id.ToString();
                    }
                    else if (zkillLast.victim != null)
                    {
                        foreach (var attacker in zkillLast.attackers)
                        {
                            if (attacker.character_id == characterID.character.FirstOrDefault())
                            {
                                lastShipType = attacker.ship_type_id.ToString();
                            }
                        }
                    }

                    try
                    {
                        responce = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/universe/types/{lastShipType}/?datasource=tranquility&language=en-us");
                        if (responce.IsSuccessStatusCode)
                        {
                            lastShip = JsonConvert.DeserializeObject<ShipType>(await responce.Content.ReadAsStringAsync());
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        await Client_Log(new LogMessage(LogSeverity.Error, "char", ex.Message, ex));
                    }

                    var lastSeen = zkillLast.killmail_time;

                    try
                    {
                        if (characterData.alliance_id != -1)
                        {
                            responce = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/alliances/{characterData.alliance_id}/?datasource=tranquility");
                            if (responce.IsSuccessStatusCode)
                            {
                                allianceData = JsonConvert.DeserializeObject<AllianceData>(await responce.Content.ReadAsStringAsync());
                            }
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        await Client_Log(new LogMessage(LogSeverity.Error, "char", ex.Message, ex));
                    }

                    var alliance = allianceData.name ?? "None";


                    await channel.SendMessageAsync($"```Name: {characterData.name}{Environment.NewLine}" +
                        $"DOB: {characterData.birthday}{Environment.NewLine}{Environment.NewLine}" +
                        $"Corporation Name: {corporationData.name}{Environment.NewLine}" +
                        $"Alliance Name: {alliance}{Environment.NewLine}{Environment.NewLine}" +
                        $"Last System: {systemData.name}{Environment.NewLine}" +
                        $"Last Ship: {lastShip.name}{Environment.NewLine}" +
                        $"Last Seen: {lastSeen}{Environment.NewLine}```" +
                        $"ZKill: https://zkillboard.com/character/{characterID.character[0]}/");
                }
            }
            await Task.CompletedTask;
        }
        #endregion

        //Corp
        #region Corp
        internal async static Task Corp(ICommandContext context, string x)
        {
            var channel = context.Channel;
            var responce = await Program._httpClient.GetAsync(
                $"https://esi.tech.ccp.is/latest/search/?categories=corporation&datasource=tranquility&language=en-us&search={x}&strict=true");
            CorpIDLookup corpContent = new CorpIDLookup();
            if (!responce.IsSuccessStatusCode)
            {
                await channel.SendMessageAsync($"{context.User.Mention}, Corporation ESI Failure, Please try again later");
            }
            else
            {
                corpContent = JsonConvert.DeserializeObject<CorpIDLookup>(await responce.Content.ReadAsStringAsync());
                if (corpContent.corporation == null)
                {
                    await channel.SendMessageAsync($"{context.User.Mention}, Corp not found please try again");
                }
                else
                {
                    responce = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/corporations/{corpContent.corporation[0]}/?datasource=tranquility");

                    var CorpDetailsContent = JsonConvert.DeserializeObject<CorporationData>(await responce.Content.ReadAsStringAsync());
                    responce = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/characters/{CorpDetailsContent.ceo_id}/?datasource=tranquility");

                    var CEONameContent = JsonConvert.DeserializeObject<CharacterData>(await responce.Content.ReadAsStringAsync());
                    var alliance = "None";
                    if (CorpDetailsContent.alliance_id != -1)
                    {
                        responce = await Program._httpClient.GetAsync($"https://esi.tech.ccp.is/latest/alliances/{CorpDetailsContent.alliance_id}/?datasource=tranquility");
                        var allyContent = JsonConvert.DeserializeObject<AllianceData>(await responce.Content.ReadAsStringAsync());
                        alliance = allyContent.name;
                    }
                    await channel.SendMessageAsync($"```Corp Name: {CorpDetailsContent.name}{Environment.NewLine}" +
                      $"Corp Ticker: {CorpDetailsContent.ticker}{Environment.NewLine}" +
                      $"CEO: {CEONameContent.name}{Environment.NewLine}" +
                      $"Alliance Name: {alliance}{Environment.NewLine}" +
                      $"Member Count: {CorpDetailsContent.member_count}{Environment.NewLine}```" +
                      $"ZKill: https://zkillboard.com/corporation/{corpContent.corporation[0]}/");
                }
            }
            await Task.CompletedTask;
        }
        #endregion

        //Time
        #region Time
        internal async static Task EveTime(ICommandContext context)
        {
            try
            {
                var format = Program.Settings.GetSection("config")["timeformat"];
                var utcTime = DateTime.UtcNow.ToString(format);
                await context.Message.Channel.SendMessageAsync($"{context.Message.Author.Mention} Current EVE Time is {utcTime}");
            }
            catch (Exception ex)
            {
                await Client_Log(new LogMessage(LogSeverity.Error, "EveTime", ex.Message, ex));
            }
        }
        #endregion

        //Discord Stuff
        #region Discord Modules
        internal static async Task InstallCommands()
        {
            Program.Client.MessageReceived += HandleCommand;
            await Program.Commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        internal static async Task HandleCommand(SocketMessage messageParam)
        {

            var message = messageParam as SocketUserMessage;
            if (message == null) return;

            int argPos = 0;

            if (!(message.HasCharPrefix('!', ref argPos) || message.HasMentionPrefix
                    (Program.Client.CurrentUser, ref argPos))) return;

            var context = new CommandContext(Program.Client, message);

            var result = await Program.Commands.ExecuteAsync(context, argPos, Program.ServiceCollection);
            if (!result.IsSuccess && result.ErrorReason == "Unknown command.")
                await context.Channel.SendMessageAsync(result.ErrorReason);
        }
        #endregion

        //Complete
        #region MysqlQuery
        internal static async Task<IList<IDictionary<string, object>>> MysqlQuery(string connstring, string query)
        {
            using (MySqlConnection conn = new MySqlConnection(connstring))
            {
                MySqlCommand cmd = conn.CreateCommand();
                List<IDictionary<string, object>> list = new List<IDictionary<string, object>>(); ;
                cmd.CommandText = query;
                try
                {
                    conn.ConnectionString = connstring;
                    conn.Open();
                    MySqlDataReader reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var record = new Dictionary<string, object>();

                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var key = reader.GetName(i);
                            var value = reader[i];
                            record.Add(key, value);
                        }

                        list.Add(record);
                    }

                    return list;
                }
                catch (MySqlException ex)
                {
                    await Client_Log(new LogMessage(LogSeverity.Error, "mySQL", query + " " + ex.Message, ex));
                }
                await Task.Yield();
                return list;
            }
        }
        #endregion

        //SQLite Query
        #region SQLiteQuery
        internal async static Task<string> SQLiteDataQuery(string table, string field, string name)
        {
            using (SqliteConnection con = new SqliteConnection("Data Source = Opux.db;"))
            using (SqliteCommand querySQL = new SqliteCommand($"SELECT {field} FROM {table} WHERE name = @name", con))
            {
                await con.OpenAsync();
                querySQL.Parameters.Add(new SqliteParameter("@name", name));
                try
                {
                    using (SqliteDataReader r = await querySQL.ExecuteReaderAsync())
                    {
                        var result = await r.ReadAsync();
                        return r.GetString(0) ?? "";
                    }
                }
                catch (Exception ex)
                {
                    await Client_Log(new LogMessage(LogSeverity.Error, "SQLite", ex.Message, ex));
                    return null;
                }
            }
        }
        internal async static Task<List<int>> SQLiteDataQuery(string table)
        {
            using (SqliteConnection con = new SqliteConnection("Data Source = Opux.db;"))
            using (SqliteCommand querySQL = new SqliteCommand($"SELECT * FROM {table}", con))
            {
                await con.OpenAsync();
                try
                {
                    using (SqliteDataReader r = await querySQL.ExecuteReaderAsync())
                    {
                        var list = new List<int>();
                        while (await r.ReadAsync())
                        {
                            list.Add(Convert.ToInt32(r["Id"]));
                        }

                        return list;
                    }
                }
                catch (Exception ex)
                {
                    await Client_Log(new LogMessage(LogSeverity.Error, "SQLite", ex.Message, ex));
                    return null;
                }
            }
        }
        #endregion

        //SQLite Update
        #region SQLiteQuery
        internal async static Task SQLiteDataUpdate(string table, string field, string name, string data)
        {
            using (SqliteConnection con = new SqliteConnection("Data Source = Opux.db;"))
            using (SqliteCommand insertSQL = new SqliteCommand($"UPDATE {table} SET {field} = @data WHERE name = @name", con))
            {
                await con.OpenAsync();
                insertSQL.Parameters.Add(new SqliteParameter("@name", name));
                insertSQL.Parameters.Add(new SqliteParameter("@data", data));
                try
                {
                    insertSQL.ExecuteNonQuery();
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    await Client_Log(new LogMessage(LogSeverity.Error, "SQLite", ex.Message, ex));
                }
            }
        }
        #endregion

        //SQLite Delete
        #region SQLiteDelete
        internal async static Task SQLiteDataDelete(string table, string name)
        {
            using (SqliteConnection con = new SqliteConnection("Data Source = Opux.db;"))
            using (SqliteCommand insertSQL = new SqliteCommand($"REMOVE FROM {table} WHERE name = @name", con))
            {
                await con.OpenAsync();
                insertSQL.Parameters.Add(new SqliteParameter("@name", name));
                try
                {
                    insertSQL.ExecuteNonQuery();
                    await Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    await Client_Log(new LogMessage(LogSeverity.Error, "SQLite", ex.Message, ex));
                }
            }
        }
        #endregion

        //StripHTML Tags From string
        #region StripHTML
        /// <summary>
        /// Remove HTML from string with Regex.
        /// </summary>
        public static string StripTagsRegex(string source)
        {
            return Regex.Replace(source, "<.*?>", string.Empty);
        }

        /// <summary>
        /// Compiled regular expression for performance.
        /// </summary>
        static Regex _htmlRegex = new Regex("<.*?>", RegexOptions.Compiled);

        /// <summary>
        /// Remove HTML from string with compiled Regex.
        /// </summary>
        public static string StripTagsRegexCompiled(string source)
        {
            return _htmlRegex.Replace(source, string.Empty);
        }

        /// <summary>
        /// Remove HTML tags from string using char array.
        /// </summary>
        public static string StripTagsCharArray(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }
        #endregion
        }

    #region JToken null/empty check
    internal static class JsonExtensions
    {
        public static bool IsNullOrEmpty(this JToken token)
        {
            return (token == null) ||
                   (token.Type == JTokenType.Array && !token.HasValues) ||
                   (token.Type == JTokenType.Object && !token.HasValues) ||
                   (token.Type == JTokenType.String && token.HasValues) ||
                   (token.Type == JTokenType.String && token.ToString() == String.Empty) ||
                   (token.Type == JTokenType.Null);
        }
    }
    #endregion
}
