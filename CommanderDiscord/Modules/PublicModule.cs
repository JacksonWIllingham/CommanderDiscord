using CommanderDiscord.Data.Models.Messages;
using CommanderDiscord.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CommanderDiscord.Modules
{
    // Modules must be public and inherit from an IModuleBase
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        // Dependency Injection will fill this value in for us
        public PictureService PictureService { get; set; }
        public FileService FileService { get; set; }

        public MarkovService MarkovService { get; set; }

        public MessageDatabaseService MessageDatabaseService { get; set; }

        [Command("ping")]
        [Alias("pong", "hello")]
        public Task PingAsync()
            => ReplyAsync("pong!");

        [Command("cat")]
        public async Task CatAsync()
        {
            // Get a stream containing an image of a cat
            var stream = await PictureService.GetCatPictureAsync();
            // Streams must be seeked to their beginning before being uploaded!
            stream.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(stream, "cat.png");
        }

        // Get info on a user, or the user who invoked the command if one is not specified
        [Command("userinfo")]
        public async Task UserInfoAsync(IUser user = null)
        {
            user = user ?? Context.User;

            await ReplyAsync(user.ToString());
        }

        [Command("hist")]
        public async Task Hist(uint histLength)
        {
            IAsyncEnumerable<IMessage> x = Context.Channel.GetMessagesAsync((int)histLength).Flatten<IMessage>();
            IAsyncEnumerator<IMessage> y = x.GetEnumerator();

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"test.txt"))
            {
                while (await y.MoveNext())
                {
                   var sensorData = y.Current;
                   string line = sensorData.Author + ":" + sensorData.Content;
                   file.WriteLine(line);
                }
            }
                

            // Get a stream containing an image of a cat
            //var stream = await PictureService.GetCatPictureAsync();
            var stream = await FileService.GetFileAsync("test.txt");
            // Streams must be seeked to their beginning before being uploaded!
            stream.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(stream, "test.txt");
        }

        [Command("bm")]
        public async Task bulidMarkov()
        {
            MarkovService.BuildLoad(Context.Guild);            
        }

        [Command("bmasync", RunMode = RunMode.Async)]
        public async Task bulidMarkovAsync()
        {
            MarkovService.BuildLoad(Context.Guild);
        }

        [Command("m")]
        public async Task markov(IUser user)
        {
            await ReplyAsync(MarkovService.GenerateMessage(user.Id));
        }

        // Ban a user
        [Command("ban")]
        [RequireContext(ContextType.Guild)]
        // make sure the user invoking the command can ban
        [RequireUserPermission(GuildPermission.BanMembers)]
        // make sure the bot itself can ban
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task BanUserAsync(IGuildUser user, [Remainder] string reason = null)
        {
            await user.Guild.AddBanAsync(user, reason: reason);
            await ReplyAsync("ok!");
        }

        // [Remainder] takes the rest of the command's arguments as one argument, rather than splitting every space
        [Command("echo")]
        public Task EchoAsync([Remainder] string text)
            // Insert a ZWSP before the text to prevent triggering other bots!
            => ReplyAsync('\u200B' + text);

        // 'params' will parse space-separated elements into a list
        [Command("list")]
        public Task ListAsync(params string[] objects)
            => ReplyAsync("You listed: " + string.Join("; ", objects));

        // Setting a custom ErrorMessage property will help clarify the precondition error
        [Command("guild_only")]
        [RequireContext(ContextType.Guild, ErrorMessage = "Sorry, this command must be ran from within a server, not a DM!")]
        public Task GuildOnlyCommand()
            => ReplyAsync("Nothing to see here!");
    }
}
