using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TestConsole
{
    class Program
    {
        private enum ReaderState
        {
            NextLine = 0,
            FindingMatches,
            FindingName,
        }

        static void Main(string[] args)
        {
            Dictionary<string, string> items = new Dictionary<string, string>();
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TestConsole.Item.cs"))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    ReaderState state = ReaderState.NextLine;

                    string id = string.Empty;
                    string idLine = string.Empty;
                    while (reader.Peek() != -1)
                    {
                        string line = reader.ReadLine();

                        switch (state)
                        {
                            case ReaderState.NextLine:
                                {
                                    MatchCollection matches = Regex.Matches(line, @"public void SetDefaults");
                                    if (matches.Count > 0)
                                    {
                                        state = ReaderState.FindingMatches;
                                    }
                                }
                                break;
                            case ReaderState.FindingMatches:
                                {
                                    foreach (Match m in Regex.Matches(line, @"type [=<>]= (\d+)"))
                                    {
                                        id = m.Groups[1].Value;
                                        /*
                                        if (string.IsNullOrEmpty(id))
                                        {
                                            Console.WriteLine("Empty id - {0}", line);
                                        }*/
                                        state = ReaderState.FindingName;
                                    }

                                    MatchCollection matches = Regex.Matches(line, @"public void");
                                    if (matches.Count > 0)
                                    {
                                        state = ReaderState.NextLine;
                                        goto case ReaderState.NextLine;
                                    }
                                }
                                break;
                            case ReaderState.FindingName:
                                {
                                    foreach (Match m in Regex.Matches(line, @"this.name = ""(.+)"""))
                                    {
                                        /*
                                        if (string.IsNullOrEmpty(m.Groups[1].Value))
                                        {
                                            Console.WriteLine("Empty value - {0}", line);
                                        }*/
                                        string name = Regex.Replace(m.Groups[1].Value.Replace(" ", "_"), @"[^a-zA-Z0-9_]", string.Empty);
                                        name = Regex.Replace(name, @"^1", "One");
                                        name = Regex.Replace(name, @"^3", "Three");
                                        name = Regex.Replace(name, @"^5", "Five");
                                        if (items.ContainsKey(name))
                                        {
                                            name = string.Format("{0}_{1}", name, id);
                                        }
                                        
                                        items.Add(name, id);
                                        //Console.WriteLine("{0} - {1}",m.Groups[1].Value, id);
                                        id = string.Empty;
                                        state = ReaderState.FindingMatches;
                                    }

                                    MatchCollection matches = Regex.Matches(line, @"public void");
                                    if (matches.Count > 0)
                                    {
                                        state = ReaderState.NextLine;
                                        goto case ReaderState.NextLine;
                                    }
                                }
                                break;
                        }
                    }
                }
            }

            foreach(KeyValuePair<string,string> pair in items)
            {
                Console.WriteLine("{0} - {1}", pair.Key, pair.Value);
            }

            Console.ReadLine();
        }
    }
}
