using System;
using System.Collections.Generic;
using CSGSI;
using CSGSI.Events;
using System.Runtime.InteropServices;
using System.Timers;

/* Main file */

namespace loudmouth
{
    class Program
    {
        static CSGSI.GameStateListener gsl;
        static int playerKills = -1;
        static int myDeaths = -1;
        static Random rng = new Random();

        /// <summary>
        /// Insert point, launch paramters; dev = log to console only, s = simple mode
        /// </summary>
        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "dev")
                    Utils._testing = true;
                if (args[i] == "s")
                    Utils._singlesMode = true;
            }
            Utils.Init();
            gsl = new GameStateListener(1338);
            if (!gsl.Start()) Environment.Exit(0);
            gsl.NewGameState += OnNewGameState;

            if (gsl.Running) Utils.log(0, "loud mouth online!");
            if (Utils._singlesMode) Utils.log(0, "simple output mode on");
        }

        /// <summary>
        /// Returns if the game is live
        /// </summary>
        static bool bGameActive(GameState gs)
        {
            return ((gs.Round.Phase.ToString().ToLower() == "live"
            || gs.Round.Phase.ToString().ToLower() == "over")
            && (gs.Map.Phase.ToString().ToLower() == "live"
            || gs.Map.Phase.ToString().ToLower() == "intermission"));
        }

        static bool isLocalPlayer(GameState gs)
        {
            return gs.Player.SteamID.ToString() == Utils.me;
        }

        /// <summary>
        /// Kills and deaths are updated in this method
        /// </summary>
        static void OnNewGameState(GameState gs)
        {
            if (bGameActive(gs) && isLocalPlayer(gs))
            {
                if (playerKills == -1)
                    playerKills = gs.Player.MatchStats.Kills;
                var curkills = gs.Player.MatchStats.Kills;
                if (curkills > playerKills)
                {
                    for (int i = 0; i < (curkills - playerKills); i++)
                    {
                    if (Utils._singlesMode)
                        Utils.owo("+" + Environment.NewLine + "enemydown");
                    else if (!Utils._singlesMode)
                        onKill(gs);
                    }
                }
                playerKills = curkills;


                if (myDeaths == -1)
                    myDeaths = gs.Player.MatchStats.Deaths;
                var curDeaths = gs.Player.MatchStats.Deaths;
                if (curDeaths != myDeaths && !(gs.Map.Round == 1 && gs.Player.MatchStats.Deaths == 0))
                {
                    if (Utils._singlesMode)
                        Utils.owo("-");
                    else if (!Utils._singlesMode)
                        onDeath(gs);
                }
                myDeaths = curDeaths;
            }
        }

        /// <summary>
        /// We got a kill! Time to tell entire server about it!
        /// </summary>
        static void onKill(GameState gs)
        {
            var cheese = new List<string>();
            if (gs.Player.State.Smoked > 32)
            {
                cheese.AddRange(new[] {
                "~ from within the smoke ~",
                "puff puff im in the smokes",
                "really cloudy here tbh",
                "i could barely see anything here wtf",
                "jesus - take the mouse"
                });
            }
            else if (gs.Player.State.Flashed > 16)
            {
                cheese.AddRange(new[] {
                "LOL i was blind",
                "owned while flashed lmao",
                "ez blind kills",
                "sit down whoever you are because im BLIND"
                });
            }
            else
            {
                cheese.AddRange(new[] {
                "sit down",
                "later",
                "hey, how about a break?",
                "you alright?",
                "hit or miss? guess i never miss, huh?",
                "ez",
                "ezpz",
                "you just got dabbed on!",
                $"how u like the taste of my {gs.Player.Weapons.ActiveWeapon.Name.Substring(7)}?",
                "owned",
                "ownd",
                "whats happening with you",
                "get pooped on",
                });
            }

            String dad = cheese[rng.Next(cheese.Count)];
            if (rng.Next(100) > 50) // 50% chance
            {
                var postfix = new List<string> {
                    " kid",
                    " kiddo",
                    " nerd",
                    " geek"
                    };
                dad += postfix[rng.Next(postfix.Count)];
            }
            Utils.owo(dad);
        }

        /// <summary>
        /// You died and its not your fault
        /// </summary>
        static void onDeath(GameState gs)
        {
            var cheese = new List<string>();
            if (gs.Player.State.Flashed > 16)
            {
                cheese.AddRange(new[] {
                    "i was BLIND",
                    "how do i shoot blind?",
                    "oops i was flashed",
                    "help i cant see",
                    "why is my screen white"
                    });
            }
            else
            {
                cheese.AddRange(new[] {
                "oops",
                "i meant to do that :)",
                "wtf lag",
                "i was looking at the map",
                "excuse me?",
                "oh",
                "i was tabbed out :(",
                "fricking tickrate"
                    });
            }
            String dad = cheese[rng.Next(cheese.Count)];
            Utils.owo(dad);
        }

    }
}