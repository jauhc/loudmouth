## Raw_Kit loudmouth for Counter-Strike: Global Offensive
A product of curiosity and boredom.

A kill/death announcer in chat for CSGO. Works by listening to the game's [Game State Intergration](https://developer.valvesoftware.com/wiki/Counter-Strike:_Global_Offensive_Game_State_Integration) updates.
Messages are sent to a config file which is executed when an event occurs, then cleared.

Powered by [CSGSI by rakijah](https://github.com/rakijah/CSGSI)

### Requirements
- CSGO
- .NET Framework of sorts - I wrote this with dotnet core so idk ¯\\_(ツ)_/¯

### Install & Usage
- Create `cheese.cfg` in your `{csgo path here}/cfg` folder, preferred if it was empty
- Set the path of `cheese.cfg` in the `config.txt` file
- `bind RSHIFT "exec cheese"` in CSGO console
- `dotnet run` to run
- `dotnet run dev` to run silently (outputs to terminal, not csgo)


### So how does it work?
- Program listens to events from the game sent to a specific port as POST requests
- Parses the json package to find if you got a kill or death
- Picks a random cheesy line from a list 
- Prepends "say" to it so it's a valid CSGO console command
- Writes it to file specified in `config.txt`
- Presses right shift to execute script

### Results
![results](https://github.com/jauhc/loudmouth/raw/master/rk_l.jpg)

### Disclaimer
There is *nothing* here that could trigger a ban, the low level keyboard input is nothing compared to random programs opening handles


_(Raw_Kit is a prefix I use for personal projects that last longer than a few nights)_
