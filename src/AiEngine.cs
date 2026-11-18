using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace AIMClassic
{
    public class AiProviderDef
    {
        public string Key;
        public string DisplayName;
        public string DefaultEndpoint;
        public string DefaultModel;
        public bool RequiresKey;
    }

    public class AiModelPreset
    {
        public string Id;
        public string DisplayName;
        public string Provider;
        public string Category;
        public string DescEn;
        public string DescRu;
    }

    public static class AiEngine
    {
        private static readonly JavaScriptSerializer _json = new JavaScriptSerializer { MaxJsonLength = 10485760 };
        private static readonly Random _rng = new Random();

        public static readonly AiProviderDef[] Providers = new AiProviderDef[]
        {
            new AiProviderDef { Key = "Gemini", DisplayName = "Google Gemini (Argon / Hydrogen)", DefaultEndpoint = "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions", DefaultModel = "gemini-4-hydrogen", RequiresKey = true },
            new AiProviderDef { Key = "OpenAI", DisplayName = "OpenAI ChatGPT (Astra / GPT-6)", DefaultEndpoint = "https://api.openai.com/v1/chat/completions", DefaultModel = "gpt-6-astra", RequiresKey = true },
            new AiProviderDef { Key = "Anthropic", DisplayName = "Anthropic Claude", DefaultEndpoint = "https://api.anthropic.com/v1/messages", DefaultModel = "claude-5-sonnet", RequiresKey = true },
            new AiProviderDef { Key = "OpenRouter", DisplayName = "OpenRouter (All Models)", DefaultEndpoint = "https://openrouter.ai/api/v1/chat/completions", DefaultModel = "google/gemini-4-argon", RequiresKey = true },
            new AiProviderDef { Key = "DeepSeek", DisplayName = "DeepSeek", DefaultEndpoint = "https://api.deepseek.com/chat/completions", DefaultModel = "deepseek-v4", RequiresKey = true },
            new AiProviderDef { Key = "xAI", DisplayName = "xAI Grok", DefaultEndpoint = "https://api.x.ai/v1/chat/completions", DefaultModel = "grok-4", RequiresKey = true },
            new AiProviderDef { Key = "Groq", DisplayName = "Groq Cloud (Fast)", DefaultEndpoint = "https://api.groq.com/openai/v1/chat/completions", DefaultModel = "llama-4-maverick-17b-128e-instruct", RequiresKey = true },
            new AiProviderDef { Key = "Mistral", DisplayName = "Mistral AI", DefaultEndpoint = "https://api.mistral.ai/v1/chat/completions", DefaultModel = "mistral-large-latest", RequiresKey = true },
            new AiProviderDef { Key = "Perplexity", DisplayName = "Perplexity (Web Search)", DefaultEndpoint = "https://api.perplexity.ai/chat/completions", DefaultModel = "sonar-pro", RequiresKey = true },
            new AiProviderDef { Key = "Qwen", DisplayName = "Alibaba Qwen", DefaultEndpoint = "https://dashscope-intl.aliyuncs.com/compatible-mode/v1/chat/completions", DefaultModel = "qwen3-235b-a22b", RequiresKey = true },
            new AiProviderDef { Key = "Together", DisplayName = "Together AI", DefaultEndpoint = "https://api.together.xyz/v1/chat/completions", DefaultModel = "meta-llama/Llama-4-Maverick-17B-128E-Instruct-FP8", RequiresKey = true },
            new AiProviderDef { Key = "Ollama", DisplayName = "Ollama (Local)", DefaultEndpoint = "http://localhost:11434/v1/chat/completions", DefaultModel = "llama3.3", RequiresKey = false },
            new AiProviderDef { Key = "Custom", DisplayName = "Custom OpenAI-Compatible", DefaultEndpoint = "", DefaultModel = "", RequiresKey = false }
        };

        public static readonly AiModelPreset[] ModelCatalog = new AiModelPreset[]
        {
            // Google Gemini 4 / 3 / 2.5
            new AiModelPreset { Id = "gemini-4-argon", DisplayName = "Gemini 4 Argon", Provider = "Gemini", Category = "Work & Reasoning", DescEn = "Frontier heavy reasoning, software architecture & 1M context", DescRu = "Флагман Gemini 4 для сложной работы, кода и глубокого анализа (1M)" },
            new AiModelPreset { Id = "gemini-4-hydrogen", DisplayName = "Gemini 4 Hydrogen", Provider = "Gemini", Category = "Fast Coding & Work", DescEn = "Fast, high-efficiency execution model for daily work & coding", DescRu = "Быстрая и эффективная модель для ежедневной работы и кода" },
            new AiModelPreset { Id = "gemini-4-carbon", DisplayName = "Gemini 4 Carbon", Provider = "Gemini", Category = "Frontier Heavy", DescEn = "Frontier Gemini 4 model for complex planning & analysis", DescRu = "Тяжёлая пограничная модель Gemini 4 для сложных задач" },
            new AiModelPreset { Id = "gemini-3.1-pro", DisplayName = "Gemini 3.1 Pro", Provider = "Gemini", Category = "Reasoning", DescEn = "Balanced pro-grade reasoning and writing assistant", DescRu = "Профессиональная сбалансированная модель Gemini 3.1 Pro" },
            new AiModelPreset { Id = "gemini-2.5-pro", DisplayName = "Gemini 2.5 Pro", Provider = "Gemini", Category = "Reasoning", DescEn = "Reliable Pro workhorse for code, docs & logic", DescRu = "Надёжная Pro модель для кода, текстов и логики" },
            new AiModelPreset { Id = "gemini-2.5-flash", DisplayName = "Gemini 2.5 Flash", Provider = "Gemini", Category = "Fast Chat", DescEn = "Ultra-fast everyday Gemini model", DescRu = "Очень быстрая универсальная модель Gemini" },
            new AiModelPreset { Id = "gemini-2.5-flash-lite", DisplayName = "Gemini 2.5 Flash-Lite", Provider = "Gemini", Category = "Lightweight", DescEn = "Low-cost rapid replies & translations", DescRu = "Экономная мгновенная модель для чата и переводов" },
            new AiModelPreset { Id = "gemini-2.0-flash", DisplayName = "Gemini 2.0 Flash", Provider = "Gemini", Category = "Fast Chat", DescEn = "Classic high-speed Gemini Flash", DescRu = "Классическая быстрая модель Gemini 2.0 Flash" },

            // OpenAI ChatGPT / GPT-6 / GPT-5 / o-series / 4o
            new AiModelPreset { Id = "gpt-6-astra", DisplayName = "GPT-6 Astra", Provider = "OpenAI", Category = "Flagship Work", DescEn = "OpenAI GPT-6 Astra — top-tier reasoning, work, research & code", DescRu = "Флагман OpenAI GPT-6 Astra — максимум интеллекта для работы и кода" },
            new AiModelPreset { Id = "gpt-6-sol", DisplayName = "GPT-6 Sol", Provider = "OpenAI", Category = "Balanced Work", DescEn = "OpenAI GPT-6 Sol — balanced power & speed for everyday projects", DescRu = "GPT-6 Sol — баланс мощности и скорости для проектов" },
            new AiModelPreset { Id = "gpt-6-luna", DisplayName = "GPT-6 Luna", Provider = "OpenAI", Category = "Fast Chat & Work", DescEn = "OpenAI GPT-6 Luna — fast agile chat & coding companion", DescRu = "GPT-6 Luna — лёгкая и быстрая версия GPT-6" },
            new AiModelPreset { Id = "gpt-5.4", DisplayName = "GPT-5.4", Provider = "OpenAI", Category = "Coding & Writing", DescEn = "High-precision GPT-5.4 generation for engineering & docs", DescRu = "Точная модель GPT-5.4 для разработки и документов" },
            new AiModelPreset { Id = "gpt-5", DisplayName = "GPT-5", Provider = "OpenAI", Category = "Flagship", DescEn = "General-purpose GPT-5 flagship model", DescRu = "Универсальный флагман GPT-5" },
            new AiModelPreset { Id = "gpt-5-mini", DisplayName = "GPT-5 Mini", Provider = "OpenAI", Category = "Fast & Economical", DescEn = "Fast, affordable GPT-5 work model", DescRu = "Быстрая и недорогая модель GPT-5 Mini" },
            new AiModelPreset { Id = "o3", DisplayName = "OpenAI o3", Provider = "OpenAI", Category = "Deep Reasoning", DescEn = "Step-by-step STEM, math & algorithmic reasoning", DescRu = "Модель глубоких пошаговых рассуждений (математика, код)" },
            new AiModelPreset { Id = "o4-mini", DisplayName = "OpenAI o4-mini", Provider = "OpenAI", Category = "Fast Reasoning", DescEn = "Compact fast STEM reasoning model", DescRu = "Компактная быстрая модель логических рассуждений" },
            new AiModelPreset { Id = "gpt-4.1", DisplayName = "GPT-4.1", Provider = "OpenAI", Category = "Structured Work", DescEn = "High instruction compliance & coding", DescRu = "Высокая точность следования инструкциям" },
            new AiModelPreset { Id = "gpt-4.1-mini", DisplayName = "GPT-4.1 Mini", Provider = "OpenAI", Category = "Fast Work", DescEn = "Low-latency structured tasks", DescRu = "Быстрая модель для повседневных задач" },
            new AiModelPreset { Id = "gpt-4o", DisplayName = "ChatGPT GPT-4o", Provider = "OpenAI", Category = "Classic ChatGPT", DescEn = "Versatile multimodal ChatGPT classic", DescRu = "Классический универсальный ChatGPT GPT-4o" },
            new AiModelPreset { Id = "gpt-4o-mini", DisplayName = "ChatGPT GPT-4o Mini", Provider = "OpenAI", Category = "Fast Light", DescEn = "Fast lightweight ChatGPT variant", DescRu = "Лёгкий и быстрый ChatGPT GPT-4o Mini" },

            // Anthropic Claude
            new AiModelPreset { Id = "claude-5-opus", DisplayName = "Claude 5 Opus", Provider = "Anthropic", Category = "Flagship Work", DescEn = "Anthropic's heaviest frontier model for architecture & prose", DescRu = "Топовая модель Claude 5 Opus для сложной архитектуры и текстов" },
            new AiModelPreset { Id = "claude-5-sonnet", DisplayName = "Claude 5 Sonnet", Provider = "Anthropic", Category = "Coding & Work", DescEn = "Fast flagship-grade Claude for programming & analysis", DescRu = "Лучшая сбалансированная модель Claude для кода и работы" },
            new AiModelPreset { Id = "claude-fable", DisplayName = "Claude Fable", Provider = "Anthropic", Category = "Creative & Research", DescEn = "Long-form writing & nuanced analysis", DescRu = "Глубокие тексты, аналитика и исследования" },
            new AiModelPreset { Id = "claude-opus-4-6", DisplayName = "Claude Opus 4.6", Provider = "Anthropic", Category = "Heavy Reasoning", DescEn = "Enterprise coding & deep problem solving", DescRu = "Мощное рассуждение и рефакторинг кода" },
            new AiModelPreset { Id = "claude-sonnet-4-6", DisplayName = "Claude Sonnet 4.6", Provider = "Anthropic", Category = "Coding Agent", DescEn = "High-speed software engineering model", DescRu = "Быстрый ИИ-инженер для разработки" },
            new AiModelPreset { Id = "claude-haiku-4-5", DisplayName = "Claude Haiku 4.5", Provider = "Anthropic", Category = "Instant Speed", DescEn = "Ultra-low latency concise responses", DescRu = "Мгновенные ответы с низкой задержкой" },

            // OpenRouter Unified Gateway (1 key -> all models)
            new AiModelPreset { Id = "google/gemini-4-argon", DisplayName = "Gemini 4 Argon (OpenRouter)", Provider = "OpenRouter", Category = "Unified Work", DescEn = "Access Google Gemini 4 Argon through one OpenRouter key", DescRu = "Доступ к Gemini 4 Argon через один ключ OpenRouter" },
            new AiModelPreset { Id = "google/gemini-4-hydrogen", DisplayName = "Gemini 4 Hydrogen (OpenRouter)", Provider = "OpenRouter", Category = "Unified Fast", DescEn = "Access Google Gemini 4 Hydrogen through OpenRouter", DescRu = "Доступ к Gemini 4 Hydrogen через OpenRouter" },
            new AiModelPreset { Id = "google/gemini-4-carbon", DisplayName = "Gemini 4 Carbon (OpenRouter)", Provider = "OpenRouter", Category = "Unified Heavy", DescEn = "Access Google Gemini 4 Carbon through OpenRouter", DescRu = "Доступ к Gemini 4 Carbon через OpenRouter" },
            new AiModelPreset { Id = "openai/gpt-6-astra", DisplayName = "GPT-6 Astra (OpenRouter)", Provider = "OpenRouter", Category = "Unified Flagship", DescEn = "Access OpenAI GPT-6 Astra through OpenRouter", DescRu = "Доступ к GPT-6 Astra через OpenRouter" },
            new AiModelPreset { Id = "openai/gpt-5", DisplayName = "GPT-5 (OpenRouter)", Provider = "OpenRouter", Category = "Unified Pro", DescEn = "Access OpenAI GPT-5 through OpenRouter", DescRu = "Доступ к OpenAI GPT-5 через OpenRouter" },
            new AiModelPreset { Id = "anthropic/claude-5-opus", DisplayName = "Claude 5 Opus (OpenRouter)", Provider = "OpenRouter", Category = "Unified Flagship", DescEn = "Access Claude 5 Opus through OpenRouter", DescRu = "Доступ к Claude 5 Opus через OpenRouter" },
            new AiModelPreset { Id = "anthropic/claude-sonnet-4-6", DisplayName = "Claude Sonnet 4.6 (OpenRouter)", Provider = "OpenRouter", Category = "Unified Coding", DescEn = "Access Claude Sonnet 4.6 through OpenRouter", DescRu = "Доступ к Claude Sonnet 4.6 через OpenRouter" },
            new AiModelPreset { Id = "deepseek/deepseek-v4", DisplayName = "DeepSeek V4 (OpenRouter)", Provider = "OpenRouter", Category = "Unified Coding", DescEn = "Access DeepSeek V4 through OpenRouter", DescRu = "Доступ к DeepSeek V4 через OpenRouter" },
            new AiModelPreset { Id = "x-ai/grok-4", DisplayName = "xAI Grok 4 (OpenRouter)", Provider = "OpenRouter", Category = "Unified Reasoning", DescEn = "Access xAI Grok 4 through OpenRouter", DescRu = "Доступ к Grok 4 через OpenRouter" },
            new AiModelPreset { Id = "qwen/qwen3-235b-a22b", DisplayName = "Qwen 3 235B (OpenRouter)", Provider = "OpenRouter", Category = "Open-Weight Giant", DescEn = "235B MoE model great for coding & multilingual RU/EN", DescRu = "Мощная открытая модель Qwen 3 (отлично знает русский и код)" },
            new AiModelPreset { Id = "meta-llama/llama-4-maverick", DisplayName = "Llama 4 Maverick (OpenRouter)", Provider = "OpenRouter", Category = "Open-Weight", DescEn = "Meta Llama 4 MoE generalist work model", DescRu = "Meta Llama 4 Maverick для задач и диалогов" },
            new AiModelPreset { Id = "meta-llama/llama-3.3-70b-instruct:free", DisplayName = "Llama 3.3 70B (Free)", Provider = "OpenRouter", Category = "Free Tier", DescEn = "Free community OpenRouter endpoint", DescRu = "Бесплатная модель на OpenRouter" },
            new AiModelPreset { Id = "google/gemini-2.5-flash:free", DisplayName = "Gemini 2.5 Flash (Free)", Provider = "OpenRouter", Category = "Free Tier", DescEn = "Free Gemini tier via OpenRouter", DescRu = "Бесплатный тариф Gemini через OpenRouter" },

            // DeepSeek
            new AiModelPreset { Id = "deepseek-v4", DisplayName = "DeepSeek V4", Provider = "DeepSeek", Category = "Flagship Code", DescEn = "Latest DeepSeek V4 model for coding & engineering", DescRu = "Новейшая модель DeepSeek V4 для программирования" },
            new AiModelPreset { Id = "deepseek-chat", DisplayName = "DeepSeek Chat (V3)", Provider = "DeepSeek", Category = "Fast & Cheap", DescEn = "Cost-effective work & general chat model", DescRu = "Быстрая и экономичная модель для работы и чата" },
            new AiModelPreset { Id = "deepseek-reasoner", DisplayName = "DeepSeek R1 / Reasoner", Provider = "DeepSeek", Category = "Thinking", DescEn = "Deep chain-of-thought reasoning for hard bugs & math", DescRu = "Режим глубоких размышлений для сложных задач" },

            // xAI Grok
            new AiModelPreset { Id = "grok-4", DisplayName = "xAI Grok 4", Provider = "xAI", Category = "Flagship", DescEn = "xAI flagship model with sharp analysis", DescRu = "Флагман xAI Grok 4 для анализа и задач" },
            new AiModelPreset { Id = "grok-3", DisplayName = "xAI Grok 3", Provider = "xAI", Category = "Balanced", DescEn = "Strong general reasoning & coding", DescRu = "Мощная универсальная модель Grok 3" },
            new AiModelPreset { Id = "grok-3-mini", DisplayName = "xAI Grok 3 Mini", Provider = "xAI", Category = "Fast Thinking", DescEn = "Fast reasoning at lower latency", DescRu = "Быстрая версия Grok 3 Mini" },

            // Groq
            new AiModelPreset { Id = "llama-4-maverick-17b-128e-instruct", DisplayName = "Llama 4 Maverick (Groq)", Provider = "Groq", Category = "Ultra-Fast", DescEn = "Sub-second LPU inference on Groq", DescRu = "Сверхбыстрая генерация на Groq" },
            new AiModelPreset { Id = "llama-3.3-70b-versatile", DisplayName = "Llama 3.3 70B Versatile", Provider = "Groq", Category = "Fast General", DescEn = "Versatile 70B open-weight model", DescRu = "Универсальная модель 70B на Groq" },
            new AiModelPreset { Id = "qwen-qwq-32b", DisplayName = "Qwen QwQ 32B Reasoning", Provider = "Groq", Category = "Fast Thinking", DescEn = "Fast open reasoning model on Groq", DescRu = "Быстрая думающая модель QwQ 32B" },
            new AiModelPreset { Id = "deepseek-r1-distill-llama-70b", DisplayName = "DeepSeek R1 70B (Groq)", Provider = "Groq", Category = "Reasoning", DescEn = "Distilled R1 reasoning at Groq speed", DescRu = "DeepSeek R1 со скоростью Groq" },

            // Mistral
            new AiModelPreset { Id = "mistral-large-latest", DisplayName = "Mistral Large", Provider = "Mistral", Category = "Enterprise", DescEn = "European flagship multilingual reasoning", DescRu = "Флагман Mistral для сложных многоязычных задач" },
            new AiModelPreset { Id = "codestral-latest", DisplayName = "Codestral", Provider = "Mistral", Category = "Code Specialist", DescEn = "Dedicated software engineering model", DescRu = "Специализированная модель для написания кода" },

            // Perplexity
            new AiModelPreset { Id = "sonar-pro", DisplayName = "Perplexity Sonar Pro", Provider = "Perplexity", Category = "Web Search AI", DescEn = "Online real-time cited research answers", DescRu = "Поиск в реальном времени с актуальными фактами" },
            new AiModelPreset { Id = "sonar-reasoning-pro", DisplayName = "Sonar Reasoning Pro", Provider = "Perplexity", Category = "Deep Research", DescEn = "Multi-step cited web research", DescRu = "Глубокое веб-исследование и анализ" },

            // Qwen
            new AiModelPreset { Id = "qwen3-235b-a22b", DisplayName = "Qwen 3 235B", Provider = "Qwen", Category = "Flagship", DescEn = "Alibaba flagship hybrid reasoning model", DescRu = "Флагман Qwen 3 для кода, математики и текстов" },
            new AiModelPreset { Id = "qwen-coder-plus", DisplayName = "Qwen Coder Plus", Provider = "Qwen", Category = "Coding", DescEn = "Dedicated code generation specialist", DescRu = "Специализированная модель для программистов" },

            // Together AI
            new AiModelPreset { Id = "meta-llama/Llama-4-Maverick-17B-128E-Instruct-FP8", DisplayName = "Llama 4 Maverick FP8", Provider = "Together", Category = "Cloud Open", DescEn = "Hosted Llama 4 high-throughput inference", DescRu = "Облачная Llama 4 Maverick" },
            new AiModelPreset { Id = "Qwen/Qwen2.5-Coder-32B-Instruct", DisplayName = "Qwen 2.5 Coder 32B", Provider = "Together", Category = "Coding", DescEn = "Dedicated coding model", DescRu = "Специализированный кодер Qwen" },

            // Local Ollama
            new AiModelPreset { Id = "llama3.3", DisplayName = "Llama 3.3 (Local)", Provider = "Ollama", Category = "Offline / Privacy", DescEn = "Local model via Ollama (no API key needed)", DescRu = "Локальная модель без интернета (Ollama)" },
            new AiModelPreset { Id = "qwen3", DisplayName = "Qwen 3 (Local)", Provider = "Ollama", Category = "Offline / RU+EN", DescEn = "Local Qwen 3 via Ollama", DescRu = "Локальная Qwen 3 (отлично понимает русский)" },
            new AiModelPreset { Id = "deepseek-r1", DisplayName = "DeepSeek R1 (Local)", Provider = "Ollama", Category = "Offline Reasoning", DescEn = "Local DeepSeek R1 via Ollama", DescRu = "Локальная думающая модель DeepSeek R1" },
            new AiModelPreset { Id = "gemma3", DisplayName = "Gemma 3 (Local)", Provider = "Ollama", Category = "Offline Google", DescEn = "Local Google Gemma 3 via Ollama", DescRu = "Локальная лёгкая модель Google Gemma 3" },
            new AiModelPreset { Id = "codellama", DisplayName = "CodeLlama (Local)", Provider = "Ollama", Category = "Offline Code", DescEn = "Local coding assistant", DescRu = "Локальный помощник программиста" }
        };

        public static AiProviderDef GetProviderDef(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                foreach (AiProviderDef p in Providers)
                {
                    if (string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase)) return p;
                }
            }
            return Providers[Providers.Length - 1];
        }

        public static List<AiModelPreset> GetModelsForProvider(string provider)
        {
            List<AiModelPreset> list = new List<AiModelPreset>();
            foreach (AiModelPreset m in ModelCatalog)
            {
                if (string.IsNullOrEmpty(provider) || string.Equals(m.Provider, provider, StringComparison.OrdinalIgnoreCase))
                    list.Add(m);
            }
            return list;
        }

        public static AiModelPreset GetModelPreset(string modelId)
        {
            if (string.IsNullOrWhiteSpace(modelId)) return null;
            string needle = modelId.Trim();
            foreach (AiModelPreset m in ModelCatalog)
            {
                if (string.Equals(m.Id, needle, StringComparison.OrdinalIgnoreCase)) return m;
            }
            return null;
        }

        public static bool ResolveConnection(Buddy buddy, AppSettings cfg, out string endpoint, out string apiKey, out string model, out string providerName)
        {
            endpoint = cfg.AiEndpoint ?? "";
            apiKey = cfg.AiApiKey ?? "";
            model = (buddy != null && !string.IsNullOrWhiteSpace(buddy.AiModelOverride))
                ? buddy.AiModelOverride.Trim()
                : (string.IsNullOrWhiteSpace(cfg.AiModel) ? "gpt-4o-mini" : cfg.AiModel.Trim());
            providerName = "Custom";

            AiModelPreset preset = GetModelPreset(model);
            string targetProv = preset != null ? preset.Provider : InferProviderFromModel(model);
            if (!string.IsNullOrEmpty(targetProv)) providerName = targetProv;

            List<ApiKeyProfile> profiles = cfg.ApiProfiles ?? new List<ApiKeyProfile>();
            ApiKeyProfile activeProf = profiles.FirstOrDefault(p => p.Id == cfg.ActiveApiProfileId);

            // If user explicitly chose a model (on Buddy or globally), prefer a configured profile for that vendor if active profile is empty or from another vendor
            ApiKeyProfile vendorProf = null;
            if (!string.IsNullOrEmpty(targetProv))
            {
                vendorProf = profiles.FirstOrDefault(p =>
                    string.Equals(p.Provider, targetProv, StringComparison.OrdinalIgnoreCase) &&
                    (!string.IsNullOrWhiteSpace(p.ApiKey) || p.Provider == "Ollama"));
            }

            if (vendorProf != null)
            {
                endpoint = vendorProf.Endpoint;
                apiKey = vendorProf.ApiKey;
                providerName = vendorProf.Provider;
            }
            else if (activeProf != null && (!string.IsNullOrWhiteSpace(activeProf.ApiKey) || activeProf.Provider == "Ollama"))
            {
                if (activeProf.Provider == "OpenRouter" && preset != null && preset.Provider != "OpenRouter")
                {
                    // Seamless OpenRouter auto-routing when user only entered an OpenRouter key
                    endpoint = activeProf.Endpoint;
                    apiKey = activeProf.ApiKey;
                    providerName = "OpenRouter";
                    string mapped = MapToOpenRouterSlug(model, preset.Provider);
                    if (!string.IsNullOrEmpty(mapped)) model = mapped;
                }
                else
                {
                    endpoint = activeProf.Endpoint;
                    apiKey = activeProf.ApiKey;
                    providerName = activeProf.Provider;
                }
            }
            else
            {
                // Last resort 1: if user has OpenRouter key saved anywhere, use it cleanly
                ApiKeyProfile orProf = profiles.FirstOrDefault(p =>
                    p.Provider == "OpenRouter" && !string.IsNullOrWhiteSpace(p.ApiKey));
                if (orProf != null && preset != null)
                {
                    endpoint = orProf.Endpoint;
                    apiKey = orProf.ApiKey;
                    providerName = "OpenRouter";
                    string mapped = MapToOpenRouterSlug(model, preset.Provider);
                    if (!string.IsNullOrEmpty(mapped)) model = mapped;
                }
                else
                {
                    // Last resort 2: No cloud API key -> auto-use local Ollama brain if downloaded/running!
                    List<string> localMods = LocalAiManager.CachedModels;
                    if (localMods != null && localMods.Count > 0)
                    {
                        endpoint = LocalAiManager.OLLAMA_BASE_URL + "/v1/chat/completions";
                        apiKey = "";
                        providerName = "Ollama";
                        string curModel = model;
                        bool matchesInstalled = localMods.Any(x => string.Equals(x, curModel, StringComparison.OrdinalIgnoreCase));
                        if (!matchesInstalled) model = localMods[0];
                        return true;
                    }
                }
            }

            bool isLocal = !string.IsNullOrWhiteSpace(endpoint) &&
                           (endpoint.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            endpoint.IndexOf("127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0);
            if (isLocal)
            {
                // Only count Ollama local endpoint as active when a local model is downloaded or manually configured
                List<string> cached = LocalAiManager.CachedModels;
                if (cached != null && cached.Count > 0)
                {
                    string curMod = model;
                    if (!cached.Any(x => string.Equals(x, curMod, StringComparison.OrdinalIgnoreCase)) &&
                        preset != null && preset.Provider != "Ollama")
                    {
                        model = cached[0];
                    }
                    return true;
                }
                return LocalAiManager.CachedRunning && !string.IsNullOrWhiteSpace(model);
            }
            return !string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey);
        }

        public static bool HasAnyCloudApiKey(AppSettings cfg)
        {
            if (cfg == null) return false;
            if (!string.IsNullOrWhiteSpace(cfg.AiApiKey)) return true;
            if (cfg.ApiProfiles != null)
            {
                foreach (ApiKeyProfile p in cfg.ApiProfiles)
                {
                    if (!string.Equals(p.Provider, "Ollama", StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(p.ApiKey))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static string InferProviderFromModel(string model)
        {
            if (string.IsNullOrEmpty(model)) return "";
            string m = model.ToLowerInvariant();
            if (m.Contains("/")) return "OpenRouter";
            if (m.StartsWith("gemini")) return "Gemini";
            if (m.StartsWith("gpt") || m.StartsWith("o3") || m.StartsWith("o4")) return "OpenAI";
            if (m.StartsWith("claude")) return "Anthropic";
            if (m.StartsWith("deepseek")) return "DeepSeek";
            if (m.StartsWith("grok")) return "xAI";
            if (m.StartsWith("mistral") || m.StartsWith("codestral")) return "Mistral";
            if (m.StartsWith("sonar")) return "Perplexity";
            if (m.StartsWith("qwen")) return "Qwen";
            return "";
        }

        private static string MapToOpenRouterSlug(string modelId, string prov)
        {
            if (modelId.Contains("/")) return modelId;
            if (prov == "Gemini") return "google/" + modelId;
            if (prov == "OpenAI") return "openai/" + modelId;
            if (prov == "Anthropic") return "anthropic/" + modelId;
            if (prov == "DeepSeek") return "deepseek/" + modelId;
            if (prov == "xAI") return "x-ai/" + modelId;
            if (prov == "Qwen") return "qwen/" + modelId;
            if (prov == "Mistral") return "mistralai/" + modelId;
            return modelId;
        }

        public static string GetActiveAiModeLabel(Buddy buddy = null)
        {
            AppSettings cfg = Store.State.Settings;
            if (!cfg.AiEnabled) return "AI Disabled";
            string ep, key, mod, prov;
            if (ResolveConnection(buddy, cfg, out ep, out key, out mod, out prov))
            {
                return "API: " + mod;
            }
            string preferred = (buddy != null && !string.IsNullOrWhiteSpace(buddy.AiModelOverride))
                ? buddy.AiModelOverride
                : cfg.AiModel;
            return "Offline (" + (string.IsNullOrWhiteSpace(preferred) ? "Add Key" : preferred) + ")";
        }

        public static bool IsApiConfigured()
        {
            return ShouldCallApi(Store.State.Settings);
        }

        public static string TranslateWithApi(string text, string targetLang)
        {
            AppSettings cfg = Store.State.Settings;
            if (!ShouldCallApi(cfg) || string.IsNullOrWhiteSpace(text)) return null;
            string langName = targetLang == "ru" ? "Russian" : "English";
            try
            {
                object[] msgs = new object[]
                {
                    new { role = "system", content = "Translate the user message naturally into " + langName + ". Preserve emoticons like :-) or <3. Output only the translation." },
                    new { role = "user", content = text }
                };
                return ExecuteChatCompletion(cfg.AiEndpoint, cfg.AiApiKey, cfg.AiModel, msgs, 220, 0.2);
            }
            catch { return null; }
        }

        public static bool TestConnection(string endpoint, string apiKey, string model, out string details)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    details = "Specify an API Endpoint URL.";
                    return false;
                }
                object[] msgs = new object[] { new { role = "user", content = "Say OK in 3 words." } };
                string extracted = ExecuteChatCompletion(endpoint, apiKey, model, msgs, 25, 0.2);
                details = "Connected! Reply: " + (extracted ?? "OK");
                return true;
            }
            catch (WebException wex)
            {
                string errBody = "";
                try
                {
                    if (wex.Response != null)
                    {
                        using (StreamReader sr = new StreamReader(wex.Response.GetResponseStream(), Encoding.UTF8))
                        {
                            errBody = sr.ReadToEnd();
                        }
                    }
                }
                catch { }
                details = string.IsNullOrEmpty(errBody) ? wex.Message : (wex.Message + " -> " + (errBody.Length > 160 ? errBody.Substring(0, 160) : errBody));
                return false;
            }
            catch (Exception ex)
            {
                details = ex.Message;
                return false;
            }
        }

        private static string ExecuteChatCompletion(string endpoint, string apiKey, string model, object[] messages, int maxTokens, double temp)
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Tls;
            string ep = endpoint.Trim();
            bool isLoopback = ep.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              ep.IndexOf("127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0;
            if (ep.StartsWith("http://localhost:11434", StringComparison.OrdinalIgnoreCase))
            {
                ep = "http://127.0.0.1:11434" + ep.Substring("http://localhost:11434".Length);
            }
            bool isAnthropic = ep.IndexOf("anthropic.com", StringComparison.OrdinalIgnoreCase) >= 0 ||
                               ep.EndsWith("/v1/messages", StringComparison.OrdinalIgnoreCase);

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(ep);
            req.Method = "POST";
            if (isLoopback) req.Proxy = null;
            req.ContentType = "application/json";
            req.Timeout = 45000;

            string bodyJson;
            if (isAnthropic)
            {
                if (!string.IsNullOrWhiteSpace(apiKey)) req.Headers["x-api-key"] = apiKey.Trim();
                req.Headers["anthropic-version"] = "2023-06-01";

                string sysPrompt = "";
                List<Dictionary<string, string>> nonSys = new List<Dictionary<string, string>>();
                foreach (object mObj in messages)
                {
                    Dictionary<string, object> d = _json.Deserialize<Dictionary<string, object>>(_json.Serialize(mObj));
                    string r = d != null && d.ContainsKey("role") ? Convert.ToString(d["role"]) : "user";
                    string c = d != null && d.ContainsKey("content") ? Convert.ToString(d["content"]) : "";
                    if (r == "system") sysPrompt = string.IsNullOrEmpty(sysPrompt) ? c : (sysPrompt + "\n" + c);
                    else
                    {
                        Dictionary<string, string> entry = new Dictionary<string, string>();
                        entry["role"] = (r == "assistant") ? "assistant" : "user";
                        entry["content"] = c;
                        nonSys.Add(entry);
                    }
                }
                if (nonSys.Count == 0)
                {
                    Dictionary<string, string> entry = new Dictionary<string, string>();
                    entry["role"] = "user";
                    entry["content"] = "Hello";
                    nonSys.Add(entry);
                }
                Dictionary<string, object> ap = new Dictionary<string, object>();
                ap["model"] = string.IsNullOrWhiteSpace(model) ? "claude-5-sonnet" : model.Trim();
                ap["max_tokens"] = Math.Max(64, maxTokens);
                if (!string.IsNullOrEmpty(sysPrompt)) ap["system"] = sysPrompt;
                ap["messages"] = nonSys;
                bodyJson = _json.Serialize(ap);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    req.Headers["Authorization"] = "Bearer " + apiKey.Trim();
                }
                var payload = new
                {
                    model = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model.Trim(),
                    messages = messages,
                    max_tokens = maxTokens,
                    temperature = temp
                };
                bodyJson = _json.Serialize(payload);
            }

            byte[] bodyBytes = Encoding.UTF8.GetBytes(bodyJson);
            req.ContentLength = bodyBytes.Length;
            using (Stream rs = req.GetRequestStream()) rs.Write(bodyBytes, 0, bodyBytes.Length);

            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
            {
                return ExtractContentFromJson(sr.ReadToEnd());
            }
        }

        private static string ExtractContentFromJson(string raw)
        {
            try
            {
                Dictionary<string, object> doc = _json.Deserialize<Dictionary<string, object>>(raw);
                if (doc == null) return null;

                // OpenAI / Gemini OpenAI-compat / OpenRouter / DeepSeek / Grok
                object choicesObj;
                if (doc.TryGetValue("choices", out choicesObj))
                {
                    object[] choices = choicesObj as object[];
                    if (choices != null && choices.Length > 0)
                    {
                        Dictionary<string, object> first = choices[0] as Dictionary<string, object>;
                        if (first != null && first.ContainsKey("message"))
                        {
                            Dictionary<string, object> msgObj = first["message"] as Dictionary<string, object>;
                            if (msgObj != null && msgObj.ContainsKey("content"))
                            {
                                object contentObj = msgObj["content"];
                                if (contentObj is string) return (string)contentObj;
                                object[] parts = contentObj as object[];
                                if (parts != null)
                                {
                                    StringBuilder sb = new StringBuilder();
                                    foreach (object pt in parts)
                                    {
                                        Dictionary<string, object> pd = pt as Dictionary<string, object>;
                                        if (pd != null && pd.ContainsKey("text")) sb.Append(Convert.ToString(pd["text"]));
                                    }
                                    return sb.ToString();
                                }
                            }
                        }
                    }
                }

                // Anthropic native Messages API response
                object contentArrObj;
                if (doc.TryGetValue("content", out contentArrObj))
                {
                    object[] contentArr = contentArrObj as object[];
                    if (contentArr != null && contentArr.Length > 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (object blk in contentArr)
                        {
                            Dictionary<string, object> bd = blk as Dictionary<string, object>;
                            if (bd != null && bd.ContainsKey("text"))
                            {
                                if (sb.Length > 0) sb.AppendLine();
                                sb.Append(Convert.ToString(bd["text"]));
                            }
                        }
                        if (sb.Length > 0) return sb.ToString();
                    }
                }
            }
            catch { }
            return null;
        }

        public static bool FetchModelsFromEndpoint(string endpoint, string apiKey, out List<string> models, out string error)
        {
            models = new List<string>();
            error = "";
            try
            {
                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    error = "Specify Endpoint URL first.";
                    return false;
                }
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | SecurityProtocolType.Tls;
                string ep = endpoint.Trim();
                bool isLoopback = ep.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  ep.IndexOf("127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0;
                if (ep.StartsWith("http://localhost:11434", StringComparison.OrdinalIgnoreCase))
                {
                    ep = "http://127.0.0.1:11434" + ep.Substring("http://localhost:11434".Length);
                }
                bool isAnthropic = ep.IndexOf("anthropic.com", StringComparison.OrdinalIgnoreCase) >= 0;
                string modelsUrl;
                if (ep.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
                    modelsUrl = ep.Substring(0, ep.Length - "/chat/completions".Length) + "/models";
                else if (ep.EndsWith("/messages", StringComparison.OrdinalIgnoreCase))
                    modelsUrl = ep.Substring(0, ep.Length - "/messages".Length) + "/models";
                else
                    modelsUrl = ep.TrimEnd('/') + "/models";

                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(modelsUrl);
                req.Method = "GET";
                if (isLoopback) req.Proxy = null;
                req.Timeout = 10000;
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    if (isAnthropic)
                    {
                        req.Headers["x-api-key"] = apiKey.Trim();
                        req.Headers["anthropic-version"] = "2023-06-01";
                    }
                    else
                    {
                        req.Headers["Authorization"] = "Bearer " + apiKey.Trim();
                    }
                }

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string raw = sr.ReadToEnd();
                    Dictionary<string, object> doc = _json.Deserialize<Dictionary<string, object>>(raw);
                    if (doc != null && doc.ContainsKey("data"))
                    {
                        object[] data = doc["data"] as object[];
                        if (data != null)
                        {
                            foreach (object item in data)
                            {
                                Dictionary<string, object> m = item as Dictionary<string, object>;
                                if (m != null && m.ContainsKey("id"))
                                {
                                    string id = Convert.ToString(m["id"]);
                                    if (!string.IsNullOrWhiteSpace(id) && !models.Contains(id)) models.Add(id);
                                }
                            }
                        }
                    }
                }
                models.Sort(StringComparer.OrdinalIgnoreCase);
                return models.Count > 0;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static string GenerateReply(Buddy buddy, string userMessage, string userScreenName)
        {
            AppSettings cfg = Store.State.Settings;
            if (cfg.AiEnabled && !HasAnyCloudApiKey(cfg) && !LocalAiManager.CachedRunning)
            {
                LocalAiManager.EnsureOrAutoCheckWithoutKeys();
            }
            string ep, key, model, prov;
            if (cfg.AiEnabled && ResolveConnection(buddy, cfg, out ep, out key, out model, out prov))
            {
                string apiReply = TryCallResolvedApi(ep, key, model, buddy, userMessage, userScreenName);
                if (!string.IsNullOrWhiteSpace(apiReply)) return apiReply.Trim();
            }
            return GenerateLocalSmartReply(buddy, userMessage, userScreenName);
        }

        private static bool ShouldCallApi(AppSettings cfg)
        {
            if (string.IsNullOrWhiteSpace(cfg.AiEndpoint)) return false;
            bool isLocal = cfg.AiEndpoint.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           cfg.AiEndpoint.IndexOf("127.0.0.1", StringComparison.OrdinalIgnoreCase) >= 0;
            return isLocal || !string.IsNullOrWhiteSpace(cfg.AiApiKey);
        }

        private static string TryCallResolvedApi(string endpoint, string apiKey, string model, Buddy buddy, string userMessage, string userScreenName)
        {
            try
            {
                string personaDesc = BuildPersonaSystemPrompt(buddy, userScreenName);
                List<object> msgs = new List<object>();
                msgs.Add(new { role = "system", content = personaDesc });

                List<ChatMessage> hist;
                if (Store.State.ChatHistory.TryGetValue(buddy.ScreenName, out hist))
                {
                    foreach (ChatMessage m in hist.Skip(Math.Max(0, hist.Count - 12)))
                    {
                        if (m.Sender == "SYSTEM") continue;
                        string role = string.Equals(m.Sender, userScreenName, StringComparison.OrdinalIgnoreCase) ? "user" : "assistant";
                        msgs.Add(new { role = role, content = m.Text ?? "" });
                    }
                }
                if (msgs.Count == 1)
                {
                    msgs.Add(new { role = "user", content = userMessage });
                }

                bool isWork = buddy != null && (buddy.Persona == "work" || buddy.Persona == "assistant");
                return ExecuteChatCompletion(endpoint, apiKey, model, msgs.ToArray(), isWork ? 1200 : 280, isWork ? 0.5 : 0.85);
            }
            catch { }
            return null;
        }

        private static string BuildPersonaSystemPrompt(Buddy buddy, string userSN)
        {
            if (buddy != null && buddy.Persona == "work")
            {
                return string.Format(
                    "You are {0} ({1}), a professional AI work & engineering assistant integrated into AIM Classic. " +
                    "Help {2} efficiently with coding, writing, analysis, research, planning, and debugging. " +
                    "Give clear, accurate, complete answers in the user's language (Russian or English).",
                    buddy.ScreenName,
                    string.IsNullOrWhiteSpace(buddy.AiModelOverride) ? "AI Work Model" : buddy.AiModelOverride,
                    userSN);
            }
            string basePrompt = string.Format(
                "You are {0} chatting on AOL Instant Messenger (AIM) around 2005-2007 with {1}. " +
                "Your interests: {2}. Your bio: {3}. Keep replies natural, concise (1-3 sentences), friendly, using occasional classic emoticons like :-), :-D, ;-). " +
                "Always reply in the same language the user writes in (Russian or English).",
                buddy.ScreenName, userSN, buddy.Interests ?? "music, games", buddy.Bio ?? "");

            if (buddy.Persona == "assistant")
            {
                return "You are AIM_AI_Helper, a smart, helpful, friendly AI work & chat assistant inside classic AIM. Reply in Russian if user writes in Russian, otherwise English. Give genuinely helpful answers.";
            }
            if (buddy.Persona == "gamer")
            {
                return basePrompt + " You love Counter-Strike 1.6, Warcraft III, WoW, and LAN parties.";
            }
            if (buddy.Persona == "geek")
            {
                return basePrompt + " You love The Matrix, Linux, overclocking Pentium 4 PCs, and Newgrounds Flash games.";
            }
            return basePrompt;
        }

        /* ==================================================================
           Built-in Bilingual Smart Conversational AI (Zero API Key Needed)
           ================================================================== */
        private static string GenerateLocalSmartReply(Buddy buddy, string text, string userSN)
        {
            string msg = (text ?? "").Trim();
            string low = msg.ToLowerInvariant();
            bool hasCyrillic = ContainsCyrillic(msg);
            bool hasLatin = Regex.IsMatch(msg, "[a-zA-Z]");
            bool isRu = hasCyrillic || (!hasLatin && buddy.Persona == "russian_friend");

            // 1. Quick math solver (e.g. "25*4" or "сколько будет 12 + 8")
            Match math = Regex.Match(msg, @"(?<!\d)(\d+(?:\.\d+)?)\s*([\+\-\*/])\s*(\d+(?:\.\d+)?)(?!\d)");
            if (math.Success)
            {
                double a, b;
                if (double.TryParse(math.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out a) &&
                    double.TryParse(math.Groups[3].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out b))
                {
                    string op = math.Groups[2].Value;
                    double res = op == "+" ? a + b : (op == "-" ? a - b : (op == "*" ? a * b : (b != 0 ? a / b : double.NaN)));
                    if (!double.IsNaN(res))
                    {
                        return isRu
                            ? string.Format("Легко! Получается {0:0.####} :-)", res)
                            : string.Format("Easy! That's {0:0.####} :-)", res);
                    }
                }
            }

            // 2. Time / date
            if (ContainsAny(low, new[] { "который час", "сколько времени", "время", "what time", "time is it" }))
            {
                return isRu
                    ? string.Format("На моих часах в трее XP сейчас {0} :-)", DateTime.Now.ToString("HH:mm"))
                    : string.Format("My XP system tray clock says {0} :-)", DateTime.Now.ToString("h:mm tt"));
            }

            // 3. Identity / who are you / AI & model check
            if (ContainsAny(low, new[] { "кто ты", "ты бот", "ты ии", "как тебя зовут", "модел", "argon", "hydrogen", "astra", "who are you", "are you ai", "what model", "which model" }))
            {
                string chosenMod = (buddy != null && !string.IsNullOrWhiteSpace(buddy.AiModelOverride))
                    ? buddy.AiModelOverride
                    : (Store.State.Settings.AiModel ?? "built-in");
                if (buddy.Persona == "work" || buddy.Persona == "assistant")
                {
                    return isRu
                        ? string.Format("Я {0} (выбрана модель: {1})! Сейчас работаю во встроенном оффлайн-режиме. Добавь свой API ключ в разделе «API Ключи» (Gemini 4 Argon/Hydrogen, GPT-6 Astra, Claude, DeepSeek, OpenRouter или локальная Ollama) — и я отвечу с полной силой нейросети :-)", buddy.ScreenName, chosenMod)
                        : string.Format("I'm {0} (selected model: {1})! Currently running on the built-in offline engine. Add your API key under 'API Keys' for full cloud reasoning (Gemini 4 Argon/Hydrogen, GPT-6 Astra, Claude 5, DeepSeek, Grok, OpenRouter, or Ollama) :-)", buddy.ScreenName, chosenMod);
                }
                return isRu
                    ? string.Format("Я {0}! {1} А ты давно в AIM сидишь, {2}? ;-)", buddy.ScreenName, buddy.Bio ?? "", userSN)
                    : string.Format("I'm {0}! Into {1}. How long have you been on AIM, {2}? ;-)", buddy.ScreenName, buddy.Interests ?? "music and games", userSN);
            }

            // 4. Games (checked before generic greetings so "Hey! What games..." hits Games)
            if (ContainsAny(low, new[] { "игр", "кс", "cs", "counter", "wow", "варкрафт", "dota", "дота", "game", "play" }))
            {
                return isRu
                    ? "О, игры это тема! B-) Погнали на de_dust2 в CS 1.6 или в Warcraft III на прохождение! В какую версию больше любишь рубиться?"
                    : "Aw yeah!! B-) Nothing beats CS 1.6 on de_dust2 or late-night WoW raids! What games are on your PC right now?";
            }

            // 5. Music
            if (ContainsAny(low, new[] { "музык", "песн", "трек", "winamp", "linkin", "рок", "рэп", "music", "song", "listen", "band" }))
            {
                return isRu
                    ? "Сейчас в Winamp крутится Linkin Park — Numb и старый добрый транс 2000-х :-D А что у тебя в плейлисте играет?"
                    : "Got Linkin Park and Blink-182 blasting in Winamp right now :-D What's playing on your end?";
            }

            // 6. Jokes / boredom
            if (ContainsAny(low, new[] { "скуч", "анекдот", "шутк", "bored", "joke", "funny" }))
            {
                string[] ruJokes = new[]
                {
                    "Знаешь, почему в 2006-м все сидели в чатах до 3 ночи? Потому что безлимит включался после полуночи :-D",
                    "Классика: устанавливаешь тему Windows XP Luna Blue — и настроение сразу +100% :-)",
                    "Рассказать секрет? Настоящий Pentium 4 не греется — он просто делится теплом с комнатой ;-)"
                };
                string[] enJokes = new[]
                {
                    "Why did 2006 AIM users never sleep? Waiting to see if their crush's Away Message changed :-D",
                    "Classic XP truth: watching the green segmented progress bar hit 100% fixes everything ;-)",
                    "Fun fact: dial-up taught us more patience than any meditation app ever could :-P"
                };
                return isRu ? Pick(ruJokes) : Pick(enJokes);
            }

            // 7. How are you / дела
            if (ContainsAny(low, new[] { "как дела", "че как", "как сам", "как ты", "how are you", "how r u", "how's it", "whats up", "what's up" }))
            {
                string[] ruHow = new[]
                {
                    "Всё супер! :-) Попиваю чай, качаю пару треков и болтаю в AIM. А у тебя как?",
                    "Да потихоньку, ностальгия по 2006 году прямо греет душу :-D Как сам?",
                    "Отлично! Сижу в онлайне, жду когда ребята в CS соберутся. У тебя какие планы на вечер?"
                };
                string[] enHow = new[]
                {
                    "Doing awesome! :-) Just chilling at my desk with some music. How about you?",
                    "Pretty great!! Loving this classic 2006 XP vibe :-D What are your plans tonight?",
                    "Not bad at all! Waiting on a download to finish. How's everything with u?"
                };
                return isRu ? Pick(ruHow) : Pick(enHow);
            }

            // 8. Greetings
            if (ContainsAny(low, new[] { "привет", "здравств", "здаров", "ку", "хай", "hello", "hey", "hi ", "hi!", "sup", "yo " }) || low == "hi" || low == "yo")
            {
                string[] ruGreet = new[]
                {
                    string.Format("Приветик, {0}! :-) Как твой день проходит?", userSN),
                    "О, здарова!! :-D Рад тебя видеть в сети! Чем занимаешься?",
                    "Привет! Сижу на Windows XP, слушаю Winamp. Что нового? ;-)"
                };
                string[] enGreet = new[]
                {
                    string.Format("Heyyy {0}!! :-) How's your day going?", userSN),
                    "Yo!! Good to see u online :-D What are you up to right now?",
                    "Hiii! Just listening to Winamp and browsing around. What's new? ;-)"
                };
                return isRu ? Pick(ruGreet) : Pick(enGreet);
            }

            // 9. Bye / AFK
            if (ContainsAny(low, new[] { "пока", "бб", "удачи", "спокойной", "bye", "gtg", "cya", "ttyl", "night" }))
            {
                return isRu
                    ? "Давай, до связи! Если что — оставляй сообщение, я в сети ;-) Пока!"
                    : "Aight ttyl!! Catch u later on AIM ;-) Bye!";
            }

            // 10. Question handling & contextual echo
            if (msg.EndsWith("?"))
            {
                if (isRu)
                {
                    string[] ruQ = new[]
                    {
                        string.Format("Хороший вопрос! Если подумать про «{0}», я бы точно выбрал вариант с душой и без спешки :-) А ты сам как считаешь?", TrimTopic(msg)),
                        "Хм, интересная мысль :-O На самом деле всё зависит от настроения, но звучит круто! Расскажешь подробнее?",
                        "Ого, заставил задуматься! :-) Мне кажется, в духе 2006 года ответ один — пробовать и не бояться!"
                    };
                    return Pick(ruQ);
                }
                else
                {
                    string[] enQ = new[]
                    {
                        "Ooh good question! Honestly I think it depends on the vibe, what's your take on it? :-)",
                        "Hmm interesting :-O Definitely worth trying out! What made u think of that?",
                        "Haha totally! I'd go for it :-D What do u think?"
                    };
                    return Pick(enQ);
                }
            }

            // 11. General smart conversational continuation referencing interests
            if (isRu)
            {
                string[] ruGen = new[]
                {
                    string.Format("Понял тебя, {0}! Кстати, звучит очень даже круто :-) А что ещё интересного произошло за последнее время?", userSN),
                    string.Format("Ахах, точно подмечено :-D Кстати, мне тут как раз близка тема {0}. Хочешь обсудим?", buddy.Interests ?? "музыки и игр"),
                    "Ого, класс! ;-) Люблю такие разговоры в старом добром AIM. Продолжай, я внимательно читаю!",
                    "Угу, согласен на все 100% <3 Если захочешь, можем ещё зайти в групповую комнату #AIM_Lobby_2006!"
                };
                return Pick(ruGen);
            }
            else
            {
                string[] enGen = new[]
                {
                    string.Format("Haha totally get that, {0}! :-) So what else has been going on lately?", userSN),
                    string.Format("Ooh nice!! :-D Speaking of which, I've been super into {0} lately. You?", buddy.Interests ?? "music & gaming"),
                    "For real! ;-) Love having chill convos on AIM like this. Tell me more!",
                    "lol awesome <3 btw feel free to test sending a file or jumping into #AIM_Lobby_2006!"
                };
                return Pick(enGen);
            }
        }

        private static bool ContainsCyrillic(string s)
        {
            foreach (char c in s)
            {
                if (c >= 0x0400 && c <= 0x04FF) return true;
            }
            return false;
        }

        private static bool ContainsAny(string text, string[] needles)
        {
            foreach (string n in needles)
            {
                if (text.Contains(n)) return true;
            }
            return false;
        }

        private static string TrimTopic(string s)
        {
            string t = s.TrimEnd('?', ' ');
            return t.Length > 34 ? t.Substring(0, 34) + "..." : t;
        }

        private static string Pick(string[] arr)
        {
            return arr[_rng.Next(arr.Length)];
        }
    }
}
