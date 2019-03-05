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
        static CSGSI.GameState oldState;
        static int oldAmmo = 0;
        static int playerKills = -64;
        static int roundHS = -64;
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
                if (args[i] == "p")
                    Utils._puntualMode = true;
            }
            // dont allow overlapping modes
            if (Utils._puntualMode && Utils._singlesMode) {
                Utils.log(1, "Overlapping modes! Fix your parameters.");
                Environment.Exit(1);
            }
            Utils.Init();
            gsl = new GameStateListener(1338);
            if (!gsl.Start()) Environment.Exit(0);
            gsl.NewGameState += OnNewGameState;

            if (gsl.Running) Utils.log(0, "loud mouth online!");
            if (Utils._testing) Utils.log(0, "dev mode on");
            // actual modes here, have them as `else if` so they dont mess with eachother
            if (Utils._singlesMode) Utils.log(0, "simple output mode on");
            else if (Utils._puntualMode) Utils.log(0, "punctual mode on");
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

        static bool isGun(CSGSI.Nodes.WeaponType w)
        {
            if (w == CSGSI.Nodes.WeaponType.Pistol
                || w == CSGSI.Nodes.WeaponType.Rifle
                || w == CSGSI.Nodes.WeaponType.SniperRifle
                || w == CSGSI.Nodes.WeaponType.SubmachineGun
                || w == CSGSI.Nodes.WeaponType.Shotgun
                || w == CSGSI.Nodes.WeaponType.MachineGun)
                return true;
            return false;
        }

        /// <summary>
        /// Kills and deaths are updated in this method
        /// </summary>
        static void OnNewGameState(GameState gs)
        {
            if (bGameActive(gs) && isLocalPlayer(gs))
            {
                if (((double)gs.Player.Weapons.ActiveWeapon.AmmoClip / (double)gs.Player.Weapons.ActiveWeapon.AmmoClipMax) < 0.3
                && (gs.Player.Weapons.ActiveWeapon.AmmoClip < oldState.Player.Weapons.ActiveWeapon.AmmoClip)
                && isGun(gs.Player.Weapons.ActiveWeapon.Type)
                && gs.Player.Weapons.ActiveWeapon.AmmoClipMax > 1)
                    Console.Beep(2334, 256);
                oldAmmo = gs.Player.Weapons.ActiveWeapon.AmmoClip;

                // round 1 start check thing
                if (gs.Round.Phase.ToString().ToLower() == "live" 
                && gs.Map.Round == 1 
                && (oldState.Map.Phase.ToString().ToLower() == "freezetime" 
                && oldState.Round.Phase.ToString().ToLower() == "freezetime"))
                {
                    Utils.owo($"Game start event");
                }

                if (playerKills == -64)
                    playerKills = gs.Player.MatchStats.Kills;
                var curkills = gs.Player.MatchStats.Kills;
                if (curkills > playerKills) {
                    for (int i = 0; i < (curkills - playerKills); i++) {
                        if (Utils._puntualMode) {
                            string o = $"Kill #{gs.Player.State.RoundKills} [Round {gs.Map.Round.ToString()}]";
                            if (gs.Player.State.RoundKillHS > roundHS) {
                                o += $" (hs)";
                            }
                            roundHS = gs.Player.State.RoundKillHS;
                            if (gs.Player.State.Flashed != 0)
                                o += $" / {Math.Round((double)(gs.Player.State.Flashed / (double)255) * 100,1)}% flashed";
                            if (gs.Player.State.Smoked != 0)
                                o += $" / {Math.Round((double)(gs.Player.State.Smoked / (double)255) * 100,1)}% smoked";
                            if (gs.Player.State.Burning != 0)
                                o += $" / {Math.Round((double)(gs.Player.State.Burning / (double)255) * 100,1)}% burning";
                            o += $"{Environment.NewLine} enemydown";
                            Utils.owo(o);
                        }
                        if (Utils._singlesMode)
                            Utils.owo("+" + Environment.NewLine + "enemydown");
                        else if (!Utils._singlesMode && !Utils._puntualMode)
                            onKill(gs);
                    }
                }
                playerKills = curkills;


                if (myDeaths == -1)
                    myDeaths = gs.Player.MatchStats.Deaths;
                var curDeaths = gs.Player.MatchStats.Deaths;
                if (curDeaths > myDeaths && !(gs.Map.Round == 1 && gs.Player.MatchStats.Deaths == 0))
                {
                    if (Utils._puntualMode) // todo
                        Utils.owo("oops i have died");
                    if (Utils._singlesMode)
                        Utils.owo("-");
                    else if (!Utils._singlesMode && !Utils._puntualMode)
                        onDeath(gs);
                }
                myDeaths = curDeaths;
            }
            oldState = gs;
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
            Utils.owo(dad + Environment.NewLine + "enemydown");
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