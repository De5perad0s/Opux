using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Opux2;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HttpListener = System.Net.Http.HttpListener;

namespace authWeb
{
    public class AuthWeb : ModuleBase, IPlugin
    {
        public static IConfiguration Configuration { get; set; }

        internal static HttpListener listener;
        internal static readonly HttpClient _httpClient = new HttpClient();

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

        }

        public async Task UnLoad()
        {
            var Enabled = await Functions.CheckPluginEnabled(Name);

            if (listener != null && !Enabled && listener.IsListening)
            {
                await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, "Stopping AuthWeb Server"));
                listener.Close();
                listener.Dispose();
            }
            await Task.CompletedTask;
        }

            public async Task Pulse()
        {
            if (!_Running)
            {
                var Enabled = await Functions.CheckPluginEnabled(Name);
                if (listener == null && Enabled || Enabled && !listener.IsListening)
                {
                    await Webserver();
                }
                await Task.CompletedTask;
            }
        }

        private async Task Webserver()
        {
            var port = 8090;
            await Logger.DiscordClient_Log(new LogMessage(LogSeverity.Info, Name, "Starting AuthWeb Server"));
            listener = new HttpListener(IPAddress.Any, port);

            listener.Request += async (sender, context) =>
            {
                if (context.Request.Url.LocalPath == "/")
                {
                    var callbackurl = "";
                    var client_id = "";

                    await context.Response.WriteContentAsync("<!doctype html>" +
                        "<html>" +
                        "<head>" +
                        "    <title>Discord Authenticator</title>" +
                        "    <meta name=\"viewport\" content=\"width=device-width\">" +
                        "    <link rel=\"stylesheet\" href=\"https://djyhxgczejc94.cloudfront.net/frameworks/bootstrap/3.0.0/themes/cirrus/bootstrap.min.css\">" +
                        "    <script type=\"text/javascript\" src=\"https://ajax.googleapis.com/ajax/libs/jquery/2.0.3/jquery.min.js\"></script>" +
                        "    <script type=\"text/javascript\" src=\"https://netdna.bootstrapcdn.com/bootstrap/3.1.1/js/bootstrap.min.js\"></script>" +
                        "    <style type=\"text/css\">" +
                        "        /* Space out content a bit */" +
                        "        body {" +
                        "            padding-top: 20px;" +
                        "            padding-bottom: 20px;" +
                        "        }" +
                        "        /* Everything but the jumbotron gets side spacing for mobile first views */" +
                        "        .header, .marketing, .footer {" +
                        "            padding-left: 15px;" +
                        "            padding-right: 15px;" +
                        "        }" +
                        "       /* Custom page header */" +
                        "        .header {" +
                        "            border-bottom: 1px solid #e5e5e5;" +
                        "        }" +
                        "        /* Make the masthead heading the same height as the navigation */" +
                        "        .header h3 {" +
                        "            margin-top: 0;" +
                        "            margin-bottom: 0;" +
                        "            line-height: 40px;" +
                        "            padding-bottom: 19px;" +
                        "        }" +
                        "        /* Custom page footer */" +
                        "        .footer {" +
                        "            padding-top: 19px;" +
                        "            color: #777;" +
                        "            border-top: 1px solid #e5e5e5;" +
                        "        }" +
                        "        /* Customize container */" +
                        "        @media(min-width: 768px) {" +
                        "            .container {" +
                        "                max-width: 730px;" +
                        "            }" +
                        "        }" +
                        "        .container-narrow > hr {" +
                        "            margin: 30px 0;" +
                        "        }" +
                        "        /* Main marketing message and sign up button */" +
                        "        .jumbotron {" +
                        "            text-align: center;" +
                        "            border-bottom: 1px solid #e5e5e5;" +
                        "        }" +
                        "        .jumbotron .btn {" +
                        "            font-size: 21px;" +
                        "            padding: 14px 24px;" +
                        "            color: #0D191D;" +
                        "        }" +
                        "        /* Supporting marketing content */" +
                        "        .marketing {" +
                        "            margin: 40px 0;" +
                        "        }" +
                        "        .marketing p + h4 {" +
                        "            margin-top: 28px;" +
                        "        }" +
                        "        /* Responsive: Portrait tablets and up */" +
                        "        @media screen and(min-width: 768px) {" +
                        "            /* Remove the padding we set earlier */" +
                        "            .header, .marketing, .footer {" +
                        "                padding-left: 0;" +
                        "                padding-right: 0;" +
                        "            }" +
                        "            /* Space out the masthead */" +
                        "            .header {" +
                        "                margin-bottom: 30px;" +
                        "            }" +
                        "            /* Remove the bottom border on the jumbotron for visual effect */" +
                        "            .jumbotron {" +
                        "                border-bottom: 0;" +
                        "            }" +
                        "        }" +
                        "    </style>" +
                        "</head>" +
                        "" +
                        "<body background=\"img/background.jpg\">" +
                        "<div class=\"container\">" +
                        "    <div class=\"header\">" +
                        "        <ul class=\"nav nav-pills pull-right\"></ul>" +
                        "    </div>" +
                        "    <div class=\"jumbotron\">" +
                        "        <h1>Discord</h1>" +
                        "        <p class=\"lead\">Click the button below to login with your EVE Online account.</p>" +
                        "        <p><a href=\"https://login.eveonline.com/oauth/authorize?response_type=code&amp;redirect_uri=" + callbackurl + "&amp;client_id=" + client_id + "\"><img src=\"https://images.contentful.com/idjq7aai9ylm/4fSjj56uD6CYwYyus4KmES/4f6385c91e6de56274d99496e6adebab/EVE_SSO_Login_Buttons_Large_Black.png\"/></a></p>" +
                        "    </div>" +
                        "</div>" +
                        "<!-- /container -->" +
                        "</body>" +
                        "</html>");
                }
                else if (context.Request.Url.LocalPath == "/callback.php")
                {
                    await context.Response.WriteContentAsync("<!doctype html>" +
                        "<html>" +
                        "<head>" +
                        "    <title>Discord Authenticator</title>");
                }
                await Task.CompletedTask;

                context.Response.Close();
            };

            listener.Start();

            await Task.CompletedTask;
        }
    }
}
