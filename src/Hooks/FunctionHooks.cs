using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RainbowNeedles;

public static partial class Hooks
{
    private const float THREAD_COLOR_MULTIPLIER = 0.85f;
    private const float FADED_COLOR_MULTIPLIER = 0.6f;
    private const float FRAME_COLOR_ADDITION = 0.005f;

    private static float GetFrameColorAddition() => FRAME_COLOR_ADDITION * (ModOptions.cycleSpeed.Value / 100.0f);

    private static ConditionalWeakTable<PlayerGraphics.TailSpeckles, Dictionary<int, Color>> PlayerColoredSpeckles { get; } = new();
    private static ConditionalWeakTable<Player, StrongBox<Color>> NextSpearColor { get; } = new();
    private static ConditionalWeakTable<Spear, StrongBox<Color>> ColoredSpears { get; } = new();


    private static void ApplyFunctionHooks()
    {
        On.Spear.DrawSprites += Spear_DrawSprites;

        On.Spear.Umbilical.ApplyPalette += Umbilical_ApplyPalette;
        On.PlayerGraphics.TailSpeckles.DrawSprites += TailSpeckles_DrawSprites;
        On.PlayerGraphics.TailSpeckles.ctor += TailSpeckles_ctor;

        try
        {
            IL.Spear.Umbilical.DrawSprites += Umbilical_DrawSpritesIL;
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError("Umbilical.DrawSprites IL Exception:\n" + e);
        }
    }


    private static void TailSpeckles_ctor(On.PlayerGraphics.TailSpeckles.orig_ctor orig, PlayerGraphics.TailSpeckles self, PlayerGraphics pGraphics, int startSprite)
    {
        orig(self, pGraphics, startSprite);

        self.rows = ModOptions.tailRows.Value;
        self.lines = ModOptions.tailLines.Value;
        self.numberOfSprites = self.rows * self.lines + 1;

        self.newSpearSlot();
    }

    private static void TailSpeckles_DrawSprites(On.PlayerGraphics.TailSpeckles.orig_DrawSprites orig, PlayerGraphics.TailSpeckles self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);

        if (self.pGraphics.player.SlugCatClass != MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear) return;

        if (!PlayerColoredSpeckles.TryGetValue(self, out var coloredSpeckles))
        {
            coloredSpeckles = new();

            for (int i = 0; i < self.rows; i++)
            {
                for (int j = 0; j < self.lines; j++)
                {
                    int index = self.startSprite + i * self.lines + j;
                    coloredSpeckles[index] = Custom.HSL2RGB(((1.0f / (self.rows + self.lines)) * (i + j)), 1.0f, 0.6f);
                }
            }

            PlayerColoredSpeckles.Add(self, coloredSpeckles);
        }

