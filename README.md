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
- `swap <player1> <player2>`: swap 2 players in different teams/squads
- `kill <player> (reason)`: kill a player
- `curse <player> (reason)`: curse a player. cursed player will be killed on kill
- `uncurse <player>`: uncurse a cursed player
- `kick <player> (reason)`: kick a player from the server
- `ban <player> (reason)`: ban a player from the server
- `lookup <player|@id>`: find player in database

### Super Admin Commands
- `putgroup <player|@id> <group>`: change a players group

### FAQ
- Command Prefixes: `! @ /`
- Avaliable User Groups: `user`, `vip`, `admin`, `superadmin`, `owner`
