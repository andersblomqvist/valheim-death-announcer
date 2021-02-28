using System.Collections;
using UnityEngine;
using BepInEx;
using HarmonyLib;

/**
 *  Draws a GUI text label which works as other Valheim hud elements. It's displayed when other GUI elements
 *  such as minimap, toolbar etc are displayed.
 *  
 *  Call Draw() method for rendering the GUI text label.
 */
namespace DeathAnnouncer
{
    public class HudText
    {
        /**
         *  Prebuilt GUIStyles which matches Valheim text styles.
         */
        public enum Style
        {
            Rune,
            Rounded,
        }

        // Styling of the text
        private GUIStyle m_style;
        private GUIStyle m_shadow_style;

        private Vector2 shadowOffset = new Vector2(-2, 2);

        public HudText(Style style, Color color, int fontSize)
        {
            m_style = SetStyle(style, fontSize);
            m_style.normal.textColor = color;

            m_shadow_style = SetStyle(style, fontSize);
            m_shadow_style.normal.textColor = Color.black;
        }

        /**
         *  Render the GUI label at specified position
         */
        public void Render(Vector2 position, string text)
        {
            // Hide if player is in screen shot mode (Ctrl + F3)
            if (Hud.IsUserHidden())
                return;

            // Hide GUI text when it should not show. This is when other Valheim GUI also is hidden, mostly.
            if (Player.m_localPlayer.IsDead()
                || Player.m_localPlayer.InCutscene()
                || Player.m_localPlayer.IsTeleporting()
                || InventoryGui.IsVisible()
                || Minimap.IsOpen())
                return;

            GUI.Label(new Rect(position + shadowOffset, new Vector2(0, 0)), text, m_shadow_style);
            GUI.Label(new Rect(position, new Vector2(0, 0)), text, m_style);
        }

        /**
         *  Create the GUIStyle for matching Valheim fonts
         */
        private GUIStyle SetStyle(Style style, int fsize)
        {
            GUIStyle s = new GUIStyle();

            if(style == Style.Rune)
            {
                s = new GUIStyle
                {
                    richText = true,
                    fontSize = fsize,
                    alignment = TextAnchor.UpperLeft,
                    font = GetFont("Norse"),
                };
            }

            else if (style == Style.Rounded)
            {
                s = new GUIStyle
                {
                    richText = true,
                    fontSize = fsize,
                    alignment = TextAnchor.UpperLeft,
                    font = GetFont("AveriaSerifLibre-Bold"),
                };
            }

            return s;
        }


        /**
         *  Returns the specified font by font name.
         */
        private Font GetFont(string fontName)
        {
            // Search for the font and return it
            foreach (Font font in ResouceLoader.fonts)
            {
                if (font.name.Equals(fontName))
                    return font;
            }

            Debug.LogWarning($"[DeathAnnouncer] Could not find specified font: {fontName}");
            return ResouceLoader.fonts[0];
        }
    }
}
