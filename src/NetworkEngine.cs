using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace AIMClassic
{
    public class DiscoveredPeer
    {
        public string ScreenName { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Status { get; set; }
        public string Avatar { get; set; }
        public string Bio { get; set; }
        public string AwayText { get; set; }
        public string Source { get; set; } // LAN | Relay | Direct
        public DateTime LastSeen { get; set; }
    }

    public class NetPacket
    {
        public string Type { get; set; } // HELLO | IM | TYPING | RING | FRIEND_REQ | FRIEND_ACC | STATUS | GOODBYE | RELAY_REG | RELAY_LIST | RELAY_ROUTE
        public string From { get; set; }
        public string To { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Status { get; set; }
        public string Avatar { get; set; }
        public string Bio { get; set; }
        public string AwayText { get; set; }
        public string Text { get; set; }
        public string Font { get; set; }
        public string Color { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool Underline { get; set; }
        public List<DiscoveredPeer> Peers { get; set; }
        public bool Ok { get; set; }
    }

    public static class NetworkEngine
    {
        private const int UDP_DISCOVERY_PORT = 5191;
        private static readonly JavaScriptSerializer _json = new JavaScriptSerializer();
        private static readonly object _sync = new object();

        private static bool _running = false;
        private static TcpListener _p2pListener;
        private static UdpClient _udpReceiver;
        private static Thread _p2pAcceptThread;
        private static Thread _udpListenThread;
        private static Thread _beaconThread;

        private static TcpListener _relayListener;
        private static Thread _relayThread;
        private static bool _relayHosting = false;
        private static readonly Dictionary<string, DiscoveredPeer> _relayDirectory = new Dictionary<string, DiscoveredPeer>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<NetPacket>> _relayInbox = new Dictionary<string, List<NetPacket>>(StringComparer.OrdinalIgnoreCase);

        public static int ActiveP2pPort { get; private set; }
        public static bool IsRelayHosting { get { return _relayHosting; } }
        public static int ActiveRelayHostPort { get; private set; }
        public static string LocalIpAddress { get; private set; }
        public static string LastNetworkError { get; private set; }
        public static string GlobalCloudStatus { get; private set; }
        public static string LastGlobalError { get; private set; }

        private static long _lastDirPollUnix = 0;
        private static long _lastPmPollUnix = 0;
        private static readonly HashSet<string> _seenNtfyIds = new HashSet<string>();

        public static readonly Dictionary<string, DiscoveredPeer> DiscoveredPeers =
            new Dictionary<string, DiscoveredPeer>(StringComparer.OrdinalIgnoreCase);

        public static readonly Dictionary<string, ChatRoom> DiscoveredPublicRooms =
            new Dictionary<string, ChatRoom>(StringComparer.OrdinalIgnoreCase);

        public static event Action PeersUpdated;
        public static event Action PublicRoomsUpdated;
        public static event Action<string, string, string, string> RoomMessageReceived; // roomName, from, text, msgType
        public static event Action<string, string, int> TypingReceived; // from, host, port
        public static event Action<string> RingReceived;

        static NetworkEngine()
        {
            LocalIpAddress = DetectLocalIp();
            GlobalCloudStatus = "Idle";
            try { ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls | (SecurityProtocolType)768 | (SecurityProtocolType)3072; }
            catch { }
        }

        public static string DetectLocalIp()
        {
            try
            {
                string hostName = Dns.GetHostName();
                IPAddress[] addrs = Dns.GetHostAddresses(hostName);
                foreach (IPAddress ip in addrs)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "127.0.0.1";
        }

        public static string GetMyInviteCode()
        {
            Account me = Store.GetActiveAccount();
            string sn = me != null ? me.ScreenName : "AIMUser";
            int port = ActiveP2pPort > 0 ? ActiveP2pPort : Store.State.Settings.P2pListenPort;
            return string.Format("{0}@{1}:{2}", sn, LocalIpAddress, port);
        }

        public static bool ParseInviteCode(string raw, out string screenName, out string host, out int port)
        {
            screenName = "";
            host = "";
            port = 5192;
            string s = (raw ?? "").Trim();
            if (string.IsNullOrEmpty(s)) return false;

            int at = s.IndexOf('@');
            if (at > 0)
            {
                screenName = s.Substring(0, at).Trim();
                s = s.Substring(at + 1).Trim();
            }
            int colon = s.IndexOf(':');
            if (colon > 0)
            {
                host = s.Substring(0, colon).Trim();
                int parsedPort;
                if (int.TryParse(s.Substring(colon + 1).Trim(), out parsedPort) && parsedPort > 0 && parsedPort < 65535)
                    port = parsedPort;
            }
            else if (s.Contains(".") || string.Equals(s, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                host = s;
            }
            else if (string.IsNullOrEmpty(screenName))
            {
                screenName = s;
            }
            return !string.IsNullOrEmpty(screenName) || !string.IsNullOrEmpty(host);
        }

        public static void Start()
        {
            lock (_sync)
            {
                if (_running) return;
                _running = true;
                LastNetworkError = "";
                LocalIpAddress = DetectLocalIp();

                int basePort = Store.State.Settings.P2pListenPort > 0 ? Store.State.Settings.P2pListenPort : 5192;
                for (int p = basePort; p <= basePort + 20; p++)
                {
                    try
                    {
                        TcpListener listener = new TcpListener(IPAddress.Any, p);
                        listener.Start();
                        _p2pListener = listener;
                        ActiveP2pPort = p;
                        break;
                    }
                    catch (Exception ex)
                    {
                        LastNetworkError = ex.Message;
                    }
                }

                if (_p2pListener != null)
                {
                    _p2pAcceptThread = new Thread(P2pAcceptLoop) { IsBackground = true };
                    _p2pAcceptThread.Start();
                }

                TryStartUdpListener();

                _beaconThread = new Thread(BeaconAndSyncLoop) { IsBackground = true };
                _beaconThread.Start();
            }
        }

        public static void Stop()
        {
            BroadcastGoodbye();
            lock (_sync)
            {
                _running = false;
                try { if (_p2pListener != null) _p2pListener.Stop(); } catch { }
                _p2pListener = null;
                try { if (_udpReceiver != null) _udpReceiver.Close(); } catch { }
                _udpReceiver = null;
            }
        }

        private static void TryStartUdpListener()
        {
            try
            {
                UdpClient udp = new UdpClient();
                udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udp.ExclusiveAddressUse = false;
                udp.Client.Bind(new IPEndPoint(IPAddress.Any, UDP_DISCOVERY_PORT));
                udp.EnableBroadcast = true;
                _udpReceiver = udp;
                _udpListenThread = new Thread(UdpListenLoop) { IsBackground = true };
                _udpListenThread.Start();
            }
            catch { }
        }

        private static void UdpListenLoop()
        {
            while (_running && _udpReceiver != null)
            {
                try
                {
                    IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = _udpReceiver.Receive(ref remote);
                    string msg = Encoding.UTF8.GetString(data);
                    HandleUdpBeacon(msg, remote.Address.ToString());
                }
                catch
                {
                    if (!_running) break;
                    Thread.Sleep(250);
                }
            }
        }

        private static void HandleUdpBeacon(string msg, string senderIp)
        {
            if (string.IsNullOrEmpty(msg) || !msg.StartsWith("AIM2K6|")) return;
            string[] parts = msg.Split(new[] { '|' }, 7);
            if (parts.Length < 4) return;
            string verb = parts[1];
            string sn = parts[2];

            Account self = Store.GetActiveAccount();
            if (verb == "PUB" && parts.Length >= 5)
            {
                string rName = parts[3];
                string cat = parts[4];
                string topic = parts.Length > 5 ? parts[5] : "";
                RegisterDiscoveredPublicRoom(rName, topic, cat, sn);
                return;
            }
            if (verb == "RMSG" && parts.Length >= 6)
            {
                if (self != null && string.Equals(sn, self.ScreenName, StringComparison.OrdinalIgnoreCase)) return;
                string rName = parts[3];
                string mType = parts[4];
                string rText = parts[5];
                Action<string, string, string, string> rev = RoomMessageReceived;
                if (rev != null) rev(rName, sn, rText, mType);
                return;
            }

            int port;
            if (!int.TryParse(parts[3], out port)) port = 5192;
            string status = parts.Length > 4 ? parts[4] : "online";
            string avatar = parts.Length > 5 ? parts[5] : "AIM Icon.gif";
            string bio = parts.Length > 6 ? parts[6] : "";

            Account me = Store.GetActiveAccount();
            if (me != null && string.Equals(sn, me.ScreenName, StringComparison.OrdinalIgnoreCase) && port == ActiveP2pPort)
                return;

            if (verb == "BYE")
            {
                lock (DiscoveredPeers)
                {
                    DiscoveredPeer old;
                    if (DiscoveredPeers.TryGetValue(sn, out old))
                        old.Status = "offline";
                }
                UpdateRealBuddyStatus(sn, senderIp, port, "offline", avatar, bio);
                NotifyPeersUpdated();
                return;
            }

            if (status == "invisible") status = "offline";
            DiscoveredPeer peer = new DiscoveredPeer
            {
                ScreenName = sn,
                Host = senderIp,
                Port = port,
                Status = status,
                Avatar = avatar,
                Bio = bio,
                Source = "LAN / P2P",
                LastSeen = DateTime.UtcNow
            };
            lock (DiscoveredPeers)
            {
                DiscoveredPeers[sn] = peer;
            }
            UpdateRealBuddyStatus(sn, senderIp, port, status, avatar, bio);
            NotifyPeersUpdated();
        }

        private static void BeaconAndSyncLoop()
        {
            int cycle = 0;
            while (_running)
            {
                try
                {
                    if (Store.State.LoggedIn && Store.State.Settings.AutoDiscoverLan && ActiveP2pPort > 0)
                    {
                        SendUdpBeacon("HELLO");
                    }

                    // Relay Server Sync (Register + Poll Inbox + Refresh Online List)
                    if (Store.State.LoggedIn && !string.IsNullOrWhiteSpace(Store.State.Settings.RelayServerHost))
                    {
                        SyncWithRelayServer();
                    }

                    // Global Internet Directory & Cloud PMs (ntfy.sh pub-sub)
                    if (Store.State.LoggedIn && Store.State.Settings.GlobalCloudEnabled)
                    {
                        if (cycle % 5 == 0) PublishGlobalHello();
                        if (cycle % 2 == 0) PollGlobalCloud();
                    }

                    // Mark peers offline if unseen > 35s
                    if (cycle % 2 == 0)
                    {
                        DateTime cutoff = DateTime.UtcNow.AddSeconds(-35);
                        List<string> stale = new List<string>();
                        lock (DiscoveredPeers)
                        {
                            foreach (KeyValuePair<string, DiscoveredPeer> kv in DiscoveredPeers)
                            {
                                if (kv.Value.LastSeen < cutoff && kv.Value.Status != "offline")
                                {
                                    kv.Value.Status = "offline";
                                    stale.Add(kv.Key);
                                }
                            }
                        }
                        foreach (string s in stale) UpdateRealBuddyStatus(s, null, 0, "offline", null, null);
                        if (stale.Count > 0) NotifyPeersUpdated();
                    }
                    cycle++;
                }
                catch { }
                Thread.Sleep(3000);
            }
        }

        public static void BroadcastStatusChange()
        {
            if (!_running) return;
            ThreadPool.QueueUserWorkItem(delegate
            {
                SendUdpBeacon("HELLO");
                if (!string.IsNullOrWhiteSpace(Store.State.Settings.RelayServerHost))
                    SyncWithRelayServer();
                if (Store.State.Settings.GlobalCloudEnabled)
                    PublishGlobalHello();
            });
        }

        public static void BroadcastGoodbye()
        {
            try { SendUdpBeacon("BYE"); } catch { }
            try
            {
                if (Store.State.Settings.GlobalCloudEnabled)
                    PublishGlobalGoodbye();
            }
            catch { }
        }

        public static void SyncGlobalCloudNow()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                if (Store.State.LoggedIn && Store.State.Settings.GlobalCloudEnabled)
                {
                    PublishGlobalHello();
                    PollGlobalCloud();
                }
            });
        }

        private static void SendUdpBeacon(string verb)
        {
            if (ActiveP2pPort <= 0) return;
            Account me = Store.GetActiveAccount();
            string sn = me != null ? me.ScreenName : "AIMUser";
            string st = Store.State.CurrentUser != null ? (Store.State.CurrentUser.Status ?? "online") : "online";
            string av = me != null ? (me.Avatar ?? "AIM Icon.gif") : "AIM Icon.gif";
            string bio = (me != null ? (me.Bio ?? "") : "").Replace("|", "/").Replace("\r", " ").Replace("\n", " ");
            if (bio.Length > 90) bio = bio.Substring(0, 90);

            string payload = string.Format("AIM2K6|{0}|{1}|{2}|{3}|{4}|{5}", verb, sn.Replace("|", ""), ActiveP2pPort, st, av.Replace("|", ""), bio);
            byte[] bytes = Encoding.UTF8.GetBytes(payload);

            using (UdpClient uc = new UdpClient())
            {
                uc.EnableBroadcast = true;
                try { uc.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, UDP_DISCOVERY_PORT)); } catch { }
                try { uc.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, UDP_DISCOVERY_PORT)); } catch { }
            }
        }

        private static void P2pAcceptLoop()
        {
            while (_running && _p2pListener != null)
            {
                try
                {
                    TcpClient client = _p2pListener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(delegate { HandleIncomingTcpClient(client); });
                }
                catch
                {
                    if (!_running) break;
                    Thread.Sleep(150);
                }
            }
        }

        private static void HandleIncomingTcpClient(TcpClient client)
        {
            using (client)
            {
                try
                {
                    client.ReceiveTimeout = 6000;
                    client.SendTimeout = 6000;
                    string remoteIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                    using (NetworkStream ns = client.GetStream())
                    using (StreamReader sr = new StreamReader(ns, Encoding.UTF8))
                    using (StreamWriter sw = new StreamWriter(ns, new UTF8Encoding(false)) { AutoFlush = true })
                    {
                        string line = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) return;
                        NetPacket pkt = _json.Deserialize<NetPacket>(line);
                        if (pkt == null) return;

                        NetPacket resp = ProcessPacket(pkt, remoteIp);
                        if (resp != null)
                        {
                            sw.WriteLine(_json.Serialize(resp));
                        }
                    }
                }
                catch { }
            }
        }

        private static NetPacket ProcessPacket(NetPacket pkt, string remoteIp)
        {
            Account me = Store.GetActiveAccount();
            string mySN = me != null ? me.ScreenName : "AIMUser";
            bool fromGlobal = string.Equals(remoteIp, "Global Cloud", StringComparison.OrdinalIgnoreCase);
            string host = !string.IsNullOrEmpty(pkt.Host) ? pkt.Host : (fromGlobal ? "" : remoteIp);
            int port = !string.IsNullOrEmpty(host) ? (pkt.Port > 0 ? pkt.Port : 5192) : 0;

            if (!string.IsNullOrEmpty(pkt.From))
            {
                if (pkt.Type == "GOODBYE")
                {
                    lock (DiscoveredPeers)
                    {
                        DiscoveredPeer gone;
                        if (DiscoveredPeers.TryGetValue(pkt.From, out gone)) gone.Status = "offline";
                    }
                    UpdateRealBuddyStatus(pkt.From, host, port, "offline", pkt.Avatar, pkt.Bio);
                    NotifyPeersUpdated();
                    return new NetPacket { Type = "ACK", Ok = true };
                }

                string nextStatus = string.IsNullOrEmpty(pkt.Status) ? "online" : pkt.Status;
                if (nextStatus == "invisible") nextStatus = "offline";
                DiscoveredPeer dp = new DiscoveredPeer
                {
                    ScreenName = pkt.From,
                    Host = host,
                    Port = port,
                    Status = nextStatus,
                    Avatar = string.IsNullOrEmpty(pkt.Avatar) ? "AIM Icon.gif" : pkt.Avatar,
                    Bio = pkt.Bio ?? "",
                    Source = fromGlobal ? "Global Cloud" : "Direct / P2P",
                    LastSeen = DateTime.UtcNow
                };
                lock (DiscoveredPeers) { DiscoveredPeers[pkt.From] = dp; }
                UpdateRealBuddyStatus(pkt.From, host, port, dp.Status, dp.Avatar, dp.Bio);
                if (pkt.Type == "HELLO") NotifyPeersUpdated();
            }

            if (pkt.Type == "HELLO")
            {
                return new NetPacket
                {
                    Type = "HELLO_ACK",
                    Ok = true,
                    From = mySN,
                    Port = ActiveP2pPort,
                    Status = Store.State.CurrentUser.Status ?? "online",
                    Avatar = me != null ? me.Avatar : "AIM Icon.gif",
                    Bio = me != null ? me.Bio : ""
                };
            }
            if (pkt.Type == "IM")
            {
                AimApp.MarshalToUi(delegate
                {
                    if (Store.GetBuddy(pkt.From) == null)
                    {
                        Store.AddRealFriend(pkt.From, host, port, "online", pkt.Avatar, pkt.Bio, null);
                    }
                    AimApp.ReceiveIncomingIM(pkt.From, pkt.Text ?? "");
                });
                return new NetPacket { Type = "ACK", Ok = true };
            }
            if (pkt.Type == "TYPING")
            {
                Action<string, string, int> ev = TypingReceived;
                if (ev != null) ev(pkt.From, host, port);
                return new NetPacket { Type = "ACK", Ok = true };
            }
            if (pkt.Type == "RING")
            {
                Action<string> ev = RingReceived;
                if (ev != null) ev(pkt.From);
                AimApp.MarshalToUi(delegate
                {
                    SoundEngine.Play("ring");
                    Store.AppendChatMessage(pkt.From, new ChatMessage
                    {
                        Sender = "SYSTEM",
                        Text = string.Format("*** {0} sent you a real Audio Alert Ring!", pkt.From)
                    });
                    PrivateChatForm f;
                    if (AimApp.ImWindows.TryGetValue(pkt.From, out f) && !f.IsDisposed) f.ReloadTranscript();
                    else AimApp.OpenPrivateChat(pkt.From);
                });
                return new NetPacket { Type = "ACK", Ok = true };
            }
            if (pkt.Type == "FRIEND_REQ")
            {
                AimApp.MarshalToUi(delegate
                {
                    SoundEngine.Play("buddyDing");
                    Store.AddRealFriend(pkt.From, host, port, "online", pkt.Avatar, pkt.Bio, null);
                    AimApp.ShowToast(
                        I18n.T("New Real Friend Added!", "Новый реальный друг!"),
                        string.Format(I18n.T("{0} added you on AIM Network", "{0} добавил(а) вас в сети AIM"), pkt.From),
                        pkt.Avatar ?? "AIM Icon.gif",
                        delegate { AimApp.OpenPrivateChat(pkt.From); });
                });
                return new NetPacket { Type = "FRIEND_ACC", Ok = true, From = mySN, Avatar = me != null ? me.Avatar : "AIM Icon.gif" };
            }
            if (pkt.Type == "MAIL")
            {
                AimApp.MarshalToUi(delegate
                {
                    SoundEngine.Play("youveGotMail");
                    MailItem item = new MailItem
                    {
                        Id = Guid.NewGuid().ToString("N").Substring(0, 10),
                        From = (pkt.From ?? "AIMUser") + "@aim.com",
                        To = (mySN ?? "me") + "@aim.com",
                        Subject = string.IsNullOrEmpty(pkt.Font) ? "AIM Mail Message" : pkt.Font,
                        Body = pkt.Text ?? "",
                        Date = Store.NowDateStr() + " " + Store.NowTimeStr(),
                        Read = false
                    };
                    Store.State.MailItems.Insert(0, item);
                    Store.Notify("mail");
                    AimApp.ShowToast(
                        I18n.T("You've got mail!", "Вам пришло письмо!"),
                        string.Format("{0}: {1}", item.From, item.Subject),
                        pkt.Avatar ?? "AIM Icon.gif",
                        delegate { AimApp.OpenMailInbox(); });
                });
                return new NetPacket { Type = "ACK", Ok = true };
            }
            return new NetPacket { Type = "ACK", Ok = true };
        }

        private static void UpdateRealBuddyStatus(string sn, string host, int port, string status, string avatar, string bio)
        {
            Buddy b = Store.GetBuddy(sn);
            if (b == null || !b.IsRealUser) return;
            bool changed = false;
            string prevStatus = b.Status;
            if (!string.IsNullOrEmpty(host) && b.PeerHost != host) { b.PeerHost = host; changed = true; }
            if (port > 0 && b.PeerPort != port) { b.PeerPort = port; changed = true; }
            if (!string.IsNullOrEmpty(status) && b.Status != status) { b.Status = status; changed = true; }
            if (!string.IsNullOrEmpty(avatar) && b.Avatar != avatar) { b.Avatar = avatar; changed = true; }
            if (!string.IsNullOrEmpty(bio) && b.Bio != bio) { b.Bio = bio; changed = true; }
            if (changed)
            {
                AimApp.MarshalToUi(delegate
                {
                    if (prevStatus == "offline" && b.Status == "online") SoundEngine.Play("doorOpen");
                    else if (prevStatus != "offline" && b.Status == "offline") SoundEngine.Play("doorClose");
                    Store.Notify("buddies");
                });
            }
        }

        public static bool ProbePeer(string host, int port, out NetPacket ack, out string error)
        {
            ack = null;
            error = "";
            Account me = Store.GetActiveAccount();
            NetPacket req = new NetPacket
            {
                Type = "HELLO",
                From = me != null ? me.ScreenName : "AIMUser",
                Host = LocalIpAddress,
                Port = ActiveP2pPort,
                Status = Store.State.CurrentUser.Status ?? "online",
                Avatar = me != null ? me.Avatar : "AIM Icon.gif",
                Bio = me != null ? me.Bio : ""
            };
            ack = SendTcpPacket(host, port, req, out error);
            return ack != null && ack.Ok;
        }

        public static bool SendRealIm(Buddy b, ChatMessage sent, out string error)
        {
            error = "";
            Account me = Store.GetActiveAccount();
            NetPacket pkt = new NetPacket
            {
                Type = "IM",
                From = me != null ? me.ScreenName : "AIMUser",
                To = b.ScreenName,
                Host = LocalIpAddress,
                Port = ActiveP2pPort,
                Avatar = me != null ? me.Avatar : "AIM Icon.gif",
                Bio = me != null ? me.Bio : "",
                Text = sent.Text,
                Font = sent.Font,
                Color = sent.Color,
                Bold = sent.Bold,
                Italic = sent.Italic,
                Underline = sent.Underline
            };
            return DeliverToBuddyOrRelay(b, pkt, out error);
        }

        public static void SendFriendRequest(Buddy b)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                string err;
                Account me = Store.GetActiveAccount();
                DeliverToBuddyOrRelay(b, new NetPacket
                {
                    Type = "FRIEND_REQ",
                    From = me != null ? me.ScreenName : "AIMUser",
                    To = b.ScreenName,
                    Host = LocalIpAddress,
                    Port = ActiveP2pPort,
                    Avatar = me != null ? me.Avatar : "AIM Icon.gif",
                    Bio = me != null ? me.Bio : ""
                }, out err);
            });
        }

        public static void SendTypingNotification(Buddy b)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                string err;
                DeliverToBuddyOrRelay(b, new NetPacket
                {
                    Type = "TYPING",
                    From = Store.GetActiveAccount().ScreenName,
                    To = b.ScreenName,
                    Host = LocalIpAddress,
                    Port = ActiveP2pPort
                }, out err);
            });
        }

        public static void SendRingNotification(Buddy b)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                string err;
                DeliverToBuddyOrRelay(b, new NetPacket
                {
                    Type = "RING",
                    From = Store.GetActiveAccount().ScreenName,
                    To = b.ScreenName,
                    Host = LocalIpAddress,
                    Port = ActiveP2pPort
                }, out err);
            });
        }

        private static bool DeliverToBuddyOrRelay(Buddy b, NetPacket pkt, out string error)
        {
            error = "";
            string targetHost = b.PeerHost;
            int targetPort = b.PeerPort;

            DiscoveredPeer cached;
            lock (DiscoveredPeers)
            {
                if (DiscoveredPeers.TryGetValue(b.ScreenName, out cached) && !string.IsNullOrEmpty(cached.Host))
                {
                    if (string.IsNullOrEmpty(targetHost) || cached.Source == "LAN / P2P")
                    {
                        targetHost = cached.Host;
                        targetPort = cached.Port > 0 ? cached.Port : targetPort;
                    }
                }
            }

            if (!string.IsNullOrEmpty(targetHost) && targetPort > 0)
            {
                NetPacket ack = SendTcpPacket(targetHost, targetPort, pkt, out error);
                if (ack != null) return true;
            }

            if (!string.IsNullOrWhiteSpace(Store.State.Settings.RelayServerHost))
            {
                NetPacket relayPkt = new NetPacket
                {
                    Type = "RELAY_ROUTE",
                    From = pkt.From,
                    To = b.ScreenName,
                    Text = _json.Serialize(pkt)
                };
                NetPacket rAck = SendTcpPacket(Store.State.Settings.RelayServerHost.Trim(), Store.State.Settings.RelayServerPort, relayPkt, out error);
                if (rAck != null && rAck.Ok) return true;
            }

            if (Store.State.Settings.GlobalCloudEnabled)
            {
                string globalErr;
                if (PublishGlobalPm(b.ScreenName, pkt, out globalErr)) return true;
                error = globalErr;
            }

            if (string.IsNullOrEmpty(error))
                error = "Friend unreachable (Enable Global Cloud Directory or set P2P IP / Relay Server).";
            return false;
        }

        private static NetPacket SendTcpPacket(string host, int port, NetPacket pkt, out string error)
        {
            error = "";
            try
            {
                using (TcpClient cli = new TcpClient())
                {
                    IAsyncResult ar = cli.BeginConnect(host, port, null, null);
                    if (!ar.AsyncWaitHandle.WaitOne(3500))
                    {
                        error = "Connection timed out (" + host + ":" + port + ")";
                        return null;
                    }
                    cli.EndConnect(ar);
                    cli.ReceiveTimeout = 4500;
                    cli.SendTimeout = 4500;

                    using (NetworkStream ns = cli.GetStream())
                    using (StreamWriter sw = new StreamWriter(ns, new UTF8Encoding(false)) { AutoFlush = true })
                    using (StreamReader sr = new StreamReader(ns, Encoding.UTF8))
                    {
                        sw.WriteLine(_json.Serialize(pkt));
                        string line = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) return new NetPacket { Ok = true };
                        return _json.Deserialize<NetPacket>(line);
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        /* ==================================================================
           Built-in Optional Shared AIM Relay Server (for Internet friends)
           ================================================================== */
        public static bool StartRelayServer(int port, out string error)
        {
            error = "";
            if (_relayHosting) return true;
            try
            {
                TcpListener tl = new TcpListener(IPAddress.Any, port);
                tl.Start();
                _relayListener = tl;
                _relayHosting = true;
                ActiveRelayHostPort = port;
                _relayThread = new Thread(RelayAcceptLoop) { IsBackground = true };
                _relayThread.Start();
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static void StopRelayServer()
        {
            _relayHosting = false;
            try { if (_relayListener != null) _relayListener.Stop(); } catch { }
            _relayListener = null;
        }

        private static void RelayAcceptLoop()
        {
            while (_relayHosting && _relayListener != null)
            {
                try
                {
                    TcpClient c = _relayListener.AcceptTcpClient();
                    ThreadPool.QueueUserWorkItem(delegate { HandleRelayClient(c); });
                }
                catch
                {
                    if (!_relayHosting) break;
                    Thread.Sleep(150);
                }
            }
        }

        private static void HandleRelayClient(TcpClient cli)
        {
            using (cli)
            {
                try
                {
                    string remoteIp = ((IPEndPoint)cli.Client.RemoteEndPoint).Address.ToString();
                    using (NetworkStream ns = cli.GetStream())
                    using (StreamReader sr = new StreamReader(ns, Encoding.UTF8))
                    using (StreamWriter sw = new StreamWriter(ns, new UTF8Encoding(false)) { AutoFlush = true })
                    {
                        string line = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) return;
                        NetPacket pkt = _json.Deserialize<NetPacket>(line);
                        if (pkt == null) return;

                        if (pkt.Type == "RELAY_REG" && !string.IsNullOrEmpty(pkt.From))
                        {
                            List<NetPacket> pending = null;
                            List<DiscoveredPeer> peersCopy = new List<DiscoveredPeer>();
                            lock (_relayDirectory)
                            {
                                _relayDirectory[pkt.From] = new DiscoveredPeer
                                {
                                    ScreenName = pkt.From,
                                    Host = remoteIp,
                                    Port = pkt.Port,
                                    Status = pkt.Status ?? "online",
                                    Avatar = pkt.Avatar ?? "AIM Icon.gif",
                                    Bio = pkt.Bio ?? "",
                                    Source = "AIM Relay",
                                    LastSeen = DateTime.UtcNow
                                };
                                DateTime alive = DateTime.UtcNow.AddSeconds(-30);
                                foreach (DiscoveredPeer p in _relayDirectory.Values)
                                {
                                    if (p.LastSeen >= alive) peersCopy.Add(p);
                                }
                                List<NetPacket> box;
                                if (_relayInbox.TryGetValue(pkt.From, out box) && box.Count > 0)
                                {
                                    pending = new List<NetPacket>(box);
                                    box.Clear();
                                }
                            }
                            string packedPending = pending != null ? _json.Serialize(pending) : "";
                            sw.WriteLine(_json.Serialize(new NetPacket { Type = "RELAY_SYNC", Ok = true, Peers = peersCopy, Text = packedPending }));
                            return;
                        }

                        if (pkt.Type == "RELAY_ROUTE" && !string.IsNullOrEmpty(pkt.To) && !string.IsNullOrEmpty(pkt.Text))
                        {
                            NetPacket inner = _json.Deserialize<NetPacket>(pkt.Text);
                            if (inner != null)
                            {
                                lock (_relayDirectory)
                                {
                                    List<NetPacket> box;
                                    if (!_relayInbox.TryGetValue(pkt.To, out box))
                                    {
                                        box = new List<NetPacket>();
                                        _relayInbox[pkt.To] = box;
                                    }
                                    box.Add(inner);
                                }
                                sw.WriteLine(_json.Serialize(new NetPacket { Type = "ACK", Ok = true }));
                                return;
                            }
                        }
                        sw.WriteLine(_json.Serialize(new NetPacket { Type = "ACK", Ok = false }));
                    }
                }
                catch { }
            }
        }

        private static void SyncWithRelayServer()
        {
            string host = (Store.State.Settings.RelayServerHost ?? "").Trim();
            int port = Store.State.Settings.RelayServerPort > 0 ? Store.State.Settings.RelayServerPort : 5190;
            if (string.IsNullOrEmpty(host)) return;

            Account me = Store.GetActiveAccount();
            string mySN = me != null ? me.ScreenName : "AIMUser";
            string err;
            NetPacket resp = SendTcpPacket(host, port, new NetPacket
            {
                Type = "RELAY_REG",
                From = mySN,
                Port = ActiveP2pPort,
                Status = Store.State.CurrentUser.Status ?? "online",
                Avatar = me != null ? me.Avatar : "AIM Icon.gif",
                Bio = me != null ? me.Bio : ""
            }, out err);
            if (resp == null || !resp.Ok) return;

            if (resp.Peers != null)
            {
                lock (DiscoveredPeers)
                {
                    foreach (DiscoveredPeer p in resp.Peers)
                    {
                        if (string.Equals(p.ScreenName, mySN, StringComparison.OrdinalIgnoreCase)) continue;
                        p.Source = "AIM Relay";
                        p.LastSeen = DateTime.UtcNow;
                        DiscoveredPeers[p.ScreenName] = p;
                        UpdateRealBuddyStatus(p.ScreenName, p.Host, p.Port, p.Status, p.Avatar, p.Bio);
                    }
                }
                NotifyPeersUpdated();
            }

            if (!string.IsNullOrWhiteSpace(resp.Text))
            {
                try
                {
                    List<NetPacket> inbox = _json.Deserialize<List<NetPacket>>(resp.Text);
                    if (inbox != null)
                    {
                        foreach (NetPacket inner in inbox) ProcessPacket(inner, host);
                    }
                }
                catch { }
            }
        }

        private static void NotifyPeersUpdated()
        {
            Action ev = PeersUpdated;
            if (ev != null) ev();
        }

        /* ==================================================================
           Global Cloud Friends Directory & Messaging (ntfy.sh HTTPS pub-sub)
           ================================================================== */
        private static string Sha256Hex16(string input)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? ""));
                StringBuilder sb = new StringBuilder(16);
                for (int i = 0; i < 8 && i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

        private static string NormalizeRoomCode(string room)
        {
            string r = (room ?? "").Trim().ToLowerInvariant();
            return string.IsNullOrEmpty(r) ? "aim-global-2006" : r;
        }

        public static string GetGlobalDirTopic(string room)
        {
            return "aimc_v59_dir_" + Sha256Hex16(NormalizeRoomCode(room));
        }

        public static string GetGlobalPmTopic(string screenName, string room)
        {
            string key = (screenName ?? "").Trim().ToLowerInvariant() + ":" + NormalizeRoomCode(room);
            return "aimc_v59_pm_" + Sha256Hex16(key);
        }

        private static void PublishGlobalHello()
        {
            Account me = Store.GetActiveAccount();
            if (me == null) return;
            string room = Store.State.Settings.GlobalCloudRoom;
            NetPacket pkt = new NetPacket
            {
                Type = "HELLO",
                From = me.ScreenName,
                Status = Store.State.CurrentUser != null ? (Store.State.CurrentUser.Status ?? "online") : "online",
                Avatar = me.Avatar ?? "AIM Icon.gif",
                AwayText = Store.State.CurrentUser != null ? (Store.State.CurrentUser.CustomStatusNote ?? "") : "",
                Bio = me.Bio ?? ""
            };
            string err;
            if (PublishNtfy(GetGlobalDirTopic(room), _json.Serialize(pkt), out err))
            {
                GlobalCloudStatus = "Connected (" + NormalizeRoomCode(room) + ")";
                LastGlobalError = "";
            }
            else
            {
                GlobalCloudStatus = "Offline (" + err + ")";
                LastGlobalError = err;
            }
        }

        private static void PublishGlobalGoodbye()
        {
            Account me = Store.GetActiveAccount();
            if (me == null) return;
            string room = Store.State.Settings.GlobalCloudRoom;
            NetPacket pkt = new NetPacket
            {
                Type = "GOODBYE",
                From = me.ScreenName,
                Status = "offline",
                Avatar = me.Avatar ?? "AIM Icon.gif"
            };
            string err;
            PublishNtfy(GetGlobalDirTopic(room), _json.Serialize(pkt), out err);
        }

        private static bool PublishGlobalPm(string targetScreenName, NetPacket pkt, out string error)
        {
            string room = Store.State.Settings.GlobalCloudRoom;
            // Clear local LAN IP from cloud relay packet so remote peers route replies back through Cloud/nick unless IP is public
            NetPacket cloudPkt = new NetPacket
            {
                Type = pkt.Type,
                From = pkt.From,
                To = targetScreenName,
                Status = Store.State.CurrentUser != null ? (Store.State.CurrentUser.Status ?? "online") : "online",
                Avatar = pkt.Avatar,
                Bio = pkt.Bio,
                Text = pkt.Text,
                Font = pkt.Font,
                Color = pkt.Color,
                Bold = pkt.Bold,
                Italic = pkt.Italic,
                Underline = pkt.Underline
            };
            return PublishNtfy(GetGlobalPmTopic(targetScreenName, room), _json.Serialize(cloudPkt), out error);
        }

        public static bool SendGlobalMail(string toScreenName, string subject, string body, out string error)
        {
            Account me = Store.GetActiveAccount();
            NetPacket pkt = new NetPacket
            {
                Type = "MAIL",
                From = me != null ? me.ScreenName : "AIMUser",
                To = toScreenName,
                Avatar = me != null ? me.Avatar : "AIM Icon.gif",
                Font = subject,
                Text = body
            };
            return PublishGlobalPm(toScreenName, pkt, out error);
        }

        private static void PollGlobalCloud()
        {
            Account me = Store.GetActiveAccount();
            if (me == null) return;
            string room = Store.State.Settings.GlobalCloudRoom;

            List<string> dirMsgs;
            string dirErr;
            if (PollNtfy(GetGlobalDirTopic(room), ref _lastDirPollUnix, "10m", out dirMsgs, out dirErr))
            {
                GlobalCloudStatus = "Connected (" + NormalizeRoomCode(room) + ")";
                LastGlobalError = "";
                foreach (string raw in dirMsgs)
                {
                    try
                    {
                        NetPacket p = _json.Deserialize<NetPacket>(raw);
                        if (p == null || string.IsNullOrEmpty(p.From)) continue;
                        if (string.Equals(p.From, me.ScreenName, StringComparison.OrdinalIgnoreCase)) continue;
                        ProcessPacket(p, "Global Cloud");
                    }
                    catch { }
                }
            }
            else if (!string.IsNullOrEmpty(dirErr))
            {
                GlobalCloudStatus = "Offline (" + dirErr + ")";
                LastGlobalError = dirErr;
            }

            List<string> pmMsgs;
            string pmErr;
            if (PollNtfy(GetGlobalPmTopic(me.ScreenName, room), ref _lastPmPollUnix, "15m", out pmMsgs, out pmErr))
            {
                foreach (string raw in pmMsgs)
                {
                    try
                    {
                        NetPacket p = _json.Deserialize<NetPacket>(raw);
                        if (p == null || string.IsNullOrEmpty(p.From)) continue;
                        if (string.Equals(p.From, me.ScreenName, StringComparison.OrdinalIgnoreCase)) continue;
                        ProcessPacket(p, "Global Cloud");
                    }
                    catch { }
                }
            }
        }

        private static bool PublishNtfy(string topic, string messageBody, out string error)
        {
            error = "";
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://ntfy.sh/" + Uri.EscapeDataString(topic));
                req.Method = "POST";
                req.Timeout = 4500;
                req.ReadWriteTimeout = 4500;
                req.ContentType = "text/plain; charset=utf-8";
                req.ServicePoint.Expect100Continue = false;
                byte[] bytes = Encoding.UTF8.GetBytes(messageBody);
                req.ContentLength = bytes.Length;
                using (Stream rs = req.GetRequestStream()) rs.Write(bytes, 0, bytes.Length);
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    return (int)resp.StatusCode >= 200 && (int)resp.StatusCode < 300;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool PollNtfy(string topic, ref long sinceUnix, string defaultWindow, out List<string> messages, out string error)
        {
            messages = new List<string>();
            error = "";
            try
            {
                string sinceParam = sinceUnix > 0 ? sinceUnix.ToString() : defaultWindow;
                string url = "https://ntfy.sh/" + Uri.EscapeDataString(topic) + "/json?poll=1&since=" + sinceParam;
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.Timeout = 4500;
                req.ReadWriteTimeout = 4500;
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        Dictionary<string, object> obj = _json.Deserialize<Dictionary<string, object>>(line);
                        if (obj == null) continue;
                        object evObj;
                        if (!obj.TryGetValue("event", out evObj) || !string.Equals(Convert.ToString(evObj), "message", StringComparison.OrdinalIgnoreCase))
                            continue;
                        string id = obj.ContainsKey("id") ? Convert.ToString(obj["id"]) : "";
                        lock (_seenNtfyIds)
                        {
                            if (!string.IsNullOrEmpty(id))
                            {
                                if (_seenNtfyIds.Contains(id)) continue;
                                if (_seenNtfyIds.Count > 2000) _seenNtfyIds.Clear();
                                _seenNtfyIds.Add(id);
                            }
                        }
                        if (obj.ContainsKey("time"))
                        {
                            long t;
                            if (long.TryParse(Convert.ToString(obj["time"]), out t) && t > sinceUnix)
                                sinceUnix = t;
                        }
                        if (obj.ContainsKey("message") && obj["message"] != null)
                        {
                            messages.Add(Convert.ToString(obj["message"]));
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /* ==================================================================
           Global Public Groups Directory & Real-Time Public Room Chat
           ================================================================== */
        private static long _lastPubDirPollUnix = 0;

        public static string GetGlobalPubGroupsTopic(string roomNamespace)
        {
            return "aimc_v60_pubdir_" + Sha256Hex16(NormalizeRoomCode(roomNamespace));
        }

        public static string GetGlobalPublicRoomTopic(string roomName, string roomNamespace)
        {
            string key = (roomName ?? "").Trim().ToLowerInvariant() + ":" + NormalizeRoomCode(roomNamespace);
            return "aimc_v60_room_" + Sha256Hex16(key);
        }

        private static void RegisterDiscoveredPublicRoom(string name, string topic, string category, string createdBy)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            lock (DiscoveredPublicRooms)
            {
                DiscoveredPublicRooms[name] = new ChatRoom
                {
                    Id = "pub_" + Sha256Hex16(name.ToLowerInvariant()),
                    Name = name.StartsWith("#") ? name : "#" + name,
                    Topic = string.IsNullOrEmpty(topic) ? "Global AIM Public Group" : topic,
                    Category = string.IsNullOrEmpty(category) ? "Global" : category,
                    CreatedBy = string.IsNullOrEmpty(createdBy) ? "AIM User" : createdBy,
                    IsPublic = true,
                    Participants = new List<string>(),
                    Messages = new List<ChatMessage>()
                };
            }
            Action ev = PublicRoomsUpdated;
            if (ev != null) ev();
        }

        private static void SendUdpRaw(string payload)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(payload);
                using (UdpClient uc = new UdpClient())
                {
                    uc.EnableBroadcast = true;
                    try { uc.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, UDP_DISCOVERY_PORT)); } catch { }
                    try { uc.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Loopback, UDP_DISCOVERY_PORT)); } catch { }
                }
            }
            catch { }
        }

        public static void PublishPublicRoom(ChatRoom room)
        {
            if (room == null || string.IsNullOrWhiteSpace(room.Name)) return;
            Account me = Store.GetActiveAccount();
            string meSn = me != null ? me.ScreenName : "AIMUser";
            string creator = string.IsNullOrEmpty(room.CreatedBy) ? meSn : room.CreatedBy;
            RegisterDiscoveredPublicRoom(room.Name, room.Topic, room.Category, creator);

            string cleanName = room.Name.Replace("|", "");
            string cleanCat = (room.Category ?? "Global").Replace("|", "");
            string cleanTopic = (room.Topic ?? "").Replace("|", "/").Replace("\r", " ").Replace("\n", " ");
            SendUdpRaw(string.Format("AIM2K6|PUB|{0}|{1}|{2}|{3}", creator.Replace("|", ""), cleanName, cleanCat, cleanTopic));

            if (Store.State.Settings.GlobalCloudEnabled)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    NetPacket pkt = new NetPacket
                    {
                        Type = "PUB_ROOM",
                        From = creator,
                        To = room.Name,
                        Font = room.Category ?? "Global",
                        Text = room.Topic ?? ""
                    };
                    string err;
                    PublishNtfy(GetGlobalPubGroupsTopic(Store.State.Settings.GlobalCloudRoom), _json.Serialize(pkt), out err);
                });
            }
        }

        public static void RefreshPublicRoomsGlobal()
        {
            if (!Store.State.Settings.GlobalCloudEnabled) return;
            ThreadPool.QueueUserWorkItem(delegate
            {
                List<string> msgs;
                string err;
                if (PollNtfy(GetGlobalPubGroupsTopic(Store.State.Settings.GlobalCloudRoom), ref _lastPubDirPollUnix, "12h", out msgs, out err))
                {
                    foreach (string raw in msgs)
                    {
                        try
                        {
                            NetPacket p = _json.Deserialize<NetPacket>(raw);
                            if (p != null && p.Type == "PUB_ROOM" && !string.IsNullOrWhiteSpace(p.To))
                            {
                                RegisterDiscoveredPublicRoom(p.To, p.Text, p.Font, p.From);
                            }
                        }
                        catch { }
                    }
                }
            });
        }

        public static void SendPublicRoomMessage(string roomName, string text, string msgType)
        {
            if (string.IsNullOrWhiteSpace(roomName)) return;
            Account me = Store.GetActiveAccount();
            string meSn = me != null ? me.ScreenName : "AIMUser";
            string mType = string.IsNullOrEmpty(msgType) ? "ROOM_MSG" : msgType;
            string safeTxt = (text ?? "").Replace("\r", " ").Replace("\n", " ");
            SendUdpRaw(string.Format("AIM2K6|RMSG|{0}|{1}|{2}|{3}", meSn.Replace("|", ""), roomName.Replace("|", ""), mType, safeTxt.Replace("|", "/")));

            if (Store.State.Settings.GlobalCloudEnabled)
            {
                ThreadPool.QueueUserWorkItem(delegate
                {
                    NetPacket pkt = new NetPacket
                    {
                        Type = mType,
                        From = meSn,
                        To = roomName,
                        Text = text ?? ""
                    };
                    string err;
                    PublishNtfy(GetGlobalPublicRoomTopic(roomName, Store.State.Settings.GlobalCloudRoom), _json.Serialize(pkt), out err);
                });
            }
        }

        public static void PollPublicRoom(string roomName, ref long sinceUnix)
        {
            if (string.IsNullOrWhiteSpace(roomName) || !Store.State.Settings.GlobalCloudEnabled) return;
            Account me = Store.GetActiveAccount();
            string meSn = me != null ? me.ScreenName : "AIMUser";
            List<string> msgs;
            string err;
            if (PollNtfy(GetGlobalPublicRoomTopic(roomName, Store.State.Settings.GlobalCloudRoom), ref sinceUnix, "15m", out msgs, out err))
            {
                foreach (string raw in msgs)
                {
                    try
                    {
                        NetPacket p = _json.Deserialize<NetPacket>(raw);
                        if (p == null || string.IsNullOrEmpty(p.From)) continue;
                        if (string.Equals(p.From, meSn, StringComparison.OrdinalIgnoreCase)) continue;
                        Action<string, string, string, string> rev = RoomMessageReceived;
                        if (rev != null) rev(roomName, p.From, p.Text ?? "", p.Type ?? "ROOM_MSG");
                    }
                    catch { }
                }
            }
        }
    }
}
