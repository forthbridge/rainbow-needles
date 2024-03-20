using Menu.Remix.MixedUI;

namespace RainbowNeedles;

public sealed class ModOptions : OptionsTemplate
{
    public static ModOptions Instance { get; } = new();

    public static void RegisterOI()
    {
        if (MachineConnector.GetRegisteredOI(Plugin.MOD_ID) != Instance)
        {
            MachineConnector.SetRegisteredOI(Plugin.MOD_ID, Instance);
        }
    }


    // Configurables

    public static Configurable<bool> rainbowNeedles = Instance.config.Bind("rainbowNeedles", true, new ConfigurableInfo(
        "Randomises the hue of extracted needles.",
        null, "", "Rainbow Needles?"));

    public static Configurable<bool> rainbowSpeckles = Instance.config.Bind("rainbowSpeckles", true, new ConfigurableInfo(
        "Randomizes the hue of tail speckles.",
        null, "", "Rainbow Speckles?"));

    public static Configurable<bool> rainbowThread = Instance.config.Bind("rainbowThread", true, new ConfigurableInfo(
        "Randomizes the hue of the thread connecting the spears to Spearmaster. Depends on Rainbow Needles.",
        null, "", "Rainbow Thread?"));

    public static Configurable<bool> needlesFade = Instance.config.Bind("needlesFade", true, new ConfigurableInfo(
        "Whether the color of needles fades after being thrown. Only affects rainbow needles.",
        null, "", "Needles Fade?"));

    public static Configurable<bool> specklesCycle = Instance.config.Bind("specklesCycle", true, new ConfigurableInfo(
        "Whether the color of speckles cycles between all hues",
        null, "", "Speckles Cycle?"));

    public static Configurable<bool> needlesCycle = Instance.config.Bind("needlesCycle", false, new ConfigurableInfo(
        "Whether the color of needles slowly cycles between all hues.",
        null, "", "Needles Cycle?"));

    public static Configurable<int> tailRows = Instance.config.Bind("tailRows", 5, new ConfigurableInfo(
        "Influences the amount of holes present on Spearmaster's tail vertically." +
        "\nHold and drag up or down to change.",
        new ConfigAcceptableRange<int>(0, 20), "", "Tail Rows"));

    public static Configurable<int> tailLines = Instance.config.Bind("tailLines", 3, new ConfigurableInfo(
        "Influences the amount of holes present on Spearmaster's tail horizontally." +
        "\nHold and drag up or down to change.",
        new ConfigAcceptableRange<int>(0, 20), "", "Tail Lines"));

    public static Configurable<int> cycleSpeed = Instance.config.Bind("cycleSpeed", 100, new ConfigurableInfo(
        "Modifier for how fast the rainbow cycles, if cycling is enabled.",
        new ConfigAcceptableRange<int>(1, 500), "", "Cycle Speed Modifier"));


    private const int NUMBER_OF_TABS = 1;

    public override void Initialize()
    {
        base.Initialize();
        Tabs = new OpTab[NUMBER_OF_TABS];
        int tabIndex = -1;

        AddTab(ref tabIndex, "General");

        AddCheckBox(rainbowNeedles, (string)rainbowNeedles.info.Tags[0]);
        AddCheckBox(rainbowSpeckles, (string)rainbowSpeckles.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[tabIndex]);

        AddCheckBox(rainbowThread, (string)rainbowThread.info.Tags[0]);
        AddCheckBox(needlesFade, (string)needlesFade.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[tabIndex]);

        AddCheckBox(needlesCycle, (string)needlesCycle.info.Tags[0]);
        AddCheckBox(specklesCycle, (string)specklesCycle.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[tabIndex]);

        AddNewLine();

        AddSlider(cycleSpeed, (string)cycleSpeed.info.Tags[0], "1%", "500%");
        DrawSliders(ref Tabs[tabIndex]);

        AddDragger(tailRows, (string)tailRows.info.Tags[0]);
        AddDragger(tailLines, (string)tailLines.info.Tags[0]);
        DrawDraggers(ref Tabs[tabIndex]);

        AddNewLine(5);
        DrawBox(ref Tabs[tabIndex]);
    }
}