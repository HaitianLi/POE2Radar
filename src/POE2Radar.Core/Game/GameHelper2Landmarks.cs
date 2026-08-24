namespace POE2Radar.Core.Game;

/// <summary>
/// The curated tile-target lists from the GameHelper2 Radar plugin
/// (<c>boss_arena_tgt_files.txt</c> + <c>stairs_tgt_files.txt</c>) — the "reference" completeness
/// layer the local radar lacks: endgame-map boss arenas and dungeon stairs, keyed by terrain tile
/// path. Surfaced as landmarks when <c>RadarSettings.UseGh2Landmarks</c> is on.
///
/// <para>Ported from <c>Gordin/GameHelper2</c> (Plugins/Radar). The keys are stored in the local
/// cleaned form: <c>.tdtx:0-y:0</c> → <c>.tdt</c> (the in-memory <c>TgtFileStruct.TgtPath</c> form),
/// matching <see cref="CustomLandmarkData"/>'s convention.</para>
/// </summary>
public static class GameHelper2Landmarks
{
    // (tile path .tdt, label) — the label is the radar's on-map term, localized via Localization.Term.
    private static readonly (string Path, string Label)[] Global =
    [
        // ── boss_arena_tgt_files.txt ──
        ("Metadata/Terrain/Maps/Bluff/Tiles/Feature/Beacon_01.tdt", "Boss"),
        ("Metadata/Terrain/Maps/LostTowers/Tiles/PillarArena01.tdt", "Boss"),
        ("Metadata/Terrain/Maps/Mesa/Tiles/Peak.tdt", "Boss"),
        ("Metadata/Terrain/Maps/Overgrown/Tiles/OvergrownRuinArena_01.tdt", "Boss"),
        ("Metadata/Terrain/Dungeon/Machinarium/BossWall01.tdt", "Boss"),
        ("Metadata/Terrain/Maps/UberDoryani/Tiles/Doryani_Arena_01.tdt", "Boss"),
        ("Metadata/Terrain/Jungle/SnakePit/MapAugury/AuguryArena.tdt", "Boss"),
        ("Metadata/Terrain/Maps/Spring/Tiles/SpringArena_01.tdt", "Boss"),
        ("Metadata/Terrain/Maps/SulphuricCaverns/Tiles/Features/SulphuricCaverns_Boss_01.tdt", "Boss"),
        ("Metadata/Terrain/Woods/Features_Cairns/CairnBrecon.tdt", "Boss"),
        ("Metadata/Terrain/Maps/Cenotes/Tiles/CenotesArena01.tdt", "Boss"),
        ("Metadata/Terrain/Maps/Sump/Tiles/SumpArena.tdt", "Boss"),
        ("Metadata/Terrain/Maps/Penitentiary/Tiles/VillageArena_Garrison.tdt", "Boss"),
        ("Metadata/Terrain/Desert/Stromatolite/CcCc_ArenaGate.tdt", "Boss"),
        ("Metadata/Terrain/Maps/Rupture/Tiles/arena_01.tdt", "Boss"),
        ("Metadata/Terrain/Maps/Mesa/Tiles/PeakArena_01.tdt", "Boss"),
        ("Metadata/Terrain/Desert/Oasis/Features/OasisArena01.tdt", "Boss"),
        ("Metadata/Terrain/Woods/WideRiver/CcSM_01.tdt", "Boss"),
        ("Metadata/Terrain/Maps/Necropolis/Tiles/Boss_ArenaFloor.tdt", "Boss"),
        // ── stairs_tgt_files.txt ──
        ("Metadata/Terrain/Dungeon/Ziggurat/ziggurat_outerwall_end_stairsup_01.tdt", "Stairs"),
        ("Metadata/Terrain/Maps/SwampTower/Tiles/Lower_Exterior/SwampTower_BasementOuterWall_EndStairsUp_01.tdt", "Stairs"),
        ("Metadata/Terrain/Maps/SwampTower/Tiles/Lower_Exterior/SwampTower_BasementOuterWall_EndStairsUp_02.tdt", "Stairs"),
        ("Metadata/Terrain/Maps/SwampTower/Tiles/Mid_Exterior/SwampTower_OuterWallEnd_StairsUp_02.tdt", "Stairs"),
        ("Metadata/Terrain/Maps/SwampTower/Tiles/CentrePillar_02.tdt", "Stairs"),
        ("Metadata/Terrain/Maps/SwampTower/Tiles/Mid_Exterior/SwampTower_OuterWallEnd_StairsUp_01.tdt", "Stairs"),
        ("Metadata/Terrain/Dungeon/Ziggurat/ziggurat_outerwall_end_stairsup_02.tdt", "Stairs"),
    ];

    /// <summary>Number of ported GH2 tile targets (boss arenas + stairs).</summary>
    public static int Count => Global.Length;

    /// <summary>Return "Boss"/"Stairs" when the in-memory tile path matches a GH2 target, else null.
    /// Substring match (OrdinalIgnoreCase) on the cleaned <c>.tdt</c> path — same semantics as
    /// <see cref="CustomLandmarkData.TryMatch"/>.</summary>
    public static string? TryMatch(string tilePath)
    {
        if (string.IsNullOrEmpty(tilePath)) return null;
        foreach (var (path, label) in Global)
            if (tilePath.Contains(path, StringComparison.OrdinalIgnoreCase))
                return label;
        return null;
    }
}
