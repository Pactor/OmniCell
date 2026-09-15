#region License

// Copyright (c) 2005-2014, CellAO Team
// 
// 
// All rights reserved.
// 
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

#endregion

namespace ZoneEngine
{
    #region Usings ...

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using OmniCell.Communication.ISComV2Client;
    using OmniCell.Communication.Messages;
    using OmniCell.Core.Actions;
    using OmniCell.Core.Entities;
    using OmniCell.Core.Events;
    using OmniCell.Core.Items;
    using OmniCell.Core.Nanos;
    using OmniCell.Core.Playfields;
    using OmniCell.Database;
    using OmniCell.Database.Dao;
    using OmniCell.Database.Entities;
    using OmniCell.Enums;
    using OmniCell.Core.Statels;
    using OmniCell.ObjectManager;

    using locales;


    using NLog;

    using SmokeLounge.AOtomation.Messaging.GameData;

    using Utility;
    using Utility.Config;

    using ZoneEngine.Core;
    using ZoneEngine.Core.Combat;
    using ZoneEngine.Core.Controllers;
    using ZoneEngine.Core.Functions;
    using ZoneEngine.Core.Playfields;
    using ZoneEngine.Core.KnuBot;
    using ZoneEngine.Core.Quests;
    using ZoneEngine.Script;

    #endregion

    /// <summary>
    /// Program Class for ZoneEngine
    /// </summary>
    internal class Program
    {
        #region Static Fields

        /// <summary>
        /// </summary>
        public static ISComV2Client ISComClient;

        /// <summary>
        /// </summary>
        public static ZoneServer zoneServer;

        /// <summary>
        /// </summary>
        private static readonly ServerConsoleCommands consoleCommands = new ServerConsoleCommands();

        /// <summary>
        /// </summary>
        private static bool exited = false;

        #endregion

        #region Methods

