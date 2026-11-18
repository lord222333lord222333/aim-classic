import 'dart:async';
import 'dart:convert';
import 'dart:math';
import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'protocol.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const AimClassicMobileApp());
}

class _ThemePalette {
  final String key;
  final String name;
  final Color primary;
  final Color dark;
  final Color bg;
  final Color card;
  final Color gold;

  const _ThemePalette({
    required this.key,
    required this.name,
    required this.primary,
    required this.dark,
    required this.bg,
    required this.card,
    required this.gold,
  });
}

const Map<String, _ThemePalette> kThemes = {
  'aero': _ThemePalette(
    key: 'aero',
    name: 'Frutiger Aero',
    primary: Color(0xFF0A84E6),
    dark: Color(0xFF04448C),
    bg: Color(0xFFE4F4FF),
    card: Color(0xFFF6FBFF),
    gold: Color(0xFFFFCC00),
  ),
  'luna': _ThemePalette(
    key: 'luna',
    name: 'XP Luna Blue',
    primary: Color(0xFF127DFF),
    dark: Color(0xFF003DB8),
    bg: Color(0xFFECE9D8),
    card: Color(0xFFFFFFFF),
    gold: Color(0xFFFFCC00),
  ),
  'silver': _ThemePalette(
    key: 'silver',
    name: 'XP Silver',
    primary: Color(0xFF6B778D),
    dark: Color(0xFF3E495C),
    bg: Color(0xFFE8EAEF),
    card: Color(0xFFFAFBFD),
    gold: Color(0xFFFFD24D),
  ),
  'ice': _ThemePalette(
    key: 'ice',
    name: 'AIM Ice',
    primary: Color(0xFF1E9BD7),
    dark: Color(0xFF0B5886),
    bg: Color(0xFFEAF7FC),
    card: Color(0xFFFFFFFF),
    gold: Color(0xFFFFCF33),
  ),
  'royale': _ThemePalette(
    key: 'royale',
    name: 'XP Royale',
    primary: Color(0xFF2865B6),
    dark: Color(0xFF113A7E),
    bg: Color(0xFFE8EEF7),
    card: Color(0xFFFFFFFF),
    gold: Color(0xFFFFD438),
  ),
  'olive': _ThemePalette(
    key: 'olive',
    name: 'XP Olive',
    primary: Color(0xFF9BA865),
    dark: Color(0xFF6D7942),
    bg: Color(0xFFF0EEE0),
    card: Color(0xFFFFFEF9),
    gold: Color(0xFFFFCF33),
  ),
  'neon': _ThemePalette(
    key: 'neon',
    name: 'Y2K Neon',
    primary: Color(0xFF6319D4),
    dark: Color(0xFF270B6B),
    bg: Color(0xFFEFEAFD),
    card: Color(0xFFFFFFFF),
    gold: Color(0xFF3CF4FF),
  ),
};

class _MailEntry {
  final String from;
  final String subject;
  final String body;
  final String date;
  bool read;
  _MailEntry({required this.from, required this.subject, required this.body, required this.date, this.read = false});
}

class AimClassicMobileApp extends StatefulWidget {
  const AimClassicMobileApp({super.key});

  @override
  State<AimClassicMobileApp> createState() => _AimClassicMobileAppState();
}

class _AimClassicMobileAppState extends State<AimClassicMobileApp> {
  String _themeKey = 'aero';
  String _lang = 'ru';
  String _nick = 'PhoneUser_${100 + Random().nextInt(899)}';
  String _room = 'AIM-Global-2006';
  String _aiProvider = 'Gemini';
  String _aiEndpoint = 'https://generativelanguage.googleapis.com/v1beta/models';
  String _aiKey = '';
  String _aiModel = 'gemini-4-argon';

  int _tab = 0;
  int _hubSection = 0;
  int _bannerIdx = 0;
  bool _showBanners = true;
  String? _activeChatPeer;
  String? _activePublicRoom;

  final List<_MailEntry> _mails = [
    _MailEntry(from: 'welcome@aol.com', subject: 'Welcome to AIM Classic 6.0!', body: 'Enjoy Rotating Banners, Theme Workshop, Support Diagnostics, Update Center & Global Chat!', date: 'Today'),
    _MailEntry(from: 'AeroFan@aim.com', subject: 'Frutiger Aero & XP Skins Ready', body: 'Open the AIM Hub tab -> Workshop to apply all 7 retro themes!', date: 'Today'),
  ];

  final List<AimBuddy> _buddies = [
    AimBuddy(screenName: 'Argon_AI', group: 'AI Bots', status: 'online', isBot: true, statusNote: 'Gemini 4 Argon Assistant'),
    AimBuddy(screenName: 'Hydrogen_Dev', group: 'AI Bots', status: 'online', isBot: true, statusNote: 'Fast Code & Tech AI'),
    AimBuddy(screenName: 'Astra_GPT', group: 'AI Bots', status: 'online', isBot: true, statusNote: 'ChatGPT-6 Astra Reasoning'),
    AimBuddy(screenName: 'AIM_AI_Helper', group: 'AI Bots', status: 'online', isBot: true, statusNote: 'Classic AIM Offline Helper'),
  ];

