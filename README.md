# Description
A simple procon plugin that uses MySQL database the sync admins and penalities across multiple Battlefield Bad Company 2 servers

# Usage
- Place the .cs file in plugins/BFBC2 folder
- Put in your MySql database credentials
- Join any of your server and type `!iamowner`

## Supported Games
- Battlefield Bad Company 2

### User Commands
- `help`: show list of available commands
- `admins`: display admins online

### VIP Commands
- `killme`: commit a suicide

### Admin Commands
- `say <message>`: say a message as server
- `psay <player> <message>`: say a message to a player
- `yell <message>`: yell a message as server
- `pyell <player> <message>`: yell a message to a player
- `swapme <player>`: swap yourself with another player
- `swap <player1> <player2>`: swap 2 players in different teams/squads
- `kill <player> (reason)`: kill a player
- `curse <player> (reason)`: curse a player. cursed player will be killed on kill
- `uncurse <player>`: uncurse a cursed player
- `kick <player> (reason)`: kick a player from the server
- `ban <player> (reason)`: ban a player from the server
- `lookup <player|@id>`: find player in database
- `penalties (penalty-type)`: fetch last 5 penalties from database

### Super Admin Commands
- `putgroup <player|@id> <group>`: change a players group

### FAQ
- Command Prefixes: `! @ /`
- Required Arguments are denoted by [] and optional arguments are denoted by ()
- Available User Groups: `user`, `vip`, `admin`, `superadmin`, `owner`
- Available Penalty Types: `kill`, `curse`, `kick`, `ban`

### Known Issues
- [x] admins can kill/kick/ban each other
- [x] !lookup doesn't check for aliases
- [x] ban command can be used on self
- [x] !putgroup doesn't show any success response
- [x] !putgroup is case sensitive
