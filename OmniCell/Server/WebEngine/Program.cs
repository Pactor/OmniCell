namespace WebEngine
{
    using System;
    using System.Net;

    /// <summary>
    /// Serves the panels the Anarchy Online client's embedded browser asks for.
    /// </summary>
    /// <remarks>
    /// The client navigates to URLs compiled into GUI.dll. OmniCell-Launcher
    /// rewrites those strings in the client's memory at launch so they point at
    /// this server. Nothing on disk is modified and no elevation is needed at
    /// either end.
    ///
    /// Port 80 is the default because the patched URLs must fit inside the
    /// original strings, and adding ":port" costs characters that the shortest
    /// of them cannot spare. See OmniCell-Launcher's WebUrlPatch for the budget.
    /// </remarks>
    public static class Program
    {
        private static int Main(string[] args)
        {
            int port = 80;
            IPAddress bind = IPAddress.Any;

            foreach (string arg in args)
            {
                if (arg.StartsWith("port=", StringComparison.OrdinalIgnoreCase))
                {
                    if (!int.TryParse(arg.Substring(5), out port))
                    {
                        Console.WriteLine("Bad port: " + arg);
                        return 1;
                    }
                }
                else if (arg.StartsWith("bind=", StringComparison.OrdinalIgnoreCase))
                {
                    if (!IPAddress.TryParse(arg.Substring(5), out bind))
                    {
                        Console.WriteLine("Bad bind address: " + arg);
                        return 1;
                    }
                }
                else if (arg.StartsWith("adminlevel=", StringComparison.OrdinalIgnoreCase))
                {
                    int level;
                    if (!int.TryParse(arg.Substring(11), out level) || level < 1)
                    {
                        Console.WriteLine("Bad admin level (a GM level of 1 or more): " + arg);
                        return 1;
                    }

                    AdminAuth.RequiredGmLevel = level;
                }
                else if (arg == "/?" || arg == "-h" || arg == "--help")
                {
                    Usage();
                    return 0;
                }
            }

            HttpServer server = new HttpServer(bind, port);
            server.RequestHandled += (remote, method, target) =>
                Log("REQ", remote + "  " + method + " " + target);
            server.Failed += reason => Log("ERR", reason);

            try
            {
                server.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("  Could not listen on " + bind + ":" + port);
                Console.WriteLine("  " + ex.Message);
                Console.WriteLine();
                if (port < 1024)
                {
                    Console.WriteLine("  Another program may already hold this port. IIS and");
                    Console.WriteLine("  Skype are the usual culprits on 80. Try port=8080 and");
                    Console.WriteLine("  set the launcher to match.");
                    Console.WriteLine();
                }

                return 1;
            }

            Banner(bind, port);

            // Reading every icon takes a few seconds; start now so the page is usually ready
            // by the time anyone opens it.
            IconCatalog.StartLoading();

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                Log("---", "Shutting down");
                server.Stop();
                Environment.Exit(0);
            };

            while (true)
            {
                string line = Console.ReadLine();
                if (line == null)
                {
                    // Redirected or closed stdin. Keep serving.
                    System.Threading.Thread.Sleep(int.MaxValue);
                    continue;
                }

                if (line.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase)
                    || line.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }

            server.Stop();
            return 0;
        }

        private static void Banner(IPAddress bind, int port)
        {
            Console.WriteLine();
            Console.WriteLine("  OmniCell WebEngine");
            Console.WriteLine("  Listening on " + bind + ":" + port);
            Console.WriteLine();
            Console.WriteLine("  Serving:");
            Console.WriteLine("    " + Endpoints.Shop + "          shop panel");
            Console.WriteLine("    " + Endpoints.Market + "        market panel");
            Console.WriteLine("    " + Endpoints.Petition + "             petition panel");
            Console.WriteLine("    " + Endpoints.DailyRewards + "         daily rewards panel");
            Console.WriteLine("    " + Endpoints.ShowItem + "      item bundle details");
            Console.WriteLine("    /org/stats/d/<dim>/name/<org id>/basicstats.xml   org roster JSON for chat bots");
            Console.WriteLine("    /character/bio/d/<dim>/name/<name>/bio.xml        character JSON for chat bots");
            Console.WriteLine("    /history /timers/bosses /timers/gaubuffs /gmi/aoid/<id> /towers/sites");
            Console.WriteLine("                                                      empty bot lookups (not tracked)");
            Console.WriteLine("    " + IconPages.Page + "                                            icon browser (client from AO_CLIENT in paths.cfg)");
            Console.WriteLine("    " + IconPages.ImagePrefix + "<icon id>.png                            one icon image");
            Console.WriteLine("    " + AdminAuth.LoginPath + "                                      admin sign-in for the two above");
            Console.WriteLine();
            Console.WriteLine("  Admin pages need a game account with GM level " + AdminAuth.RequiredGmLevel
                              + " or higher (adminlevel=N to change), checked against MySQL.");
            if (!IPAddress.IsLoopback(bind))
            {
                Console.WriteLine("  WARNING: admin sign-in is plain HTTP, so passwords cross the network unencrypted.");
                Console.WriteLine("           Use bind=127.0.0.1 for admin work until WebEngine serves HTTPS.");
            }
            Console.WriteLine();
            Console.WriteLine("  Every request is logged below. A path shown as 'Not mapped'");
            Console.WriteLine("  is one the client wants that Endpoints.Mappings has missed.");
            Console.WriteLine();
            Console.WriteLine("  Type quit to stop.");
            Console.WriteLine();
        }

        private static void Usage()
        {
            Console.WriteLine("WebEngine [port=80] [bind=0.0.0.0] [adminlevel=1]");
        }

        private static void Log(string tag, string message)
        {
            Console.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "  " + tag + "  " + message);
        }
    }
}