  final List<PublicRoomInfo> _publicRooms = [
    PublicRoomInfo(name: 'AIM_Lobby_2006', topic: 'Classic 2006 Nostalgia & Global Hangout', category: 'General', createdBy: 'AIM'),
    PublicRoomInfo(name: 'AI_Playground', topic: 'Discuss Argon, Hydrogen, Astra & Ollama', category: 'Tech & AI', createdBy: 'AIM'),
    PublicRoomInfo(name: 'Music_Lounge', topic: 'Now Playing & Synth Beats', category: 'Music', createdBy: 'AIM'),
  ];

  final Map<String, List<AimMessage>> _pmLogs = {};
  final Map<String, List<AimMessage>> _roomLogs = {};
  final Set<String> _seenNtfyIds = {};

  final TextEditingController _pmCtrl = TextEditingController();
  final TextEditingController _roomMsgCtrl = TextEditingController();
  Timer? _pollTimer;
  Timer? _bannerTimer;
  bool _syncing = false;

  String tr(String en, String ru) => _lang == 'ru' ? ru : en;

  @override
  void initState() {
    super.initState();
    _loadSettings();
    _pollTimer = Timer.periodic(const Duration(seconds: 6), (_) => _pollNetwork());
    _bannerTimer = Timer.periodic(const Duration(seconds: 10), (_) {
      if (mounted) setState(() => _bannerIdx = (_bannerIdx + 1) % 5);
    });
  }

  @override
  void dispose() {
    _pollTimer?.cancel();
    _bannerTimer?.cancel();
    _pmCtrl.dispose();
    _roomMsgCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadSettings() async {
    final prefs = await SharedPreferences.getInstance();
    setState(() {
      _themeKey = prefs.getString('theme') ?? 'aero';
      _lang = prefs.getString('lang') ?? 'ru';
      _nick = prefs.getString('nick') ?? _nick;
      _room = prefs.getString('room') ?? 'AIM-Global-2006';
      _aiProvider = prefs.getString('aiProvider') ?? 'Gemini';
      _aiEndpoint = prefs.getString('aiEndpoint') ?? _aiEndpoint;
      _aiKey = prefs.getString('aiKey') ?? '';
      _aiModel = prefs.getString('aiModel') ?? 'gemini-4-argon';

      final savedBuddies = prefs.getString('buddies');
      if (savedBuddies != null && savedBuddies.isNotEmpty) {
        try {
          final List<dynamic> decoded = jsonDecode(savedBuddies);
          final custom = decoded.map((e) => AimBuddy.fromJson(Map<String, dynamic>.from(e))).toList();
          for (final b in custom) {
            if (_buddies.indexWhere((x) => x.screenName.toLowerCase() == b.screenName.toLowerCase()) < 0) {
              _buddies.add(b);
            }
          }
        } catch (_) {}
      }
      _activeChatPeer ??= 'Argon_AI';
      _activePublicRoom ??= _publicRooms.first.name;
    });
    await _announcePresence();
    await _pollNetwork();
  }

  Future<void> _saveSettings() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('theme', _themeKey);
    await prefs.setString('lang', _lang);
    await prefs.setString('nick', _nick);
    await prefs.setString('room', _room);
    await prefs.setString('aiProvider', _aiProvider);
    await prefs.setString('aiEndpoint', _aiEndpoint);
    await prefs.setString('aiKey', _aiKey);
    await prefs.setString('aiModel', _aiModel);
    await prefs.setString('buddies', jsonEncode(_buddies.where((b) => !b.isBot).map((b) => b.toJson()).toList()));
  }

  Future<void> _announcePresence() async {
    try {
      await AimProtocol.publishJson(AimProtocol.dirTopic(_room), {
        'Type': 'HELLO',
        'From': _nick,
        'Status': 'online',
        'Avatar': 'AIM Icon.gif',
        'AwayText': 'AIM Mobile (${defaultTargetPlatformName()})',
        'Bio': 'AIM Classic Mobile Flutter',
      });
    } catch (_) {}
  }

  String defaultTargetPlatformName() {
    switch (Theme.of(context).platform) {
      case TargetPlatform.iOS:
        return 'iPhone';
      case TargetPlatform.android:
        return 'Android';
      default:
        return 'Mobile';
    }
  }

