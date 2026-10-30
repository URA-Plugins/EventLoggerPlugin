using System.Collections.Frozen;

namespace EventLoggerPlugin
{
    /// <summary>
    /// EventLogger 使用的游戏领域常量（排除名单、友人首次点击事件等），
    /// 数据来自游戏逆向，随游戏版本更新需要维护。
    /// </summary>
    internal static class EventConstants
    {
        // 排除佐岳充电,SS,继承,老登三选一,第三年凯旋门（输/赢）,以及无事发生直接到下一回合的情况(-1)
        public static readonly FrozenSet<int> ExcludedEvents = new[] { 809043003, 400006112, 400000040, 400006474, 400006439, 830241003, -1 }.ToFrozenSet();

        // 友人和团队卡不计入连续事件，这里仅排除这几个
        public static readonly FrozenSet<int> ExcludedFriendCards = new[] { 30160, 30137, 30067, 30052, 10104, 30188, 10109, 30207, 30241, 30257, 30276, 10128, 10138, 10141, 30305 }.ToFrozenSet();

        // 这些回合不能触发连续事件
        public static readonly FrozenSet<int> ExcludedTurns = new[] { 1, 25, 31, 35, 37, 38, 39, 40, 49, 51, 55, 59, 61, 62, 63, 64, 72, 73, 74, 75, 76, 77, 78 }.ToFrozenSet();

        // 友人首次点击事件ID列表
        public static readonly FrozenSet<int> FriendFirstEvents = new[]
        {
            809001101,  // 拉面绿帽
            809043002,  // 佐岳
            809044002,  // 凉花
            830137001,  // 女神
        }.ToFrozenSet();

// 其他特殊事件（无需监控）
        //     400006112,  // 凯旋门-ss训练
        //     400006115,  // 凯旋门-剧本PT增加
        //     830137003,  // 女神-点击训练（三选一事件）
        //     830241003,  // 老登-点击训练（三选一事件）
        //     809043003,  // 佐岳-点击训练
        //     809044003,  // 凉花-点击训练
//            400000040,  // 继承

        // 特殊支援卡（只有一段事件）
        public static readonly FrozenDictionary<int, int> CardEventSpecialCount = new Dictionary<int, int>
        {
            { 30244, 1 },
            { 30258, 1 },
            { 30270, 1 }
        }.ToFrozenDictionary();
    }
}
