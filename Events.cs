using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Commands;

namespace VIP_Tags;

public partial class VIP_Tags
{
	public static HookResult OnPlayerChat(CCSPlayerController? player, CommandInfo info)
	{
		if (player == null || info.GetArg(1).Length == 0)
		{
			return HookResult.Continue;
		}

		string command = info.GetArg(1);

		if (command == "cancel")
		{
			Instance._awaitingTags.Remove(player.SteamID);
			Instance._tags?.MainMenu(player);
			return HookResult.Handled;
		}

		if (CoreConfig.SilentChatTrigger.Any(i => command.StartsWith(i)))
		{
			return HookResult.Continue;
		}

		if (CoreConfig.PublicChatTrigger.Any(i => command.StartsWith(i)))
		{
			return HookResult.Continue;
		}

		if (Instance._api?.IsClientVip(player) == false)
		{
			return HookResult.Continue;
		}

		Instance._userSettings.TryGetValue(player.SteamID, out UserSettings? playerData);

		// Check if the userId is in the "_awaitingTags" List
		Instance._awaitingTags.TryGetValue(player.SteamID, out (string type, int awaitingUnixTime) awaitingTag);
		int currentTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		int delay = Instance.Config.TagTimeout;

		// Check if the awaitingTag exists and the didnt pass the timeout
		if (awaitingTag.type != null && currentTime < (awaitingTag.awaitingUnixTime + delay))
		{
			Instance._api?.PrintToChat(player, message: $"You have more {(awaitingTag.awaitingUnixTime + delay) - currentTime} seconds to enter your tag");

			// Check if the the tag, contains one of the blocked tags
			if (Instance.Config.BlockedTags.Any(tag => command.Contains(tag)))
			{
				Instance._tags?.AwaitOrExtendTag(player.SteamID, awaitingTag.type);
				Instance._api?.PrintToChat(player, Instance._api?.GetTranslatedText("tag.BlockedTagError")!);
				return HookResult.Handled;
			}

			// Check for min and max lengths, and restart -> cancel the timeout
			if (command.Length < Instance.Config.ChatTagMin || command.Length > Instance.Config.ChatTagMax)
			{
				Instance._tags?.AwaitOrExtendTag(player.SteamID, awaitingTag.type);

				switch (awaitingTag.type)
				{
					case "chat":
						Instance._api?.PrintToChat(player, Instance._api?.GetTranslatedText("tag.ChatTagLengthError", Instance.Config.ChatTagMin, Instance.Config.ChatTagMax)!);
						break;

					case "scoreboard":
						Instance._api?.PrintToChat(player, Instance._api?.GetTranslatedText("tag.ScoreboardTagLengthError", Instance.Config.ScoreboardTagMin, Instance.Config.ScoreboardTagMax)!);
						break;
				}

				return HookResult.Handled;
			}

			// Set the player's chat tag
			if (playerData != null)
			{
				switch (awaitingTag.type)
				{
					case "chat":
						playerData.ChatTag = command;
						break;

					case "scoreboard":
						playerData.ScoreboardTag = command;
						break;
				}
			}
			else
			{
				switch (awaitingTag.type)
				{
					case "chat":
						Instance._userSettings[player.SteamID] = new UserSettings { ChatTag = command };
						break;

					case "scoreboard":
						Instance._userSettings[player.SteamID] = new UserSettings { ScoreboardTag = command };
						player.Clan = command;
						Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
						break;
				}
			}

			Instance._awaitingTags.Remove(player.SteamID);
			Instance._tags?.MainMenu(player);

			switch (awaitingTag.type)
			{
				case "chat":
					Instance._api?.PrintToChat(player, Instance._api?.GetTranslatedText("tag.ChatTagSelected", command)!);
					Instance._api?.SetPlayerCookie(player.SteamID, "chatTag", command);
					break;
				case "scoreboard":
					Instance._api?.PrintToChat(player, Instance._api?.GetTranslatedText("tag.ScoreboardTagSelected", command)!);
					Instance._api?.SetPlayerCookie(player.SteamID, "scoreboardTag", command);
					break;
			}

			return HookResult.Handled;
		}

		// Get the player's tag data
		if (playerData == null)
		{
			return HookResult.Continue;
		}

		bool teammessage = info.GetArg(0) == "say_team";
		string deadname = player.PawnIsAlive ? string.Empty : Instance.Config.Settings["deadname"];

		string? tag = Instance.Config.ChatTagEnabled ? playerData.ChatTag : null;
		string? tagColor = Instance.Config.ChatTagColorEnabled ? playerData.ChatTagColor : null;
		string? namecolor = Instance.Config.NameColorEnabled ? playerData.NameColor : null;
		string? chatcolor = Instance.Config.ChatColorEnabled ? playerData.ChatColor : null;

		string message = FormatMessage(deadname, teammessage ? TeamName(player.Team) : string.Empty, tag, tagColor, namecolor, chatcolor, player, command);

		static string FormatMessage(string deadIcon, string teamname, string? tag, string? tagColor, string? namecolor, string? chatcolor, CCSPlayerController player, string text)
		{
			return ReplaceTags($" {deadIcon} {teamname} {WrapWithBraces(tagColor)}{tag} {(namecolor != null ? WrapWithBraces(namecolor) : ChatColors.ForPlayer(player))}{player.PlayerName}{ChatColors.Default}: {WrapWithBraces(chatcolor)}{text}", player.Team);
		}

		if (info.GetArg(0) == "say_team")
		{
			foreach (CCSPlayerController target in Utilities.GetPlayers().Where(target => target.Team == player.Team && !target.IsBot))
			{
				target.PrintToChat(message);
			}
		}
		else
		{
			Server.PrintToChatAll(message);
		}

		return HookResult.Handled;
	}

	public static void OnTick()
	{
		if (++Instance.GlobalTick != 200)
		{
			return;
		}

		Instance.GlobalTick = 0;

		foreach (CCSPlayerController player in Utilities.GetPlayers())
		{
			if (Instance._api?.IsClientVip(player) == true)
			{
				if (Instance._userSettings.TryGetValue(player.SteamID, out UserSettings? playerData) && playerData != null)
				{
					if (playerData.ScoreboardTag != null)
					{
						if (Instance.Config.ScoreboardTagEnabled)
						{
							player.Clan = playerData.ScoreboardTag;
							Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
						}
					}
				}
			}
		}
	}

	public static void OnPlayerSpawn(CCSPlayerController player)
	{
		if (Instance._api?.IsClientVip(player) == true)
		{
			if (Instance._userSettings.TryGetValue(player.SteamID, out UserSettings? playerData) && playerData != null)
			{
				if (playerData.ScoreboardTag != null)
				{
					if (Instance.Config.ScoreboardTagEnabled)
					{
						player.Clan = playerData.ScoreboardTag;
						Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
					}
				}
			}
		}
	}

	public static HookResult OnRoundStart(EventRoundStart roundStartEvent, GameEventInfo info)
	{
		foreach (CCSPlayerController player in Utilities.GetPlayers())
		{
			if (Instance._api?.IsClientVip(player) == true)
			{
				if (Instance._userSettings.TryGetValue(player.SteamID, out UserSettings? playerData) && playerData != null)
				{
					if (playerData.ScoreboardTag != null)
					{
						if (Instance.Config.ScoreboardTagEnabled)
						{
							player.Clan = playerData.ScoreboardTag;
							Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
						}
					}
				}
			}
		}

		return HookResult.Continue;
	}
}
