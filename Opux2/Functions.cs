using Discord;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Opux2
{
    public static class Functions
    {
        public static string RemoveWhitespace(this string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }

        internal static async Task MySqlSetup()
        {
            string Name = "Opux2";
            string table = "plugin_config";
            try
            {
                if ((await MySql.MysqlQuery($"SELECT * FROM {table} LIMIT 1")) == null)
                {
                    Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, $"Creating {table} Table in MySql Database")).Wait();

                    var result = await MySql.MysqlQuery($"CREATE TABLE {table} (id int(6) NOT NULL AUTO_INCREMENT, name TEXT NOT NULL, enabled tinyint(4) NOT NULL, PRIMARY KEY (id)) ENGINE=InnoDB DEFAULT CHARSET=latin1");

                    if ((await MySql.MysqlQuery($"SELECT * FROM {table} LIMIT 1")) != null)
                    {
                        Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, "Table Created")).Wait();
                    }
                    else
                    {
                        Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, "Error Creating Table")).Wait();
                    }
                }
                else
                {
                    await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, $"Loaded Plugin {Name}"));
                }
            }
            catch (Exception ex)
            {
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Error, Name, $"{Name} Failed to Load {ex.Message}", ex));
            }
        }

        public static async Task<bool> CheckPluginEnabled(string Name)
        {
            string table = "plugin_config";
            if (Base.MySqlAvaliable)
            {
                var result = (await MySql.MysqlQuery($"SELECT * FROM {table} WHERE name=\"{Name}\""));

                if (result.Count == 0)
                {
                    return false;
                }
                else
                {
                    foreach (var r in result)
                    {
                        if ((string)r["name"] == Name && Convert.ToBoolean(r["enabled"]))
                            return true;
                    }
                }
            }
            return false;
        }
    }
}
