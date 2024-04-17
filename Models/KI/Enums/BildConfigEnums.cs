using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Reflection;


namespace KaffeBot.Models.KI.Enums
{
    /// <summary>
    /// Auswahl zur Anfertigung von Bildern
    /// </summary>
    public abstract class BildConfigEnums
    {
        /// <summary>
        /// Auswahl für das Modell
        /// </summary>
        public enum Modelle
        {
            /// <summary>
            /// Unbekanntes Model
            /// </summary>
            [Description("wd-1-5-beta2-aesthetic-unclip-h-fp16.safetensors")]
            [AiAttribut("Standard Modell")]
            Wd15Beta2AestheticUnclipHFp16,

            /// <summary>
            /// Standart Furry Model mit NSFW Funktion
            /// </summary>
            [Description("Furry\\furrydiffusion.ckpt")]
            [AiAttribut("Standart Furry Model mit NSFW Funktion")]
            Furrydiffusion,

            /// <summary>
            /// Model sammlung zum Anfertigen von Furry´s early version
            /// </summary>
            [Description("Furry\\furryrock_V30.safetensors")]
            [AiAttribut("Model sammlung zum Anfertigen von Furry\u00b4s early version")]
            FurryrockV30,

            /// <summary>
            /// Aktuellste Version der Modelsammlung
            /// </summary>
            [Description("Furry\\furryrock_V70.safetensors")]
            [AiAttribut("Aktuellste Version der Modelsammlung")]
            FurryrockV70,

            /// <summary>
            /// Model zur Erstellung von Menschenähnlichen Furry´s
            /// </summary>
            [Description("Furry\\furryVixens_v20.safetensors")]
            [AiAttribut("Model zur Erstellung von Menschenähnlichen Furry\u00b4s")]
            FurryVixensV20,

            /// <summary>
            /// Das Gleiche Model wie Furry Vixen nur weiter trainiert mit verschiedenen Zusätzen
            /// </summary>
            [Description("Furry\\furryVixens_v20BakedVAE.safetensors")]
            [AiAttribut("Das Gleiche Model wie Furry Vixen nur weiter trainiert mit verschiedenen Zusätzen")]
            FurryVixensV20BakedVae,

            /// <summary>
            /// Model zur Erstellung von Cute Furry´s
            /// </summary>
            [Description("Furry\\neatnessFluffyFurMix_zephyr.safetensors")]
            [AiAttribut("Model zur Erstellung von Cute Furry\u00b4s")]
            NeatnessFluffyFurMixZephyr,

            /// <summary>
            /// Ein weiteres Furry merged Model
            /// </summary>
            [Description("Furry\\SD - NSFW - Furry - Furry Verse.safetensors")]
            [AiAttribut("Ein weiteres Furry merged Model")]
            SdNsfwFurryFurryVerse,

            /// <summary>
            /// Ein weiteres Furry merged Model
            /// </summary>
            [Description("Furry\\SD - NSFW - Furry - Manacyst.safetensors")]
            [AiAttribut("Ein weiteres Furry merged Model")]
            SdNsfwFurryManacyst,

            /// <summary>
            /// Ein weiteres Furry merged Model
            /// </summary>
            [Description("Furry\\yiffymix_v34.safetensors")]
            [AiAttribut("Ein weiteres Furry merged Model")]
            YiffymixV34,

            /// <summary>
            /// Model zur Erstellung von normalenm Hentai Figuren.
            /// </summary>
            [Description("Hentai\\hassakuHentaiModel_v13.safetensors")]
            [AiAttribut("Model zur Erstellung von normalenm Hentai Figuren.")]
            HassakuHentaiModelV13,

            /// <summary>
            /// Hentai Modell
            /// </summary>
            [Description("Hentai\\hentaidigitalart_v13Mature.safetensors")]
            [AiAttribut("Hentai Modell")]
            HentaidigitalartV13Mature,

            /// <summary>
            /// Hentai Model
            /// </summary>
            [Description("Hentai\\hentaiMixXLRoadTo_v50.safetensors")]
            [AiAttribut("Hentai Modell")]
            HentaiMixXlRoadToV50,

