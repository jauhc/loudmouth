using System;
using System.Collections.Generic;
using System.Timers;
using System.Text.RegularExpressions;
using System.Threading;

/*
Purpose of file:
    this file contains the small things that make this work
    remote console parser and its methods
*/
/*
    todo:
        ACTUAL CONTROLS FOR OPTIONS??
        !commands
        !req <youtube url>

*/
public class Settings
{
    public bool state = true;
    public bool owo = true;
    public bool kills = false;
    public bool deaths = false;
    public bool greets = true;
}
public static class Utils
{
    public static PrimS.Telnet.Client client = new PrimS.Telnet.Client("localhost", 2121, new System.Threading.CancellationToken());
    static System.Timers.Timer timer1;
    public static readonly Random rng = new Random();
    public static readonly string cmdHash = terribleHash(14); // for command verification purposes
    public static bool _testing = false;
    public static bool _singlesMode = false;
    public static bool _puntualMode = false;
    public static bool pastaCooked = false;
    public static String me = "0";
    public static CSGSI.Nodes.PlayerNode myNode;
    // could check gs.Provider.SteamID
    public static CSGSI.GameState gameState = new CSGSI.GameState("");
    public static List<CSGSI.Nodes.PlayerNode> players = new List<CSGSI.Nodes.PlayerNode>();
    public static List<string> teamMates = new List<string>();
    public static Settings settings = new Settings();

