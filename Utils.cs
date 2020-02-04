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
        persistent settings? could be stored as .cfg in game folder
        ACTUAL CONTROLS FOR OPTIONS?? <-- what was i thinking
        !req <youtube url>

*/
public class Settings
{
    public bool state = true;
    public bool owo = true;
    public bool kills = false;
    public bool killsRadio = true;
    public bool dmgReport = false;
    public bool deaths = false;
    public bool greets = false;
    public bool clanid = false;
    public bool clanfx = false;
}

public static class Utils
{

    static System.Timers.Timer speechTimer, clanTimer;
    public static readonly Random rng = new Random();
    public static readonly string cmdHash = terribleHash(14); // for command verification purposes
    public static bool _testing = false;
    public static bool _singlesMode = false;
    public static bool _puntualMode = false;
    public static bool pastaCooked = false;
    public static int gamePort = 1338;
    public static int netPort = 2121;
    public static String me = "0"; // holy FUCK solve this already
    public static CSGSI.Nodes.PlayerNode myNode;
    // could check gs.Provider.SteamID
    public static string myname = "";
    public static CSGSI.GameState gameState = new CSGSI.GameState("");
    public static List<CSGSI.Nodes.PlayerNode> players = new List<CSGSI.Nodes.PlayerNode>();
    public static List<string> teamMates = new List<string>();
    public static Settings settings = new Settings();
    public static PrimS.Telnet.Client client;
    public static PoopAudio.Audio audioPlayer = new PoopAudio.Audio();

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
    //public static Action<string> run = s => client.WriteLine($"{s}\n");
    public static void run(string s)
    {
        if (!settings.state)
            echo(s);
        client.WriteLine($"{s}\n");
    }

    /// <summary>
    /// Dumb wrapper for sleep method
    /// </summary>
    public static void sleep(int ms, int noise = 0)
    {
        if (noise > 0)
            ms += rng.Next(1, noise);
        if (ms > 0)
            System.Threading.Thread.Sleep(ms);
    }