            /// <summary>
            /// Hentai Model
            /// </summary>
            [Description("Hentai\\matrixHentaiPlusXL_v10.safetensors")]
            [AiAttribut("Hentai Modell")]
            MatrixHentaiPlusXlV10,

            /// <summary>
            /// Artist Model Hentai sammlung
            /// </summary>
            [Description("Hentai\\meinamix_meinaV11.safetensors")]
            [AiAttribut("Artist Model Hentai sammlung")]
            MeinamixMeinaV11,

            /// <summary>
            /// Model zur Generierung von nicht NFSW Inhalten für RPG´s
            /// </summary>
            [Description("Normal\\aZovyaRPGArtistTools_v4.safetensors")]
            [AiAttribut("Model zur Generierung von nicht NFSW Inhalten für RPG\u00b4s")]
            AZovyaRpgArtistToolsV4,

            /// <summary>
            /// Model zur Erstellung von Realistischen Bildenr mit NSFW Funktion
            /// </summary>
            [Description("Normal\\dreamshaper_8 - NSFW.safetensors")]
            [AiAttribut("Model zur Erstellung von Realistischen Bildenr mit NSFW Funktion")]
            Dreamshaper8Nsfw,

            /// <summary>
            /// Normales standart Model
            /// </summary>
            [Description("Normal\\sd-v1-4.ckpt")]
            [AiAttribut("Normales standart Model")]
            SdV14
        }

        /// <summary>
        /// Lora auswahl
        /// </summary>
        public enum LoraStack
        {
            /// <summary>
            /// Anime Hellsing Char Alucard, Seras, Integra
            /// </summary>
            [Description("Anime Char\\alucardserasintegra.safetensors")]
            [AiAttribut("Anime Hellsing Char Alucard, Seras, Integra")]
            Alucardserasintegra,

            /// <summary>
            /// Anime Elfenlied
            /// </summary>
            [Description("Anime Char\\ElfenLied3RS.safetensors")]
            [AiAttribut("Anime Elfenlied")]
            ElfenLied3Rs,

            /// <summary>
            /// Anime FT Char Erza v1
            /// </summary>
            [Description("Anime Char\\erza_v1.safetensors")]
            [AiAttribut("Anime FT Char Erza v1")]
            ErzaV1,

            /// <summary>
            /// Anime FT Char Carla
            /// </summary>
            [Description("Anime Char\\fairytailcarla-lora-nochekaiser.safetensors")]
            [AiAttribut("Anime FT Char Carla")]
            FairytailcarlaLoraNochekaiser,

            /// <summary>
            /// Anime FT Char Lucy
            /// </summary>
            [Description("Anime Char\\fairytail_lucyheartfilia-11.safetensors")]
            [AiAttribut("Anime FT Char Lucy")]
            FairytailLucyheartfilia11,

            /// <summary>
            /// Anime FT Allgemein Styling
            /// </summary>
            [Description("Anime Char\\fairy_tail_style.safetensors")]
            [AiAttribut("Anime FT Allgemein Styling")]
            FairyTailStyle,

            /// <summary>
            /// Fury trained on Dally Works
            /// </summary>
            [Description("Anime Char\\Furry - Dalle.safetensors")]
            [AiAttribut("Fury trained on Dally Works")]
            FurryDalle,

            /// <summary>
            /// Furry Female trained
            /// </summary>
            [Description("Anime Char\\Furry - Female.safetensors")]
            [AiAttribut("Furry Female trained")]
            FurryFemale,

            /// <summary>
            /// Small Furry´s
            /// </summary>
            [Description("Anime Char\\Furry - Tora.safetensors")]
            [AiAttribut("Small Furry\u00b4s")]
            FurryTora,

            /// <summary>
            /// Ganondorf from Zelda Style
            /// </summary>
            [Description("Anime Char\\GanondorfZeldaSeries0.safetensors")]
            [AiAttribut("Ganondorf from Zelda Style")]
            GanondorfZeldaSeries0,

            /// <summary>
            /// Anime Hellsing Char Seras
            /// </summary>
            [Description("Anime Char\\hellsing_serasvictoria-10.safetensors")]
            [AiAttribut("Anime Hellsing Char Seras")]
            HellsingSerasvictoria10,

