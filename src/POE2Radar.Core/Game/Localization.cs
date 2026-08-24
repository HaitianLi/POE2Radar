namespace POE2Radar.Core.Game;

/// <summary>Supported radar languages. The codes match the GameHelper2 Radar localization set.</summary>
public enum RadarLanguage { English = 0, SimplifiedChinese = 1, TraditionalChinese = 2 }

/// <summary>
/// Trilingual localization for the radar. Covers two layers:
/// <list type="number">
/// <item>The radar's OWN UI terms (settings labels, menu text — dashboard localizes via the
///   <see cref="Term"/> vocabulary and its own language dropdown).</item>
/// <item>The small, fixed on-map GAME-term vocabulary: league-mechanic labels, POI/marker labels,
///   and entity-category names — the highest-visibility terms that actually render on the map.</item>
/// </list>
///
/// <para>This is the SEED table (English / 简体中文 / 繁體中文). The FULL game-content name tables
/// (every entity / area / landmark name in three languages) are a separate data import from the
/// game's own localization — see <see cref="Name"/> / the resources/poe2-data pipeline; those strings
/// pass through untranslated until the tables are imported.</para>
/// </summary>
public sealed class Localization
{
    public static Localization Shared { get; } = new();

    private RadarLanguage _lang = RadarLanguage.English;
    public RadarLanguage Language { get => _lang; set => _lang = value; }

    // key → [English, 简体中文, 繁體中文]
    private static readonly Dictionary<string, string[]> Terms = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── League mechanics (on-map marker labels) ──
        ["Expedition"]     = ["Expedition", "先祖密藏", "先祖密藏"],
        ["Ritual"]         = ["Ritual", "祭祀", "祭祀"],
        ["Breach"]         = ["Breach", "裂隙", "裂隙"],
        ["Abyss"]          = ["Abyss", "深渊", "深淵"],
        ["Essence"]        = ["Essence", "精华", "精華"],
        ["Strongbox"]      = ["Strongbox", "保险箱", "保險箱"],
        ["Shrine"]         = ["Shrine", "神龛", "神龕"],
        ["Delirium"]       = ["Delirium", "迷雾", "迷霧"],
        ["Legion"]         = ["Legion", "军团", "軍團"],
        ["Temple"]         = ["Temple", "神庙", "神廟"],
        ["Incursion"]      = ["Incursion", "阿尔瓦神庙", "阿爾瓦神廟"],
        ["WaygateDevice"]  = ["Waygate Device", "神庙传送装置", "神廟傳送裝置"],
        // ── POI / marker labels ──
        ["Waypoint"]       = ["Waypoint", "传送点", "傳送點"],
        ["Checkpoint"]     = ["Checkpoint", "记录点", "記錄點"],
        ["Portal"]         = ["Portal", "传送门", "傳送門"],
        ["Town Portal"]    = ["Town Portal", "城镇传送门", "城鎮傳送門"],
        ["Stash"]          = ["Stash", "仓库", "倉庫"],
        ["Boss"]           = ["Boss", "首领", "首領"],
        ["Stairs"]         = ["Stairs", "楼梯", "樓梯"],
        ["Entrance"]       = ["Entrance", "入口", "入口"],
        ["Exit"]           = ["Exit", "出口", "出口"],
        ["Transition"]     = ["Transition", "出入口", "出入口"],
        ["Quest Marker"]   = ["Quest Marker", "任务标记", "任務標記"],
        ["Quest Object"]   = ["Quest Object", "任务目标", "任務目標"],
        ["Quest Chest"]    = ["Quest Chest", "任务宝箱", "任務寶箱"],
        ["Reforging Bench"] = ["Reforging Bench", "重铸台", "重鑄台"],
        ["Crafting Bench"] = ["Crafting Bench", "工艺台", "工藝台"],
        ["Abyss Crack"]    = ["Abyss Crack", "深渊裂隙", "深淵裂隙"],
        // ── Entity categories ──
        ["Player"]         = ["Player", "玩家", "玩家"],
        ["NPC"]            = ["NPC", "NPC", "NPC"],
        ["Monster"]        = ["Monster", "怪物", "怪物"],
        ["Chest"]          = ["Chest", "宝箱", "寶箱"],
        ["Unique Chest"]   = ["Unique Chest", "传奇宝箱", "傳奇寶箱"],
        ["Rare Chest"]     = ["Rare Chest", "稀有宝箱", "稀有寶箱"],
        ["Landmark"]       = ["Landmark", "地标", "地標"],
        ["Point of Interest"] = ["Point of Interest", "兴趣点", "興趣點"],
    };

    /// <summary>Translate a UI term by key. Falls back to the key itself when unknown.</summary>
    public string T(string key)
        => Terms.TryGetValue(key, out var v) ? v[(int)_lang] : key;

    /// <summary>
    /// Translate a KNOWN on-map label term. Returns <paramref name="text"/> unchanged when it isn't a
    /// known term, so English game-content names pass through until the full tables are imported.
    /// </summary>
    public string Term(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return Terms.TryGetValue(text, out var v) ? v[(int)_lang] : text;
    }

    /// <summary>
    /// Localized entity name for a metadata path. Currently delegates to the English
    /// <see cref="EntityNameResolver"/>; this is the hook point for the imported zh name tables.
    /// </summary>
    public string Name(string metadataPath)
        => EntityNameResolver.Shared.ResolveOrShorten(metadataPath);
}