        for (int i = 0; i < self.rows; i++)
        {
            for (int j = 0; j < self.lines; j++)
            {
                int index = self.startSprite + i * self.lines + j;

                if (ModOptions.rainbowSpeckles.Value)
                {
                    sLeaser.sprites[index].color = coloredSpeckles[index];

                    if (ModOptions.specklesCycle.Value)
                    {
                        var currentColor = coloredSpeckles[index];
                        var HSL = Custom.RGB2HSL(currentColor);
                        var hue = HSL.x + GetFrameColorAddition();

                        if (hue > 1.0f)
                        {
                            hue = 0.0f;
                        }

                        coloredSpeckles[index] = Custom.HSL2RGB(hue, HSL.y, HSL.z);
                    }
                }


                if (i == self.spearRow && j == self.spearLine && ModOptions.rainbowNeedles.Value)
                {

                    if (!NextSpearColor.TryGetValue(self.pGraphics.player, out var targetColor))
                    {
                        targetColor = new();
                        NextSpearColor.Add(self.pGraphics.player, targetColor);
                    }

                    targetColor.Value = coloredSpeckles[index];
                
                    sLeaser.sprites[self.startSprite + self.lines * self.rows].color = coloredSpeckles[index];
                }
            }
        }
    }

    private static void Spear_DrawSprites(On.Spear.orig_DrawSprites orig, Spear self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        if (!ColoredSpears.TryGetValue(self, out var spearColor))
        {
            if (self.IsNeedle && ModOptions.rainbowNeedles.Value && !self.slatedForDeletetion)
            {
                if (self.grabbedBy.Count > 0 && self.grabbedBy[0].grabber is Player player)
                {
                    if (NextSpearColor.TryGetValue(player, out var targetColor))
                    {
                        spearColor = new StrongBox<Color>(new Color(targetColor.Value.r, targetColor.Value.g, targetColor.Value.b));
                        ColoredSpears.Add(self, spearColor);
                    }
                }
                else
                {
                    // This happens when the player's hands are full, default to a random color
                    spearColor = new StrongBox<Color>(Random.ColorHSV(0.0f, 1.0f, 1.0f, 1.0f, 0.6f, 0.6f));
                    ColoredSpears.Add(self, spearColor);
                }
            }
        }

        if (spearColor != null && ModOptions.needlesCycle.Value && self.spearmasterNeedle_hasConnection)
        {
            var HSL = Custom.RGB2HSL(spearColor.Value);
            var hue = HSL.x + GetFrameColorAddition();

            if (hue > 1.0f)
            {
                hue = 0.0f;
            }

            spearColor.Value = Custom.HSL2RGB(hue, HSL.y, HSL.z);
        }

        orig(self, sLeaser, rCam, timeStacker, camPos);

        // Could IL Hook, was lazy
        float fade = (float)self.spearmasterNeedle_fadecounter / self.spearmasterNeedle_fadecounter_max;
    
        if (self.spearmasterNeedle_hasConnection || !ModOptions.needlesFade.Value)
        {
            fade = 1.0f;
        }

        if (fade < 0.01f)
        {
            fade = 0.01f;
        }

        if (spearColor != null)
        {
            var fadedColor = spearColor.Value * FADED_COLOR_MULTIPLIER;
            
            sLeaser.sprites[0].color = Color.Lerp(spearColor.Value, fadedColor, 1.0f - fade);
        }
    }


    private static void Umbilical_DrawSpritesIL(ILContext il)
    {
        var c = new ILCursor(il);

        c.GotoNext(MoveType.Before,
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<Spear.Umbilical>("fogColor"),
            x => x.MatchLdcR4(1.0f));

        c.Index += 2;
        c.RemoveRange(3);

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<Spear.Umbilical, float>>((umbilical) =>
        {
            ColoredSpears.TryGetValue(umbilical.maggot, out var spearColor);

            return spearColor == null || !ModOptions.rainbowThread.Value ? 1.0f : spearColor.Value.r * THREAD_COLOR_MULTIPLIER;
        });

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<Spear.Umbilical, float>>((umbilical) =>
        {
            ColoredSpears.TryGetValue(umbilical.maggot, out var spearColor);

            return spearColor == null || !ModOptions.rainbowThread.Value ? 0.0f : spearColor.Value.g * THREAD_COLOR_MULTIPLIER;
        });

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<Spear.Umbilical, float>>((umbilical) =>
        {
            ColoredSpears.TryGetValue(umbilical.maggot, out var spearColor);
            
            return spearColor == null || !ModOptions.rainbowThread.Value ? 0.0f : spearColor.Value.b * THREAD_COLOR_MULTIPLIER;
        });
    }

    private static void Umbilical_ApplyPalette(On.Spear.Umbilical.orig_ApplyPalette orig, Spear.Umbilical self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        orig(self, sLeaser, rCam, palette);

        if (!ModOptions.rainbowThread.Value) return;

        ColoredSpears.TryGetValue(self.maggot, out var spearColor);

        if (spearColor == null) return;

        self.threadCol = Color.Lerp(spearColor.Value * THREAD_COLOR_MULTIPLIER, palette.fogColor, 0.2f);
    }
}