            /// <summary>
            /// Anime Sailermoon Char Jupiter
            /// </summary>
            [Description("Anime Char\\Jupiter10_16.safetensors")]
            [AiAttribut("Anime Sailermoon Char Jupiter")]
            Jupiter1016,

            /// <summary>
            /// Loli in Sakura Oriko Style
            /// </summary>
            [Description("Anime Char\\Loli - Sakura Oriko Style.safetensors")]
            [AiAttribut("Loli in Sakura Oriko Style")]
            LoliSakuraOrikoStyle,

            /// <summary>
            ///  Anime Helluva Boss Char Loona
            /// </summary>
            [Description("Anime Char\\loona-lora-v3-latest.safetensors")]
            [AiAttribut("Anime Helluva Boss Char Loona")]
            LoonaLoraV3Latest,

            /// <summary>
            /// Anime ELfen Lied Char Lucy
            /// </summary>
            [Description("Anime Char\\Lucy_ElfenLied.safetensors")]
            [AiAttribut("Anime ELfen Lied Char Lucy")]
            LucyElfenLied,

            /// <summary>
            /// Anime Pokemon Style
            /// </summary>
            [Description("Anime Char\\pokemon_v3_offset.safetensors")]
            [AiAttribut("Anime ELfen Lied Char Lucy")]
            PokemonV3Offset,

            /// <summary>
            /// Anime Pokemon for evo styles
            /// </summary>
            [Description("Anime Char\\S1-Furry-Eeveeution.safetensors")]
            [AiAttribut("Anime Pokemon for evo styles")]
            S1FurryEeveeution,

            /// <summary>
            /// Anime Sailermoon Char Mars
            /// </summary>
            [Description("Anime Char\\Sailor_Mars_hino_rei-01.safetensors")]
            [AiAttribut("Anime Sailermoon Char Mars")]
            SailorMarsHinoRei01,

            /// <summary>
            /// Anime Outline Styles
            /// </summary>
            [Description("Anime Char\\Style - Anime-Outlines.safetensors")]
            [AiAttribut("Anime Outline Styles")]
            StyleAnimeOutlines,

            /// <summary>
            /// Komi Shouko Style
            /// </summary>
            [Description("Anime Char\\Style - Komi Shouko.safetensors")]
            [AiAttribut("Komi Shouko Style")]
            StyleKomiShouko,

            /// <summary>
            /// Major - Hellsing
            /// </summary>
            [Description("Anime Char\\V1HUMajorLoRA.safetensors")]
            [AiAttribut("Major - Hellsing")]
            V1HuMajorLoRa,

            /// <summary>
            /// Anime Eminece of Shadow Char Delta
            /// </summary>
            [Description("Anime Char\\WoofWoofDeltaV1-000011.safetensors")]
            [AiAttribut("Anime Eminece of Shadow Char Delta")]
            WoofWoofDeltaV1000011,

            /// <summary>
            /// Zelda style Princess
            /// </summary>
            [Description("Anime Char\\zelda-albw-fef.safetensors")]
            [AiAttribut("Zelda style Princess")]
            ZeldaAlbwFef,

            /// <summary>
            /// Zelda Botw Style
            /// </summary>
            [Description("Anime Char\\zelda_botw_v1.safetensors")]
            [AiAttribut("Zelda Botw Style")]
            ZeldaBotwV1,

            /// <summary>
            /// Adding more details
            /// </summary>
            [Description("Body Helper\\add_detail.safetensors")]
            [AiAttribut("Adding more details")]
            AddDetail,

            /// <summary>
            /// Styling for better Breast
            /// </summary>
            [Description("Body Helper\\Breasts_Helper_Trail_v2.safetensors")]
            [AiAttribut("Styling for better Breast")]
            BreastsHelperTrailV2,

            /// <summary>
            /// Squeezing breast together
            /// </summary>
            [Description("Body Helper\\breasts_squeezed_together_v0.2.safetensors")]
            [AiAttribut("Squeezing breast together")]
            BreastsSqueezedTogetherV02,

            /// <summary>
            /// Klassiche Button fürs Hemd ;)
            /// </summary>
            [Description("Body Helper\\Button.safetensors")]
            [AiAttribut("Klassiche Button fürs Hemd ;)")]
            Button,

