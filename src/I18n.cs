using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace AIMClassic
{
    public static class I18n
    {
        private static readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        public static bool IsRussian
        {
            get
            {
                try { return string.Equals(Store.State.Settings.Language, "ru", StringComparison.OrdinalIgnoreCase); }
                catch { return false; }
            }
        }

        public static string T(string en, string ru)
        {
            return IsRussian ? ru : en;
        }

        public static string TranslateMessage(string text, string targetLang, out string method)
        {
            method = "Offline Dictionary";
            if (string.IsNullOrWhiteSpace(text)) return text;
            string tl = string.Equals(targetLang, "ru", StringComparison.OrdinalIgnoreCase) ? "ru" : "en";

            // 1. Try active AI API if user configured one
            if (AiEngine.IsApiConfigured())
            {
                string ai = AiEngine.TranslateWithApi(text, tl);
                if (!string.IsNullOrWhiteSpace(ai))
                {
                    method = "AI API";
                    return ai.Trim();
                }
            }

            // 2. Try free Google Translate public endpoint when online
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Tls;
                string url = string.Format(
                    "https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={0}&dt=t&q={1}",
                    tl, Uri.EscapeDataString(text));
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.Timeout = 3500;
                req.UserAgent = "Mozilla/5.0 (Windows NT 5.1) AIMClassic/5.9";
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string raw = sr.ReadToEnd();
                    object parsed = _json.DeserializeObject(raw);
                    object[] root = parsed as object[];
                    if (root != null && root.Length > 0)
                    {
                        object[] sentences = root[0] as object[];
                        if (sentences != null)
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (object item in sentences)
                            {
                                object[] seg = item as object[];
                                if (seg != null && seg.Length > 0 && seg[0] != null)
                                    sb.Append(seg[0].ToString());
                            }
                            string res = sb.ToString().Trim();
                            if (!string.IsNullOrEmpty(res))
                            {
                                method = "Online Translator";
                                return res;
                            }
                        }
                    }
                }
            }
            catch { }

            // 3. Built-in offline EN<->RU chat phrase/word fallback
            return OfflineTranslateFallback(text, tl);
        }

        private static string OfflineTranslateFallback(string text, string targetLang)
        {
            bool hasCyrillic = Regex.IsMatch(text, @"[\u0400-\u04FF]");
            if ((targetLang == "en" && !hasCyrillic) || (targetLang == "ru" && hasCyrillic))
            {
                return text;
            }

            string[,] pairs = new string[,]
            {
                { "привет всем", "hello everyone" },
                { "привет", "hi" },
                { "здравствуйте", "hello" },
                { "как дела", "how are you" },
                { "что нового", "what's new" },
                { "хорошо", "good" },
                { "отлично", "great" },
                { "спасибо", "thanks" },
                { "пожалуйста", "please" },
                { "пока", "bye" },
                { "увидимся", "see you" },
                { "спокойной ночи", "good night" },
                { "доброе утро", "good morning" },
                { "я скоро вернусь", "be right back" },
                { "давай поиграем", "let's play" },
                { "где ты", "where are you" },
                { "я тут", "i am here" },
                { "да", "yes" },
                { "нет", "no" },
                { "друг", "friend" },
                { "помощь", "help" },
                { "музыка", "music" },
                { "игры", "games" },
                { "работа", "work" },
                { "школа", "school" },
                { "время", "time" }
            };

            string output = text;
            for (int i = 0; i < pairs.GetLength(0); i++)
            {
                string from = targetLang == "en" ? pairs[i, 0] : pairs[i, 1];
                string to = targetLang == "en" ? pairs[i, 1] : pairs[i, 0];
                output = Regex.Replace(output, Regex.Escape(from), to, RegexOptions.IgnoreCase);
            }
            return output == text ? string.Format("[{0}] {1}", targetLang.ToUpperInvariant(), text) : output;
        }
    }
}
