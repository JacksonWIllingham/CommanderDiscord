using CommanderDiscord.Data.Models.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommanderDiscord.Services
{
    public class MessageDatabaseService
    {
        
        private const string STRING_TERMINATOR = "TERMINATE";

        private static List<Message> messages { get; set; }

        private static Dictionary<string, ulong> UsernameToId { get; set; }

        private static Dictionary<ulong, Dictionary<string, Dictionary<string, int>>> markovMap { get; set; }

        public MessageDatabaseService()
        {
            if (messages == null)
            {
                messages = new List<Message>();
            }

            if(UsernameToId==null)
            {
                UsernameToId = new Dictionary<string, ulong>();
            }

            if(markovMap==null)
            {
                markovMap = new Dictionary<ulong, Dictionary<string, Dictionary<string, int>>>();
            }
        }

        public void AddMessage(Message msg)
        {
            
            messages.Add(msg);
        }

        public void Build()
        {
            foreach(Message m in messages)
            {
                UsernameToId[m.Username] = m.UniqueId;

                string body = m.Content;
                string[] specialChars = { "!", "?", ",", "\"", ". " };
                foreach(string c in specialChars)
                {
                    body = body.Replace(c, " "+c+" ");
                }
                List<string> tokens = new List<string>(body.Split(' '));

                for (int i = 0; i < tokens.Count; i++)
                {
                    string nextToken = "";
                    if (i == tokens.Count - 1)
                    {
                        //STRING_TERMINATOR
                        nextToken = STRING_TERMINATOR;

                    }
                    else // my name is jackson
                    {
                        nextToken = tokens[i + 1];
                    }

                    if(!markovMap.ContainsKey(m.UniqueId))
                    {
                        markovMap[m.UniqueId] = new Dictionary<string, Dictionary<string, int>>();
                    }

                    if(!markovMap[m.UniqueId].ContainsKey(tokens[i]))
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
            debugString();
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
                            if (currentWord != STRING_TERMINATOR)
                            {
                                result += " " + currentWord;
                            }
                            break;
                        }
                    }

                    if (currentWord == STRING_TERMINATOR)
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

        private void debugString()
        {
            string s = "";

            foreach(ulong a in markovMap.Keys)
            {
                s += a.ToString() + "\n";
                foreach(KeyValuePair<string, Dictionary<string, int>> b in markovMap[a])
                {
                    s += "\t" + b.Key + "\n"; // + ":" + +"\n";
                    foreach(KeyValuePair<string, int> c in b.Value)
                    {
                        s += "\t\t" + c.Key + ":" + c.Value + "\n";
                    }
                }
            }

            Console.WriteLine(s);
        }
    }
}