    /// <summary>
    /// are we on windows?
    /// </summary>
    public static bool isWindows()
    {
        return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows);
    }

    static void initTimer()
    {
        // speechTimer is for speaking
        speechTimer = new System.Timers.Timer(800);
        speechTimer.Elapsed += speakBuffer;
        speechTimer.Enabled = false;

        // clanTimer is for clanid spam
        clanTimer = new System.Timers.Timer(483);
        clanTimer.Elapsed += clanSpam;
        clanTimer.Enabled = (settings.clanid || settings.clanfx);
    }

    private static int clanIdx = 0;
    private static bool clanState = true;
    public static void clanSpam(Object source = null, ElapsedEventArgs e = null)
    {
        if (!settings.clanfx)
        {
            if (clanIdx >= clanList.Count)
                clanIdx = 0;
            run($"cl_clanid {clanList[clanIdx++]}");
        }
        else if (settings.clanfx)
        {
            if (clanIdx == 0) clanState = true;
            else if (clanIdx + 1 >= clanList.Count) clanState = false;
            if (clanState) run($"cl_clanid {clanList[clanIdx++]}");
            else if (!clanState) run($"cl_clanid {clanList[clanIdx--]}");
        }
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
            run($"{thingsToSay.Dequeue()}");
        }
        if (thingsToSay.Count > 0) { speechTimer.Start(); speechTimer.Enabled = true; }
        if (thingsToSay.Count == 0) { speechTimer.Stop(); speechTimer.Enabled = true; }
    }

    /// <summary>
    /// Read config and shit, warns and stuff if somethings wrong
    /// WINDOWS ONLY
    /// </summary>
    static string readConfig()
    {
        if (!isWindows())
            return "notwindows";

        string blob = "\"loudmouth\"\n{\n\t\"uri\" \"http://localhost:" + gamePort + "\"\n\t\"timeout\" \"5.0\"\n\t\"buffer\"  \"0.05\"\n\t\"throttle\" \"0.1\"\n\t\"heartbeat\" \"3.0\"\n\t\"data\"\n\t{\n\t\t\"provider\"\t\t\t\t\t\"1\"\n\t\t\"map\"\t\t\t\t\t\t\"1\"\n\t\t\"round\"\t\t\t\t\t\t\"1\"\n\t\t\"player_id\"\t\t\t\t\t\"1\"\n\t\t\"player_weapons\"\t\t\t\"1\"\n\t\t\"player_match_stats\"\t\t\"1\"\n\t\t\"player_state\"\t\t\t\t\"1\"\n\t\t\"allplayers_id\"\t\t\t\t\"1\"\n\t\t\"allplayers_state\"\t\t\t\"1\"\n\t\t\"allplayers_match_stats\"\t\"1\"\n\t}\n}";
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
                    if (_testing) log($"{cfgFolder}\\config.cfg");
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

    static List<int> clanList = new List<int>();
    /// <summary>
    /// creates aliases for controls
    /// </summary>
    private static void createAliases()
    {
        // vvoooo groups 1-10
        clanList.AddRange(new[] { 7670261, 7670266, 7670268, 7670273, 7670276, 7670621, 7670634, 7670641, 7670647 });

        // TODO make toggles instead?
        run($"alias loud \"echo 0 LIST {cmdHash}\"");
        sleep(50, 16);

        run($"setinfo loud_state_o \"\"");
        run($"alias loud_state_off \"echo 0 STATE {cmdHash}\"");
        run($"alias loud_state_on \"echo 1 STATE {cmdHash}\"");
        sleep(50, 16);

        run($"setinfo loud_owo_o \"\"");
        run($"alias loud_owo_off \"echo 0 OWO {cmdHash}\"");
        run($"alias loud_owo_on \"echo 1 OWO {cmdHash}\"");
        sleep(50, 16);

        run($"setinfo loud_dmgreport_o \"\"");
        run($"alias loud_dmgreport_off \"echo 0 DMGREPORT {cmdHash}\"");
        run($"alias loud_dmgreport_on \"echo 1 DMGREPORT {cmdHash}\"");
        sleep(50, 16);

        run($"setinfo loud_kills_o \"\"");
        run($"alias loud_kills_off \"echo 0 KILLS {cmdHash}\"");
        run($"alias loud_kills_on \"echo 1 KILLS {cmdHash}\"");
        sleep(50, 16);

        run($"setinfo loud_killradio_o \"\"");
        run($"alias loud_killradio_off \"echo 0 KILLSRADIO {cmdHash}\"");
        run($"alias loud_killradio_on \"echo 1 KILLSRADIO {cmdHash}\"");
        sleep(50, 16);

        run($"setinfo loud_death_o \"\"");
        run($"alias loud_death_off \"echo 0 DETH {cmdHash}\"");
        run($"alias loud_death_on \"echo 1 DETH {cmdHash}\"");
        sleep(50, 16);

        run($"setinfo loud_greet_o \"\"");
        run($"alias loud_greet_off \"echo 0 GREET {cmdHash}\"");
        run($"alias loud_greet_on \"echo 1 GREET {cmdHash}\"");
        sleep(50, 16);

        run($"setinfo loud_clan_o \"\"");
        run($"alias loud_clan_off \"echo 0 CLAN {cmdHash}\"");
        run($"alias loud_clan_on \"echo 1 CLAN {cmdHash}\"");
        sleep(50, 16);

        run($"setinfo loud_clan_wave_o \"\"");
        run($"alias loud_clan_wave_off \"echo 0 CLANFX {cmdHash}\"");
        run($"alias loud_clan_wave_on \"echo 1 CLANFX {cmdHash}\"");
        sleep(50, 16);

        echo("Commands created!");
    }

    /// <summary>
    /// list of chat commands bla bla
    /// </summary>
    private static void chatCommand(string sender, string message, bool teamChat = false)
    {
        string sayCmd = "say ";
        if (teamChat) sayCmd = "say_team ";
        if (sender.Trim() == myname.Trim()) return; // could sleep but meh
        // if (sender.IndexOf(myname) > -1) return;
        message = message.ToLower();

        if (message.IndexOf("!help") > -1)
        {
            owo($"{sayCmd} commands available: !random, owo");
        }
        else if (message.IndexOf("!gg") > -1
        || message.IndexOf("!rs") > -1)
        {
            owo($"{sayCmd} unknown command, see commands available with !help");
        }
        else if (message.IndexOf("!random") > -1)
        {
            owo($"{sayCmd}{sender} you have rolled {rng.Next(1, 100)} (1-100)!");
        }
        else if (settings.owo)
        {
            if (message.IndexOf("owo") > -1 || message.IndexOf("uwu") > -1)
                owo(sayCmd + uwuPasta[rng.Next(0, uwuPasta.Count)]);
        }
        else if (settings.greets)
        {
            if (message.IndexOf("hi") > -1)
                owo(sayCmd + message);
            else if (message.IndexOf("hey") > -1)
                owo(sayCmd + message);
            else if (message.IndexOf("sup") > -1)
                owo(sayCmd + message);
            else if (message.IndexOf("hello") > -1)
                owo(sayCmd + message);
            else if (message.IndexOf("ty") > -1)
                owo(sayCmd + "np");
            else if (message.IndexOf("thanks") > -1)
                owo(sayCmd + "no problem");
        }
        /*
        else if (message.IndexOf("") > -1)
            owo("");
        */
    }

    /// <summary>
    /// checks when user sets commands from console
    /// TODO a PROPER settings manager instead of this shit
    /// </summary>
    private static void checkCvars(string[] data)
    {
        bool set = (data[0] == "1") ? true : false;
        switch (data[1])
        {
            // TODO prettify this pls? \t stuff?
            case "LIST":
                echo($"STATE = " + (settings.state ? "ON" : "OFF"));
                echo($"OWO = " + (settings.owo ? "ON" : "OFF"));
                echo($"DMGREPORT = " + (settings.dmgReport ? "ON" : "OFF"));
                echo($"KILLS = " + (settings.kills ? "ON" : "OFF"));
                echo($"KILLSRADIO = " + (settings.killsRadio ? "ON" : "OFF"));
                echo($"DEATHS = " + (settings.deaths ? "ON" : "OFF"));
                echo($"GREETS = " + (settings.greets ? "ON" : "OFF"));
                echo($"CLANS = " + (settings.clanid ? "ON" : "OFF"));
                echo($"CLANFX = " + (settings.clanid ? "ON" : "OFF"));
                break;

            case "STATE":
                settings.state = set;
                break;

            case "OWO":
                settings.owo = set;
                break;

            case "CLAN":
                settings.clanid = set;
                clanTimer.Enabled = set;
                run("cl_clanid 0");
                break;

            case "CLANFX":
                settings.clanfx = set;
                break;

            case "DMGREPORT":
                settings.dmgReport = set;
                break;

            case "KILLS":
                settings.kills = set;
                break;

            case "KILLSRADIO":
                settings.killsRadio = set;
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

    public const string msgCode = "‎ : "; // DONT TOUCH OR WE ALL DIE
    public const string uniqueCode = "‎"; // DONT TOUCH OR WE ALL DIE
    public static void chatParser(ref string data)
    {
        bool teamSay = false;
        if (data.IndexOf(msgCode) > -1)
        {
            var caller = data.Trim();
            var dial = caller.Split('\n');
            for (int i = 0; i < dial.Length; i++)
            {
                if (dial[i].IndexOf(msgCode) > -1)
                    caller = dial[i];
            }
            try
            {
                string message = caller.Substring(caller.IndexOf(msgCode) + 3).Trim();

                if (caller.IndexOf("Terrorist) ") > -1) teamSay = true;
                string sender = caller.Substring(0, caller.IndexOf(msgCode))
                .Replace("*DEAD*", "")
                .Replace("(Terrorist) ", "")
                .Replace("(Counter-Terrorist) ", "");
                if (teamSay)
                {
                    int atAt = sender.LastIndexOf("@");
                    if (sender.LastIndexOf("@") > 1)
                        sender = sender.Substring(0, atAt - 1);
                }
                if (sender.LastIndexOf(uniqueCode) > -1)
                    sender = sender.Substring(0, sender.Length - 3);
                string teamPrefix = teamSay ? "(TEAM)" : "";
                log(2, $"{teamPrefix} sender id [{sender}] says: {message}");
                if (message.Length == 0) return;
                chatCommand(sender, message, teamSay);
            }
            catch (System.Exception e)
            {
                log(1, $"{e}");
                throw;
            }
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
                Console.Write(rawOutput); // prints out everything
                if (settings.dmgReport)
                    damageDone(ref rawOutput); // testing
                chatParser(ref rawOutput);

                int hashIdx = rawOutput.IndexOf(cmdHash);
                if (hashIdx > -1)
                    checkCvars(rawOutput.Substring(0, hashIdx).Split(' '));
            }
        }
    }

    /// <summary>
    /// parses damage done
    /// </summary>
    public static void damageDone(ref string data)
    {
        string final = "";
        var first_idx = data.IndexOf("-------------------------\r\nDamage Given to ");
        //Console.WriteLine($"first idx {first_idx}");
        if (first_idx > -1)
        {
            var lined_output = data.Split(Environment.NewLine);
            for (int i = 1; i < lined_output.Length; i++)
            {
                // TODO get player name from output and check if alive via GSI
                // very simple if using sanitised names
                // also no friendly fire check
                var dmg = 0;
                var breaking_point1 = lined_output[i].IndexOf("Damage Taken");
                var breaking_point2 = lined_output[i + 1].IndexOf("-------------------------");
                //Console.WriteLine($"bb1: {breaking_point1} | bb2: {breaking_point2}");
                if (breaking_point1 > -1 && breaking_point2 > -1) break;
                if (isFriendHere(lined_output[i])) continue; // might cause issues in ffa dm
                var last_dash = lined_output[i].LastIndexOf('-');
                var split_line = lined_output[i].Substring(last_dash);
                var end_of_line = lined_output[i].IndexOf(" in ");
                if (lined_output[i] == "-------------------------") continue;
                //Console.WriteLine($"LINE NUMBER {i}: {split_line} dash@{last_dash} eol@{end_of_line}");
                //Console.WriteLine($"LATEST LINE: {lined_output[i]}");
                try
                {
                    dmg = int.Parse(lined_output[i].Substring(last_dash + 2, end_of_line - last_dash - 1));
                }
                catch (System.Exception e)
                {
                    throw e;
                }
                if (dmg > 99 || dmg < 1) continue;
                final += $"-{lined_output[i].Substring(last_dash + 2, end_of_line - last_dash - 1)}";
            }
            log($"\n\n[DMGDONETEST] output: {final}\n");
        }
    }

    /// <summary>
    /// Returns if the game is live
    /// </summary>
    public static bool bGameActive(ref CSGSI.GameState gs)
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
    /// audioplayer wrapper, ONLY USE AS THREAD
    /// </summary>
    public static void playSound(int[] freq, int[] len)
    {
        freq = freq ?? new int[] { 880, 587, 698, 880, 587, 698, 880, 1047, 988, 784, 698, 784, 880, 587, 523, 658, 587 };
        len = len ?? new int[] { 500, 1000, 500, 500, 1000, 500, 250, 250, 500, 500, 250, 250, 500, 500, 250, 250, 750 };

        PoopAudio.Audio.play(new PoopAudio.Tone(freq, len));
    }

    /// <summary>
    /// dumb
    /// </summary>
    public static string getCommit()
    {
        try
        {   // Open the text file using a stream reader.
            using (System.IO.StreamReader sr = new System.IO.StreamReader("./.git/ORIG_HEAD"))
            {
                // Read the stream to a string, and write the string to the console.
                string line = sr.ReadToEnd();
                return line.Substring(0, 7);
            }
        }
        catch (System.IO.IOException)
        {
            return "";
        }
    }

    /// <summary>
    /// Set style of console window and initialises variables
    /// </summary>
    public static void Init()
    {
        Console.Title = $"Project loudmouth ({getCommit()})";
        //Console.SetWindowSize(56, 15);
        //Console.SetBufferSize(56, 15);
        Console.ForegroundColor = ConsoleColor.White;
        string cfg = readConfig();
        if (cfg == "")
        {
            log(2, $"Could not find configuration");
            Environment.Exit(1);
        }
        else if (cfg == "notwindows")
        {
            log(1, "attempted to do windows specific config on nonwindows platform");
        }

        bool connected = false;
        while (!connected) // 30s timeout loop
        {
            try
            {
                client = new PrimS.Telnet.Client("localhost", netPort, new System.Threading.CancellationToken()); // blocking
                connected = true;
                break;
            }
            catch (Exception e)
            {
                log(1, "Could not connect to remote console... retrying...");
                if (_testing)
                    log(1, $"{e}");
            }
        }

        if (client.IsConnected) echo("RCON Connected!");
        me = getMyCommunityID();
        initTimer();
        cookPasta();
        createAliases();
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

        new Thread(() =>
        {
            playSound(new int[0], new int[0]); // should default to song of storms
        }).Start();

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
    public static void owo(string what)
    {
        thingsToSay.Enqueue(what);
        if (!speechTimer.Enabled)
        {
            speechTimer.Start();
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
        if (!isWindows())
        {
            log(1, "attempted to do windows specific registry read on nonwindows platform");
            return "";
        }
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
    /// saving for future use in chat commands
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