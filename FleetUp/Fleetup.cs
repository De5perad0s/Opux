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
        public async Task Add([Remainder] string x)
        {
            try
            {
                var split = x.Split(',');

                var guildid = Functions.RemoveWhitespace(split[0]);
                var channelid = Functions.RemoveWhitespace(split[1]);
                var userid = Functions.RemoveWhitespace(split[2]);
                var groupid = Functions.RemoveWhitespace(split[3]);
                var apicode = Functions.RemoveWhitespace(split[4]);

                var result = await Opux2.MySql.MysqlQuery($"INSERT INTO FleetUp (channelid, guildid, UserId, APICode, GroupID, fleetUpLastPostedOperation, announce_post) " +
                    $"VALUES ({channelid}, {guildid}, {userid}, '{apicode}', {groupid}, 0, 0) " +
                    $"ON DUPLICATE KEY UPDATE channelid = VALUES(channelid)");

            }
            catch (Exception ex)
            {
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, Name, $"{Name} Add Failure {ex.Message}", ex));
            }
        }

        [Command("add", RunMode = RunMode.Async), Summary("Add's a fleetup API")]
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

        static IConfiguration Configuration { get; set; }
        static bool MySqlExists { get; set; }
        static DateTime _lastRun { get; set; }
        static bool _Running { get; set; }

        public bool Configured { get; private set; }

        public string Name => "FleetUp";

        public string Description => "FleetUp Intergration";

        public string Author => "Jimmy06";

        public Version Version => new Version(0, 0, 0, 1);

        public async Task OnLoad()
        {
            try
            {
                if ((await Opux2.MySql.MysqlQuery("SELECT * FROM FleetUp LIMIT 1")) == null)
                {
                    Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "FleetUp", "Creating Fleetup Table in MySql Database")).Wait();

                    var result = await Opux2.MySql.MysqlQuery("CREATE TABLE FleetUp (id int(6) NOT NULL AUTO_INCREMENT, channelid bigint(20) unsigned NOT NULL, " +
                        "guildid bigint(20) unsigned NOT NULL, UserId smallint(6) NOT NULL, APICode text NOT NULL, GroupID mediumint(9) NOT NULL, " +
                        "fleetUpLastPostedOperation mediumint(9) NOT NULL, announce_post tinyint(4) NOT NULL, PRIMARY KEY (id), UNIQUE KEY (GroupID) ) ENGINE=InnoDB DEFAULT CHARSET=latin1");

                    if ((await Opux2.MySql.MysqlQuery("SELECT * FROM FleetUp LIMIT 1")) != null)
                    {
                        Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "FleetUp", "Table Created")).Wait();
                        MySqlExists = true;
                        _Running = false;
                        await Base.Commands.AddModuleAsync(GetType());
                        await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, $"Loaded Plugin {Name}"));
                    }
                    else
                    {
                        Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, "FleetUp", "Error Creating Table")).Wait();
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
            if (MySqlExists == true && DateTime.UtcNow >= _lastRun.AddSeconds(1) && _Running == false && Configured)
            {
                _Running = true;

                var fleetupList = await Opux2.MySql.MysqlQuery("SELECT * FROM FleetUp");

                foreach (var f in fleetupList)
                {
                    var channelRaw = Convert.ToUInt64("channelid");
                    var guildidRaw = Convert.ToUInt64("guildid");
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
    }
}
