/*	InfinityPlugin.cs -  Procon Plugin [BC2]

	Version: 0.0.0.1

	Code Credit:
	PapaCharlie9  -  Basic Plugin Template Part (BasicPlugin.cs)
	maxdralle -  MySQL Main Functions (VipSlotManager.cs)
	MorpheusX(AUT) - MySQL Functions (CRemoteBanlist.cs)R

	This plugin file is part of PRoCon Frostbite.

	This plugin is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This plugin is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with PRoCon Frostbite.  If not, see <http://www.gnu.org/licenses/>.

*/

using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices;
using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;
using PRoCon.Core.Events;

using MySql.Data.MySqlClient;

namespace PRoConEvents
{
    using CapturableEvent = CapturableEvents;

    public class InfinityPlugin : PRoConPluginAPI, IPRoConPluginInterface
    {

        //////////////////////
        #region Variables & Initialisation
        //////////////////////
        
        private bool _fIsEnabled;
        private int _fDebugLevel;

        private bool _tableExists;
        private readonly Dictionary<string, CPlayerInfo> _mDicPlayerInfo;
        private readonly Dictionary<string, AuthSoldier> _mAuthPlayerInfo;
        private readonly Dictionary<string, string> _mUserCommands;
        private readonly Dictionary<string, string> _mAdminCommands;
        private readonly List<string> _lCursedPlayers;
        private readonly List<string> _lAdminsOnline;
        private CServerInfo _serverInfo;

        private string _settingStrSqlHostname;
        private string _settingStrSqlPort;
        private string _settingStrSqlDatabase;
        private string _settingStrSqlUsername;
        private string _settingStrSqlPassword;
        
        private readonly string _newLiner;
        private int _settingYellDuring;

        private string _cmdHelp;
        private string _cmdAdminsOnline;

        private string _cmdSay;
        private string _cmdPlayerSay;
        private string _cmdYell;
        private string _cmdPlayerYell;
        private string _cmdSwap;
        private string _cmdCurse;
        private string _cmdUncurse;
        private string _cmdKill;
        private string _cmdKick;
        private string _cmdBan;
        private string _cmdConfirm;

        public InfinityPlugin()
        {
            _fIsEnabled = false;
            _fDebugLevel = 1;

            _tableExists = false;
            _mDicPlayerInfo = new Dictionary<string, CPlayerInfo>();
            _mAuthPlayerInfo = new Dictionary<string, AuthSoldier>();
            _mUserCommands = new Dictionary<string, string>();
            _mAdminCommands = new Dictionary<string, string>();
            _lCursedPlayers = new List<string>();
            _lAdminsOnline = new List<string>();
            _serverInfo = new CServerInfo();

            _settingStrSqlHostname = string.Empty;
            _settingStrSqlPort = "3306";
            _settingStrSqlDatabase = string.Empty;
            _settingStrSqlUsername = string.Empty;
            _settingStrSqlPassword = string.Empty;

            _newLiner = "";
            _settingYellDuring = 5;

            _cmdHelp = "help";
            _cmdAdminsOnline = "admins";
            
            _cmdSay = "say";
            _cmdPlayerSay = "psay";
            _cmdYell = "yell";
            _cmdPlayerYell = "pyell";
            _cmdCurse = "curse";
            _cmdUncurse = "uncurse";
            _cmdKill = "kill";
            _cmdKick = "kick";
            _cmdSwap = "swap";
            _cmdBan = "ban";
            _cmdConfirm = "yes";
        }
        
        #endregion

        //////////////////////
        #region BasicPlugin.cs part by PapaCharlie9@gmail.com
        //////////////////////

        private enum MessageType { Warning, Error, Exception, Normal }

        private string FormatMessage(string msg, MessageType type)
        {
            string prefix = "[^b" + GetPluginName() + "^n] ";

            switch (type)
            {
                case MessageType.Warning:
                    prefix += "^1^bWARNING^0^n: ";
                    break;
                case MessageType.Error:
                    prefix += "^1^bERROR^0^n: ";
                    break;
                case MessageType.Exception:
                    prefix += "^1^bEXCEPTION^0^n: ";
                    break;
                case MessageType.Normal:
                    break;

                default:
                    prefix += "";
                    return prefix;
            }

            return prefix + msg;
        }

        private void LogWrite(string msg)
        {
            ExecuteCommand("procon.protected.pluginconsole.write", msg);
        }

        private void ConsoleWrite(string msg, MessageType type)
        {
            LogWrite(FormatMessage(msg, type));
        }

        private void ConsoleWrite(string msg)
        {
            ConsoleWrite(msg, MessageType.Normal);
        }

        private void ConsoleWarn(string msg)
        {
            ConsoleWrite(msg, MessageType.Warning);
        }

        private void ConsoleError(string msg)
        {
            ConsoleWrite(msg, MessageType.Error);
        }

        public void ConsoleException(string msg)
        {
            ConsoleWrite(msg, MessageType.Exception);
        }

        private void DebugWrite(string msg, int level)
        {
            if (_fDebugLevel >= level) ConsoleWrite(msg, MessageType.Normal);
        }

        public void ServerCommand(params string[] args)
        {
            List<string> list = new List<string> {"procon.protected.send"};
            list.AddRange(args);
            ExecuteCommand(list.ToArray());
        }

        #endregion

        //////////////////////
        #region PLUGIN Details
        //////////////////////

