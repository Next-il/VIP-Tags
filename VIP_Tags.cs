using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Capabilities;
using VipCoreApi;
using static CounterStrikeSharp.API.Core.Listeners;
using static VipCoreApi.IVipCoreApi;

namespace VIP_Tags;

[MinimumApiVersion(246)]
public partial class VIP_Tags : BasePlugin, IPluginConfig<TagsConfig>
{
	public override string ModuleAuthor => "ShiNxz";
	public override string ModuleName => "[VIP] Tags";
	public override string ModuleVersion => "v1.0.0";

	public TagsConfig Config { get; set; } = new();
	public static VIP_Tags Instance { get; private set; } = new();

	private IVipCoreApi? _api;
	private Tags? _tags;

	public int GlobalTick { get; set; }

	private readonly Dictionary<ulong, UserSettings?> _userSettings = [];

	private readonly Dictionary<ulong, (string type, int awaitingUnixTime)> _awaitingTags = [];

	private PluginCapability<IVipCoreApi> PluginCapability { get; } = new("vipcore:core");

	public override void OnAllPluginsLoaded(bool hotReload)
	{
		_api = PluginCapability.Get();
		if (_api == null) return;

		_tags = new Tags(_api, this, _userSettings);
		_api.RegisterFeature(_tags, FeatureType.Selectable);
	}

	public override void Load(bool hotReload)
	{
		base.Load(hotReload);

		Instance = this;

		AddCommandListener("say", OnPlayerChat, HookMode.Pre);
		AddCommandListener("say_team", OnPlayerChat, HookMode.Pre);
		RegisterEventHandler<EventRoundStart>(OnRoundStart);
		RegisterListener<OnTick>(OnTick);
	}

	public override void Unload(bool hotReload)
	{
		if (_api != null && _tags != null)
		{
			_api?.UnRegisterFeature(_tags);
		}

		RemoveCommandListener("say", OnPlayerChat, HookMode.Pre);
		RemoveCommandListener(name: "say_team", OnPlayerChat, HookMode.Pre);
		RemoveListener<OnTick>(OnTick);
	}

	public void OnConfigParsed(TagsConfig config)
	{
		Console.WriteLine("[VIP Tags] Config parsed!");
		Config = config;
		Instance = this;
	}

	public class UserSettings
	{
		public string? ScoreboardTag { get; set; } = null;
		public string? ChatTag { get; set; } = null;
		public string? ChatTagColor { get; set; } = null;
		public string? NameColor { get; set; } = null;
		public string? ChatColor { get; set; } = null;
	}

	public partial class Tags : VipFeatureBase
	{
		public override string Feature => "Tags";

		private readonly Dictionary<ulong, UserSettings?> _userSettings;
		private readonly VIP_Tags _app;

		public Tags(IVipCoreApi api, VIP_Tags app, Dictionary<ulong, UserSettings?> userSettings) : base(api)
		{
			_userSettings = userSettings;
			_app = app;

			app.RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
			{
				CCSPlayerController? player = @event.Userid;
				if (player == null)
				{
					return HookResult.Continue;
				}

				if (IsClientVip(player))
				{
					string ScoreboardTagCookie = GetPlayerCookie<string>(player.SteamID, "scoreboardTag");
					string ChatTagCookie = GetPlayerCookie<string>(player.SteamID, "chatTag");
					string ChatTagColorCookie = GetPlayerCookie<string>(player.SteamID, "chatTagColor");
					string NameColorCookie = GetPlayerCookie<string>(player.SteamID, "nameColor");
					string ChatColorCookie = GetPlayerCookie<string>(player.SteamID, "chatColor");

					_userSettings[player.SteamID] = new UserSettings
					{
						ScoreboardTag = ScoreboardTagCookie,
						ChatTag = ChatTagCookie,
						ChatTagColor = ChatTagColorCookie,
						NameColor = NameColorCookie,
						ChatColor = ChatColorCookie
					};
				}

				return HookResult.Continue;
			});

