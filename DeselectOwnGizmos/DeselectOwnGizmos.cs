using Elements.Core;
using FrooxEngine;
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.NET.Common;
using BepInExResoniteShim;
using FrooxEngine.ProtoFlux;


namespace DeselectOwnGizmos
{
    [ResonitePlugin(PluginMetadata.GUID, PluginMetadata.NAME, PluginMetadata.VERSION, PluginMetadata.AUTHORS, PluginMetadata.REPOSITORY_URL)]
    [BepInDependency(BepInExResoniteShim.PluginMetadata.GUID)]
    public class DeselectOwnGizmos : BasePlugin
    {
        private static ConfigEntry<bool>? ShowOnProtoFluxTool;

        public override void Load()
        {
            ShowOnProtoFluxTool = Config.Bind(
                "General",
                "ShowOnProtoFluxTool",
                false,
                "Adds deselect button to the Protoflux tool"
            );

            HarmonyInstance.PatchAll();

            Log.LogInfo("DeselectOwnGizmos mod loaded!");
        }

        const string DeselectOwnIcon = "6c6fe0b17b9f9fc07d9c47363988eb98560ff2daf81132bc041bb4ef6a487c18";

        [HarmonyPatch]
        static class ToolPatches
        {

            [HarmonyPostfix]
            [HarmonyPatch(typeof(DevTool), "GenerateMenuItems")]
            public static void AddDeselectButton(DevTool __instance, ContextMenu menu, SyncRef<Slot> ____currentGizmo, SyncRef<Slot> ____previousGizmo)
            {
                Uri deselect = __instance.Cloud.Assets.GenerateURL(DeselectOwnIcon);
                ContextMenuItem item = menu.AddItem("Deselect Own", deselect, colorX.White);

                item.Button.LocalPressed += (IButton button, ButtonEventData eventData) => Deselect(__instance, ____currentGizmo, ____previousGizmo);
            }
            
            [HarmonyPostfix]
            [HarmonyPatch(typeof(ProtoFluxTool), "GenerateMenuItems")]
            public static void FluxAddDeselectButton(ProtoFluxTool __instance, ContextMenu menu)
            {
                if (!ShowOnProtoFluxTool.Value) return;

                Uri deselect = __instance.Cloud.Assets.GenerateURL(DeselectOwnIcon);
                ContextMenuItem item = menu.AddItem("Deselect Own", deselect, colorX.White);

                item.Button.LocalPressed += (IButton button, ButtonEventData eventData) => Deselect(__instance);
            }


            private static void Deselect(Tool tool, SyncRef<Slot> currentGizmo = null, SyncRef<Slot> previousGizmo = null)
            {
                tool.World.RootSlot.GetComponentsInChildren<SlotGizmo>(IsLocalUserGizmo).ForEach((SlotGizmo s) => s.Slot.Destroy());
                tool.ActiveHandler?.CloseContextMenu();

                if (tool is DevTool devTool)
                {
                    currentGizmo.Target = null;
                    previousGizmo.Target = null;
                    SelectAnchor(devTool, null);
                }
            }
            static bool IsLocalUserGizmo(SlotGizmo gizmo) => gizmo.World.GetUserByAllocationID(gizmo.ReferenceID.User).IsLocalUser;


            [HarmonyReversePatch]
            [HarmonyPatch(typeof(DevTool), "SelectAnchor")]
            public static void SelectAnchor(DevTool instance, PointAnchor pointAnchor) => throw new NotImplementedException("It's a stub");
        }

        [HarmonyPatch(typeof(SlotRecord), "Pressed")]
        static class GenerateGizmoFromInspector
        {
            public static void Prefix(SlotRecord __instance, ref double ____lastPress)
            {
                if (__instance.World.IsAuthority || !(__instance.Time.WorldTime - ____lastPress < 0.35)) return;

                if (__instance.TargetSlot.Target != null && !__instance.TargetSlot.Target.IsRootSlot)
                    __instance.TargetSlot.Target?.GetGizmo();
            }
        }

        [HarmonyPatch(typeof(DevCreateNewForm), "OpenInspector")]
        static class GenerateGizmoCreateNew
        {
            public static void Prefix(Slot slot)
            {
                if (!slot.World.IsAuthority) slot.GetGizmo();
            }
        }
    }
}