        public List<CPluginVariable> GetPluginVariables()
        {
            return GetDisplayPluginVariables();
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>
            {
                new CPluginVariable("MySQL Details|Host", _settingStrSqlHostname.GetType(), _settingStrSqlHostname),
                new CPluginVariable("MySQL Details|Port", _settingStrSqlPort.GetType(), _settingStrSqlPort),
                new CPluginVariable("MySQL Details|Database", _settingStrSqlDatabase.GetType(), _settingStrSqlDatabase),
                new CPluginVariable("MySQL Details|Username", _settingStrSqlUsername.GetType(), _settingStrSqlUsername),
                new CPluginVariable("MySQL Details|Password", _settingStrSqlPassword.GetType(), _settingStrSqlPassword),
                new CPluginVariable("User Commands|Help Menu", _cmdHelp.GetType(), _cmdHelp),
                new CPluginVariable("User Commands|View Admins Online", _cmdAdminsOnline.GetType(), _cmdAdminsOnline),
                new CPluginVariable("Admin Commands|Say", _cmdSay.GetType(), _cmdSay),
                new CPluginVariable("Admin Commands|Playersay", _cmdPlayerSay.GetType(), _cmdPlayerSay),
                new CPluginVariable("Admin Commands|Yell", _cmdYell.GetType(), _cmdYell),
                new CPluginVariable("Admin Commands|Playeryell", _cmdPlayerYell.GetType(), _cmdPlayerYell),
                new CPluginVariable("Admin Commands|Swap", _cmdSwap.GetType(), _cmdSwap),
                new CPluginVariable("Admin Commands|Kill", _cmdKill.GetType(), _cmdKill),
                new CPluginVariable("Admin Commands|Curse", _cmdCurse.GetType(), _cmdCurse),
                new CPluginVariable("Admin Commands|Uncurse", _cmdUncurse.GetType(), _cmdUncurse),
                new CPluginVariable("Admin Commands|Kick", _cmdKick.GetType(), _cmdKick),
                new CPluginVariable("Admin Commands|Ban", _cmdBan.GetType(), _cmdBan),
                new CPluginVariable("Admin Commands|Confirm", _cmdConfirm.GetType(), _cmdConfirm),
                new CPluginVariable("Settings|Debug level", _fDebugLevel.GetType(), _fDebugLevel),
                new CPluginVariable("Settings|During for Yell and PYell in sec. (5-60)", _settingYellDuring.GetType(), _settingYellDuring)
            };
            
            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            UnRegisterAllCommands();
            DebugWrite("[SetPluginVariable] Variable: " + strVariable + " Value: " + strValue, 4);

            if (Regex.Match(strVariable, @"Debug level").Success)
            {
                int tmp;
                int.TryParse(strValue, out tmp);
                if (tmp >= 0 && tmp <= 4) { _fDebugLevel = tmp; }
                else { ConsoleError("Invalid value for Debug Level: '" + strValue + "'. It must be a number between 1 and 4. (e.g.: 3)"); }
            }
            else if (Regex.Match(strVariable, @"Host").Success)
            {
                if (_fIsEnabled) { ConsoleError("SQL Settings locked! Please disable the Plugin and try again..."); return; }
                if (strValue.Length <= 100) { _settingStrSqlHostname = strValue.Replace(Environment.NewLine, ""); }
            }
            else if (Regex.Match(strVariable, @"Port").Success)
            {
                if (_fIsEnabled) { ConsoleError("SQL Settings locked! Please disable the Plugin and try again..."); return; }
                int tmpPort;
                int.TryParse(strValue, out tmpPort);
                if (tmpPort > 0 && tmpPort < 65536) { _settingStrSqlPort = tmpPort.ToString(); }
                else { ConsoleError("Invalid value for MySQL Port: '" + strValue + "'. Port must be a number between 1 and 65535. (e.g.: 3306)"); }
            }
            else if (Regex.Match(strVariable, @"Database").Success)
            {
                if (_fIsEnabled) { ConsoleError("SQL Settings locked! Please disable the Plugin and try again..."); return; }
                if (strValue.Length <= 100) { _settingStrSqlDatabase = strValue.Replace(Environment.NewLine, ""); }
            }
            else if (Regex.Match(strVariable, @"Username").Success)
            {
                if (_fIsEnabled) { ConsoleError("SQL Settings locked! Please disable the Plugin and try again..."); return; }
                if (strValue.Length <= 100) { _settingStrSqlUsername = strValue.Replace(Environment.NewLine, ""); }
            }
            else if (Regex.Match(strVariable, @"Password").Success)
            {
                if (_fIsEnabled) { ConsoleError("SQL Settings locked! Please disable the Plugin and try again..."); return; }
                if (strValue.Length <= 100) { _settingStrSqlPassword = strValue.Replace(Environment.NewLine, ""); }
            }
            else if (Regex.Match(strVariable, @"During for Yell and PYell in sec").Success)
            {
                int tmpyelltime;
                int.TryParse(strValue, out tmpyelltime);
                if (tmpyelltime >= 5 && tmpyelltime <= 60)
                {
                    _settingYellDuring = tmpyelltime;
                }
                else
                {
                    ConsoleError("Invalid value for Yell During. Time must be a number between 5 and 60. (e.g.: 15)");
                }
            }

            else if (Regex.Match(strVariable, @"Help Menu").Success) _cmdHelp = strValue;
            else if (Regex.Match(strVariable, @"View Admins Online").Success) _cmdAdminsOnline = strValue;
            else if (Regex.Match(strVariable, @"Say").Success) _cmdSay = strValue;
            else if (Regex.Match(strVariable, @"Playersay").Success) _cmdPlayerSay = strValue;
            else if (Regex.Match(strVariable, @"Yell").Success) _cmdYell = strValue;
            else if (Regex.Match(strVariable, @"Playeryell").Success) _cmdPlayerYell = strValue;
            else if (Regex.Match(strVariable, @"Swap").Success) _cmdSwap = strValue;
            else if (Regex.Match(strVariable, @"Kill").Success) _cmdKill = strValue;
            else if (Regex.Match(strVariable, @"Curse").Success) _cmdCurse = strValue;
            else if (Regex.Match(strVariable, @"Uncurse").Success) _cmdUncurse = strValue;
            else if (Regex.Match(strVariable, @"Kick").Success) _cmdKick = strValue;
            else if (Regex.Match(strVariable, @"Ban").Success) _cmdBan = strValue;
            else if (Regex.Match(strVariable, @"Confirm").Success) _cmdConfirm = strValue;

            SaveCommands();
            RegisterAllCommands();
        }
        
        public string GetPluginName()
        {
            return "Infinity BC2 Admin";
        }

        public string GetPluginVersion()
        {
            return "0.0.0.1";
        }

        public string GetPluginAuthor()
        {
            return "Martian";
        }

        public string GetPluginWebsite()
        {
            return "discord.me/infinitygaming";
        }

        public string GetPluginDescription()
        {
            return @"
            <h1>Your Title Here</h1>
            <p>TBD</p>

            <h2>Description</h2>
            <p>TBD</p>

            <h2>Commands</h2>
            <p>TBD</p>

            <h2>Settings</h2>
            <p>TBD</p>

            <h2>Development</h2>
            <p>TBD</p>
            <h3>Changelog</h3>
            <blockquote><h4>1.0.0.0 (15-SEP-2012)</h4>
	            - initial version<br/>
            </blockquote>
            ";
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
        {
            RegisterEvents(GetType().Name, "OnServerInfo", "OnResponseError", "OnListPlayers", "OnPlayerLeft", "OnPlayerTeamChange", "OnPlayerSquadChange", "OnPlayerKilled");
        }

        public void OnPluginEnable()
        {
            _fIsEnabled = true;
            _mDicPlayerInfo.Clear();
            _mAuthPlayerInfo.Clear();
            _lCursedPlayers.Clear();
            _lAdminsOnline.Clear();
            ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
            RegisterAllCommands();
            BuildRequiredTables();
            SaveCommands();
            ConsoleWrite("Enabled!");
        }

        public void OnPluginDisable()
        {
            _fIsEnabled = false;
            _mDicPlayerInfo.Clear();
            _mAuthPlayerInfo.Clear();
            _lCursedPlayers.Clear();
            _lAdminsOnline.Clear();
            UnRegisterAllCommands();
            ConsoleWrite("Disabled!");
        }

        #endregion

        //////////////////////
        #region EVENTS
        //////////////////////

        public override void OnServerInfo(CServerInfo serverInfo) 
        {
            DebugWrite("[EVENT] [OnServerInfo] - RoundTime: " + serverInfo.RoundTime, 3);
            _serverInfo = serverInfo;
        }

        public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset cpsSubset)
        {
            if (cpsSubset.Subset == CPlayerSubset.PlayerSubsetType.All)
            {
                DebugWrite("[EVENT] [OnListPlayers] - Count: " + players.Count, 3);
                foreach (CPlayerInfo cpiPlayer in players)
                {
                    string player = cpiPlayer.SoldierName;
                    if (IsPlayerInfoCached(player))
                    {
                        _mDicPlayerInfo[player] = cpiPlayer;
                    }
                    else 
                    { 
                        _mDicPlayerInfo.Add(player, cpiPlayer);
                    }
                    if (!IsAuthenticated(player)) { AuthenticateSoldier(player); }
                }
            }
            RegisterAllCommands();
        }

