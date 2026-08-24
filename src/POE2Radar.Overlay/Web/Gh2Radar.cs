namespace POE2Radar.Overlay.Web;

/// <summary>
/// The GameHelper2 Radar plugin's entity/terrain icon recognitions, ported as a ready-made
/// <see cref="DisplayRule"/> list. Installed as an overlay layer on top of the user's editable
/// ruleset when <c>RadarSettings.UseGh2Radar</c> is on, so the two radars can be compared live
/// from the dashboard without touching the user's rules.
///
/// <para>Only the recognitions the local radar LACKS are ported here (the mechanics the local
/// seeded rules already cover — Ritual / Breach / Essence / Strongbox / Shrine / Expedition
/// marker — are left alone). Each rule is metadata-substring + category-gated, matching the
/// local <see cref="MechanicStyle"/> convention; shapes/colours are the closest local SVG
/// equivalents of GH2's icons.png sprites.</para>
/// </summary>
public static class Gh2Radar
{
    /// <summary>Build the additive GH2 icon/landmark recognitions (ordered; first enabled match wins).</summary>
    public static IReadOnlyList<DisplayRule> BuildRules() => new[]
    {
        // ── Azmeri Tormented Spirits (GH2 AzmeriSpiritIcons): the 12 capture-target spirits live under
        //    Metadata/Monsters/TormentedSpirits/TormentedSpiritofthe{…}. A distinct glyph so they stand
        //    out from combat monsters (they're chase/capture targets, not fight targets). ──
        new DisplayRule
        {
            Name = "Tormented Spirit", Categories = new() { "Monster" },
            Match = new() { "Metadata/Monsters/TormentedSpirits/" },
            Shape = "Gem", Color = "#FF7AE0", Opacity = 1f, Size = 6.5f, Label = "Tormented Spirit",
        },

        // ── Abyss (GH2 AbyssIcons): crack / pit / node objects. Gated to Object/Other so the league's
        //    combat monsters ("…/LeagueAbyss/…") don't get the marker. ──
        new DisplayRule
        {
            Name = "Abyss", Categories = new() { "Object", "Other" },
            Match = new() { "Abyss" },
            Shape = "Hexagon", Color = "#C44DFF", Opacity = 1f, Size = 6f, Label = "Abyss",
        },

        // ── Delirium (GH2 DeliriumIcons): the mirror spawners / shard bosses / bombs under
        //    Metadata/MiscellaneousObjects/Delirium/. ──
        new DisplayRule
        {
            Name = "Delirium", Categories = new() { "Other" },
            Match = new() { "Delirium" },
            Shape = "Diamond", Color = "#C9C9E8", Opacity = 1f, Size = 6f, Label = "Delirium",
        },

        // ── Incursion / Temple (GH2 TempleIcons): the Vaal-Ruins waygate device (entity form; the
        //    terrain-tile form is already covered by the built-in WaygateDevice Tile rule). ──
        new DisplayRule
        {
            Name = "Incursion", Categories = new() { "Other" },
            Match = new() { "Incursion" },
            Shape = "Eye", Color = "#F2E55A", Opacity = 1f, Size = 6f, Label = "Incursion",
        },

        // ── Sekhemas / Sanctum (GH2 SekhemasIcons): trial objects (relics, fountains, boons). ──
        new DisplayRule
        {
            Name = "Sekhemas", Categories = new() { "Object", "Other" },
            Match = new() { "Sanctum", "Sekhemas", "TrialOfTheSekhemas" },
            Shape = "Shield", Color = "#E0C070", Opacity = 1f, Size = 6f, Label = "Sekhemas",
        },

        // ── Expedition reward chests (GH2 ExpeditionMarkerIcons): distinct from the encounter marker
        //    (already covered locally) — these are the spawned reward chests. ──
        new DisplayRule
        {
            Name = "Expedition chest", Categories = new() { "Chest" },
            Match = new() { "Expedition" },
            Shape = "Chest", Color = "#FFD926", Opacity = 1f, Size = 5.5f, Label = "Expedition chest",
        },

        // ── Specific strongboxes (GH2 StrongboxPathMap). ──
        new DisplayRule
        {
            Name = "Research Strongbox", Categories = new() { "Chest" },
            Match = new() { "ResearchStrongbox" },
            Shape = "Chest", Color = "#FFB300", Opacity = 1f, Size = 5.5f, Label = "Research box",
        },
        new DisplayRule
        {
            Name = "Armourer Strongbox", Categories = new() { "Chest" },
            Match = new() { "ArmourerStrongbox" },
            Shape = "Chest", Color = "#FFB300", Opacity = 1f, Size = 5.5f, Label = "Armourer box",
        },

        // ── Campaign Runestones (GH2 RunestoneIcons, terrain tile form): every rune terrain variant
        //    lives under Metadata/Terrain/Leagues/Expedition/Tiles/CampaignRunes/. A Tile rule so the
        //    terrain scanner surfaces them as landmarks (the encounter DEVICE itself is already handled
        //    by the monolith overlay). ──
        new DisplayRule
        {
            Name = "Runestones", Categories = new() { "Tile" },
            Match = new() { "CampaignRunes" },
            Shape = "Gem", Color = "#8AE0FF", Opacity = 1f, Size = 5f, Label = "Runestones",
        },
    };
}
