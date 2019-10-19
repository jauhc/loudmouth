using System;
using System.Collections.Generic;
using System.Timers;
using System.Text.RegularExpressions;
using System.Threading;

/*
Purpose of file:
    this file contains the
    small things that make this work
*/
/*
    todo:
        ACTUAL CONTROLS FOR OPTIONS??
        !commands
        !req <youtube url>

*/
public static class Utils
{
    public static PrimS.Telnet.Client client = new PrimS.Telnet.Client("localhost", 2121, new System.Threading.CancellationToken());
    static System.Timers.Timer timer1;
    static String configFile = "";
    public static readonly Random rng = new Random();
    public static bool _testing = false;
    public static bool _singlesMode = false;
    public static bool _puntualMode = false;
    public static String me = "0";
    public static CSGSI.Nodes.PlayerNode myNode;
    // could check gs.Provider.SteamID
    public static CSGSI.GameState gameState = new CSGSI.GameState("");
    public static List<CSGSI.Nodes.PlayerNode> players = new List<CSGSI.Nodes.PlayerNode>();
    public static List<string> teamMates = new List<string>();

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
    public static Action<string> echo = s => client.WriteLine($"echo {s}\n");
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
            log(0, thingsToSay.Dequeue());
            return;
        }
        run($"say {thingsToSay.Dequeue()}");
        if (thingsToSay.Count > 0) { timer1.Start(); timer1.Enabled = true; }
        if (thingsToSay.Count == 0) { timer1.Stop(); timer1.Enabled = true; }
    }

    /// <summary>
    /// Read config and shit, warns and stuff if somethings wrong
    /// The file config.txt overrides automatic detection
    /// </summary>
    static bool readConfig()
    {
        if (System.IO.File.Exists("config.txt"))
        {
            configFile = System.IO.File.ReadAllText("config.txt").Trim();
            if (System.IO.File.Exists(configFile))
                log(0, "cfg file found!");
            else
            {
                log(1, "config.txt found but cheese.cfg does not exist?");
                return false;
            }
        }

        var steamRegv = Microsoft.Win32.Registry.GetValue("HKEY_CURRENT_USER\\Software\\Valve\\Steam\\", "SteamPath", 0);
        var libraryFile = System.IO.File.ReadAllText($"{steamRegv}\\steamapps\\libraryfolders.vdf").Trim();
        var regx = new Regex("\".*?\"");
        var matches = regx.Matches(libraryFile);
        foreach (var item in matches)
        {
            if (item.ToString().Contains("\\"))
            {
                var path = item.ToString().Replace("\"", "");
                if (System.IO.File.Exists($"{path}\\steamapps\\appmanifest_730.acf"))
                {
                    // todo here:
                    // create a GSI config file if not already present
                    configFile = $"{path}\\steamapps\\common\\Counter-Strike Global Offensive\\csgo\\cfg\\cheese.cfg";
                    log(0, "Found install path with magic!");
                    if (_testing) log(0, configFile);
                    return true;
                }
            }
        }
        return false;
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


    private static void chatCommand(string sender, string message)
    {
        if (message.IndexOf("!help") > -1)
        {
            if (_testing)
            {
                echo("commands available: !random");
                return;
            }
            owo("commands available: !random");
        }
        else if (message.IndexOf("!random") > -1)
        {
            if (_testing)
            {
                echo($"{sender} you have rolled {rng.Next(1, 100)}!");
                return;
            }
            owo($"{sender} you have rolled {rng.Next(1, 100)}!");
        }
    }

    public static string msgCode = "‎ : "; // DONT TOUCH OR WE ALL DIE
    public static void chatParser(string data)
    {
        int codeAt = data.IndexOf(msgCode);
        if (codeAt > -1)
        {
            // to figure out: how to get rid of *DEAD*
            var caller = data.Trim();
            var dial = caller.Split('\n');
            for (int i = 0; i < dial.Length; i++)
            {
                if (dial[i].IndexOf(msgCode) > -1)
                    caller = dial[i];
            }
            var message = caller.Substring(caller.pooperFind(':') + 2);
            caller = caller.Substring(0, caller.pooperFind(':') - 4);

            log(2, $"caller id [{caller}] says: {message}");
            if (message.IndexOf("!roll") > -1)
                echo("not yet implemented lole!");
        }
    }

    /// <summary>
    /// atrocious
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
            }
        }
    }

    public static void damageDone(string data)
    {
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
                        if (Char.Parse(outputLines[i]) < 100 || Char.Parse(outputLines[i]) > 15)
                            final += $"-{outputLines[i]} ";
                    }
                }
            }
            echo(final);
            //thingsToSay.Enqueue(final); // "It just works" -Todd Howard
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
        if (!readConfig()) Environment.Exit(1);
        while (!client.IsConnected)
        {
            log(2, "Awaiting RCON connection...");
            sleep(2000);
        } // probably bad
        if (client.IsConnected) echo("[RCON CONNECTED!!]");
        me = getMyCommunityID();
        InitTimer();
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
        Utils.log(0, $"Found active userid! ({communityID})");
        return communityID;
    }

    /// <summary>
    /// making a multiline string printable etc
    /// </summary>
    public static string makePrintable(this string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);

        foreach (char i in s)
            if (i != '\n' && i != '\r' && i != '\t')
                sb.Append(i);

        return sb.ToString();
    }
}