        public override void OnPlayerLeft(CPlayerInfo playerInfo)
        {
            string soldierName = playerInfo.SoldierName;
            DebugWrite("[EVENT] [OnPlayerLeft] - Player: " + soldierName, 3);

            if (IsPlayerInfoCached(soldierName))
            {
                _mDicPlayerInfo.Remove(soldierName);
            }

            if (IsAuthenticated(soldierName))
            {
                _mAuthPlayerInfo.Remove(soldierName);
            }

            if (_lCursedPlayers.Contains(soldierName))
            {
                _lCursedPlayers.Remove(soldierName);
            }
            
            if (_lAdminsOnline.Contains(soldierName))
            {
                _lAdminsOnline.Remove(soldierName);
            }
            
        }

        public override void OnPlayerTeamChange(string soldierName, int teamId, int squadId)
        {
            DebugWrite("[EVENT] [OnPlayerTeamChange] - Player: " + soldierName + " TeamId: " + teamId + " SquadId: " + squadId, 3);
            if (IsPlayerInfoCached(soldierName))
            {
                _mDicPlayerInfo[soldierName].TeamID = teamId;
            }
        }

        public override void OnPlayerSquadChange(string soldierName, int teamId, int squadId) 
        {
            DebugWrite("[EVENT] [OnPlayerSquadChange] - Player: " + soldierName + " TeamId: " + teamId + " SquadId: " + squadId, 3);
            
            if (IsPlayerInfoCached(soldierName))
            {
                _mDicPlayerInfo[soldierName].TeamID = teamId;
                _mDicPlayerInfo[soldierName].SquadID = squadId;
            }

        }

        public override void OnPlayerKilled(Kill kKillerVictimDetails)
        {
            string victim = kKillerVictimDetails.Victim.SoldierName;
            string killer = kKillerVictimDetails.Killer.SoldierName;

            DebugWrite("[EVENT] [OnPlayerKilled] - Killer: " + killer + " Victim: " + victim, 3);
            
            if (_lCursedPlayers.Contains(killer))
            {
                PlayerSayMsg(killer, "You are cursed on this server");
                Kill(killer);
                DebugWrite("[KILL] - Cursed Player: " + killer, 2);
            }
        }

        #endregion

        //////////////////////
        #region UNUSED EVENTS
        //////////////////////

        public override void OnResponseError(List<string> requestWords, string error) { }

        public override void OnVersion(string serverType, string version) { }

        public override void OnPlayerJoin(string soldierName) { }
        
        public override void OnPlayerAuthenticated(string strSoldierName, string strGuid) { }

        public override void OnGlobalChat(string speaker, string message) { }

        public override void OnTeamChat(string speaker, string message, int teamId) { }

        public override void OnSquadChat(string speaker, string message, int teamId, int squadId) { }

        public override void OnRoundOverPlayers(List<CPlayerInfo> players) { }

        public override void OnRoundOverTeamScores(List<TeamScore> teamScores) { }

        public override void OnRoundOver(int winningTeamId) { }

        public override void OnLoadingLevel(string mapFileName, int roundsPlayed, int roundsTotal) { }
        
        public override void OnLevelLoaded(string mapFileName, string gamemode, int roundsPlayed, int roundsTotal) { } // BF3

        public override void OnLevelStarted() { }

        #endregion

        //////////////////////
        #region COMMANDS
        //////////////////////

        private void SaveCommands()
        {
            _mUserCommands.Clear();
            _mAdminCommands.Clear();
            
            if (!string.IsNullOrEmpty(_cmdHelp)) _mUserCommands.Add(_cmdHelp, ": display command information");
            if (!string.IsNullOrEmpty(_cmdAdminsOnline)) _mUserCommands.Add(_cmdAdminsOnline, ": display admins online");
            
            if (!string.IsNullOrEmpty(_cmdSay)) _mAdminCommands.Add(_cmdSay, ": say a message as server");
            if (!string.IsNullOrEmpty(_cmdPlayerSay)) _mAdminCommands.Add(_cmdPlayerSay, ": <player> <message>: say a message to a player");
            if (!string.IsNullOrEmpty(_cmdYell)) _mAdminCommands.Add(_cmdYell, ": yell a message as server");
            if (!string.IsNullOrEmpty(_cmdPlayerYell)) _mAdminCommands.Add(_cmdPlayerYell, ": <player> <message>: yell a message to a player");
            if (!string.IsNullOrEmpty(_cmdSwap)) _mAdminCommands.Add(_cmdSwap, " <player1> <player2>: swap 2 players");
            if (!string.IsNullOrEmpty(_cmdKill)) _mAdminCommands.Add(_cmdKill, " <player> (reason): kill a player");
            if (!string.IsNullOrEmpty(_cmdCurse)) _mAdminCommands.Add(_cmdCurse, " <player> (reason): curse a player");
            if (!string.IsNullOrEmpty(_cmdUncurse)) _mAdminCommands.Add(_cmdUncurse, " <player>: uncurse a player");
            if (!string.IsNullOrEmpty(_cmdKick)) _mAdminCommands.Add(_cmdKick, " <player> (reason): kick a player");
            if (!string.IsNullOrEmpty(_cmdBan)) _mAdminCommands.Add(_cmdBan, " <player> (reason): ban a player");
            
        }

        private void RegisterAllCommands()
        {
            if (!_fIsEnabled) return;
            List<string> emptyList = new List<string>();
            List<string> scope = Listify("@", "!", "#");
            MatchCommand confirmationCommand = new MatchCommand(scope, _cmdConfirm, Listify<MatchArgumentFormat>());

            RegisterCommand(new MatchCommand("InfinityPlugin", "OnCommandAdminsOnline", scope, _cmdAdminsOnline,
                    new List<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "list admins online"
                )
            );

            RegisterCommand(new MatchCommand("InfinityPlugin", "OnCommandHelp", scope, _cmdHelp,
                    new List<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Help command"
                )
            );
            
            RegisterCommand(new MatchCommand("InfinityPlugin", "OnCommandSay", scope, _cmdSay,
                    new List<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Say a message as server"
                )
            );
            
            RegisterCommand(new MatchCommand("InfinityPlugin", "OnCommandYell", scope, _cmdYell,
                    new List<MatchArgumentFormat>(), new ExecutionRequirements(ExecutionScope.All), "Yell a message as server"
                )
            );
            
            RegisterCommand(new MatchCommand("InfinityPlugin", "OnCommandPlayerSay", scope, _cmdPlayerSay,
                    Listify(new MatchArgumentFormat("playername", new List<string>(_mDicPlayerInfo.Keys))),
                    new ExecutionRequirements(ExecutionScope.All, 2, confirmationCommand),
                    "Say a message to a player"
                )
            );
            
            RegisterCommand(new MatchCommand("InfinityPlugin", "OnCommandPlayerYell", scope, _cmdPlayerYell,
                    Listify(new MatchArgumentFormat("playername", new List<string>(_mDicPlayerInfo.Keys))),
                    new ExecutionRequirements(ExecutionScope.All, 2, confirmationCommand),
                    "Yell a message to a player"
                )
            );

            RegisterCommand(new MatchCommand("InfinityPlugin", "OnCommandSwap", scope, _cmdSwap,
                    Listify(
                        new MatchArgumentFormat("player1", new List<string>(_mDicPlayerInfo.Keys)), 
                        new MatchArgumentFormat("player2", new List<string>(_mDicPlayerInfo.Keys))
                    ),
                    new ExecutionRequirements(ExecutionScope.All, 2, confirmationCommand),
                    "swap 2 player"
                )
            );
            
            RegisterCommand(new MatchCommand("InfinityPlugin", "OnCommandKill", scope, _cmdKill,
                    Listify(
                        new MatchArgumentFormat("playername", new List<string>(_mDicPlayerInfo.Keys)), 
                        new MatchArgumentFormat("optional: reason", emptyList)),
                    new ExecutionRequirements(ExecutionScope.All, 2, confirmationCommand),
                    "kill a player"
                )
            );

            RegisterCommand(new MatchCommand("InfinityPlugin", "OnCommandCurse", scope, _cmdCurse,
                    Listify(new MatchArgumentFormat("playername", new List<string>(_mDicPlayerInfo.Keys)), 
                        new MatchArgumentFormat("optional: reason", emptyList)),
                    new ExecutionRequirements(ExecutionScope.All, 2, confirmationCommand),
                    "curse a player"
                )
            );

            RegisterCommand(new MatchCommand("InfinityPlugin", "OnCommandUnCurse", scope, _cmdUncurse,
                    Listify(new MatchArgumentFormat("playername", new List<string>(_mDicPlayerInfo.Keys))),
                    new ExecutionRequirements(ExecutionScope.All, 2, confirmationCommand),
                    "uncurse a player"
                )
            );

            RegisterCommand(new MatchCommand("InfinityPlugin", "OnCommandKick", scope, _cmdKick,
                    Listify(new MatchArgumentFormat("playername", new List<string>(_mDicPlayerInfo.Keys)), 
                        new MatchArgumentFormat("optional: reason", emptyList)),
                    new ExecutionRequirements(ExecutionScope.All, 2, confirmationCommand),
                    "curse a player"
                )
            );
            
            RegisterCommand(new MatchCommand("InfinityPlugin", "OnCommandBan", scope, _cmdBan,
                    Listify(new MatchArgumentFormat("playername", new List<string>(_mDicPlayerInfo.Keys)), 
                        new MatchArgumentFormat("optional: reason", emptyList)),
                    new ExecutionRequirements(ExecutionScope.All, 2, confirmationCommand),
                    "ban a player"
                )
            );
            
        }