  Future<void> _pollNetwork() async {
    if (_syncing || !mounted) return;
    _syncing = true;
    try {
      // 1. Directory presence (matches Desktop NetPacket Type: HELLO/GOODBYE)
      final dirEvents = await AimProtocol.fetchNtfyEvents(AimProtocol.dirTopic(_room), since: '10m');
      for (final ev in dirEvents) {
        final from = (ev['From'] ?? ev['sn'] ?? '').toString().trim();
        if (from.isEmpty || from.toLowerCase() == _nick.toLowerCase()) continue;
        final t = (ev['Type'] ?? ev['type'] ?? '').toString().toUpperCase();
        if (t == 'GOODBYE') continue;
        final status = (ev['Status'] ?? ev['status'] ?? 'online').toString();
        final note = (ev['AwayText'] ?? ev['note'] ?? '').toString();
        final idx = _buddies.indexWhere((b) => b.screenName.toLowerCase() == from.toLowerCase());
        if (idx < 0) {
          _buddies.add(AimBuddy(
            screenName: from,
            group: 'Online Peers',
            status: status,
            isBot: false,
            isRealPeer: true,
            statusNote: note,
          ));
        } else if (!_buddies[idx].isBot) {
          _buddies[idx].status = status;
          _buddies[idx].statusNote = note;
          _buddies[idx].isRealPeer = true;
        }
      }

      // 2. Private messages to me (matches Desktop NetPacket Type: IM, RING, FRIEND_REQ)
      final pmEvents = await AimProtocol.fetchNtfyEvents(AimProtocol.pmTopic(_nick, _room), since: '15m');
      for (final ev in pmEvents) {
        final id = (ev['_ntfyId'] ?? '').toString();
        if (id.isNotEmpty && !_seenNtfyIds.add('pm_$id')) continue;
        final t = (ev['Type'] ?? ev['type'] ?? '').toString().toUpperCase();
        final from = (ev['From'] ?? ev['from'] ?? '').toString().trim();
        final text = (ev['Text'] ?? ev['text'] ?? '').toString();
        if (from.isEmpty || from.toLowerCase() == _nick.toLowerCase()) continue;
        final key = from.toLowerCase();
        if (t == 'IM' || t == 'PM' || t == 'FRIEND_REQ') {
          if (_buddies.indexWhere((b) => b.screenName.toLowerCase() == key) < 0) {
            _buddies.add(AimBuddy(screenName: from, group: 'Real Friends', isRealPeer: true));
          }
          if (t != 'FRIEND_REQ') {
            _pmLogs.putIfAbsent(key, () => []);
            _pmLogs[key]!.add(AimMessage(from: from, text: text, time: _hhmm(), isSelf: false));
          }
        } else if (t == 'MAIL') {
          _mails.insert(0, _MailEntry(
            from: '$from@aim.com',
            subject: (ev['Font'] ?? 'AIM Mail').toString(),
            body: text,
            date: _hhmm(),
          ));
        } else if (t == 'RING') {
          _pmLogs.putIfAbsent(key, () => []);
          _pmLogs[key]!.add(AimMessage(from: from, text: '*BUZZ!* Attention!', time: _hhmm(), isSelf: false, isBuzz: true));
          HapticFeedback.heavyImpact();
        }
      }

      // 3. Public group directory (matches Desktop NetPacket Type: PUB_ROOM, To, Font=Category, Text=Topic)
      final pubDir = await AimProtocol.fetchNtfyEvents(AimProtocol.pubDirTopic(_room), since: '12h');
      for (final ev in pubDir) {
        final t = (ev['Type'] ?? ev['type'] ?? '').toString().toUpperCase();
        if (t == 'PUB_ROOM' || t == 'PUB_REG') {
          final name = (ev['To'] ?? ev['name'] ?? '').toString().trim();
          if (name.isEmpty) continue;
          if (_publicRooms.indexWhere((r) => r.name.toLowerCase() == name.toLowerCase()) < 0) {
            _publicRooms.add(PublicRoomInfo(
              name: name,
              topic: (ev['Text'] ?? ev['topic'] ?? 'Public Chat').toString(),
              category: (ev['Font'] ?? ev['category'] ?? 'General').toString(),
              createdBy: (ev['From'] ?? ev['creator'] ?? 'User').toString(),
            ));
          }
        }
      }

      // 4. Active public room messages (matches Desktop NetPacket Type: ROOM_MSG, ROOM_JOIN)
      if (_activePublicRoom != null && _activePublicRoom!.isNotEmpty) {
        final roomEvs = await AimProtocol.fetchNtfyEvents(
          AimProtocol.pubRoomTopic(_activePublicRoom!, _room),
          since: '15m',
        );
        final rKey = _activePublicRoom!.toLowerCase();
        _roomLogs.putIfAbsent(rKey, () => []);
        for (final ev in roomEvs) {
          final id = (ev['_ntfyId'] ?? '').toString();
          if (id.isNotEmpty && !_seenNtfyIds.add('rm_$id')) continue;
          final t = (ev['Type'] ?? ev['type'] ?? 'ROOM_MSG').toString().toUpperCase();
          final from = (ev['From'] ?? ev['from'] ?? '').toString().trim();
          final text = (ev['Text'] ?? ev['text'] ?? '').toString();
          if (from.isEmpty || from.toLowerCase() == _nick.toLowerCase()) continue;
          if (t == 'ROOM_JOIN') {
            _roomLogs[rKey]!.add(AimMessage(from: 'SYSTEM', text: '*** $from joined!', time: _hhmm(), isSelf: false));
          } else {
            _roomLogs[rKey]!.add(AimMessage(from: from, text: text, time: _hhmm(), isSelf: false));
          }
        }
      }

      if (mounted) setState(() {});
    } catch (_) {
    } finally {
      _syncing = false;
    }
  }

  String _hhmm() {
    final now = DateTime.now();
    return '${now.hour.toString().padLeft(2, '0')}:${now.minute.toString().padLeft(2, '0')}';
  }

  Future<void> _sendPm(String text, {bool buzz = false}) async {
    final peerSn = _activeChatPeer;
    if (peerSn == null || peerSn.trim().isEmpty) return;
    final clean = buzz ? 'BUZZ' : text.trim();
    if (clean.isEmpty) return;
    _pmCtrl.clear();

    final peer = _buddies.firstWhere(
      (b) => b.screenName.toLowerCase() == peerSn.toLowerCase(),
      orElse: () => AimBuddy(screenName: peerSn, group: 'Buddies'),
    );

    final key = peer.screenName.toLowerCase();
    _pmLogs.putIfAbsent(key, () => []);
    setState(() {
      _pmLogs[key]!.add(AimMessage(
        from: _nick,
        text: buzz ? tr('*BUZZ!* Attention!', '*BUZZ!* Внимание!') : clean,
        time: _hhmm(),
        isSelf: true,
        isBuzz: buzz,
      ));
    });
    if (buzz) HapticFeedback.heavyImpact();

    if (peer.isBot) {
      final reply = await AimProtocol.askAiBot(
        botName: peer.screenName,
        userText: clean,
        provider: _aiProvider,
        endpoint: _aiEndpoint,
        apiKey: _aiKey,
        model: _aiModel,
      );
      if (mounted) {
        setState(() {
          _pmLogs[key]!.add(AimMessage(from: peer.screenName, text: reply, time: _hhmm(), isSelf: false));
        });
      }
    } else {
      try {
        await AimProtocol.publishJson(AimProtocol.pmTopic(peer.screenName, _room), {
          'Type': buzz ? 'RING' : 'IM',
          'From': _nick,
          'To': peer.screenName,
          'Status': 'online',
          'Avatar': 'AIM Icon.gif',
          'Text': buzz ? 'Buzz!' : clean,
        });
      } catch (_) {}
    }
  }

