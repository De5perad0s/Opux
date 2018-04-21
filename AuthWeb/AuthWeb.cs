using Discord;
using Discord.Commands;
using ESIClient.Client;
using Microsoft.Extensions.Configuration;
using Opux2;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using HttpListener = System.Net.Http.HttpListener;

namespace authWeb
{
    public class AuthWeb : ModuleBase, IPlugin
    {
        public static IConfiguration Configuration { get; set; }

        internal static HttpListener listener;

        public string Name => "AuthWeb";

        public string Description => "Interface for AuthWeb and the User";

        public string Author => "Jimmy06";

        public Version Version => new Version(0, 0, 0, 1);

        public bool MySqlExists { get; private set; }
        public bool _Running { get; private set; }

        public async Task OnLoad()
        {
            try
            {
                string table = "authweb";
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

            if (listener == null || !listener.IsListening)
            {
                var port = 8090;
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, "Starting AuthWeb Server"));
                listener = new HttpListener(IPAddress.Any, port);

                listener.Request += async (sender, context) =>
                {
                    if (context.Request.Url.LocalPath == "/")
                    {
                        var status = await new ESIClient.Api.StatusApi().GetStatusAsync();
                        context.Response.Headers.Add("Content-Type", "text/html");

                        await context.Response.WriteContentAsync(status.ToJson().ToString());
                    }
                    await Task.CompletedTask;

                    context.Response.Close();
                };

                listener.Start();

                await Task.CompletedTask;
            }
        }

        public async Task Pulse()
        {
            await Task.CompletedTask;
        }
    }
}
