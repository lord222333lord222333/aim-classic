using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Reflection;

namespace AIMClassic
{
    public static class SoundEngine
    {
        private static readonly Dictionary<string, byte[]> _wavCache = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, SoundPlayer> _players = new Dictionary<string, SoundPlayer>(StringComparer.OrdinalIgnoreCase);

        public static readonly Dictionary<string, string> EventLabels = new Dictionary<string, string>
        {
            { "welcome", "Sign On — \"Welcome!\"" },
            { "goodbye", "Sign Off — \"Goodbye!\"" },
            { "youveGotMail", "AIM Mail — \"You've got mail!\"" },
            { "youveGotPictures", "Pictures — \"You've got pictures!\"" },
            { "youveGotVoicemail", "Voicemail — \"You've got voicemail!\"" },
            { "connectedChime", "Connected Arpeggio Chime" },
            { "buddyDing", "AIM 4-Note Xylophone Chime" },
            { "dialupModem", "56k Dial-Up Modem Handshake" },
            { "doorOpen", "Buddy Signs On (Door Open)" },
            { "doorClose", "Buddy Signs Off (Door Close)" },
            { "imNew", "New IM Window (First Message)" },
            { "imReceive", "Instant Message Received" },
            { "imSend", "Instant Message Sent" },
            { "ring", "Audio Ring / Incoming File Offer" },
            { "buddyAlert", "Buddy Alert / Warning" },
            { "chatJoin", "Group Chat Room Join" },
            { "chatLeave", "Group Chat Room Leave / Clear" },
            { "talkClick", "Status / UI Click" },
            { "fileDone", "File Transfer — \"File's done!\"" }
        };

        private static readonly Dictionary<string, string> _fileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "welcome", "welcome.wav" },
            { "goodbye", "goodbye.wav" },
            { "youveGotMail", "youve_got_mail.wav" },
            { "youveGotPictures", "youve_got_pictures.wav" },
            { "youveGotVoicemail", "youve_got_voicemail.wav" },
            { "connectedChime", "connected_chime.wav" },
            { "buddyDing", "buddy_ding.wav" },
            { "dialupModem", "dialup_modem.wav" },
            { "doorOpen", "door_open.wav" },
            { "doorClose", "door_close.wav" },
            { "imNew", "im_new.wav" },
            { "imReceive", "im_receive.wav" },
            { "imSend", "im_send.wav" },
            { "ring", "ring.wav" },
            { "buddyAlert", "buddy_alert.wav" },
            { "alert", "buddy_alert.wav" },
            { "chatJoin", "chat_join.wav" },
            { "chatLeave", "chat_leave.wav" },
            { "talkClick", "talk_click.wav" },
            { "fileDone", "files_done.wav" }
        };

        static SoundEngine()
        {
            LoadAllSounds();
        }

        private static void LoadAllSounds()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string diskDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "sounds");

            foreach (KeyValuePair<string, string> kv in _fileMap)
            {
                string key = kv.Key;
                string fileName = kv.Value;
                byte[] bytes = null;

                string resName = "AIMClassic.Sounds." + fileName;
                using (Stream s = asm.GetManifestResourceStream(resName))
                {
                    if (s != null)
                    {
                        bytes = ReadAllBytes(s);
                    }
                }
                if (bytes == null)
                {
                    string diskPath = Path.Combine(diskDir, fileName);
                    if (File.Exists(diskPath))
                    {
                        try { bytes = File.ReadAllBytes(diskPath); } catch { }
                    }
                }
                if (bytes == null)
                {
                    bytes = BuildFallbackBeep(key);
                }

                _wavCache[key] = bytes;
                try
                {
                    MemoryStream ms = new MemoryStream(bytes);
                    SoundPlayer sp = new SoundPlayer(ms);
                    sp.Load();
                    _players[key] = sp;
                }
                catch { }
            }
        }

        private static byte[] ReadAllBytes(Stream s)
        {
            byte[] buf = new byte[s.Length];
            int pos = 0;
            while (pos < buf.Length)
            {
                int r = s.Read(buf, pos, buf.Length - pos);
                if (r <= 0) break;
                pos += r;
            }
            return buf;
        }

        private static byte[] BuildFallbackBeep(string key)
        {
            const int sr = 22050;
            int n = sr / 8;
            double freq = key == "imSend" ? 660 : 880;
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                int dataBytes = n * 2;
                bw.Write(new[] { 'R', 'I', 'F', 'F' });
                bw.Write(36 + dataBytes);
                bw.Write(new[] { 'W', 'A', 'V', 'E' });
                bw.Write(new[] { 'f', 'm', 't', ' ' });
                bw.Write(16); bw.Write((short)1); bw.Write((short)1);
                bw.Write(sr); bw.Write(sr * 2); bw.Write((short)2); bw.Write((short)16);
                bw.Write(new[] { 'd', 'a', 't', 'a' });
                bw.Write(dataBytes);
                for (int i = 0; i < n; i++)
                {
                    double env = Math.Min(1.0, i / 120.0) * Math.Min(1.0, (n - i) / 180.0);
                    short sample = (short)(Math.Sin(2 * Math.PI * freq * i / sr) * 0.22 * env * 32767);
                    bw.Write(sample);
                }
                return ms.ToArray();
            }
        }

        public static bool IsEventEnabled(string eventKey)
        {
            AppSettings s = Store.State.Settings;
            if (s.SoundMuted) return false;
            if (Store.State.CurrentUser.Status == "busy" && s.MuteWhenBusy) return false;
            switch (eventKey)
            {
                case "welcome": return s.SoundWelcome;
                case "goodbye": return s.SoundGoodbye;
                case "youveGotMail": return s.SoundMail;
                case "doorOpen": return s.SoundDoorOpen;
                case "doorClose": return s.SoundDoorClose;
                case "imNew":
                case "imReceive": return s.SoundImReceive;
                case "imSend": return s.SoundImSend;
                case "ring": return s.SoundRing;
                case "chatJoin":
                case "chatLeave": return s.SoundChat;
                case "fileDone": return s.SoundFileDone;
                case "buddyAlert":
                case "alert": return s.SoundAlert;
                default: return true;
            }
        }

        public static void Play(string eventKey, bool force = false, bool synchronous = false)
        {
            try
            {
                if (!force && !IsEventEnabled(eventKey)) return;
                SoundPlayer sp;
                if (_players.TryGetValue(eventKey, out sp) && sp != null)
                {
                    if (synchronous) sp.PlaySync();
                    else sp.Play();
                }
            }
            catch { }
        }
    }
}
