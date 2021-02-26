using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

/**
 *  This mod adds a death announce text when a player dies. It shows the big yellow text in the middle
 *  of the screen for all other players online. It says player name and how many times that player has died.
 *  
 *  @version 2020-02-26
 *  @author Anders <Branders> Blomqvist
 */
namespace DeathAnnouncer
{
    [BepInPlugin("com.branders.deathannouncermod", "Death Announcer Mod", "1.0.0.0")]
    public class DeathAnnouncerMod : BaseUnityPlugin
    {
        // State all config values
        public static ConfigEntry<string> deathMessage;
        public static ConfigEntry<bool> enableDeathMessage;
        public static ConfigEntry<bool> enableShowDeathCount;
        public static ConfigEntry<bool> enableDeathsText;
        public static ConfigEntry<bool> enableChatMessage;
        public static ConfigEntry<bool> enableMapPing;
        public static ConfigEntry<Vector2> deathsTextLocation;

        // GUI elements for the Deaths: # text under the minimap
        private static Font deathFont;
        private static GUIStyle style;
        private static GUIStyle shadow;
        private static Vector2 deathsTextPos;
        private static Vector2 shadowOffset;

        private void Awake()
        {
            // Init config values
            deathMessage = Config.Bind("General", "DeathMessage", "have died!", "Message after player name: <player name> <message>");
            enableDeathMessage = Config.Bind("General", "EnableDeathMessage", true, "Toggle big death message on/off");
            enableShowDeathCount = Config.Bind("General", "ShowDeathCount", true, "Show the numbers of deaths in death message");
            enableDeathsText = Config.Bind("General", "EnableDeathsText", true, "Toggle the death text under minimap on/off");
            enableChatMessage = Config.Bind("General", "EnableChatMessage", true, "Toggle chat announce on/off");
            enableMapPing = Config.Bind("General", "EnableMapPing", true, "Toggle map ping on/off");
            deathsTextLocation = Config.Bind("General", "DeathsTextLocation", new Vector2(0.88f, 0.225f), "Location of the no. of deaths text (in screen percentage!) Ignore decimals");

            Harmony.CreateAndPatchAll(typeof(DeathAnnouncerMod));

            // Transform the config percentage values to screen pos
            deathsTextPos = new Vector2(Screen.width * deathsTextLocation.Value.x, Screen.height * deathsTextLocation.Value.y);
            shadowOffset = new Vector2(-2, 2);
        }

        /**
         *  On startup, retrieve list of fonts and find searched font
         */
        [HarmonyPatch(typeof(FejdStartup), "Awake")]
        [HarmonyPostfix]
        private static void GetFonts()
        {
            // Get font list
            Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();

            // Search for the font and set deathFont
            foreach(Font font in fonts)
            {
                if(font.name == "AveriaSerifLibre-Bold")
                {
                    deathFont = font;
                    break;
                }
            }

            // Style for the readable text
            style = new GUIStyle
            {
                richText = true,
                fontSize = 16,
                alignment = TextAnchor.UpperLeft,
                font = deathFont,
            };
            style.normal.textColor = Color.white;

            // Style for the shadow, same as above but black
            shadow = new GUIStyle
            {
                richText = true,
                fontSize = 16,
                alignment = TextAnchor.UpperLeft,
                font = deathFont,
                
            };
            shadow.normal.textColor = Color.black;
        }

        /**
         *  Adds the command: 
         *      deaths          prints out number of deaths for local player
         *      deaths show     show GUI text with number of deaths
         */
        [HarmonyPatch(typeof(Console), "InputText")]
        [HarmonyPrefix]
        private static void ConsoleInputText()
        {
            string cmd = Console.instance.m_input.text;
            
            if(cmd.StartsWith("deaths"))
            {
                if (cmd.Length < 7)
                    Console.instance.Print($"You have died: {GetPlayerDeaths()} times!");
                else if (cmd.Substring(7).Equals("show"))
                    enableDeathsText.BoxedValue = !enableDeathsText.Value;
                else
                    Console.instance.Print("Unknown deaths command. Try: deaths show");
            }
        }

        /**
         *  Called when local player dies. Sends out a death message to other player. 
         *  Messages depends on config aswell.
         *  
         *  @param __instance Local player reference
         */
        [HarmonyPatch(typeof(Player), "OnDeath")]
        [HarmonyPrefix]
        private static void NotifyOtherPlayers(Player __instance)
        {
            int deaths = GetPlayerDeaths();

            Debug.Log($"{__instance.GetPlayerName()} died: #{deaths}");

            // Send chat message and a ping on the map at death position
            if (enableChatMessage.Value)
                Chat.instance.SendText(Talker.Type.Shout, $"{__instance.GetPlayerName()} {deathMessage.Value} #{deaths}");

            // Do a map ping at death position
            if (enableMapPing.Value)
                Chat.instance.SendPing(__instance.transform.position);

            // Send message to all other players (will not show local client)
            if (enableDeathMessage.Value)
                MessageHud.instance.MessageAll(MessageHud.MessageType.Center, $"{__instance.GetPlayerName()} {deathMessage.Value} #{deaths}");
        }

        /**
         *  Draws the number of deaths label
         */
        private void OnGUI()
        {
            if (Player.m_localPlayer != null)
            {
                // Hide if player is in screen shot mode (Ctrl + F3)
                if (Hud.IsUserHidden())
                    return;

                // Hide if player is either dead or in a cut scene. Example of a cut scene is when the player logs in and rises up from the
                // ground. Other GUI are enabled when the player get to control the character.
                if (Player.m_localPlayer.IsDead() || Player.m_localPlayer.InCutscene())
                    return;

                // Check config enable/disable
                if (!enableDeathsText.Value)
                    return;

                GUI.Label(new Rect(deathsTextPos + shadowOffset, new Vector2(0, 0)), $"Deaths: {GetPlayerDeaths()}", shadow);
                GUI.Label(new Rect(deathsTextPos, new Vector2(0, 0)), $"Deaths: {GetPlayerDeaths()}", style);
            }
        }

        /**
         *  Returns the number of deaths local player has
         */
        private static int GetPlayerDeaths()
        {
            return Game.instance.GetPlayerProfile().m_playerStats.m_deaths;
        }
    }
}
