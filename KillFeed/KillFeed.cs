using Discord;
using Discord.Commands;
using ESIClient.Api;
using ESIClient.Client;
using ESIClient.Model;
using Microsoft.Extensions.Configuration;
using Opux2;
using System;
using System.Threading.Tasks;

namespace killFeed
{
    public class KillFeed : ModuleBase, IPlugin
    {
        static DateTime LastRun { get; set; }
        public bool MySqlExists { get; private set; }
        static bool _Running { get; set; }
        static bool _FirstRunDone { get; set; }
        static bool _VIP { get; set; }
        static string _Version { get; set; }
        static bool _Offline { get; set; }
        static DateTime _Starttime { get; set; }
        static string table = "killfeed";

        public string Name => "Killfeed";

        public string Description => "Posts kills from zKill redisq";

        public string Author => "Jimmy06";

        public Version Version => new Version(0, 0, 0, 1);

        public async Task OnLoad()
        {
            try
            {
                if ((await Opux2.MySql.MysqlQuery($"SELECT * FROM {table} LIMIT 1")) == null)
                {
                    Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, $"Creating {table} Table in MySql Database")).Wait();

                    var result = await Opux2.MySql.MysqlQuery($"CREATE TABLE {table} (id int(6) NOT NULL AUTO_INCREMENT, channelid bigint(20) unsigned NOT NULL, " +
                        "guildid bigint(20) unsigned NOT NULL, groupid bigint(20) unsigned NOT NULL, mediumint(9) NOT NULL, alliance tinyint(4) NOT NULL, PRIMARY KEY (id), UNIQUE KEY (GroupID) ) ENGINE=InnoDB DEFAULT CHARSET=latin1");

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
            await Task.CompletedTask;
        }
    }
}