            /// <summary>
            /// CG Style
            /// </summary>
            [Description("Body Helper\\CGgamebuttonbsw.safetensors")]
            [AiAttribut("CG Style")]
            CGgamebuttonbsw,

            /// <summary>
            /// Icon Style
            /// </summary>
            [Description("Body Helper\\CGgamebuttonicon csw.safetensors")]
            [AiAttribut("Icon Style")]
            CGgamebuttoniconCsw,

            /// <summary>
            /// Furry Horse Helper
            /// </summary>
            [Description("Body Helper\\FurryHorse_v1.1.safetensors")]
            [AiAttribut("Furry Horse Helper")]
            FurryHorseV11,

            /// <summary>
            /// Big Bigger Hyper Boobs
            /// </summary>
            [Description("Body Helper\\hyperbreasts_v6.safetensors")]
            [AiAttribut("Big Bigger Hyper Boobs")]
            hyperbreastsv6,

            /// <summary>
            /// Standart Hentai stomache pen
            /// </summary>
            [Description("Body Helper\\hyper_penetration_stomach_bulge_v8all-000004.safetensors")]
            [AiAttribut("Standart Hentai stomache pen")]
            HyperPenetrationStomachBulgeV8All000004,

            /// <summary>
            /// Weitere version
            /// </summary>
            [Description("Body Helper\\hyper_penetration_stomach_bulge_v8nice-000006.safetensors")]
            [AiAttribut("Weitere version")]
            hyperpenetrationstomachbulgev8nice000006,

            /// <summary>
            /// Helper zur erstellen einer besseren Vagina
            /// </summary>
            [Description("Body Helper\\InniePussy1 v4.safetensors")]
            [AiAttribut("Helper zur erstellen einer besseren Vagina")]
            InniePussy1V4,

            /// <summary>
            /// Furry Female Helper
            /// </summary>
            [Description("Body Helper\\lora_Furry_female.safetensors")]
            [AiAttribut("Furry Female Helper")]
            LoraFurryFemale,

            /// <summary>
            /// Mastrubations Helfer für dein Bild
            /// </summary>
            [Description("Body Helper\\masturbation-v1.safetensors")]
            [AiAttribut("Mastrubations Helfer für dein Bild")]
            masturbationv1,

            /// <summary>
            /// Helfer zum erstellen von Planeten
            /// </summary>
            [Description("Body Helper\\planets.safetensors")]
            [AiAttribut("Helfer zum erstellen von Planeten")]
            planets,

            /// <summary>
            /// Helfer für Natürlich wirkende Brüste
            /// </summary>
            [Description("Body Helper\\qqq-natural_breasts-v2-000007.safetensors")]
            [AiAttribut("Helfer für Natürlich wirkende Brüste")]
            qqqnaturalbreastsv2000007,

            /// <summary>
            /// Helfer zum erstellen ängstlicher gesicht ausdrücker
            /// </summary>
            [Description("Body Helper\\scared-expression-v1.8-000005.safetensors")]
            [AiAttribut("Helfer zum erstellen ängstlicher gesicht ausdrücker")]
            ScaredExpressionV18000005,

            /// <summary>
            /// Mehr Body Details Helfer
            /// </summary>
            [Description("Body Helper\\Style - More Details.safetensors")]
            [AiAttribut("Mehr Body Details Helfer")]
            StyleMoreDetails,

            /// <summary>
            /// Helfer für zerrissene Kleidung und co
            /// </summary>
            [Description("Body Helper\\torn.safetensors")]
            [AiAttribut("Helfer für zerrissene Kleidung und co")]
            torn,

            /// <summary>
            /// SD Vagina verbesserung
            /// </summary>
            [Description("Body Helper\\XXX - InniePussy SD.safetensors")]
            [AiAttribut("SD Vagina verbesserung")]
            XxxInniePussySd,

            /// <summary>
            /// Hand im Schlüppi ;)
            /// </summary>
            [Description("Pose\\000 - Hand in Panties.safetensors")]
            [AiAttribut("Hand im Schlüppi ;)")]
            HandinPanties,

            /// <summary>
            /// Helfer zum Panty fallen lassen
            /// </summary>
            [Description("Pose\\000 - pantypull-drop-r1.safetensors")]
            [AiAttribut("Helfer zum Panty fallen lassen")]
            pantypulldropr1,

