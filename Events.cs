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

		// Check if the userId is in the "_awaitingChatTagUsers" List
		if (Instance._awaitingChatTagUsers.Contains(player.SteamID))
		{
			// Set the player's chat tag
			if (playerData != null)
			{
				playerData.ChatTag = command;
			}
			else
			{
				Instance._userSettings[player.SteamID] = new UserSettings { ChatTag = command };
			}

			Instance._awaitingChatTagUsers.Remove(player.SteamID);
			Instance._api?.PrintToChat(player, Instance._api?.GetTranslatedText("tag.ChatTagSelected", command)!);
			Instance._api?.SetPlayerCookie(player.SteamID, "chatTag", command);
			Instance._tags?.MainMenu(player);

			return HookResult.Handled;
		}

		// Check if the user is in the "_awaitingScoreboardTagUsers" List
		if (Instance._awaitingScoreboardTagUsers.Contains(player.SteamID))
		{
			// Set the player's scoreboard tag
			if (playerData != null)
			{
				playerData.ScoreboardTag = command;
			}
			else
			{
				Instance._userSettings[player.SteamID] = new UserSettings { ScoreboardTag = command };
			}

			Instance._awaitingScoreboardTagUsers.Remove(player.SteamID);
			Instance._api?.PrintToChat(player, Instance._api?.GetTranslatedText("tag.ScoreboardTagSelected", command)!);
			Instance._api?.SetPlayerCookie(player.SteamID, "scoreboardTag", command);
			Instance._tags?.MainMenu(player);

			return HookResult.Handled;
		}

		// Get the player's tag data
		if (playerData == null)
		{
			return HookResult.Continue;
		}

		bool teammessage = info.GetArg(0) == "say_team";
		string deadname = player.PawnIsAlive ? string.Empty : Instance.Config.Settings["deadname"];

		string? tag = playerData.ChatTag;
		string? tagColor = playerData.ChatTagColor;
		string? namecolor = playerData.NameColor;
		string? chatcolor = playerData.ChatColor;

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
						player.Clan = playerData.ScoreboardTag;
						Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
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
					player.Clan = playerData.ScoreboardTag;
					Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
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
						player.Clan = playerData.ScoreboardTag;
						Utilities.SetStateChanged(player, "CCSPlayerController", "m_szClan");
					}
				}
			}
		}

		return HookResult.Continue;
	}
}
