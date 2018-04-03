using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Opux2
{
    public class GlobalCommands : ModuleBase
    {
        [Command("help", RunMode = RunMode.Async), Summary("Reports help text.")]
        public async Task Help()
        {
            foreach (var c in Base.Commands.Modules)
            {
                var start = $"{Context.User.Mention}, {Environment.NewLine}";
                if (c.IsSubmodule)
                {
                    foreach (var command in c.Commands)
                    {
                        var com = $"```Name: {command.Name}, Summary:{command.Summary}```{Environment.NewLine}";
                        await ReplyAsync($"{com}");
                    }
                }
                else
                {

                    foreach (var sub in c.Submodules)
                    {

                        foreach (var command in sub.Commands)
                        {
                            var com = $"```Name: {command.Name}, Summary:{command.Summary}```{Environment.NewLine}";
                            await ReplyAsync($"{com}");
                        }
                    }
                }
            }
        }

        [Command("stats", RunMode = RunMode.Async), Summary("Reports help text.")]
        public async Task Stats()
        {
            var start = $"{Context.User.Mention}, Welcome to Opux Help The following plugins are Enabled{Environment.NewLine}";
            var middle = "";
            foreach (var p in Base.Plugins)
            {
                middle += $"```Name: {p.Name}, Version: {p.Version}```{Environment.NewLine}";
            }
            await ReplyAsync($"{start}{middle}");
        }

        [Command("plugin enable", RunMode = RunMode.Async), Summary("")]
        [RequireOwner]
        public async Task PluginEnable([Remainder]string x)
        {
            var result = await MySql.MysqlQuery($"UPDATE plugin_config SET enabled=1 WHERE name=\"{x}\"");

            if (Convert.ToInt16(result[0].Keys.FirstOrDefault()) > 0)
            {
                await ReplyAsync($"{Context.User.Mention}, Plugin {x} Enabled");
            }
            else
            {
                await ReplyAsync($"{Context.User.Mention}, Cant find plugin, Check Plugin Name");
            }
        }

        [Command("plugin disable", RunMode = RunMode.Async), Summary("")]
        [RequireOwner]
        public async Task PluginDisable([Remainder]string x)
        {
            var result = await MySql.MysqlQuery($"UPDATE plugin_config SET enabled=0 WHERE name=\"{x}\"");
            if (Convert.ToInt16(result[0].Keys.FirstOrDefault()) > 0)
            {
                await ReplyAsync($"{Context.User.Mention}, Plugin {x} Disabled");
            }
            else
            {
                await ReplyAsync($"{Context.User.Mention}, Cant find plugin, Check Plugin Name");
            }
        }
    }
}
