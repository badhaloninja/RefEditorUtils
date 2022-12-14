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
                if(target.TargetType == typeof(Slot))
                {
                    UIBuilder ui = new UIBuilder(__instance.Slot[0]);
                    ui.Root[ui.Root.ChildrenCount - 1].OrderOffset = 99; // Offset clear button

                    ui.Style.FlexibleWidth = -1f;
                    ui.Style.MinWidth = 24f;


                    var rootBtn = ui.Button("R");

                    var refSetRoot = rootBtn.Slot.AttachComponent<ButtonReferenceSet<Slot>>();
                    refSetRoot.TargetReference.TrySet(target);
                    refSetRoot.SetReference.TrySet(__instance.World.RootSlot);

                    var button = ui.Button("LS");
                    var logixRoot = button.Slot.AddSlot("logix");

                    var refSet = button.Slot.AttachComponent<ButtonReferenceSet<Slot>>();
                    refSet.TargetReference.TrySet(target);

                    // Logix Support for others clicking the button and respecting local user
                    var localUser = logixRoot.AttachComponent<LocalUserSpace>();
                    var cast = logixRoot.AttachComponent<CastClass<Slot, IWorldElement>>();
                    var toRefId = logixRoot.AttachComponent<ReferenceID>();
                    var drive = logixRoot.AttachComponent<DriverNode<RefID>>();


                    cast.In.TrySet(localUser);
                    toRefId.Element.TrySet(cast);
                    drive.Source.TrySet(toRefId);

                    drive.DriveTarget.TrySet(refSet.SetReference);
                    // Local user space end
                    return;
                }

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

                    // Logix Support for others clicking the button and respecting local user
                    var localUser = logixRoot.AttachComponent<LocalUser>();
                    var cast = logixRoot.AttachComponent<CastClass<User,IWorldElement>>();
                    var toRefId = logixRoot.AttachComponent<ReferenceID>();
                    var drive = logixRoot.AttachComponent<DriverNode<RefID>>();

                    cast.In.TrySet(localUser);
                    toRefId.Element.TrySet(cast);
                    drive.Source.TrySet(toRefId);

                    drive.DriveTarget.TrySet(refSet.SetReference);
                    //


                    var list = ui.Button("L");
                    var multiplayerSupport = list.Slot.AttachComponent<ReferenceField<User>>();


                    // Cursed multiplayer support
                    //list.SetupValueSet(multiplayerSupport.Value, true, null, null);
                    var multSet = list.Slot.AttachComponent<ButtonReferenceSet<User>>();
                    multSet.SetReference.DriveFrom(refSet.SetReference);
                    multSet.TargetReference.TrySet(multiplayerSupport.Reference);
                    // This is awful 
                    multiplayerSupport.Reference.OnValueChange += (field) => {
                        if (multiplayerSupport.Reference.Target == null) return; // if false return
                        // Sometimes this triggers from a non locking thread
                        list.Slot.RunSynchronously(() =>
                        {
                            var slot = GenerateUserSelector(target);

                            float3 offset = list.Slot.Forward * -0.05f * multiplayerSupport.Reference.Target.Root.GlobalScale;
                            float3 point = list.Slot.LocalPointToGlobal(list.RectTransform.ComputeGlobalComputeRect().Center);
                            slot.GlobalPosition = point + offset;

                            slot.GlobalRotation = list.Slot.GlobalRotation;

                            slot.LocalScale *= multiplayerSupport.Reference.Target.Root.GlobalScale;

                            multiplayerSupport.Reference.Target = null;
                            //list.Slot.RunInUpdates(1, () => field.Value = false); // Cant do it immediately
                        });

                    };
                    return;
                }
            }

            static Slot GenerateUserSelector(ISyncRef targetRef)
            {
                Slot root = targetRef.World.LocalUserSpace.AddSlot("User Selector", false);
                root.AttachComponent<Grabbable>().Scalable.Value = true;
                UIBuilder uibuilder = new UIBuilder(root, 600f, 1000f, 0.0005f);

                uibuilder.Panel(new color(1f, 0.8f), true);
                uibuilder.Style.ForceExpandHeight = false;
                uibuilder.ScrollArea();
                uibuilder.VerticalLayout(8f, 8f);
                uibuilder.FitContent(SizeFit.Disabled, SizeFit.MinSize);

                uibuilder.Style.MinHeight = 32f;
                color usersColor = new color(1f, 0.8f, 1f);
                root.World.AllUsers.Do((user) => // Generate user list
                {
                    var text = string.Format("{0}{1}{2} ({3}){4}", // ;-;
                        user.HeadDevice == HeadOutputDevice.Headless ? "<i><color=#000a>" : "",
                        user.IsHost ? "<b>" : "",
                        user.UserName,
                        user.UserID,
                        user.IsHost ? " \u265B" : ""
                        );

                    var button = uibuilder.Button(text, usersColor);
                    button.RequireLockInToPress.Value = true; // Allow dragging without pressing

                    // Set Ref Value
                    var btnRefSet = button.Slot.AttachComponent<ButtonReferenceSet<User>>();
                    btnRefSet.TargetReference.TrySet(targetRef);
                    btnRefSet.SetReference.TrySet(user);

                    var uyh = button.Slot.AttachComponent<ButtonActionTrigger>();
                    uyh.OnPressed.TrySet(new Action(root.Destroy)); // Wow that works apparently 
                    });

                return root;
            }
        }
    }
}