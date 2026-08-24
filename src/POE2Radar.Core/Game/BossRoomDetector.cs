namespace POE2Radar.Core.Game;

/// <summary>
/// Generic boss-room detection by terrain-tile name. Unlike the curated lists
/// (<see cref="CustomLandmarkData"/> campaign entries + <see cref="GameHelper2Landmarks"/>'s 19 endgame
/// arenas), this matcher needs no per-area entry: it flags ANY tile whose cleaned <c>.tdt</c> path
/// contains a boss-room signal word, so a boss arena surfaces even on maps the curated lists don't
/// cover yet. It complements (never replaces) the curated lists — both return the same "Boss" label,
/// and the landmark scan already de-duplicates per distinct tile path.
///
/// <para>Signal words are deliberately conservative (two): <c>boss</c> (covers "BossRoom", "BossWall",
/// "SulphuricCaverns_Boss_01", "Boss_ArenaFloor", …) and <c>arena</c> (covers "PillarArena01",
/// "Doryani_Arena_01", "AuguryArena", "arena_01", …). Together they match ~17 of the 19 GH2
/// boss-arena targets (missing only the oddly-named "Peak"/"Beacon_01"/"CairnBrecon"/"CcSM_01") plus
/// every campaign "…_Bossroom…" tile — with essentially no false positives, since PoE2 terrain tiles
/// don't use "boss"/"arena" for non-boss geometry. Case-insensitive substring match, same semantics
/// as <see cref="GameHelper2Landmarks.TryMatch"/>.</para>
/// </summary>
public static class BossRoomDetector
{
    private static readonly string[] Keywords = ["boss", "arena"];

    /// <summary>Return the label "Boss" when the tile path looks like a boss room, else null.</summary>
    public static string? TryMatch(string tilePath)
    {
        if (string.IsNullOrEmpty(tilePath)) return null;
        foreach (var k in Keywords)
            if (tilePath.Contains(k, StringComparison.OrdinalIgnoreCase))
                return "Boss";
        return null;
    }
}