        /// <summary>
        /// Check the database
        /// </summary>
        /// <param name="parts">
        /// </param>
        private static void CheckDatabase(string[] parts)
        {
            Misc.CheckDatabase();
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        private static bool CheckZoneServerCreation()
        {
            try
            {
                zoneServer = new ZoneServer();
            }
            catch (Exception e)
            {
                LogUtil.ErrorException(e);
                return false;
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="args">
        /// </param>
        private static void CommandLoop(string[] args)
        {
            bool processedargs = false;
            // Say what the server is actually doing before listing the
            // commands. The command list alone reads as a prompt - it says
            // 'Enter "start" to start server' whether or not the server is
            // already up, which made an autostarted engine look like it was
            // sitting there waiting.
            if (zoneServer.IsRunning)
            {
                Colouring.Push(ConsoleColor.Green);
                Console.WriteLine(
                    "Zone server started automatically and is listening on {0}.",
                    zoneServer.TcpEndPoint);
                Console.WriteLine("Nothing to type. Pass -noautostart to start it by hand instead.");
                Colouring.Pop();
            }
            else
            {
                Colouring.Push(ConsoleColor.Yellow);
                Console.WriteLine("Zone server is NOT running.");
                Colouring.Pop();
            }

            Console.WriteLine(locales.ZoneEngineConsoleCommands);

            while (!exited)
            {
                if (!processedargs)
                {
                    if (args.Length == 1)
                    {
                        if (args[0].ToLower() == "/autostart")
                        {
                            Console.WriteLine(locales.ServerConsoleAutostart);
                            StartTheServer();
                        }
                    }

                    processedargs = true;
                }

                string consoleCommand = Console.ReadLine();

                if (consoleCommand != null)
                {
                    if (!consoleCommands.Execute(consoleCommand))
                    {
                        ShowCommandHelp();
                    }
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="sender">
        /// </param>
        /// <param name="e">
        /// </param>
        private static void ConsoleCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (zoneServer != null)
            {
                exited = true;
                ISComClient.ShutDown();
                zoneServer.DisconnectAllClients();
                LogUtil.Debug(DebugInfoDetail.Engine, "Shutting down ZoneEngine hard");
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="messageobject">
        /// </param>
        private static void ISComClientOnReceiveData(object sender, DynamicMessage messageobject)
        {
            zoneServer.ProcessISComMessage(messageobject);
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        private static bool ISComInitialization()
        {
            int port;
            IPAddress chatEngineIp;
            try
            {
                ISComClient = new ISComV2Client();
                string chatip = ConfigReadWrite.Instance.CurrentConfig.ChatIP;
                chatEngineIp = IPAddress.Parse(chatip);
                port = ConfigReadWrite.Instance.CurrentConfig.CommPort;
            }
            catch (Exception e)
            {
                LogUtil.ErrorException(e);
                return false;
            }

            try
            {
                ISComClient.OnReceiveData += ISComClientOnReceiveData;
                ISComClient.Connect(chatEngineIp, port);
            }
            catch (Exception e)
            {
                LogUtil.ErrorException(e);
                return true;
            }

            return true;
        }

        /// <summary>
        /// Initializing methods go here
        /// </summary>
        /// <returns>
        /// true if ok
        /// </returns>
        private static bool Initialize()
        {
            Console.WriteLine();
            Colouring.Push(ConsoleColor.Green);

            if (!InitializeGameFunctions())
            {
                Colouring.Push(ConsoleColor.Red);
                Console.WriteLine(locales.ErrorInitializingGamefunctions);
                Colouring.Pop();
                Colouring.Pop();
                return false;
            }

            if (!InitializeLogAndBug())
            {
                Colouring.Push(ConsoleColor.Red);
                Console.WriteLine(locales.ErrorInitializingNLogNBug);
                Colouring.Pop();
                Colouring.Pop();
                return false;
            }

            if (!CheckZoneServerCreation())
            {
                Colouring.Push(ConsoleColor.Red);
                Console.WriteLine(locales.ErrorCreatingZoneServerInstance);
                Colouring.Pop();
                Colouring.Pop();
                return false;
            }

            if (!ISComInitialization())
            {
                Colouring.Push(ConsoleColor.Red);
                Console.WriteLine(locales.ErrorInitializingISCom);
                Colouring.Pop();
                Colouring.Pop();
                return false;
            }

            if (!InizializeTCPIP())
            {
                Colouring.Push(ConsoleColor.Red);
                Console.WriteLine(locales.ErrorTCPIPSetup);
                Colouring.Pop();
                Colouring.Pop();
                return false;
            }

            if (!Misc.CheckDatabase())
            {
                Colouring.Push(ConsoleColor.Red);
                Console.WriteLine(locales.ErrorInitializingDatabase);
                Colouring.Pop();
                Colouring.Pop();
                return false;
            }

            Colouring.Push(ConsoleColor.Green);
            if (!LoadItemsAndNanos())
            {
                Colouring.Push(ConsoleColor.Red);
                Console.WriteLine(locales.ErrorLoadingItemsNanos);
                Colouring.Pop();
                Colouring.Pop();
                return false;
            }

            Colouring.Pop();

            Colouring.Push(ConsoleColor.Green);
            if (!LoadTradeSkills())
            {
                Colouring.Push(ConsoleColor.Red);
                Console.WriteLine("No locale yet: Error reading trade skills");
                Colouring.Pop();
                Colouring.Pop();
                return false;
            }

            Colouring.Pop();

            if (!InitializeConsoleCommands())
            {
                return false;
            }

            Colouring.Pop();

            return true;
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        private static bool InitializeConsoleCommands()
        {
            consoleCommands.Engine = "Zone";

            consoleCommands.AddEntry("start", StartServer);
            consoleCommands.AddEntry("startm", StartServerMultipleScriptDlls);
            consoleCommands.AddEntry("running", IsServerRunning);
            consoleCommands.AddEntry("ping", PingChatServer);

            consoleCommands.AddEntry("stop", StopServer);

            consoleCommands.AddEntry("exit", ShutDownServer);
            consoleCommands.AddEntry("quit", ShutDownServer);

            consoleCommands.AddEntry("check", CheckDatabase);
            consoleCommands.AddEntry("updatedb", CheckDatabase);

            consoleCommands.AddEntry("online", ShowOnlineCharacters);
            consoleCommands.AddEntry("pf", ShowPlayfield);
            consoleCommands.AddEntry("quests", CheckQuests);
            consoleCommands.AddEntry("ls", ListAvailableScripts);

            consoleCommands.AddEntry("debug", SetDebug);

            return true;
        }

        private static void SetDebug(string[] obj)
        {
            if (obj.Length == 1)
            {
                LogUtil.Toggle("");
            }
            else
            {
                for (int i = 1; i < obj.Length; i++)
                {
                    LogUtil.Toggle(obj[i]);
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        private static bool InitializeGameFunctions()
        {
            try
            {
                Colouring.Push(ConsoleColor.Green);
                Console.WriteLine(
                    "{0} Game functions loaded",
                    FunctionCollection.Instance.NumberofRegisteredFunctions());
            }
            catch (Exception e)
            {
                LogUtil.ErrorException(e);
                Colouring.Pop();
                return false;
            }

            Colouring.Pop();
            return true;
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        private static bool InitializeLogAndBug()
        {
            try
            {
                // Setup and enable NLog logging to file
                LogUtil.SetupConsoleLogging(LogLevel.Debug);
                LogUtil.SetupFileLogging("${basedir}/ZoneEngineLog.txt", LogLevel.Trace);


                // Nothing else catches exceptions that escape a thread, so route them
                // into the normal log. Handler exceptions are caught closer to the
                // source in MessagePublisher; these are the ones that got past it.
                AppDomain.CurrentDomain.UnhandledException += (s, ea) =>
                    LogUtil.ErrorException(
                        ea.ExceptionObject as Exception,
                        "Unhandled exception (terminating: {0})",
                        ea.IsTerminating);
                TaskScheduler.UnobservedTaskException += (s, ea) =>
                    {
                        LogUtil.ErrorException(ea.Exception, "Unobserved task exception");
                        ea.SetObserved();
                    };
            }
            catch (Exception e)
            {
                LogUtil.ErrorException(e);

                Colouring.Push(ConsoleColor.Red);
                Console.WriteLine(locales.ErrorInitializingNLogNBug);
                Console.WriteLine(e.Message);
                Colouring.Pop();
                return false;
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        private static bool InizializeTCPIP()
        {
            int Port = Convert.ToInt32(ConfigReadWrite.Instance.CurrentConfig.ZonePort);
            try
            {
                if (ConfigReadWrite.Instance.CurrentConfig.ListenIP == "0.0.0.0")
                {
                    zoneServer.TcpEndPoint = new IPEndPoint(IPAddress.Any, Port);
                }
                else
                {
                    zoneServer.TcpEndPoint = new IPEndPoint(IPAddress.Parse(ConfigReadWrite.Instance.CurrentConfig.ListenIP), Port);
                }

                zoneServer.MaximumPendingConnections = 100;
            }
            catch (Exception e)
            {
                LogUtil.ErrorException(e);

                Colouring.Push(ConsoleColor.Red);
                Console.WriteLine(locales.ErrorIPAddressParseFailed);
                Console.Write(e.Message);
                Colouring.Pop();
                Console.ReadKey();

                return false;
            }

            return true;
        }

        /// <summary>
        /// </summary>
        /// <param name="parts">
        /// </param>
        private static void IsServerRunning(string[] parts)
        {
            Colouring.Push(ConsoleColor.White);
            if (zoneServer.IsRunning)
            {
                Console.WriteLine(locales.ServerConsoleServerIsRunning);
            }
            else
            {
                Console.WriteLine(locales.ServerConsoleServerIsNotRunning);
            }

            Colouring.Pop();
        }

        /// <summary>
        /// </summary>
        /// <param name="parts">
        /// </param>
        private static void ListAvailableScripts(string[] parts)
        {
            // list all available scripts, dont remove it since it does what it should
            Colouring.Push(ConsoleColor.White);
            Console.WriteLine(locales.ServerConsoleAvailableScripts + ":");

            string[] files = Directory.GetFiles(
                "Scripts" + Path.DirectorySeparatorChar,
                "*.cs",
                SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                Console.WriteLine(locales.ServerConsoleNoScriptsFound);
                return;
            }

            Colouring.Push(ConsoleColor.Green);
            foreach (string s in files)
            {
                Console.WriteLine(s);
            }

            Colouring.Pop();
        }

        /// <summary>
        /// Load items and Nanos into static lists
        /// </summary>
        /// <returns>
        /// true if ok
        /// </returns>
        private static bool LoadItemsAndNanos()
        {
            Colouring.Push(ConsoleColor.Green);
            try
            {

                ItemLoader.CacheAllItems();
            }
            catch (Exception e)
            {
                LogUtil.ErrorException(e);

                Colouring.Pop();
                Colouring.Push(ConsoleColor.Red);
                Console.WriteLine(locales.ErrorReadingItemsFile);
                Console.WriteLine(e.Message);
                Colouring.Pop();
                return false;
            }

            Colouring.Pop();

            Colouring.Push(ConsoleColor.Green);
            try
            {
                NanoLoader.CacheAllNanos();
                Console.WriteLine();
            }
            catch (Exception e)
            {
                LogUtil.ErrorException(e);

                Colouring.Pop();
                Colouring.Push(ConsoleColor.Red);
                Console.WriteLine(locales.ErrorReadingNanosFile);
                Console.WriteLine(e.Message);
                Colouring.Pop();
                return false;
            }

            Colouring.Pop();

            Colouring.Push(ConsoleColor.Green);
            try
            {
                Console.WriteLine("Loaded {0} Playfields", PlayfieldLoader.CacheAllPlayfieldData());
                Console.WriteLine("Loaded {0} Quests", QuestManager.Load());
                Console.WriteLine("Loaded {0} Levels", Leveling.Load());
                Console.WriteLine("Loaded {0} Creature Experience Levels", Experience.Load());
                Console.WriteLine("Loaded {0} Fixture Behaviours", FixtureBehaviours.Load());
                Console.WriteLine("Loaded {0} Combat Weapon Descriptors", CombatWeaponProfiles.Load());
                Console.WriteLine("Loaded {0} SpawnItem Keys", ItemSpawns.Load());
                Console.WriteLine();
            }
            catch (Exception e)
            {
                LogUtil.ErrorException(e);

                Colouring.Pop();
                Colouring.Push(ConsoleColor.Red);
                Console.WriteLine("Error reading playfield content pack");
                Console.WriteLine(e.Message);
                Colouring.Pop();
                return false;
            }

            Colouring.Pop();

            return true;
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// </returns>
        private static bool LoadTradeSkills()
        {
            try
            {
                int temp = TradeSkill.Instance.ItemNames.Count;
            }
            catch (Exception e)
            {
                LogUtil.ErrorException(e);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="args">
        /// Command line parameters
        /// </param>
        private static void Main(string[] args)
        {
            Console.CancelKeyPress += ConsoleCancelKeyPress;

            OnScreenBanner.PrintBanner(ConsoleColor.Green);

            Console.WriteLine();
            Console.WriteLine(locales.ServerConsoleMainText, DateTime.Now.Year);

            if (!Initialize())
            {
                Console.WriteLine(locales.ErrorInitializingEngine);
                Console.WriteLine("Press enter to exit");
                Console.ReadLine();
            }
            else
            {
                // Start by itself, once Initialize has finished and the data is
                // loaded. It used to sit at the console waiting for "start" to
                // be typed, which meant three windows and three commands before
                // anything listened.
                //
                // This runs the same StartServer the console command runs,
                // rather than StartTheServer underneath it. The two are not
                // equivalent: StartServer compiles scripts differently and
                // refuses if the server is already up.
                //
                // Pass -noautostart to get the old behaviour.
                if (args == null
                    || !args.Any(a => string.Equals(a, "-noautostart", StringComparison.OrdinalIgnoreCase)))
                {
                    StartServer(new string[0]);
                }
                CommandLoop(args);
            }

            // NLog<->Mono lockup fix
            LogManager.Configuration = null;
        }

        /// <summary>
        /// </summary>
        /// <param name="parts">
        /// </param>
        private static void PingChatServer(string[] parts)
        {
            // ChatCom.Server.Ping();
            Console.WriteLine("Ping is disabled till we can do it");
        }

        /// <summary>
        /// </summary>
        private static void ShowCommandHelp()
        {
            Colouring.Push(ConsoleColor.White);
            Console.WriteLine(locales.ServerConsoleAvailableCommands);
            Console.WriteLine("---------------------------");
            Console.WriteLine(consoleCommands.HelpAll());
            Console.WriteLine("---------------------------");
            Console.WriteLine();
            Colouring.Pop();
        }

        /// <summary>
        /// </summary>
        /// <param name="parts">
        /// </param>
        private static void ShowOnlineCharacters(string[] parts)
        {
            if (zoneServer.IsRunning)
            {
                Colouring.Push(ConsoleColor.White);

                // TODO: Check all clients inside playfields
                lock (zoneServer.Clients)
                {
                    foreach (ZoneClient c in zoneServer.Clients)
                    {
                        Console.WriteLine(
                            "Character " + c.Controller.Character.Name + " online in PF "
                            + c.Controller.Character.Playfield.Identity.Instance);
                    }
                }

                Colouring.Pop();
            }
        }


        /// <summary>
        /// Load a playfield and say what came up in it.
        /// </summary>
        /// <remarks>
        /// A playfield is built the first time somebody walks into it, so until
        /// a client zones in there is nothing to look at and no way to tell a
        /// populated playfield from an empty one. This asks for it by number
        /// and prints the roll call.
        ///
        /// It is an operator command, not a game one - it leaves the playfield
        /// loaded afterwards, exactly as a client arriving would have.
        /// </remarks>
        /// <param name="parts">
        /// </param>
        private static void ShowPlayfield(string[] parts)
        {
            if (!zoneServer.IsRunning)
            {
                Console.WriteLine(locales.ServerConsoleServerIsNotRunning);
                return;
            }

            int playfieldId;
            if ((parts.Length < 2) || !int.TryParse(parts[1], out playfieldId))
            {
                Console.WriteLine("pf <playfield id> - what is standing in that playfield.");
                return;
            }

            if (!PlayfieldLoader.PFData.ContainsKey(playfieldId))
            {
                Colouring.Push(ConsoleColor.Red);
                Console.WriteLine("No playfield {0} in playfields.ocp.", playfieldId);
                Colouring.Pop();
                return;
            }

            var identity = new Identity { Type = IdentityType.Playfield, Instance = playfieldId };
            IPlayfield playfield;
            try
            {
                playfield = zoneServer.PlayfieldById(identity);
            }
            catch (Exception e)
            {
                Colouring.Push(ConsoleColor.Red);
                Console.WriteLine("Playfield {0} failed to load: {1}", playfieldId, e.Message);
                Colouring.Pop();
                return;
            }

            ICharacter[] characters = Pool.Instance.GetAll<ICharacter>(playfield.Identity).ToArray();
            Vendor[] vendors = Pool.Instance.GetAll<Vendor>(playfield.Identity).ToArray();
            StaticDynel[] fixtures = Pool.Instance.GetAll<StaticDynel>(playfield.Identity).ToArray();

            int givers = characters.Count(
                x =>
                    {
                        var npc = x.Controller as NPCController;
                        return npc != null && npc.KnuBot != null;
                    });

            Colouring.Push(ConsoleColor.White);
            Console.WriteLine(
                "{0} ({1}): {2} characters, {3} of them will talk; {4} vendors, {5} fixtures, {6} statels",
                PlayfieldLoader.PFData[playfieldId].Name.Trim(),
                playfieldId,
                characters.Length,
                givers,
                vendors.Length,
                fixtures.Length,
                PlayfieldLoader.PFData[playfieldId].Statels.Count);
            Colouring.Pop();

            foreach (Vendor vendor in vendors.OrderBy(v => v.Identity.Instance))
            {
                int stock = vendor.BaseInventory.Pages[vendor.BaseInventory.StandardPage].List().Count;
                Console.WriteLine(
                    "  shop {0,-34} {1,3} items{2}",
                    Cut(vendor.Name, 34),
                    stock,
                    stock == 0 ? "   (no stock in the database)" : string.Empty);
            }

            foreach (var byTemplate in fixtures.GroupBy(x => x.Template.ID).OrderBy(g => g.Key))
            {
                DBItemName named = ItemNamesDao.Instance.Get(byTemplate.Key);
                Console.WriteLine(
                    "  fixture {0,-31} x{1,-3} at {2:0},{3:0},{4:0}",
                    Cut(named == null ? "template " + byTemplate.Key : named.Name, 31),
                    byTemplate.Count(),
                    byTemplate.First().Coordinate.x,
                    byTemplate.First().Coordinate.y,
                    byTemplate.First().Coordinate.z);
            }

            foreach (var byName in characters.GroupBy(x => x.Name).OrderByDescending(g => g.Count()).ThenBy(g => g.Key))
            {
                ICharacter first = byName.First();
                Console.WriteLine(
                    "  {0,-34} x{1,-3} level {2,-3} at {3:0},{4:0},{5:0}",
                    Cut(byName.Key, 34),
                    byName.Count(),
                    first.Stats[StatIds.level].Value,
                    first.Coordinates().x,
                    first.Coordinates().y,
                    first.Coordinates().z);
            }
        }

        /// <summary>
        /// Whether every quest of a playfield can actually be walked.
        /// </summary>
        /// <remarks>
        /// A quest needs three things to work and each of them can be missing
        /// on its own: somebody standing there who offers it, an objective that
        /// names something you can reach, and the same somebody willing to take
        /// it back when it is done. Reading the tables says nothing about the
        /// last two, because whether a character can be spoken to is decided
        /// when the playfield is built and whether a fixture exists is decided
        /// by what the captures placed.
        ///
        /// So this builds the playfield and asks it. Every line it prints is a
        /// question answered against live objects, not against a row.
        /// </remarks>
        /// <param name="parts">
        /// </param>
        private static void CheckQuests(string[] parts)
        {
            if (!zoneServer.IsRunning)
            {
                Console.WriteLine(locales.ServerConsoleServerIsNotRunning);
                return;
            }

            int playfieldId;
            if ((parts.Length < 2) || !int.TryParse(parts[1], out playfieldId)
                || !PlayfieldLoader.PFData.ContainsKey(playfieldId))
            {
                Console.WriteLine("quests <playfield id> - whether every quest there can be finished.");
                return;
            }

            IPlayfield playfield = zoneServer.PlayfieldById(
                new Identity { Type = IdentityType.Playfield, Instance = playfieldId });

            ICharacter[] characters = Pool.Instance.GetAll<ICharacter>(playfield.Identity).ToArray();
            StaticDynel[] fixtures = Pool.Instance.GetAll<StaticDynel>(playfield.Identity).ToArray();

            var talkable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var present = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var byId = new Dictionary<int, ICharacter>();
            foreach (ICharacter character in characters)
            {
                present.Add(character.Name);
                byId[character.Identity.Instance] = character;
                var npc = character.Controller as NPCController;
                if (npc != null && npc.KnuBot != null)
                {
                    talkable.Add(character.Name);
                }
            }

            var standing = new HashSet<int>();
            foreach (StaticDynel fixture in fixtures)
            {
                standing.Add(fixture.Identity.Instance);
            }

            int walkable = 0;
            var broken = new List<string>();

            // A staged quest is handed out by a conversation, or by the stage before it finishing.
            List<DBKnuBotDialogue> dialogue = KnuBotDialogueDao.Instance.GetWhere(new { Playfield = playfieldId }).ToList();
            var follows = new HashSet<int>(
                QuestManager.All().SelectMany(q => QuestManager.TransitionsOf(q.Id)).Select(t => t.ToQuest));

            foreach (DBQuest quest in QuestManager.All().Where(q => q.Playfield == playfieldId).OrderBy(q => q.Id))
            {
                var wrong = new List<string>();

                ICharacter giver;
                DBKnuBotDialogue handout = dialogue.FirstOrDefault(l => l.Kind == 2 && l.ActionValue == quest.Id);
                if (handout != null && !talkable.Contains(handout.NpcName))
                {
                    wrong.Add(handout.NpcName + " hands it out but cannot be spoken to");
                }
                else if (handout != null || follows.Contains(quest.Id))
                {
                    // Handed out; nothing more to check about who gives it.
                }
                else if (!byId.TryGetValue(quest.GiverId, out giver))
                {
                    wrong.Add("nobody gives it");
                }
                else
                {
                    var npc = giver.Controller as NPCController;
                    if (npc == null || npc.KnuBot == null)
                    {
                        wrong.Add(giver.Name + " gives it but cannot be spoken to");
                    }
                    else if (npc.KnuBot is ScriptedKnuBot)
                    {
                        // A conversation with no handover step can be walked
                        // through and never gives anything out.
                        if (!KnuBotScriptDao.Instance
                            .GetWhere(new { Npc = quest.GiverId, Playfield = playfieldId })
                            .Any(l => l.Grants != 0))
                        {
                            wrong.Add(giver.Name + " talks, but nothing in it hands a quest over");
                        }
                    }
                    else
                    {
                        wrong.Add(giver.Name + " has no captured conversation");
                    }
                }

                IList<DBQuestObjective> objectives = QuestManager.ObjectivesOf(quest.Id);
                if (objectives.Count == 0)
                {
                    wrong.Add("no objective");
                }

                foreach (DBQuestObjective objective in objectives)
                {
                    string validationError = QuestStateRules.ObjectiveValidationError(objective);
                    if (validationError != null)
                    {
                        wrong.Add("objective " + objective.Ordinal + " " + validationError);
                        continue;
                    }

                    switch ((QuestObjectiveType)objective.ObjectiveType)
                    {
                        case QuestObjectiveType.TalkTo:
                            if (!talkable.Contains(objective.Target))
                            {
                                wrong.Add(
                                    present.Contains(objective.Target)
                                        ? objective.Target + " is here but cannot be spoken to"
                                        : objective.Target + " is not here");
                            }

                            break;

                        case QuestObjectiveType.Kill:
                            if (!QuestStateRules.TargetNames(objective.Target).Any(present.Contains))
                            {
                                wrong.Add("nothing called " + objective.Target + " is here to kill");
                            }

                            break;

                        case QuestObjectiveType.Use:
                            if (!FixtureExists(objective.Target, playfieldId, standing, fixtures))
                            {
                                wrong.Add("fixture " + objective.Target + " is not here");
                            }

                            break;

                        case QuestObjectiveType.UseItemOn:
                            int usedOn;
                            bool targetExists;
                            if (int.TryParse(objective.Target, out usedOn))
                            {
                                targetExists = FixtureExists(objective.Target, playfieldId, standing, fixtures)
                                               || byId.ContainsKey(usedOn);
                            }
                            else
                            {
                                targetExists = present.Contains(objective.Target)
                                               || fixtures.Any(
                                                   f => string.Equals(
                                                       TradeSkill.Instance.GetItemName(
                                                           f.Template.ID,
                                                           f.Template.ID,
                                                           f.Template.Quality),
                                                       objective.Target,
                                                       StringComparison.OrdinalIgnoreCase));
                            }

                            if (!targetExists)
                            {
                                wrong.Add("nothing called " + objective.Target + " is here to use an item on");
                            }

                            break;

                        case QuestObjectiveType.Reach:
                            float ignoredX;
                            string[] spot = objective.Target.Split(',');
                            if (spot.Length != 2
                                || !float.TryParse(
                                        spot[0],
                                        NumberStyles.Float,
                                        CultureInfo.InvariantCulture,
                                        out ignoredX))
                            {
                                wrong.Add("its place, " + objective.Target + ", is not a position");
                            }

                            break;

                        case QuestObjectiveType.Collect:
                        case QuestObjectiveType.Purchase:
                        case QuestObjectiveType.TradeSkill:
                        case QuestObjectiveType.Equip:
                            // Somewhere in the playfield has to sell the parts,
                            // or the recipe cannot be started. Checked as "a
                            // shop here has something of that name, or a recipe
                            // makes it", because an implant is bought in pieces
                            // and never sold whole.
                            if (!ItemExists(objective.Target))
                            {
                                wrong.Add("nothing called " + objective.Target + " exists to be made");
                            }

                            break;

                        case QuestObjectiveType.HandIn:
                            // Two ways this goes wrong, and they are different
                            // mistakes. The item may not exist, same as for
                            // Collect - or it may exist and the character may
                            // never ask for it, in which case the box the
                            // player would put it in is never opened and the
                            // quest sits in the log forever.
                            if (!ItemExists(objective.Target) && !TradeSkill.Instance.ItemNames.ContainsKey(objective.TargetLowId))
                            {
                                wrong.Add("nothing called " + objective.Target + " exists to hand over");
                            }
                            else if (dialogue.Any(l => l.Kind == 3))
                            {
                                // A conversation's trade opens for whichever stage wants an item.
                            }
                            else
                            {
                                DBKnuBotScript handover = KnuBotScriptDao.Instance.GetWhere(
                                    new
                                        {
                                            Grants = quest.Id,
                                            Action = (int)ScriptAction.OpenQuestTrade,
                                            Playfield = playfieldId
                                        }).FirstOrDefault();
                                ICharacter recipient;
                                if (handover == null)
                                {
                                    wrong.Add("no conversation opens a trade for the " + objective.Target);
                                }
                                else if (!byId.TryGetValue(handover.Npc, out recipient)
                                         || !talkable.Contains(recipient.Name))
                                {
                                    wrong.Add("the recipient for " + objective.Target + " cannot be spoken to");
                                }
                            }

                            break;

                        case QuestObjectiveType.UseItem:
                            if (!ItemExists(objective.Target))
                            {
                                wrong.Add("nothing called " + objective.Target + " exists to use");
                            }

                            break;

                        case QuestObjectiveType.UseItemOnCharacter:
                            if (!present.Contains(objective.Target))
                            {
                                wrong.Add("nobody called " + objective.Target + " is here to use an item on");
                            }

                            break;

                        case QuestObjectiveType.DialogueAnswer:
                            if (!dialogue.Any(l => l.Kind == 1 && string.Equals(l.Text, objective.Target, StringComparison.Ordinal)))
                            {
                                wrong.Add("no conversation offers \"" + objective.Target + "\"");
                            }

                            break;

                        default:
                            wrong.Add("objective kind " + objective.ObjectiveType + " has no way to finish");
                            break;
                    }
                }

                if (wrong.Count == 0)
                {
                    walkable++;
                }
                else
                {
                    broken.Add(
                        string.Format("  {0,-32} {1}", Cut(quest.Name, 32), string.Join("; ", wrong.ToArray())));
                }
            }

            int total = QuestManager.All().Count(q => q.Playfield == playfieldId);
            Colouring.Push(walkable == total ? ConsoleColor.Green : ConsoleColor.White);
            Console.WriteLine(
                "{0} ({1}): {2} of {3} quests can be started, finished and handed in.",
                PlayfieldLoader.PFData[playfieldId].Name.Trim(),
                playfieldId,
                walkable,
                total);
            Colouring.Pop();

            foreach (string line in broken)
            {
                Console.WriteLine(line);
            }
        }

        /// <summary>
        /// A fixture objective's target is here: a spawned fixture's instance or template, or a fixture
        /// of the playfield's own data, which the client has without being sent it.
        /// </summary>
        private static bool FixtureExists(string target, int playfieldId, HashSet<int> standing, IEnumerable<StaticDynel> fixtures)
        {
            int id;
            if (!int.TryParse(target, NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
            {
                return false;
            }

            return standing.Contains(id)
                   || fixtures.Any(f => f.Template.ID == id)
                   || PlayfieldLoader.PFData[playfieldId].Statels.Any(s => s.Identity.Instance == id);
        }

        /// <summary>
        /// An item named by an objective exists, by id or by name.
        /// </summary>
        private static bool ItemExists(string target)
        {
            int id;
            return int.TryParse(target, NumberStyles.Integer, CultureInfo.InvariantCulture, out id)
                       ? TradeSkill.Instance.ItemNames.ContainsKey(id)
                       : TradeSkill.Instance.ItemNames.Values.Any(n => string.Equals(n, target, StringComparison.OrdinalIgnoreCase));
        }

        private static string Cut(string text, int width)
        {
            return text.Length > width ? text.Substring(0, width) : text;
        }

        /// <summary>
        /// </summary>
        /// <param name="parts">
        /// </param>
        private static void ShutDownServer(string[] parts)
        {
            if (zoneServer.IsRunning)
            {
                zoneServer.Stop();
            }

            ISComClient.ShutDown();
            exited = true;
        }

        /// <summary>
        /// </summary>
        /// <param name="parts">
        /// </param>
        private static void StartServer(string[] parts)
        {
            if (zoneServer.IsRunning)
            {
                Colouring.Push(ConsoleColor.Red);
                Console.WriteLine(locales.ServerConsoleServerIsRunning);
                Colouring.Pop();
            }
            else
            {
                // TODO: Add Sql Check.
                StartTheServer();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="parts">
        /// </param>
        private static void StartServerMultipleScriptDlls(string[] parts)
        {
            // Multiple dll compile
            if (zoneServer.IsRunning)
            {
                Colouring.Push(ConsoleColor.Red);
                Console.WriteLine(locales.ServerConsoleServerIsRunning);
                Colouring.Pop();
            }
            else
            {
                // TODO: Add Sql Check.
                StartTheServer();
            }
        }

        /// <summary>
        /// </summary>
        private static void StartTheServer()
        {
            // TODO: Read playfield data, check which playfields have to be created, and create them
            // TODO: Cache neccessary Spawns and Mobs
            // TODO: Cache neccessary Doors 
            // TODO: Cache neccessary statels
            // TODO: Cache Vendors

            // Console.WriteLine(Core.Playfields.Playfields.Instance.playfields[0].name);

            // Starting anyway left a zone without KnuBots or scripts and only a line in the console to say so.
            if (!ScriptCompiler.Instance.Compile(true))
            {
                Colouring.Push(ConsoleColor.Red);
                Console.WriteLine("The scripts did not compile, so the zone server was not started. Fix the errors above and type start.");
                Colouring.Pop();
                return;
            }

            Console.WriteLine(ScriptCompiler.Instance.AddScriptMembers() + " chat commands loaded");
            zoneServer.Start(true, false);
        }

        /// <summary>
        /// </summary>
        /// <param name="parts">
        /// </param>
        private static void StopServer(string[] parts)
        {
            if (!zoneServer.IsRunning)
            {
                Colouring.Push(ConsoleColor.Red);
                Console.WriteLine(locales.ServerConsoleServerIsNotRunning);
                Colouring.Pop();
            }
            else
            {
                zoneServer.Stop();
            }
        }

        #endregion
    }
}
