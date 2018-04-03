using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Opux2;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FleetUp
{
    [Group("fleetup")]
    class FleetUp : ModuleBase, IPlugin
    {

        [Command("add", RunMode = RunMode.Async), Summary("Add's a fleetup API")]
        [RequireOwner]
        public async Task Add([Remainder] string x)
        {
            try
            {
                var test = ((IDMChannel)Context.Channel);

                var split = x.Split(',');

                var guildid = Functions.RemoveWhitespace(split[0]);
                var channelid = Functions.RemoveWhitespace(split[1]);
                var userid = Functions.RemoveWhitespace(split[2]);
                var groupid = Functions.RemoveWhitespace(split[3]);
                var apicode = Functions.RemoveWhitespace(split[4]);

                var result = await Opux2.MySql.MysqlQuery($"INSERT INTO {table} (channelid, guildid, UserId, APICode, GroupID, fleetUpLastPostedOperation, announce_post) " +
                    $"VALUES ({channelid}, {guildid}, {userid}, '{apicode}', {groupid}, 0, 0) " +
                    $"ON DUPLICATE KEY UPDATE channelid = VALUES(channelid)");

            }
            catch (InvalidCastException ex)
            {
                if (ex.Message.Contains("Discord.IDMChannel"))
                {
                    await Context.Message.DeleteAsync();
                    await ReplyAsync($"{Context.User.Mention}, Please DM me these settings I've deleted your message to try and protect your settings");
                }
            }
            catch (Exception ex)
            {
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, Name, $"{Name} Add Failure {ex.Message}", ex));
            }
        }

        [Command("add", RunMode = RunMode.Async), Summary("Add's a fleetup API")]
        [RequireOwner]
        public async Task Add()
        {
            try
            {
                await ReplyAsync($"Usage: ```!fleetup add guildid, channelid, userid, groupid, apicode```{Environment.NewLine}" +
                    $"```!fleetup add 289437048908283915, 289437048908283915, 12294, 49471, US9TNjNVRTWpmKMVCJTxVLmmHXXqYE```");
            }
            catch
            { }
        }

        [Command("delete", RunMode = RunMode.Async), Summary("Delete's a fleetup API")]
        [RequireOwner]
        public async Task Delete([Remainder] string x)
        {
            try
            {
                var result = await Opux2.MySql.MysqlQuery($"DELETE FROM {table} WHERE GroupID={x}");
            }
            catch (InvalidCastException ex)
            {
                if (ex.Message.Contains("Discord.IDMChannel"))
                {
                    await Context.Message.DeleteAsync();
                    await ReplyAsync($"{Context.User.Mention}, Please DM me these settings I've deleted your message to try and protect your settings");
                }
            }
            catch (Exception ex)
            {
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, Name, $"{Name} Add Failure {ex.Message}", ex));
            }
        }

        [Command("delete", RunMode = RunMode.Async), Summary("Delete's a fleetup API")]
        [RequireOwner]
        public async Task Delete()
        {
            try
            {
                await ReplyAsync($"Usage: ```!fleetup delete groupid```{Environment.NewLine}" +
                    $"```!fleetup delete 49471```");
            }
            catch
            { }
        }

        [Command("announce", RunMode = RunMode.Async), Summary("Changes the announce setting of a Group")]
        [RequireOwner]
        public async Task Announce([Remainder] string x)
        {
            try
            {
                var split = x.Split(',');

                var groupid = Functions.RemoveWhitespace(split[0]);
                var announce = Functions.RemoveWhitespace(split[1]);

                var result = await Opux2.MySql.MysqlQuery($"UPDATE {table} SET announce_post={announce} WHERE groupid={groupid}");

                await ReplyAsync($"{Context.User.Mention}, If you entered a valid group it will now be updated");
            }
            catch
            {
                await ReplyAsync($"{Context.User.Mention}, Please check your request as it is malformed.");
            }
        }

        [Command("announce", RunMode = RunMode.Async), Summary("Changes the announce setting of a Group")]
        [RequireOwner]
        public async Task Announce()
        {
            try
            {
                await ReplyAsync($"Usage: ```!fleetup announce, groupid, int```{Environment.NewLine}" +
                    $"```!fleetup announce, 49471, 1```");
            }
            catch
            { }
        }

        [Command("active", RunMode = RunMode.Async), Summary("Add's a fleetup API")]
        [RequireOwner]
        public async Task Active([Remainder] string x)
        {

        }

        [Command("active", RunMode = RunMode.Async), Summary("Add's a fleetup API")]
        [RequireOwner]
        public async Task Active()
        {
            await ReplyAsync($"{Context.User.Mention}, Use 1 or 0 to enable or disable fleetup");
        }

        static IConfiguration Configuration { get; set; }
        static bool MySqlExists { get; set; }
        static DateTime _lastRun { get; set; }
        static bool _Running { get; set; }
        static string table = "fleetup";

        public bool Configured { get; private set; }

        public string Name => "FleetUp";

        public string Description => "FleetUp Intergration";

        public string Author => "Jimmy06";

        public Version Version => new Version(0, 0, 0, 1);

        public async Task OnLoad()
        {
            try
            {
                if ((await Opux2.MySql.MysqlQuery($"SELECT * FROM plugin_config WHERE name=\"{Name}\"")).Count == 0)
                {
                    await Opux2.MySql.MysqlQuery($"INSERT INTO plugin_config (name, enabled) " +
                        $"VALUES (\"{Name}\", 0)");
                }
                if ((await Opux2.MySql.MysqlQuery($"SELECT * FROM {table} LIMIT 1")) == null)
                {
                    Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, $"Creating {table} Table in MySql Database")).Wait();

                    var result = await Opux2.MySql.MysqlQuery($"CREATE TABLE {table} (id int(6) NOT NULL AUTO_INCREMENT, channelid bigint(20) unsigned NOT NULL, " +
                        "guildid bigint(20) unsigned NOT NULL, UserId smallint(6) NOT NULL, APICode text NOT NULL, GroupID mediumint(9) NOT NULL, " +
                        "fleetUpLastPostedOperation mediumint(9) NOT NULL, announce_post tinyint(4) NOT NULL, PRIMARY KEY (id), UNIQUE KEY (GroupID) ) ENGINE=InnoDB DEFAULT CHARSET=latin1");

                    if ((await Opux2.MySql.MysqlQuery($"SELECT * FROM {table} LIMIT 1")) != null)
                    {
                        Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, "Table Created")).Wait();
                        MySqlExists = true;
                        _Running = false;
                        await Base.Commands.AddModuleAsync(GetType());
                        await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, $"Loaded Plugin {Name}"));
                    }
                    else
                    {
                        Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, "Error Creating Table")).Wait();
                    }
                }
                else
                {
                    MySqlExists = true;
                    _Running = false;
                    await Base.Commands.AddModuleAsync(GetType());
                    await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, $"Loaded Plugin {Name}"));
                }
            }
            catch (Exception ex)
            {
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, Name, $"{Name} Failed to Load {ex.Message}", ex));
            }
        }

        public async Task Pulse()
        {
            try
            {
                if (MySqlExists == true && DateTime.UtcNow >= _lastRun.AddSeconds(1) && _Running == false)
                {
                    _Running = true;

                    var fleetupList = await Opux2.MySql.MysqlQuery($"SELECT * FROM {table}");

                    foreach (var f in fleetupList)
                    {
                        var channelRaw = Convert.ToUInt64(f["channelid"]);
                        var guildidRaw = Convert.ToUInt64(f["guildid"]);
                        var UserId = Convert.ToUInt32(f["UserId"]);
                        var APICode = Convert.ToString(f["APICode"]);
                        var GroupID = Convert.ToUInt32(f["GroupID"]);
                        var lastopid = Convert.ToUInt32(f["fleetUpLastPostedOperation"]);
                        var announce_post = Convert.ToBoolean(f["announce_post"]);
                        var channel = Base.DiscordClient.GetGuild(guildidRaw).GetTextChannel(channelRaw);
                    }

                    _Running = false;
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _Running = false;
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, $"{ex.Message}", ex));
            }
        }
    }
}