			app.RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
			{
				var player = @event.Userid;
				if (player == null) return HookResult.Continue;

				if (userSettings.ContainsKey(player.SteamID))
				{
					userSettings[player.SteamID] = null;
				}

				return HookResult.Continue;
			});
		}

		public override void OnSelectItem(CCSPlayerController player, FeatureState state)
		{
			if (IsClientVip(player))
			{
				MainMenu(player);
			}
		}

		public void MainMenu(CCSPlayerController player)
		{
			CenterHtmlMenu Menu = new("VIP Tag", _app);

			_userSettings.TryGetValue(player.SteamID, out UserSettings? playerData);
			string? defaultColor = "Default";

			Menu.AddMenuOption($"Scoreboard: {playerData?.ScoreboardTag ?? "None"}", (controller, option) => HandleChooseScoreboardTag(player));
			Menu.AddMenuOption($"Chat Tag: {playerData?.ChatTag ?? "None"}", (controller, option) => HandleChooseChatTag(player));
			Menu.AddMenuOption($"Chat Tag Color: {playerData?.ChatTagColor ?? "None"}", (controller, option) => OpenTagColorMenu(player));
			Menu.AddMenuOption($"Name Color: {playerData?.NameColor ?? defaultColor}", (controller, option) => OpenNameColorMenu(player));
			Menu.AddMenuOption($"Chat Color: {playerData?.ChatColor ?? defaultColor}", (controller, option) => OpenChatColorMenu(player));

			MenuManager.OpenCenterHtmlMenu(_app, player, Menu);
		}

		private void HandleChooseChatTag(CCSPlayerController player)
		{
			AwaitOrExtendTag(player.SteamID, "chat");
			PrintToChat(player, ReplaceLineBreaks(GetTranslatedText("tag.ChatTagPrompt")));
			MenuManager.CloseActiveMenu(player);
		}

		private void HandleChooseScoreboardTag(CCSPlayerController player)
		{
			AwaitOrExtendTag(player.SteamID, "scoreboard");
			PrintToChat(player, ReplaceLineBreaks(GetTranslatedText("tag.ScoreboardTagPrompt")));
			MenuManager.CloseActiveMenu(player);
		}

		public static void StopAwaitingTag(ulong steamId)
		{
			Instance._awaitingTags.Remove(steamId);
		}

		public void AwaitOrExtendTag(ulong steamId, string type)
		{
			if (Instance._awaitingTags.TryGetValue(steamId, out (string type, int awaitingUnixTime) value))
			{
				if (value.type == type)
				{
					Instance._awaitingTags[steamId] = (type, (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
				}
			}
			else
			{
				Instance._awaitingTags[steamId] = (type, (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
			}
		}

		private void OpenChatColorMenu(CCSPlayerController player)
		{
			CenterHtmlMenu menu = new("Chat Color", _app);

			foreach (var color in Colors)
			{
				_userSettings.TryGetValue(player.SteamID, out UserSettings? playerData);

				bool isSelected = playerData?.ChatColor == color;
				if (playerData?.ChatColor == null)
				{
					isSelected = color == "Default";
				}

				menu.AddMenuOption($"{color} {(isSelected ? "(Selected)" : "")}", (controller, option) =>
				{
					if (_userSettings.TryGetValue(player.SteamID, out UserSettings? value) && value != null)
					{
						value.ChatColor = color;
						PrintToChat(player, GetTranslatedText("tag.ColorSelected", color));
					}
					else
					{
						_userSettings[player.SteamID] = new UserSettings { ChatColor = color };
					}

					SetPlayerCookie(player.SteamID, "chatColor", color);
					MainMenu(player);
				});
			}

			MenuManager.OpenCenterHtmlMenu(_app, player, menu);
		}

		private void OpenTagColorMenu(CCSPlayerController player)
		{
			CenterHtmlMenu menu = new("Chat Tag Color", _app);

			foreach (var color in Colors)
			{
				_userSettings.TryGetValue(player.SteamID, out UserSettings? playerData);

				bool isSelected = playerData?.ChatTagColor == color;
				if (playerData?.ChatTagColor == null)
				{
					isSelected = color == "Default";
				}

				menu.AddMenuOption($"{color} {(isSelected ? "(Selected)" : "")}", (controller, option) =>
				{
					if (_userSettings.TryGetValue(player.SteamID, out UserSettings? value) && value != null)
					{
						value.ChatTagColor = color;
						PrintToChat(player, GetTranslatedText("tag.ColorSelected", color));
					}
					else
					{
						_userSettings[player.SteamID] = new UserSettings { ChatTagColor = color };
					}

					SetPlayerCookie(player.SteamID, "chatTagColor", color);
					MainMenu(player);
				});
			}

			MenuManager.OpenCenterHtmlMenu(_app, player, menu);
		}

		private void OpenNameColorMenu(CCSPlayerController player)
		{
			CenterHtmlMenu menu = new("Name Color", _app);

			foreach (var color in Colors)
			{
				_userSettings.TryGetValue(player.SteamID, out UserSettings? playerData);

				bool isSelected = playerData?.NameColor == color;
				if (playerData?.NameColor == null)
				{
					isSelected = color == "Default";
				}

				menu.AddMenuOption($"{color} {(isSelected ? "(Selected)" : "")}", (controller, option) =>
				{
					{
						if (_userSettings.TryGetValue(player.SteamID, out UserSettings? value) && value != null)
						{
							value.NameColor = color;
							PrintToChat(player, GetTranslatedText("tag.ColorSelected", color));
						}
						else
						{
							_userSettings[player.SteamID] = new UserSettings { NameColor = color };
						}

						SetPlayerCookie(player.SteamID, "nameColor", color);
						MainMenu(player);
					}
				});
			}

			MenuManager.OpenCenterHtmlMenu(_app, player, menu);
		}
	}
}
