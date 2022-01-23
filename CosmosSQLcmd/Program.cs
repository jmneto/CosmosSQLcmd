using Microsoft.Azure.Cosmos;
using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CosmosSQLcmd
{
    internal sealed class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                Console.TreatControlCAsInput = true;    // Disable CTRL+C

                await EditorHelper.Editor(Config.From(args));
            }
            catch (Exception ex)
            {
                Console.Clear();
                Console.WriteLine(ex.ToString());
            }
        }
    }

    internal class Config
    {
        [Option("endpoint", Required = true, HelpText = "Azure Cosmos DB account endpoint URI")]
        public string? EndPoint { get; set; }

        [Option("key", Required = true, HelpText = "Azure Cosmos DB account read access key")]
        [JsonIgnore]
        public string? Key { get; set; }

        [Option("database", Required = true, HelpText = "Target database to use")]
        public string? Database { get; set; }

        [Option("container", Required = true, HelpText = "Target container to use")]
        public string? Container { get; set; }

        [Option("cp", Required = false, HelpText = "Connection policy:Direct|Gateway (Default:Direct)")]
        public string ConnectionPolicy { get; set; } = "Direct";

        [Option("maxfetchsize", Required = false, HelpText = "Number of items per fetch (Default:100)")]
        public int MaxItemstoFetch { get; set; } = 100;

        [Option("metrics", Required = false, HelpText = "Include metrics")]
        public bool Metrics { get; set; }

        internal static Config From(string[] args)
        {
            Config? options = null;

            // Parse command parameters
            var parserResult = (new CommandLine.Parser((settings) =>
                                            {
                                                settings.CaseSensitive = false;
                                                settings.HelpWriter = null;
                                            })).ParseArguments<Config>(args);
            parserResult
              .WithParsed<Config>(e => options = e)
              .WithNotParsed(errs => DisplayHelp(parserResult, errs));

            // If parameters are wrong, exit program
            if (options == null)
                Environment.Exit(1);

            return options;
        }

        private static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            HelpText? helpText = null;
            if (errs.IsVersion())
                helpText = HelpText.AutoBuild(result);
            else
            {
                helpText = HelpText.AutoBuild(result, h =>
                {
                    h.AdditionalNewLineAfterOption = false;
                    h.Heading = "CosmosSQLcmd";
                    h.Copyright = @"https://github.com/jmneto";
                    h.AutoVersion = false;
                    return HelpText.DefaultParsingErrorsHandler(result, h);
                }, e => e);
            }
            Console.WriteLine(helpText);
        }
    }

    internal static class EditorHelper
    {
        public static async Task Editor(Config config)
        {
            // Clear Screen
            Console.Clear();

            // Prepare header lines
            string[] header = { String.Format("CosmosSQLcmd | ({0}{1})({2})({3})", config.EndPoint, config.ConnectionPolicy, config.Database, config.Container), "Editor Mode | Press CTRL+E to execute query | ESC to exit" };
            const int headerrows = 2;

            // Editor text buffer
            List<string> lines = new List<string>();
            lines.Add("select * from c");

            // Initialze cursor index
            int rowCursorIndex = 0;
            int colCursorIndex = 0;

            // Initialize Console Key Info
            ConsoleKeyInfo cki = new ConsoleKeyInfo();
            
            // Main Loop
            do
            {
                // prepare max cursor indexes visible on screen
                int maxRowCursorIndex = Console.WindowHeight - headerrows - 1;    // -1 = Make it zero based
                int maxColCursorIndex = Console.WindowWidth - 1;                        // -1 = Make it zero based

                // Processing according to pressed key

                // Backspace Key
                if (cki.Key == ConsoleKey.Backspace)
                {
                    if (colCursorIndex > 0)
                    {
                        lines[rowCursorIndex] = lines[rowCursorIndex].Substring(0, colCursorIndex - 1) + lines[rowCursorIndex].Substring(colCursorIndex, lines[rowCursorIndex].Length - colCursorIndex);
                        colCursorIndex--;
                    }
                    else if (rowCursorIndex > 0 && lines[rowCursorIndex - 1].Length == 0)
                    {
                        rowCursorIndex--;
                        lines.RemoveAt(rowCursorIndex);
                    }
                    else if (rowCursorIndex > 0 && lines[rowCursorIndex - 1].Length > 0)
                    {
                        colCursorIndex = lines[rowCursorIndex - 1].Length;
                        string aux = lines[rowCursorIndex];
                        lines.RemoveAt(rowCursorIndex);
                        rowCursorIndex--;
                        lines[rowCursorIndex] += aux;
                    }
                    else if (rowCursorIndex > 0)
                    {
                        rowCursorIndex--;
                        colCursorIndex = lines[rowCursorIndex].Length;
                    }
                }

                // Delete Key
                else if (cki.Key == ConsoleKey.Delete)
                {
                    if (colCursorIndex < lines[rowCursorIndex].Length)
                    {
                        lines[rowCursorIndex] = lines[rowCursorIndex].Substring(0, colCursorIndex) + lines[rowCursorIndex].Substring(colCursorIndex + 1, lines[rowCursorIndex].Length - colCursorIndex - 1);
                    }
                    else if (lines[rowCursorIndex].Length == 0)
                    {
                        if (rowCursorIndex < lines.Count - 1)
                        {
                            lines.RemoveAt(rowCursorIndex);
                        }
                    }
                    else if (colCursorIndex == lines[rowCursorIndex].Length)
                    {
                        if (rowCursorIndex < lines.Count - 1)
                        {
                            string aux = lines[rowCursorIndex + 1];
                            lines[rowCursorIndex] += aux;
                            lines.RemoveAt(rowCursorIndex + 1);
                        }
                    }
                }

                // Arrows
                else if (cki.Key == ConsoleKey.LeftArrow)
                {
                    if (colCursorIndex > 0)
                        colCursorIndex--;
                }
                else if (cki.Key == ConsoleKey.RightArrow)
                {
                    if (colCursorIndex < lines[rowCursorIndex].Length)
                        colCursorIndex++;
                }
                else if (cki.Key == ConsoleKey.UpArrow)
                {
                    if (rowCursorIndex > 0)
                    {
                        rowCursorIndex--;
                        if (colCursorIndex > lines[rowCursorIndex].Length)
                            colCursorIndex = lines[rowCursorIndex].Length;
                    }
                }
                else if (cki.Key == ConsoleKey.DownArrow)
                {
                    if (rowCursorIndex < Math.Min(lines.Count - 1, maxRowCursorIndex))
                    {
                        rowCursorIndex++;
                        if (colCursorIndex > lines[rowCursorIndex].Length)
                            colCursorIndex = lines[rowCursorIndex].Length;
                    }
                }
                else if (cki.Key == ConsoleKey.Home)
                {
                    colCursorIndex = 0;
                }
                else if (cki.Key == ConsoleKey.End)
                {
                    colCursorIndex = lines[rowCursorIndex].Length;
                }

                // Tab Key
                else if (cki.Key == ConsoleKey.Tab)
                {
                    if (colCursorIndex + 4 < maxColCursorIndex)
                    {
                        lines[rowCursorIndex] = lines[rowCursorIndex].Substring(0, colCursorIndex) + new String(' ', 4) + (colCursorIndex < lines[rowCursorIndex].Length ? lines[rowCursorIndex].Substring(colCursorIndex, lines[rowCursorIndex].Length - colCursorIndex) : "");
                        colCursorIndex += 4;
                    }
                }

                // Enter Key
                else if (cki.Key == ConsoleKey.Enter)
                {
                    if (rowCursorIndex < maxRowCursorIndex)
                    {
                        string aux = "";
                        if (colCursorIndex < lines[rowCursorIndex].Length)
                        {
                            aux = lines[rowCursorIndex].Substring(colCursorIndex, lines[rowCursorIndex].Length - colCursorIndex);
                            lines[rowCursorIndex] = lines[rowCursorIndex].Substring(0, colCursorIndex);
                        }

                        rowCursorIndex++;
                        colCursorIndex = 0;
                        lines.Insert(rowCursorIndex, aux);
                    }
                }

                // Capture some keys that we do not process
                else if ((cki.Key == ConsoleKey.X || cki.Key == ConsoleKey.C || cki.Key == ConsoleKey.V) && ((cki.Modifiers & ConsoleModifiers.Control) != 0))
                {
                    Console.Clear();
                    Console.Write("Use Mouse Right Click Actions for Copy/Paste.\nHit any key to continue...");
                    Console.ReadKey(true);
                }
                else if (cki.Key == ConsoleKey.Insert)
                {
                    Console.Clear();
                    Console.Write("Insert mode is always ON.\nHit any key to continue...");
                    Console.ReadKey(true);
                }
                else if (cki.Key == ConsoleKey.PageUp) { }
                else if (cki.Key == ConsoleKey.PageDown) { }
                else if (cki.Key == 0) { }

                // Execute Query
                else if ((cki.Key == ConsoleKey.E && ((cki.Modifiers & ConsoleModifiers.Control) != 0)))
                {
                    Console.SetCursorPosition(0, lines.Count + headerrows);

                    string query = "";

                    foreach (string l in lines)
                        query += (l + " ");

                    await CosmosDBhelper.ExecuteAsync(config, query);
                }

                // Escape - Exit Key
                else if (cki.Key == ConsoleKey.Escape)
                {
                    break;
                }

                // Text, Digit, etc. Add to text.
                else
                {
                    if (!char.IsControl(cki.KeyChar))
                        if (colCursorIndex < maxColCursorIndex)
                        {
                            lines[rowCursorIndex] = lines[rowCursorIndex].Substring(0, colCursorIndex) + cki.KeyChar.ToString() + (colCursorIndex < lines[rowCursorIndex].Length ? lines[rowCursorIndex].Substring(colCursorIndex, lines[rowCursorIndex].Length - colCursorIndex) : "");
                            colCursorIndex++;
                        }
                }

                // Screen Resized adjustment to buffer size and cursor location
                // Limit text buffers to the size of the console window - 1 (space for the cursor) (requred for when screen width is resized)
                // Adjust cursor locations
                lines[rowCursorIndex] = lines[rowCursorIndex].Substring(0, Math.Min(Console.WindowWidth - 1, lines[rowCursorIndex].Length));
                if (colCursorIndex > maxColCursorIndex)
                    colCursorIndex = maxColCursorIndex;
                if (rowCursorIndex > maxRowCursorIndex)
                    rowCursorIndex = maxRowCursorIndex;

                // Now Let's print the screen
                int maxCol = Console.WindowWidth;

                // Header (2 lines)
                Console.SetCursorPosition(0, 0);
                using (ConsoleColorContext ct = new ConsoleColorContext(ConsoleColor.White))
                    foreach (string s in header)
                        Console.Write(s.PadRight(maxCol).Substring(0, maxCol));

                // Print the text and complet all screen with blanks
                Console.SetCursorPosition(0, headerrows);
                using (ConsoleColorContext ct = new ConsoleColorContext(ConsoleColor.Green))
                    for (int i = 0; i <= maxRowCursorIndex; i++)
                        if (i < lines.Count)
                            Console.Write(lines[i].PadRight(maxCol).Substring(0, maxCol));
                        else
                            Console.Write("".PadRight(maxCol).Substring(0, maxCol));

                // In case there is more in the buffer than in the screen available size
                if (lines.Count - 1 > maxRowCursorIndex)
                {
                    Console.SetCursorPosition(maxCol - 7, maxRowCursorIndex + headerrows);
                    using (ConsoleColorContext ct = new ConsoleColorContext(ConsoleColor.Red))
                        Console.Write("more \u2193");
                }

                // Position the cursor the correct location
                Console.SetCursorPosition(colCursorIndex, rowCursorIndex + headerrows);

                // Read next keyboard key
                cki = Console.ReadKey(true);

            } while (true);
        }
    }

    internal static class CosmosDBhelper
    {
        public static async Task ExecuteAsync(Config config, string query)
        {
            // Required for the screen animation
            object lck = new object();

            try
            {
                // Connect to Cosmos DB and execute the query
                using (CosmosClient cosmosClient = CreateCosmosClient(config))
                {
                    Database database = cosmosClient.GetDatabase(config.Database);
                    Container container = database.GetContainer(config.Container);

                    QueryDefinition queryDefinition = new QueryDefinition(query);

                    using (FeedIterator feedIterator = container.GetItemQueryStreamIterator(queryDefinition, null, requestOptions: new QueryRequestOptions() { MaxItemCount = config.MaxItemstoFetch }))
                    {
                        while (feedIterator.HasMoreResults)
                        {
                            // Screen animation
                            Anime.DoAnime(lck);

                            lock (lck)
                                using (ConsoleColorContext ct = new ConsoleColorContext(ConsoleColor.Cyan))
                                    Console.Write("...Fetching (max:{0})...", config.MaxItemstoFetch);

                            using (ResponseMessage response = await feedIterator.ReadNextAsync())
                            {
                                if (!response.IsSuccessStatusCode)
                                {
                                    string msg = response.ErrorMessage;
                                    throw new Exception(String.Format("Error message first 1000 chars:\n{0}", msg.Substring(0, Math.Min(1000,msg.Length))));
                                }

                                using (StreamReader sr = new StreamReader(response.Content))
                                {
                                    string content = sr.ReadToEnd();

                                    JToken parsedJson = JToken.Parse(content);
                                    var beautified = parsedJson.ToString(Formatting.Indented);

                                    lock (lck)
                                        using (ConsoleColorContext ct = new ConsoleColorContext(ConsoleColor.Yellow))
                                            Console.WriteLine("\n{0}\n{1}\n{0}", new String('-', Console.WindowWidth), beautified);
                                }

                                Anime.StopAnime();

                                if (config.Metrics)
                                {
                                    JToken parsedJson = JToken.Parse(response.Diagnostics.ToString());
                                    IList<JToken> t = parsedJson.SelectTokens("$.....['Query Metrics']").ToList();

                                    Console.WriteLine("\nMetrics");
                                    foreach (string s in t)
                                        Console.WriteLine(s);
                                }
                            }

                            // If there are more results offer option to fetch more data or return to editor, if we are done offer option to return to editor
                            if (feedIterator.HasMoreResults)
                            {
                                lock (lck)
                                    using (ConsoleColorContext ct = new ConsoleColorContext(ConsoleColor.Green))
                                        Console.Write("\nPress any any key to fetch more data, ESC to go back to editor");

                                ConsoleKeyInfo cki = Console.ReadKey(true);
                                if (cki.Key == ConsoleKey.Escape)
                                    break;
                            }
                            else
                            {
                                lock (lck)
                                    using (ConsoleColorContext ct = new ConsoleColorContext(ConsoleColor.Green))
                                        Console.Write("\nQuery Completed. Press any key to return to the editor");

                                Console.ReadKey(true);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Anime.StopAnime();

                lock (lck)
                    using (ConsoleColorContext ct = new ConsoleColorContext(ConsoleColor.Red))
                    {
                        Console.WriteLine("\n\nError:\n");
                        Console.WriteLine(ex.Message.ToString());
                        Console.Write("\nPress any key to return to the editor");
                    }
                Console.ReadKey(true);
            }
            finally
            {
                Anime.StopAnime();
                Console.Clear();
            }
        }

        private static CosmosClient CreateCosmosClient(Config config)
        {
            CosmosClientOptions clientOptions = new Microsoft.Azure.Cosmos.CosmosClientOptions()
            {
                MaxRetryAttemptsOnRateLimitedRequests = 10,
                ConnectionMode = ("gateway" == config.ConnectionPolicy.ToLower() ? ConnectionMode.Gateway : ConnectionMode.Direct)
            };

            return new CosmosClient(
                        config.EndPoint,
                        config.Key,
                        clientOptions);
        }
    }

    internal static class Anime
    {
        private static Boolean cont = false;

        public static void DoAnime(object lck)
        {
            lock (lck)
            {
                if (cont == true)
                    return;

                cont = true;
            }

            Task.Run(() =>
            {
                int anim = 0;

                string[] anime = { "\\", "|", "/", "-" };

                int origRow = Console.CursorTop;
                int origCol = Console.CursorLeft;

                while (cont)
                {
                    anim += 1;

                    if (anim > 3)
                        anim = 0;

                    lock (lck)
                    {
                        origRow = Console.CursorTop;
                        origCol = Console.CursorLeft;

                        Console.Write("{0}", anime[anim]);
                        Console.SetCursorPosition(origCol, origRow);
                    }

                    Thread.Sleep(100);
                }
            });
        }

        public static void StopAnime()
        {
            cont = false;
        }
    }

    internal class ConsoleColorContext : IDisposable
    {
        ConsoleColor beforeContextForegroundColor;
        ConsoleColor beforeContextBackgroundColor;


        public ConsoleColorContext(ConsoleColor fgcolor)
        {
            this.beforeContextForegroundColor = Console.ForegroundColor;
            this.beforeContextBackgroundColor = Console.BackgroundColor;
            Console.ForegroundColor = fgcolor;
        }

        public ConsoleColorContext(ConsoleColor fgcolor, ConsoleColor bgcolor)
        {
            this.beforeContextForegroundColor = Console.ForegroundColor;
            this.beforeContextBackgroundColor = Console.BackgroundColor;
            Console.ForegroundColor = fgcolor;
            Console.BackgroundColor = bgcolor;
        }

        public void Dispose()
        {
            Console.ForegroundColor = this.beforeContextForegroundColor;
            Console.BackgroundColor = this.beforeContextBackgroundColor;
        }
    }
}