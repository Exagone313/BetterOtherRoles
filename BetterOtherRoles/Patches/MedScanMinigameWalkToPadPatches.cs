using System.Collections;
using System.Linq;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using BetterOtherRoles.Modules;
using HarmonyLib;
using UnityEngine;

namespace BetterOtherRoles.Patches;

[HarmonyPatch(typeof(MedScanMinigame), nameof(MedScanMinigame.WalkToPad))]
public static class MedScanMinigameWalkToPadPatches
{
    private static CustomOption RandomizeScanPlayerPosition => CustomOptionHolder.RandomizePositionDuringScan;

    [HarmonyPrefix]
    private static bool Prefix(MedScanMinigame __instance, ref Il2CppSystem.Collections.IEnumerator __result)
    {
        if (!RandomizeScanPlayerPosition.getBool() || !(Helpers.isPolus() || Helpers.isMira() || Helpers.isSkeld() || SubmergedCompatibility.IsSubmerged)) return true;
        __result = WalkToPadEnumerator(__instance).WrapToIl2Cpp();
        return false;
    }
    
    private static IEnumerator WalkToPadEnumerator(MedScanMinigame minigame)
    {
        GameObject panel = null;
        if (SubmergedCompatibility.IsSubmerged)
        {
            panel = Object.FindObjectsOfType<GameObject>()
                .FirstOrDefault(o => o.name == "console_medscan");
        }
        else if (Helpers.isPolus() || (ShipStatus.Instance && ShipStatus.Instance.Type == ShipStatus.MapType.Pb))
        {
            panel = Object.FindObjectsOfType<GameObject>()
                .FirstOrDefault(o => o.name == "panel_medplatform");
        }
        else if (Helpers.isSkeld() || Helpers.isMira())
        {
            panel = Object.FindObjectsOfType<GameObject>()
                .FirstOrDefault(o => o.name == "MedScanner");
        }

        if (panel == null || Camera.main == null) yield break;
                
        var panelSize = panel.GetComponent<SpriteRenderer>().bounds.size * 0.3f;
            
        minigame.state = MedScanMinigame.PositionState.WalkingToPad;
        var myPhysics = PlayerControl.LocalPlayer.MyPhysics;

        Vector2 worldPos = ShipStatus.Instance.MedScanner.Position;
        var xRange = UnityEngine.Random.Range(-panelSize.x, panelSize.x);
        var yRange = UnityEngine.Random.Range(-panelSize.y, 0f);
        worldPos += new Vector2(xRange, yRange);

        Camera.main.GetComponent<FollowerCamera>().Locked = false;
        yield return myPhysics.WalkPlayerTo(worldPos, 0.001f, 1f);
        yield return new WaitForSeconds(0.1f);
        Camera.main.GetComponent<FollowerCamera>().Locked = true;
        minigame.walking = null;
    }
}