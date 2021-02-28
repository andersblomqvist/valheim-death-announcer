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
    [BepInPlugin("com.branders.deathannouncermod", "Death Announcer Mod", "1.0.0.1")]
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

        private static Vector2 deathsTextPos;
        private static HudText deathText;

        /**
         *  Mod init
         */
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

            // Transform the config percentage values to screen pos
            deathsTextPos = new Vector2(Screen.width * deathsTextLocation.Value.x, Screen.height * deathsTextLocation.Value.y);

            var harmony = new Harmony("com.branders.deathannouncermod");
            harmony.PatchAll();
            Harmony.CreateAndPatchAll(typeof(DeathAnnouncerMod));
        }

        /**
         *  Init GUI elements
         */
        [HarmonyPatch(typeof(Hud), "Awake")]
        [HarmonyPostfix]
        private static void SetupGui()
        {
            // Number of deaths text under the minimap
            deathText = new HudText(HudText.Style.Rounded, Color.white, 16);
        }

        /**
         *  Adds the command: 
         *      deaths          prints out number of deaths for local player
         *      deaths show     show GUI text with number of deaths
         */
        [HarmonyPatch(typeof(Console), "InputText")]
        [HarmonyPostfix]
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
        private static void SendDeathMessage(Player __instance)
        {
            int deaths = GetPlayerDeaths() + 1;

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
                // Check config enable/disable
                if (!enableDeathsText.Value)
                    return;

                deathText.Render(deathsTextPos, $"Deaths: {GetPlayerDeaths()}");
            }
        }

        /**
         *  Returns the number of deaths for local player
         */
        private static int GetPlayerDeaths()
        {
            return Game.instance.GetPlayerProfile().m_playerStats.m_deaths;
        }
    }
}