            /// <summary>
            /// Helfer zum ausziehen der Panty
            /// </summary>
            [Description("Pose\\000 - undressing_panties_v0.1.safetensors")]
            [AiAttribut("Helfer zum ausziehen der Panty")]
            undressingpantiesv01,

            /// <summary>
            /// Pose  für drück und saug darstellung
            /// </summary>
            [Description("Pose\\concept-hug_and_suck-v1.safetensors")]
            [AiAttribut("Pose für drück und saug darstellung")]
            ConceptHugAndSuckV1,

            /// <summary>
            /// Deeptrhoat helper
            /// </summary>
            [Description("Pose\\concept-staged-deepthroat-v3.safetensors")]
            [AiAttribut("Deeptrhoat helper")]
            ConceptStagedDeepthroatV3,

            /// <summary>
            /// Middel Vaginal Pen Helfer
            /// </summary>
            [Description("Pose\\concept-tip-mid-base-vaginal-v1.safetensors")]
            [AiAttribut("Middel Vaginal Pen Helfer")]
            ConceptTipMidBaseVaginalV1,

            /// <summary>
            /// Cum Bedingtes aufblähen
            /// </summary>
            [Description("Pose\\Cumflation.safetensors")]
            [AiAttribut("Cum Bedingtes aufblähen")]
            Cumflation,

            /// <summary>
            /// weiter version von Überdimensionalen Brüsten
            /// </summary>
            [Description("Pose\\hyperbreasts_v6.safetensors")]
            [AiAttribut("weiter version von Überdimensionalen Brüsten")]
            HyperbreastsV6,

            /// <summary>
            /// Hyper version der Bauchg Pen andeutung
            /// </summary>
            [Description("Pose\\hyper_penetration_stomach_bulge_v8nice-000006.safetensors")]
            [AiAttribut("Hyper version der Bauchg Pen andeutung")]
            HyperPenetrationStomachBulgeV8Nice000006,

            /// <summary>
            /// Weiteres mastrubation Helferlein
            /// </summary>
            [Description("Pose\\masturbation-v1.safetensors")]
            [AiAttribut("Weiteres mastrubation Helferlein")]
            MasturbationV1,

            /// <summary>
            /// Erstellen von mehreren Hintern gleichzeitig Helfer
            /// </summary>
            [Description("Pose\\MultipleAsses_v1.safetensors")]
            [AiAttribut("Erstellen von mehreren Hintern gleichzeitig Helfer")]
            MultipleAssesV1,

            /// <summary>
            /// GHirl Group gen Helper
            /// </summary>
            [Description("Pose\\MultipleGirlsGroup.safetensors")]
            [AiAttribut("GHirl Group gen Helper")]
            MultipleGirlsGroup,

            /// <summary>
            /// Kopfkissen drücken Helfer
            /// </summary>
            [Description("Pose\\pillow_hug-10.safetensors")]
            [AiAttribut("Kopfkissen drücken Helfer")]
            pillowhug10,

            /// <summary>
            /// Schwangeren Harem Helfer
            /// </summary>
            [Description("Pose\\PregnantHarem.safetensors")]
            [AiAttribut("Schwangeren Harem Helfer")]
            PregnantHarem,

            /// <summary>
            /// Bukkake Helfer
            /// </summary>
            [Description("Pose\\qqq-bukkake-v2-000007.safetensors")]
            [AiAttribut("Bukkake Helfer")]
            QqqBukkakeV2000007,

            /// <summary>
            /// V2 Helfer für Natürliche Brüste
            /// </summary>
            [Description("Pose\\qqq-natural_breasts-v2-000007.safetensors")]
            [AiAttribut("V2 Helfer für Natürliche Brüste")]
            QqqNaturalBreastsV2000007,

            /// <summary>
            /// POV ASS Helper
            /// </summary>
            [Description("Pose\\qqq-pov_ass-v2.safetensors")]
            [AiAttribut("POV ASS Helper")]
            QqqPovAssV2,

