using FrooxEngine;
using HarmonyLib;
using NeosModLoader;
using System;
using FrooxEngine.UIX;
using FrooxEngine.LogiX;
using FrooxEngine.LogiX.WorldModel;
using BaseX;
using FrooxEngine.LogiX.References;
using FrooxEngine.LogiX.Cast;

namespace RefEditorUtils
{
    public class RefEditorUtils : NeosMod
    {
        public override string Name => "RefEditorUtils";
        public override string Author => "badhaloninja";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/badhaloninja/RefEditorUtils";
        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("me.badhaloninja.RefEditorUtils");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(RefEditor), "Setup")]
        class RefEditor_Setup_Patch
        {
            public static void Postfix(RefEditor __instance, ISyncRef target)
            {
                if (target.TargetType == typeof(User))
                {
                    UIBuilder ui = new UIBuilder(__instance.Slot[0]);
                    ui.Root[ui.Root.ChildrenCount - 1].OrderOffset = 99; // Offset clear button

                    ui.Style.FlexibleWidth = -1f;
                    ui.Style.MinWidth = 24f;
                    var button = ui.Button("U");
                    var logixRoot = button.Slot.AddSlot("logix");

                    var refSet = button.Slot.AttachComponent<ButtonReferenceSet<User>>();
                    refSet.TargetReference.TrySet(target);

                    var localUser = logixRoot.AttachComponent<LocalUser>();
                    var cast = logixRoot.AttachComponent<CastClass<User,IWorldElement>>();
                    var toRefId = logixRoot.AttachComponent<ReferenceID>();
                    var drive = logixRoot.AttachComponent<DriverNode<RefID>>();

                    cast.In.TrySet(localUser);
                    toRefId.Element.TrySet(cast);
                    drive.Source.TrySet(toRefId);

                    drive.DriveTarget.TrySet(refSet.SetReference);

                    return;
                }
            }
        }
    }
}