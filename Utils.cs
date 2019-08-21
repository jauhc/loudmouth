using System;
using System.Collections.Generic;
using System.Timers;
using System.Text.RegularExpressions;
using System.Threading;
using PrimS.Telnet;

/*
Purpose of file:
    this file contains the
    small things that make this work
 */

/*
   todo:
   move from keyboard event to telnet
 */
public static class Utils
{
    static System.Threading.CancellationToken ct = new System.Threading.CancellationToken();
    public static PrimS.Telnet.Client client = new PrimS.Telnet.Client("localhost", 2121, ct);
    static System.Timers.Timer timer1;
    static String configFile = "";
    public static bool _testing = false;
    public static bool _singlesMode = false;
    public static bool _puntualMode = false;
    public static String me = "0";
    // could check gs.Provider.SteamID

    /// <summary>
    /// A fancy log method, 0 = INFO, 1 = ERROR, 2 = WARN
    /// </summary>
    public static void log(byte mode, string data)
    {
        var t = DateTime.Now.ToString("HH':'mm':'ss");
        if (mode == 1)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{t}] + {data}");
        }
        else if (mode == 2)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{t}] * {data}");
        }
        else
        {
            Console.WriteLine($"[{t}] - {data}");
        }
        Console.ForegroundColor = ConsoleColor.White;
    }
    public static Action<string> echo = s => client.WriteLine($"echo {s}");
    public static Action<string> run = s => client.WriteLine(s);

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
        timer1 = new System.Timers.Timer(778);
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
    /// Set style of console window and initialises variables
    /// </summary>
    public static void Init()
    {
        Console.Clear();
        Console.Title = "Project loudmouth";
        Console.SetWindowSize(56, 15);
        Console.SetBufferSize(56, 15);
        Console.ForegroundColor = ConsoleColor.White;
        if (!readConfig()) Environment.Exit(1);
        while (!client.IsConnected)
        {
            log(2, "Awaiting RCON connection...");
            sleep(2000);
        } // probably bad
        if (client.IsConnected) log(0, "RCON Connected!");
        me = getMyCommunityID();
        InitTimer();
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