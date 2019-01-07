using UnityEngine;
using Verse;

namespace Outfitted
{
    public static class ResourceBank
    {
        [StaticConstructorOnStartup]
        public static class Textures
        {
            public static readonly Texture2D AddButton = ContentFinder<Texture2D>.Get("add");

            public static readonly Texture2D BgColor = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.2f, 1));

            public static readonly Texture2D DeleteButton = ContentFinder<Texture2D>.Get("delete");

            public static readonly Texture2D Drop = ContentFinder<Texture2D>.Get("UI/Buttons/Drop");

            public static readonly Texture2D FloatRangeSliderTex = ContentFinder<Texture2D>.Get("UI/Widgets/RangeSlider");

            public static readonly Texture2D Info = ContentFinder<Texture2D>.Get("UI/Buttons/InfoButton");

            public static readonly Texture2D ResetButton = ContentFinder<Texture2D>.Get("reset");

            public static readonly Texture2D White = SolidColorMaterials.NewSolidColorTexture(Color.white);

            public static readonly Texture2D ShirtBasic = ContentFinder<Texture2D>.Get("Things/Pawn/Humanlike/Apparel/ShirtBasic/ShirtBasic");
        }

        public static class Strings
        {
            static string TL(string s) => (s).Translate();
            static string TL(string s, string arg) => (s).Translate(arg);

            public static readonly string PreferedTemperature = TL("PreferedTemperature");
            public static readonly string TemperatureRangeReset = TL("TemperatureRangeReset");
            public static readonly string PreferedStats = TL("PreferedStats");
            public static readonly string StatPriorityAdd = TL("StatPriorityAdd");
            public static readonly string None = TL("None");

            public static readonly string OutfitShow = TL("OutfitShow");
            public static readonly string PenaltyWornByCorpse = TL("PenaltyWornByCorpse");
            public static readonly string PenaltyWornByCorpseTooltip = TL("PenaltyWornByCorpseTooltip");

            public static string StatPriorityDelete(string labelCap) => TL("StatPriorityDelete", labelCap);
            public static string StatPriorityReset(string labelCap) => TL("StatPriorityReset", labelCap);
        }
    }
}