    /// <summary>
    /// A fancy log method, 0 = INFO, 1 = ERROR, 2 = WARN
    /// </summary>
    public static void log(byte mode, string data)
    {
        var t = DateTime.Now.ToString("HH':'mm':'ss");
        switch (mode)
        {
            case 1:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{t}] + {data}");
                break;
            case 2:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[{t}] * {data}");
                break;
            default:
                Console.WriteLine($"[{t}] - {data}");
                break;
        }
        Console.ForegroundColor = ConsoleColor.White;
    }

    /// <summary>
    /// simpler override for log
    /// </summary>
    public static void log(string data)
    {
        log(0, data);
    }
    public static Action<string> echo = s => client.WriteLine($"echo [Loudmouth] - {s}\n");
    public static Action<string> run = s => client.WriteLine($"{s}\n");

    /// <summary>
    /// Dumb wrapper for sleep method
    /// </summary>
    public static void sleep(int ms)
    {
        if (ms > 0)
            System.Threading.Thread.Sleep(ms);
    }

    static void InitTimer()
    {
        timer1 = new System.Timers.Timer(800);
        timer1.Elapsed += speakBuffer;
        timer1.Enabled = false;
    }

    static Queue<string> thingsToSay = new Queue<string>();
    /// <summary>
    /// Big method big purpose low readability
    /// </summary>
    public static void speakBuffer(Object source = null, ElapsedEventArgs e = null)
    {
        if (_testing)
        {
            echo($"{thingsToSay.Dequeue()}");
        }
        else
        {
            run($"say {thingsToSay.Dequeue()}");
        }
        if (thingsToSay.Count > 0) { timer1.Start(); timer1.Enabled = true; }
        if (thingsToSay.Count == 0) { timer1.Stop(); timer1.Enabled = true; }
    }

    /// <summary>
    /// Read config and shit, warns and stuff if somethings wrong
    /// </summary>
    static string readConfig()
    {
        string blob = "\"loudmouth\"\n{\n\t\"uri\" \"http://localhost:1338\"\n\t\"timeout\" \"5.0\"\n\t\"buffer\"  \"0.05\"\n\t\"throttle\" \"0.1\"\n\t\"heartbeat\" \"3.0\"\n\t\"data\"\n\t{\n\t\t\"provider\"\t\t\t\t\t\"1\"\n\t\t\"map\"\t\t\t\t\t\t\"1\"\n\t\t\"round\"\t\t\t\t\t\t\"1\"\n\t\t\"player_id\"\t\t\t\t\t\"1\"\n\t\t\"player_weapons\"\t\t\t\"1\"\n\t\t\"player_match_stats\"\t\t\"1\"\n\t\t\"player_state\"\t\t\t\t\"1\"\n\t\t\"allplayers_id\"\t\t\t\t\"1\"\n\t\t\"allplayers_state\"\t\t\t\"1\"\n\t\t\"allplayers_match_stats\"\t\"1\"\n\t}\n}";
        var steamRegv = Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\\Software\\Valve\\Steam\\", "SteamPath", 0);
        string libraryFile = System.IO.File.ReadAllText($"{steamRegv}\\steamapps\\libraryfolders.vdf").Trim();
        string configFile = "";
        Regex regx = new Regex("\".*?\"");
        var matches = regx.Matches(libraryFile);
        foreach (var item in matches)
        {
            if (item.ToString().Contains("\\"))
            {
                string path = item.ToString().Replace("\"", "");
                if (System.IO.File.Exists($"{path}\\steamapps\\appmanifest_730.acf"))
                {
                    string cfgFolder = $"{path}\\steamapps\\common\\Counter-Strike Global Offensive\\csgo\\cfg";
                    // sort of sanity check if config.cfg is present to make sure its the correct dir
                    configFile = $"{cfgFolder}\\gamestate_integration_loudmouth.cfg";
                    log(2, $"{cfgFolder}\\config.cfg");
                    if (System.IO.File.Exists($"{cfgFolder}\\settings_default.scr"))
                    {
                        if (!System.IO.File.Exists(configFile))
                            System.IO.File.WriteAllText(configFile, blob);
                        log("Found install path with magic!");
                        return cfgFolder;
                    }
                }
            }
        }
        return "";
    }

    /// <summary>
    /// load teammates
    /// </summary>
    public static void readMates(CSGSI.Events.RoundPhaseChangedEventArgs e)
    {
        players.Clear();
        players.ForEach(p =>
        {
            if (myNode.Team == p.Team)
                teamMates.Add(p.Name);
        });
    }


    /// <summary>
    /// attempts to find friendly name from string given
    /// </summary>
    public static bool isFriendHere(string toParse)
    {
        for (byte i = 0; i < teamMates.Count; i++)
        {
            if (toParse.IndexOf(teamMates[i]) > -1)
                return true;
        }
        return false;
    }

    /// <summary>
    /// bad find
    /// </summary>
    private static int pooperFind(this string s, char t)
    {
        int poop = s.IndexOf(t);
        if (poop > -1)
            return poop;
        return s.Length;
    }

    /// <summary>
    /// bad reverse find
    /// </summary>
    private static int poopyFind(this string s, char t)
    {
        int poop = s.LastIndexOf(t);
        if (poop > -1)
            return poop;
        return s.Length;
    }

    private static List<string> uwuPasta = new List<string>();

    /// <summary>
    /// i must wash hands before i attend to cooking
    /// </summary>
    private static void cookPasta()
    {
        uwuPasta.AddRange(new[] {
            "rawr x3 nuzzles how are you",
            "pounces on you you're so warm",
            "o3o notices you have a bulge o: someone's happy ;)",
            "nuzzles your necky wecky~ murr~ hehehe",
            "rubbies your bulgy wolgy you're so big :oooo",
            "rubbies more on your bulgy wolgy it doesn't stop growing ·///·",
            "kisses you and lickies your necky daddy likies (;",
            "nuzzles wuzzles I hope daddy really likes $:",
            "wiggles butt and squirms I want to see your big daddy meat~",
            "wiggles butt I have a little itch o3o",
            "wags tail can you please get my itch~",
            "puts paws on your chest nyea~",
            "its a seven inch itch rubs your chest can you help me pwease",
            "squirms pwetty pwease sad face I need to be punished",
            "runs paws down your chest and bites lip like I need to be punished really good~",
            "paws on your bulge as I lick my lips I'm getting thirsty",
            "I can go for some milk unbuttons your pants as my eyes glow you smell so musky :v",
            "licks shaft mmmm~ so musky drools all over your cock your daddy meat",
            "I like fondles Mr. Fuzzy Balls hehe puts snout on balls and inhales deeply",
            "oh god im so hard~ licks balls punish me daddy~",
            "nyea~ squirms more and wiggles butt I love your musky goodness",
            "bites lip please punish me licks lips nyea~",
            "suckles on your tip so good licks pre of your cock salty goodness~",
            "eyes role back and goes balls deep mmmm~ moans and suckles"
        });
        pastaCooked = true;
    }

    /// <summary>
    /// creates aliases for controls
    /// </summary>
    private static void createAliases(string path)
    {
        run($"alias loud \"echo 0 LIST {cmdHash}\"");
        sleep(300);

        run($"setinfo loud_owo_o \"\"");
        run($"alias loud_owo_off \"echo 0 OWO {cmdHash}\"");
        run($"alias loud_owo_on \"echo 1 OWO {cmdHash}\"");
        sleep(300);

        run($"setinfo loud_kills_o \"\"");
        run($"alias loud_kills_off \"echo 0 KILLS {cmdHash}\"");
        run($"alias loud_kills_on \"echo 1 KILLS {cmdHash}\"");
        sleep(300);

        run($"setinfo loud_death_o \"\"");
        run($"alias loud_death_off \"echo 0 DETH {cmdHash}\"");
        run($"alias loud_death_on \"echo 1 DETH {cmdHash}\"");
        sleep(300);

        run($"setinfo loud_greet_o \"\"");
        run($"alias loud_greet_off \"echo 0 GREET {cmdHash}\"");
        run($"alias loud_greet_on \"echo 1 GREET {cmdHash}\"");
        sleep(300);

        echo("Commands created!");
    }

    /// <summary>
    /// list of chat commands bla bla
    /// </summary>
    private static void chatCommand(string sender, string message)
    {
        if (message.IndexOf("!help") > -1)
        {
            owo("commands available: !random");
        }
        else if (message.IndexOf("!random") > -1)
        {
            owo($"{sender} you have rolled {rng.Next(1, 100)} (1-100)!");
        }
        else if (settings.owo)
        {
            if (message.IndexOf("owo") > -1 || message.IndexOf("uwu") > -1)
                owo(uwuPasta[rng.Next(0, uwuPasta.Count)]);
        }
        else if (settings.greets)
        {
            if (message.IndexOf("hi") > -1)
                owo(message);
            else if (message.IndexOf("hey") > -1)
                owo(message);
            else if (message.IndexOf("sup") > -1)
                owo(message);
            else if (message.IndexOf("hello") > -1)
                owo(message);
            else if (message.IndexOf("ty") > -1)
                owo("np");
            else if (message.IndexOf("thanks") > -1)
                owo("no problem");
        }
        /*
        else if (message.IndexOf("") > -1)
            owo("");
        */
    }

    /// <summary>
    /// checks when user sets commands from console
    /// </summary>
    private static void checkCvars(string[] data)
    {
        bool set = (data[0] == "1") ? true : false;
        switch (data[1])
        {
            case "LIST":
                echo($"OWO = " + (settings.owo ? "ON" : "OFF"));
                echo($"KILLS = " + (settings.kills ? "ON" : "OFF"));
                echo($"DEATHS = " + (settings.deaths ? "ON" : "OFF"));
                echo($"GREETS = " + (settings.greets ? "ON" : "OFF"));
                break;

            case "OWO":
                settings.owo = set;
                break;

            case "KILLS":
                settings.kills = set;
                break;

            case "DETH":
                settings.deaths = set;
                break;

            case "GREET":
                settings.greets = set;
                break;

            default:
                echo("somehow you broke the settings?");
                break;
        }
        if (data[1] != "LIST")
            echo(set ? $"{data[1]} enabled!" : $"{data[1]} disabled!");
    }

    public static string msgCode = "‎ : "; // DONT TOUCH OR WE ALL DIE
    public static void chatParser(string data)
    {
        if (data.IndexOf(msgCode) > -1)
        {
            var caller = data.Trim();
            var dial = caller.Split('\n');
            for (int i = 0; i < dial.Length; i++)
            {
                if (dial[i].IndexOf(msgCode) > -1)
                    caller = dial[i];
            }
            var message = caller.Substring(caller.pooperFind(':') + 2);
            caller = caller.Substring(0, caller.pooperFind(':') - 4).Trim();
            caller = caller.Replace("*DEAD*", "");
            caller = caller.Replace("(Terrorist) ", "");
            caller = caller.Replace("(Counter-Terrorist) ", "");
            if (caller.IndexOf(me) > -1)
                return;

            // make self check ignore, "status" in console breaks this
            log(2, $"caller id [{caller}] says: {message}");
            chatCommand(caller, message);
        }
    }

    /// <summary>
    /// atrocious - but it works
    /// </summary>
    public static async void rconParser()
    {
        while (client.IsConnected)
        {
            string rawOutput = await client.ReadAsync();

            if (rawOutput.Length > 0)
            {
                Console.Write(rawOutput);
                // add check here for IF WE WANT TO TELL DMG DONE
                damageDone(rawOutput);
                chatParser(rawOutput);

                int hashIdx = rawOutput.IndexOf(cmdHash);
                if (hashIdx > -1)
                    checkCvars(rawOutput.Substring(0, hashIdx).Split(' '));
            }
        }
    }

    /// <summary>
    /// parses damage done
    /// </summary>
    public static void damageDone(string data)
    {
        return; // broken for now - suggestion: REDO
        int indexOne = data.IndexOf(" - Damage Given\r\n-------------------------"); // 13
        if (indexOne > -1)
        {
            data = data.Substring(indexOne + 44);
            string[] outputLines = data.Split("\r\n");
            string final = "";
            for (ushort i = 0; i < outputLines.Length - 1; i++)
            {
                if (isFriendHere(outputLines[i]))
                    continue;
                int indexTwo = outputLines[i].LastIndexOf("-");
                if (indexTwo > -1)
                {
                    if (outputLines[i].IndexOf("hit") > -1)
                    {
                        outputLines[i] = outputLines[i].Substring(indexTwo + 2);
                        outputLines[i] = outputLines[i].Substring(0, outputLines[i].IndexOf(" "));
                        try
                        {
                            if (Int32.Parse(outputLines[i]) < 100 || Int32.Parse(outputLines[i]) > 15)
                                final += $"-{outputLines[i]} ";
                        }
                        catch (System.Exception e)
                        {
                            log(1, $"{e}");
                        }
                    }
                }
            }
            echo(final);
            //owo(final); // "It just works" -Todd Howard
        }
    }

    /// <summary>
    /// Returns if the game is live
    /// </summary>
    public static bool bGameActive(CSGSI.GameState gs)
    {
        try
        {
            return ((gs.Round.Phase.ToString().ToLower() == "live"
            || gs.Round.Phase.ToString().ToLower() == "over")
            && (gs.Map.Phase.ToString().ToLower() == "live"
            || gs.Map.Phase.ToString().ToLower() == "intermission"));
        }
        catch (System.Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Set style of console window and initialises variables
    /// </summary>
    public static void Init()
    {
        Console.Title = "Project loudmouth";
        //Console.SetWindowSize(56, 15);
        //Console.SetBufferSize(56, 15);
        Console.ForegroundColor = ConsoleColor.White;
        string cfg = readConfig();
        if (cfg == "") Environment.Exit(1);
        while (!client.IsConnected)
        {
            log(2, "Awaiting RCON connection...");
            sleep(2000);
        } // probably bad
        if (client.IsConnected) echo("RCON Connected!");
        me = getMyCommunityID();
        InitTimer();
        cookPasta();
        createAliases(cfg);
        Thread logger = new Thread((new ThreadStart(rconParser)));
        logger.Start();
        /* 
        if (bGameActive(gameState))
        {
            players.AddRange(gameState.AllPlayers.PlayerList);
            players.ForEach(p =>
            {
                if (p.SteamID == me)
                    myNode = p;
            });
        }*/

        if (_testing)
        {
            Console.Title = $"[dev] {Console.Title} ({me})";
            log(0, "test string 1");
            log(1, "test string 2");
            log(2, "test string 3");
        }
    }

    /// <summary>
    /// Method that is called when string is generated and must be said
    /// </summary>
    public static void owo(String what)
    {
        thingsToSay.Enqueue(what);
        if (!timer1.Enabled)
        {
            timer1.Start();
            speakBuffer();
        }
    }

    /// <summary>
    /// Purpose: Getting steam3id from registry value, then convert it to communityid format
    /// todo: what if the registry value does not exist?
    /// todo: some kind of check if the user changed steam accounts while running this application
    /// </summary>
    static String getMyCommunityID()
    {
        // sort of found this method by accident, nothing on google about this!
        var steam3ID = Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\\Software\\Valve\\Steam\\ActiveProcess", "ActiveUser", 0);
        if (steam3ID == null)
        {
            Utils.log(1, "Failed to find steam user in registry!");
            return "0";
        }
        String communityID = "7656" + (Convert.ToInt32(steam3ID) + 1197960265728).ToString();
        log($"Found active userid! ({communityID})");
        return communityID;
    }

    /// <summary>
    /// (obsolete) making a multiline string printable etc
    /// </summary>
    public static string makePrintable(this string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);

        foreach (char i in s)
            if (i != '\n' && i != '\r' && i != '\t')
                sb.Append(i);

        return sb.ToString();
    }

    /// <summary>
    /// terrible hash method in case if streamer uses so sniper cant mess with user
    /// </summary>
    public static string terribleHash(int length)
    {
        string characters = "iIl1|!o0OS5B8";
        System.Text.StringBuilder result = new System.Text.StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            result.Append(characters[rng.Next(characters.Length)]);
        }
        return result.ToString();
    }
}