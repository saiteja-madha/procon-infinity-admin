/*	InfinityPlugin.cs -  Procon Plugin [BC2]

	Version: 0.0.0.1

	Code Credit:
	PapaCharlie9 - Basic Plugin Template Part (BasicPlugin.cs)
	maxdralle - MySQL Main Functions (VipSlotManager.cs)

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
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
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
        
        // constants
        private const string SettingsUserCommandsPrefix = "1. User Commands|";
        private const string SettingsVipCommandsPrefix = "2. VIP Commands|";
        private const string SettingsAdminCommandsPrefix = "3. Admin Commands|";
        private const string SettingsSuperAdminCommandsPrefix = "4. Super Admin Commands|";
        private const string SettingsMySqlPrefix = "5. MySQL Details|";
        private const string SettingsAdditionalPrefix = "6. Additional Settings|";
        private const string NewLiner = "";
        private const int MaxLineLength = 100;
        private readonly DateTime _layerStartingTime = DateTime.UtcNow;
        
        // Variables
        private bool _fIsEnabled;
        private bool _firstCheck;
        private bool _tableExists;
        
        // Cache variables
        private readonly Dictionary<string, CPlayerInfo> _mDicPlayerInfo;
        private readonly Dictionary<string, AuthSoldier> _mAuthPlayerInfo;
        private readonly Dictionary<string, Command> _mIngameCommands;
        private readonly Dictionary<string, ConfirmationEntry> _mPendingConfirmations;
        private readonly List<string> _lCursedPlayers;
        private readonly List<string> _lAdminsOnline;
        private CServerInfo _serverInfo;

        // Database Settings
        private string _settingStrSqlHostname;
        private string _settingStrSqlPort;
        private string _settingStrSqlDatabase;
        private string _settingStrSqlUsername;
        private string _settingStrSqlPassword;
        
        // Additional settings
        private int _fDebugLevel;
        private int _settingYellDuring;

        // User Commands
        private string _cmdHelp;
        private string _cmdAdminsOnline;
        
        // VIP Commands
        private string _cmdKillMe;

        // Admin Commands
        private string _cmdConfirm;
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
        private string _cmdUnban;
        private string _cmdLookup;
        private string _cmdPutGroup;
        private string _cmdPenalties;

        public InfinityPlugin()
        {
            _fIsEnabled = false;
            _fDebugLevel = 1;
            _tableExists = false;
            _firstCheck = false;
            _settingYellDuring = 5;

            _mDicPlayerInfo = new Dictionary<string, CPlayerInfo>();
            _mAuthPlayerInfo = new Dictionary<string, AuthSoldier>();
            _mIngameCommands = new Dictionary<string, Command>();
            _mPendingConfirmations = new Dictionary<string, ConfirmationEntry>();
            _lCursedPlayers = new List<string>();
            _lAdminsOnline = new List<string>();
            _serverInfo = new CServerInfo();

            _settingStrSqlHostname = string.Empty;
            _settingStrSqlPort = "3306";
            _settingStrSqlDatabase = string.Empty;
            _settingStrSqlUsername = string.Empty;
            _settingStrSqlPassword = string.Empty;
            
            _cmdHelp = "help";
            _cmdAdminsOnline = "admins";

            _cmdKillMe = "killme";

            _cmdConfirm = "yes";
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
            _cmdUnban = "unban";
            _cmdLookup = "lookup";
            _cmdPutGroup = "putgroup";
            _cmdPenalties = "penalties";
        }

        #endregion

        //////////////////////
        #region BasicPlugin.cs part by PapaCharlie9@gmail.com
        //////////////////////

        private enum MessageType
        {
            Warning,
            Error,
            Exception,
            Normal
        }

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
            var lstReturn = new List<CPluginVariable> {
                new CPluginVariable(SettingsMySqlPrefix + "Host", _settingStrSqlHostname.GetType(), _settingStrSqlHostname),
                new CPluginVariable(SettingsMySqlPrefix + "Port", _settingStrSqlPort.GetType(), _settingStrSqlPort),
                new CPluginVariable(SettingsMySqlPrefix + "Database", _settingStrSqlDatabase.GetType(), _settingStrSqlDatabase),
                new CPluginVariable(SettingsMySqlPrefix + "Username", _settingStrSqlUsername.GetType(), _settingStrSqlUsername),
                new CPluginVariable(SettingsMySqlPrefix + "Password", _settingStrSqlPassword.GetType(), _settingStrSqlPassword),
                new CPluginVariable(SettingsUserCommandsPrefix + "Help Menu", _cmdHelp.GetType(), _cmdHelp),
                new CPluginVariable(SettingsUserCommandsPrefix + "View Admins Online", _cmdAdminsOnline.GetType(), _cmdAdminsOnline),
                new CPluginVariable(SettingsVipCommandsPrefix + "Commit a suicide", _cmdKillMe.GetType(), _cmdKillMe),
                new CPluginVariable(SettingsAdminCommandsPrefix + "Say", _cmdSay.GetType(), _cmdSay),
                new CPluginVariable(SettingsAdminCommandsPrefix + "Playersay", _cmdPlayerSay.GetType(), _cmdPlayerSay),
                new CPluginVariable(SettingsAdminCommandsPrefix + "Yell", _cmdYell.GetType(), _cmdYell),
                new CPluginVariable(SettingsAdminCommandsPrefix + "Playeryell", _cmdPlayerYell.GetType(), _cmdPlayerYell),
                new CPluginVariable(SettingsAdminCommandsPrefix + "Swap", _cmdSwap.GetType(), _cmdSwap),
                new CPluginVariable(SettingsAdminCommandsPrefix + "Kill", _cmdKill.GetType(), _cmdKill),
                new CPluginVariable(SettingsAdminCommandsPrefix + "Curse", _cmdCurse.GetType(), _cmdCurse),
                new CPluginVariable(SettingsAdminCommandsPrefix + "Uncurse", _cmdUncurse.GetType(), _cmdUncurse),
                new CPluginVariable(SettingsAdminCommandsPrefix + "Kick", _cmdKick.GetType(), _cmdKick),
                new CPluginVariable(SettingsAdminCommandsPrefix + "Ban", _cmdBan.GetType(), _cmdBan),
                new CPluginVariable(SettingsAdminCommandsPrefix + "Unban", _cmdUnban.GetType(), _cmdUnban),
                new CPluginVariable(SettingsAdminCommandsPrefix + "Lookup", _cmdLookup.GetType(), _cmdLookup),
                new CPluginVariable(SettingsAdminCommandsPrefix + "Recent Penalties", _cmdPenalties.GetType(), _cmdPenalties),
                new CPluginVariable(SettingsSuperAdminCommandsPrefix + "Set player group", _cmdPutGroup.GetType(), _cmdPutGroup),
                new CPluginVariable(SettingsAdminCommandsPrefix + "Confirm", _cmdConfirm.GetType(), _cmdConfirm),
                new CPluginVariable(SettingsAdditionalPrefix + "Debug level", _fDebugLevel.GetType(), _fDebugLevel),
                new CPluginVariable(SettingsAdditionalPrefix + "During for Yell and PYell in sec. (5-60)", _settingYellDuring.GetType(), _settingYellDuring)
            };

            return lstReturn;
        }

        public void SetPluginVariable(string strVariable, string strValue)
        {
            bool layerReady = (DateTime.UtcNow - _layerStartingTime).TotalSeconds > 30;
            DebugWrite("[SetPluginVariable] Variable: " + strVariable + " Value: " + strValue, 4);

            if (Regex.Match(strVariable, @"Debug level").Success)
            {
                int tmp;
                int.TryParse(strValue, out tmp);
                if (tmp >= 0 && tmp <= 4)
                {
                    _fDebugLevel = tmp;
                }
                else
                {
                    ConsoleError("Invalid value for Debug Level: '" + strValue +
                                 "'. It must be a number between 1 and 4. (e.g.: 3)");
                }
            }
            else if (Regex.Match(strVariable, @"Host").Success)
            {
                if (_fIsEnabled && layerReady && _firstCheck)
                {
                    ConsoleError("SQL Settings locked! Please disable the Plugin and try again...");
                    return;
                }

                if (strValue.Length <= 100)
                {
                    _settingStrSqlHostname = strValue.Replace(Environment.NewLine, "");
                }
            }
            else if (Regex.Match(strVariable, @"Port").Success)
            {
                if (_fIsEnabled && layerReady && _firstCheck)
                {
                    ConsoleError("SQL Settings locked! Please disable the Plugin and try again...");
                    return;
                }
                int.TryParse(strValue, out int tmpPort);
                if (tmpPort > 0 && tmpPort < 65536)
                {
                    _settingStrSqlPort = tmpPort.ToString();
                }
                else
                {
                    ConsoleError("Invalid value for MySQL Port: '" + strValue +
                                 "'. Port must be a number between 1 and 65535. (e.g.: 3306)");
                }
            }
            else if (Regex.Match(strVariable, @"Database").Success)
            {
                if (_fIsEnabled && layerReady && _firstCheck)
                {
                    ConsoleError("SQL Settings locked! Please disable the Plugin and try again...");
                    return;
                }
                if (strValue.Length <= 100)
                {
                    _settingStrSqlDatabase = strValue.Replace(Environment.NewLine, "");
                }
            }
            else if (Regex.Match(strVariable, @"Username").Success)
            {
                if (_fIsEnabled && layerReady && _firstCheck)
                {
                    ConsoleError("SQL Settings locked! Please disable the Plugin and try again...");
                    return;
                }
                if (strValue.Length <= 100)
                {
                    _settingStrSqlUsername = strValue.Replace(Environment.NewLine, "");
                }
            }
            else if (Regex.Match(strVariable, @"Password").Success)
            {
                if (_fIsEnabled && layerReady && _firstCheck)
                {
                    ConsoleError("SQL Settings locked! Please disable the Plugin and try again...");
                    return;
                }
                if (strValue.Length <= 100)
                {
                    _settingStrSqlPassword = strValue.Replace(Environment.NewLine, "");
                }
            }
            else if (Regex.Match(strVariable, @"During for Yell and PYell in sec").Success)
            {
                int.TryParse(strValue, out int tmpyelltime);
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
            else if (Regex.Match(strVariable, @"Commit a suicide").Success) _cmdKillMe = strValue;
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
            else if (Regex.Match(strVariable, @"Unban").Success) _cmdUnban = strValue;
            else if (Regex.Match(strVariable, @"Lookup").Success) _cmdLookup = strValue;
            else if (Regex.Match(strVariable, @"Recent Penalties").Success) _cmdPenalties = strValue;
            else if (Regex.Match(strVariable, @"Set player group").Success) _cmdPutGroup = strValue;
            else if (Regex.Match(strVariable, @"Confirm").Success) _cmdConfirm = strValue;
            
            RegisterIngameCommands();
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
            RegisterEvents(GetType().Name, "OnServerInfo", "OnResponseError", "OnListPlayers", "OnPlayerLeft", "OnPlayerTeamChange", "OnPlayerSquadChange", "OnPlayerKilled", "OnGlobalChat", "OnTeamChat", "OnSquadChat");
        }

        public void OnPluginEnable()
        {
            _fIsEnabled = true;
            _firstCheck = false;
            _mDicPlayerInfo.Clear();
            _mAuthPlayerInfo.Clear();
            _mIngameCommands.Clear();
            _lCursedPlayers.Clear();
            _lAdminsOnline.Clear();
            ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
            RegisterIngameCommands();
            ExecuteCommand("procon.protected.tasks.add", "InfinityPlugin", "3", "9", "20", "procon.protected.plugins.call", "InfinityPlugin", "PluginStarter");
            ConsoleWrite("Enabled!");
        }

        public void OnPluginDisable()
        {
            _fIsEnabled = false;
            _firstCheck = false;
            _mDicPlayerInfo.Clear();
            _mAuthPlayerInfo.Clear();
            _mIngameCommands.Clear();
            _lCursedPlayers.Clear();
            _lAdminsOnline.Clear();
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

                    if (!IsAuthenticated(player))
                    {
                        AuthenticateSoldier(player);
                    }
                }
            }
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

        public override void OnGlobalChat(string speaker, string message)
        {
            CommandHandler(speaker, message);
        }

        public override void OnTeamChat(string speaker, string message, int teamId)
        {
            CommandHandler(speaker, message);
        }

        public override void OnSquadChat(string speaker, string message, int teamId, int squadId)
        {
            CommandHandler(speaker, message);
        }

        #endregion

        //////////////////////
        #region UNUSED EVENTS
        //////////////////////

        public override void OnResponseError(List<string> requestWords, string error) { }

        public override void OnVersion(string serverType, string version) { }

        public override void OnPlayerJoin(string soldierName) { }

        public override void OnPlayerAuthenticated(string strSoldierName, string strGuid) { }

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

        private bool HadCommandPrivileges(string name, Command cmd)
        {
            if (!IsAuthenticated(name))
            {
                PlayerSayMsg(name, "You are not authenticated yet. Please try again later");
                return false;
            }
            AuthSoldier client = _mAuthPlayerInfo[name];
            if (client.UserGroup >= cmd.UserGroupId)
                return true;
            PlayerSayMsg(name, "You do not have enough permission to use this command");
            return false;
        }

        private void RegisterIngameCommands()
        {
            _mIngameCommands.Clear();
            
            if (!string.IsNullOrEmpty(_cmdConfirm))
            {
                _mIngameCommands.Add(_cmdConfirm,
                    new Command(_cmdConfirm, "confirmation", "", UserGroup.User, 0, false, 0, OnCommandConfirm));
            }

            if (!string.IsNullOrEmpty(_cmdHelp))
            {
                _mIngameCommands.Add(_cmdHelp,
                    new Command(_cmdHelp, "display available commands", "", UserGroup.User, 0, false, 0, OnCommandHelp));
            }

            if (!string.IsNullOrEmpty(_cmdAdminsOnline))
            {
                _mIngameCommands.Add(_cmdAdminsOnline,
                    new Command(_cmdAdminsOnline, "show admins online", "", UserGroup.User, 0, false, 0, OnCommandAdminsOnline));
            }            
            
            if (!string.IsNullOrEmpty(_cmdKillMe))
            {
                _mIngameCommands.Add(_cmdKillMe,
                    new Command(_cmdKillMe, "commit a suicide", "", UserGroup.Vip, 0, false, 0, OnCommandKillMe));
            }

            if (!string.IsNullOrEmpty(_cmdSay))
            {
                _mIngameCommands.Add(_cmdSay,
                    new Command(_cmdSay, "say a message as server", "<message>", UserGroup.Admin, 1, false, 0, OnCommandSay));
            }

            if (!string.IsNullOrEmpty(_cmdPlayerSay))
            {
                _mIngameCommands.Add(_cmdPlayerSay,
                    new Command(_cmdPlayerSay, "say a message to a player", "<player> <message>", UserGroup.Admin, 2, false, 1, OnCommandPlayerSay));
            }

            if (!string.IsNullOrEmpty(_cmdYell))
            {
                _mIngameCommands.Add(_cmdYell,
                    new Command(_cmdYell, "yell a message as server", "<message>", UserGroup.Admin, 1, false, 0, OnCommandYell));
            }

            if (!string.IsNullOrEmpty(_cmdPlayerYell))
            {
                _mIngameCommands.Add(_cmdPlayerYell,
                    new Command(_cmdPlayerYell, "yell a message to a player", "<player> <message>", UserGroup.Admin, 2, false, 1, OnCommandPlayerYell));
            }

            if (!string.IsNullOrEmpty(_cmdSwap))
            {
                _mIngameCommands.Add(_cmdSwap,
                    new Command(_cmdSwap, "swap 2 players", "<player>", UserGroup.Admin, 2, false, 2, OnCommandSwap));
            }
            
            if (!string.IsNullOrEmpty(_cmdKill))
            {
                _mIngameCommands.Add(_cmdKill,
                    new Command(_cmdKill, "kill a player", "<player> (reason)", UserGroup.Admin, 1, true, 1, OnCommandKill));
            }
            
            if (!string.IsNullOrEmpty(_cmdCurse))
            {
                _mIngameCommands.Add(_cmdCurse,
                    new Command(_cmdCurse, "curse a player", "<@uid/player> (reason)", UserGroup.Admin, 1, true, 1, true, OnCommandCurse));
            }
            
            if (!string.IsNullOrEmpty(_cmdUncurse))
            {
                _mIngameCommands.Add(_cmdUncurse,
                    new Command(_cmdUncurse, "uncurse a player", "<@uid/player>", UserGroup.Admin, 1, true, 1, true, OnCommandUnCurse));
            }
            
            if (!string.IsNullOrEmpty(_cmdKick))
            {
                _mIngameCommands.Add(_cmdKick,
                    new Command(_cmdKick, "kick a player", "<player> (reason)", UserGroup.Admin, 1, true, 1, OnCommandKick));
            }
            
            if (!string.IsNullOrEmpty(_cmdBan))
            {
                _mIngameCommands.Add(_cmdBan,
                    new Command(_cmdBan, "ban a player", "<@uid/player> (reason)", UserGroup.Admin, 1, true, 1, true, OnCommandBan));
            }
            
            if (!string.IsNullOrEmpty(_cmdUnban))
            {
                _mIngameCommands.Add(_cmdUnban,
                    new Command(_cmdUnban, "unban a player", "<@uid>", UserGroup.Admin, 1, true, 0, true, OnCommandUnBan));
            }
            
            if (!string.IsNullOrEmpty(_cmdLookup))
            {
                _mIngameCommands.Add(_cmdLookup,
                    new Command(_cmdLookup, "search a player in database", "<@uid/name>", UserGroup.Admin, 1, true, 0, true, OnCommandLookUp));
            }
            
            if (!string.IsNullOrEmpty(_cmdPutGroup))
            {
                _mIngameCommands.Add(_cmdPutGroup,
                    new Command(_cmdPutGroup, "put user in specified group", "<@uid/name>", UserGroup.SuperAdmin, 1, true, 1, true, OnCommandPutGroup));
            }
            
            if (!string.IsNullOrEmpty(_cmdPenalties))
            {
                _mIngameCommands.Add(_cmdPenalties,
                    new Command(_cmdPenalties, "list previous penalties", "(penalty)", UserGroup.SuperAdmin, 0, false, 0, false, OnCommandPenalties));
            }

        }

        private void CommandHandler(string speaker, string message)
        {
            const string pattern = @"^/?(?<scope>!|@|/)(?<command>[^\s]+)[ ]?(?<arguments>.*)";
            Match match = Regex.Match(message, pattern, RegexOptions.IgnoreCase);
            if (!match.Success) return;

            string scope = match.Groups["scope"].Value;
            string invoke = match.Groups["command"].Value.ToLower();
            string arguments = match.Groups["arguments"].Value;
            
            if (invoke.Equals("iamowner", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(arguments))
            {
                OnCommandIAmOwner(speaker);
                return;
            }

            if (!_mIngameCommands.ContainsKey(invoke)) return;
            Command cmd = _mIngameCommands[invoke];
            if (!HadCommandPrivileges(speaker, cmd)) return;
            CapturedCommand capturedCommand = new CapturedCommand(scope, invoke, arguments);
            
            if (cmd.MinArgs > 0)
            {
                if (string.IsNullOrEmpty(arguments))
                {
                    PlayerSayMsg(speaker, "Incorrect command usage! Missing arguments");
                    PlayerSayMsg(speaker, cmd.GetHelp());
                    return;
                }
            }
            
            int uid = 0;
            if (cmd.CanAcceptUid && arguments.StartsWith("@"))
            {
                string[] split = arguments.Split(' ');
                string id = split.FirstOrDefault()?.Replace("@", "");
                int.TryParse(id, out uid);
            }
            
            if (uid > 0)
            {
                capturedCommand.UserId = uid;
                string[] strings = arguments.Split(' ').Skip(1).ToArray();
                capturedCommand.ExtraArguments = string.Join(" ", strings);
                DebugWrite("[CommandHandler] Command with UID - " + scope + invoke + " @" + uid, 4);
            }

            if (uid == 0 && cmd.ArgsSoldiers > 0)
            {
                capturedCommand = cmd.ParseArguments(arguments, new List<string>(_mDicPlayerInfo.Keys));
                if (capturedCommand == null)
                {
                    PlayerSayMsg(speaker, "No matching players found!");
                    return;
                }
            }

            if (cmd.RequiresConfirmation && !cmd.CanAcceptUid)
            {
                PlayerSayMsg(speaker, string.Format("Did you mean? {0}", capturedCommand));
                if (_mPendingConfirmations.ContainsKey(speaker))
                    _mPendingConfirmations[speaker] = new ConfirmationEntry(cmd, capturedCommand);
                else
                    _mPendingConfirmations.Add(speaker, new ConfirmationEntry(cmd, capturedCommand));
                DebugWrite("[OnCommandConfirm] - Added confirmation entry - Speaker: " + speaker + " Command: " + invoke, 4);
                return;
            }

            cmd.HandleMethod.Invoke(speaker, capturedCommand);
        }
        
        private void OnCommandIAmOwner(string strSpeaker)
        {
            DebugWrite("[COMMAND] [IAMOWNER] - speaker: " + strSpeaker, 2);
            bool ownerExists = CheckOwnerExists();
            if (ownerExists)
            {
                PlayerSayMsg(strSpeaker, "Oops! I already have my master setup");
                return;
            }

            bool success = PutGroup(_mAuthPlayerInfo[strSpeaker].Id, UserGroup.Admin);

            string message;
            if (success)
            {
                _mAuthPlayerInfo[strSpeaker].UserGroup = (int) UserGroup.Admin;
                message = "You are now my master. Type !help for available commands";
            }
            else
            {
                message = "Err! There was some error connecting to the database";
            }
            PlayerSayMsg(strSpeaker, message);
        }

        private void OnCommandConfirm(string strSpeaker, CapturedCommand capCommand)
        {
            DebugWrite("[COMMAND] [CONFIRM] - " + strSpeaker, 2);
            if (!_mPendingConfirmations.ContainsKey(strSpeaker))
            {
                DebugWrite("[OnCommandConfirm] - " + strSpeaker + " - No command to be confirmed", 4);
                return;
            }

            ConfirmationEntry entry = _mPendingConfirmations[strSpeaker];
            Command cmd = entry.Command;

            try
            {
                cmd.HandleMethod.Invoke(strSpeaker, entry.CapturedCommand);
            }
            finally
            {
                _mPendingConfirmations.Remove(strSpeaker);
            }
        }

        private void OnCommandHelp(string strSpeaker, CapturedCommand capCommand)
        {
            if (capCommand.ExtraArguments.Length == 0)
            {
                DebugWrite("[COMMAND] [HELP] - " + strSpeaker, 2);
                string message = "[Available Commands] - ";
                string[] availableCommands = _mIngameCommands.Keys.Where(i => _mIngameCommands[i].UserGroupId <= _mAuthPlayerInfo[strSpeaker].UserGroup).ToArray();
                message += string.Join(", ", availableCommands);
                PlayerSayMsg(strSpeaker, message);
            }
            else
            {
                string cmdName = capCommand.ExtraArguments;
                DebugWrite("[COMMAND] [HELP] - " + strSpeaker + " - " + cmdName, 4);

                if (_mIngameCommands.ContainsKey(cmdName.ToLower()))
                {
                    Command cmd = _mIngameCommands[cmdName.ToLower()];
                    if (cmd.UserGroupId > _mAuthPlayerInfo[strSpeaker].UserGroup) return;
                    PlayerSayMsg(strSpeaker, cmd.GetHelp());
                }
                else
                {
                    PlayerSayMsg(strSpeaker, "No command found matching " + cmdName);
                }
            }
        }

        private void OnCommandAdminsOnline(string strSpeaker, CapturedCommand capCommand)
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
        
        private void OnCommandKillMe(string strSpeaker, CapturedCommand capCommand)
        {
            DebugWrite("[COMMAND] [KILLME] - " + strSpeaker, 2);
            if (string.IsNullOrEmpty(capCommand.ExtraArguments)) return;
            Kill(strSpeaker);
        }

        private void OnCommandSay(string strSpeaker, CapturedCommand capCommand)
        {
            if (string.IsNullOrEmpty(capCommand.ExtraArguments)) return;
            DebugWrite("[COMMAND] [SAY] - " + strSpeaker, 2);
            SayMsg(capCommand.ExtraArguments);
        }

        private void OnCommandPlayerSay(string strSpeaker, CapturedCommand capCommand)
        {
            DebugWrite("[COMMAND] [PLAYER-SAY] - " + strSpeaker, 2);
            string target = capCommand.MatchedArguments[0].Argument;
            string message = capCommand.ExtraArguments;
            if (!IsPlayerInfoCached(target)) return;
            PlayerSayMsg(target, message);
        }

        private void OnCommandYell(string strSpeaker, CapturedCommand capCommand)
        {
            if (string.IsNullOrEmpty(capCommand.ExtraArguments)) return;
            DebugWrite("[COMMAND] [YELL] - " + strSpeaker, 2);
            YellMsg(capCommand.ExtraArguments);
        }

        private void OnCommandPlayerYell(string strSpeaker, CapturedCommand capCommand)
        {
            DebugWrite("[COMMAND] [PLAYER-YELL] - " + strSpeaker, 2);
            string target = capCommand.MatchedArguments[0].Argument;
            string message = capCommand.ExtraArguments;
            if (!IsPlayerInfoCached(target)) return;
            PlayerYellMsg(target, message);
        }

        private void OnCommandSwap(string strSpeaker, CapturedCommand capCommand)
        {
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

        private void OnCommandKill(string strSpeaker, CapturedCommand capCommand)
        {
            string target = capCommand.MatchedArguments[0].Argument;
            string reason = capCommand.ExtraArguments;
            DebugWrite("[COMMAND] [KILL] - Admin: " + strSpeaker + " Target: " + target, 2);

            if (!IsPlayerInfoCached(target)) return;
            if (!IsAuthenticated(target))
            {
                PlayerSayMsg(strSpeaker, "Failed to authenticate player. Try again later");
                return;
            }
            if (_mAuthPlayerInfo[target].UserGroup >= _mAuthPlayerInfo[strSpeaker].UserGroup)
            {
                PlayerSayMsg(strSpeaker, "You cannot use this command on same or higher group users");
                return;
            }
            string message = target + " is killed by " + strSpeaker;
            if (!string.IsNullOrEmpty(reason)) message += " for " + reason;

            Kill(target);
            SayMsg(message);
            
            AuthSoldier adminData = _mAuthPlayerInfo[strSpeaker];
            AuthSoldier targetData = _mAuthPlayerInfo[target];
            AddPenaltyToDb(targetData.Id, adminData.Id, reason, Penalty.Kill);
        }
        
        private void OnCommandCurse(string strSpeaker, CapturedCommand capCommand)
        {
            string reason = capCommand.ExtraArguments;
            string target;
            bool success;

            if (capCommand.UserId > 0)
            {
                DebugWrite("[COMMAND] [CURSE] - Admin: " + strSpeaker + " UserId: " + capCommand.UserId, 2);
                AuthSoldier client = LookupClient(capCommand.UserId);
                target = client.SoldierName;
                if (string.IsNullOrEmpty(target))
                {
                    PlayerSayMsg(strSpeaker, "No users found with userId: " + target);
                    return;
                }

                if (client.UserGroup >= _mAuthPlayerInfo[strSpeaker].UserGroup)
                {
                    PlayerSayMsg(strSpeaker, "You cannot use this command on same or higher group users");
                    return;
                }

                if (client.PenaltyType == (int) Penalty.Curse)
                {
                    PlayerSayMsg(strSpeaker, target + " is already cursed");
                    return;
                }
                
                success = AddPenaltyToDb(capCommand.UserId, _mAuthPlayerInfo[strSpeaker].Id, reason, Penalty.Curse);
                if (success)
                {
                    string match = FindIngameSoldierFromId(capCommand.UserId);
                    if (!string.IsNullOrEmpty(match))
                    {
                        _lCursedPlayers.Add(match);
                    }
                }
            }
            else
            {
                target = capCommand.MatchedArguments[0].Argument;
                DebugWrite("[COMMAND] [CURSE] - Admin: " + strSpeaker + " Target: " + target, 2);
                if (!IsPlayerInfoCached(target)) return;
                if (!IsAuthenticated(target))
                {
                    PlayerSayMsg(strSpeaker, "Failed to authenticate player. Try again later");
                    return;
                }
                if (_mAuthPlayerInfo[target].UserGroup >= _mAuthPlayerInfo[strSpeaker].UserGroup)
                {
                    PlayerSayMsg(strSpeaker, "You cannot use this command on same or higher group users");
                    return;
                }
                if (_lCursedPlayers.Contains(target))
                {
                    PlayerSayMsg(strSpeaker, target + " is already cursed");
                    return;
                }
                
                AuthSoldier adminData = _mAuthPlayerInfo[strSpeaker];
                AuthSoldier targetData = _mAuthPlayerInfo[target];
                
                success = AddPenaltyToDb(targetData.Id, adminData.Id, reason, Penalty.Curse);
                if (success) _lCursedPlayers.Add(target);
            }

            if (success)
            {
                string message = target + " is cursed by " + strSpeaker;
                if (!string.IsNullOrEmpty(reason)) message += " for " + reason;
                SayMsg(message);
            }
            else
            {
                PlayerSayMsg(strSpeaker, "Failed to curse " + target + ". Database error!");
            }
            
        }

        private void OnCommandUnCurse(string strSpeaker, CapturedCommand capCommand)
        {
            string target;
            bool success;
            if (capCommand.UserId > 0)
            {
                DebugWrite("[COMMAND] [UNCURSE] - Admin: " + strSpeaker + " UserId: " + capCommand.UserId, 2);
                AuthSoldier client = LookupClient(capCommand.UserId);
                target = client.SoldierName;
                if (string.IsNullOrEmpty(target))
                {
                    PlayerSayMsg(strSpeaker, "No users found with userId: " + target);
                    return;
                }

                if (client.PenaltyType != (int) Penalty.Curse)
                {
                    PlayerSayMsg(strSpeaker, target + " is not cursed");
                    return;
                }
                
                success = RemovePenalty(client.Id);
            }
            else
            {
                target = capCommand.MatchedArguments[0].Argument;
                DebugWrite("[COMMAND] [UNCURSE] - Admin: " + strSpeaker + " Target: " + target, 2);
                if (!IsPlayerInfoCached(target)) return;
                if (!_lCursedPlayers.Contains(target))
                {
                    string message = target + " is not cursed";
                    PlayerSayMsg(strSpeaker, message);
                    return;
                }

                if (!IsAuthenticated(target))
                {
                    PlayerSayMsg(strSpeaker, "Failed to authenticate player. Try again later");
                    return;
                }

                AuthSoldier targetData = _mAuthPlayerInfo[target];
                success = RemovePenalty(targetData.Id);
            }

            if (success)
            {
                string message = target + " is uncursed by " + strSpeaker;
                SayMsg(message);
                if (_lCursedPlayers.Contains(target)) _lCursedPlayers.Remove(target);
            }
            else
            {
                PlayerSayMsg(strSpeaker, "Failed to uncurse " + target + ". Database error!");
            }
        }

        private void OnCommandKick(string strSpeaker, CapturedCommand capCommand)
        {
            string target = capCommand.MatchedArguments[0].Argument;
            string reason = capCommand.ExtraArguments;
            DebugWrite("[COMMAND] [KICK] - Admin: " + strSpeaker + " Target: " + target, 2);

            if (!IsPlayerInfoCached(target)) return;
            if (!IsAuthenticated(target))
            {
                PlayerSayMsg(strSpeaker, "Failed to authenticate player. Try again later");
                return;
            }
            if (_mAuthPlayerInfo[target].UserGroup >= _mAuthPlayerInfo[strSpeaker].UserGroup)
            {
                PlayerSayMsg(strSpeaker, "You cannot use this command on same or higher group users");
                return;
            }
            
            string message = target + " is kicked by " + strSpeaker;
            if (!string.IsNullOrEmpty(reason)) message += " for " + reason;

            SayMsg(message);
            Kick(target, reason);

            AuthSoldier adminData = _mAuthPlayerInfo[strSpeaker];
            AuthSoldier targetData = _mAuthPlayerInfo[target];
            AddPenaltyToDb(targetData.Id, adminData.Id, reason, Penalty.Kick);
        }

        private void OnCommandBan(string strSpeaker, CapturedCommand capCommand)
        {
            string reason = capCommand.ExtraArguments;
            bool success;
            string target;
            if (capCommand.UserId > 0)
            {
                DebugWrite("[COMMAND] [BAN] - Admin: " + strSpeaker + " UserId: " + capCommand.UserId, 2);
                AuthSoldier client = LookupClient(capCommand.UserId);
                target = client.SoldierName;
                if (string.IsNullOrEmpty(target))
                {
                    PlayerSayMsg(strSpeaker, "No users found with userId: " + target);
                    return;
                }
                if (client.UserGroup >= _mAuthPlayerInfo[strSpeaker].UserGroup)
                {
                    PlayerSayMsg(strSpeaker, "You cannot use this command on same or higher group users");
                    return;
                }
                if (client.PenaltyType == (int) Penalty.Ban)
                {
                    PlayerSayMsg(strSpeaker, target + " is already banned");
                    return;
                }
                
                success = AddPenaltyToDb(capCommand.UserId, _mAuthPlayerInfo[strSpeaker].Id, reason, Penalty.Ban);
                if (success)
                {
                    ConsoleWarn("Success executed BAN");
                    string match = FindIngameSoldierFromId(capCommand.UserId);
                    if (!string.IsNullOrEmpty(match))
                    {
                        Kick(match, "You are banned by " + strSpeaker);
                    }
                }
            }
            else
            {
                target = capCommand.MatchedArguments[0].Argument;
                DebugWrite("[COMMAND] [BAN] - Admin: " + strSpeaker + " Target: " + target, 2);
                if (!IsPlayerInfoCached(target)) return;
                if (!IsAuthenticated(target))
                {
                    PlayerSayMsg(strSpeaker, "Failed to authenticate player. Try again later");
                    return;
                }
                if (_mAuthPlayerInfo[target].UserGroup >= _mAuthPlayerInfo[strSpeaker].UserGroup)
                {
                    PlayerSayMsg(strSpeaker, "You cannot use this command on same or higher group users");
                    return;
                }
                AuthSoldier adminData = _mAuthPlayerInfo[strSpeaker];
                AuthSoldier targetData = _mAuthPlayerInfo[target];

                success = AddPenaltyToDb(targetData.Id, adminData.Id, reason, Penalty.Ban);
                if (success) Kick(target, "You are banned by " + strSpeaker);
            }

            if (success)
            {
                string message = target + " is banned by " + strSpeaker;
                if (!string.IsNullOrEmpty(reason)) message += " for " + reason;
                SayMsg(message);
            }
            else
            {
                PlayerSayMsg(strSpeaker, "Failed to Ban " + target + ". Database error!");
            }
        }

        private void OnCommandUnBan(string strSpeaker, CapturedCommand capCommand)
        {
            if (capCommand.UserId == 0) return;
            
            DebugWrite("[COMMAND] [UNBAN] - " + strSpeaker + " UserId: " + capCommand.UserId, 2);
            AuthSoldier client = LookupClient(capCommand.UserId);
            if (client.Id == 0)
            {
                PlayerSayMsg(strSpeaker, "No user found with userId: " + capCommand.UserId);
                return;
            }

            if (client.PenaltyType != (int) Penalty.Ban)
            {
                PlayerSayMsg(strSpeaker, client.SoldierName + " (" + client.Id + ") " + "is not banned!");
                return;
            }

            string message = RemovePenalty(client.Id)
                ? client.SoldierName + " (" + client.Id + ") " + "is unbanned successfully!"
                : "Failed to unban " + client.SoldierName;

            PlayerSayMsg(strSpeaker, message);
        }

        private void OnCommandLookUp(string strSpeaker, CapturedCommand capCommand)
        {
            if (capCommand.UserId > 0)
            {
                DebugWrite("[COMMAND] [LOOKUP] - " + strSpeaker + " UserId: " + capCommand.UserId, 2);
                AuthSoldier client = LookupClient(capCommand.UserId);
                string message = client.Id == 0 ? "No user found with userId: " + capCommand.UserId : "Player found: " + client.SoldierName + " (" + capCommand.UserId + ")";
                PlayerSayMsg(strSpeaker, message);
            }
            else
            {
                DebugWrite("[COMMAND] [LOOKUP] - " + strSpeaker + " Match: " + capCommand.ExtraArguments, 2);
                string matchingName = capCommand.ExtraArguments;
                if (matchingName.Length < 3)
                {
                    PlayerSayMsg(strSpeaker, "Please provide a minimum of 3 characters");
                    return;
                }

                List<string[]> players = LookupClient(matchingName);
                if (players.Count == 0)
                {
                    PlayerSayMsg(strSpeaker, "No matching players found. Try being more specific");
                    return;
                }
                
                IEnumerable<string> enumerable = players.Select(value => value[1] + " (" + value[0] + ")");
                string matchingPlayers = "Matching Players: ";
                matchingPlayers += string.Join(", ", enumerable);
                PlayerSayMsg(strSpeaker, matchingPlayers);
            }
        }

        private void OnCommandPutGroup(string strSpeaker, CapturedCommand capCommand)
        {
            UserGroup group;
            try
            {
                group = (UserGroup) Enum.Parse(typeof(UserGroup), capCommand.ExtraArguments, true);
            }
            catch (Exception)
            {
                PlayerSayMsg(strSpeaker, "Did you enter a valid group?");
                return;
            }

            bool success;
            string target;

            if (capCommand.UserId > 0)
            {
                DebugWrite("[COMMAND] [PUTGROUP] - Admin: " + strSpeaker + " UserId: " + group, 2);
                AuthSoldier client = LookupClient(capCommand.UserId);
                target = client.SoldierName;
                if (string.IsNullOrEmpty(target))
                {
                    PlayerSayMsg(strSpeaker, "No users found with userId: " + capCommand.UserId);
                    return;
                }
                
                if (client.UserGroup >= _mAuthPlayerInfo[strSpeaker].UserGroup)
                {
                    PlayerSayMsg(strSpeaker, "You cannot use this command on same or higher group users");
                    return;
                }

                if (client.UserGroup == (int) group)
                {
                    PlayerSayMsg(strSpeaker, target + " is already a " + group);
                    return;
                }

                success = PutGroup(client.Id, group);
                if (success)
                {
                    KeyValuePair<string, AuthSoldier> match = _mAuthPlayerInfo.First(kvp => kvp.Value.Id == capCommand.UserId);
                    match.Value.UserGroup = (int) group;
                    if (group > UserGroup.Vip) _lAdminsOnline.Add(match.Key);
                }
            }
            else
            {
                target = capCommand.MatchedArguments[0].Argument;
                DebugWrite("[COMMAND] [PUTGROUP] - Admin: " + strSpeaker + " Target: " + target, 2);
                if (!IsPlayerInfoCached(target)) return;
                if (!IsAuthenticated(target))
                {
                    PlayerSayMsg(strSpeaker, "Failed to authenticate player. Try again later");
                    return;
                }

                AuthSoldier targetData = _mAuthPlayerInfo[target];
                success = PutGroup(targetData.Id, group);
                if (success)
                {
                    _mAuthPlayerInfo[target].UserGroup = (int) group;
                    if (group > UserGroup.Vip) _lAdminsOnline.Add(target);
                }
            }

            if (success)
            {
                string message = target + " is put in group " + group + " by " + strSpeaker;
                SayMsg(message);
            }
            else
            {
                PlayerSayMsg(strSpeaker, "Failed to put " + target + " in " + group + "!");
            }
        }
        
        private void OnCommandPenalties(string strSpeaker, CapturedCommand capCommand)
        {
            Penalty penalty;
            try
            {
                penalty = (Penalty) Enum.Parse(typeof(Penalty), capCommand.ExtraArguments, true);
            }
            catch (Exception)
            {
                if (!string.IsNullOrEmpty(capCommand.ExtraArguments))
                {
                    PlayerSayMsg(strSpeaker, "Not a valid penalty type. Fetching all penalties...");
                }
                penalty = Penalty.None;
            }
            
            DebugWrite("[COMMAND] [PENALTIES] - " + strSpeaker, 2);
            List<PenaltyLog> penaltyLogs = GetPenalties(penalty);

            if (penaltyLogs.Count == 0)
            {
                PlayerSayMsg(strSpeaker, "No recent penalties found");
                return;
            }
            
            PlayerSayMsg(strSpeaker, "==== Recent Penalties ====");
            foreach (string msg in penaltyLogs.Select(log => "[" + log.Penalty + "] - " + log.Player + "(" + log.PlayerId + ") by " + log.Admin + "(" + log.AdminId + ") | Reason: " + log.Reason))
            {
                PlayerSayMsg(strSpeaker, msg);
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
            if (myException.Source != null) DebugWrite("^1Source: " + myException.Source + "^0", 4);
            if (myException.StackTrace != null) DebugWrite("^1StackTrace: " + myException.StackTrace + "^0", 4);
            if (myException.InnerException != null) DebugWrite("^1InnerException: " + myException.InnerException + "^0", 4);
        }

        public void CheckSettingsSql()
        {
            // basic checks after plugin enabled
            // check 1. sql logins
            DebugWrite("[Task] [Check] Checking SQL Settings", 4);
            if (!_fIsEnabled || !SqlLoginOk()) { return; }
            
            // check 2. sql database connection
            DebugWrite("[Task] [Check] Try to connect to SQL Server", 4);
            try
            {
                using (MySqlConnection conn = new MySqlConnection(SqlLogin()))
                {
                    conn.Open();
                    DebugWrite("[Task] [Check] Test Connection to SQL was successfully", 4);
                    if (conn.State == ConnectionState.Open)
                    {
                        DebugWrite("[Task] [Check] Close SQL Connection (Con)", 4);
                        conn.Close();
                    }
                }
                _firstCheck = true;
                BuildRequiredTables();
            }
            catch (Exception ex)
            {
                ConsoleError("[Task] [Check] FATAL ERROR: CAN NOT CONNECT TO SQL SERVER! Please check your Plugin settings 'SQL Server Details' (Host IP, Port, Database, Username, PW) and try again. Maybe the provider of your Procon Layer block the connection (server firewall settings).");
                ConsoleError("[Task] [Check] Error (Con): " + ex);
                ConsoleError("[Task] [Check] Error");
                ConsoleError("[Task] [Check] Shutdown Plugin...");
                ExecuteCommand("procon.protected.plugins.enable", "InfinityPlugin", "False");
            }
        }

        private string SqlLogin()
        {
            return "Server=" + _settingStrSqlHostname + ";" + "Port=" + _settingStrSqlPort + ";" + "Database=" + _settingStrSqlDatabase + ";" + "Uid=" + _settingStrSqlUsername +
                   ";" + "Pwd=" + _settingStrSqlPassword + ";" + "Connection Timeout=5;";
        }

        private bool SqlLoginOk()
        {
            if (!string.IsNullOrEmpty(_settingStrSqlHostname) && !string.IsNullOrEmpty(_settingStrSqlPort) && !string.IsNullOrEmpty(_settingStrSqlDatabase) &&
                !string.IsNullOrEmpty(_settingStrSqlUsername) && !string.IsNullOrEmpty(_settingStrSqlPassword))
            {
                return true;
            }
            else
            {
                ConsoleWrite("[SqlLoginDetails]^8^b SQL Server Details not completed (Host IP, Port, Database, Username, PW). Please check your Plugin settings.^0^n");
                DebugWrite(
                    "[SqlLoginDetails] SQL Details: Host=`" + _settingStrSqlHostname + "` ; Port=`" + _settingStrSqlPort + "` ; Database=`" + _settingStrSqlDatabase +
                    "` ; Username=`" + _settingStrSqlUsername + "` ; Password=`" + _settingStrSqlPassword + "`", 2);
                if (_fIsEnabled)
                {
                    ExecuteCommand("procon.protected.plugins.enable", "InfinityPlugin", "False");
                }

                return false;
            }
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
                    try
                    {
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
            bool clientsTableExists = false;
            bool aliasesTableExists = false;
            bool penaltiesTableExists = false;
            bool groupsTableExists = false;
            const string query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA=@Database";
            using (MySqlCommand myCmd = new MySqlCommand(query))
            {
                myCmd.Parameters.AddWithValue("@Database", _settingStrSqlDatabase);
                DataTable resultTable = SqlQuery(myCmd, "BuildRequiredTables");

                foreach (DataRow row in resultTable.Rows)
                {
                    string table = row["TABLE_NAME"].ToString();
                    if (table.Equals("clients")) clientsTableExists = true;
                    if (table.Equals("aliases")) aliasesTableExists = true;
                    if (table.Equals("penalties")) penaltiesTableExists = true;
                    if (table.Equals("groups")) groupsTableExists = true;
                }
            }

            bool creationStatus = true;
            if (!clientsTableExists)
            {
                ConsoleWrite("Creating clients table");
                const string clientsTable = @"CREATE TABLE IF NOT EXISTS `clients` (
                                    `id` INT( 10 ) UNSIGNED NOT NULL AUTO_INCREMENT,
                                    `name` VARCHAR( 50 ) NOT NULL DEFAULT '',
                                    `guid` VARCHAR( 36 ) NOT NULL DEFAULT '',
				                    `user_group` INT( 1 ) UNSIGNED NOT NULL DEFAULT 0,
				                    `penalty_type` INT( 1 ) UNSIGNED NOT NULL DEFAULT 0,
                                    `time_add` INT ( 11 ) UNSIGNED NOT NULL DEFAULT 0,
                                    `time_edit` INT ( 11 ) UNSIGNED NOT NULL DEFAULT 0,
                                    PRIMARY KEY ( `id` )
                                    ) ENGINE = INNODB DEFAULT CHARSET = utf8;";
                
                if (!SqlNonQuery(clientsTable, "Clients-Table")) creationStatus = false;
            }

            if (creationStatus && !penaltiesTableExists)
            {
                ConsoleWrite("Creating penalties table");
                const string penaltiesTable = @"CREATE TABLE IF NOT EXISTS `penalties` (
                                    `id` INT( 11 ) UNSIGNED NOT NULL AUTO_INCREMENT,
                                    `type` INT( 1 ) UNSIGNED NOT NULL DEFAULT 0,
                                    `client_id` INT( 11 ) UNSIGNED NOT NULL,
                                    `admin_id` INT( 11 ) UNSIGNED NOT NULL,
                                    `reason` VARCHAR( 255 ) NOT NULL DEFAULT '',
                                    `server` VARCHAR( 150 ) NOT NULL DEFAULT '',
                                    `time_add` INT ( 11 ) NOT NULL DEFAULT 0,
                                    `time_edit` INT ( 11 ) NOT NULL DEFAULT 0,
                                    PRIMARY KEY ( `id` )
                                    ) ENGINE = INNODB DEFAULT CHARSET = utf8;";
                if (!SqlNonQuery(penaltiesTable, "Penalties-Table")) creationStatus = false;
            }

            if (creationStatus && !aliasesTableExists)
            {
                ConsoleWrite("Creating aliases table");
                const string aliasesTable = @"CREATE TABLE IF NOT EXISTS `aliases` (
                                    `id` INT( 10 ) UNSIGNED NOT NULL AUTO_INCREMENT,
                                    `alias` VARCHAR( 50 ) NOT NULL DEFAULT '',
                                    `client_id` INT( 10 ) UNSIGNED NOT NULL DEFAULT 0,
                                    `time_add` INT ( 10 ) UNSIGNED NOT NULL DEFAULT 0,
                                    PRIMARY KEY ( `id` )
                                    ) ENGINE = INNODB DEFAULT CHARSET = utf8;";
                if (!SqlNonQuery(aliasesTable, "Aliases-Table")) creationStatus = false;
            }

            if (creationStatus && !groupsTableExists)
            {
                ConsoleWrite("Creating groups table");
                const string groups = @"CREATE TABLE IF NOT EXISTS `groups` (
                                    `id` INT( 11 ) NOT NULL,
                                    `name` VARCHAR(32) NOT NULL DEFAULT '',
                                    `level` INT( 11 ) NOT NULL DEFAULT 0,
                                    PRIMARY KEY (`id`),
                                    KEY `level` (`level`)
                                    ) ENGINE = INNODB DEFAULT CHARSET = utf8;" +
                                      "INSERT INTO `groups` (`id`, `name`, `level`) VALUES (0, 'User', 0);" +
                                      "INSERT INTO `groups` (`id`, `name`, `level`) VALUES (1, 'Vip', 1);" +
                                      "INSERT INTO `groups` (`id`, `name`, `level`) VALUES (2, 'Admin', 60);" +
                                      "INSERT INTO `groups` (`id`, `name`, `level`) VALUES (3, 'Super Admin', 80);" +
                                      "INSERT INTO `groups` (`id`, `name`, `level`) VALUES (4, 'Owner', 100);";

                if (!SqlNonQuery(groups, "Groups-Table")) creationStatus = false;

            }
            
            if (!creationStatus)
            {
                ConsoleError("[Task] [Check] Failed to create required tables");
                ConsoleError("[Task] [Check] Shutdown Plugin...");
                ExecuteCommand("procon.protected.plugins.enable", "InfinityPlugin", "False");
                return;
            }
            
            _tableExists = true;
            ConsoleWrite("Required Tables Exists/Created");
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
            if (!_mDicPlayerInfo.ContainsKey(name))
            {
                return false;
            }

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
            if (!_mDicPlayerInfo.ContainsKey(name))
            {
                return;
            }

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
            int penaltyType = (int) penalty;

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

        private AuthSoldier LookupClient(int uid)
        {
            if (!_tableExists) return null;
            const string sql1 = @"SELECT * FROM `clients` WHERE id = @Id";
            using (MySqlCommand myCmd = new MySqlCommand(sql1))
            {
                myCmd.Parameters.AddWithValue("@Id", uid);
                DataTable resultTable = SqlQuery(myCmd, "LookupClient");
                if (resultTable.Rows.Count == 0)
                {
                    DebugWrite("[SQL-LookupClient]: No user found with id: " + uid, 4);
                    return new AuthSoldier();
                }
                else
                {
                    DebugWrite("[SQL-LookupClient]: User found with id= " + uid, 4);
                    return new AuthSoldier(resultTable.Rows[0]);
                }
            }
        }
        
        private List<string[]> LookupClient(string matchingName)
        {
            List<string[]> names = new List<string[]>();
            if (!_tableExists) return names;

            const string query =
                "SELECT clients.id, clients.name, clients.user_group, clients.penalty_type FROM `clients` WHERE name LIKE @Match UNION ALL " +
                "SELECT aliases.client_id as id, aliases.alias as name, clients.user_group, clients.penalty_type FROM `aliases` INNER JOIN `clients` ON aliases.client_id = clients.id "+
                "WHERE aliases.alias LIKE @Match LIMIT 5";

            using (MySqlCommand myCmd = new MySqlCommand(query))
            {
                myCmd.Parameters.AddWithValue("@Match", "%" + matchingName + "%");
                DataTable resultTable = SqlQuery(myCmd, "LookupClient");
                if (resultTable.Rows.Count == 0)
                {
                    DebugWrite("[SQL-LookupClient]: No user found matching: " + matchingName, 4);
                }
                else
                {
                    DebugWrite("[SQL-LookupClient]: " + resultTable.Rows.Count +" Users found matching: " + matchingName, 4);
                    names.AddRange(from DataRow row in resultTable.Rows select new[] {row["id"].ToString(), row["name"].ToString()});
                }
            }

            return names;
        }

        private bool PutGroup(int uid, UserGroup group)
        {
            if (!_tableExists) return false;
            int groupId = (int) group;
            const string sql = "UPDATE clients SET user_group = @GroupId WHERE id = @Id;";
            using (MySqlCommand myCmd = new MySqlCommand(sql))
            {
                myCmd.Parameters.AddWithValue("@GroupId", groupId);
                myCmd.Parameters.AddWithValue("@Id", uid);
                return SqlNonQuery(myCmd, "PutGroup");
            }
        }

        private bool CheckOwnerExists()
        {
            if (!_tableExists) return false;
            const string query = @"SELECT * FROM `clients` WHERE user_group = 100 LIMIT 1";
            using (MySqlCommand myCmd = new MySqlCommand(query))
            {
                SqlQuery(myCmd, "CheckOwnerExists");
                DataTable resultTable = SqlQuery(myCmd, "LookupClient");
                if (resultTable.Rows.Count > 0)
                {
                    DebugWrite("[SQL-CheckOwnerExists]: No owner found", 4);
                    return true;
                }
                DebugWrite("[SQL-CheckOwnerExists]: Owner already exists", 4);
                return false;
            }
        }

        private List<PenaltyLog> GetPenalties(Penalty penalty)
        {
            List<PenaltyLog> list = new List<PenaltyLog>();
            if (!_tableExists) return list;
            int penaltyType = (int) penalty;
            
            string sql = "SELECT p.type, p.reason, p.client_id, c1.name as client_name, p.admin_id, c2.name as admin_name FROM penalties as p INNER JOIN clients AS c1 ON c1.id=p.client_id INNER JOIN clients AS c2 ON c2.id=p.admin_id ";

            if (penaltyType > 0)
            {
                sql += "WHERE p.type = @PenaltyType ";
            }

            sql += "ORDER BY p.id DESC LIMIT 5";
            
            using (MySqlCommand myCmd = new MySqlCommand(sql))
            {
                if (penaltyType > 0)
                {
                    myCmd.Parameters.AddWithValue("@PenaltyType", penaltyType);
                }
                DataTable resultTable = SqlQuery(myCmd, "GetPenalties");
                if (resultTable.Rows.Count == 0)
                {
                    DebugWrite("[SQL-GetPenalties]: No penalties found for: " + penalty, 4);
                }
                else
                {
                    DebugWrite("[SQL-GetPenalties]: " + resultTable.Rows.Count +" Penalties found for: " + penalty, 4);
                    list.AddRange(from DataRow row in resultTable.Rows select new PenaltyLog(row));
                }
            }
            return list;
        }

        #endregion

        //////////////////////
        #region HELPER Functions
        //////////////////////

        public void PluginStarter()
        {
            if (!_firstCheck && _fIsEnabled)
            {
                // wait a little bit after layer restart
                DebugWrite("[Task] [PluginStarter] Layer restart detected. Warmup...", 4);
                if ((DateTime.UtcNow - _layerStartingTime).TotalSeconds > 10)
                {
                    Thread threadWorker1 = new Thread(new ThreadStart(delegate() { CheckSettingsSql(); }));
                    threadWorker1.IsBackground = true;
                    threadWorker1.Name = "threadworker1";
                    threadWorker1.Start();
                }
            }
        }
        
        private static long GetTimeEpoch()
        {
            return ((DateTimeOffset) DateTime.Now).ToUnixTimeSeconds();
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
            if (data == null)
            {
                return;
            } // Some DB Exception occured

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
        
        private string FindIngameSoldierFromId(int userId)
        {
            return (from entry in _mAuthPlayerInfo where entry.Value.Id == userId select entry.Key).FirstOrDefault();
        }

        #endregion

        //////////////////////
        #region PROCON EXECUTE Commands
        //////////////////////

        private void PlayerSayMsg(string target, string message)
        {
            if (!_fIsEnabled || message.Length < 3) return;
            
            ExecuteCommand("procon.protected.chat.write", "(PlayerSay " + target + ") " + message.Replace(Environment.NewLine, " "));
            if (message.Length < MaxLineLength)
            {
                ExecuteCommand("procon.protected.send", "admin.say", message.Replace(Environment.NewLine, " "), "player", target);
            }
            else
            {
                int charCount = 0;
                IEnumerable<string> lines = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).GroupBy(w => (charCount += w.Length + 1) / MaxLineLength).Select(g => string.Join(" ", g.ToArray()));
                foreach (string line in lines)
                {
                    ExecuteCommand("procon.protected.send", "admin.say", line, "player", target);
                }
            }
            DebugWrite("[PlayerSayMsg] - Player: " + target + " Message: " + message, 3);
        }

        private void SayMsg(string message)
        {
            if (!_fIsEnabled || message.Length < 3) return;
            
            ExecuteCommand("procon.protected.chat.write", message.Replace(Environment.NewLine, " "));
            if (message.Length < MaxLineLength)
            {
                ExecuteCommand("procon.protected.send", "admin.say", message.Replace(Environment.NewLine, " "), "all");
            }
            else
            {
                int charCount = 0;
                IEnumerable<string> lines = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).GroupBy(w => (charCount += w.Length + 1) / MaxLineLength).Select(g => string.Join(" ", g.ToArray()));
                foreach (string line in lines)
                {
                    ExecuteCommand("procon.protected.send", "admin.say", line, "all");
                }
            }
            DebugWrite("[SayMsg] - " + message, 3);
        }

        private void PlayerYellMsg(string target, string message)
        {
            if (!_fIsEnabled || message.Length < 3) return;
            ExecuteCommand("procon.protected.send", "admin.yell", message.Replace(Environment.NewLine, NewLiner), StrYellDuration(), "player", target);
            ExecuteCommand("procon.protected.chat.write", "(PlayerYell " + target + ") " + message.Replace(Environment.NewLine, "  -  "));
            DebugWrite("[PlayerYellMsg] - Player: " + target + " Message: " + message, 3);
        }

        private void YellMsg(string message)
        {
            if (!_fIsEnabled || message.Length < 3) return;
            ExecuteCommand("procon.protected.send", "admin.yell", message.Replace(Environment.NewLine, NewLiner), StrYellDuration(), "all");
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
            if (teamId <= 0 || squadId < 0) return;
            ExecuteCommand("procon.protected.send", "admin.say", message, "squad", teamId.ToString(), squadId.ToString());
            ExecuteCommand("procon.protected.chat.write", "(SquadSay " + teamId + "" + squadId + ") " + message.Replace(Environment.NewLine, " "));
            DebugWrite("[SquadSay] - Team: " + teamId + " Squad: " + squadId, 3);
        }

        private void Kill(string target)
        {
            if (!_mDicPlayerInfo.ContainsKey(target)) return;
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
        
        private enum Penalty
        {
            None = 0,
            Warn = 1,
            Kill = 2,
            Curse = 3,
            Kick = 4,
            Ban = 5
        }
        
        private enum UserGroup
        {
            User = 0,
            Vip = 1,
            Admin = 60,
            SuperAdmin = 80,
            Owner = 100
        }

        private class PenaltyLog
        {
            public PenaltyLog(DataRow row)
            {
                int value = Convert.ToInt32(row["type"]);
                Penalty = (Penalty) value;
                Player = row["client_name"].ToString();
                PlayerId = Convert.ToInt32(row["client_id"]);
                Admin = row["admin_name"].ToString();
                AdminId = Convert.ToInt32(row["admin_id"]);
                Reason = string.IsNullOrEmpty(row["reason"].ToString()) ? "NA" : row["reason"].ToString();
            }
            
            public Penalty Penalty { get; }
            
            public string Player { get; }
            
            public int PlayerId { get;  }
            
            public string Admin { get; }
            
            public int AdminId { get;  }

            public string Reason { get; }

        }
        
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

            public int Id { get; }

            public string SoldierName { get; }

            public int UserGroup { get; set; }

            public int PenaltyType { get; }
        }
        
        private class Command
        {
            private string Invoke { get; }

            private string Help { get; }

            private string Usage { get; }

            public int UserGroupId { get; }

            public bool RequiresConfirmation { get; }

            public int MinArgs { get; }

            public int ArgsSoldiers { get; }

            public bool CanAcceptUid { get; }

            public Action<string, CapturedCommand> HandleMethod { get; }

            public Command(string invoke, string help, string usage, UserGroup group, int minArgs, bool requiresConfirmation, int argsSoldiers,
                Action<string, CapturedCommand> handleMethod)
            {
                Invoke = invoke;
                Help = help;
                Usage = usage;
                UserGroupId = (int) group;
                RequiresConfirmation = requiresConfirmation;
                MinArgs = minArgs;
                ArgsSoldiers = argsSoldiers;
                CanAcceptUid = false;
                HandleMethod = handleMethod;
            }

            public Command(string invoke, string help, string usage, UserGroup group, int minArgs, bool requiresConfirmation, int argsSoldiers, bool canAcceptUid,
                Action<string, CapturedCommand> handleMethod)
            {
                Invoke = invoke;
                Help = help;
                Usage = usage;
                UserGroupId = (int) group;
                RequiresConfirmation = requiresConfirmation;
                MinArgs = minArgs;
                ArgsSoldiers = argsSoldiers;
                CanAcceptUid = canAcceptUid;
                HandleMethod = handleMethod;
            }

            public string GetHelp()
            {
                string message = "!";
                if (!string.IsNullOrEmpty(Invoke))
                    message += Invoke + " ";
                if (!string.IsNullOrEmpty(Usage))
                    message += Usage + ": ";
                if (!string.IsNullOrEmpty(Help))
                    message += Help;
                return message;
            }

            public CapturedCommand ParseArguments(string arguments, List<string> currentPlayers)
            {
                CapturedCommand capturedCommand = null;
                string remainingArgs = arguments;
                List<MatchArgument> lstMatchedArguments = new List<MatchArgument>();
                int num = 0;
                for (int i = 1; i <= ArgsSoldiers; i++)
                {
                    if (currentPlayers.Count > 0)
                    {
                        string strMatchedDictionaryKey;
                        int closestMatch = GetClosestMatch(remainingArgs, currentPlayers, out strMatchedDictionaryKey, out remainingArgs);
                        if (closestMatch != int.MaxValue)
                            lstMatchedArguments.Add(new MatchArgument(strMatchedDictionaryKey, closestMatch));
                    }

                    if (currentPlayers.Count == 0)
                        ++num;
                }

                if (lstMatchedArguments.Count == ArgsSoldiers - num)
                    capturedCommand = new CapturedCommand("!", Invoke, lstMatchedArguments, remainingArgs);

                return capturedCommand;
            }

            private static int GetClosestMatch(string strArguments, List<string> lstDictionary, out string strMatchedDictionaryKey, out string strRemainderArguments)
            {
                int num1 = int.MaxValue;
                strRemainderArguments = string.Empty;
                strMatchedDictionaryKey = string.Empty;
                if (lstDictionary.Count < 1) return num1;

                int val2 = 0;
                List<MatchDictionaryKey> matchDictionaryKeyList = new List<MatchDictionaryKey>();
                foreach (string lst in lstDictionary)
                {
                    matchDictionaryKeyList.Add(new MatchDictionaryKey(lst));
                    if (lst.Length > val2)
                        val2 = lst.Length;
                }

                for (int index1 = 1; index1 <= Math.Min(strArguments.Length, val2); ++index1)
                {
                    if (index1 + 1 >= strArguments.Length || strArguments[index1] == ' ')
                    {
                        for (int index2 = 0; index2 < matchDictionaryKeyList.Count; ++index2)
                        {
                            int num2 = Compute(strArguments.Substring(0, index1).ToLower(), matchDictionaryKeyList[index2].LowerCaseMatchedText);
                            if (num2 < matchDictionaryKeyList[index2].MatchedScore)
                            {
                                matchDictionaryKeyList[index2].MatchedScore = num2;
                                matchDictionaryKeyList[index2].MatchedScoreCharacters = index1;
                            }
                        }
                    }
                }

                matchDictionaryKeyList.Sort();
                int matchedScoreCharacters = matchDictionaryKeyList[0].MatchedScoreCharacters;
                num1 = matchDictionaryKeyList[0].MatchedScore;
                strMatchedDictionaryKey = matchDictionaryKeyList[0].MatchedText;
                string lower = strArguments.Substring(0, matchedScoreCharacters).ToLower();
                for (int index = 0; index < matchDictionaryKeyList.Count; ++index)
                {
                    if (matchDictionaryKeyList[index].LowerCaseMatchedText.Contains(lower))
                    {
                        num1 = matchDictionaryKeyList[index].MatchedScore;
                        strMatchedDictionaryKey = matchDictionaryKeyList[index].MatchedText;
                        matchedScoreCharacters = matchDictionaryKeyList[index].MatchedScoreCharacters;
                        break;
                    }
                }

                strRemainderArguments = matchedScoreCharacters >= strArguments.Length
                    ? strArguments.Substring(matchedScoreCharacters)
                    : strArguments.Substring(matchedScoreCharacters + 1);

                return num1;
            }

            private static int Compute(string s, string t)
            {
                int length1 = s.Length;
                int length2 = t.Length;
                int[,] numArray = new int[length1 + 1, length2 + 1];
                if (length1 == 0)
                    return length2;
                if (length2 == 0)
                    return length1;
                int index1 = 0;
                while (index1 <= length1)
                    numArray[index1, 0] = index1++;
                int index2 = 0;
                while (index2 <= length2)
                    numArray[0, index2] = index2++;
                for (int index3 = 1; index3 <= length1; ++index3)
                {
                    for (int index4 = 1; index4 <= length2; ++index4)
                    {
                        int num = (int) t[index4 - 1] == (int) s[index3 - 1] ? 0 : 1;
                        numArray[index3, index4] = Math.Min(Math.Min(numArray[index3 - 1, index4] + 1, numArray[index3, index4 - 1] + 1), numArray[index3 - 1, index4 - 1] + num);
                    }
                }

                return numArray[length1, length2];
            }
        }

        public class CapturedCommand
        {
            public string ResponseScope { get; }

            public string Invoke { get; }

            public List<MatchArgument> MatchedArguments { get; }

            public string ExtraArguments { get; set; }

            public int UserId { get; set; }

            public CapturedCommand(string strResponseScope, string strInvoke, string strExtraArguments)
            {
                ResponseScope = strResponseScope;
                Invoke = strInvoke;
                MatchedArguments = new List<MatchArgument>();
                ExtraArguments = strExtraArguments;
                UserId = 0;
            }

            public CapturedCommand(string strResponseScope, string strInvoke, List<MatchArgument> lstMatchedArguments, string strExtraArguments)
            {
                ResponseScope = strResponseScope;
                Invoke = strInvoke;
                MatchedArguments = lstMatchedArguments;
                ExtraArguments = strExtraArguments;
                UserId = 0;
            }

            public override string ToString()
            {
                string str = string.Format("{0}{1}", ResponseScope, Invoke);
                foreach (MatchArgument matchedArgument in MatchedArguments)
                    str = string.Format("{0} {1}", str, matchedArgument.Argument);
                return str;
            }
        }

        private class ConfirmationEntry
        {
            public ConfirmationEntry(Command command, CapturedCommand capturedCommand)
            {
                Command = command;
                CapturedCommand = capturedCommand;
                AddedTime = GetTimeEpoch();
            }

            public Command Command { get; }

            public CapturedCommand CapturedCommand { get; }
            
            public long AddedTime { get; }
        }

        #endregion

    }
    
} // end namespace PRoConEvents
