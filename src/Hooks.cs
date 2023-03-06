using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Media;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

namespace NeedleConfig
{
    internal static class Hooks
    {
        public static void ApplyHooks()
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
            On.Spear.DrawSprites += Spear_DrawSprites;
            
            On.Spear.Umbilical.ApplyPalette += Umbilical_ApplyPalette;
            On.PlayerGraphics.TailSpeckles.DrawSprites += TailSpeckles_DrawSprites;
            On.PlayerGraphics.TailSpeckles.ctor += TailSpeckles_ctor;
        }

        private static void TailSpeckles_ctor(On.PlayerGraphics.TailSpeckles.orig_ctor orig, PlayerGraphics.TailSpeckles self, PlayerGraphics pGraphics, int startSprite)
        {
            orig(self, pGraphics, startSprite);

            self.rows = Options.tailRows.Value;
            self.lines = Options.tailLines.Value;
            self.numberOfSprites = self.rows * self.lines + 1;
            self.newSpearSlot();
        }

        private static ConditionalWeakTable<PlayerGraphics.TailSpeckles, Dictionary<int, Color>> playerColoredSpeckles = new ConditionalWeakTable<PlayerGraphics.TailSpeckles, Dictionary<int, Color>>();
        private static ConditionalWeakTable<Player, StrongBox<Color>> nextSpearColor = new ConditionalWeakTable<Player, StrongBox<Color>>();


        private static void TailSpeckles_DrawSprites(On.PlayerGraphics.TailSpeckles.orig_DrawSprites orig, PlayerGraphics.TailSpeckles self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);

            if (self.pGraphics.player.SlugCatClass != MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear) return;

            Dictionary<int, Color> coloredSpeckles;
            playerColoredSpeckles.TryGetValue(self, out coloredSpeckles);

            if (coloredSpeckles == null)
            {
                coloredSpeckles = new Dictionary<int, Color>();

                for (int i = 0; i < self.rows; i++)
                {
                    for (int j = 0; j < self.lines; j++)
                    {
                        int index = self.startSprite + i * self.lines + j;
                        coloredSpeckles[index] = Custom.HSL2RGB((1.0f / (self.rows + self.lines)) * (i + j), 1.0f, 0.6f);
                    }
                }

                playerColoredSpeckles.Add(self, coloredSpeckles);
            }

            for (int i = 0; i < self.rows; i++)
            {
                for (int j = 0; j < self.lines; j++)
                {
                    int index = self.startSprite + i * self.lines + j;
                    
                    if (Options.rainbowSpeckles.Value) sLeaser.sprites[index].color = coloredSpeckles[index];

                    if (i == self.spearRow && j == self.spearLine && Options.rainbowNeedles.Value)
                    {
                        StrongBox<Color> targetColor;
                        nextSpearColor.TryGetValue(self.pGraphics.player, out targetColor);

                        if (targetColor != null) nextSpearColor.Remove(self.pGraphics.player);

                        nextSpearColor.Add(self.pGraphics.player, new StrongBox<Color>(coloredSpeckles[index]));
                        sLeaser.sprites[self.startSprite + self.lines * self.rows].color = coloredSpeckles[index];
                    }
                }
            }
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
                IL.Spear.Umbilical.DrawSprites += Umbilical_DrawSpritesIL;
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogError(ex);
            }
        }

        private const float THREAD_COLOR_MULTIPLIER = 0.85f;
        private const float FADED_COLOR_MULTIPLIER = 0.6f;

        private static ConditionalWeakTable<Spear, StrongBox<Color>> coloredSpears = new ConditionalWeakTable<Spear, StrongBox<Color>>();

        private static void Spear_DrawSprites(On.Spear.orig_DrawSprites orig, Spear self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            StrongBox<Color> spearColor;
            coloredSpears.TryGetValue(self, out spearColor);

            if (spearColor == null && self.IsNeedle && Options.rainbowNeedles.Value && !self.slatedForDeletetion)
            {
                if (self.grabbedBy.Count > 0 && self.grabbedBy[0].grabber is Player player)
                {
                    StrongBox<Color> targetColor;
                    nextSpearColor.TryGetValue(player, out targetColor);

                    if (targetColor != null) coloredSpears.Add(self, targetColor);
                }
            }

            orig(self, sLeaser, rCam, timeStacker, camPos);

            // Could IL Hook, was lazy
            float fade = (float)self.spearmasterNeedle_fadecounter / self.spearmasterNeedle_fadecounter_max;
            if (self.spearmasterNeedle_hasConnection || !Options.needlesFade.Value) fade = 1.0f;
            if (fade < 0.01f) fade = 0.01f;

            if (spearColor != null)
            {
                Color fadedColor = spearColor.Value * FADED_COLOR_MULTIPLIER;
                sLeaser.sprites[0].color = Color.Lerp(spearColor.Value, fadedColor, 1.0f - fade);
            }
        }

        private static void Umbilical_DrawSpritesIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Spear.Umbilical>("fogColor"),
                x => x.MatchLdcR4(1.0f));

            c.Index += 2;
            c.RemoveRange(3);

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<Spear.Umbilical, float>>((umbilical) =>
            {
                StrongBox<Color> spearColor;
                coloredSpears.TryGetValue(umbilical.maggot, out spearColor);
                return spearColor == null || !Options.rainbowThread.Value ? 1.0f : spearColor.Value.r * THREAD_COLOR_MULTIPLIER;
            });

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<Spear.Umbilical, float>>((umbilical) =>
            {
                StrongBox<Color> spearColor;
                coloredSpears.TryGetValue(umbilical.maggot, out spearColor);
                return spearColor == null || !Options.rainbowThread.Value ? 0.0f : spearColor.Value.g * THREAD_COLOR_MULTIPLIER;
            });

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<Spear.Umbilical, float>>((umbilical) =>
            {
                StrongBox<Color> spearColor;
                coloredSpears.TryGetValue(umbilical.maggot, out spearColor);
                return spearColor == null || !Options.rainbowThread.Value ? 0.0f : spearColor.Value.b * THREAD_COLOR_MULTIPLIER;
            });
        }

        private static void Umbilical_ApplyPalette(On.Spear.Umbilical.orig_ApplyPalette orig, Spear.Umbilical self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);

            if (!Options.rainbowThread.Value) return;

            StrongBox<Color> spearColor;
            coloredSpears.TryGetValue(self.maggot, out spearColor);

            if (spearColor == null) return;
            self.threadCol = Color.Lerp(spearColor.Value * THREAD_COLOR_MULTIPLIER, palette.fogColor, 0.2f);
        }
    }
}
