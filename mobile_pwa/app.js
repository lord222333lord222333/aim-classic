// AIM Classic Mobile (Android & Apple iOS PWA/APK + Full PC Feature Parity v6.0.4020)
(function () {
  const NTFY_BASE = 'https://ntfy.sh/';
  const STORAGE_KEY = 'aim_classic_mobile_state_v60';
  const CURRENT_VERSION = '6.0.4020';

  // Pure-JS SHA-256 so non-HTTPS LAN / local URLs derive identical topics to PC NetworkEngine.cs!
  function sha256Hex16(str) {
    function rightRotate(v, amount) { return (v >>> amount) | (v << (32 - amount)); }
    const mathPow = Math.pow, maxWord = mathPow(2, 32), lengthProperty = 'length';
    let result = '', words = [], ascii = unescape(encodeURIComponent(str || ''));
    const asciiBitLength = ascii[lengthProperty] * 8;
    let hash = [], k = [], primeCounter = 0;
    const isComposite = {};
    for (let candidate = 2; primeCounter < 64; candidate++) {
      if (!isComposite[candidate]) {
        for (let i = 0; i < 313; i += candidate) isComposite[i] = candidate;
        hash[primeCounter] = (mathPow(candidate, 0.5) * maxWord) | 0;
        k[primeCounter++] = (mathPow(candidate, 1 / 3) * maxWord) | 0;
      }
    }
    ascii += '\x80';
    while ((ascii[lengthProperty] % 64) - 56) ascii += '\x00';
    for (let i = 0; i < ascii[lengthProperty]; i++) {
      const j = ascii.charCodeAt(i);
      words[i >> 2] |= j << (((3 - i) % 4) * 8);
    }
    words[words[lengthProperty]] = ((asciiBitLength / maxWord) | 0);
    words[words[lengthProperty]] = asciiBitLength;
    for (let j = 0; j < words[lengthProperty];) {
      const w = words.slice(j, (j += 16)), oldHash = hash;
      hash = hash.slice(0, 8);
      for (let i = 0; i < 64; i++) {
        const w15 = w[i - 15], w2 = w[i - 2];
        const a = hash[0], e = hash[4];
        const temp1 = hash[7] + (rightRotate(e, 6) ^ rightRotate(e, 11) ^ rightRotate(e, 25))
          + ((e & hash[5]) ^ ((~e) & hash[6])) + k[i]
          + (w[i] = (i < 16) ? w[i] : (w[i - 16] + (rightRotate(w15, 7) ^ rightRotate(w15, 18) ^ (w15 >>> 3))
          + w[i - 7] + (rightRotate(w2, 17) ^ rightRotate(w2, 19) ^ (w2 >>> 10))) | 0);
        const temp2 = (rightRotate(a, 2) ^ rightRotate(a, 13) ^ rightRotate(a, 22))
          + ((a & hash[1]) ^ (a & hash[2]) ^ (hash[1] & hash[2]));
        hash = [(temp1 + temp2) | 0].concat(hash);
        hash[4] = (hash[4] + temp1) | 0;
      }
      for (let i = 0; i < 8; i++) hash[i] = (hash[i] + oldHash[i]) | 0;
    }
    for (let i = 0; i < 2; i++) {
      const val = hash[i];
      for (let j = 3; j >= 0; j--) result += ((val >> (j * 8)) & 255).toString(16).padStart(2, '0');
    }
    return result.slice(0, 16);
  }

  function normRoom(r) { return (r || '').trim().toLowerCase() || 'aim-global-2006'; }
  function dirTopic(r) { return 'aimc_v59_dir_' + sha256Hex16(normRoom(r)); }
  function pmTopic(sn, r) { return 'aimc_v59_pm_' + sha256Hex16((sn || '').trim().toLowerCase() + ':' + normRoom(r)); }
  function pubDirTopic(r) { return 'aimc_v60_pubdir_' + sha256Hex16(normRoom(r)); }
  function pubRoomTopic(name, r) { return 'aimc_v60_room_' + sha256Hex16((name || '').trim().toLowerCase() + ':' + normRoom(r)); }

  const MODEL_CATALOG = [
    'gemini-4-argon', 'gemini-4-hydrogen', 'gemini-4-carbon', 'gemini-2.5-flash', 'gemini-2.5-pro',
    'gpt-6-astra', 'gpt-6-sol', 'gpt-6-luna', 'gpt-5', 'gpt-4o', 'gpt-4o-mini',
    'claude-5-opus', 'claude-5-sonnet', 'claude-4.5-haiku',
    'deepseek-v4', 'deepseek-reasoner', 'deepseek-chat', 'grok-4', 'grok-3-mini',
    'google/gemini-4-argon', 'google/gemini-4-hydrogen', 'openai/gpt-6-astra',
    'meta-llama/llama-4-maverick', 'llama3.2:1b', 'qwen2.5:1.5b', 'gemma3:1b'
  ];

  // Matches PC RetroBanners.cs slides
  const BANNER_SLIDES = [
    { tag: 'GLOBAL P2P', en: 'Add Global Friends Anywhere!', sEn: 'Connect via Global Room or Nickname ->', ru: 'Добавляй друзей по всему миру!', sRu: 'Глобальная комната (ntfy) и общие чаты ->', img: './banners/banner_globe.jpg', action: 'real' },
    { tag: 'FRUTIGER AERO', en: 'NEW: Frutiger Aero Glossy Theme!', sEn: '2007 Aqua glass windows & wallpaper in Workshop ->', ru: 'НОВОЕ: Тема Фрутигер Аэро (Aqua Glass)!', sRu: 'Глянцевое стекло 2007 и обои в Мастерской ->', img: './banners/banner_aero.jpg', action: 'frutiger' },
    { tag: 'AOL MUSIC', en: 'AIM Music Player & Lobby Hits', sEn: 'Play Aero synth / MP3 & join #AIM_Lobby_2006 ->', ru: 'Музыкальный плеер AIM и чаты 2000-х', sRu: 'Включай треки, MP3 и заходи в #AIM_Lobby_2006 ->', img: './banners/banner_music.jpg', action: 'music' },
    { tag: 'WORKSHOP', en: 'AIM Theme & Avatar Workshop', sEn: '7 XP skins, custom theme studio & 2006 buddy icons ->', ru: 'Мастерская тем и аватарок AIM', sRu: '7 тем оформления, конструктор скинов и иконки ->', img: './banners/banner_workshop.jpg', action: 'workshop' },
    { tag: 'AIM MAIL', en: "You've Got Mail! Open Inbox", sEn: 'Read retro AOL emails & write friends via cloud ->', ru: 'Вам письмо! Открыть AOL Почту', sRu: 'Читайте письма, отвечайте и пишите друзьям ->', img: './banners/banner_mail.jpg', action: 'mail' },
    { tag: 'BOTS + API', en: 'AI Bots, Updates & Support Center', sEn: 'Argon / Astra models, diagnostics & Update Center ->', ru: 'ИИ-боты, поддержка и центр обновлений', sRu: 'Модели Argon / Astra, диагностика и обновления ->', img: './banners/banner_aero.jpg', action: 'support' }
  ];

  // Matches PC WorkshopForm.cs 7 built-in skins
  const BUILTIN_SKINS = [
    { key: 'aero', title: 'Frutiger Aero (2007)', badge: 'FEATURED', desc: 'Glossy Aqua Glass + Sky Wallpaper', top: '#7ce8ff', bot: '#0564a4', face: '#e4f6fd', gold: '#ffdc3e', glass: true },
    { key: 'luna', title: 'XP Luna Blue (2005)', badge: 'CLASSIC', desc: 'Authentic Windows XP Sapphire', top: '#3593ff', bot: '#003db8', face: '#ece9d8', gold: '#ffcc00', glass: false },
    { key: 'royale', title: 'XP Royale Energy', badge: '2006 SKIN', desc: 'Media Center Glossy Royal Blue', top: '#65a2e8', bot: '#113a7e', face: '#e8eef7', gold: '#ffd438', glass: true },
    { key: 'ice', title: 'AIM 6.0 Triton Ice', badge: 'AIM 6', desc: 'Cool Frost Slate-Blue Chrome', top: '#4fa0f3', bot: '#14529e', face: '#ebf1f8', gold: '#ffcc00', glass: false },
    { key: 'silver', title: 'Windows XP Silver', badge: 'XP PRO', desc: 'Metallic Silver + Navy Accent', top: '#e6e8f0', bot: '#7b8196', face: '#e6e7ec', gold: '#ffcc00', glass: false },
    { key: 'olive', title: 'XP Homestead Olive', badge: 'RETRO XP', desc: 'Warm Olive Green + Cream Face', top: '#c2ce91', bot: '#6d7942', face: '#f0eee0', gold: '#ffcf33', glass: false },
    { key: 'neon', title: 'Y2K Cyber Neon', badge: 'WINAMP', desc: 'Glossy Violet Chrome + Cyan', top: '#a24dff', bot: '#270b6b', face: '#efeafd', gold: '#3cf4ff', glass: true }
  ];

  const AVATAR_PACK = [
    { id: 'aim_icon.gif', label: 'AIM Classic', pc: 'AIM Icon.gif' },
    { id: 'aol_icon.gif', label: 'AOL Triangle', pc: 'AOL Icon.gif' },
    { id: 'smiley.gif', label: 'Smiley', pc: 'Smiley Face.gif' },
    { id: 'wink.gif', label: 'Wink ;-)', pc: 'Wink.gif' },
    { id: 'hal9000.gif', label: 'HAL 9000', pc: 'Hal9000.gif' },
    { id: 'robot.gif', label: 'Robot AI', pc: 'Robot.gif' },
    { id: 'alien.gif', label: 'Alien', pc: 'Alien.gif' },
    { id: 'butterfly.gif', label: 'Butterfly', pc: 'Butterfly.gif' },
    { id: 'pirate.gif', label: 'Pirate', pc: 'Pirate.gif' },
    { id: 'skull.gif', label: 'Skull', pc: 'Skull & Crossbones.gif' },
    { id: 'vamp.gif', label: 'Vamp', pc: 'Vamp.gif' },
    { id: 'dragon.gif', label: 'Dragon', pc: 'Dragon 1.gif' },
    { id: 'cat.gif', label: 'Cat', pc: 'Cat.gif' },
    { id: 'dog.gif', label: 'Dog', pc: 'Dog.gif' },
    { id: 'heart.gif', label: 'Heart', pc: 'Heart.gif' },
    { id: 'basketball.gif', label: 'Basket', pc: 'Basketball.gif' }
  ];

  const AWAY_PRESETS = [
    { title: 'Default Away', msg: 'Sorry, I am away from my phone/PC right now. Leave an IM!' },
    { title: 'BRB / Coffee', msg: 'Stepped away for coffee — back in 5 minutes! :-)' },
    { title: 'School / Work', msg: 'Busy studying/working. Leave a message or email me!' },
    { title: 'Gaming (CS 1.6)', msg: 'Playing Counter-Strike 1.6 — DO NOT BUZZ!! >:-)' },
    { title: 'Listening to Music', msg: 'Chillin with AIM Music Player & Frutiger Aero vibes ♪' }
  ];

  const DEFAULT_STATE = {
    nick: 'PhoneUser_' + Math.floor(100 + Math.random() * 899),
    avatar: './avatars/aim_icon.gif',
    status: 'online',
    mood: 'On AIM Mobile 6.0! :-)',
    awayText: AWAY_PRESETS[0].msg,
    room: 'AIM-Global-2006',
    lang: (navigator.language || '').toLowerCase().startsWith('ru') ? 'ru' : 'en',
    theme: 'aero',
    aeroWallpaper: true,
    showBanners: true,
    muted: false,
    updChannel: 'Stable (v6.0)',
    updAuto: true,
    updManifestUrl: '',
    aiModel: 'gemini-4-argon',
    aiEndpoint: 'https://generativelanguage.googleapis.com/v1beta/openai/chat/completions',
    aiKey: '',
    shareMusic: true,
    customThemes: [],
    friends: [
      { sn: 'Argon_AI', alias: 'Gemini 4 Argon (Work)', status: 'online', isReal: false, model: 'gemini-4-argon', avatar: './avatars/robot.gif' },
      { sn: 'Hydrogen_Dev', alias: 'Gemini 4 Hydrogen (Code)', status: 'online', isReal: false, model: 'gemini-4-hydrogen', avatar: './avatars/hal9000.gif' },
      { sn: 'Astra_GPT', alias: 'GPT-6 Astra (ChatGPT)', status: 'online', isReal: false, model: 'gpt-6-astra', avatar: './avatars/alien.gif' },
      { sn: 'AIM_AI_Helper', alias: 'AI Assistant', status: 'online', isReal: false, model: '', avatar: './avatars/aim_icon.gif' }
    ],
    discoveredPeers: [],
    chats: {
      'Argon_AI': [{ sender: 'Argon_AI', text: 'Hi from AIM Classic Mobile 6.0! Banners, Workshop, Mail, Support, Updates & AI Bots are all ready :-)', time: 'Now' }]
    },
    rooms: [
      { name: '#Global_AIM_Plaza', cat: 'Global', by: 'GlobalHub', topic: 'World hangout for PC & Phone AIM users!', msgs: [] },
      { name: '#FrutigerAero_Hub', cat: 'Themes & Design', by: 'AeroFan', topic: '2007 Glossy Aqua Glass & XP Vibes', msgs: [] },
      { name: '#AIM_Lobby_2006', cat: 'General', by: 'AIM Team', topic: 'Classic 2006 AIM chat room :-)', msgs: [] },
      { name: '#AI_Work_Lab', cat: 'Tech & AI', by: 'Argon_AI', topic: 'Argon, Hydrogen, Astra & Ollama discussions', msgs: [] }
    ],
    mail: [
      { id: 'm1', from: 'welcome@aol.com', to: 'Me@aim.com', subj: 'Welcome to AIM Classic 6.0 Mobile!', date: 'Today', read: false, body: 'Now with full PC parity: Rotating Promo Banners, Theme Workshop (7 skins + Creator Studio + 16 Retro Avatars), AOL Mail Inbox, Support Diagnostics, Update Center & Global Public Groups!' },
      { id: 'm2', from: 'AeroFan@aim.com', to: 'Me@aim.com', subj: 'Frutiger Aero & XP Themes Workshop', date: 'Today', read: false, body: 'Open AIM Hub -> Workshop to install Frutiger Aero, XP Luna, Royale, Silver, Olive, or design your own .aimtheme.json!' },
      { id: 'm3', from: 'DarkKnight_88@aim.com', to: 'Me@aim.com', subj: 'Join #Global_AIM_Plaza!', date: 'Yesterday', read: false, body: 'See you in the Global Public Groups Hub from your Android phone or Windows PC B-)' }
    ]
  };

  let state = loadState();
  let activeChatSn = 'Argon_AI';
  let activeRoomName = null;
  let selectedMailId = null;
  let bannerIndex = 0;
  let hubSubtab = 'music';
  const seenIds = new Set();
  let dirSince = 0, pmSince = 0, pubSince = 0, roomSince = 0;
  let deferredInstall = null;
  let foundUpdateUrl = '';

  function loadState() {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) {
        const parsed = JSON.parse(raw);
        return Object.assign({}, JSON.parse(JSON.stringify(DEFAULT_STATE)), parsed);
      }
    } catch {}
    return JSON.parse(JSON.stringify(DEFAULT_STATE));
  }
  function saveState() {
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify(state)); } catch {}
  }

  function T(en, ru) { return state.lang === 'ru' ? ru : en; }
  function nowStr() { return new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }); }
  function esc(s) { return String(s || '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;'); }

  function mapAvatarSrc(avName) {
    if (!avName) return './avatars/aim_icon.gif';
    if (avName.startsWith('data:') || avName.startsWith('./') || avName.startsWith('http')) return avName;
    const found = AVATAR_PACK.find(a => a.pc.toLowerCase() === avName.toLowerCase() || a.id.toLowerCase() === avName.toLowerCase());
    return found ? ('./avatars/' + found.id) : './avatars/aim_icon.gif';
  }

  // =========================================================================
  // Classic AIM Real WAV Sounds + Web Audio Synth & Music Player
  // =========================================================================
  const SOUND_PACK = [
    { key: 'welcome', label: 'Welcome!', file: 'welcome.wav' },
    { key: 'mail', label: "You've Got Mail", file: 'youve_got_mail.wav' },
    { key: 'pictures', label: 'Got Pictures', file: 'youve_got_pictures.wav' },
    { key: 'voicemail', label: 'Voicemail', file: 'youve_got_voicemail.wav' },
    { key: 'imSend', label: 'IM Send', file: 'im_send.wav' },
    { key: 'imReceive', label: 'IM Receive', file: 'im_receive.wav' },
    { key: 'imNew', label: 'IM New', file: 'im_new.wav' },
    { key: 'ring', label: 'Ring / Buzz', file: 'ring.wav' },
    { key: 'doorOpen', label: 'Door Open', file: 'door_open.wav' },
    { key: 'doorClose', label: 'Door Close', file: 'door_close.wav' },
    { key: 'chatJoin', label: 'Chat Join', file: 'chat_join.wav' },
    { key: 'chatLeave', label: 'Chat Leave', file: 'chat_leave.wav' },
    { key: 'ding', label: 'Buddy Ding', file: 'buddy_ding.wav' },
    { key: 'alert', label: 'Buddy Alert', file: 'buddy_alert.wav' },
    { key: 'chime', label: 'Connected', file: 'connected_chime.wav' },
    { key: 'filesDone', label: 'Files Done', file: 'files_done.wav' },
    { key: 'click', label: 'Talk Click', file: 'talk_click.wav' },
    { key: 'goodbye', label: 'Goodbye', file: 'goodbye.wav' },
    { key: 'modem', label: '56K Modem', file: 'dialup_modem.wav' }
  ];
  const wavCache = {};
  let activeSfxNode = null;
  let audioCtx = null;

  function getAC() {
    const AC = window.AudioContext || window.webkitAudioContext;
    if (!AC) return null;
    if (!audioCtx) audioCtx = new AC();
    if (audioCtx.state === 'suspended') audioCtx.resume().catch(() => {});
    return audioCtx;
  }

  function playToneFallback(kind) {
    const ac = getAC();
    if (!ac) return;
    const seqs = {
      welcome: [[523.25, 0, 0.10], [659.25, 0.09, 0.12], [783.99, 0.18, 0.22]],
      mail: [[587.33, 0, 0.12], [880.00, 0.11, 0.24]],
      ding: [[880.00, 0, 0.09], [1046.50, 0.08, 0.14]],
      ring: [[987.77, 0, 0.08], [783.99, 0.08, 0.08], [987.77, 0.16, 0.12]],
      goodbye: [[659.25, 0, 0.10], [523.25, 0.10, 0.12], [392.00, 0.20, 0.24]]
    };
    const notes = seqs[kind] || seqs.ding;
    const t0 = ac.currentTime;
    notes.forEach(([f, off, dur]) => {
      const o = ac.createOscillator(), g = ac.createGain();
      o.type = 'triangle';
      o.frequency.setValueAtTime(f, t0 + off);
      g.gain.setValueAtTime(0.001, t0 + off);
      g.gain.exponentialRampToValueAtTime(0.08, t0 + off + 0.015);
      g.gain.exponentialRampToValueAtTime(0.001, t0 + off + dur);
      o.connect(g); g.connect(ac.destination);
      o.start(t0 + off); o.stop(t0 + off + dur + 0.02);
    });
  }

  async function playAimSound(kind) {
    if (state.muted) return;
    const item = SOUND_PACK.find(s => s.key === kind) || SOUND_PACK.find(s => s.key === 'ding');
    if (!item) { playToneFallback(kind); return; }
    const url = './sounds/' + item.file;
    try {
      const ac = getAC();
      if (ac) {
        if (!wavCache[url]) {
          const resp = await fetch(url);
          if (!resp.ok) throw new Error('HTTP ' + resp.status);
          const ab = await resp.arrayBuffer();
          wavCache[url] = await ac.decodeAudioData(ab);
        }
        if (activeSfxNode && item.key === 'modem') {
          try { activeSfxNode.stop(); } catch {}
        }
        const src = ac.createBufferSource();
        src.buffer = wavCache[url];
        src.connect(ac.destination);
        src.start(0);
        activeSfxNode = src;
        return;
      }
    } catch {}
    try {
      const a = new Audio(url);
      a.play().catch(() => playToneFallback(kind));
    } catch {
      playToneFallback(kind);
    }
  }

  const SYNTH_TRACKS = [
    { id: 's0', title: 'Frutiger Aero Breeze', artist: 'Aqua Synth 2007', chords: [[261.63, 329.63, 392, 493.88], [174.61, 220, 261.63, 329.63], [220, 261.63, 329.63, 392], [196, 246.94, 293.66, 392]] },
    { id: 's1', title: 'Late Night AIM 2006', artist: 'Lo-Fi Buddy Chords', chords: [[220, 261.63, 329.63, 392], [196, 246.94, 293.66, 349.23], [174.61, 220, 261.63, 329.63], [164.81, 196, 246.94, 293.66]] },
    { id: 's2', title: 'XP Welcome Dream', artist: 'Luna Glass Pad', chords: [[293.66, 369.99, 440, 554.37], [246.94, 293.66, 369.99, 440], [196, 246.94, 293.66, 369.99], [220, 277.18, 329.63, 392]] },
    { id: 's3', title: 'Winamp Retro Groove', artist: '2000s Chiptune', chords: [[261.63, 329.63, 392, 523.25], [220, 261.63, 329.63, 440], [174.61, 220, 261.63, 349.23], [196, 246.94, 293.66, 392]] }
  ];
  const playlist = SYNTH_TRACKS.map(t => Object.assign({ synth: true }, t));
  let trackIdx = 0, isPlaying = false, synthTimer = null, htmlAudio = null, playSec = 0, secTimer = null;

  function stopMusic() {
    isPlaying = false;
    if (synthTimer) { clearInterval(synthTimer); synthTimer = null; }
    if (secTimer) { clearInterval(secTimer); secTimer = null; }
    if (htmlAudio) { htmlAudio.pause(); htmlAudio.currentTime = 0; }
    playSec = 0;
    updateMusicUi();
  }

  function playTrack(idx) {
    stopMusic();
    if (idx >= 0 && idx < playlist.length) trackIdx = idx;
    const tr = playlist[trackIdx];
    if (!tr) return;
    isPlaying = true;
    playSec = 0;
    secTimer = setInterval(() => {
      playSec++;
      const mm = String(Math.floor(playSec / 60)).padStart(2, '0');
      const ss = String(playSec % 60).padStart(2, '0');
      document.getElementById('oled-time').textContent = mm + ':' + ss;
    }, 1000);

    if (state.shareMusic) {
      state.mood = '♪ ' + tr.title + ' — ' + tr.artist;
      document.getElementById('me-mood').value = state.mood;
      saveState();
      publishPresence();
    }

    if (tr.synth) {
      const ac = getAC();
      if (ac) {
        let step = 0;
        const playStep = () => {
          if (!isPlaying || !ac || state.muted) return;
          const chord = tr.chords[Math.floor(step / 2) % tr.chords.length];
          const now = ac.currentTime;
          chord.forEach((freq, i) => {
            const osc = ac.createOscillator(), g = ac.createGain();
            osc.type = i === 0 ? 'triangle' : 'sine';
            osc.frequency.setValueAtTime(freq, now);
            g.gain.setValueAtTime(0.001, now);
            g.gain.exponentialRampToValueAtTime(0.04, now + 0.12);
            g.gain.exponentialRampToValueAtTime(0.001, now + 1.15);
            osc.connect(g); g.connect(ac.destination);
            osc.start(now); osc.stop(now + 1.2);
          });
          step++;
        };
        playStep();
        synthTimer = setInterval(playStep, 1250);
      }
    } else if (tr.url) {
      if (!htmlAudio) htmlAudio = new Audio();
      htmlAudio.src = tr.url;
      htmlAudio.muted = !!state.muted;
      htmlAudio.onended = () => playTrack((trackIdx + 1) % playlist.length);
      htmlAudio.play().catch(() => {});
    }
    updateMusicUi();
  }

  // =========================================================================
  // ntfy.sh Global Cloud Sync (Identical topics & NetPacket schema as PC)
  // =========================================================================
  async function publishNtfy(topic, packet) {
    try {
      await fetch(NTFY_BASE + encodeURIComponent(topic), { method: 'POST', body: JSON.stringify(packet) });
      setOnlineBadge(true);
    } catch { setOnlineBadge(false); }
  }

  async function pollNtfy(topic, sinceVal, defaultWin, onMsg) {
    try {
      const since = sinceVal > 0 ? String(sinceVal) : defaultWin;
      const res = await fetch(`${NTFY_BASE}${encodeURIComponent(topic)}/json?poll=1&since=${since}`);
      if (!res.ok) return sinceVal;
      setOnlineBadge(true);
      const txt = await res.text();
      let maxTime = sinceVal;
      for (const line of txt.split('\n')) {
        if (!line.trim()) continue;
        try {
          const obj = JSON.parse(line);
          if (obj.event !== 'message') continue;
          if (obj.id && seenIds.has(obj.id)) continue;
          if (obj.id) { if (seenIds.size > 1500) seenIds.clear(); seenIds.add(obj.id); }
          if (obj.time && obj.time > maxTime) maxTime = obj.time;
          if (obj.message) onMsg(JSON.parse(obj.message));
        } catch {}
      }
      return maxTime;
    } catch { setOnlineBadge(false); return sinceVal; }
  }

  function setOnlineBadge(ok) {
    const dot = document.getElementById('cloud-dot');
    dot.textContent = ok ? 'GLOBAL OK' : 'OFFLINE';
    dot.className = 'status-badge ' + (ok ? 'online' : 'offline');
  }

  function publishPresence() {
    const note = state.status === 'away' ? (state.awayText || state.mood) : state.mood;
    publishNtfy(dirTopic(state.room), {
      Type: 'HELLO', From: state.nick, Status: state.status,
      Avatar: state.avatar, AwayText: note, Bio: 'AIM Classic 6.0 Mobile'
    });
  }

  async function syncGlobal() {
    dirSince = await pollNtfy(dirTopic(state.room), dirSince, '10m', (p) => {
      if (!p || !p.From || p.From.toLowerCase() === state.nick.toLowerCase()) return;
      const av = mapAvatarSrc(p.Avatar);
      const ex = state.discoveredPeers.find(x => x.sn.toLowerCase() === p.From.toLowerCase());
      if (ex) { ex.status = p.Status || 'online'; ex.mood = p.AwayText || ''; ex.avatar = av; }
      else { state.discoveredPeers.push({ sn: p.From, status: p.Status || 'online', mood: p.AwayText || '', avatar: av, bio: p.Bio || '' }); playAimSound('doorOpen'); }
      const fr = state.friends.find(f => f.sn.toLowerCase() === p.From.toLowerCase());
      if (fr) { fr.status = p.Status || 'online'; fr.mood = p.AwayText || ''; fr.avatar = av; }
      renderBuddies();
    });

    pmSince = await pollNtfy(pmTopic(state.nick, state.room), pmSince, '15m', (p) => {
      if (!p || !p.From || p.From.toLowerCase() === state.nick.toLowerCase()) return;
      if (p.Type === 'IM' || p.Type === 'FRIEND_REQ') {
        if (!state.friends.some(f => f.sn.toLowerCase() === p.From.toLowerCase())) {
          state.friends.unshift({ sn: p.From, alias: '', status: 'online', isReal: true, model: '', avatar: mapAvatarSrc(p.Avatar) });
        }
        if (p.Type === 'IM') {
          pushChatMsg(p.From, p.From, p.Text || '');
          playAimSound('imReceive');
        }
      } else if (p.Type === 'MAIL') {
        state.mail.unshift({ id: 'm_' + Date.now(), from: p.From + '@aim.com', to: state.nick + '@aim.com', subj: p.Font || 'AIM Mail', date: nowStr(), read: false, body: p.Text || '' });
        saveState();
        renderMail();
        playAimSound('mail');
      } else if (p.Type === 'RING') {
        pushChatMsg(p.From, 'SYSTEM', '*** ' + p.From + ' sent Buzz / Ring!');
        triggerBuzzUi();
        playAimSound('ring');
      }
    });

    pubSince = await pollNtfy(pubDirTopic(state.room), pubSince, '12h', (p) => {
      if (p && p.Type === 'PUB_ROOM' && p.To) {
        if (!state.rooms.some(r => r.name.toLowerCase() === p.To.toLowerCase())) {
          state.rooms.push({ name: p.To, cat: p.Font || 'Global', by: p.From || 'User', topic: p.Text || '', msgs: [] });
          saveState();
          renderPublicRooms();
        }
      }
    });

    if (activeRoomName) {
      const target = activeRoomName;
      roomSince = await pollNtfy(pubRoomTopic(target, state.room), roomSince, '15m', (p) => {
        if (!p || !p.From || p.From.toLowerCase() === state.nick.toLowerCase()) return;
        const rm = state.rooms.find(r => r.name.toLowerCase() === target.toLowerCase());
        if (!rm) return;
        if (p.Type === 'ROOM_JOIN') rm.msgs.push({ sender: 'SYSTEM', text: '*** ' + p.From + ' joined!', time: nowStr() });
        else if (p.Type === 'ROOM_MSG') rm.msgs.push({ sender: p.From, text: p.Text || '', time: nowStr() });
        saveState();
        renderRoomMessages();
      });
    }
  }

  // =========================================================================
  // AI Chat Engine
  // =========================================================================
  async function generateAiReply(buddy, prompt) {
    const model = buddy.model || state.aiModel || 'gemini-4-argon';
    if (state.aiKey && state.aiEndpoint) {
      try {
        const res = await fetch(state.aiEndpoint, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', 'Authorization': 'Bearer ' + state.aiKey.trim() },
          body: JSON.stringify({
            model,
            messages: [
              { role: 'system', content: 'You are ' + buddy.sn + ' (' + model + '), a helpful AIM mobile assistant. Reply concisely in the same language (Russian or English).' },
              { role: 'user', content: prompt }
            ],
            max_tokens: 700
          })
        });
        if (res.ok) {
          const data = await res.json();
          const txt = data && data.choices && data.choices[0] && data.choices[0].message && data.choices[0].message.content;
          if (txt) return txt.trim();
        }
      } catch {}
    }
    const isRu = /[\u0400-\u04FF]/.test(prompt) || state.lang === 'ru';
    const low = prompt.toLowerCase();
    if (/привет|hello|hey|hi/.test(low)) {
      return isRu ? `Привет! Я ${buddy.sn} (${model}) на AIM Mobile :-) Вставь API-ключ в Настройках для полного облачного ИИ!`
                  : `Hey!! I'm ${buddy.sn} (${model}) on AIM Mobile :-) Add an API key in Setup for full cloud AI replies!`;
    }
    return isRu ? `Принято (${model}): «${prompt.slice(0, 60)}» :-) Добавь ключ в Setup для облачного ИИ.`
                : `Got it (${model}): "${prompt.slice(0, 60)}" :-) Add your API key in Setup for live cloud reasoning!`;
  }

  // =========================================================================
  // Rotating Promo Banners (PC AimBannerStrip Parity)
  // =========================================================================
  function handleBannerAction(action) {
    if (action === 'real') {
      switchTab('buddies');
      document.getElementById('add-friend-input').focus();
    } else if (action === 'frutiger') {
      applyThemeKey('aero', true);
      switchTab('media');
      switchHubPane('workshop');
    } else if (action === 'music') {
      switchTab('media');
      switchHubPane('music');
    } else if (action === 'workshop') {
      switchTab('media');
      switchHubPane('workshop');
    } else if (action === 'mail') {
      switchTab('media');
      switchHubPane('mail');
    } else if (action === 'support') {
      switchTab('media');
      switchHubPane('support');
    }
  }

  function renderBanners() {
    const slots = [
      { id: 'banner-slot-main', compact: false },
      { id: 'banner-slot-chat', compact: true }
    ];
    const slide = BANNER_SLIDES[bannerIndex % BANNER_SLIDES.length];
    slots.forEach(({ id, compact }) => {
      const host = document.getElementById(id);
      if (!host) return;
      if (!state.showBanners) { host.innerHTML = ''; return; }
      const title = state.lang === 'ru' ? slide.ru : slide.en;
      const sub = state.lang === 'ru' ? slide.sRu : slide.sEn;
      host.innerHTML = `
        <div class="promo-banner ${compact ? 'compact' : ''}" style="background-image:url('${slide.img}')">
          <div class="promo-banner-tag">${esc(slide.tag)}</div>
          <div class="promo-banner-txt">
            <div class="promo-banner-title">${esc(title)}</div>
            <div class="promo-banner-sub">${esc(sub)}</div>
          </div>
          <div class="promo-banner-ctrls">
            <button class="promo-ctrl-btn b-close" type="button" title="Hide banners">✕</button>
            <div style="display:flex;gap:3px">
              <button class="promo-ctrl-btn b-prev" type="button">‹</button>
              <button class="promo-ctrl-btn b-next" type="button">›</button>
            </div>
          </div>
        </div>`;
      const card = host.querySelector('.promo-banner');
      card.onclick = (e) => {
        if (e.target.closest('.promo-ctrl-btn')) return;
        handleBannerAction(slide.action);
      };
      host.querySelector('.b-prev').onclick = (e) => { e.stopPropagation(); bannerIndex = (bannerIndex - 1 + BANNER_SLIDES.length) % BANNER_SLIDES.length; renderBanners(); };
      host.querySelector('.b-next').onclick = (e) => { e.stopPropagation(); bannerIndex = (bannerIndex + 1) % BANNER_SLIDES.length; renderBanners(); };
      host.querySelector('.b-close').onclick = (e) => { e.stopPropagation(); state.showBanners = false; document.getElementById('chk-show-banners').checked = false; saveState(); renderBanners(); };
    });
  }

  // =========================================================================
  // Themes, Workshop & Localization
  // =========================================================================
  function clearCustomCssVars() {
    ['--face', '--face-dark', '--title-top', '--title-mid', '--title-bot', '--navy', '--gold', '--border'].forEach(v => {
      document.documentElement.style.removeProperty(v);
    });
  }

  function applyThemeKey(key, forceWallpaper) {
    state.theme = key || 'aero';
    if (forceWallpaper) state.aeroWallpaper = true;
    saveState();
    applyThemeAndLang();
    renderWorkshopSkins();
  }

  function applyThemeAndLang() {
    clearCustomCssVars();
    const k = state.theme || 'aero';
    if (k.startsWith('Custom:')) {
      const id = k.slice(7);
      const ct = (state.customThemes || []).find(x => x.Id === id);
      if (ct) {
        document.body.className = (state.aeroWallpaper ? 'aero-wallpaper' : '');
        const r = document.documentElement.style;
        r.setProperty('--title-top', ct.TitleTopHex || '#2eceff');
        r.setProperty('--title-mid', ct.TitleTopHex || '#18a8e0');
        r.setProperty('--title-bot', ct.TitleBotHex || '#086cb2');
        r.setProperty('--face', ct.FaceHex || '#e4f6fd');
        r.setProperty('--face-dark', ct.FaceHex || '#c6e5f5');
        r.setProperty('--gold', ct.GoldHex || '#ffcc00');
        r.setProperty('--navy', ct.TitleBotHex || '#003366');
        r.setProperty('--border', ct.TitleBotHex || '#0c8ac8');
      } else {
        document.body.className = 'theme-aero' + (state.aeroWallpaper ? ' aero-wallpaper' : '');
      }
    } else {
      document.body.className = 'theme-' + k + (state.aeroWallpaper ? ' aero-wallpaper' : '');
    }

    document.getElementById('me-name').textContent = state.nick;
    document.getElementById('me-avatar').src = mapAvatarSrc(state.avatar);
    document.getElementById('me-status').value = state.status;
    document.getElementById('me-mood').value = state.mood || '';
    document.getElementById('hdr-sub').textContent = state.room + ' • ' + state.aiModel;
    document.getElementById('btn-mute-hdr').textContent = state.muted ? '🔇' : '🔊';
    document.getElementById('chk-mute-sound').checked = !!state.muted;
    document.getElementById('chk-show-banners').checked = !!state.showBanners;
    document.getElementById('chk-aero-bg').checked = !!state.aeroWallpaper;

    // Navigation & Labels (EN / RU)
    document.getElementById('nav-buddies').textContent = T('Buddies', 'Друзья');
    document.getElementById('nav-chat').textContent = T('IM', 'Чат');
    document.getElementById('nav-public').textContent = T('Public', 'Группы');
    document.getElementById('nav-media').textContent = T('AIM Hub', 'AIM Хаб');
    document.getElementById('nav-settings').textContent = T('Setup', 'Настройки');
    document.getElementById('btn-away-edit').textContent = T('Away', 'Away');
    document.getElementById('btn-add-friend').textContent = T('+ Friend', '+ Друг');
    document.getElementById('btn-sync-peers').textContent = T('Sync', 'Поиск');
    document.getElementById('lbl-pub-hub').textContent = T('Global Public Groups', 'Публичные группы (Мир)');
    document.getElementById('subtab-music').textContent = T('♫ Music', '♫ Плеер');
    document.getElementById('subtab-workshop').textContent = T('🎨 Workshop', '🎨 Темы');
    document.getElementById('subtab-support').textContent = T('🛠 Support', '🛠 Поддержка');
    document.getElementById('subtab-updates').textContent = T('🔄 Updates', '🔄 Обновления');
    document.getElementById('sec-profile').textContent = T('Profile, Banners & Language', 'Профиль, баннеры и язык');
    document.getElementById('sec-ai').textContent = T('API Keys for AI Bots (LLM)', 'API Ключи для ИИ-ботов');
    document.getElementById('btn-save-settings').textContent = T('Save & Sync Now', 'Сохранить и обновить');

    renderMail();
    renderBanners();
    refreshDiagnostics();
    refreshGuide();
    refreshUpdatesUi();
  }

  function switchTab(tab) {
    document.querySelectorAll('.tab-page').forEach(el => el.classList.remove('active'));
    document.querySelectorAll('.nav-btn').forEach(el => el.classList.toggle('active', el.dataset.tab === tab));
    document.getElementById('view-' + tab).classList.add('active');
  }

  function switchHubPane(pane) {
    hubSubtab = pane;
    ['music', 'mail', 'workshop', 'support', 'updates'].forEach(p => {
      document.getElementById('subtab-' + p).classList.toggle('active', p === pane);
      document.getElementById('pane-' + p).classList.toggle('hidden', p !== pane);
    });
    if (pane === 'workshop') { renderWorkshopSkins(); renderWorkshopAvatars(); renderWorkshopSounds(); }
    if (pane === 'support') { refreshDiagnostics(); refreshGuide(); }
    if (pane === 'updates') { refreshUpdatesUi(); }
    if (pane === 'mail') { renderMail(); }
  }

  // =========================================================================
  // Workshop Gallery, Creator Studio & Avatar Pack
  // =========================================================================
  function renderWorkshopSkins() {
    const grid = document.getElementById('ws-skin-gallery');
    if (!grid) return;
    grid.innerHTML = '';
    const all = BUILTIN_SKINS.map(s => Object.assign({ custom: null }, s)).concat(
      (state.customThemes || []).map(ct => ({
        key: 'Custom:' + ct.Id, title: ct.Name, badge: 'WORKSHOP', desc: 'by ' + (ct.Author || 'User'),
        top: ct.TitleTopHex, bot: ct.TitleBotHex, face: ct.FaceHex, gold: ct.GoldHex, glass: !!ct.IsGlass, custom: ct
      }))
    );

    all.forEach(s => {
      const active = String(state.theme).toLowerCase() === s.key.toLowerCase();
      const card = document.createElement('div');
      card.className = 'skin-card' + (active ? ' active' : '');
      card.innerHTML = `
        <div class="mini-win-preview" style="border-color:${esc(s.bot)}">
          <div class="mini-win-bar" style="background:linear-gradient(180deg,${esc(s.top)},${esc(s.bot)});border-bottom:2px solid ${esc(s.gold)}">
            <span>AIM</span><span>${active ? '✓' : esc(s.badge)}</span>
          </div>
          <div class="mini-win-body" style="background:${esc(s.face)};color:${esc(s.bot)}">${esc(s.title)}</div>
        </div>
        <div class="skin-title">${esc(s.title)}</div>
        <div class="skin-desc">${esc(s.desc)}</div>
        <div style="display:flex;gap:4px">
          <button class="xp-btn sm ${active ? 'gold' : 'primary'} act-apply" style="flex:1" type="button">${active ? T('Applied', 'Активна') : T('Apply', 'Применить')}</button>
          ${s.custom ? '<button class="xp-btn sm danger act-del" type="button">✕</button>' : ''}
        </div>`;
      card.querySelector('.act-apply').onclick = () => {
        applyThemeKey(s.key, s.key === 'aero');
        playAimSound('ding');
      };
      const del = card.querySelector('.act-del');
      if (del) {
        del.onclick = () => {
          state.customThemes = (state.customThemes || []).filter(x => x.Id !== s.custom.Id);
          if (state.theme === s.key) state.theme = 'aero';
          saveState(); applyThemeAndLang(); renderWorkshopSkins();
        };
      }
      grid.appendChild(card);
    });
  }

  function updateStudioPreview() {
    const top = document.getElementById('ws-st-top').value;
    const bot = document.getElementById('ws-st-bot').value;
    const face = document.getElementById('ws-st-face').value;
    const gold = document.getElementById('ws-st-gold').value;
    const name = document.getElementById('ws-st-name').value || 'Preview';
    const bar = document.getElementById('ws-prev-bar');
    const body = document.getElementById('ws-prev-body');
    const box = document.getElementById('ws-st-preview');
    bar.style.background = `linear-gradient(180deg, ${top}, ${bot})`;
    bar.style.borderBottom = `2px solid ${gold}`;
    box.style.borderColor = bot;
    body.style.background = face;
    body.style.color = bot;
    body.textContent = name;
  }

  function buildCustomThemeDefFromStudio() {
    return {
      Id: Math.random().toString(16).slice(2, 10),
      Name: (document.getElementById('ws-st-name').value || '').trim() || 'Custom Skin',
      Author: (document.getElementById('ws-st-author').value || '').trim() || state.nick,
      TitleTopHex: document.getElementById('ws-st-top').value,
      TitleBotHex: document.getElementById('ws-st-bot').value,
      FaceHex: document.getElementById('ws-st-face').value,
      GoldHex: document.getElementById('ws-st-gold').value,
      IsGlass: document.getElementById('ws-st-glass').checked
    };
  }

  function renderWorkshopAvatars() {
    const grid = document.getElementById('ws-avatar-grid');
    if (!grid) return;
    grid.innerHTML = '';
    AVATAR_PACK.forEach(av => {
      const path = './avatars/' + av.id;
      const tile = document.createElement('div');
      tile.className = 'avatar-tile' + (state.avatar === path ? ' selected' : '');
      tile.innerHTML = `<img src="${path}" alt="${esc(av.label)}" /><span>${esc(av.label)}</span>`;
      tile.onclick = () => {
        state.avatar = path;
        saveState();
        applyThemeAndLang();
        renderWorkshopAvatars();
        publishPresence();
        playAimSound('pictures');
        document.getElementById('ws-pack-status').textContent = T('Applied avatar: ', 'Установлен аватар: ') + av.label;
      };
      grid.appendChild(tile);
    });
  }

  function renderWorkshopSounds() {
    const host = document.getElementById('ws-sound-grid');
    if (!host) return;
    host.innerHTML = '';
    SOUND_PACK.forEach(s => {
      const btn = document.createElement('button');
      btn.type = 'button';
      btn.className = 'xp-btn sm';
      btn.textContent = '▶ ' + s.label;
      btn.onclick = () => {
        playAimSound(s.key);
        document.getElementById('ws-pack-status').textContent = T('Playing WAV: ', 'Играет звук: ') + s.file;
      };
      host.appendChild(btn);
    });
  }

  // =========================================================================
  // Support Diagnostics, Real Friends Guide, Ticket & Updates
  // =========================================================================
  function refreshDiagnostics() {
    const el = document.getElementById('sup-diag-text');
    if (!el) return;
    const realCount = state.friends.filter(f => f.isReal).length;
    el.value = [
      `AIM Classic Mobile Diagnostics (${new Date().toLocaleString()})`,
      '----------------------------------------------------',
      `Version          : v${CURRENT_VERSION}`,
      `Active Nickname  : ${state.nick}`,
      `UI Language      : ${state.lang}`,
      `Theme Skin       : ${state.theme}`,
      `Global Room      : ${state.room}`,
      `Dir Topic        : ${dirTopic(state.room)}`,
      `Online Status    : ${state.status}`,
      `Friends Saved    : ${realCount} real / ${state.friends.length - realCount} bots`,
      `Discovered Peers : ${state.discoveredPeers.length}`,
      `Public Rooms     : ${state.rooms.length}`,
      `Mail Messages    : ${state.mail.length} (${state.mail.filter(m => !m.read).length} unread)`,
      `AI Active Model  : ${state.aiModel} (${state.aiKey ? 'API Key Set' : 'Built-In Smart Bot'})`,
      `Sound / Banners  : ${state.muted ? 'Muted' : 'Active'} / Banners=${state.showBanners}`
    ].join('\n');
  }

  function refreshGuide() {
    const el = document.getElementById('sup-guide-text');
    if (!el) return;
    el.value = T(
      'HOW TO CHAT WITH REAL PEOPLE (PC <-> PHONE)\n' +
      '==========================================\n\n' +
      '1. Make sure PC (AIMClassic.exe) and Phone are in the same Global Room Code (default: AIM-Global-2006).\n' +
      '2. Tap "+ Friend" or Sync on the Buddies tab — live users appear under "Online in Global Room".\n' +
      '3. Tap a person to send real IMs, Buzz!, or AIM Mail between Windows PC, Android & iPhone.\n' +
      '4. Join Public Groups (#Global_AIM_Plaza, #AIM_Lobby_2006) to chat with everyone at once!',
      'КАК ОБЩАТЬСЯ С РЕАЛЬНЫМИ ЛЮДЬМИ (ПК <-> ТЕЛЕФОН)\n' +
      '================================================\n\n' +
      '1. Убедитесь, что на ПК (AIMClassic.exe) и на телефоне одинаковый код комнаты (по умолчанию AIM-Global-2006).\n' +
      '2. На вкладке "Друзья" введите ник друга или нажмите "Поиск" — активные люди появятся в списке онлайн.\n' +
      '3. Нажмите на друга, чтобы писать сообщения, отправлять Buzz! и письма AIM Mail между ПК, Android и iPhone.\n' +
      '4. Заходите в Публичные группы (#Global_AIM_Plaza, #AIM_Lobby_2006) для общего чата!'
    );
  }

  function getLocalChangelog() {
    return (
      `AIM Classic v${CURRENT_VERSION} (PC + Android + iPhone Sync Build):\n` +
      '  + Mobile Android (.apk) & PWA with full PC parity\n' +
      '  + Rotating Promo Banners & 16 Classic 2005-2008 Buddy Icons\n' +
      '  + AIM Theme Workshop (7 Skins + Studio Creator + .aimtheme.json)\n' +
      '  + AOL/AIM Mail Inbox (Reply, Delete, Cloud Send) & Retro Music Player\n' +
      '  + Support Diagnostics, Away Editor, History Export & 26 AI Models\n\n' +
      'AIM Classic v5.9.3861:\n' +
      '  + Real P2P + Global ntfy Cloud Rooms & Live Chat Translator'
    );
  }

  function refreshUpdatesUi() {
    const notes = document.getElementById('upd-notes');
    if (notes && !notes.dataset.custom) notes.value = getLocalChangelog();
    const cur = document.getElementById('upd-cur-lbl');
    if (cur) cur.textContent = T(`Installed: AIM Classic v${CURRENT_VERSION} (Mobile + APK)`, `Установлено: AIM Classic v${CURRENT_VERSION} (Моб. + APK)`);
  }

  function downloadTextFile(filename, content) {
    try {
      const blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url; a.download = filename;
      document.body.appendChild(a); a.click(); document.body.removeChild(a);
      setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch {}
  }

  // =========================================================================
  // Buddies, IM, Public Groups, Music & Mail Rendering
  // =========================================================================
  function renderBuddies() {
    const host = document.getElementById('buddies-container');
    host.innerHTML = '';

    const real = state.friends.filter(f => f.isReal);
    const ai = state.friends.filter(f => !f.isReal);

    const makeGroup = (title, items, isDisc) => {
      const hdr = document.createElement('div');
      hdr.className = 'group-hdr';
      hdr.textContent = title + ' (' + items.length + ')';
      host.appendChild(hdr);
      items.forEach(b => {
        const row = document.createElement('div');
        row.className = 'buddy-item';
        const subTxt = isDisc ? T('Tap to add & chat', 'Нажмите чтобы добавить') : (b.mood || b.alias || (b.isReal ? 'Global Friend' : b.model));
        row.innerHTML = `
          <div class="buddy-left">
            <img class="buddy-av-mini" src="${esc(mapAvatarSrc(b.avatar))}" alt="" />
            <span class="b-dot ${b.status || 'online'}"></span>
            <div style="min-width:0"><div class="b-name">${esc(b.sn)}</div><div class="b-sub">${esc(subTxt || '')}</div></div>
          </div>
          <div style="display:flex;align-items:center;gap:5px">
            <button class="b-info-btn" type="button" title="Profile">ℹ</button>
            <span class="b-tag ${b.isReal || isDisc ? 'real' : ''}">${isDisc ? '+ ADD' : (b.isReal ? 'REAL' : 'AI')}</span>
          </div>`;
        row.querySelector('.b-info-btn').onclick = (e) => { e.stopPropagation(); openProfileModal(b); };
        row.onclick = () => {
          if (isDisc && !state.friends.some(x => x.sn.toLowerCase() === b.sn.toLowerCase())) {
            state.friends.unshift({ sn: b.sn, alias: b.bio || '', status: b.status || 'online', mood: b.mood || '', avatar: b.avatar || './avatars/aim_icon.gif', isReal: true, model: '' });
            saveState();
          }
          openChat(b.sn);
        };
        host.appendChild(row);
      });
    };

    makeGroup(T('Real Friends', 'Реальные друзья'), real, false);
    const discNew = state.discoveredPeers.filter(p => !state.friends.some(f => f.sn.toLowerCase() === p.sn.toLowerCase()));
    if (discNew.length) makeGroup(T('Online in Global Room', 'Онлайн в мире сейчас'), discNew, true);
    makeGroup(T('AI Work & Chat Helpers', 'ИИ-боты для работы'), ai, false);
  }

  function openChat(sn) {
    activeChatSn = sn;
    const b = state.friends.find(x => x.sn.toLowerCase() === sn.toLowerCase()) || { sn, isReal: true, model: '' };
    document.getElementById('chat-peer-name').textContent = b.sn;
    document.getElementById('chat-peer-sub').textContent = b.isReal ? T('Real Global Friend', 'Реальный друг') : (b.alias || 'AI Assistant');
    document.getElementById('chat-model-badge').textContent = b.isReal ? 'Global Cloud' : (b.model || state.aiModel);
    renderChatMessages();
    switchTab('chat');
  }

  function pushChatMsg(sn, sender, text) {
    if (!state.chats[sn]) state.chats[sn] = [];
    state.chats[sn].push({ sender, text, time: nowStr() });
    if (state.chats[sn].length > 120) state.chats[sn].shift();
    saveState();
    if (activeChatSn && activeChatSn.toLowerCase() === sn.toLowerCase()) renderChatMessages();
  }

  function renderChatMessages() {
    const box = document.getElementById('chat-messages');
    const list = state.chats[activeChatSn] || [];
    box.innerHTML = list.map(m => {
      if (m.sender === 'SYSTEM') return `<div class="msg-line msg-sys">${esc(m.text)}</div>`;
      const cls = m.sender === state.nick ? 'msg-sender-me' : 'msg-sender-them';
      return `<div class="msg-line"><span class="msg-ts">(${esc(m.time)})</span><span class="${cls}">${esc(m.sender)}:</span> ${esc(m.text)}</div>`;
    }).join('') || `<div class="msg-sys">Say hi to ${esc(activeChatSn)}! :-)</div>`;
    box.scrollTop = box.scrollHeight;
  }

  async function sendIm() {
    const inp = document.getElementById('chat-input');
    const txt = inp.value.trim();
    if (!txt) return;
    inp.value = '';
    if (txt === '/roll') { pushChatMsg(activeChatSn, 'SYSTEM', '🎲 Rolled ' + (1 + Math.floor(Math.random() * 100))); return; }
    if (txt === '/8ball') { pushChatMsg(activeChatSn, 'SYSTEM', '🎱 Magic 8-Ball: Yes, definitely :-D'); return; }

    pushChatMsg(activeChatSn, state.nick, txt);
    playAimSound('imSend');
    const b = state.friends.find(x => x.sn.toLowerCase() === activeChatSn.toLowerCase()) || { sn: activeChatSn, isReal: true };
    if (b.isReal) {
      publishNtfy(pmTopic(b.sn, state.room), { Type: 'IM', From: state.nick, To: b.sn, Avatar: state.avatar, Text: txt });
    } else {
      const reply = await generateAiReply(b, txt);
      pushChatMsg(b.sn, b.sn, reply);
      playAimSound('imReceive');
    }
  }

  function triggerBuzzUi() {
    const shell = document.getElementById('app');
    shell.classList.remove('buzzing');
    void shell.offsetWidth;
    shell.classList.add('buzzing');
    if (navigator.vibrate) navigator.vibrate([90, 50, 90]);
  }

  function renderPublicRooms() {
    const q = (document.getElementById('room-search').value || '').toLowerCase();
    const list = document.getElementById('public-rooms-list');
    list.innerHTML = '';
    state.rooms.filter(r => !q || r.name.toLowerCase().includes(q) || (r.topic || '').toLowerCase().includes(q)).forEach(r => {
      const el = document.createElement('div');
      el.className = 'buddy-item';
      el.innerHTML = `<div style="min-width:0"><div class="b-name">${esc(r.name)}</div><div class="b-sub">${esc(r.topic || '')}</div></div><span class="b-tag">${esc(r.cat || 'Global')}</span>`;
      el.onclick = () => joinPublicRoom(r.name);
      list.appendChild(el);
    });
  }

  function joinPublicRoom(name) {
    activeRoomName = name;
    roomSince = 0;
    const rm = state.rooms.find(r => r.name.toLowerCase() === name.toLowerCase());
    if (!rm) return;
    document.getElementById('public-hub-view').classList.add('hidden');
    document.getElementById('public-room-view').classList.remove('hidden');
    document.getElementById('room-active-name').textContent = rm.name;
    document.getElementById('room-active-topic').textContent = rm.topic || 'Public Room';
    publishNtfy(pubRoomTopic(rm.name, state.room), { Type: 'ROOM_JOIN', From: state.nick, To: rm.name, Text: state.nick });
    playAimSound('chatJoin');
    renderRoomMessages();
    syncGlobal();
  }

  function renderRoomMessages() {
    const rm = state.rooms.find(r => r.name === activeRoomName);
    if (!rm) return;
    const box = document.getElementById('room-messages');
    box.innerHTML = (rm.msgs || []).map(m => {
      if (m.sender === 'SYSTEM') return `<div class="msg-line msg-sys">${esc(m.text)}</div>`;
      const cls = m.sender === state.nick ? 'msg-sender-me' : 'msg-sender-them';
      return `<div class="msg-line"><span class="msg-ts">(${esc(m.time)})</span><span class="${cls}">${esc(m.sender)}:</span> ${esc(m.text)}</div>`;
    }).join('') || `<div class="msg-sys">*** Welcome to ${esc(rm.name)}!</div>`;
    box.scrollTop = box.scrollHeight;
  }

  async function sendRoomMsg() {
    const inp = document.getElementById('room-input');
    const txt = inp.value.trim();
    const rm = state.rooms.find(r => r.name === activeRoomName);
    if (!txt || !rm) return;
    inp.value = '';
    if (txt === '/roll') {
      const s = `🎲 ${state.nick} rolled ${1 + Math.floor(Math.random() * 100)}!`;
      rm.msgs.push({ sender: 'SYSTEM', text: s, time: nowStr() });
      saveState(); renderRoomMessages(); return;
    }
    rm.msgs.push({ sender: state.nick, text: txt, time: nowStr() });
    saveState(); renderRoomMessages();
    publishNtfy(pubRoomTopic(rm.name, state.room), { Type: 'ROOM_MSG', From: state.nick, To: rm.name, Text: txt });
    if (document.getElementById('chk-room-ai').checked) {
      const r = await generateAiReply({ sn: 'AIM_AI_Helper', model: state.aiModel }, txt);
      rm.msgs.push({ sender: 'AIM_AI_Helper', text: r, time: nowStr() });
      saveState(); renderRoomMessages();
    }
  }

  function updateMusicUi() {
    const tr = playlist[trackIdx];
    document.getElementById('oled-state').textContent = isPlaying ? 'PLAYING ♪' : 'READY';
    document.getElementById('oled-title').textContent = tr ? tr.title : 'No Track';
    document.getElementById('oled-artist').textContent = tr ? tr.artist : '';
    const box = document.getElementById('playlist-box');
    box.innerHTML = '';
    playlist.forEach((p, idx) => {
      const d = document.createElement('div');
      d.className = 'buddy-item';
      d.innerHTML = `<div><div class="b-name">${idx === trackIdx && isPlaying ? '▶ ' : ''}${esc(p.title)}</div><div class="b-sub">${esc(p.artist)}</div></div><span class="b-tag">${p.synth ? 'SYNTH' : 'MP3'}</span>`;
      d.onclick = () => playTrack(idx);
      box.appendChild(d);
    });
  }

  function renderMail() {
    const unread = state.mail.filter(m => !m.read).length;
    document.getElementById('subtab-mail').textContent = `✉ Mail (${unread})`;
    document.getElementById('mail-banner-tag').textContent = unread > 0
      ? T(`You've got mail! (${unread} unread)`, `Вам письмо! (${unread} новых)`)
      : T('AIM® Mail Inbox — All Read', 'AIM® Почта — Всё прочитано');
    const list = document.getElementById('mail-list');
    list.innerHTML = '';
    state.mail.forEach(m => {
      const el = document.createElement('div');
      el.className = 'buddy-item';
      el.innerHTML = `<div style="min-width:0"><div class="b-name">${!m.read ? '● ' : ''}${esc(m.subj)}</div><div class="b-sub">${esc(m.from)} — ${esc(m.date)}</div></div><span class="b-tag">${!m.read ? 'NEW' : 'READ'}</span>`;
      el.onclick = () => selectMail(m);
      list.appendChild(el);
    });
    if (!selectedMailId && state.mail.length > 0) selectMail(state.mail[0], true);
  }

  function selectMail(m, keepUnread) {
    selectedMailId = m.id;
    if (!keepUnread && !m.read) { m.read = true; saveState(); renderMail(); }
    document.getElementById('mail-reader').classList.remove('hidden');
    document.getElementById('mail-r-subj').textContent = m.subj;
    document.getElementById('mail-r-from').textContent = T('From: ', 'От: ') + (m.from || '');
    document.getElementById('mail-r-to').textContent = T('To: ', 'Кому: ') + (m.to || state.nick + '@aim.com');
    document.getElementById('mail-r-date').textContent = m.date || '';
    document.getElementById('mail-r-body').textContent = m.body || '';
  }

  function openComposeMailModal(presetTo, presetSubj) {
    openModal(T('Write AIM Mail', 'Новое письмо AIM Mail'),
      `<label class="fld-lbl">${T('To (ScreenName or @aim.com):', 'Кому (ник или email):')}</label>
       <input id="m-to" class="xp-input" value="${esc(presetTo || '')}" placeholder="CoolGamer_07" />
       <label class="fld-lbl">${T('Subject:', 'Тема:')}</label>
       <input id="m-sub" class="xp-input" value="${esc(presetSubj || '')}" placeholder="Hello from AIM Mobile" />
       <label class="fld-lbl">${T('Message:', 'Сообщение:')}</label>
       <textarea id="m-body" class="xp-input" rows="4" placeholder="Message..."></textarea>
       <div class="row-between mt-8"><button id="m-send" class="xp-btn primary">${T('Send Mail', 'Отправить')}</button><button id="m-can" class="xp-btn">${T('Cancel', 'Отмена')}</button></div>`,
      (close) => {
        document.getElementById('m-can').onclick = close;
        document.getElementById('m-send').onclick = () => {
          const to = document.getElementById('m-to').value.trim();
          const sub = document.getElementById('m-sub').value.trim() || '(No Subject)';
          const body = document.getElementById('m-body').value;
          if (!to) return;
          const cleanNick = to.includes('@') ? to.split('@')[0] : to;
          publishNtfy(pmTopic(cleanNick, state.room), { Type: 'MAIL', From: state.nick, To: cleanNick, Font: sub, Text: body });
          const newM = { id: 's_' + Date.now(), from: state.nick + '@aim.com (Sent)', to, subj: '[Sent] ' + sub, date: nowStr(), read: true, body };
          state.mail.unshift(newM);
          saveState(); renderMail(); selectMail(newM, true); playAimSound('mail'); close();
        };
      });
  }

  // =========================================================================
  // Modals: Profile Info, Away Message Editor, Chat History Viewer
  // =========================================================================
  function openModal(title, html, onBind) {
    document.getElementById('modal-title').textContent = title;
    document.getElementById('modal-body').innerHTML = html;
    document.getElementById('modal-overlay').classList.remove('hidden');
    const close = () => document.getElementById('modal-overlay').classList.add('hidden');
    document.getElementById('modal-x').onclick = close;
    onBind(close);
  }

  function openProfileModal(b) {
    openModal(T('Buddy Info: ', 'Профиль: ') + b.sn,
      `<div class="user-row">
         <img src="${esc(mapAvatarSrc(b.avatar))}" class="my-avatar" alt="" />
         <div>
           <div><strong>${esc(b.sn)}</strong></div>
           <div class="muted">${b.isReal ? T('Real Global User', 'Реальный пользователь') : ('AI Bot · ' + (b.model || state.aiModel))}</div>
           <div class="small-txt mt-6">${esc(b.mood || b.alias || 'AIM Classic User')}</div>
         </div>
       </div>
       <div class="transport-row mt-8">
         <button id="pr-im" class="xp-btn primary sm" type="button">IM</button>
         <button id="pr-mail" class="xp-btn sm" type="button">Mail</button>
         ${b.isReal ? `<button id="pr-del" class="xp-btn danger sm" type="button">${T('Remove', 'Удалить')}</button>` : ''}
         <button id="pr-ok" class="xp-btn sm" type="button">${T('Close', 'Закрыть')}</button>
       </div>`,
      (close) => {
        document.getElementById('pr-ok').onclick = close;
        document.getElementById('pr-im').onclick = () => { close(); openChat(b.sn); };
        document.getElementById('pr-mail').onclick = () => { close(); switchTab('media'); switchHubPane('mail'); openComposeMailModal(b.sn, 'Hello!'); };
        const del = document.getElementById('pr-del');
        if (del) {
          del.onclick = () => {
            state.friends = state.friends.filter(x => x.sn.toLowerCase() !== b.sn.toLowerCase());
            saveState(); renderBuddies(); close();
          };
        }
      });
  }

  function openAwayModal() {
    openModal(T('Away Message Editor', 'Редактор Away-сообщений'),
      `<label class="fld-lbl">${T('Presets:', 'Готовые шаблоны:')}</label>
       <select id="aw-preset" class="xp-input">
         ${AWAY_PRESETS.map((p, i) => `<option value="${i}">${esc(p.title)}</option>`).join('')}
       </select>
       <label class="fld-lbl">${T('Away Message:', 'Текст статуса Away:')}</label>
       <textarea id="aw-txt" class="xp-input" rows="3">${esc(state.awayText || AWAY_PRESETS[0].msg)}</textarea>
       <div class="row-between mt-8">
         <button id="aw-set" class="xp-btn primary">${T('Set Away Now', 'Включить Away')}</button>
         <button id="aw-back" class="xp-btn gold">${T("I'm Back", 'Я вернулся')}</button>
         <button id="aw-can" class="xp-btn">${T('Close', 'Закрыть')}</button>
       </div>`,
      (close) => {
        document.getElementById('aw-can').onclick = close;
        document.getElementById('aw-preset').onchange = (e) => {
          document.getElementById('aw-txt').value = AWAY_PRESETS[Number(e.target.value)].msg;
        };
        document.getElementById('aw-set').onclick = () => {
          state.awayText = document.getElementById('aw-txt').value.trim() || AWAY_PRESETS[0].msg;
          state.status = 'away';
          state.mood = '✎ ' + state.awayText;
          saveState(); applyThemeAndLang(); publishPresence(); close();
        };
        document.getElementById('aw-back').onclick = () => {
          state.status = 'online';
          state.mood = 'On AIM Mobile 6.0! :-)';
          saveState(); applyThemeAndLang(); publishPresence(); close();
        };
      });
  }

  function openHistoryModal() {
    const keys = Object.keys(state.chats || {});
    const dump = keys.map(k => `=== Chat with ${k} ===\n` + (state.chats[k] || []).map(m => `[${m.time}] ${m.sender}: ${m.text}`).join('\n')).join('\n\n') || 'No chat history saved yet.';
    openModal(T('Chat History Viewer', 'Архив истории чатов'),
      `<textarea id="hist-dump" class="diag-box" style="height:220px" readonly>${esc(dump)}</textarea>
       <div class="row-between mt-8">
         <button id="hi-dl" class="xp-btn primary sm">${T('Download .txt', 'Скачать .txt')}</button>
         <button id="hi-clr" class="xp-btn danger sm">${T('Clear', 'Очистить')}</button>
         <button id="hi-ok" class="xp-btn sm">${T('Close', 'Закрыть')}</button>
       </div>`,
      (close) => {
        document.getElementById('hi-ok').onclick = close;
        document.getElementById('hi-dl').onclick = () => downloadTextFile('AIM_Chat_History.txt', dump);
        document.getElementById('hi-clr').onclick = () => {
          state.chats = {}; saveState(); renderChatMessages(); document.getElementById('hist-dump').value = 'History cleared.';
        };
      });
  }

  // =========================================================================
  // Bootstrap Event Handlers
  // =========================================================================
  function init() {
    if ('serviceWorker' in navigator) navigator.serviceWorker.register('./sw.js').catch(() => {});
    window.addEventListener('beforeinstallprompt', (e) => {
      e.preventDefault(); deferredInstall = e;
      document.getElementById('btn-install-banner').classList.remove('hidden');
    });
    document.getElementById('btn-install-banner').onclick = async () => {
      if (deferredInstall) { deferredInstall.prompt(); await deferredInstall.userChoice; deferredInstall = null; }
    };

    const eq = document.getElementById('eq-bars');
    for (let i = 0; i < 16; i++) { const b = document.createElement('div'); b.className = 'eq-bar'; eq.appendChild(b); }
    setInterval(() => {
      eq.querySelectorAll('.eq-bar').forEach(b => { b.style.height = isPlaying ? (6 + Math.floor(Math.random() * 28)) + 'px' : '4px'; });
    }, 120);

    const selMod = document.getElementById('set-ai-model');
    MODEL_CATALOG.forEach(m => { const o = document.createElement('option'); o.value = m; o.textContent = m; selMod.appendChild(o); });
    document.getElementById('set-nick').value = state.nick;
    document.getElementById('set-room').value = state.room;
    document.getElementById('set-lang').value = state.lang;
    document.getElementById('set-theme').value = state.theme;
    selMod.value = MODEL_CATALOG.includes(state.aiModel) ? state.aiModel : MODEL_CATALOG[0];
    document.getElementById('set-ai-endpoint').value = state.aiEndpoint || '';
    document.getElementById('set-ai-provider').onchange = (e) => { document.getElementById('set-ai-endpoint').value = e.target.value; };
    document.getElementById('set-ai-key').value = state.aiKey || '';
    document.getElementById('chk-show-key').onchange = (e) => {
      document.getElementById('set-ai-key').type = e.target.checked ? 'text' : 'password';
    };

    document.querySelectorAll('.nav-btn').forEach(b => { b.onclick = () => switchTab(b.dataset.tab); });
    document.getElementById('btn-mute-hdr').onclick = () => {
      state.muted = !state.muted;
      if (htmlAudio) htmlAudio.muted = !!state.muted;
      saveState(); applyThemeAndLang();
    };
    document.getElementById('me-avatar').onclick = () => { switchTab('media'); switchHubPane('workshop'); document.getElementById('ws-tab-packs').click(); };
    document.getElementById('btn-away-edit').onclick = openAwayModal;
    document.getElementById('btn-back-buddies').onclick = () => switchTab('buddies');
    document.getElementById('btn-peer-info').onclick = () => {
      const b = state.friends.find(x => x.sn.toLowerCase() === activeChatSn.toLowerCase()) || { sn: activeChatSn, isReal: true };
      openProfileModal(b);
    };
    document.getElementById('btn-send-im').onclick = sendIm;
    document.getElementById('chat-input').onkeydown = (e) => { if (e.key === 'Enter') sendIm(); };
    document.querySelectorAll('.emo-chip').forEach(c => { c.onclick = () => { const i = document.getElementById('chat-input'); i.value += (i.value ? ' ' : '') + c.dataset.emo + ' '; i.focus(); }; });
    document.getElementById('btn-buzz').onclick = () => {
      triggerBuzzUi(); playAimSound('ring');
      publishNtfy(pmTopic(activeChatSn, state.room), { Type: 'RING', From: state.nick, To: activeChatSn });
      pushChatMsg(activeChatSn, 'SYSTEM', '*** Sent Buzz!');
    };
    document.getElementById('btn-translate-chat').onclick = () => {
      const list = state.chats[activeChatSn] || [];
      const last = list.filter(m => m.sender !== 'SYSTEM').pop();
      if (!last) return;
      const toRu = !/[\u0400-\u04FF]/.test(last.text);
      pushChatMsg(activeChatSn, 'SYSTEM', `[${toRu ? 'RU' : 'EN'}] ` + last.text);
    };

    document.getElementById('btn-add-friend').onclick = () => {
      const inp = document.getElementById('add-friend-input');
      const sn = inp.value.trim();
      if (sn.length < 2) return;
      inp.value = '';
      if (!state.friends.some(f => f.sn.toLowerCase() === sn.toLowerCase())) {
        state.friends.unshift({ sn, alias: '', status: 'online', isReal: true, model: '', avatar: './avatars/aim_icon.gif' });
        saveState();
        publishNtfy(pmTopic(sn, state.room), { Type: 'FRIEND_REQ', From: state.nick, To: sn, Avatar: state.avatar });
      }
      renderBuddies();
    };
    document.getElementById('btn-sync-peers').onclick = () => { publishPresence(); syncGlobal(); };
    document.getElementById('me-status').onchange = (e) => { state.status = e.target.value; saveState(); publishPresence(); };
    document.getElementById('me-mood').onchange = (e) => { state.mood = e.target.value; saveState(); publishPresence(); };

    // Public groups
    document.getElementById('room-search').oninput = renderPublicRooms;
    document.getElementById('btn-refresh-rooms').onclick = syncGlobal;
    document.getElementById('btn-leave-room').onclick = () => {
      activeRoomName = null;
      playAimSound('chatLeave');
      document.getElementById('public-room-view').classList.add('hidden');
      document.getElementById('public-hub-view').classList.remove('hidden');
    };
    document.getElementById('btn-send-room').onclick = sendRoomMsg;
    document.getElementById('room-input').onkeydown = (e) => { if (e.key === 'Enter') sendRoomMsg(); };
    document.getElementById('btn-new-room').onclick = () => {
      openModal(T('Create Public Group', 'Создать публичную группу'),
        `<input id="m-rname" class="xp-input" placeholder="#My_Public_Group" value="#Global_Friends" />
         <input id="m-rtop" class="xp-input mt-6" placeholder="Topic..." value="Welcome everyone!" />
         <div class="row-between mt-8"><button id="m-ok" class="xp-btn primary">${T('Create', 'Создать')}</button><button id="m-can" class="xp-btn">${T('Cancel', 'Отмена')}</button></div>`,
        (close) => {
          document.getElementById('m-can').onclick = close;
          document.getElementById('m-ok').onclick = () => {
            let name = document.getElementById('m-rname').value.trim();
            if (!name) return;
            if (!name.startsWith('#')) name = '#' + name;
            const topic = document.getElementById('m-rtop').value.trim();
            state.rooms.unshift({ name, cat: 'Global', by: state.nick, topic, msgs: [] });
            saveState();
            publishNtfy(pubDirTopic(state.room), { Type: 'PUB_ROOM', From: state.nick, To: name, Font: 'Global', Text: topic });
            close(); renderPublicRooms(); joinPublicRoom(name);
          };
        });
    };

    // Hub subtabs
    ['music', 'mail', 'workshop', 'support', 'updates'].forEach(p => {
      document.getElementById('subtab-' + p).onclick = () => switchHubPane(p);
    });

    // Music
    document.getElementById('btn-m-play').onclick = () => playTrack(trackIdx);
    document.getElementById('btn-m-stop').onclick = stopMusic;
    document.getElementById('btn-m-next').onclick = () => playTrack((trackIdx + 1) % playlist.length);
    document.getElementById('btn-m-prev').onclick = () => playTrack((trackIdx - 1 + playlist.length) % playlist.length);
    document.getElementById('file-add-audio').onchange = (e) => {
      Array.from(e.target.files || []).forEach(f => {
        playlist.push({ id: 'u_' + Date.now(), title: f.name.replace(/\.[^.]+$/, ''), artist: 'Phone Audio', url: URL.createObjectURL(f), synth: false });
      });
      updateMusicUi();
    };

    // Mail
    document.getElementById('btn-check-mail').onclick = () => { syncGlobal(); playAimSound('ding'); };
    document.getElementById('btn-compose-mail').onclick = () => openComposeMailModal('', '');
    document.getElementById('btn-mail-reply').onclick = () => {
      const m = state.mail.find(x => x.id === selectedMailId);
      if (!m) return;
      const subj = m.subj.startsWith('Re:') ? m.subj : ('Re: ' + m.subj);
      openComposeMailModal(m.from.replace(' (Sent)', ''), subj);
    };
    document.getElementById('btn-mail-mark').onclick = () => {
      const m = state.mail.find(x => x.id === selectedMailId);
      if (m) { m.read = !m.read; saveState(); renderMail(); }
    };
    document.getElementById('btn-mail-del').onclick = () => {
      if (!selectedMailId) return;
      state.mail = state.mail.filter(x => x.id !== selectedMailId);
      selectedMailId = null;
      document.getElementById('mail-reader').classList.add('hidden');
      saveState(); renderMail();
    };

    // Workshop subtabs & controls
    ['skins', 'studio', 'packs'].forEach(sp => {
      document.getElementById('ws-tab-' + sp).onclick = () => {
        ['skins', 'studio', 'packs'].forEach(x => {
          document.getElementById('ws-tab-' + x).classList.toggle('active', x === sp);
          document.getElementById('ws-pane-' + x).classList.toggle('hidden', x !== sp);
        });
      };
    });
    document.getElementById('chk-aero-bg').onchange = (e) => {
      state.aeroWallpaper = e.target.checked; saveState(); applyThemeAndLang();
    };
    ['ws-st-name', 'ws-st-top', 'ws-st-bot', 'ws-st-face', 'ws-st-gold'].forEach(id => {
      document.getElementById(id).oninput = updateStudioPreview;
    });
    updateStudioPreview();
    document.getElementById('btn-ws-save').onclick = () => {
      const def = buildCustomThemeDefFromStudio();
      state.customThemes = state.customThemes || [];
      state.customThemes.push(def);
      applyThemeKey('Custom:' + def.Id, false);
      document.getElementById('ws-tab-skins').click();
      playAimSound('welcome');
    };
    document.getElementById('btn-ws-export').onclick = () => {
      const def = buildCustomThemeDefFromStudio();
      downloadTextFile(def.Name.replace(/\s+/g, '_') + '.aimtheme.json', JSON.stringify(def, null, 2));
    };
    document.getElementById('file-ws-import').onchange = (e) => {
      const f = e.target.files && e.target.files[0];
      if (!f) return;
      const r = new FileReader();
      r.onload = () => {
        try {
          const imp = JSON.parse(r.result);
          if (!imp || !imp.Name) throw new Error('Invalid JSON');
          imp.Id = Math.random().toString(16).slice(2, 10);
          state.customThemes = state.customThemes || [];
          state.customThemes.push(imp);
          applyThemeKey('Custom:' + imp.Id, false);
          document.getElementById('ws-tab-skins').click();
        } catch {}
      };
      r.readAsText(f);
    };
    document.getElementById('file-av-import').onchange = (e) => {
      const f = e.target.files && e.target.files[0];
      if (!f) return;
      const r = new FileReader();
      r.onload = () => {
        state.avatar = r.result; saveState(); applyThemeAndLang(); publishPresence(); playAimSound('ding');
        document.getElementById('ws-pack-status').textContent = T('Custom icon loaded!', 'Свой аватар установлен!');
      };
      r.readAsDataURL(f);
    };
    document.getElementById('btn-snd-welcome').onclick = () => playAimSound('welcome');
    document.getElementById('btn-snd-mail').onclick = () => playAimSound('mail');
    document.getElementById('btn-snd-ring').onclick = () => playAimSound('ring');
    document.getElementById('btn-snd-bye').onclick = () => playAimSound('goodbye');

    // Support Center subtabs & controls
    ['diag', 'guide', 'ticket'].forEach(sp => {
      document.getElementById('sup-tab-' + sp).onclick = () => {
        ['diag', 'guide', 'ticket'].forEach(x => {
          document.getElementById('sup-tab-' + x).classList.toggle('active', x === sp);
          document.getElementById('sup-pane-' + x).classList.toggle('hidden', x !== sp);
        });
      };
    });
    document.getElementById('btn-sup-refresh').onclick = () => { refreshDiagnostics(); playAimSound('ding'); };
    document.getElementById('btn-sup-sound').onclick = () => playAimSound('welcome');
    document.getElementById('btn-sup-copy').onclick = () => {
      if (navigator.clipboard) navigator.clipboard.writeText(document.getElementById('sup-diag-text').value).catch(() => {});
    };
    document.getElementById('btn-sup-save').onclick = () => { downloadTextFile('AIM_Diagnostics.txt', document.getElementById('sup-diag-text').value); playAimSound('filesDone'); };
    document.getElementById('btn-open-away').onclick = openAwayModal;
    document.getElementById('btn-open-history').onclick = openHistoryModal;
    document.getElementById('btn-open-history2').onclick = openHistoryModal;
    document.getElementById('btn-sup-ticket').onclick = () => {
      const cat = document.getElementById('sup-t-cat').value;
      const sub = document.getElementById('sup-t-subj').value.trim();
      const body = document.getElementById('sup-t-body').value.trim();
      if (!sub || !body) return;
      refreshDiagnostics();
      const txt = `Category: ${cat}\nSubject: ${sub}\n\nDescription:\n${body}\n\n--- DIAGNOSTICS ---\n${document.getElementById('sup-diag-text').value}`;
      downloadTextFile('AIM_Support_Ticket.txt', txt);
      playAimSound('filesDone');
    };

    // Update Center controls
    document.getElementById('upd-channel').value = state.updChannel || 'Stable (v6.0)';
    document.getElementById('upd-chk-auto').checked = state.updAuto !== false;
    document.getElementById('upd-manifest-url').value = state.updManifestUrl || '';
    document.getElementById('btn-upd-check').onclick = async () => {
      state.updChannel = document.getElementById('upd-channel').value;
      state.updAuto = document.getElementById('upd-chk-auto').checked;
      state.updManifestUrl = document.getElementById('upd-manifest-url').value.trim();
      saveState();
      const prog = document.getElementById('upd-prog-bar');
      const st = document.getElementById('upd-status');
      const notes = document.getElementById('upd-notes');
      const dlBtn = document.getElementById('btn-upd-download');
      prog.style.width = '30%';
      st.textContent = T('Checking for updates...', 'Проверка обновлений...');
      dlBtn.classList.add('hidden');
      if (state.updManifestUrl) {
        try {
          const r = await fetch(state.updManifestUrl);
          if (r.ok) {
            const j = await r.json();
            prog.style.width = '100%';
            if (j.version && j.version !== CURRENT_VERSION) {
              foundUpdateUrl = j.url || '';
              st.textContent = T('Update available: v', 'Доступно обновление: v') + j.version;
              notes.dataset.custom = '1';
              notes.value = `NEW VERSION: v${j.version}\n${j.notes || ''}\n\n${getLocalChangelog()}`;
              if (foundUpdateUrl) dlBtn.classList.remove('hidden');
              playAimSound('ding');
              return;
            }
          }
        } catch {}
      }
      setTimeout(() => {
        prog.style.width = '100%';
        st.textContent = T(`You have the latest AIM Mobile v${CURRENT_VERSION}!`, `Установлена актуальная версия AIM v${CURRENT_VERSION}!`);
        playAimSound('welcome');
      }, 350);
    };
    document.getElementById('btn-upd-download').onclick = () => { if (foundUpdateUrl) window.open(foundUpdateUrl, '_blank'); };

    // Settings
    document.getElementById('btn-test-api').onclick = async () => {
      const st = document.getElementById('api-test-status');
      state.aiModel = selMod.value;
      state.aiEndpoint = document.getElementById('set-ai-endpoint').value.trim();
      state.aiKey = document.getElementById('set-ai-key').value.trim();
      st.textContent = T('Testing AI connection...', 'Проверка API ключа...');
      const reply = await generateAiReply({ sn: 'TestBot', model: state.aiModel }, 'Say OK in 3 words.');
      st.textContent = T('Result: ', 'Ответ: ') + reply.slice(0, 90);
    };

    document.getElementById('btn-save-settings').onclick = () => {
      state.nick = document.getElementById('set-nick').value.trim() || state.nick;
      state.room = document.getElementById('set-room').value.trim() || 'AIM-Global-2006';
      state.lang = document.getElementById('set-lang').value;
      state.theme = document.getElementById('set-theme').value;
      state.showBanners = document.getElementById('chk-show-banners').checked;
      state.muted = document.getElementById('chk-mute-sound').checked;
      state.aiModel = selMod.value;
      state.aiEndpoint = document.getElementById('set-ai-endpoint').value.trim() || document.getElementById('set-ai-provider').value;
      state.aiKey = document.getElementById('set-ai-key').value.trim();
      saveState(); applyThemeAndLang(); publishPresence(); syncGlobal();
      playAimSound('ding');
      switchTab('buddies');
    };

    fetch('./api/bootstrap').then(r => r.ok ? r.json() : null).then(pc => {
      if (pc && pc.room && !localStorage.getItem(STORAGE_KEY)) {
        state.room = pc.room;
        if (pc.lang) state.lang = pc.lang;
        saveState();
        document.getElementById('set-room').value = state.room;
        document.getElementById('set-lang').value = state.lang;
        applyThemeAndLang();
      }
    }).catch(() => {});

    applyThemeAndLang();
    renderBuddies();
    renderPublicRooms();
    updateMusicUi();
    renderMail();
    renderWorkshopSkins();
    renderWorkshopAvatars();
    renderWorkshopSounds();
    publishPresence();
    syncGlobal();
    setInterval(publishPresence, 18000);
    setInterval(syncGlobal, 4500);
    setInterval(() => { bannerIndex = (bannerIndex + 1) % BANNER_SLIDES.length; renderBanners(); }, 10000);
  }

  init();
})();