  Future<void> _sendPublicRoomMessage() async {
    final room = _activePublicRoom;
    final txt = _roomMsgCtrl.text.trim();
    if (room == null || room.isEmpty || txt.isEmpty) return;
    _roomMsgCtrl.clear();

    final rKey = room.toLowerCase();
    _roomLogs.putIfAbsent(rKey, () => []);
    final displayTxt = txt == '/roll' ? '🎲 Rolled d100: ${Random().nextInt(100) + 1}' : txt;

    setState(() {
      _roomLogs[rKey]!.add(AimMessage(from: _nick, text: displayTxt, time: _hhmm(), isSelf: true));
    });

    try {
      await AimProtocol.publishJson(AimProtocol.pubRoomTopic(room, _room), {
        'Type': 'ROOM_MSG',
        'From': _nick,
        'To': room,
        'Text': displayTxt,
      });
    } catch (_) {}
  }

  void _openAddFriendDialog(BuildContext ctx) {
    final ctrl = TextEditingController();
    showDialog(
      context: ctx,
      builder: (dCtx) => AlertDialog(
        title: Text(tr('Add Real Friend', 'Добавить реального друга')),
        content: TextField(
          controller: ctrl,
          decoration: InputDecoration(
            labelText: tr('Friend Screen Name', 'Ник друга в AIM'),
            hintText: 'e.g. CoolGamer_07',
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(dCtx), child: Text(tr('Cancel', 'Отмена'))),
          ElevatedButton(
            onPressed: () {
              final name = ctrl.text.trim();
              if (name.isNotEmpty) {
                setState(() {
                  if (_buddies.indexWhere((b) => b.screenName.toLowerCase() == name.toLowerCase()) < 0) {
                    _buddies.add(AimBuddy(screenName: name, group: 'Real Friends', isRealPeer: true));
                  }
                  _activeChatPeer = name;
                  _tab = 1;
                });
                _saveSettings();
              }
              Navigator.pop(dCtx);
            },
            child: Text(tr('Add & Chat', 'Добавить и открыть')),
          ),
        ],
      ),
    );
  }

