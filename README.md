**[C#] [VIP] Module - Tags** Players can choose their own custom clan tag, chat tag, and colors for their names, tag and chat messages.
this plugin is built for the [VIP](https://github.com/partiusfabaa/cs2-VIPCore) and used as a module.

## Installation

1. Install the [VIP Core](https://github.com/partiusfabaa/cs2-VIPCore) plugin.
2. Download this module [here](https://github.com/Next-il/VIP-Tags/releases).
3. Unpack and place the plugin in `game/csgo/addons/counterstrikesharp/plugins/VIP_Tags`
4. After the first launch, configure the plugin in `addons/counterstrikesharp/configs/plugins/VIP_Tags/VIP_Tags.json`
5. Add the translations to `addons\counterstrikesharp\plugins\VIPCore\lang\(lang).json`.
6. Restart the server

## Features
💎 Choose custom clan (scoreboard) tag with chat input. <br />
💎 Choose custom chat tag (before the player name) with a custom chat input. <br />
💎 Choose custom colors for the clan tag, chat tag, and player name. <br />
💎 Compatible with admin commands (gags). <br />


## Configuration file (VIP_Tags.json)

```
{
  "ConfigVersion": 1,
  "ChatSettings": {
    "deadname": "[DEAD]",
    "nonename": "{White}(NONE)",
    "specname": "{Purple}(SPEC)",
    "tname": "{Yellow}(T)",
    "ctname": "{Blue}(CT)"
  },
  "TagTimeout": 20,
}
```

## Translations

```
{
	...
    "tag.ChatTagPrompt": "Enter the tag you want to use in the chat",
	"tag.ChatTagSelected": "Your chat tag is now: {lime}{0}",
	"tag.ScoreboardTagPrompt": "Enter the tag you want to use in the scoreboard",
	"tag.ColorSelected": "You have selected the color: {lime}{0}",
	"tag.TagTimeout": "Time is up, if you want to set a tag please use the command again",
	"tag.ScoreboardTagSelected": "Your scoreboard tag is now: {lime}{0}"
}
```
add to `addons\counterstrikesharp\plugins\VIPCore\lang\(lang).json`