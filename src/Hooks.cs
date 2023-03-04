using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NeedleConfig
{
    internal static class Hooks
    {
        public static void ApplyHooks()
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        }

        private static bool isInit = false;

        private static void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);

            if (isInit) return;
            isInit = true;

            MachineConnector.SetRegisteredOI(Plugin.MOD_ID, Options.instance);

            try
            {
                IL.Player.GrabUpdate += Player_GrabUpdate;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError(ex);
            }
        }

        private static Dictionary<Player, bool> wasInputProcessed = new Dictionary<Player, bool>();

        private static void Player_GrabUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // First 10% Extraction Speed
            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(16),
                x => x.MatchLdloc(16),
                x => x.MatchLdfld<PlayerGraphics.TailSpeckles>("spearProg"),
                x => x.MatchLdcR4(0.11f));

            c.Remove();

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<Player, float>>((player) => (Options.needleExtractSpeedFirst.Value / 100.0f) * 0.1f);


            // Rest of Extraction Speed
            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(16),
                x => x.MatchLdloc(16),
                x => x.MatchLdfld<PlayerGraphics.TailSpeckles>("spearProg"),
                x => x.MatchLdcR4(1));

            c.Remove();

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<Player, float>>((player) => (Options.needleExtractSpeedLast.Value / 100.0f) * 0.05f);


            //// Override MSC Needle Check
            //c.GotoNext(MoveType.Before,
            //    x => x.MatchLdarg(0),
            //    x => x.MatchCallOrCallvirt<PhysicalObject>("get_graphicsModule"),
            //    x => x.MatchIsinst("PlayerGraphics"),
            //    x => x.MatchCallvirt<PlayerGraphics>("get_useJollyColor"));

            //c.RemoveRange(4);

            //c.Emit(OpCodes.Ldarg_0);
            //c.EmitDelegate<Func<Player, bool>>((player) =>
            //{
            //    return ((PlayerGraphics)player.graphicsModule).useJollyColor || Options.rainbowNeedles.Value;
            //});



            //// Override Colour
            //c.GotoNext(MoveType.Before,
            //    x => x.MatchLdcI4(2),
            //    x => x.MatchCallOrCallvirt<PlayerGraphics>("JollyColor"));

            //c.Index += 1;
            //c.Remove();

            //c.Emit(OpCodes.Ldarg_0);
            //c.EmitDelegate<Func<Player, Color>>((player) =>
            //{
            //    return Random.ColorHSV();
            //});



            //// Failure of Epic Proportions
            //c.GotoNext(MoveType.After,
            //    x => x.MatchLdarg(0),
            //    x => x.MatchLdcI4(0),
            //    x => x.MatchStfld<Player>("wantToThrow"));

            //c.Emit(OpCodes.Ldloc_S, (byte)19);
            //c.Emit<AbstractPhysicalObject>(OpCodes.Ldfld, "realizedObject");
            //c.Emit(OpCodes.Isinst, typeof(Spear));

            //c.Emit(OpCodes.Ldarg_0);
            //c.Emit<Player>(OpCodes.Call, "get_playerState");
            //c.Emit<PlayerState>(OpCodes.Ldfld, "playerNumber");

            //c.Emit(OpCodes.Ldc_I4_2);

            //c.Emit(OpCodes.Ldarg_0);
            //c.EmitDelegate<Func<Player, Color>>((player) => UnityEngine.Random.ColorHSV());

            //c.Emit<Color>(OpCodes.Newobj, "ctor");

            //c.Emit<Spear>(OpCodes.Stfld, "jollyCustomColor");
        }
    }
}
