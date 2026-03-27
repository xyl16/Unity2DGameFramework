using System;
using System.Collections.Generic;

/// <summary>
/// 示例配置 - 物品配置
/// </summary>
[Serializable]
public class ItemConfig : BaseConfigRow
{
    public int id;
    public string name;
    public int type;      // 1:武器 2:消耗品 3:材料
    public int rarity;    // 1:普通 2:稀有 3:史诗 4:传说
    public string baseAttr;
    public string description;
    public int price;
    public int stackLimit;

    public override void Parse(Dictionary<string, string> data)
    {
        id = GetInt(data, "id");
        name = GetString(data, "name");
        type = GetInt(data, "type");
        rarity = GetInt(data, "rarity");
        baseAttr = GetString(data, "baseAttr");
        description = GetString(data, "description");
        price = GetInt(data, "price");
        stackLimit = GetInt(data, "stackLimit");
    }
}

/// <summary>
/// 示例配置 - 角色配置
/// </summary>
[Serializable]
public class CharacterConfig : BaseConfigRow
{
    public int id;
    public string name;
    public int quality;
    public int hp;
    public int mp;
    public int attack;
    public int defense;
    public int speed;
    public string skills;
    public string avatar;

    public override void Parse(Dictionary<string, string> data)
    {
        id = GetInt(data, "id");
        name = GetString(data, "name");
        quality = GetInt(data, "quality");
        hp = GetInt(data, "hp");
        mp = GetInt(data, "mp");
        attack = GetInt(data, "attack");
        defense = GetInt(data, "defense");
        speed = GetInt(data, "speed");
        skills = GetString(data, "skills");
        avatar = GetString(data, "avatar");
    }
}

/// <summary>
/// 示例网络消息 - 登录请求
/// </summary>
[Serializable]
public class LoginRequest : ProtoMessage
{
    public override ushort MessageId { get { return 1001; } }

    public string username;
    public string password;
    public string deviceId;
    public string platform;
    public int version;
}

/// <summary>
/// 示例网络消息 - 登录响应
/// </summary>
[Serializable]
public class LoginResponse : ProtoMessage
{
    public override ushort MessageId { get { return 1002; } }

    public int code;
    public string message;
    public string token;
    public long userId;
    public PlayerInfo playerInfo;
}

/// <summary>
/// 玩家信息
/// </summary>
[Serializable]
public class PlayerInfo
{
    public long userId;
    public string nickname;
    public int level;
    public long exp;
    public int vipLevel;
    public long coin;
    public long diamond;
}

/// <summary>
/// 示例网络消息 - 角色列表请求
/// </summary>
[Serializable]
public class GetCharacterListRequest : ProtoMessage
{
    public override ushort MessageId { get { return 1003; } }

    public string token;
}

/// <summary>
/// 示例网络消息 - 角色列表响应
/// </summary>
[Serializable]
public class GetCharacterListResponse : ProtoMessage
{
    public override ushort MessageId { get { return 1004; } }

    public int code;
    public string message;
    public List<CharacterData> characters;
}

/// <summary>
/// 角色数据
/// </summary>
[Serializable]
public class CharacterData
{
    public int characterId;
    public int level;
    public long exp;
    public List<int> skills;
    public Dictionary<int, int> equipment;
}

/// <summary>
/// 示例网络消息 - 战斗开始请求
/// </summary>
[Serializable]
public class BattleStartRequest : ProtoMessage
{
    public override ushort MessageId { get { return 2001; } }

    public string token;
    public int battleId;
    public int battleType;  // 1:PVE 2:PVP 3:活动
}

/// <summary>
/// 示例网络消息 - 战斗开始响应
/// </summary>
[Serializable]
public class BattleStartResponse : ProtoMessage
{
    public override ushort MessageId { get { return 2002; } }

    public int code;
    public string message;
    public long battleId;
    public List<EnemyData> enemies;
    public int seed;  // 随机种子
}

/// <summary>
/// 敌人数据
/// </summary>
[Serializable]
public class EnemyData
{
    public int enemyId;
    public int level;
    public int hp;
    public int attack;
    public int defense;
    public int position;
}

/// <summary>
/// 示例网络消息 - 战斗结果上报
/// </summary>
[Serializable]
public class BattleResultReport : ProtoMessage
{
    public override ushort MessageId { get { return 2003; } }

    public string token;
    public long battleId;
    public int result;    // 0:失败 1:胜利
    public int usedTime;
    public int damageDealt;
    public int damageTaken;
}

/// <summary>
/// 示例网络消息 - 战斗结果响应
/// </summary>
[Serializable]
public class BattleResultResponse : ProtoMessage
{
    public override ushort MessageId { get { return 2004; } }

    public int code;
    public string message;
    public List<RewardData> rewards;
    public long expGained;
    public long coinGained;
}

/// <summary>
/// 奖励数据
/// </summary>
[Serializable]
public class RewardData
{
    public int itemId;
    public int count;
}

/// <summary>
/// 消息ID常量定义
/// </summary>
public static class MessageID
{
    // 系统消息 (1000-1999)
    public const ushort HEARTBEAT = 1000;
    public const ushort LOGIN_REQUEST = 1001;
    public const ushort LOGIN_RESPONSE = 1002;
    public const ushort LOGOUT_REQUEST = 1003;
    public const ushort LOGOUT_RESPONSE = 1004;
    public const ushort GET_CHARACTER_LIST_REQUEST = 1005;
    public const ushort GET_CHARACTER_LIST_RESPONSE = 1006;

    // 战斗消息 (2000-2999)
    public const ushort BATTLE_START_REQUEST = 2001;
    public const ushort BATTLE_START_RESPONSE = 2002;
    public const ushort BATTLE_ACTION_REPORT = 2003;
    public const ushort BATTLE_ACTION_SYNC = 2004;
    public const ushort BATTLE_END_REPORT = 2005;
    public const ushort BATTLE_END_RESPONSE = 2006;

    // 聊天消息 (3000-3999)
    public const ushort CHAT_SEND_REQUEST = 3001;
    public const ushort CHAT_SEND_RESPONSE = 3002;
    public const ushort CHAT_BROADCAST = 3003;

    // 社交消息 (4000-4999)
    public const ushort FRIEND_LIST_REQUEST = 4001;
    public const ushort FRIEND_LIST_RESPONSE = 4002;
    public const ushort FRIEND_ADD_REQUEST = 4003;
    public const ushort FRIEND_ADD_RESPONSE = 4004;
    public const ushort FRIEND_REMOVE_REQUEST = 4005;
    public const ushort FRIEND_REMOVE_RESPONSE = 4006;

    // 商城消息 (5000-5999)
    public const ushort SHOP_INFO_REQUEST = 5001;
    public const ushort SHOP_INFO_RESPONSE = 5002;
    public const ushort BUY_ITEM_REQUEST = 5003;
    public const ushort BUY_ITEM_RESPONSE = 5004;

    // 公告消息 (6000-6999)
    public const ushort NOTICE_BROADCAST = 6001;
    public const ushort ACTIVITY_INFO = 6002;
}