        private void UnRegisterAllCommands()
        {
            List<string> emptyList = new List<string>();
            
            UnregisterCommand(new MatchCommand(emptyList, _cmdAdminsOnline, new List<MatchArgumentFormat>()));
            
            UnregisterCommand(new MatchCommand(emptyList, _cmdHelp, new List<MatchArgumentFormat>()));
            
            UnregisterCommand(new MatchCommand(emptyList, _cmdSay, new List<MatchArgumentFormat>()));
            
            UnregisterCommand(new MatchCommand(emptyList, _cmdYell, new List<MatchArgumentFormat>()));
            
            UnregisterCommand(new MatchCommand(emptyList, _cmdPlayerSay,
                Listify(new MatchArgumentFormat("playername", emptyList))
                )
            );
            
            UnregisterCommand(new MatchCommand(emptyList, _cmdPlayerYell,
                    Listify(new MatchArgumentFormat("playername", emptyList))
                )
            );
            
            UnregisterCommand(new MatchCommand(emptyList, _cmdSwap,
                    Listify(
                        new MatchArgumentFormat("player1", emptyList),
                        new MatchArgumentFormat("player2", emptyList)
                    )
                )
            );
            
            UnregisterCommand(new MatchCommand(emptyList, _cmdKill,
                    Listify(
                        new MatchArgumentFormat("playername", emptyList),
                        new MatchArgumentFormat("optional: reason", emptyList)
                    )
                )
            );
            
            UnregisterCommand(new MatchCommand(emptyList, _cmdCurse,
                    Listify(
                        new MatchArgumentFormat("playername", emptyList),
                        new MatchArgumentFormat("optional: reason", emptyList)
                    )
                )
            );
            
            UnregisterCommand(new MatchCommand(emptyList, _cmdUncurse,
                    Listify(new MatchArgumentFormat("playername", emptyList))
                )
            );
            
            UnregisterCommand(new MatchCommand(emptyList, _cmdKick,
                    Listify(
                        new MatchArgumentFormat("playername", emptyList),
                        new MatchArgumentFormat("optional: reason", emptyList)
                    )
                )
            );
            
            UnregisterCommand(new MatchCommand(emptyList, _cmdBan,
                    Listify(
                        new MatchArgumentFormat("playername", emptyList),
                        new MatchArgumentFormat("optional: reason", emptyList)
                    )
                )
            );

        }

        private bool IsAdmin(string name)
        {
            return IsAuthenticated(name) && _mAuthPlayerInfo[name].UserGroup != 0;
        }
        
        private bool HasAdminPrivileges(string name)
        {
            if (IsAdmin(name)) return true;
            PlayerSayMsg(name, "You do not have enough permission to use this command");
            return false;
        }
        
        public void OnCommandSay(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (capCommand.ExtraArguments.Length == 0) return;
            if (!HasAdminPrivileges(strSpeaker)) return;

            string message = capCommand.ExtraArguments;
            DebugWrite("[COMMAND] [SAY] - " + strSpeaker, 2);
            
            SayMsg(message);
        }
        
        public void OnCommandPlayerSay(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!HasAdminPrivileges(strSpeaker)) return;
            if (!IsPlayerInfoCached(capCommand.MatchedArguments[0].Argument)) return;
            if (capCommand.ExtraArguments.Length == 0) return;
            
            string target = capCommand.MatchedArguments[0].Argument;
            string message = capCommand.ExtraArguments;
            DebugWrite("[COMMAND] [PLAYER-SAY] - " + strSpeaker, 2);
            
            PlayerSayMsg(target, message);
        }

        public void OnCommandYell(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (capCommand.ExtraArguments.Length == 0) return;
            if (!HasAdminPrivileges(strSpeaker)) return;

            string message = capCommand.ExtraArguments;
            DebugWrite("[COMMAND] [YELL] - " + strSpeaker, 2);
            
            YellMsg(message);
        }
        
        public void OnCommandPlayerYell(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!HasAdminPrivileges(strSpeaker)) return;
            if (!IsPlayerInfoCached(capCommand.MatchedArguments[0].Argument)) return;
            if (capCommand.ExtraArguments.Length == 0) return;
            
            string target = capCommand.MatchedArguments[0].Argument;
            string message = capCommand.ExtraArguments;
            
            DebugWrite("[COMMAND] [PLAYER-YELL] - " + strSpeaker, 2);
            
