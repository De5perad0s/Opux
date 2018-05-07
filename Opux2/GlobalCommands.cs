using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
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
        [RequireRole("Admin")]
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
                await Base.Plugins.FirstOrDefault(p => p.Name == x).UnLoad();
                await ReplyAsync($"{Context.User.Mention}, Plugin {x} Disabled");
            }
            else
            {
                await ReplyAsync($"{Context.User.Mention}, Cant find plugin, Check Plugin Name");
            }
        }

        [Command("plugin list", RunMode = RunMode.Async), Summary("")]
        [RequireOwner]
        public async Task PluginList()
        {
            var result = await MySql.MysqlQuery($"SELECT * FROM plugin_config");

            if (result.Count > 0)
            {
                foreach (var plugin in result)
                {
                    await ReplyAsync($"{plugin["name"]}");
                }
            }
        }
    }

    public class RequireRoleAttribute : RequireContextAttribute
    {
        private ulong[] _roleIds;
        private string[] _roleNames;

        ///// <summary> Requires that the command caller has ANY of the supplied role ids. </summary>
        //public RequireRoleAttribute(params ulong[] roleIds) : base(ContextType.Guild)
        //    => _roleIds = roleIds;
        ///// <summary> Requires that the command caller has ANY of the supplied role names. </summary>
        //public RequireRoleAttribute(params string[] roleNames) : base(ContextType.Guild)
        //    => _roleNames = roleNames;

        public RequireRoleAttribute(params ulong[] roleIds) : base(ContextType.Guild)
        {
            _roleIds = roleIds;
        }

        public RequireRoleAttribute(params string[] roleNames) : base(ContextType.Guild)
        {
            _roleNames = roleNames;
        }

        public override Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            var allowedRoleIds = new List<ulong>();

            if (_roleIds != null)
                allowedRoleIds.AddRange(_roleIds);
            if (_roleNames != null)
                allowedRoleIds.AddRange(context.Guild.Roles.Where(x => _roleNames.Contains(x.Name)).Select(x => x.Id));

            return (context.User as IGuildUser).RoleIds.Intersect(allowedRoleIds).Any()
            ? Task.FromResult(PreconditionResult.FromSuccess())
            : Task.FromResult(PreconditionResult.FromError("You do not have a role required to execute this command."));
        }
    }
}