            /// <summary>
            /// POV reverse fellatio
            /// </summary>
            [Description("Pose\\reverse fellatio lion2-000012.safetensors")]
            [AiAttribut("POV reverse fellatio")]
            ReverseFellatioLion2000012,

            /// <summary>
            /// Helfer für selfgrab on Breast
            /// </summary>
            [Description("Pose\\SelfBreastGrab.safetensors")]
            [AiAttribut("Helfer für selfgrab on Breast")]
            SelfBreastGrab,

            /// <summary>
            /// Pose Detailos adden
            /// </summary>
            [Description("Pose\\Style - Add Detail.safetensors")]
            [AiAttribut("Pose Detailos adden")]
            StyleAddDetail,

            /// <summary>
            /// Helfer für tittenfuck und suck
            /// </summary>
            [Description("Pose\\tittysuck.safetensors")]
            [AiAttribut("Helfer für tittenfuck und suck")]
            tittysuck,

            /// <summary>
            /// Helfer zum, waschen der eigenen Haare
            /// </summary>
            [Description("Pose\\washing_ownhair.safetensors")]
            [AiAttribut("Helfer zum, waschen der eigenen Haare")]
            WashingOwnhair,

            /// <summary>
            /// Humping Pose Helper
            /// </summary>
            [Description("Pose\\XXX - Humping.safetensors")]
            [AiAttribut("Humping Pose Helper")]
            XxxHumping,

            /// <summary>
            /// Helper für Doggystyle von der seite
            /// </summary>
            [Description("Pose\\XXX - Side-Doggy.safetensors")]
            [AiAttribut("Helper für Doggystyle von der seite")]
            XxxSideDoggy,

            /// <summary>
            /// Ass Licking helper V2
            /// </summary>
            [Description("POV\\Ass Licking V2.safetensors")]
            [AiAttribut("Ass Licking helper V2")]
            AssLickingV2,

            /// <summary>
            /// Furry Burp Helper
            /// </summary>
            [Description("POV\\borp2-fluffyrock-768-lion-e24-05-lion_30.safetensors")]
            [AiAttribut("Furry Burp Helper")]
            Borp2Fluffyrock768LionE2405Lion30,

            /// <summary>
            /// Carnage Style! - Blood and gore
            /// </summary>
            [Description("POV\\CarnageStyle.safetensors")]
            [AiAttribut("Carnage Style! - Blood and gore")]
            CarnageStyle,

            /// <summary>
            /// POV Breast
            /// </summary>
            [Description("POV\\concept-pov_breasts-v1.safetensors")]
            [AiAttribut("POV Breast")]
            ConceptPovBreastsV1,

            /// <summary>
            /// POV Feet from below
            /// </summary>
            [Description("POV\\FeetFomBelow.safetensors")]
            [AiAttribut("POV Feet from below")]
            FeetFomBelow,

            /// <summary>
            /// Pillow Hug v10
            /// </summary>
            [Description("POV\\pillow_hug-10.safetensors")]
            [AiAttribut("Pillow Hug v10")]
            PillowHug10,

            /// <summary>
            /// Lesbian POV licking
            /// </summary>
            [Description("POV\\povcun-000015.safetensors")]
            [AiAttribut("Lesbian POV licking")]
            Povcun000015,

            /// <summary>
            /// Ring Gag 
            /// </summary>
            [Description("POV\\ring_yiffy3_v2.safetensors")]
            [AiAttribut("Ring Gag ")]
            RingYiffy3V2,

            /// <summary>
            /// SAhirt anheben
            /// </summary>
            [Description("POV\\Style - ShirtLift.safetensors")]
            [AiAttribut("SAhirt anheben")]
            StyleShirtLift,

            /// <summary>
            /// Deepthroat to base
            /// </summary>
            [Description("POV\\XXX - Deepthroat (all the way to the base).safetensors")]
            [AiAttribut("Deepthroat to base")]
            XxxDeepthroatallTheWayToTheBase,

            /// <summary>
            /// Headgrab Helper
            /// </summary>
            [Description("POV\\XXX - Headgrabblowjob.safetensors")]
            [AiAttribut("Headgrab Helper")]
            XxxHeadgrabblowjob,

