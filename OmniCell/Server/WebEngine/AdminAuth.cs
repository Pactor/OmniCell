namespace WebEngine
{
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;

    using OmniCell.Database.Dao;

    /// <summary>
    /// Sign-in for the admin pages, using the game's own accounts: the login table's username,
    /// its password hash, and its GM level.
    /// </summary>
    /// <remarks>
    /// A signed-in admin gets a random session token in an HttpOnly, SameSite=Strict cookie, kept
    /// only in memory, so restarting WebEngine signs everyone out. The password is checked once at
    /// sign-in rather than on every request: the stored hash costs ~100 ms by design, and the icon
    /// browser alone makes thousands of requests.
    ///
    /// WebEngine speaks plain HTTP, so the password crosses the network unencrypted. Until HTTPS is
    /// added, bind to 127.0.0.1 (or a trusted network) when these pages are in use.
    /// </remarks>
    public static class AdminAuth
    {
        public const string LoginPath = "/admin/login";

        public const string LogoutPath = "/admin/logout";

        public const string SessionPath = "/admin/session";

        private const string CookieName = "omnicell_admin";

        private static readonly TimeSpan IdleTimeout = TimeSpan.FromHours(2);

        private static readonly TimeSpan MaxSessionAge = TimeSpan.FromHours(12);

        private const int MaxFailures = 5;

        private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(10);

        private static readonly ConcurrentDictionary<string, Session> Sessions =
            new ConcurrentDictionary<string, Session>(StringComparer.Ordinal);

        private static readonly ConcurrentDictionary<string, Failures> FailuresByAddress =
            new ConcurrentDictionary<string, Failures>(StringComparer.Ordinal);

        // Checked against when the username does not exist, so an unknown name takes as long
        // as a wrong password and the timing does not reveal which accounts exist.
        private static readonly string DummyHash = "25000:" + Convert.ToBase64String(new byte[30]) + ":" + Convert.ToBase64String(new byte[30]);

        static AdminAuth()
        {
            RequiredGmLevel = 1;
        }

        /// <summary>The lowest login.GM that may sign in. Every in-game GM command needs 1.</summary>
        public static int RequiredGmLevel { get; set; }

        public sealed class Session
        {
            public string Username;

            public int GmLevel;

            public DateTime Created;

            public DateTime LastSeen;
        }

        private sealed class Failures
        {
            public int Count;

            public DateTime WindowStart;
        }

        /// <summary>The signed-in admin for this request, or null.</summary>
        public static Session Current(HttpRequest request)
        {
            string token = request.Cookie(CookieName);
            Session session;
            if (string.IsNullOrEmpty(token) || !Sessions.TryGetValue(token, out session))
            {
                return null;
            }

            DateTime now = DateTime.UtcNow;
            if (now - session.LastSeen > IdleTimeout || now - session.Created > MaxSessionAge)
            {
                Sessions.TryRemove(token, out session);
                return null;
            }

            session.LastSeen = now;
            return session;
        }

        /// <summary>Handles /admin/login, /admin/logout and /admin/session; null for anything else.</summary>
        public static PageResult Route(HttpRequest request)
        {
            string path = "/" + request.Path.Trim('/').ToLowerInvariant();
            if (path == LoginPath)
            {
                if (request.Method == "POST")
                {
                    return SignIn(request);
                }

                Session existing = Current(request);
                string returnTo = SafeReturn(request.QueryValue("return"));
                return existing != null ? PageResult.Redirect(returnTo) : new PageResult(200, LoginPage(returnTo, null, string.Empty));
            }

            if (path == LogoutPath)
            {
                if (request.Method != "POST")
                {
                    return new PageResult(405, "Sign out with the Sign out button.", "text/plain; charset=utf-8");
                }

                string token = request.Cookie(CookieName);
                Session removed;
                if (!string.IsNullOrEmpty(token))
                {
                    Sessions.TryRemove(token, out removed);
                }

                return PageResult.Redirect(LoginPath)
                                 .WithHeader("Set-Cookie", CookieName + "=; Path=/; Max-Age=0; HttpOnly; SameSite=Strict");
            }

            if (path == SessionPath)
            {
                Session session = Current(request);
                if (session == null)
                {
                    return Unauthorized();
                }

                return new PageResult(
                    200,
                    "{\"username\":\"" + JsonEscape(session.Username) + "\",\"gmLevel\":" + session.GmLevel + "}",
                    "application/json; charset=utf-8");
            }

            return null;
        }

        /// <summary>
        /// For a page: send the browser to sign in and come back. For data or images: 401.
        /// </summary>
        public static PageResult Challenge(HttpRequest request, bool isPage)
        {
            return isPage
                       ? PageResult.Redirect(LoginPath + "?return=" + WebUtility.UrlEncode(request.Target))
                       : Unauthorized();
        }

        private static PageResult Unauthorized()
        {
            return new PageResult(401, "{\"error\":\"sign in required\",\"login\":\"" + LoginPath + "\"}", "application/json; charset=utf-8");
        }

        private static PageResult SignIn(HttpRequest request)
        {
            string username = request.FormValue("username").Trim();
            string password = request.FormValue("password");
            string returnTo = SafeReturn(request.FormValue("return"));
            string address = request.RemoteAddress.ToString();

            if (IsLockedOut(address))
            {
                return new PageResult(
                    429,
                    LoginPage(returnTo, "Too many failed sign-ins from this address. Try again in a few minutes.", username));
            }

            if (username.Length == 0 || password.Length == 0)
            {
                return new PageResult(200, LoginPage(returnTo, "Enter your account name and password.", username));
            }

            OmniCell.Database.Dao.DBLoginData account;
            try
            {
                account = LoginDataDao.Instance.GetByUsername(username);
            }
            catch (Exception ex)
            {
                return new PageResult(
                    500,
                    LoginPage(returnTo, "The account database could not be reached (" + ex.Message + "). Is MySQL running and set in Config.xml?", username));
            }

            bool passwordOk = ValidatePassword(password, account != null ? account.Password : DummyHash) && account != null;
            if (!passwordOk)
            {
                RecordFailure(address);
                return new PageResult(200, LoginPage(returnTo, "That account name and password do not match.", username));
            }

            if (account.GM < RequiredGmLevel)
            {
                RecordFailure(address);
                return new PageResult(
                    403,
                    LoginPage(returnTo, "This account is not a game admin (GM level " + RequiredGmLevel + " or higher is required).", username));
            }

            Failures cleared;
            FailuresByAddress.TryRemove(address, out cleared);

            string token = NewToken();
            DateTime now = DateTime.UtcNow;
            Sessions[token] = new Session { Username = account.Username, GmLevel = account.GM, Created = now, LastSeen = now };
            PruneExpired(now);

            // Add "; Secure" here once WebEngine serves HTTPS.
            return PageResult.Redirect(returnTo)
                             .WithHeader("Set-Cookie", CookieName + "=" + token + "; Path=/; HttpOnly; SameSite=Strict");
        }

        /// <summary>
        /// Checks a password against a login.Password hash, "iterations:salt:hash" in base64 with
        /// PBKDF2-SHA1 - the format OmniCell.Core's PasswordHash writes.
        /// </summary>
        private static bool ValidatePassword(string password, string storedHash)
        {
            try
            {
                string[] parts = (storedHash ?? string.Empty).Split(':');
                if (parts.Length != 3)
                {
                    return false;
                }

                int iterations = int.Parse(parts[0]);
                if (iterations < 1 || iterations > 5000000)
                {
                    return false;
                }

                byte[] salt = Convert.FromBase64String(parts[1]);
                byte[] expected = Convert.FromBase64String(parts[2]);
                byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(password),
                    salt,
                    iterations,
                    HashAlgorithmName.SHA1,
                    expected.Length);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool IsLockedOut(string address)
        {
            Failures failures;
            if (!FailuresByAddress.TryGetValue(address, out failures))
            {
                return false;
            }

            lock (failures)
            {
                if (DateTime.UtcNow - failures.WindowStart > FailureWindow)
                {
                    failures.Count = 0;
                    failures.WindowStart = DateTime.UtcNow;
                }

                return failures.Count >= MaxFailures;
            }
        }

        private static void RecordFailure(string address)
        {
            Failures failures = FailuresByAddress.GetOrAdd(address, a => new Failures { WindowStart = DateTime.UtcNow });
            lock (failures)
            {
                if (DateTime.UtcNow - failures.WindowStart > FailureWindow)
                {
                    failures.Count = 0;
                    failures.WindowStart = DateTime.UtcNow;
                }

                failures.Count++;
            }
        }

        private static void PruneExpired(DateTime now)
        {
            foreach (var pair in Sessions)
            {
                if (now - pair.Value.LastSeen > IdleTimeout || now - pair.Value.Created > MaxSessionAge)
                {
                    Session removed;
                    Sessions.TryRemove(pair.Key, out removed);
                }
            }
        }

        private static string NewToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        /// <summary>Only same-site paths, so the login page cannot be used to bounce someone elsewhere.</summary>
        private static string SafeReturn(string returnTo)
        {
            if (string.IsNullOrEmpty(returnTo) || returnTo[0] != '/' || returnTo.StartsWith("//", StringComparison.Ordinal)
                || returnTo.IndexOf('\\') >= 0 || returnTo.IndexOf('\r') >= 0 || returnTo.IndexOf('\n') >= 0)
            {
                return IconPages.Page;
            }

            return returnTo;
        }

        private static string JsonEscape(string s)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in s ?? string.Empty)
            {
                if (c == '"' || c == '\\')
                {
                    sb.Append('\\').Append(c);
                }
                else if (c < 0x20)
                {
                    sb.Append("\\u").Append(((int)c).ToString("x4"));
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        private static string LoginPage(string returnTo, string error, string username)
        {
            string e = WebUtility.HtmlEncode(error ?? string.Empty);
            return "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\">"
                   + "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">"
                   + "<title>Sign in · OmniCell</title><style>"
                   + ":root{--bg:#0d1117;--panel:#161b22;--border:#30363d;--text:#e6edf3;--muted:#8b949e;--accent:#58a6ff;--err:#f85149}"
                   + "*{box-sizing:border-box}html,body{margin:0;min-height:100%;background:var(--bg);color:#c9d1d9;"
                   + "font:14px/1.45 'Segoe UI',system-ui,-apple-system,Tahoma,sans-serif}"
                   + ".wrap{min-height:100vh;display:grid;place-items:center;padding:24px 16px}"
                   + ".card{width:min(380px,100%);padding:28px;border:1px solid var(--border);border-radius:12px;background:var(--panel)}"
                   + "h1{margin:0 0 4px;font-size:19px;font-weight:600;color:var(--text)}"
                   + ".sub{margin:0 0 22px;font-size:13px;color:var(--muted)}"
                   + "label{display:block;margin:0 0 6px;font-size:13px;font-weight:600;color:var(--text)}"
                   + "input{width:100%;height:38px;margin:0 0 16px;padding:0 12px;border-radius:6px;border:1px solid var(--border);"
                   + "background:var(--bg);color:var(--text);font:inherit;outline:none}"
                   + "input:focus{border-color:var(--accent);box-shadow:0 0 0 3px rgba(56,139,253,.15)}"
                   + "button{width:100%;height:38px;margin-top:4px;border:0;border-radius:6px;background:#238636;color:#fff;"
                   + "font:600 14px inherit;font-family:inherit;cursor:pointer}button:hover{background:#2ea043}"
                   + ".err{margin:0 0 16px;padding:10px 12px;border:1px solid rgba(248,81,73,.4);border-radius:6px;"
                   + "background:rgba(248,81,73,.1);color:var(--err);font-size:13px}"
                   + ".foot{margin:18px 0 0;font-size:12px;color:var(--muted);text-align:center}"
                   + "</style></head><body><div class=\"wrap\"><form class=\"card\" method=\"post\" action=\"" + LoginPath + "\">"
                   + "<h1>Admin sign-in</h1>"
                   + "<p class=\"sub\">Use a game account with GM level " + RequiredGmLevel + " or higher.</p>"
                   + (e.Length > 0 ? "<p class=\"err\" role=\"alert\">" + e + "</p>" : string.Empty)
                   + "<input type=\"hidden\" name=\"return\" value=\"" + WebUtility.HtmlEncode(returnTo) + "\">"
                   + "<label for=\"username\">Account name</label>"
                   + "<input id=\"username\" name=\"username\" autocomplete=\"username\" required autofocus value=\""
                   + WebUtility.HtmlEncode(username ?? string.Empty) + "\">"
                   + "<label for=\"password\">Password</label>"
                   + "<input id=\"password\" name=\"password\" type=\"password\" autocomplete=\"current-password\" required>"
                   + "<button type=\"submit\">Sign in</button>"
                   + "<p class=\"foot\">OmniCell WebEngine</p>"
                   + "</form></div></body></html>";
        }
    }
}
