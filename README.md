## Project loudmouth for Counter-Strike: Global Offensive
A product of curiosity and boredom.

A kill/death announcer in chat for CSGO. Works by listening to the game's [Game State Intergration](https://developer.valvesoftware.com/wiki/Counter-Strike:_Global_Offensive_Game_State_Integration) updates.

Powered by [CSGSI by rakijah](https://github.com/rakijah/CSGSI)

### Prerequisites
- CSGO
- .NET Core Runtime

### Install & Usage
- Running .NET Core binaries requires some sort of .NET runtime unless statically compiled
- `-netconport 2121` to CS:GO launch options
- `dotnet run` to run
- `dotnet run dev` to run silently (outputs to terminal, not csgo)


### So how does it work?
- Program listens to events from the game sent to a specific port as POST requests
- Parses the json package to find if you got a kill or death
- Picks a random cheesy line from a list 
- Prepends "say" to it so it's a valid CSGO console command
- Sends generated text to remote console for execution

### Results
![results](https://github.com/jauhc/loudmouth/raw/master/rk_l.jpg)

### todo list
- the whole multikill jazz
- proper text parsing from console (damage done)

### optional features i wont prioritise
- finding configs on linux

### if you *really* need this on linux heres what you need to do
- `getMyCommunityID()` in `Utils.cs` should return your communityid (needed for chat parsing or something i forget ;))
- read and sort out `readConfig()` in `Utils.cs`


### Disclaimer
There is *nothing* here that could trigger a ban, the remote console is Valve's own addition to the Source engine.