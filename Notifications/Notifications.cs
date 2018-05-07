using Discord;
using Discord.Commands;
using Opux2;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Notifications
{
    [Group]
    public class Notifications : ModuleBase, IPlugin
    {
        internal static readonly HttpClient _httpClient = new HttpClient();

        [Command("notifications", RunMode = RunMode.Async), Summary("Returns the time in EVE")]
        public async Task Time()
        {
            if (await Functions.CheckPluginEnabled(Name))
            {
                await ReplyAsync($"Time In EVE is Currently {DateTime.UtcNow}");
            }
        }

        public string Name => "Notifications";

        public string Description => "Gets and displays the EVE time";

        public string Author => "Jimmy06";

        public Version Version => new Version(0, 0, 0, 1);

        public async Task OnLoad()
        {
            try
            {
                var ready = await Functions.EnableListener();

                Base._httpListener.Request += _httpListener_Request;

                if ((await Opux2.MySql.MysqlQuery($"SELECT * FROM plugin_config WHERE name=\"{Name}\"")).Count == 0)
                {
                    await Opux2.MySql.MysqlQuery($"INSERT INTO plugin_config (name, enabled) " +
                        $"VALUES (\"{Name}\", 0)");
                }

                await Base.Commands.AddModuleAsync(GetType());
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, $"Loaded Plugin {Name}"));
            }
            catch (Exception ex)
            {
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, Name, $"{Name} Failed to Load {ex.Message}", ex));
            }
        }

        private void _httpListener_Request(object sender, HttpListenerRequestEventArgs e)
        {
                if (e.Request.Url.LocalPath == "/test")
                {
                    e.Response.WriteContentAsync("<!doctype html>" +
                                               "<html>" +
                                               "<head>" +
                                               $"<body>{DateTime.UtcNow}</body>");
                    e.Response.Close();
                }
        }

        public Task UnLoad()
        {
            return null;
        }

        public async Task Pulse()
        {
            await Task.CompletedTask;
        }
    }
}