            PlayerYellMsg(target, message);
        }

        public void OnCommandSwap(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!HasAdminPrivileges(strSpeaker)) return;
            if (!IsPlayerInfoCached(capCommand.MatchedArguments[0].Argument) || !IsPlayerInfoCached(capCommand.MatchedArguments[1].Argument)) return;

            CPlayerInfo player1 = _mDicPlayerInfo[capCommand.MatchedArguments[0].Argument];
            CPlayerInfo player2 = _mDicPlayerInfo[capCommand.MatchedArguments[1].Argument];

            if (player1.TeamID <= 0 || player2.TeamID <= 0)
            {
                PlayerSayMsg(strSpeaker, "Swap Error! Are both players in either team?");
                return;
            }

            if (player1.TeamID == player2.TeamID && player1.SquadID == player2.SquadID)
            {
                PlayerSayMsg(strSpeaker, "You cannot swap players in same team and squad");
                return;
            }

            if (_serverInfo.PlayerCount == _serverInfo.MaxPlayerCount)
            {
                MovePlayer(player1.SoldierName, 0, 0);
                MovePlayer(player2.SoldierName, 0, 0);

                MovePlayer(player1.SoldierName, player1.TeamID == 1 ? 2 : 1, 0);
                MovePlayer(player2.SoldierName, player2.TeamID == 1 ? 2 : 1, 0);

                DebugWrite("[COMMAND] [SWAP] - Player1: " + player1.SoldierName + "Player2: " + player2.SoldierName, 2);
            }
            else
            {
                MovePlayer(player1.SoldierName, player1.TeamID == 1 ? 2 : 1, 0);
                MovePlayer(player2.SoldierName, player2.TeamID == 1 ? 2 : 1, 0);
                DebugWrite("[COMMAND] [SWAP] - Player1: " + player1.SoldierName + "Player2: " + player2.SoldierName, 2);
            }
        }

        public void OnCommandCurse(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!HasAdminPrivileges(strSpeaker)) return;
            if (!IsPlayerInfoCached(capCommand.MatchedArguments[0].Argument)) return;

            string target = capCommand.MatchedArguments[0].Argument;
            string reason = capCommand.ExtraArguments;
            
            DebugWrite("[COMMAND] [CURSE] - Admin: " + strSpeaker + " Target: " + target, 2);

            if (_lCursedPlayers.Contains(target))
            {
                string message = target + " is already cursed";
                PlayerSayMsg(strSpeaker, message);
            }
            else
            {
                if (!IsAuthenticated(target))
                {
                    PlayerSayMsg(target, "Failed to authenticate player. Try again later");
                    return;
                }

                AuthSoldier adminData = _mAuthPlayerInfo[strSpeaker];
                AuthSoldier targetData = _mAuthPlayerInfo[target];

                bool success = AddPenaltyToDb(targetData.Id, adminData.Id, reason, Penalty.Curse);

                if (success)
                {
                    string message = target + " is cursed by " + strSpeaker;
                    if (!string.IsNullOrEmpty(reason)) message += " for " + reason;

                    SayMsg(message);
                    _lCursedPlayers.Add(target);
                }
                else
                {
                    PlayerSayMsg(strSpeaker, "Failed to curse " + target + ". Database error!");
                }
            }
        }

        public void OnCommandUnCurse(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!HasAdminPrivileges(strSpeaker)) return;
            if (!IsPlayerInfoCached(capCommand.MatchedArguments[0].Argument)) return;
            
            string target = capCommand.MatchedArguments[0].Argument;
            string reason = capCommand.ExtraArguments;
            
            DebugWrite("[COMMAND] [UNCURSE] - Admin: " + strSpeaker + " Target: " + target, 2);

            if (!_lCursedPlayers.Contains(target))
            {
                string message = target + " is not cursed";
                PlayerSayMsg(strSpeaker, message);
            }
            else
            {
                if (!IsAuthenticated(target))
                {
                    PlayerSayMsg(target, "Failed to authenticate player. Try again later");
                    return;
                }

                AuthSoldier targetData = _mAuthPlayerInfo[target];
                bool success = RemovePenalty(targetData.Id);

                if (success)
                {
                    string message = target + " is uncursed by " + strSpeaker;
                    if (!string.IsNullOrEmpty(reason)) message += " for " + reason;

                    SayMsg(message);
                    _lCursedPlayers.Remove(target);
                }
                else
                {
                    PlayerSayMsg(strSpeaker, "Failed to uncurse " + target + ". Database error!");
                }
            }
        }

        public void OnCommandKill(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!HasAdminPrivileges(strSpeaker)) return;
            if (!IsPlayerInfoCached(capCommand.MatchedArguments[0].Argument)) return;

            string target = capCommand.MatchedArguments[0].Argument;
            string reason = capCommand.ExtraArguments;
            
            DebugWrite("[COMMAND] [KILL] - Admin: " + strSpeaker + " Target: " + target, 2);
            
            string message = target + " is killed by " + strSpeaker;
            if (!string.IsNullOrEmpty(reason)) message += " for " + reason;

            Kill(target);
            SayMsg(message);

            if (!IsAuthenticated(target)) return;
            AuthSoldier adminData = _mAuthPlayerInfo[strSpeaker];
            AuthSoldier targetData = _mAuthPlayerInfo[target];
            AddPenaltyToDb(targetData.Id, adminData.Id, reason, Penalty.Kill);
        }

        public void OnCommandKick(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!HasAdminPrivileges(strSpeaker)) return;
            if (!IsPlayerInfoCached(capCommand.MatchedArguments[0].Argument)) return;
            
            string target = capCommand.MatchedArguments[0].Argument;
            string reason = capCommand.ExtraArguments;
            
            DebugWrite("[COMMAND] [KICK] - Admin: " + strSpeaker + " Target: " + target, 2);
            
            string message = target + " is kicked by " + strSpeaker;
            if (!string.IsNullOrEmpty(reason)) message += " for " + reason;

            SayMsg(message);
            Kick(target, reason);

            if (!IsAuthenticated(target)) return;
            AuthSoldier adminData = _mAuthPlayerInfo[strSpeaker];
            AuthSoldier targetData = _mAuthPlayerInfo[target];
            AddPenaltyToDb(targetData.Id, adminData.Id, reason, Penalty.Kick);
        }

        public void OnCommandBan(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            if (!HasAdminPrivileges(strSpeaker)) return;
            if (!IsPlayerInfoCached(capCommand.MatchedArguments[0].Argument)) return;

            string target = capCommand.MatchedArguments[0].Argument;
            string reason = capCommand.ExtraArguments;
            
            DebugWrite("[COMMAND] [BAN] - Admin: " + strSpeaker + " Target: " + target, 2);

            if (!IsAuthenticated(target))
            {
                PlayerSayMsg(target, "Failed to authenticate player. Try again later");
                return;
            }

            AuthSoldier adminData = _mAuthPlayerInfo[strSpeaker];
            AuthSoldier targetData = _mAuthPlayerInfo[target];

            bool success = AddPenaltyToDb(targetData.Id, adminData.Id, reason, Penalty.Ban);

            if (success)
            {
                string message = target + " is banned by " + strSpeaker;
                if (!string.IsNullOrEmpty(reason)) message += " for " + reason;
                Kick(target, "You are banned by " + strSpeaker);
                SayMsg(message);
            }
            else
            {
                PlayerSayMsg(strSpeaker, "Failed to Ban " + target + ". Database error!");
            }
        }

        public void OnCommandAdminsOnline(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            DebugWrite("[COMMAND] [ADMINS-ONLINE] - " + strSpeaker, 2);

            if (_lAdminsOnline.Count == 0)
            {
                PlayerSayMsg(strSpeaker, "No admins online");
            }
            else
            {
                string admins = string.Join(", ", _lAdminsOnline);
                PlayerSayMsg(strSpeaker, "Admins Online: " + admins);
            }
        }

        public void OnCommandHelp(string strSpeaker, string strText, MatchCommand mtcCommand, CapturedCommand capCommand, CPlayerSubset subMatchedScope)
        {
            DebugWrite("[COMMAND] [HELP] - " + strSpeaker, 2);

            if (capCommand.ExtraArguments.Length == 0)
            {
                string message = "[Available Commands] - ";
                message += string.Join(", ", _mUserCommands.Keys);
                if (IsAdmin(strSpeaker)) message += ", " + string.Join(", ", _mAdminCommands.Keys);
                PlayerSayMsg(strSpeaker, message);
            }
            else
            {
                string cmdName = capCommand.ExtraArguments;
                if (!IsAdmin(strSpeaker))
                {
                    if (!_mUserCommands.ContainsKey(cmdName))
                    {
                        PlayerSayMsg(strSpeaker, "No command found matching " + cmdName);
                        return;
                    }

                    PlayerSayMsg(strSpeaker, _mUserCommands[cmdName]);
                }
                else
                {
                    if (!_mUserCommands.ContainsKey(cmdName) && !_mAdminCommands.ContainsKey(cmdName))
                    {
                        PlayerSayMsg(strSpeaker, "No command found matching " + cmdName);
                        return;
                    }

                    string usage = cmdName + "";
                    usage += _mUserCommands.ContainsKey(cmdName) ? _mUserCommands[cmdName] : _mAdminCommands[cmdName];
                    PlayerSayMsg(strSpeaker, usage);
                }
            }
        }

        #endregion

        //////////////////////
        #region SQL Funtions
        //////////////////////

        private void DisplayMySqlErrorCollection(ExternalException myException)
        {
            LogWrite("^1Message: " + myException.Message + "^0");
            LogWrite("^1Native: " + myException.ErrorCode + "^0");
            if (myException.Source != null) LogWrite("^1Source: " + myException.Source + "^0");
            if (myException.StackTrace != null) LogWrite("^1StackTrace: " + myException.StackTrace + "^0");
            if (myException.InnerException != null) LogWrite("^1InnerException: " + myException.InnerException + "^0");
        }
        
        private string SqlLogin()
        {
            return "Server=" + _settingStrSqlHostname + ";" + "Port=" + _settingStrSqlPort + ";" + "Database=" + _settingStrSqlDatabase + ";" + "Uid=" + _settingStrSqlUsername + ";" + "Pwd=" + _settingStrSqlPassword + ";" + "Connection Timeout=5;";
        }

        private DataTable SqlQuery(MySqlCommand query, string debugFunction)
        {
            string debugPrefix = "[SqlQuery] [" + debugFunction + "] ";
            DataTable myDataTable = new DataTable();

            if (query == null)
            {
                DebugWrite(debugPrefix + "Query is null", 4);
                return myDataTable;
            }
            if (query.CommandText.Equals(string.Empty))
            {
                DebugWrite(debugPrefix + "CommandText is empty", 4);
                return myDataTable;
            }
            try
            {
                using (MySqlConnection connection = new MySqlConnection(SqlLogin()))
                {
                    try
                    {
                        query.Connection = connection;
                        using (MySqlDataAdapter myAdapter = new MySqlDataAdapter(query))
                        {
                            myAdapter.Fill(myDataTable);
                        }
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
            catch (MySqlException ex)
            {
                ConsoleError(debugPrefix + "Error in SQL.");
                DisplayMySqlErrorCollection(ex);
            }
            catch (Exception c)
            {
                ConsoleError(debugPrefix + "Error in SQL Query: " + c);
            }

            return myDataTable;
        }

        private bool SqlNonQuery(string query, string debugFunction)
        {
            using (MySqlCommand mySqlCommand = new MySqlCommand(query))
                return SqlNonQuery(mySqlCommand, debugFunction);
        }
        
        private bool SqlNonQuery(MySqlCommand query, string debugFunction)
        {
            string debugPrefix = "[SqlNonQuery] [" + debugFunction + "] ";
            bool sqlOk = false;

            if (query == null)
            {
                ConsoleWrite(debugPrefix + "query is null");
                return false;
            }
                
            if (query.CommandText.Equals(string.Empty))
            {
                DebugWrite(debugPrefix + "CommandText is empty", 4);
                return false;
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(SqlLogin()))
                {
                    connection.Open();
                    query.Connection = connection;
                    try {
                        query.ExecuteNonQuery();
                        sqlOk = true;
                    }
                    finally
                    {
                        connection.Close();
                        DebugWrite(debugPrefix + "Close SQL Connection (Con)", 4);
                    }
                }
            }
            catch (MySqlException ex)
            {
                ConsoleError(debugPrefix + "Error in SQL Query: " + ex);
                DisplayMySqlErrorCollection(ex);
            }
            catch (Exception ex)
            {
                ConsoleError(debugPrefix + "Error in SQL Query: " + ex);
            }

            return sqlOk;
        }
        
        private void BuildRequiredTables()
        {
            const string clientsTable = @"CREATE TABLE IF NOT EXISTS `clients` (
                                    `id` INT( 11 ) NOT NULL AUTO_INCREMENT,
                                    `name` VARCHAR( 50 ) DEFAULT NULL,
                                    `guid` VARCHAR( 35 ) DEFAULT NULL,
									`user_group` INT( 1 ) NOT NULL DEFAULT 0,
									`penalty_type` INT( 1 ) DEFAULT 0,
                                    `time_add` INT ( 11 ) NOT NULL DEFAULT 0,
                                    `time_edit` INT ( 11 ) NOT NULL DEFAULT 0,
                                    PRIMARY KEY ( `id` )
                                    ) ENGINE = INNODB DEFAULT CHARSET = utf8;";
            
            const string penaltiesTable = @"CREATE TABLE IF NOT EXISTS `penalties` (
                                    `id` INT( 11 ) NOT NULL AUTO_INCREMENT,
                                    `type` INT( 1 ) DEFAULT 0,
                                    `client_id` INT( 11 ) NOT NULL,
                                    `admin_id` INT( 11 ) NOT NULL,
                                    `reason` VARCHAR( 150 ) DEFAULT NULL,
                                    `server` VARCHAR( 150 ) DEFAULT NULL,
                                    `time_add` INT ( 11 ) NOT NULL DEFAULT 0,
                                    `time_edit` INT ( 11 ) NOT NULL DEFAULT 0,
                                    PRIMARY KEY ( `id` )
                                    ) ENGINE = INNODB DEFAULT CHARSET = utf8;";
            
            const string aliasesTable = @"CREATE TABLE IF NOT EXISTS `aliases` (
                                    `id` INT( 11 ) NOT NULL AUTO_INCREMENT,
                                    `alias` VARCHAR( 50 ) DEFAULT NULL,
                                    `client_id` INT( 11 ) NOT NULL,
                                    `time_add` INT ( 11 ) NOT NULL DEFAULT 0,
                                    PRIMARY KEY ( `id` )
                                    ) ENGINE = INNODB DEFAULT CHARSET = utf8;";
            
            bool sqlOk1 = SqlNonQuery(clientsTable, "BuildRequiredTables");
            bool sqlOk2 = SqlNonQuery(penaltiesTable, "BuildRequiredTables");
            bool sqlOk3 = SqlNonQuery(aliasesTable, "BuildRequiredTables");
            
            if (!sqlOk1 || !sqlOk2 || !sqlOk3)
            {
                ConsoleError("[SQL-BuildRequiredTables] Failed to create required tables");
                return;
            }
            _tableExists = true;
            DebugWrite("[SQL-BuildRequiredTables] Tables Created", 4);
        }

        private AuthSoldier RetrieveSoldierData(string name)
        {
            if (!_tableExists) return null;
            if (!_mDicPlayerInfo.ContainsKey(name)) return null;
            
            CPlayerInfo playerInfo = _mDicPlayerInfo[name];

            string clanTag = playerInfo.ClanTag;
            string soldier = GetEffectiveSoldierName(name, clanTag);
            string guid = playerInfo.GUID;

            string sql = @"SELECT * FROM `clients`";
            bool sqlender = true;

            if (!string.IsNullOrEmpty(soldier))
            {
                sql += " WHERE (";
                sqlender = false;
                sql += @"name LIKE @Soldier";
            }

            if (!string.IsNullOrEmpty(guid))
            {
                if (sqlender)
                {
                    sql += " WHERE (";
                }
                else
                {
                    sql += " OR ";
                }
                sql += @" guid LIKE @Guid";
            }

            sql += ");";

            using (MySqlCommand myCmd = new MySqlCommand(sql))
            {
                if (!string.IsNullOrEmpty(soldier))
                {
                    myCmd.Parameters.AddWithValue("@Soldier", soldier);
                }
                if (!string.IsNullOrEmpty(soldier))
                {
                    myCmd.Parameters.AddWithValue("@Guid", guid);
                }
                
                DataTable resultTable = SqlQuery(myCmd, "RetrieveSoldierData");
                if (resultTable.Rows.Count == 0)
                {
                    DebugWrite("[SQL-RetrieveSoldierData] No data for: " + name, 4);
                    return new AuthSoldier();
                }

                foreach (DataRow row in resultTable.Rows)
                {
                    DebugWrite("[SQL-RetrieveSoldierData] Retrieved Soldier data: " + name, 4);
                    return new AuthSoldier(row);
                }
            }
            return null;
        }

        private bool RegisterSoldier(string name)
        {
            if (!_tableExists) return false;
            if (!_mDicPlayerInfo.ContainsKey(name)) { return false; }

            CPlayerInfo playerInfo = _mDicPlayerInfo[name];
            string clanTag = playerInfo.ClanTag;
            string soldier = GetEffectiveSoldierName(name, clanTag);
            string guid = playerInfo.GUID;
            long unix = GetTimeEpoch();
            
            const string sql = @"INSERT INTO clients (`name`, `guid`, `time_add`, `time_edit`) VALUES (@Soldier, @Guid, @TimeAdd, @TimeEdit)";

            using (MySqlCommand myCom = new MySqlCommand(sql))
            {
                myCom.Parameters.AddWithValue("@Soldier", soldier);
                myCom.Parameters.AddWithValue("@Guid", guid);
                myCom.Parameters.AddWithValue("@TimeAdd", unix);
                myCom.Parameters.AddWithValue("@TimeEdit", unix);
                return SqlNonQuery(myCom, "RegisterSoldier");
            }
        }

        private void CheckSoldierAlias(string name, int clientId)
        {
            if (!_tableExists) return;
            if (!_mDicPlayerInfo.ContainsKey(name)) { return; }
            
            CPlayerInfo playerInfo = _mDicPlayerInfo[name];
            string clanTag = playerInfo.ClanTag;
            string soldier = GetEffectiveSoldierName(name, clanTag);
            
            const string sql1 = @"SELECT * FROM `aliases` WHERE client_id = @Id AND alias LIKE @Soldier";

            using (MySqlCommand myCmd = new MySqlCommand(sql1))
            {
                myCmd.Parameters.AddWithValue("@Id", clientId);
                myCmd.Parameters.AddWithValue("@Soldier", soldier);
                
                DataTable resultTable = SqlQuery(myCmd, "CheckSoldierAlias");
                if (resultTable.Rows.Count == 0)
                {
                    DebugWrite("[SQL-checkAlias]: Trying to insert new alias: " + soldier, 4);
                    long unix = GetTimeEpoch();
                    const string sql2 = "INSERT INTO `aliases` (`alias`, `client_id`, `time_add`) VALUES (@Alias, @Id, @TimeAdd)";

                    using (MySqlCommand myCmd2 = new MySqlCommand(sql2))
                    {
                        myCmd2.Parameters.AddWithValue("@Alias", soldier);
                        myCmd2.Parameters.AddWithValue("@Id", clientId);
                        myCmd2.Parameters.AddWithValue("@TimeAdd", unix);

                        bool sqlOk = SqlNonQuery(myCmd2, "CheckSoldierAlias");
                        if (sqlOk)
                        {
                            DebugWrite("[SQL-checkAlias]: New Alias added: " + soldier, 4);
                        }
                    }
                }
                else
                {
                    DebugWrite("[SQL-checkAlias]: Alias already exists for: " + name, 4);
                }
            }
        }

        private bool AddPenaltyToDb(int uid, int adminId, string reason, Penalty penalty)
        {
            if (!_tableExists) return false;
            int penaltyType = (int)penalty;

            long unix = GetTimeEpoch();
            string sql1 = "INSERT INTO `penalties` (`client_id`, `admin_id`, `reason`, `type`, `server`, `time_add`, `time_edit`) VALUES ('"
                          + uid + "', '"
                          + adminId + "', '"
                          + reason + "', '"
                          + penaltyType + "', '"
                          + _serverInfo.ServerName + "', '"
                          + unix + "', '"
                          + unix + "');";

            bool sqlOk1 = SqlNonQuery(sql1, "add" + penalty);

            bool sqlOk2 = true;
            if (penalty == Penalty.Curse || penalty == Penalty.Ban)
            {
                string sql2 = "UPDATE clients SET penalty_type = " + penaltyType + " WHERE id = " + uid + ";";
                sqlOk2 = SqlNonQuery(sql2, "add" + penalty);
            }

            return sqlOk1 && sqlOk2;
        }

        private bool RemovePenalty(int uid)
        {
            if (!_tableExists) return false;
            long unix = GetTimeEpoch();
            string sql = "UPDATE clients SET penalty_type = " + 0 + ", time_edit = " + unix + " WHERE id = " + uid + ";";

            return SqlNonQuery(sql, "RemovePenalty");
        }
        
        #endregion

        //////////////////////
        #region HELPER Functions
        //////////////////////

        private static long GetTimeEpoch()
        {
            return ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
        }

        private static string GetEffectiveSoldierName(string name, string clantag)
        {
            string soldier = "";
            if (!string.IsNullOrEmpty(clantag))
                soldier += "[" + clantag + "] ";
            soldier += "" + name;
            return soldier;
        }

        private bool IsPlayerInfoCached(string player)
        {
            return _mDicPlayerInfo.ContainsKey(player);
        }

        private bool IsAuthenticated(string player)
        {
            return _mAuthPlayerInfo.ContainsKey(player);
        }

        private void AuthenticateSoldier(string name)
        {
            AuthSoldier data = RetrieveSoldierData(name);
            if (data == null) { return; }  // Some DB Exception occured

            if (data.Id == 0)
            {
                RegisterSoldier(name);
                return;
            }

            _mAuthPlayerInfo.Add(name, data);
            DebugWrite("[AuthenticateSoldier] New User Authenticated: " + name, 3);

            switch (data.PenaltyType)
            {
                case (int) Penalty.Curse:
                    _lCursedPlayers.Add(name);
                    break;
                
                case (int) Penalty.Ban:
                    Kick(name, "You are banned on this server");
                    DebugWrite("[BANNED] User Kicked (Ban): " + name, 2);
                    break;
            }
            
            if (data.UserGroup > 0)
            {
                _lAdminsOnline.Add(name);
            }

            if (data.SoldierName.Equals(GetEffectiveSoldierName(_mDicPlayerInfo[name].SoldierName, _mDicPlayerInfo[name].ClanTag)))
            {
                DebugWrite("[AuthenticateSoldier] Soldier Name verified: " + name, 3);
            }
            else
            {
                CheckSoldierAlias(name, data.Id);
            }

        }

        private string StrYellDuration()
        {
            return (_settingYellDuring * 1000).ToString();
        }

        #endregion

        //////////////////////
        #region PROCON EXECUTE Commands
        //////////////////////

        private void PlayerSayMsg(string target, string message)
        {
            if (!_fIsEnabled || message.Length < 3) return;
            ExecuteCommand("procon.protected.send", "admin.say", message.Replace(Environment.NewLine, " "), "player", target);
            ExecuteCommand("procon.protected.chat.write", "(PlayerSay " + target + ") " + message.Replace(Environment.NewLine, " "));
            DebugWrite("[PlayerSayMsg] - Player: " + target + " Message: " +message, 3);
        }

        private void PlayerSayMsg(string target, string message, int msgDelay)
        {
            if (!_fIsEnabled || message.Length < 3) return;
            if (msgDelay == 0)
            {
                PlayerSayMsg(target, message);
                return;
            }
            ExecuteCommand("procon.protected.tasks.add", "Infinity Admin", msgDelay.ToString(), "1", "1", "procon.protected.send", "admin.say", message.Replace(Environment.NewLine, " "), "player", target);
            ExecuteCommand("procon.protected.chat.write", "(PlayerSay " + target + ") " + message.Replace(Environment.NewLine, " "));
            DebugWrite("[PlayerSayMsg] - Player: " + target + " Message: " +message, 3);
        }

        private void SayMsg(string message)
        {
            if (!_fIsEnabled || message.Length < 3) return;
            ExecuteCommand("procon.protected.send", "admin.say", message.Replace(Environment.NewLine, " "), "all");
            ExecuteCommand("procon.protected.chat.write", message.Replace(Environment.NewLine, " "));
            DebugWrite("[SayMsg] - " + message, 3);
        }

        private void SayMsg(string message, int msgDelay)
        {
            if (!_fIsEnabled || message.Length < 3) return;
            if (msgDelay == 0)
            {
                SayMsg(message);
                return;
            }
            ExecuteCommand("procon.protected.tasks.add", GetPluginName(), msgDelay.ToString(), "1", "1", "procon.protected.send", "admin.say", message.Replace(Environment.NewLine, " "), "all");
            ExecuteCommand("procon.protected.chat.write", message.Replace(Environment.NewLine, " "));
            DebugWrite("[SayMsg] - " + message, 3);
        }

        private void PlayerYellMsg(string target, string message)
        {
            if (!_fIsEnabled || message.Length < 3) return;
            ExecuteCommand("procon.protected.send", "admin.yell", message.Replace(Environment.NewLine, _newLiner), StrYellDuration(), "player", target);
            ExecuteCommand("procon.protected.chat.write", "(PlayerYell " + target + ") " + message.Replace(Environment.NewLine, "  -  "));
            DebugWrite("[PlayerYellMsg] - Player: " + target + " Message: " +message, 3);
        }

        private void PlayerYellMsg(string target, string message, int msgDelay)
        {
            if (!_fIsEnabled || message.Length < 3) return;
            if (msgDelay == 0)
            {
                PlayerYellMsg(target, message);
                return;
            }
            ExecuteCommand("procon.protected.tasks.add", GetPluginName(), msgDelay.ToString(), "1", "1", "procon.protected.send", "admin.yell", "[VIP SLOT] " + _newLiner + message.Replace(Environment.NewLine, _newLiner), StrYellDuration(), "player", target);
            ExecuteCommand("procon.protected.chat.write", "(PlayerYell " + target + ") " + message.Replace(Environment.NewLine, "  -  "));
            DebugWrite("[PlayerYellMsg] - Player: " + target + " Message: " +message, 3);
        }

        private void YellMsg(string message)
        {
            if (!_fIsEnabled || message.Length < 3) return;
            ExecuteCommand("procon.protected.send", "admin.yell", message.Replace(Environment.NewLine, _newLiner), StrYellDuration(), "all");
            ExecuteCommand("procon.protected.chat.write", "(Yell) " + message.Replace(Environment.NewLine, "  -  "));
            DebugWrite("[YellMsg] - " + message, 3);
        }

        private void YellMsg(string message, int msgDelay)
        {
            if (!_fIsEnabled || message.Length < 3) return;
            if (msgDelay == 0)
            {
                YellMsg(message);
                return;
            }
            ExecuteCommand("procon.protected.tasks.add", GetPluginName(), msgDelay.ToString(), "1", "1", "procon.protected.send", "admin.yell", message.Replace(Environment.NewLine, _newLiner), StrYellDuration(), "all");
            ExecuteCommand("procon.protected.chat.write", "(Yell) " + message.Replace(Environment.NewLine, "  -  "));
            DebugWrite("[YellMsg] - " + message, 3);
        }
        
        private void TeamSay(int teamId, string message)
        {
            if (!_fIsEnabled || message.Length < 3 || teamId <= 0) return;
            ExecuteCommand("procon.protected.send", "admin.say", message, "team", teamId.ToString());
            ExecuteCommand("procon.protected.chat.write", "(TeamSay " + teamId + ") " + message.Replace(Environment.NewLine, " "));
            DebugWrite("[TeamSay] - Team: " + teamId, 3);
        }
        
        private void SquadSay(int teamId, int squadId, string message)
        {
            if (!_fIsEnabled || message.Length < 3) return;
            if(teamId <= 0 || squadId < 0) return;
            ExecuteCommand("procon.protected.send", "admin.say", message, "squad", teamId.ToString(), squadId.ToString());
            ExecuteCommand("procon.protected.chat.write", "(SquadSay " + teamId + "" + squadId +") " + message.Replace(Environment.NewLine, " "));
            DebugWrite("[SquadSay] - Team: " + teamId + " Squad: " + squadId, 3);
        }

        private void Kill(string target)
        {
            ExecuteCommand("procon.protected.send", "admin.killPlayer", target);
            DebugWrite("[Kill] - " + target, 3);
        }

        private void Kick(string target, string reason)
        {
            ExecuteCommand("procon.protected.send", "admin.kickPlayer", target, reason);
            DebugWrite("[Kick] - " + target, 3);
        }

        private void MovePlayer(string target, int teamId, int squadId)
        {
            ExecuteCommand("procon.protected.send", "admin.movePlayer", target, teamId.ToString(), squadId.ToString(), "true");
            DebugWrite("[MovePlayer] - Player: " + target + " TeamId: " + teamId + " SquadId: " + squadId, 3);
        }

        #endregion

        //////////////////////
        #region MODEL CLASSES & ENUMS
        //////////////////////

        private class AuthSoldier
        {
            public AuthSoldier()
            {
                Id = 0;
                SoldierName = string.Empty;
                UserGroup = 0;
                PenaltyType = 0;
            }

            public AuthSoldier(DataRow row)
            {
                Id = Convert.ToInt32(row["id"]);
                SoldierName = row["name"].ToString();
                UserGroup = Convert.ToInt32(row["user_group"]);
                PenaltyType = Convert.ToInt32(row["penalty_type"]);
            }

            public int Id { get; private set; }

            public string SoldierName { get; private set; }

            public int UserGroup { get; private set; }

            public int PenaltyType { get; private set; }
            
        }

        private enum Penalty { 
            Warn = 1, 
            Kill = 2,
            Curse = 3,
            Kick = 4,
            Ban = 5
        }

        #endregion
        
    }

} // end namespace PRoConEvents


