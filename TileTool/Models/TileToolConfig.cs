using System.Text.Json.Serialization;

namespace TileTool.Models;

public class TileToolConfig
{
    [JsonPropertyName("outputFolder")]
    public string OutputFolder { get; set; } = string.Empty;

    [JsonPropertyName("prefix")]
    public string Prefix { get; set; } = "tile";

    [JsonPropertyName("defaultSelectionWidth")]
    public int DefaultSelectionWidth { get; set; } = 32;

    [JsonPropertyName("defaultSelectionHeight")]
    public int DefaultSelectionHeight { get; set; } = 32;

    [JsonPropertyName("nextTileIndex")]
    public int NextTileIndex { get; set; } = 1;
}
