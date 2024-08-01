using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace VIP_Tags
{
	public class TagsConfig : BasePluginConfig
	{
		[JsonPropertyName("ConfigVersion")] public override int Version { get; set; } = 1;

		[JsonPropertyName("ChatSettings")]
		public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>
		{
			{ "deadname", "[DEAD]" },
			{ "nonename", "{White}(NONE)" },
			{ "specname", "{Purple}(SPEC)" },
			{ "tname", "{Yellow}(T)" },
			{ "ctname", "{Blue}(CT)" }
		};

		[JsonPropertyName("TagTimeout")]
		public int TagTimeout { get; set; } = 15;

		[JsonPropertyName("ScoreboardTagMin")]
		public int ScoreboardTagMin { get; set; } = 1;
		[JsonPropertyName("ScoreboardTagMax")]
		public int ScoreboardTagMax { get; set; } = 17;

		[JsonPropertyName("ChatTagMin")]
		public int ChatTagMin { get; set; } = 1;
		[JsonPropertyName("ChatTagMax")]
		public int ChatTagMax { get; set; } = 17;
	}
}
