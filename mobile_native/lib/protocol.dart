import 'dart:convert';
import 'dart:math';
import 'package:crypto/crypto.dart';
import 'package:http/http.dart' as http;

class AiModelOption {
  final String id;
  final String label;
  const AiModelOption(this.id, this.label);
}

const List<AiModelOption> kAiModels = [
  AiModelOption('gemini-4-argon', 'Gemini 4 Argon [Balanced]'),
  AiModelOption('gemini-4-hydrogen', 'Gemini 4 Hydrogen [Fast]'),
  AiModelOption('gpt-6-astra', 'ChatGPT-6 Astra [Reasoning]'),
  AiModelOption('gpt-6-mini', 'ChatGPT-6 Mini [Fast]'),
  AiModelOption('gemini-2.5-flash', 'Gemini 2.5 Flash [Fast]'),
  AiModelOption('gemini-2.5-pro', 'Gemini 2.5 Pro'),
  AiModelOption('gpt-4o-mini', 'OpenAI GPT-4o Mini'),
  AiModelOption('gpt-4o', 'OpenAI GPT-4o'),
  AiModelOption('claude-5-sonnet', 'Claude 5 Sonnet'),
  AiModelOption('deepseek-v4', 'DeepSeek V4'),
  AiModelOption('grok-4', 'xAI Grok 4'),
  AiModelOption('llama-4-maverick', 'Meta Llama 4 Maverick'),
];

class AimBuddy {
  String screenName;
  String group;
  String status;
  bool isBot;
  bool isRealPeer;
  String statusNote;
  String lastSeen;

  AimBuddy({
    required this.screenName,
    required this.group,
    this.status = 'online',
    this.isBot = false,
    this.isRealPeer = false,
    this.statusNote = '',
    this.lastSeen = '',
  });

  Map<String, dynamic> toJson() => {
        'screenName': screenName,
        'group': group,
        'status': status,
        'isBot': isBot,
        'isRealPeer': isRealPeer,
        'statusNote': statusNote,
        'lastSeen': lastSeen,
      };

  factory AimBuddy.fromJson(Map<String, dynamic> j) => AimBuddy(
        screenName: (j['screenName'] ?? '').toString(),
        group: (j['group'] ?? 'Buddies').toString(),
        status: (j['status'] ?? 'online').toString(),
        isBot: j['isBot'] == true,
        isRealPeer: j['isRealPeer'] == true,
        statusNote: (j['statusNote'] ?? '').toString(),
        lastSeen: (j['lastSeen'] ?? '').toString(),
      );
}

class AimMessage {
  final String from;
  final String text;
  final String time;
  final bool isSelf;
  final bool isBuzz;

  AimMessage({
    required this.from,
    required this.text,
    required this.time,
    required this.isSelf,
    this.isBuzz = false,
  });
}

class PublicRoomInfo {
  String name;
  String topic;
  String category;
  String createdBy;

  PublicRoomInfo({
    required this.name,
    this.topic = 'Global AIM Room',
    this.category = 'General',
    this.createdBy = 'AIM',
  });
}

class AimProtocol {
  static String normRoom(String? room) {
    final s = (room ?? '').trim().toLowerCase();
    return s.isEmpty ? 'aim-global-2006' : s;
  }

  static String sha256Hex16(String input) {
    final digest = sha256.convert(utf8.encode(input));
    return digest.toString().substring(0, 16);
  }

  // Matches desktop AIMClassic NetworkEngine.cs topics byte-for-byte
  static String dirTopic(String room) => 'aimc_v59_dir_${sha256Hex16(normRoom(room))}';

  static String pmTopic(String targetSn, String room) =>
      'aimc_v59_pm_${sha256Hex16('${targetSn.trim().toLowerCase()}:${normRoom(room)}')}';

  static String pubDirTopic(String room) => 'aimc_v60_pubdir_${sha256Hex16(normRoom(room))}';

  static String pubRoomTopic(String roomName, String globalRoom) =>
      'aimc_v60_room_${sha256Hex16('${roomName.trim().toLowerCase()}:${normRoom(globalRoom)}')}';

