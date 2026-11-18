using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace AIMClassic
{
    public enum OnlineStatus
    {
        Online,
        Away,
        Busy,
        Invisible,
        Offline
    }

    public class Account
    {
        public string ScreenName { get; set; }
        public string Password { get; set; }
        public bool SavePassword { get; set; }
        public bool AutoLogin { get; set; }
        public string Avatar { get; set; }
        public string DisplayName { get; set; }
        public string Location { get; set; }
        public string MemberSince { get; set; }
        public string Interests { get; set; }
        public string Bio { get; set; }
        public string Email { get; set; }
        public bool AvailableForChat { get; set; }
    }

    public class BuddyGroup
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool Collapsed { get; set; }
    }

    public class Buddy
    {
        public string ScreenName { get; set; }
        public string Alias { get; set; }
        public string GroupId { get; set; }
        public string Status { get; set; } // online, away, busy, offline
        public string Avatar { get; set; }
        public int IdleMinutes { get; set; }
        public int WarningLevel { get; set; }
        public string AwayText { get; set; }
        public string Location { get; set; }
        public string MemberSince { get; set; }
        public string Interests { get; set; }
        public string Bio { get; set; }
        public string Persona { get; set; } // AI personality hint
        public bool Blocked { get; set; }
        public bool JustSignedOn { get; set; }
        public bool IsRealUser { get; set; } // true = real person on LAN/P2P/Relay (never AI auto-reply!)
        public string PeerHost { get; set; }
        public int PeerPort { get; set; }
        public string AiModelOverride { get; set; } // optional per-buddy LLM override e.g. gemini-4-argon, gpt-6-astra
    }

    public class AwayPreset
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public bool AutoReply { get; set; }
    }

    public class ChatMessage
    {
        public string Id { get; set; }
        public string Sender { get; set; }
        public string Text { get; set; }
        public string Time { get; set; }
        public string Date { get; set; }
        public string Font { get; set; }
        public string Color { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
    }

    public class ChatRoom
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Topic { get; set; }
        public bool IsPublic { get; set; }
        public string Category { get; set; }
        public string CreatedBy { get; set; }
        public List<string> Participants { get; set; }
        public List<ChatMessage> Messages { get; set; }
    }

    public class FileTransferItem
    {
        public string Id { get; set; }
        public string Direction { get; set; } // incoming | outgoing
        public string Buddy { get; set; }
        public string FileName { get; set; }
        public string LocalSourcePath { get; set; }
        public long SizeBytes { get; set; }
        public long TransferredBytes { get; set; }
        public double SpeedKbps { get; set; }
        public string Status { get; set; } // offering | transferring | completed | cancelled
        public string StartedAt { get; set; }
    }

    public class ApiKeyProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Provider { get; set; }
        public string Endpoint { get; set; }
        public string ApiKey { get; set; }
        public string Model { get; set; }
    }

    public class AppSettings
    {
        public string Theme { get; set; } // LunaBlue | Aim6Ice | XpSilver
        public bool ShowXpBackdrop { get; set; } // Optional 1024x768 Bliss stage
        public bool ShowTimestamps { get; set; }
        public bool EnableEmoticons { get; set; }
        public bool ShowTypingIndicator { get; set; }
        public bool AutoPopupIM { get; set; }
        public string DefaultFont { get; set; }
        public string DefaultColor { get; set; }
        public int AutoAwayMinutes { get; set; }
        public bool MuteWhenBusy { get; set; }
        public bool SoundMuted { get; set; }
        public bool SoundDoorOpen { get; set; }
        public bool SoundDoorClose { get; set; }
        public bool SoundImReceive { get; set; }
        public bool SoundImSend { get; set; }
        public bool SoundFileDone { get; set; }
        public bool SoundAlert { get; set; }
        public bool SoundWelcome { get; set; }
        public bool SoundGoodbye { get; set; }
        public bool SoundMail { get; set; }
        public bool SoundRing { get; set; }
        public bool SoundChat { get; set; }
        public int SoundVersion { get; set; }
        public string PrivacyMode { get; set; }
        public string ServerHost { get; set; }
        public string ServerPort { get; set; }

        // AI Chat Configuration
        public bool AiEnabled { get; set; }
        public string AiEndpoint { get; set; }
        public string AiApiKey { get; set; }
        public string AiModel { get; set; }
        public string ActiveApiProfileId { get; set; }
        public List<ApiKeyProfile> ApiProfiles { get; set; }

        // Language, Translation, Updates, Banners & Real User Networking
        public string Language { get; set; } // "en" | "ru"
        public bool AutoTranslateChat { get; set; }
        public string TranslateTargetLang { get; set; } // "en" | "ru"
        public bool ShowBanners { get; set; }
        public bool AutoCheckUpdates { get; set; }
        public string UpdateChannel { get; set; }
        public string UpdateManifestUrl { get; set; }
        public int P2pListenPort { get; set; }
        public bool AutoDiscoverLan { get; set; }
        public string RelayServerHost { get; set; }
        public int RelayServerPort { get; set; }
        public bool GlobalCloudEnabled { get; set; }
        public string GlobalCloudRoom { get; set; }
        public List<string> MusicPlaylistPaths { get; set; }
        public bool ShareMusicInStatus { get; set; }
        public int MusicVolume { get; set; }
        public int SettingsVersion { get; set; }
    }

    public class MailItem
    {
        public string Id { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string Date { get; set; }
        public bool Read { get; set; }
    }

    public class CustomThemeDef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public bool IsGlass { get; set; }
        public string FaceHex { get; set; }
        public string FaceDarkHex { get; set; }
        public string TitleTopHex { get; set; }
        public string TitleMidHex { get; set; }
        public string TitleBotHex { get; set; }
        public string HeaderTopHex { get; set; }
        public string HeaderBotHex { get; set; }
        public string SubTopHex { get; set; }
        public string SubBotHex { get; set; }
        public string NavyHex { get; set; }
        public string GoldHex { get; set; }
        public string TitleTextHex { get; set; }
    }

    public class UserSessionState
    {
        public string Status { get; set; } // online, away, busy, invisible
        public string AwayMessageTitle { get; set; }
        public string AwayMessageText { get; set; }
        public string CustomStatusNote { get; set; }
    }

    public class AimState
    {
        public bool LoggedIn { get; set; }
        public string ActiveAccountSN { get; set; }
        public List<Account> Accounts { get; set; }
        public UserSessionState CurrentUser { get; set; }
        public List<BuddyGroup> Groups { get; set; }
        public List<Buddy> Buddies { get; set; }
        public List<AwayPreset> AwayPresets { get; set; }
        public Dictionary<string, List<ChatMessage>> ChatHistory { get; set; }
        public List<ChatRoom> ChatRooms { get; set; }
        public List<FileTransferItem> FileTransfers { get; set; }
        public List<MailItem> MailItems { get; set; }
        public List<CustomThemeDef> CustomThemes { get; set; }
        public AppSettings Settings { get; set; }
        public Dictionary<string, bool> UnreadBuddies { get; set; }
    }

    public static class Store
    {
        private static AimState _state;
        private static readonly object _lock = new object();
        private static readonly JavaScriptSerializer _json = new JavaScriptSerializer { MaxJsonLength = 10485760 };

        public static event Action<string> Changed;

        public static string DataFilePath
        {
            get
            {
                string[] candidates = new[]
                {
                    Path.GetDirectoryName(typeof(Store).Assembly.Location),
                    AppDomain.CurrentDomain.BaseDirectory,
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AIMClassic"),
                    Path.Combine(Path.GetTempPath(), "AIMClassic")
                };
                foreach (string dir in candidates)
                {
                    if (string.IsNullOrEmpty(dir)) continue;
                    try
                    {
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        if (IsDirectoryWritable(dir)) return Path.Combine(dir, "aim_data.json");
                    }
                    catch { }
                }
                return Path.Combine(Path.GetTempPath(), "aim_data.json");
            }
        }

        private static bool IsDirectoryWritable(string dir)
        {
            try
            {
                string probe = Path.Combine(dir, ".aim_write_test");
                File.WriteAllText(probe, "1");
                File.Delete(probe);
                return true;
            }
            catch { return false; }
        }

        public static AimState State
        {
            get
            {
                if (_state == null) Load();
                return _state;
            }
        }

        public static string NowTimeStr()
        {
            return DateTime.Now.ToString("h:mm:ss tt");
        }

        public static string NowDateStr()
        {
            return DateTime.Now.ToString("MMM dd, yyyy");
        }

        public static void Load()
        {
            lock (_lock)
            {
                try
                {
                    string path = DataFilePath;
                    if (File.Exists(path))
                    {
                        string raw = File.ReadAllText(path, Encoding.UTF8);
                        AimState loaded = _json.Deserialize<AimState>(raw);
                        if (loaded != null && loaded.Accounts != null && loaded.Accounts.Count > 0)
                        {
                            EnsureDefaults(loaded);
                            Account acc = loaded.Accounts.FirstOrDefault(a => string.Equals(a.ScreenName, loaded.ActiveAccountSN, StringComparison.OrdinalIgnoreCase));
                            loaded.LoggedIn = (acc != null && acc.AutoLogin);
                            _state = loaded;
                            ApplyInstallLanguageMarker();
                            return;
                        }
                    }
                }
                catch { }
                _state = CreateDefaultState();
                ApplyInstallLanguageMarker();
                Save();
            }
        }

        private static void ApplyInstallLanguageMarker()
        {
            try
            {
                string[] dirs = new[]
                {
                    Path.GetDirectoryName(typeof(Store).Assembly.Location),
                    AppDomain.CurrentDomain.BaseDirectory
                };
                foreach (string d in dirs)
                {
                    if (string.IsNullOrEmpty(d)) continue;
                    string cfg = Path.Combine(d, "aim_install_lang.cfg");
                    if (File.Exists(cfg))
                    {
                        string lang = (File.ReadAllText(cfg, Encoding.UTF8) ?? "").Trim().ToLowerInvariant();
                        try { File.Delete(cfg); } catch { }
                        if (lang == "ru" || lang == "en")
                        {
                            _state.Settings.Language = lang;
                            Save();
                        }
                        break;
                    }
                }
            }
            catch { }
        }

        private static void EnsureDefaults(AimState st)
        {
            AimState def = CreateDefaultState();
            if (st.CurrentUser == null) st.CurrentUser = def.CurrentUser;
            if (st.Groups == null || st.Groups.Count == 0) st.Groups = def.Groups;
            if (st.Buddies == null) st.Buddies = def.Buddies;
            if (st.AwayPresets == null || st.AwayPresets.Count == 0) st.AwayPresets = def.AwayPresets;
            if (st.ChatHistory == null) st.ChatHistory = def.ChatHistory;
            if (st.ChatRooms == null || st.ChatRooms.Count == 0) st.ChatRooms = def.ChatRooms;
            if (st.FileTransfers == null) st.FileTransfers = def.FileTransfers;
            if (st.MailItems == null || st.MailItems.Count == 0) st.MailItems = def.MailItems;
            if (st.CustomThemes == null) st.CustomThemes = new List<CustomThemeDef>();
            if (st.Settings == null) st.Settings = def.Settings;
            if (st.UnreadBuddies == null) st.UnreadBuddies = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(st.Settings.AiEndpoint)) st.Settings.AiEndpoint = def.Settings.AiEndpoint;
            if (string.IsNullOrEmpty(st.Settings.AiModel)) st.Settings.AiModel = def.Settings.AiModel;
            if (string.IsNullOrEmpty(st.Settings.Theme)) st.Settings.Theme = "LunaBlue";
            if (st.Settings.ApiProfiles == null || st.Settings.ApiProfiles.Count == 0)
            {
                st.Settings.ApiProfiles = def.Settings.ApiProfiles;
                if (string.IsNullOrEmpty(st.Settings.ActiveApiProfileId))
                    st.Settings.ActiveApiProfileId = def.Settings.ActiveApiProfileId;
            }
            if (string.IsNullOrEmpty(st.Settings.Language)) st.Settings.Language = "en";
            if (string.IsNullOrEmpty(st.Settings.TranslateTargetLang)) st.Settings.TranslateTargetLang = "en";
            if (string.IsNullOrEmpty(st.Settings.UpdateChannel)) st.Settings.UpdateChannel = "Stable (v5.9)";
            if (st.Settings.P2pListenPort <= 0) st.Settings.P2pListenPort = 5192;
            if (st.Settings.RelayServerPort <= 0) st.Settings.RelayServerPort = 5190;
            if (!st.Groups.Any(g => g.Id == "grp_real"))
                st.Groups.Insert(0, new BuddyGroup { Id = "grp_real", Name = "Real Friends", Collapsed = false });
            if (st.Settings.SoundVersion < 2)
            {
                st.Settings.SoundWelcome = true;
                st.Settings.SoundGoodbye = true;
                st.Settings.SoundMail = true;
                st.Settings.SoundRing = true;
                st.Settings.SoundChat = true;
                st.Settings.SoundVersion = 2;
            }
            if (st.Settings.SettingsVersion < 3)
            {
                st.Settings.Language = "en";
                st.Settings.TranslateTargetLang = "en";
                st.Settings.ShowBanners = true;
                st.Settings.AutoCheckUpdates = true;
                st.Settings.AutoDiscoverLan = true;
                Buddy aiBuddy = st.Buddies.FirstOrDefault(b => b.ScreenName == "AIM_AI_Helper");
                if (aiBuddy != null && aiBuddy.Alias == "AI Собеседник") aiBuddy.Alias = "AI Assistant";
                Buddy mashaBuddy = st.Buddies.FirstOrDefault(b => b.ScreenName == "Masha_2005");
                if (mashaBuddy != null && mashaBuddy.Alias == "Маша") mashaBuddy.Alias = "Masha";
            }
            if (st.Settings.SettingsVersion < 4)
            {
                if (string.IsNullOrEmpty(st.Settings.GlobalCloudRoom)) st.Settings.GlobalCloudRoom = "AIM-Global-2006";
                st.Settings.GlobalCloudEnabled = true;
                st.Settings.ShowBanners = true;
                st.Settings.SettingsVersion = 4;
            }
            if (st.Settings.SettingsVersion < 5)
            {
                foreach (ApiKeyProfile dp in def.Settings.ApiProfiles)
                {
                    if (!st.Settings.ApiProfiles.Any(x => string.Equals(x.Id, dp.Id, StringComparison.OrdinalIgnoreCase)))
                    {
                        st.Settings.ApiProfiles.Add(dp);
                    }
                }
                string aiGrpId = st.Groups.Any(g => g.Id == "grp_ai") ? "grp_ai" : st.Groups[0].Id;
                foreach (Buddy db in def.Buddies.Where(b => b.GroupId == "grp_ai"))
                {
                    if (!st.Buddies.Any(b => string.Equals(b.ScreenName, db.ScreenName, StringComparison.OrdinalIgnoreCase)))
                    {
                        db.GroupId = aiGrpId;
                        st.Buddies.Add(db);
                    }
                }
                st.Settings.SettingsVersion = 5;
            }
            if (st.Settings.SettingsVersion < 6)
            {
                if (st.Settings.MusicPlaylistPaths == null) st.Settings.MusicPlaylistPaths = new List<string>();
                if (st.Settings.MusicVolume <= 0) st.Settings.MusicVolume = 85;
                st.Settings.ShareMusicInStatus = true;
                foreach (ChatRoom existing in st.ChatRooms)
                {
                    existing.IsPublic = true;
                    if (string.IsNullOrEmpty(existing.Category)) existing.Category = "General";
                    if (string.IsNullOrEmpty(existing.CreatedBy)) existing.CreatedBy = "AIM Classic";
                }
                foreach (ChatRoom dr in def.ChatRooms)
                {
                    if (!st.ChatRooms.Any(x => string.Equals(x.Name, dr.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        st.ChatRooms.Add(dr);
                    }
                }
                st.Settings.SettingsVersion = 6;
            }
            if (st.Settings.MusicPlaylistPaths == null) st.Settings.MusicPlaylistPaths = new List<string>();
        }

        public static void Save()
        {
            lock (_lock)
            {
                try
                {
                    if (_state == null) return;
                    string json = _json.Serialize(_state);
                    File.WriteAllText(DataFilePath, json, Encoding.UTF8);
                }
                catch { }
            }
        }

        public static void Notify(string topic)
        {
            Save();
            Action<string> handler = Changed;
            if (handler != null) handler(topic);
        }

        public static Account GetActiveAccount()
        {
            Account acc = State.Accounts.FirstOrDefault(a => string.Equals(a.ScreenName, State.ActiveAccountSN, StringComparison.OrdinalIgnoreCase));
            return acc ?? State.Accounts[0];
        }

        public static Buddy GetBuddy(string screenName)
        {
            if (string.IsNullOrEmpty(screenName)) return null;
            return State.Buddies.FirstOrDefault(b => string.Equals(b.ScreenName, screenName, StringComparison.OrdinalIgnoreCase));
        }

        public static void AppendChatMessage(string buddySN, ChatMessage msg)
        {
            if (!State.ChatHistory.ContainsKey(buddySN))
            {
                State.ChatHistory[buddySN] = new List<ChatMessage>();
            }
            if (string.IsNullOrEmpty(msg.Id)) msg.Id = "m_" + DateTime.Now.Ticks;
            if (string.IsNullOrEmpty(msg.Time)) msg.Time = NowTimeStr();
            if (string.IsNullOrEmpty(msg.Date)) msg.Date = "Today";
            State.ChatHistory[buddySN].Add(msg);
            Notify("history");
        }

        public static string RegisterAccount(string screenName, string password, string displayName, string email, string avatar, string location, string interests, string bio)
        {
            if (string.IsNullOrWhiteSpace(screenName) || screenName.Length < 3 || screenName.Length > 20)
                return "Screen Name must be 3-20 characters.";
            if (string.IsNullOrWhiteSpace(password) || password.Length < 3)
                return "Password must be at least 3 characters.";
            if (State.Accounts.Any(a => string.Equals(a.ScreenName, screenName, StringComparison.OrdinalIgnoreCase)))
                return "That Screen Name is already registered on this PC.";

            Account acc = new Account
            {
                ScreenName = screenName.Trim(),
                Password = password,
                SavePassword = true,
                AutoLogin = false,
                Avatar = string.IsNullOrEmpty(avatar) ? "AIM Icon.gif" : avatar,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? screenName.Trim() : displayName.Trim(),
                Location = string.IsNullOrWhiteSpace(location) ? "Internet" : location.Trim(),
                MemberSince = NowDateStr(),
                Interests = string.IsNullOrWhiteSpace(interests) ? "AIM, Music, Retro PC" : interests.Trim(),
                Bio = string.IsNullOrWhiteSpace(bio) ? string.Format("Hey! I'm {0} on AIM Classic :-)", screenName.Trim()) : bio,
                Email = string.IsNullOrWhiteSpace(email) ? screenName.Trim().ToLower() + "@aol.com" : email.Trim(),
                AvailableForChat = true
            };
            State.Accounts.Add(acc);
            Notify("accounts");
            return null;
        }

        public static Buddy AddFriendByNickname(string nickname, string alias, string groupId, string avatar, string persona, string initialStatus)
        {
            string clean = (nickname ?? "").Trim();
            if (clean.Length < 2) return null;
            Buddy existing = GetBuddy(clean);
            if (existing != null) return existing;

            // Check if nickname matches another locally registered account!
            Account localAcc = State.Accounts.FirstOrDefault(a => string.Equals(a.ScreenName, clean, StringComparison.OrdinalIgnoreCase));

            string[] fallbackAvatars = new[] { "Pirate.gif", "Cat.gif", "Dragon 1.gif", "Smiley Face.gif", "Alien.gif", "Dog.gif", "Butterfly.gif", "Robot.gif" };
            string chosenAv = !string.IsNullOrEmpty(avatar)
                ? avatar
                : (localAcc != null ? localAcc.Avatar : fallbackAvatars[Math.Abs(clean.GetHashCode()) % fallbackAvatars.Length]);

            Buddy b = new Buddy
            {
                ScreenName = localAcc != null ? localAcc.ScreenName : clean,
                Alias = alias ?? "",
                GroupId = string.IsNullOrEmpty(groupId) ? State.Groups[0].Id : groupId,
                Status = string.IsNullOrEmpty(initialStatus) ? "online" : initialStatus,
                Avatar = chosenAv,
                IdleMinutes = 0,
                WarningLevel = 0,
                AwayText = initialStatus == "away" ? "BrB! :-)" : "",
                Location = localAcc != null ? localAcc.Location : "AIM Network",
                MemberSince = localAcc != null ? localAcc.MemberSince : NowDateStr(),
                Interests = localAcc != null ? localAcc.Interests : "Chatting, Music, Games",
                Bio = localAcc != null ? localAcc.Bio : string.Format("Friend added by nickname ({0}) :-)", clean),
                Persona = string.IsNullOrEmpty(persona) ? "friendly" : persona,
                Blocked = false,
                JustSignedOn = initialStatus == "online"
            };
            State.Buddies.Add(b);
            Notify("buddies");
            return b;
        }

        public static Buddy AddRealFriend(string screenName, string peerHost, int peerPort, string status, string avatar, string bio, string groupId)
        {
            string clean = (screenName ?? "").Trim();
            if (clean.Length < 2) return null;
            if (string.IsNullOrEmpty(groupId))
            {
                BuddyGroup realGrp = State.Groups.FirstOrDefault(g => g.Id == "grp_real");
                groupId = realGrp != null ? realGrp.Id : State.Groups[0].Id;
            }
            Buddy existing = GetBuddy(clean);
            if (existing != null)
            {
                existing.IsRealUser = true;
                if (!string.IsNullOrEmpty(peerHost)) existing.PeerHost = peerHost;
                if (peerPort > 0) existing.PeerPort = peerPort;
                if (!string.IsNullOrEmpty(status)) existing.Status = status;
                if (!string.IsNullOrEmpty(avatar)) existing.Avatar = avatar;
                if (!string.IsNullOrEmpty(bio)) existing.Bio = bio;
                Notify("buddies");
                return existing;
            }
            Buddy b = new Buddy
            {
                ScreenName = clean,
                Alias = "",
                GroupId = groupId,
                Status = string.IsNullOrEmpty(status) ? "online" : status,
                Avatar = string.IsNullOrEmpty(avatar) ? "AIM Icon.gif" : avatar,
                IdleMinutes = 0,
                WarningLevel = 0,
                Location = string.IsNullOrEmpty(peerHost) ? "AIM Network" : (peerHost + ":" + peerPort),
                MemberSince = NowDateStr(),
                Interests = "AIM Real Peer",
                Bio = string.IsNullOrEmpty(bio) ? "Real AIM user connected via P2P / Relay :-)" : bio,
                Persona = "human",
                IsRealUser = true,
                PeerHost = peerHost ?? "",
                PeerPort = peerPort,
                JustSignedOn = true
            };
            State.Buddies.Add(b);
            Notify("buddies");
            return b;
        }

        public static void ResetToFactory()
        {
            _state = CreateDefaultState();
            Notify("all");
        }

        public static AimState CreateDefaultState()
        {
            return new AimState
            {
                LoggedIn = false,
                ActiveAccountSN = "RetroUser_2006",
                Accounts = new List<Account>
                {
                    new Account
                    {
                        ScreenName = "RetroUser_2006",
                        Password = "password06",
                        SavePassword = true,
                        AutoLogin = false,
                        Avatar = "AIM Icon.gif",
                        DisplayName = "Alex (RetroUser_2006)",
                        Location = "Springfield, USA",
                        MemberSince = "Oct 14, 2004",
                        Interests = "Counter-Strike 1.6, Linkin Park, Winamp, Retro PCs",
                        Bio = "~*~ Welcome to my AIM Profile ~*~\r\nListening to: Linkin Park - Numb.mp3\r\n\"Stay awesome\" :-)",
                        Email = "retrouser2006@aol.com",
                        AvailableForChat = true
                    },
                    new Account
                    {
                        ScreenName = "CoolGamer_07",
                        Password = "cs16rules",
                        SavePassword = true,
                        AutoLogin = false,
                        Avatar = "Dragon 1.gif",
                        DisplayName = "Max [Clan Leader]",
                        Location = "Seattle, WA",
                        MemberSince = "Mar 02, 2005",
                        Interests = "World of Warcraft, CS 1.6, LAN Parties",
                        Bio = "de_dust2 5v5 scrims!! :-D",
                        Email = "coolgamer07@aim.com",
                        AvailableForChat = true
                    }
                },
                CurrentUser = new UserSessionState
                {
                    Status = "online",
                    AwayMessageTitle = "BRB",
                    AwayMessageText = "~*~ BrB getting pizza & Mountain Dew ~*~ Leave a message! (%t)",
                    CustomStatusNote = "Chillin on Windows XP :-)"
                },
                Groups = new List<BuddyGroup>
                {
                    new BuddyGroup { Id = "grp_real", Name = "Real Friends", Collapsed = false },
                    new BuddyGroup { Id = "grp_ai", Name = "AI & Helpers", Collapsed = false },
                    new BuddyGroup { Id = "grp_buddies", Name = "Buddies", Collapsed = false },
                    new BuddyGroup { Id = "grp_school", Name = "School & College", Collapsed = false },
                    new BuddyGroup { Id = "grp_gaming", Name = "Gaming Clan", Collapsed = false },
                    new BuddyGroup { Id = "grp_family", Name = "Family", Collapsed = true }
                },
                Buddies = new List<Buddy>
                {
                    new Buddy
                    {
                        ScreenName = "AIM_AI_Helper",
                        Alias = "AI Assistant",
                        GroupId = "grp_ai",
                        Status = "online",
                        Avatar = "Robot.gif",
                        IdleMinutes = 0,
                        WarningLevel = 0,
                        Location = "AOL Neural Lab (2006)",
                        MemberSince = "Jan 01, 2006",
                        Interests = "AI Chat, Helping Friends, Trivia, Coding, Music",
                        Bio = "Hi! I'm the built-in AIM AI companion. Ask me anything in English or Russian! :-)",
                        Persona = "assistant"
                    },
                    new Buddy
                    {
                        ScreenName = "Argon_AI",
                        Alias = "Gemini 4 Argon (Work)",
                        GroupId = "grp_ai",
                        Status = "online",
                        Avatar = "Robot.gif",
                        IdleMinutes = 0,
                        WarningLevel = 0,
                        Location = "Google DeepMind / Gemini 4",
                        MemberSince = "Nov 16, 2026",
                        Interests = "Deep Reasoning, Engineering, Strategy, Research",
                        Bio = "Gemini 4 Argon — Heavyweight reasoning & work assistant (gemini-4-argon).",
                        Persona = "work",
                        AiModelOverride = "gemini-4-argon"
                    },
                    new Buddy
                    {
                        ScreenName = "Hydrogen_Dev",
                        Alias = "Gemini 4 Hydrogen (Code)",
                        GroupId = "grp_ai",
                        Status = "online",
                        Avatar = "Alien.gif",
                        IdleMinutes = 0,
                        WarningLevel = 0,
                        Location = "Google AI Studio",
                        MemberSince = "Nov 16, 2026",
                        Interests = "Fast Coding, Debugging, Productivity, Scripts",
                        Bio = "Gemini 4 Hydrogen — High-speed coding & everyday work model (gemini-4-hydrogen).",
                        Persona = "work",
                        AiModelOverride = "gemini-4-hydrogen"
                    },
                    new Buddy
                    {
                        ScreenName = "Astra_GPT",
                        Alias = "GPT-6 Astra (ChatGPT)",
                        GroupId = "grp_ai",
                        Status = "online",
                        Avatar = "AIM Icon.gif",
                        IdleMinutes = 0,
                        WarningLevel = 0,
                        Location = "OpenAI Frontier Lab",
                        MemberSince = "Nov 16, 2026",
                        Interests = "Analysis, Writing, Architecture, Problem Solving",
                        Bio = "OpenAI GPT-6 Astra — Flagship work & reasoning model (gpt-6-astra).",
                        Persona = "work",
                        AiModelOverride = "gpt-6-astra"
                    },
                    new Buddy
                    {
                        ScreenName = "angel_kisses92",
                        Alias = "",
                        GroupId = "grp_buddies",
                        Status = "online",
                        Avatar = "Heart.gif",
                        IdleMinutes = 0,
                        WarningLevel = 0,
                        Location = "Chicago, IL",
                        MemberSince = "Nov 11, 2004",
                        Interests = "MySpace, Avril Lavigne, Shopping, Pop-Punk",
                        Bio = "~*~ Smile because it happened <3 ~*~",
                        Persona = "teen2006"
                    },
                    new Buddy
                    {
                        ScreenName = "Masha_2005",
                        Alias = "Masha",
                        GroupId = "grp_buddies",
                        Status = "online",
                        Avatar = "Cat.gif",
                        IdleMinutes = 0,
                        WarningLevel = 0,
                        Location = "Moscow / Internet",
                        MemberSince = "Jan 25, 2005",
                        Interests = "Anime, Winamp, Forums, 2000s Music",
                        Bio = "Heyyy!! Always happy to chat :-) ;-)",
                        Persona = "russian_friend"
                    },
                    new Buddy
                    {
                        ScreenName = "xX_Sk8erBoi_Xx",
                        Alias = "Jake",
                        GroupId = "grp_buddies",
                        Status = "away",
                        Avatar = "Skull & Crossbones.gif",
                        IdleMinutes = 14,
                        WarningLevel = 5,
                        AwayText = "At the skatepark till 6!! Call my cell if u need me B-)",
                        Location = "San Diego, CA",
                        MemberSince = "Feb 08, 2005",
                        Interests = "Tony Hawk, Blink-182, Vans",
                        Bio = "Keep rollin!! :-P",
                        Persona = "skater"
                    },
                    new Buddy
                    {
                        ScreenName = "NeoMatrix2006",
                        Alias = "",
                        GroupId = "grp_school",
                        Status = "online",
                        Avatar = "Hal9000.gif",
                        IdleMinutes = 2,
                        WarningLevel = 0,
                        Location = "Room 101",
                        MemberSince = "May 18, 2003",
                        Interests = "The Matrix, Linux, Pentium 4 Overclocking, Flash Games",
                        Bio = "There is no spoon. Follow the white rabbit B-)",
                        Persona = "geek"
                    },
                    new Buddy
                    {
                        ScreenName = "sarah_cheer07",
                        Alias = "Sarah",
                        GroupId = "grp_school",
                        Status = "busy",
                        Avatar = "Butterfly.gif",
                        IdleMinutes = 3,
                        WarningLevel = 0,
                        AwayText = "Studying for AP History test!! Do NOT disturb >:(",
                        Location = "Austin, TX",
                        MemberSince = "Sep 14, 2005",
                        Interests = "Cheerleading, Yearbook, Green Day",
                        Bio = "Class of 2007!!",
                        Persona = "teen2006"
                    },
                    new Buddy
                    {
                        ScreenName = "DarkKnight_88",
                        Alias = "Kevin (CS 1.6)",
                        GroupId = "grp_gaming",
                        Status = "online",
                        Avatar = "Pirate.gif",
                        IdleMinutes = 0,
                        WarningLevel = 10,
                        Location = "Toronto, ON",
                        MemberSince = "Jul 04, 2004",
                        Interests = "CS 1.6, WoW, Quake III, Radeon 9800",
                        Bio = "[DK] Clan — de_dust2 24/7 server admin! :-D",
                        Persona = "gamer"
                    },
                    new Buddy
                    {
                        ScreenName = "CyberWolf99",
                        Alias = "",
                        GroupId = "grp_gaming",
                        Status = "offline",
                        Avatar = "Dragon 2.gif",
                        Location = "Denver, CO",
                        MemberSince = "Dec 19, 2003",
                        Interests = "Warcraft III DotA, StarCraft",
                        Bio = "For the Alliance!",
                        Persona = "gamer"
                    },
                    new Buddy
                    {
                        ScreenName = "MomAndDad_Home",
                        Alias = "Home PC",
                        GroupId = "grp_family",
                        Status = "offline",
                        Avatar = "Smiley Face.gif",
                        Location = "Living Room",
                        MemberSince = "Nov 02, 2002",
                        Interests = "Solitaire, Family Photos",
                        Bio = "Call when you get home!",
                        Persona = "friendly"
                    }
                },
                AwayPresets = new List<AwayPreset>
                {
                    new AwayPreset { Id = "away_brb", Title = "BRB", Text = "~*~ BrB getting pizza & Mountain Dew ~*~ Leave a message! (%t)", AutoReply = true },
                    new AwayPreset { Id = "away_sleep", Title = "Sleeping", Text = "Zzz... Offline in spirit |-) Hit me up tomorrow %n!", AutoReply = true },
                    new AwayPreset { Id = "away_school", Title = "At School / Work", Text = "At class right now :-/ Will reply when I'm back at my PC!", AutoReply = true },
                    new AwayPreset { Id = "away_music", Title = "Listening to Winamp", Text = "Rocking out to Linkin Park & Blink-182 :-D", AutoReply = true },
                    new AwayPreset { Id = "away_cell", Title = "AFK - Call Cell", Text = "Not at the computer! Text me if urgent ;-)", AutoReply = true }
                },
                ChatHistory = new Dictionary<string, List<ChatMessage>>(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        "AIM_AI_Helper",
                        new List<ChatMessage>
                        {
                            new ChatMessage { Id = "h0", Sender = "AIM_AI_Helper", Text = "Hey! I'm your AI companion in classic 2006 AIM :-) Ask me anything or switch languages using the Translate button!", Time = "4:10:00 PM", Date = "Today", Font = "Tahoma", Color = "#000080" }
                        }
                    },
                    {
                        "angel_kisses92",
                        new List<ChatMessage>
                        {
                            new ChatMessage { Id = "h1", Sender = "angel_kisses92", Text = "heyyy!! did u see the new episode of Lost last night?? :-O", Time = "4:12:08 PM", Date = "Today", Font = "Comic Sans MS", Color = "#660099" },
                            new ChatMessage { Id = "h2", Sender = "RetroUser_2006", Text = "yeah!! that ending was crazy :-D btw did u update ur MySpace top 8?", Time = "4:12:41 PM", Date = "Today", Font = "Tahoma", Color = "#000000" },
                            new ChatMessage { Id = "h3", Sender = "angel_kisses92", Text = "omg yes!! i put u right at #2 <3 brb gotta grab my iPod!", Time = "4:13:19 PM", Date = "Today", Font = "Comic Sans MS", Color = "#660099" }
                        }
                    },
                    {
                        "DarkKnight_88",
                        new List<ChatMessage>
                        {
                            new ChatMessage { Id = "h4", Sender = "DarkKnight_88", Text = "yo! 5v5 de_dust2 scrim at 8pm tonight, u in? B-)", Time = "8:45:12 PM", Date = "Yesterday", Font = "Tahoma", Color = "#000080" }
                        }
                    }
                },
                ChatRooms = new List<ChatRoom>
                {
                    new ChatRoom
                    {
                        Id = "room_lobby",
                        Name = "#AIM_Lobby_2006",
                        Topic = "Welcome to AIM Chat! Keep it friendly & have fun :-)",
                        IsPublic = true,
                        Category = "General",
                        CreatedBy = "AIM Team",
                        Participants = new List<string> { "RetroUser_2006", "AIM_AI_Helper", "angel_kisses92", "Masha_2005", "NeoMatrix2006", "DarkKnight_88" },
                        Messages = new List<ChatMessage>
                        {
                            new ChatMessage { Sender = "SYSTEM", Text = "*** Welcome to #AIM_Lobby_2006 — Topic: Welcome to AIM Chat! :-)", Time = "4:20:01 PM" },
                            new ChatMessage { Sender = "NeoMatrix2006", Text = "hey everyone! anyone else running WinXP SP2?", Time = "4:21:14 PM", Color = "#000099" },
                            new ChatMessage { Sender = "Masha_2005", Text = "hi everyone!! XP Luna Blue forever :-D", Time = "4:22:05 PM", Color = "#000099" },
                            new ChatMessage { Sender = "angel_kisses92", Text = "just changed my buddy icon!! what do u guys think? <3", Time = "4:24:40 PM", Color = "#000099" }
                        }
                    },
                    new ChatRoom
                    {
                        Id = "room_global_plaza",
                        Name = "#Global_AIM_Plaza",
                        Topic = "Global Public Hub — Say hi to real AIM users worldwide!",
                        IsPublic = true,
                        Category = "Global",
                        CreatedBy = "GlobalHub",
                        Participants = new List<string> { "RetroUser_2006", "AIM_AI_Helper" },
                        Messages = new List<ChatMessage>
                        {
                            new ChatMessage { Sender = "SYSTEM", Text = "*** Connected to #Global_AIM_Plaza (Global Cloud Live Room)", Time = "4:25:00 PM" }
                        }
                    },
                    new ChatRoom
                    {
                        Id = "room_aero",
                        Name = "#FrutigerAero_Hub",
                        Topic = "2007 Glossy Aqua Glass, XP Themes & Nostalgia :-D",
                        IsPublic = true,
                        Category = "Themes & Design",
                        CreatedBy = "AeroFan",
                        Participants = new List<string> { "RetroUser_2006", "Masha_2005" },
                        Messages = new List<ChatMessage>
                        {
                            new ChatMessage { Sender = "SYSTEM", Text = "*** Welcome to #FrutigerAero_Hub — Check AIM Workshop for Aqua Glass!", Time = "4:26:00 PM" }
                        }
                    },
                    new ChatRoom
                    {
                        Id = "room_cs16",
                        Name = "#CounterStrike_1_6",
                        Topic = "de_dust2 / awp_india / Clan Matches",
                        IsPublic = true,
                        Category = "Gaming",
                        CreatedBy = "DarkKnight_88",
                        Participants = new List<string> { "DarkKnight_88", "NeoMatrix2006", "xX_Sk8erBoi_Xx" },
                        Messages = new List<ChatMessage>
                        {
                            new ChatMessage { Sender = "SYSTEM", Text = "*** Welcome to #CounterStrike_1_6", Time = "3:55:00 PM" },
                            new ChatMessage { Sender = "DarkKnight_88", Text = "need 1 more for de_inferno scrim!", Time = "4:18:10 PM", Color = "#000099" }
                        }
                    },
                    new ChatRoom
                    {
                        Id = "room_music",
                        Name = "#LinkinPark_Fans",
                        Topic = "Meteora & Hybrid Theory — Best Rock of the 2000s!",
                        IsPublic = true,
                        Category = "Music",
                        CreatedBy = "Masha_2005",
                        Participants = new List<string> { "Masha_2005", "xX_Sk8erBoi_Xx", "sarah_cheer07" },
                        Messages = new List<ChatMessage>
                        {
                            new ChatMessage { Sender = "SYSTEM", Text = "*** Welcome to #LinkinPark_Fans", Time = "4:05:12 PM" },
                            new ChatMessage { Sender = "xX_Sk8erBoi_Xx", Text = "Faint is still the best track ever B-)", Time = "4:10:22 PM", Color = "#000099" }
                        }
                    },
                    new ChatRoom
                    {
                        Id = "room_ai_lab",
                        Name = "#AI_Work_Lab",
                        Topic = "Argon, Hydrogen, Astra, Ollama & Coding Help",
                        IsPublic = true,
                        Category = "Tech & AI",
                        CreatedBy = "Argon_AI",
                        Participants = new List<string> { "RetroUser_2006", "Argon_AI", "Hydrogen_Dev", "Astra_GPT" },
                        Messages = new List<ChatMessage>
                        {
                            new ChatMessage { Sender = "SYSTEM", Text = "*** Welcome to #AI_Work_Lab — Ask any AI bot or test /roll /8ball!", Time = "4:30:00 PM" }
                        }
                    }
                },
                MailItems = new List<MailItem>
                {
                    new MailItem
                    {
                        Id = "mail_1",
                        From = "welcome@aol.com",
                        To = "RetroUser_2006@aol.com",
                        Subject = "Welcome to AOL Instant Messenger 5.9 Classic!",
                        Date = "Today 4:05 PM",
                        Read = false,
                        Body = "You've got mail!\r\n\r\nWelcome back to 2005-2008 AOL Instant Messenger.\r\n\r\nNew in this edition:\r\n• Real Friends on LAN & Global Internet Cloud\r\n• AIM Theme Workshop + Frutiger Aero Gloss Theme\r\n• Chat Translator (EN <-> RU) + AI Bot API Keys\r\n• Classic Rotating Promo Banners & Update Center\r\n\r\nSee you online!\r\n— The AIM Classic Team"
                    },
                    new MailItem
                    {
                        Id = "mail_2",
                        From = "DarkKnight_88@aim.com",
                        To = "RetroUser_2006@aol.com",
                        Subject = "CS 1.6 Clan Scrim Tonight @ 8PM (de_dust2)",
                        Date = "Today 3:18 PM",
                        Read = false,
                        Body = "yo!! we got 4 players ready for 5v5 de_dust2 + cs_italy tonight.\r\nhop into #CounterStrike_1_6 when ur ready B-)"
                    },
                    new MailItem
                    {
                        Id = "mail_3",
                        From = "angel_kisses92@aol.com",
                        To = "RetroUser_2006@aol.com",
                        Subject = "omg check my new MySpace profile & away msg <3",
                        Date = "Yesterday 9:42 PM",
                        Read = false,
                        Body = "hii! i put the new Linkin Park song on my page and added u to #2!! :-) ttyl!"
                    }
                },
                CustomThemes = new List<CustomThemeDef>(),
                FileTransfers = new List<FileTransferItem>
                {
                    new FileTransferItem
                    {
                        Id = "ft_seed_1",
                        Direction = "incoming",
                        Buddy = "DarkKnight_88",
                        FileName = "CS16_aim_map.bsp",
                        SizeBytes = 1248256,
                        TransferredBytes = 1248256,
                        SpeedKbps = 64.5,
                        Status = "completed",
                        StartedAt = "Yesterday 8:50 PM"
                    }
                },
                Settings = new AppSettings
                {
                    Theme = "LunaBlue",
                    ShowXpBackdrop = false,
                    ShowTimestamps = true,
                    EnableEmoticons = true,
                    ShowTypingIndicator = true,
                    AutoPopupIM = true,
                    DefaultFont = "Tahoma",
                    DefaultColor = "#000000",
                    AutoAwayMinutes = 10,
                    MuteWhenBusy = true,
                    SoundMuted = false,
                    SoundDoorOpen = true,
                    SoundDoorClose = true,
                    SoundImReceive = true,
                    SoundImSend = true,
                    SoundFileDone = true,
                    SoundAlert = true,
                    SoundWelcome = true,
                    SoundGoodbye = true,
                    SoundMail = true,
                    SoundRing = true,
                    SoundChat = true,
                    SoundVersion = 2,
                    PrivacyMode = "allowAll",
                    ServerHost = "login.oscar.aol.com",
                    ServerPort = "5190",
                    AiEnabled = true,
                    AiEndpoint = "https://api.openai.com/v1/chat/completions",
                    AiApiKey = "",
                    AiModel = "gpt-4o-mini",
                    ActiveApiProfileId = "api_openai",
                    Language = "en",
                    AutoTranslateChat = false,
                    TranslateTargetLang = "en",
                    ShowBanners = true,
                    AutoCheckUpdates = true,
                    UpdateChannel = "Stable (v5.9)",
                    UpdateManifestUrl = "",
                    P2pListenPort = 5192,
                    AutoDiscoverLan = true,
                RelayServerHost = "",
                RelayServerPort = 5190,
                    GlobalCloudEnabled = true,
                    GlobalCloudRoom = "AIM-Global-2006",
                    MusicPlaylistPaths = new List<string>(),
                    ShareMusicInStatus = true,
                    MusicVolume = 85,
                    SettingsVersion = 6,
                ApiProfiles = new List<ApiKeyProfile>
                    {
                        new ApiKeyProfile { Id = "api_openai", Name = "OpenAI ChatGPT", Provider = "OpenAI", Endpoint = "https://api.openai.com/v1/chat/completions", ApiKey = "", Model = "gpt-6-astra" },
                        new ApiKeyProfile { Id = "api_gemini", Name = "Google Gemini", Provider = "Gemini", Endpoint = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions", ApiKey = "", Model = "gemini-4-hydrogen" },
                        new ApiKeyProfile { Id = "api_anthropic", Name = "Anthropic Claude", Provider = "Anthropic", Endpoint = "https://api.anthropic.com/v1/messages", ApiKey = "", Model = "claude-5-sonnet" },
                        new ApiKeyProfile { Id = "api_openrouter", Name = "OpenRouter Cloud", Provider = "OpenRouter", Endpoint = "https://openrouter.ai/api/v1/chat/completions", ApiKey = "", Model = "google/gemini-4-argon" },
                        new ApiKeyProfile { Id = "api_deepseek", Name = "DeepSeek V4/R1", Provider = "DeepSeek", Endpoint = "https://api.deepseek.com/chat/completions", ApiKey = "", Model = "deepseek-v4" },
                        new ApiKeyProfile { Id = "api_xai", Name = "xAI Grok", Provider = "xAI", Endpoint = "https://api.x.ai/v1/chat/completions", ApiKey = "", Model = "grok-4" },
                        new ApiKeyProfile { Id = "api_groq", Name = "Groq Fast", Provider = "Groq", Endpoint = "https://api.groq.com/openai/v1/chat/completions", ApiKey = "", Model = "llama-4-maverick-17b-128e-instruct" },
                        new ApiKeyProfile { Id = "api_mistral", Name = "Mistral AI", Provider = "Mistral", Endpoint = "https://api.mistral.ai/v1/chat/completions", ApiKey = "", Model = "mistral-large-latest" },
                        new ApiKeyProfile { Id = "api_perplexity", Name = "Perplexity Search", Provider = "Perplexity", Endpoint = "https://api.perplexity.ai/chat/completions", ApiKey = "", Model = "sonar-pro" },
                        new ApiKeyProfile { Id = "api_qwen", Name = "Alibaba Qwen", Provider = "Qwen", Endpoint = "https://dashscope-intl.aliyuncs.com/compatible-mode/v1/chat/completions", ApiKey = "", Model = "qwen3-235b-a22b" },
                        new ApiKeyProfile { Id = "api_together", Name = "Together AI", Provider = "Together", Endpoint = "https://api.together.xyz/v1/chat/completions", ApiKey = "", Model = "meta-llama/Llama-4-Maverick-17B-128E-Instruct-FP8" },
                        new ApiKeyProfile { Id = "api_ollama", Name = "Local Ollama", Provider = "Ollama", Endpoint = "http://localhost:11434/v1/chat/completions", ApiKey = "", Model = "llama3.3" }
                    }
                },
                UnreadBuddies = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
            };
        }
    }
}