            /// <summary>
            /// Side Fellation Helper
            /// </summary>
            [Description("POV\\XXX - SideFellatio (sidefellatio, fellatio, from side, 1boy, penis).safetensors")]
            [AiAttribut("Side Fellation Helper")]
            XxxSideFellatioSidefellatio,

            /// <summary>
            /// Stomach Bulge (penis-shaped distention)
            /// </summary>
            [Description("POV\\XXX - Stomach Bulge (penis-shaped distention).safetensors")]
            [AiAttribut("Stomach Bulge (penis-shaped distention)")]
            XxxStomachBulgePenisShapedDistention,

            /// <summary>
            /// Furry Tail Censoring
            /// </summary>
            [Description("Tails\\tail censor2-000008.safetensors")]
            [AiAttribut("Furry Tail Censoring")]
            TailCensor2000008,

            /// <summary>
            /// Furry Tail in pussy Helper
            /// </summary>
            [Description("Tails\\tail pussy-001_0.7.safetensors")]
            [AiAttribut("Furry Tail in pussy Helper")]
            TailPussy00107,

            /// <summary>
            /// Tail Job ;)
            /// </summary>
            [Description("Tails\\tail-pegging-v1.1.safetensors")]
            [AiAttribut("Tail Job ;)")]
            TailPeggingV11,

            /// <summary>
            /// Tail Furry Helper
            /// </summary>
            [Description("Tails\\TailsFixer.safetensors")]
            [AiAttribut("Tail Furry Helper")]
            TailsFixer,

            /// <summary>
            /// Tail  anheben Helper
            /// </summary>
            [Description("Tails\\tail_lift.safetensors")]
            [AiAttribut("Tail  anheben Helper")]
            TailLift
        }
    }


    /// <summary>
    /// zugriff auf die desc von den benötigten Enums
    /// </summary>
    internal static class EnumExtensions
    {
        // Methode zum Abrufen der Description eines Enum-Werts
        /// <summary>
        /// Bekommen Sie die Besschreibung aus dem Enum
        /// </summary>
        /// <param name="enumValue"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string? GetDescription<T>(this T enumValue) where T : IConvertible
        {
            if(enumValue is not Enum)
            {
                return null; // oder default(string), wenn kein Description-Attribut gefunden wurde
            }
            Type type = enumValue.GetType();
            Array values = Enum.GetValues(type);
#pragma warning disable
            return (from int val in values
                where val == enumValue.ToInt32(CultureInfo.InvariantCulture)
                select type.GetMember(type.GetEnumName(val))
                into memInfo
                select memInfo[0]
                       .GetCustomAttributes(typeof(DescriptionAttribute), false)
                       .FirstOrDefault()).OfType<DescriptionAttribute>()
                                         .Select(descriptionAttribute => descriptionAttribute.Description)
                                         .FirstOrDefault();
#pragma warning restore
        }

        public static Dictionary<string, List<string>> GetEnumsGroupedByCategory<T>() where T : Enum
        {
            Dictionary<string, List<string>> groupedEnums = Enum.GetValues(typeof(T))
                                                                .Cast<Enum>()
                                                                .GroupBy(e => e.GetCategory())
                                                                .ToDictionary(
                                                                    g => g.Key,
                                                                    g => g.Select(e => e.ToString()).ToList()
                                                                );
            return groupedEnums;
        }

        public static string GetAiDesc<T>(this T enumValue)
        {
            FieldInfo? fieldInfo = enumValue!.GetType().GetField(enumValue!.ToString()!);

            // Abrufen des AIAttribut aus dem FieldInfo
            AiAttribut? attribute = fieldInfo!.GetCustomAttribute<AiAttribut>(false);

            // Rückgabe der Beschreibung, falls vorhanden, sonst null
            return attribute?.Beschreibung!;
        }
        
        private static string GetCategory<T>(this T enumValue)
        {
            Type type = enumValue!.GetType();
            MemberInfo[] memberInfo = type.GetMember(enumValue.ToString()!);
            object[] attributes = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
            string description = ((DescriptionAttribute)attributes[0]).Description;
            string category = description.Contains('\\') ? description.Split('\\')[0] : description; // Nehme den ersten Teil vor dem Backslash als Kategorie oder die gesamte Beschreibung, wenn kein Backslash vorhanden ist
            return category;
        }
    }
}