  void _openCreateGroupDialog(BuildContext ctx) {
    final nameCtrl = TextEditingController();
    final topicCtrl = TextEditingController();
    showDialog(
      context: ctx,
      builder: (dCtx) => AlertDialog(
        title: Text(tr('Create Public Group', 'Создать публичную группу')),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: nameCtrl,
              decoration: InputDecoration(labelText: tr('Room Name', 'Название комнаты')),
            ),
            const SizedBox(height: 8),
            TextField(
              controller: topicCtrl,
              decoration: InputDecoration(labelText: tr('Topic', 'Тема')),
            ),
          ],
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(dCtx), child: Text(tr('Cancel', 'Отмена'))),
          ElevatedButton(
            onPressed: () async {
              final rName = nameCtrl.text.trim().replaceAll(' ', '_');
              if (rName.isEmpty) return;
              final t = topicCtrl.text.trim().isEmpty ? 'Open Public Chat' : topicCtrl.text.trim();
              setState(() {
                _publicRooms.insert(0, PublicRoomInfo(name: rName, topic: t, category: 'Community', createdBy: _nick));
                _activePublicRoom = rName;
              });
              Navigator.pop(dCtx);
              try {
                await AimProtocol.publishJson(AimProtocol.pubDirTopic(_room), {
                  'Type': 'PUB_ROOM',
                  'From': _nick,
                  'To': rName,
                  'Font': 'Community',
                  'Text': t,
                });
              } catch (_) {}
            },
            child: Text(tr('Create', 'Создать')),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final pal = kThemes[_themeKey] ?? kThemes['aero']!;
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'AIM Classic',
      theme: ThemeData(
        useMaterial3: true,
        colorSchemeSeed: pal.primary,
        scaffoldBackgroundColor: pal.bg,
      ),
      home: Builder(
        builder: (homeCtx) => Scaffold(
          appBar: AppBar(
            flexibleSpace: Container(
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  colors: [pal.primary, pal.dark],
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                ),
              ),
            ),
            foregroundColor: Colors.white,
            title: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'AIM Classic 6.0 · $_nick',
                  style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                ),
                Text(
                  'Room: $_room · ${pal.name}',
                  style: TextStyle(fontSize: 11, color: pal.gold),
                ),
              ],
            ),
            actions: [
              IconButton(
                tooltip: tr('Sync Now', 'Обновить'),
                icon: const Icon(Icons.sync),
                onPressed: () {
                  _announcePresence();
                  _pollNetwork();
                },
              ),
            ],
          ),
          body: IndexedStack(
            index: _tab,
            children: [
              _buildBuddiesTab(homeCtx, pal),
              _buildImTab(pal),
              _buildGroupsTab(homeCtx, pal),
              _buildHubTab(homeCtx, pal),
              _buildSettingsTab(pal),
            ],
          ),
          bottomNavigationBar: NavigationBar(
            selectedIndex: _tab,
            onDestinationSelected: (i) => setState(() => _tab = i),
            destinations: [
              NavigationDestination(icon: const Icon(Icons.people), label: tr('Buddies', 'Друзья')),
              NavigationDestination(icon: const Icon(Icons.chat_bubble), label: tr('IM Chat', 'Чат')),
              NavigationDestination(icon: const Icon(Icons.forum), label: tr('Groups', 'Группы')),
              NavigationDestination(icon: const Icon(Icons.apps), label: tr('AIM Hub', 'Хаб')),
              NavigationDestination(icon: const Icon(Icons.tune), label: tr('Setup', 'Настройки')),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildPromoBanner(BuildContext ctx, _ThemePalette pal) {
    if (!_showBanners) return const SizedBox.shrink();
    final slides = [
      ('GLOBAL P2P', tr('Add Global Friends Anywhere!', 'Добавляй друзей по всему миру!'), 0, 0),
      ('FRUTIGER AERO', tr('Frutiger Aero Glossy Theme in Workshop ->', 'Фрутигер Аэро и XP темы в Мастерской ->'), 3, 1),
      ('AIM MAIL', tr("You've Got Mail! Open AOL Inbox ->", 'Вам письмо! Открыть Почту AIM ->'), 3, 0),
      ('SUPPORT', tr('Diagnostics, Friends Guide & Ticket ->', 'Диагностика, помощь и поддержка ->'), 3, 2),
      ('UPDATES', tr('AIM Update Center v6.0.4020 ->', 'Центр обновлений v6.0.4020 ->'), 3, 3),
    ];
    final cur = slides[_bannerIdx % slides.length];
    return InkWell(
      onTap: () => setState(() {
        _tab = cur.$3;
        _hubSection = cur.$4;
      }),
      child: Container(
        margin: const EdgeInsets.fromLTRB(10, 4, 10, 8),
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 8),
        decoration: BoxDecoration(
          gradient: LinearGradient(colors: [pal.dark, pal.primary]),
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: pal.gold),
        ),
        child: Row(
          children: [
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 4),
              decoration: BoxDecoration(color: pal.gold, borderRadius: BorderRadius.circular(4)),
              child: Text(cur.$1, style: const TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: Colors.black)),
            ),
            const SizedBox(width: 8),
            Expanded(child: Text(cur.$2, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 12), overflow: TextOverflow.ellipsis)),
            IconButton(
              padding: EdgeInsets.zero,
              constraints: const BoxConstraints(),
              icon: const Icon(Icons.chevron_right, color: Colors.white),
              onPressed: () => setState(() => _bannerIdx = (_bannerIdx + 1) % slides.length),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildBuddiesTab(BuildContext ctx, _ThemePalette pal) {
    final bots = _buddies.where((b) => b.isBot).toList();
    final humans = _buddies.where((b) => !b.isBot).toList();
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(12, 10, 12, 6),
          child: Row(
            children: [
              Expanded(
                child: FilledButton.icon(
                  onPressed: () => _openAddFriendDialog(ctx),
                  icon: const Icon(Icons.person_add),
                  label: Text(tr('+ Real Friend', '+ Друг')),
                ),
              ),
              const SizedBox(width: 8),
              OutlinedButton.icon(
                onPressed: () {
                  _announcePresence();
                  _pollNetwork();
                },
                icon: const Icon(Icons.radar),
                label: Text(tr('Scan Online', 'Поиск')),
              ),
            ],
          ),
        ),
        Expanded(
          child: ListView(
            padding: const EdgeInsets.symmetric(horizontal: 10),
            children: [
              _sectionHeader(tr('Real Friends & Online PC/Phone (${humans.length})', 'Реальные друзья и сеть (${humans.length})'), pal),
              if (humans.isEmpty)
                Card(
                  color: pal.card,
                  child: ListTile(
                    leading: const Icon(Icons.wifi_tethering),
                    title: Text(tr('No online peers yet in $_room', 'Пока никого нет в $_room')),
                    subtitle: Text(tr('Sign on AIM Desktop in the same Global Room or tap "+ Real Friend"', 'Запустите AIM на ПК в той же комнате или нажмите "+ Друг"')),
                  ),
                ),
              for (final b in humans) _buddyTile(b, pal),
              const SizedBox(height: 8),
              _sectionHeader(tr('AI Assistant Bots (${bots.length})', 'ИИ-боты (${bots.length})'), pal),
              for (final b in bots) _buddyTile(b, pal),
            ],
          ),
        ),
        _buildPromoBanner(ctx, pal),
      ],
    );
  }

  Widget _buildHubTab(BuildContext ctx, _ThemePalette pal) {
    final unread = _mails.where((m) => !m.read).length;
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.all(8),
          child: SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: SegmentedButton<int>(
              segments: [
                ButtonSegment(value: 0, label: Text('Mail ($unread)')),
                ButtonSegment(value: 1, label: Text(tr('Workshop', 'Мастерская'))),
                ButtonSegment(value: 2, label: Text(tr('Support', 'Поддержка'))),
                ButtonSegment(value: 3, label: Text(tr('Updates', 'Обновления'))),
              ],
              selected: {_hubSection},
              onSelectionChanged: (s) => setState(() => _hubSection = s.first),
            ),
          ),
        ),
        Expanded(
          child: _hubSection == 0
              ? ListView(
                  padding: const EdgeInsets.all(10),
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text(tr("You've Got Mail!", 'AIM Почта'), style: TextStyle(fontWeight: FontWeight.bold, color: pal.dark)),
                        FilledButton.icon(
                          onPressed: () {
                            final toC = TextEditingController();
                            final subC = TextEditingController();
                            final bodyC = TextEditingController();
                            showDialog(
                              context: ctx,
                              builder: (dCtx) => AlertDialog(
                                title: Text(tr('Write AIM Mail', 'Новое письмо')),
                                content: Column(mainAxisSize: MainAxisSize.min, children: [
                                  TextField(controller: toC, decoration: const InputDecoration(labelText: 'To')),
                                  TextField(controller: subC, decoration: const InputDecoration(labelText: 'Subject')),
                                  TextField(controller: bodyC, maxLines: 3, decoration: const InputDecoration(labelText: 'Message')),
                                ]),
                                actions: [
                                  TextButton(onPressed: () => Navigator.pop(dCtx), child: Text(tr('Cancel', 'Отмена'))),
                                  ElevatedButton(
                                    onPressed: () async {
                                      final to = toC.text.trim();
                                      if (to.isEmpty) return;
                                      Navigator.pop(dCtx);
                                      await AimProtocol.publishJson(AimProtocol.pmTopic(to, _room), {
                                        'Type': 'MAIL', 'From': _nick, 'To': to, 'Font': subC.text.trim(), 'Text': bodyC.text,
                                      });
                                    },
                                    child: Text(tr('Send', 'Отправить')),
                                  ),
                                ],
                              ),
                            );
                          },
                          icon: const Icon(Icons.edit),
                          label: Text(tr('Write', 'Написать')),
                        ),
                      ],
                    ),
                    const SizedBox(height: 8),
                    for (final m in _mails)
                      Card(
                        color: pal.card,
                        child: ExpansionTile(
                          leading: Icon(m.read ? Icons.drafts : Icons.mark_email_unread, color: m.read ? Colors.grey : pal.primary),
                          title: Text(m.subject, style: TextStyle(fontWeight: m.read ? FontWeight.normal : FontWeight.bold)),
                          subtitle: Text('${m.from} · ${m.date}'),
                          onExpansionChanged: (v) { if (v && !m.read) setState(() => m.read = true); },
                          children: [Padding(padding: const EdgeInsets.all(12), child: Align(alignment: Alignment.centerLeft, child: Text(m.body)))],
                        ),
                      ),
                  ],
                )
              : _hubSection == 1
                  ? ListView(
                      padding: const EdgeInsets.all(10),
                      children: [
                        Text(tr('AIM Theme Workshop (7 PC Skins)', 'Мастерская тем AIM (7 скинов)'), style: TextStyle(fontWeight: FontWeight.bold, color: pal.dark)),
                        const SizedBox(height: 8),
                        for (final t in kThemes.values)
                          Card(
                            color: pal.card,
                            child: ListTile(
                              leading: CircleAvatar(backgroundColor: t.primary, child: Icon(Icons.palette, color: t.gold)),
                              title: Text(t.name, style: const TextStyle(fontWeight: FontWeight.bold)),
                              trailing: FilledButton(
                                onPressed: () {
                                  setState(() => _themeKey = t.key);
                                  _saveSettings();
                                },
                                child: Text(_themeKey == t.key ? tr('Active', 'Активна') : tr('Apply', 'Применить')),
                              ),
                            ),
                          ),
                      ],
                    )
                  : _hubSection == 2
                      ? ListView(
                          padding: const EdgeInsets.all(12),
                          children: [
                            Card(
                              color: pal.card,
                              child: Padding(
                                padding: const EdgeInsets.all(12),
                                child: SelectableText(
                                  'AIM Classic Mobile Diagnostics\n'
                                  'Version: v6.0.4020\nNick: $_nick\nRoom: $_room\nTheme: ${pal.name}\nAI Model: $_aiModel\nFriends: ${_buddies.length}\nPublic Rooms: ${_publicRooms.length}',
                                  style: const TextStyle(fontFamily: 'monospace', fontSize: 12),
                                ),
                              ),
                            ),
                          ],
                        )
                      : ListView(
                          padding: const EdgeInsets.all(12),
                          children: [
                            Card(
                              color: pal.card,
                              child: Padding(
                                padding: const EdgeInsets.all(12),
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text('AIM Classic v6.0.4020', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16, color: pal.dark)),
                                    const SizedBox(height: 6),
                                    Text(tr('Up to date! Latest PC & Mobile build.', 'Установлена последняя сборка v6.0.4020!')),
                                    const Divider(),
                                    const Text('+ Rotating Promo Banners & Workshop (7 Themes)\n+ AOL/AIM Mail Inbox & Cloud Send\n+ Support Diagnostics & Update Center\n+ Global Friends & Public Groups Sync'),
                                  ],
                                ),
                              ),
                            ),
                          ],
                        ),
        ),
      ],
    );
  }

  Widget _sectionHeader(String title, _ThemePalette pal) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 6, horizontal: 4),
        child: Text(title, style: TextStyle(fontWeight: FontWeight.bold, color: pal.dark, fontSize: 13)),
      );

  Widget _buddyTile(AimBuddy b, _ThemePalette pal) => Card(
        color: pal.card,
        margin: const EdgeInsets.symmetric(vertical: 3),
        child: ListTile(
          leading: CircleAvatar(
            backgroundColor: b.isBot ? pal.primary : const Color(0xFF1AA24B),
            foregroundColor: Colors.white,
            child: Icon(b.isBot ? Icons.smart_toy : Icons.person),
          ),
          title: Text(b.screenName, style: const TextStyle(fontWeight: FontWeight.w600)),
          subtitle: Text(b.statusNote.isEmpty ? b.group : b.statusNote, maxLines: 1, overflow: TextOverflow.ellipsis),
          trailing: const Icon(Icons.chevron_right),
          onTap: () {
            setState(() {
              _activeChatPeer = b.screenName;
              _tab = 1;
            });
          },
        ),
      );

  Widget _buildImTab(_ThemePalette pal) {
    final peer = _activeChatPeer ?? 'Argon_AI';
    final msgs = _pmLogs[peer.toLowerCase()] ?? [];
    return Column(
      children: [
        Container(
          color: pal.card,
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
          child: Row(
            children: [
              const Icon(Icons.chat, size: 18),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  tr('Chat with $peer', 'Чат с $peer'),
                  style: TextStyle(fontWeight: FontWeight.bold, color: pal.dark),
                ),
              ),
              TextButton.icon(
                onPressed: () => _sendPm('', buzz: true),
                icon: const Icon(Icons.vibration, size: 16, color: Colors.orange),
                label: const Text('BUZZ!'),
              ),
            ],
          ),
        ),
        Expanded(
          child: msgs.isEmpty
              ? Center(
                  child: Text(
                    tr('Say hi to $peer! Supports real AIM PC users & AI bots.', 'Напишите $peer! Сообщения доходят на ПК и к ИИ.'),
                    textAlign: TextAlign.center,
                  ),
                )
              : ListView.builder(
                  padding: const EdgeInsets.all(10),
                  itemCount: msgs.length,
                  itemBuilder: (_, i) => _msgBubble(msgs[i], pal),
                ),
        ),
        _emoticonStrip(_pmCtrl),
        Padding(
          padding: const EdgeInsets.fromLTRB(10, 4, 10, 10),
          child: Row(
            children: [
              Expanded(
                child: TextField(
                  controller: _pmCtrl,
                  decoration: InputDecoration(
                    hintText: tr('Write Instant Message...', 'Написать сообщение...'),
                    filled: true,
                    fillColor: Colors.white,
                    border: const OutlineInputBorder(),
                    isDense: true,
                  ),
                  onSubmitted: (v) => _sendPm(v),
                ),
              ),
              const SizedBox(width: 6),
              FilledButton(
                onPressed: () => _sendPm(_pmCtrl.text),
                child: const Icon(Icons.send),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildGroupsTab(BuildContext ctx, _ThemePalette pal) {
    final activeRoom = _activePublicRoom ?? _publicRooms.first.name;
    final roomMsgs = _roomLogs[activeRoom.toLowerCase()] ?? [];
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(10, 8, 10, 4),
          child: Row(
            children: [
              Expanded(
                child: SingleChildScrollView(
                  scrollDirection: Axis.horizontal,
                  child: Row(
                    children: _publicRooms
                        .map((r) => Padding(
                              padding: const EdgeInsets.only(right: 6),
                              child: ChoiceChip(
                                label: Text('#${r.name}'),
                                selected: r.name == activeRoom,
                                onSelected: (_) {
                                  setState(() => _activePublicRoom = r.name);
                                  _pollNetwork();
                                },
                              ),
                            ))
                        .toList(),
                  ),
                ),
              ),
              IconButton(
                tooltip: tr('New Public Room', 'Новая группа'),
                icon: const Icon(Icons.add_circle_outline),
                onPressed: () => _openCreateGroupDialog(ctx),
              ),
            ],
          ),
        ),
        Expanded(
          child: roomMsgs.isEmpty
              ? Center(child: Text(tr('Welcome to #$activeRoom! Send a message or /roll.', 'Вы в #$activeRoom! Напишите сообщение или /roll.')))
              : ListView.builder(
                  padding: const EdgeInsets.all(10),
                  itemCount: roomMsgs.length,
                  itemBuilder: (_, i) => _msgBubble(roomMsgs[i], pal),
                ),
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(10, 4, 10, 10),
          child: Row(
            children: [
              Expanded(
                child: TextField(
                  controller: _roomMsgCtrl,
                  decoration: InputDecoration(
                    hintText: tr('Message #$activeRoom (/roll)...', 'Сообщение в #$activeRoom (/roll)...'),
                    filled: true,
                    fillColor: Colors.white,
                    border: const OutlineInputBorder(),
                    isDense: true,
                  ),
                  onSubmitted: (_) => _sendPublicRoomMessage(),
                ),
              ),
              const SizedBox(width: 6),
              FilledButton(
                onPressed: _sendPublicRoomMessage,
                child: const Icon(Icons.send),
              ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _buildSettingsTab(_ThemePalette pal) {
    return ListView(
      padding: const EdgeInsets.all(12),
      children: [
        Card(
          color: pal.card,
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(tr('Identity & Global Room', 'Аккаунт и глобальная комната'), style: TextStyle(fontWeight: FontWeight.bold, color: pal.dark)),
                const SizedBox(height: 8),
                TextFormField(
                  initialValue: _nick,
                  decoration: InputDecoration(labelText: tr('Screen Name', 'Никнейм'), border: const OutlineInputBorder(), isDense: true),
                  onChanged: (v) => _nick = v.trim().isEmpty ? _nick : v.trim(),
                ),
                const SizedBox(height: 8),
                TextFormField(
                  initialValue: _room,
                  decoration: InputDecoration(labelText: tr('Global Room Key', 'Ключ глобальной комнаты'), border: const OutlineInputBorder(), isDense: true),
                  onChanged: (v) => _room = v.trim().isEmpty ? 'AIM-Global-2006' : v.trim(),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 8),
        Card(
          color: pal.card,
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(tr('Theme & Language', 'Тема и язык'), style: TextStyle(fontWeight: FontWeight.bold, color: pal.dark)),
                const SizedBox(height: 8),
                Wrap(
                  spacing: 8,
                  children: kThemes.values
                      .map((t) => ChoiceChip(
                            label: Text(t.name),
                            selected: _themeKey == t.key,
                            onSelected: (_) {
                              setState(() => _themeKey = t.key);
                              _saveSettings();
                            },
                          ))
                      .toList(),
                ),
                const SizedBox(height: 8),
                SegmentedButton<String>(
                  segments: const [
                    ButtonSegment(value: 'ru', label: Text('Русский')),
                    ButtonSegment(value: 'en', label: Text('English')),
                  ],
                  selected: {_lang},
                  onSelectionChanged: (s) {
                    setState(() => _lang = s.first);
                    _saveSettings();
                  },
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 8),
        Card(
          color: pal.card,
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(tr('AI Models (Argon, Hydrogen, Astra, Gemini, GPT)', 'ИИ-модели и API ключи'), style: TextStyle(fontWeight: FontWeight.bold, color: pal.dark)),
                const SizedBox(height: 8),
                DropdownButtonFormField<String>(
                  value: kAiModels.any((m) => m.id == _aiModel) ? _aiModel : kAiModels.first.id,
                  decoration: const InputDecoration(border: OutlineInputBorder(), isDense: true),
                  items: kAiModels.map((m) => DropdownMenuItem(value: m.id, child: Text(m.label))).toList(),
                  onChanged: (v) => setState(() => _aiModel = v ?? 'gemini-4-argon'),
                ),
                const SizedBox(height: 8),
                DropdownButtonFormField<String>(
                  value: _aiProvider,
                  decoration: InputDecoration(labelText: tr('Provider', 'Провайдер'), border: const OutlineInputBorder(), isDense: true),
                  items: const [
                    DropdownMenuItem(value: 'Gemini', child: Text('Google Gemini')),
                    DropdownMenuItem(value: 'OpenAI', child: Text('OpenAI / Astra / Compatible')),
                    DropdownMenuItem(value: 'OpenRouter', child: Text('OpenRouter')),
                  ],
                  onChanged: (v) => setState(() => _aiProvider = v ?? 'Gemini'),
                ),
                const SizedBox(height: 8),
                TextFormField(
                  initialValue: _aiKey,
                  obscureText: true,
                  decoration: InputDecoration(
                    labelText: tr('API Key (optional — empty uses offline bot)', 'API ключ (если пусто — встроенный бот)'),
                    border: const OutlineInputBorder(),
                    isDense: true,
                  ),
                  onChanged: (v) => _aiKey = v.trim(),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),
        FilledButton.icon(
          onPressed: () {
            _saveSettings();
            _announcePresence();
            _pollNetwork();
          },
          icon: const Icon(Icons.save),
          label: Text(tr('Save & Sync with PC', 'Сохранить и подключиться')),
        ),
      ],
    );
  }

  Widget _emoticonStrip(TextEditingController ctrl) {
    const emojis = [':-)', ':-D', ';-)', ':-P', '<3', 'O_O', '8-)', '\\m/'];
    return SizedBox(
      height: 32,
      child: ListView(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 10),
        children: emojis
            .map((e) => Padding(
                  padding: const EdgeInsets.only(right: 6),
                  child: ActionChip(
                    label: Text(e, style: const TextStyle(fontSize: 12)),
                    visualDensity: VisualDensity.compact,
                    onPressed: () {
                      ctrl.text = '${ctrl.text}$e ';
                      ctrl.selection = TextSelection.collapsed(offset: ctrl.text.length);
                    },
                  ),
                ))
            .toList(),
      ),
    );
  }

  Widget _msgBubble(AimMessage m, _ThemePalette pal) => Align(
        alignment: m.isSelf ? Alignment.centerRight : Alignment.centerLeft,
        child: Container(
          margin: const EdgeInsets.symmetric(vertical: 3),
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 7),
          constraints: const BoxConstraints(maxWidth: 290),
          decoration: BoxDecoration(
            color: m.isBuzz
                ? Colors.amber.shade100
                : m.isSelf
                    ? pal.primary.withOpacity(0.16)
                    : Colors.white,
            borderRadius: BorderRadius.circular(10),
            border: Border.all(color: m.isSelf ? pal.primary.withOpacity(0.35) : Colors.black12),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                '${m.from} · ${m.time}',
                style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: m.isSelf ? pal.dark : Colors.red.shade800),
              ),
              const SizedBox(height: 2),
              Text(m.text, style: const TextStyle(fontSize: 14)),
            ],
          ),
        ),
      );
}