  static Future<void> publishJson(String topic, Map<String, dynamic> payload) async {
    final uri = Uri.parse('https://ntfy.sh/$topic');
    await http
        .post(
          uri,
          headers: {'Content-Type': 'text/plain; charset=utf-8'},
          body: jsonEncode(payload),
        )
        .timeout(const Duration(seconds: 7));
  }

  static Future<List<Map<String, dynamic>>> fetchNtfyEvents(String topic, {String since = '60m'}) async {
    final uri = Uri.parse('https://ntfy.sh/$topic/json?poll=1&since=$since');
    final resp = await http.get(uri).timeout(const Duration(seconds: 8));
    if (resp.statusCode < 200 || resp.statusCode >= 300) return [];
    final out = <Map<String, dynamic>>[];
    for (final line in const LineSplitter().convert(resp.body)) {
      final trimmed = line.trim();
      if (trimmed.isEmpty) continue;
      try {
        final outer = jsonDecode(trimmed);
        if (outer is! Map<String, dynamic>) continue;
        if (outer['event'] != 'message') continue;
        final msgStr = (outer['message'] ?? '').toString().trim();
        if (!msgStr.startsWith('{')) continue;
        final inner = jsonDecode(msgStr);
        if (inner is Map<String, dynamic>) {
          inner['_ntfyId'] = outer['id'];
          inner['_ntfyTime'] = outer['time'];
          out.add(inner);
        }
      } catch (_) {}
    }
    return out;
  }

  static Future<String> askAiBot({
    required String botName,
    required String userText,
    required String provider,
    required String endpoint,
    required String apiKey,
    required String model,
  }) async {
    if (apiKey.trim().isNotEmpty) {
      try {
        if (provider == 'Gemini' && endpoint.contains('generativelanguage.googleapis.com')) {
          final uri = Uri.parse('https://generativelanguage.googleapis.com/v1beta/models/$model:generateContent?key=${apiKey.trim()}');
          final r = await http
              .post(
                uri,
                headers: {'Content-Type': 'application/json'},
                body: jsonEncode({
                  'contents': [
                    {
                      'parts': [
                        {'text': 'You are $botName in AIM Classic Mobile (2006). Reply concisely and helpfully.\n\nUser: $userText'}
                      ]
                    }
                  ]
                }),
              )
              .timeout(const Duration(seconds: 15));
          if (r.statusCode >= 200 && r.statusCode < 300) {
            final j = jsonDecode(r.body);
            final text = j['candidates']?[0]?['content']?['parts']?[0]?['text'];
            if (text != null && text.toString().trim().isNotEmpty) {
              return text.toString().trim();
            }
          }
        } else {
          final cleanUrl = endpoint.trim().isEmpty
              ? 'https://api.openai.com/v1/chat/completions'
              : endpoint.trim();
          final r = await http
              .post(
                Uri.parse(cleanUrl),
                headers: {
                  'Content-Type': 'application/json',
                  'Authorization': 'Bearer ${apiKey.trim()}',
                },
                body: jsonEncode({
                  'model': model,
                  'messages': [
                    {'role': 'system', 'content': 'You are $botName inside AIM Classic Mobile. Be friendly and concise.'},
                    {'role': 'user', 'content': userText},
                  ],
                  'max_tokens': 320,
                }),
              )
              .timeout(const Duration(seconds: 15));
          if (r.statusCode >= 200 && r.statusCode < 300) {
            final j = jsonDecode(r.body);
            final text = j['choices']?[0]?['message']?['content'];
            if (text != null && text.toString().trim().isNotEmpty) {
              return text.toString().trim();
            }
          }
        }
      } catch (_) {}
    }

    // Built-in offline fallback
    final lower = userText.toLowerCase();
    if (lower.contains('hi') || lower.contains('hello') || lower.contains('привет')) {
      return 'Hey! Running $model on AIM Mobile! Add your API key in Settings for live cloud answers or keep chatting offline :-)';
    }
    if (lower.contains('/roll')) {
      return 'Rolled d100: ${Random().nextInt(100) + 1}!';
    }
    return '[$botName · Offline Mode ($model)]: Received "$userText". Connect an API key in Settings for live responses!';
  }
}
