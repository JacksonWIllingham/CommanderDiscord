using CommanderDiscord.Data.Models.Messages;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CommanderDiscord.Services
{
    public class MarkovService
    {
        private static class Consts
        {
            public static int MESSAGE_HISTORY_COUNT = 10000;
            public static string TERMINATE = "TERMINATE";
        }

        private static List<Message> Messages { get; set; }

        private static Dictionary<ulong, Dictionary<string, Dictionary<string, int>>> markovMap { get; set; }


        public MarkovService()
        {

        }

        public void BuildLoadAsync(SocketGuild guild)
        {
            Messages = new List<Message>();
            Task.Run(() =>
            {
                Task.Run(() => Load(guild)).Wait();
                Task.Run(() => Build()).Wait();
            });
        }

        public void BuildLoad(SocketGuild guild)
        {
            Messages = new List<Message>();
            Load(guild);
            Build();
        }

        private void LoadAsync(SocketGuild guild)
        {
            Task.Run(() => Load(guild));
        }

        private void BuildAsync()
        {
            Task.Run(() => Build());
        }

        private void Load(SocketGuild guild)
        {
            IEnumerator<SocketGuildChannel> guildEnumerator = guild.Channels.GetEnumerator();

            SocketGuildChannel guildChannelCurrent;
            guildEnumerator.MoveNext();
            while (guildEnumerator.MoveNext())
            {
                guildChannelCurrent = guildEnumerator.Current;
                SocketTextChannel textChannel = guild.GetTextChannel(guildChannelCurrent.Id);

                if (textChannel != null)
                {
                    try
                    {

                        IAsyncEnumerable<IMessage> x = textChannel.GetMessagesAsync(Consts.MESSAGE_HISTORY_COUNT).Flatten<IMessage>();
                        IAsyncEnumerator<IMessage> y = x.GetEnumerator();

                        while (WaitFor(y.MoveNext()))
                        {
                            var msg = y.Current;

                            Message tmp = new Message();
                            tmp.UniqueId = msg.Author.Id;
                            tmp.Username = msg.Author.Username;
                            tmp.Content = msg.Content;
                            Messages.Add(tmp);
                        }
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine("ERROR: " + textChannel.Name +", "+e.ToString());
                    }
                }
            }

            guild.GetTextChannel(guild.Channels.First(x => x.Name == "bot-commands").Id).SendMessageAsync("BUILT");
        }

        private static bool WaitFor(Task<bool> x)
        {
            x.Wait();
            return x.Result;
        }

        private void Build()
        {
            foreach (Message m in Messages)
            {
                string body = m.Content;

                //strip urls
                List<string> tokens = new List<string>(body.Split(' '));
                List<string> RemoveTokens = new List<string>();
                foreach(string s in tokens)
                {
                    if(IsUrlValid(s))
                    {
                        RemoveTokens.Add(s);
                    }
                }

                foreach(string s in RemoveTokens)
                {
                    tokens.Remove(s);
                }

                body = tokens.Join(" ");

                string[] specialChars = { "!", "?", ",", "\"", ". " };
                foreach (string c in specialChars)
                {
                    body = body.Replace(c, " " + c + " ");
                }
                tokens = new List<string>(body.Split(' '));

                for (int i = 0; i < tokens.Count; i++)
                {
                    string nextToken = "";
                    if (i == tokens.Count - 1)
                    {
                        //STRING_TERMINATOR
                        nextToken = Consts.TERMINATE;

                    }
                    else // my name is jackson
                    {
                        nextToken = tokens[i + 1];
                    }

                    if (markovMap==null)
                    {
                        markovMap = new Dictionary<ulong, Dictionary<string, Dictionary<string, int>>>();
                    }

                    if (!markovMap.ContainsKey(m.UniqueId))
                    {
                        markovMap[m.UniqueId] = new Dictionary<string, Dictionary<string, int>>();
                    }

                    if (!markovMap[m.UniqueId].ContainsKey(tokens[i]))
                    {
                        markovMap[m.UniqueId][tokens[i]] = new Dictionary<string, int>();
                    }

                    if (!markovMap[m.UniqueId][tokens[i]].ContainsKey(nextToken))
                    {
                        markovMap[m.UniqueId][tokens[i]][nextToken] = 0;
                    }

                    int cc = markovMap[m.UniqueId][tokens[i]][nextToken];
                    markovMap[m.UniqueId][tokens[i]][nextToken] = cc + 1;
                }


                //markovMap[m.UniqueId]
            }
            //debugString();
        }

        private bool IsUrlValid(string url)
        {

            string pattern = @"^(http|https|ftp|)\://|[a-zA-Z0-9\-\.]+\.[a-zA-Z](:[a-zA-Z0-9]*)?/?([a-zA-Z0-9\-\._\?\,\'/\\\+&amp;%\$#\=~])*[^\.\,\)\(\s]$";
            Regex reg = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return reg.IsMatch(url);
        }

        public string GenerateMessage(ulong UserId)
        {
            string result = "";

            if (markovMap.ContainsKey(UserId))
            {
                //ulong UserId = UsernameToId[Username];
                Dictionary<string, Dictionary<string, int>> map = markovMap[UserId];

                Random random = new Random();
                int seed = random.Next(0, map.Keys.Count - 1);
                var enumerator = map.Keys.GetEnumerator();

                int index = 0;
                while (index < seed)
                {
                    index++;
                    enumerator.MoveNext();
                }

                string currentWord = enumerator.Current;
                result += currentWord;

                bool run = true;
                int MAX_LOOPS = 50;

                int loops = 0;
                while (run)
                {
                    loops++;
                    Dictionary<string, int> z = map[currentWord];

                    int sum = 0;
                    foreach (string s in z.Keys)
                    {
                        sum += z[s];
                    }

                    seed = random.Next(0, sum);

                    int currentValue = 0;
                    foreach (string s in z.Keys)
                    {
                        currentValue += z[s];
                        if (currentValue >= sum)
                        {
                            currentWord = s;
                            if (currentWord != Consts.TERMINATE)
                            {
                                result += " " + currentWord;
                            }
                            break;
                        }
                    }

                    if (currentWord == Consts.TERMINATE)
                    {
                        run = false;
                    }

                    if (loops >= MAX_LOOPS)
                    {
                        run = false;
                    }

                    if (result.Length >= 1800)
                    {
                        run = false;
                    }
                }
            }
            else
            {
                result = "No data";
            }

            return result;
        }
    }
}
