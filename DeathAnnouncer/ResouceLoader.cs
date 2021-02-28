using UnityEngine;
using HarmonyLib;

/**
 *  Fonts needs to be loaded in the Awake of the FejdStartup. This class will store all the fonts
 *  in an array which can be access directly from other classes. However, GUI styles which uses
 *  these fonts need to be created after this which is appropriate to do in Awake() of Hud class.
 */
namespace DeathAnnouncer
{
    [HarmonyPatch(typeof(FejdStartup), "Awake")]
    public class ResouceLoader
    {
        public static Font[] fonts = {};

        static void Postfix()
        {
            fonts = Resources.FindObjectsOfTypeAll<Font>();
        }
    }
}
