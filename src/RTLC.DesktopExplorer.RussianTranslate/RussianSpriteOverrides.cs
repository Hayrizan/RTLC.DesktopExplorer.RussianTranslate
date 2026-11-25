using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace RTLC.DesktopExplorer.RussianTranslate
{
    internal sealed class RussianSpriteOverrides : IDisposable
    {
        private static readonly Dictionary<string, string> Files = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["mummy_1"] = "mummy_1.png",
            ["newRubiconBMP"] = "newRubiconBMP.png",
            ["wall_crocodile_B"] = "wall_crocodile_B.png",
            ["publisherPlaceholder"] = "publisherPlaceholder.png",
            ["door_Janitor_BaseMap"] = "door_Janitor_BaseMap.png",
            ["door_Janitor_Normal"] = "door_Janitor_Normal.png",
            ["chainmailer"] = "chainmailer.png",
            ["Rug_cabinetRug_BaseMap"] = "Rug_cabinetRug_BaseMap.png",
            ["ArcadeStore_BaseMap"] = "ArcadeStore_BaseMap.png",
            ["ArcadeStore_Emissive"] = "ArcadeStore_Emissive.png",
            ["Store_Food_BaseMap"] = "Store_Food_BaseMap.png",
            ["Store_Food_Emissive"] = "Store_Food_Emissive.png",
            ["triopainting_BaseMap"] = "triopainting_BaseMap.png",
            ["feathersedge"] = "feathersedge.png",
            ["NextRoomsLogo"] = "NextRoomsLogo.png",
            ["CS_NextRoomsLogo"] = "NextRoomsLogo.png",
            ["SS_Bounce"] = "SS_Bounce.png",
            ["SS_Classic"] = "SS_Classic.png",
            ["SS_Rain"] = "SS_Rain.png",
            ["PL_counter_BaseMap"] = "PL_counter_BaseMap.png",
            ["CS_PL_counter_BaseMap"] = "PL_counter_BaseMap.png",
            ["PL_counter_Normal"] = "PL_counter_Normal.png",
            ["PL_counter_Emissive"] = "PL_counter_Emissive.png",
            ["CS_PL_counter_Emissive"] = "PL_counter_Emissive.png",
            ["CT_PL_counter_Emissive"] = "PL_counter_Emissive.png",
            ["arcadephotofront_BaseMap"] = "arcadephotofront_BaseMap.png",
            ["CS_arcadephotofront_BaseMap"] = "arcadephotofront_BaseMap.png",
            ["rod_and_reel_cover"] = "rod_and_reel_cover.png",
            ["CS_rod_and_reel_cover"] = "rod_and_reel_cover.png",
            ["CT_rod_and_reel_cover"] = "rod_and_reel_cover.png",
            ["Slasher_fest_cover"] = "Slasher_fest_cover.png",
            ["CS_Slasher_fest_cover"] = "Slasher_fest_cover.png",
            ["CT_Slasher_fest_cover"] = "Slasher_fest_cover.png",
            ["wallpaper_sweaters"] = "wallpaper_sweaters.png",
            ["stp_StorytellerBanner"] = "stp_StorytellerBanner.png",
            ["6"] = "stp_StorytellerBanner.png",
            ["stp_BetaDefault"] = "stp_BetaDefault.png",
            ["9"] = "stp_BetaDefault.png",
            ["stp_BetaSelected"] = "stp_BetaSelected.png",
            ["9_h"] = "stp_BetaSelected.png",
            ["stp_FAQDefault"] = "stp_FAQDefault.png",
            ["8"] = "stp_FAQDefault.png",
            ["stp_FAQSelected"] = "stp_FAQSelected.png",
            ["8_h"] = "stp_FAQSelected.png",
            ["stp_orderNowDefault"] = "stp_orderNowDefault.png",
            ["stp_orderNowSelected"] = "stp_orderNowSelected.png",
            ["order_now"] = "order_now.png",
            ["order_now_selected"] = "stp_orderNowSelected.png",
            ["Rabbit_Drawing"] = "Rabbit_Drawing.png",
            ["8ball_empty"] = "8ball_empty.png",
            ["ball_message_1"] = "ball_message_1.png",
            ["ball_message_2"] = "ball_message_2.png",
            ["ball_message_3"] = "ball_message_3.png",
            ["ball_message_4"] = "ball_message_4.png",
            ["ball_message_5"] = "ball_message_5.png",
            ["chrono_about_B"] = "chrono_about_B.png",
            ["color_about_Y"] = "color_about_Y.png",
            ["note_about_R"] = "note_about_R.png",
            ["Class95"] = "Class95.png",
            ["corpseImage"] = "corpseImage.png",
            ["Dream_in_gouache_cover"] = "Dream_in_gouache_cover.png",
            ["Photobooth_BaseMap"] = "Photobooth_BaseMap.png",
            ["Photobooth_Emissive"] = "Photobooth_Emissive.png",
            ["PL_Door_PL_Door_BaseMap"] = "PL_Door_PL_Door_BaseMap.png",
            ["PL_Door_PL_Door_Emissive"] = "PL_Door_PL_Door_Emissive.png",
            ["PL_Folder_PL_Folder_BaseMap"] = "PL_Folder_PL_Folder_BaseMap.png",
            ["Store_Signs_BaseMap"] = "Store_Signs_BaseMap.png",
            ["Store_Signs_Emissive"] = "Store_Signs_Emissive.png",
            ["StoreSigns2_BaseMap"] = "StoreSigns2_BaseMap.png",
            ["StoreSigns2_Emissive"] = "StoreSigns2_Emissive.png",
            ["Store_Magic_Pet_BaseMap"] = "Store_Magic_Pet_BaseMap.png",
            ["Store_Video_Art_BaseMap"] = "Store_Video_Art_BaseMap.png",
            ["comingsoon_BaseMap"] = "comingsoon_BaseMap.png",
            ["VS_Stand_BaseMap"] = "VS_Stand_BaseMap.png",
            ["VS_Walls_BaseMap"] = "VS_Walls_BaseMap.png",
            ["Group 904"] = "Group 904.png",
            ["VS_Rug_BaseMap"] = "VS_Rug_BaseMap.png",
            ["gothtopicfront_BaseMap"] = "gothtopicfront_BaseMap.png",
            ["bunnypaper_BaseMap"] = "bunnypaper_BaseMap.png",
            ["PL_Portraits_BaseMap"] = "PL_Portraits_BaseMap.png",
            ["NoEscape"] = "NoEscape.png",
            ["NoEscape_Y"] = "NoEscape_Y.png",
            ["Fleeting_face_Y"] = "Fleeting_face_Y.png",
            ["Fleeting_face_colorlens"] = "Fleeting_face_Y.png",
            ["stp_tagline"] = "stp_tagline.png",
            ["10"] = "stp_tagline.png",
            ["datarain_maskB"] = "datarain_maskB.png",
            ["DareToSayMyNameBMP"] = "DareToSayMyNameBMP.png",
            ["encryptInstructions"] = "encryptInstructions.png",
            ["VS_posters_poster_L_1_BaseMap"] = "VS_posters_poster_L_1_BaseMap.png",
            ["VS_posters_Poster_A_BaseMap"] = "VS_posters_Poster_A_BaseMap.png",
            ["VS_posters_Poster_B_BaseMap"] = "VS_posters_Poster_B_BaseMap.png",
            ["VS_posters_Poster_L_2_BaseMap"] = "VS_posters_Poster_L_2_BaseMap.png",
            ["VS_posters_Poster_L_3_BaseMap"] = "VS_posters_Poster_L_3_BaseMap.png",
            ["playcard_BaseMap"] = "playcard_BaseMap.png",
            ["CardsSpriteSheet"] = "CardsSpriteSheet.png",
            ["logo_desktop"] = "logo_desktop.png",
            ["NextOSLogin_Logo"] = "NextOSLogin_Logo.png",
            ["ID_Emmet_BaseMap"] = "ID_Emmet_BaseMap.png",
            ["ID_Max_BaseMap"] = "ID_Max_BaseMap.png",
            ["OceanTideLogo"] = "OceanTideLogo.png",
            ["logo_explorer_0"] = "logo_explorer_0.png",
            ["logo_explorer_1"] = "logo_explorer_1.png",
            ["logo_explorer_2"] = "logo_explorer_2.png",
            ["logo_explorer_3"] = "logo_explorer_3.png",
            ["logo_explorer_4"] = "logo_explorer_4.png",
            ["logo_explorer_5"] = "logo_explorer_5.png",
            ["logo_explorer_6"] = "logo_explorer_6.png",
            ["logo_explorer_7"] = "logo_explorer_7.png",
            ["logo_explorer_8"] = "logo_explorer_8.png",
            ["fridgemagnets"] = "fridgemagnets.png",
            ["Cover_torn"] = "Cover_torn.png",
            ["Cover_burnt"] = "Cover_burnt.png",
            ["Cover_painted"] = "Cover_painted.png",
            ["MeItRuneBMP"] = "MeItRuneBMP.png",
            ["RenaRuneBMP"] = "RenaRuneBMP.png",
            ["名"] = "MeItRuneBMP.png",
            ["重命"] = "RenaRuneBMP.png",
            ["wp_max_starPhaser"] = "wp_max_starPhaser.png",
            ["SkeletonKey 1"] = "SkeletonKey 1.png",
            ["banners_about_chronolog-new"] = "banners_about_chronolog-new.png",
            ["banners_about_chronolog-water"] = "banners_about_chronolog-water.png",
            ["banners_about_Websurfer"] = "banners_about_Websurfer.png",
            ["banners_about_ColorLens"] = "banners_about_ColorLens-new.png",
            ["banners_about_ColorLens-new"] = "banners_about_ColorLens-new.png",
            ["banners_about_ColorLens-moon"] = "banners_about_ColorLens-moon.png",
            ["banners_about_Notepage"] = "banners_about_Notepage.png",
            ["banners_about_Notepage-new"] = "banners_about_Notepage-new.png",
            ["banners_about_Notepage-fire"] = "banners_about_Notepage-fire.png",
            ["provider"] = "provider.png",
            ["banners_about_sketchy"] = "banners_about_sketchy.png",
            ["banners_about_stp"] = "banners_about_stp.png",
            ["MS_PosterA_BaseMap"] = "MS_PosterA_BaseMap.png",
            ["MS_PosterB_BaseMap"] = "MS_PosterB_BaseMap.png",
            ["MS_DeckSign_BaseMap"] = "MS_DeckSign_BaseMap.png",
            ["StickerPack_BaseMap"] = "StickerPack_BaseMap.png",
            ["InstantCamera_Clean_BaseMap"] = "InstantCamera_Clean_BaseMap.png",
            ["InstantCamera_BaseMap"] = "InstantCamera_BaseMap.png",
            ["MS_Paintinfg_BaseMap"] = "MS_Paintinfg_BaseMap.png",
            ["MS_Paintinfg_Normal"] = "MS_Paintinfg_Normal.png",
            ["Guppy_Chesss_Board_BaseMap"] = "Guppy_Chesss_Board_BaseMap.png",
            ["Guppy_Chesss_Board_MaskMap"] = "Guppy_Chesss_Board_MaskMap.png",
            ["Guppy_Chesss_Board_Normal"] = "Guppy_Chesss_Board_Normal.png",
            ["Elevator_BaseMap"] = "Elevator_BaseMap.png",
            ["Elevator_Normal"] = "Elevator_Normal.png",
            ["minesniffer_title"] = "minesniffer_title.png",
            ["Cabinet_MS_BaseMap"] = "Cabinet_MS_BaseMap.png",
            ["Cabinet_MS_Emissive"] = "Cabinet_MS_Emissive.png",
            ["CopperBox_BaseMap"] = "CopperBox_BaseMap.png",
            ["GunnyArcade_BaseMap"] = "GunnyArcade_BaseMap.png",
            ["locker_col_BaseMap"] = "locker_col_BaseMap.png",
            ["Cabinet_A_BaseMap"] = "Cabinet_A_BaseMap.png",
            ["Cabinet_B_BaseMap"] = "Cabinet_B_BaseMap.png",
            ["Cabinet_C_BaseMap"] = "Cabinet_C_BaseMap.png",
            ["Cabinet_D_BaseMap"] = "Cabinet_D_BaseMap.png",
            ["Cabinet_E_BaseMap"] = "Cabinet_E_BaseMap.png",
            ["Cabinet_F_BaseMap"] = "Cabinet_F_BaseMap.png",
            ["Cabinet_WH_BaseMap"] = "Cabinet_WH_BaseMap.png",
            ["Cabinet_WH_Emissive"] = "Cabinet_WH_Emissive.png",
            ["WormholePoster_BaseMap"] = "WormholePoster_BaseMap.png",
            ["YourHand_blueMaskBMP"] = "YourHand_blueMaskBMP.png",
            ["YourHand_redMaskBMP"] = "YourHand_redMaskBMP.png",
            ["vendingmachine_BaseMap"] = "vendingmachine_BaseMap.png",
            ["vendingmachine_Normal"] = "vendingmachine_Normal.png",
            ["storefronts_default_BaseMap"] = "storefronts_default_BaseMap.png",
            ["windows_default_BaseMap"] = "windows_default_BaseMap.png",
            ["trash_BaseMap"] = "trash_BaseMap.png",
            ["JanitorCone_BaseMap"] = "JanitorCone_BaseMap.png",
            ["JanitorMop_BaseMap"] = "JanitorMop_BaseMap.png",
            ["Sign_SolPlaza_BaseMap"] = "Sign_SolPlaza_BaseMap.png",
            ["wormHole_Title"] = "wormHole_Title.png",
            ["maze_password"] = "maze_password.png",
            ["Join_the_dots"] = "Join_the_dots.png",
            ["FAQ"] = "FAQ.png",
            ["WheelOfFortune"] = "WheelOfFortune.png",
            ["abandoned"] = "abandoned.png",
            ["Fortune_ticket_front"] = "Fortune_ticket_front.png",
            ["Fortune_ticket_back"] = "Fortune_ticket_back.png",
            ["arcadeCard"] = "arcadeCard.png",
            ["wall_paintings_A"] = "wall_paintings_A.png",
            ["wall_paintings_B"] = "wall_paintings_B.png",
            ["maze_lines_colors"] = "maze_lines_colors.png",
            ["maze_clouds_lines"] = "maze_clouds_lines.png",
            ["messyCloset"] = "messyCloset.png",
            ["Cherry_booms"] = "Cherry_booms.png",
            ["candyWrapper"] = "candyWrapper.png",
            ["defi"] = "defi.png",
            ["brokencompass"] = "brokencompass.png",
            ["goin_amongUsGaveMeMoney_500px"] = "goin_amongUsGaveMeMoney_500px.png",
            ["goin_amongUsGaveMeMoney"] = "goin_amongUsGaveMeMoney.png",
            ["Pix_Prescription"] = "Pix_Prescription.png",
            ["3"] = "Pix_Prescription.png",
            ["Pix_JuniorHighDiploma"] = "Pix_JuniorHighDiploma.png",
            ["Pix_AdoptionForm"] = "Pix_AdoptionForm.png",
            ["adoptionForm"] = "adoptionForm.png",
            ["Pix_InsuranceForm"] = "Pix_InsuranceForm.png",
            ["5"] = "Pix_InsuranceForm.png",
            ["Pix_EnElPamfleto"] = "Pix_EnElPamfleto.png",
            ["4"] = "Pix_EnElPamfleto.png",
            ["VS_Player_BaseMap"] = "VS_Player_BaseMap.png",
            ["Zarroh_Box_BaseMap"] = "Zarroh_Box_BaseMap.png",
            ["Monts_Furniture_color"] = "Monts_Furniture_color.png",
            ["Pinball_A_BaseMap"] = "Pinball_A_BaseMap.png",
            ["Pinball_B_BaseMap"] = "Pinball_B_BaseMap.png",
            ["CS_DareToSayMyNameBMP"] = "DareToSayMyNameBMP.png",
            ["CT_DareToSayMyNameBMP"] = "DareToSayMyNameBMP.png",
            ["CS_8ball_empty"] = "8ball_empty.png",
            ["CS_ball_message_1"] = "ball_message_1.png",
            ["CS_ball_message_2"] = "ball_message_2.png",
            ["CS_ball_message_3"] = "ball_message_3.png",
            ["CS_ball_message_4"] = "ball_message_4.png",
            ["CS_ball_message_5"] = "ball_message_5.png",
            ["CS_bunnypaper_BaseMap"] = "bunnypaper_BaseMap.png",
            ["CS_chrono_about_B"] = "chrono_about_B.png",
            ["CS_Class95"] = "Class95.png",
            ["CS_Cover_torn"] = "Cover_torn.png",
            ["CS_Cover_burnt"] = "Cover_burnt.png",
            ["CS_Cover_painted"] = "Cover_painted.png",
            ["CS_color_about_Y"] = "color_about_Y.png",
            ["CS_comingsoon_BaseMap"] = "comingsoon_BaseMap.png",
            ["CS_corpseImage"] = "corpseImage.png",
            ["CS_datarain_maskB"] = "datarain_maskB.png",
            ["CS_Dream_in_gouache_cover"] = "Dream_in_gouache_cover.png",
            ["CS_encryptInstructions"] = "encryptInstructions.png",
            ["CS_gothtopicfront_BaseMap"] = "gothtopicfront_BaseMap.png",
            ["CS_Group 904"] = "Group 904.png",
            ["CS_note_about_R"] = "note_about_R.png",
            ["CS_Photobooth_BaseMap"] = "Photobooth_BaseMap.png",
            ["CS_Photobooth_Emissive"] = "Photobooth_Emissive.png",
            ["CS_PL_Door_BaseMap"] = "PL_Door_PL_Door_BaseMap.png",
            ["CS_PL_Door_Emissive"] = "PL_Door_PL_Door_Emissive.png",
            ["CS_PL_Folder_BaseMap"] = "PL_Folder_PL_Folder_BaseMap.png",
            ["CS_PL_Portraits_BaseMap"] = "PL_Portraits_BaseMap.png",
            ["CS_Rabbit_Drawing"] = "Rabbit_Drawing.png",
            ["CS_Store_Magic_Pet_BaseMap"] = "Store_Magic_Pet_BaseMap.png",
            ["CS_Store_Signs_BaseMap"] = "Store_Signs_BaseMap.png",
            ["CS_Store_Video_Art_BaseMap"] = "Store_Video_Art_BaseMap.png",
            ["CS_StoreSigns2_BaseMap"] = "StoreSigns2_BaseMap.png",
            ["CS_VS_posters_poster_L_1_BaseMap"] = "VS_posters_poster_L_1_BaseMap.png",
            ["CS_VS_posters_Poster_L_2_BaseMap"] = "VS_posters_Poster_L_2_BaseMap.png",
            ["CS_VS_posters_Poster_L_3_BaseMap"] = "VS_posters_Poster_L_3_BaseMap.png",
            ["CS_VS_Stand_BaseMap"] = "VS_Stand_BaseMap.png",
            ["CS_VS_Walls_BaseMap"] = "VS_Walls_BaseMap.png",
            ["CS_wp_max_sweaters"] = "wallpaper_sweaters.png",
            ["CT_8ball_empty"] = "8ball_empty.png",
            ["CT_ball_message_1"] = "ball_message_1.png",
            ["CT_ball_message_2"] = "ball_message_2.png",
            ["CT_ball_message_3"] = "ball_message_3.png",
            ["CT_ball_message_4"] = "ball_message_4.png",
            ["CT_ball_message_5"] = "ball_message_5.png",
            ["CT_bunnypaper_BaseMap"] = "bunnypaper_BaseMap.png",
            ["CT_chrono_about_B"] = "chrono_about_B.png",
            ["CT_Class95"] = "Class95.png",
            ["CT_Cover_torn"] = "Cover_torn.png",
            ["CT_Cover_burnt"] = "Cover_burnt.png",
            ["CT_Cover_painted"] = "Cover_painted.png",
            ["CT_color_about_Y"] = "color_about_Y.png",
            ["CT_comingsoon_BaseMap"] = "comingsoon_BaseMap.png",
            ["CT_corpseImage"] = "corpseImage.png",
            ["CT_datarain_maskB"] = "datarain_maskB.png",
            ["CT_Dream_in_gouache_cover"] = "Dream_in_gouache_cover.png",
            ["CT_encryptInstructions"] = "encryptInstructions.png",
            ["CT_gothtopicfront_BaseMap"] = "gothtopicfront_BaseMap.png",
            ["CT_Group 904"] = "Group 904.png",
            ["CT_note_about_R"] = "note_about_R.png",
            ["CT_Photobooth_BaseMap"] = "Photobooth_BaseMap.png",
            ["CT_Photobooth_Emissive"] = "Photobooth_Emissive.png",
            ["CT_PL_Door_BaseMap"] = "PL_Door_PL_Door_BaseMap.png",
            ["CT_PL_Door_Emissive"] = "PL_Door_PL_Door_Emissive.png",
            ["CT_PL_Folder_BaseMap"] = "PL_Folder_PL_Folder_BaseMap.png",
            ["CT_PL_counter_BaseMap"] = "PL_counter_BaseMap.png",
            ["CT_arcadephotofront_BaseMap"] = "arcadephotofront_BaseMap.png",
            ["CT_PL_Portraits_BaseMap"] = "PL_Portraits_BaseMap.png",
            ["CT_Rabbit_Drawing"] = "Rabbit_Drawing.png",
            ["CT_Store_Magic_Pet_BaseMap"] = "Store_Magic_Pet_BaseMap.png",
            ["CT_Store_Signs_BaseMap"] = "Store_Signs_BaseMap.png",
            ["CT_Store_Video_Art_BaseMap"] = "Store_Video_Art_BaseMap.png",
            ["CT_StoreSigns2_BaseMap"] = "StoreSigns2_BaseMap.png",
            ["CT_VS_posters_poster_L_1_BaseMap"] = "VS_posters_poster_L_1_BaseMap.png",
            ["CT_VS_posters_Poster_L_2_BaseMap"] = "VS_posters_Poster_L_2_BaseMap.png",
            ["CT_VS_posters_Poster_L_3_BaseMap"] = "VS_posters_Poster_L_3_BaseMap.png",
            ["CT_VS_Stand_BaseMap"] = "VS_Stand_BaseMap.png",
            ["CT_VS_Walls_BaseMap"] = "VS_Walls_BaseMap.png",
            ["CT_wp_max_sweaters"] = "wallpaper_sweaters.png",
            ["ES_8ball_empty"] = "8ball_empty.png",
            ["ES_8ball_message1"] = "ball_message_1.png",
            ["ES_8ball_message2"] = "ball_message_2.png",
            ["ES_8ball_message3"] = "ball_message_3.png",
            ["ES_8ball_message4"] = "ball_message_4.png",
            ["ES_8ball_message5"] = "ball_message_5.png",
            ["ES_bunnypaper_BaseMap"] = "bunnypaper_BaseMap.png",
            ["ES_chrono_about_B"] = "chrono_about_B.png",
            ["ES_Class95"] = "Class95.png",
            ["ES_color_about_Y"] = "color_about_Y.png",
            ["ES_comingsoon_BaseMap"] = "comingsoon_BaseMap.png",
            ["ES_corpseImage"] = "corpseImage.png",
            ["ES_datarain_maskB"] = "datarain_maskB.png",
            ["ES_Dream_in_gouache_cover"] = "Dream_in_gouache_cover.png",
            ["ES_encryptinstructions"] = "encryptInstructions.png",
            ["ES_gothtopicfront_BaseMap"] = "gothtopicfront_BaseMap.png",
            ["ES_note_about_R"] = "note_about_R.png",
            ["ES_Photobooth_BaseMap"] = "Photobooth_BaseMap.png",
            ["ES_Photobooth_Emissive"] = "Photobooth_Emissive.png",
            ["ES_PL_Door_PL_Door_BaseMap"] = "PL_Door_PL_Door_BaseMap.png",
            ["ES_PL_Door_PL_Door_Emissive"] = "PL_Door_PL_Door_Emissive.png",
            ["ES_PL_Portraits_BaseMap"] = "PL_Portraits_BaseMap.png",
            ["ES_Rabbit_Drawing"] = "Rabbit_Drawing.png",
            ["ES_Store_Magic_Pet_BaseMap"] = "Store_Magic_Pet_BaseMap.png",
            ["ES_Store_Signs_BaseMap"] = "Store_Signs_BaseMap.png",
            ["ES_Store_Video_Art_BaseMap"] = "Store_Video_Art_BaseMap.png",
            ["ES_StoreSigns2_BaseMap"] = "StoreSigns2_BaseMap.png",
            ["ES_VS_posters_poster_L_1_BaseMap"] = "VS_posters_poster_L_1_BaseMap.png",
            ["ES_VS_posters_Poster_L_2_BaseMap"] = "VS_posters_Poster_L_2_BaseMap.png",
            ["ES_VS_posters_Poster_L_3_BaseMap"] = "VS_posters_Poster_L_3_BaseMap.png",
            ["ES_VS_Stand_BaseMap"] = "VS_Stand_BaseMap.png",
            ["ES_VS_Walls_BaseMap"] = "VS_Walls_BaseMap.png",
            ["ES_Group_904"] = "Group 904.png",
            ["ES_PL_Folder_BaseMap"] = "PL_Folder_PL_Folder_BaseMap.png"
        };

        private static readonly HashSet<string> LinearTextureNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "door_Janitor_Normal",
            "vendingmachine_Normal",
            "PL_counter_Normal",
            "MS_Paintinfg_Normal",
            "Guppy_Chesss_Board_MaskMap",
            "Guppy_Chesss_Board_Normal",
            "Elevator_Normal"
        };

        private readonly LocalizationBundle payload;
        private readonly ManualLogSource log;
        // One texture can contain several sprites with different UV rectangles and pivots.
        private readonly Dictionary<Sprite, Sprite> replacements = new Dictionary<Sprite, Sprite>();
        private readonly Dictionary<Sprite, Sprite> replacementSources = new Dictionary<Sprite, Sprite>();
        private static readonly System.Reflection.FieldInfo OverrideSpriteField = AccessTools.Field(typeof(Image), "m_OverrideSprite");
        private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private readonly Dictionary<Image, Sprite> originalSprites = new Dictionary<Image, Sprite>();
        private readonly Dictionary<Image, Sprite> originalOverrides = new Dictionary<Image, Sprite>();
        private readonly Dictionary<RawImage, Texture> originalRawTextures = new Dictionary<RawImage, Texture>();
        private readonly Dictionary<Material, Dictionary<int, Texture>> originalMaterialTextures = new Dictionary<Material, Dictionary<int, Texture>>();
        private readonly HashSet<string> logged = new HashSet<string>(StringComparer.Ordinal);
        private bool applyingSprite;

        internal RussianSpriteOverrides(LocalizationBundle payload, ManualLogSource log)
        {
            this.payload = payload;
            this.log = log;
        }

        internal void BeforeSpriteAssignment(Image image, ref Sprite value, bool isOverride)
        {
            if (image == null || applyingSprite)
                return;

            // The game can copy a currently localized sprite into another Image.
            // Track its native source so both images can restore English independently.
            if (value != null && replacementSources.TryGetValue(value, out Sprite source))
                value = source;

            var originals = isOverride ? originalOverrides : originalSprites;
            if (value == null || value.texture == null || !Files.ContainsKey(value.texture.name))
            {
                // An external assignment supersedes the previous image, including null.
                originals.Remove(image);
                return;
            }

            string textureName = value.texture.name;
            originals[image] = value;
            if (!RussianHardcodedText.IsRussian)
                return;

            try
            {
                value = GetReplacement(textureName, value);
            }
            catch (Exception exception)
            {
                LogOnce(textureName, $"Could not localize sprite {textureName}: {exception.Message}");
            }
        }

        internal void OnImageEnabled(Image image)
        {
            if (image == null)
                return;

            // Image.overrideSprite returns the base sprite when the override is null.
            // Read the actual field to preserve cleared overrides and equal base/override sprites.
            RefreshEnabledSprite(image, (Sprite)OverrideSpriteField.GetValue(image), true);
            RefreshEnabledSprite(image, image.sprite, false);
            RussianTitleLogo.ResizeDesktopLogo(image.rectTransform, image.overrideSprite?.texture);
        }

        private void RefreshEnabledSprite(Image image, Sprite current, bool isOverride)
        {
            Sprite desired = current;
            BeforeSpriteAssignment(image, ref desired, isOverride);
            if (desired != current)
                SetImageSprite(image, desired, isOverride);
        }

        private void SetImageSprite(Image image, Sprite value, bool isOverride)
        {
            bool previous = applyingSprite;
            applyingSprite = true;
            try
            {
                if (isOverride) image.overrideSprite = value;
                else image.sprite = value;
            }
            finally { applyingSprite = previous; }
        }

        internal void ApplyCurrentLocale()
        {
            foreach (var item in new List<KeyValuePair<Image, Sprite>>(originalSprites))
                if (item.Key != null)
                    Apply(item.Key, item.Value, false);
            foreach (var item in new List<KeyValuePair<Image, Sprite>>(originalOverrides))
                if (item.Key != null)
                    Apply(item.Key, item.Value, true);
            ApplyRawImageOverrides();
            ScanTextures();
        }

        internal void BeforeTextureAssignment(RawImage image, ref Texture value)
        {
            if (image == null)
                return;

            if (value == null || !Files.ContainsKey(value.name))
                return;

            originalRawTextures[image] = value;
            if (!RussianHardcodedText.IsRussian)
                return;

            try
            {
                value = GetReplacementTexture(value.name, value);
            }
            catch (Exception exception)
            {
                LogOnce(value.name, $"Could not localize texture {value.name}: {exception.Message}");
            }
        }

        internal void ScanTextures()
        {
            ApplyRawImageOverrides();
            ApplyMaterialOverrides();
            if (!RussianHardcodedText.IsRussian)
                return;

            foreach (RawImage image in Resources.FindObjectsOfTypeAll<RawImage>())
            {
                if (!image || !image.isActiveAndEnabled || !image.gameObject.scene.IsValid())
                    continue;

                Texture original = image.texture;
                if (original == null || !Files.ContainsKey(original.name))
                    continue;

                originalRawTextures[image] = original;
                try
                {
                    image.texture = GetReplacementTexture(original.name, original);
                }
                catch (Exception exception)
                {
                    LogOnce(original.name, $"Could not localize texture {original.name}: {exception.Message}");
                }
            }

            foreach (Material material in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (!material)
                    continue;

                string[] properties;
                try
                {
                    properties = material.GetTexturePropertyNames();
                }
                catch
                {
                    continue;
                }

                foreach (string property in properties)
                {
                    int propertyId = Shader.PropertyToID(property);
                    Texture original = material.GetTexture(propertyId);
                    if (original == null || !Files.ContainsKey(original.name))
                        continue;

                    if (!originalMaterialTextures.TryGetValue(material, out Dictionary<int, Texture> originalProperties))
                    {
                        originalProperties = new Dictionary<int, Texture>();
                        originalMaterialTextures.Add(material, originalProperties);
                    }
                    originalProperties[propertyId] = original;

                    try
                    {
                        Texture2D replacement = GetReplacementTexture(original.name, original);
                        if (material.GetTexture(propertyId) != replacement)
                            material.SetTexture(propertyId, replacement);
                    }
                    catch (Exception exception)
                    {
                        LogOnce(original.name, $"Could not localize material texture {original.name}: {exception.Message}");
                    }
                }
            }
        }

        private void ApplyRawImageOverrides()
        {
            foreach (var item in new List<KeyValuePair<RawImage, Texture>>(originalRawTextures))
            {
                RawImage image = item.Key;
                Texture original = item.Value;
                if (!image || !original || !Files.ContainsKey(original.name))
                    continue;

                try
                {
                    if (RussianHardcodedText.IsRussian)
                    {
                        Texture2D replacement = GetReplacementTexture(original.name, original);
                        if (image.texture == original)
                            image.texture = replacement;
                    }
                    else if (textures.TryGetValue(original.name, out Texture2D replacement) && image.texture == replacement)
                        image.texture = original;
                }
                catch (Exception exception)
                {
                    LogOnce(original.name, $"Could not update raw image {original.name}: {exception.Message}");
                }
            }
        }

        private void ApplyMaterialOverrides()
        {
            foreach (var materialItem in new List<KeyValuePair<Material, Dictionary<int, Texture>>>(originalMaterialTextures))
            {
                Material material = materialItem.Key;
                if (!material)
                {
                    originalMaterialTextures.Remove(materialItem.Key);
                    continue;
                }

                foreach (var textureItem in materialItem.Value)
                {
                    Texture original = textureItem.Value;
                    if (!original || !Files.ContainsKey(original.name) || !textures.TryGetValue(original.name, out Texture2D replacement))
                        continue;

                    try
                    {
                        Texture current = material.GetTexture(textureItem.Key);
                        if (RussianHardcodedText.IsRussian)
                        {
                            if (current == original)
                                material.SetTexture(textureItem.Key, replacement);
                        }
                        else if (current == replacement)
                            material.SetTexture(textureItem.Key, original);
                    }
                    catch (Exception exception)
                    {
                        LogOnce(original.name, $"Could not restore material texture {original.name}: {exception.Message}");
                    }
                }
            }
        }

        private void Apply(Image image, Sprite original, bool isOverride)
        {
            if (!image || !original)
                return;

            Sprite current = isOverride ? (Sprite)OverrideSpriteField.GetValue(image) : image.sprite;
            replacements.TryGetValue(original, out Sprite previousReplacement);
            bool isOwnReplacement = previousReplacement != null && current == previousReplacement;
            if (current != original && !isOwnReplacement)
            {
                // A backing-field/animation change also supersedes the tracked image.
                (isOverride ? originalOverrides : originalSprites).Remove(image);
                return;
            }
            if (!RussianHardcodedText.IsRussian)
            {
                if (current != original)
                    SetImageSprite(image, original, isOverride);
                return;
            }

            if (original.texture == null || !Files.ContainsKey(original.texture.name))
                return;

            try
            {
                Sprite replacement = GetReplacement(original.texture.name, original);
                if (current != replacement)
                    SetImageSprite(image, replacement, isOverride);
            }
            catch (Exception exception)
            {
                LogOnce(original.texture.name, $"Could not localize sprite {original.texture.name}: {exception.Message}");
            }
        }

        private Sprite GetReplacement(string textureName, Sprite original)
        {
            if (replacements.TryGetValue(original, out Sprite cached) && cached != null)
                return cached;

            Texture2D texture = GetReplacementTexture(textureName, original.texture);

            Rect rect = original.rect;
            if (rect.xMin < 0 || rect.yMin < 0 || rect.xMax > texture.width || rect.yMax > texture.height)
            {
                throw new InvalidDataException($"Sprite rect {rect} is outside the localized texture {texture.width}x{texture.height}.");
            }

            if (textureName == "logo_desktop")
                rect = new Rect(0, 0, texture.width, texture.height);

            Vector2 size = original.rect.size;
            Vector2 pivot = new Vector2(original.pivot.x / size.x, original.pivot.y / size.y);
            Sprite sprite = Sprite.Create(texture, rect, pivot, original.pixelsPerUnit, 0, SpriteMeshType.FullRect, original.border, false);
            if (sprite == null)
            {
                throw new InvalidOperationException("Unity could not create a sprite for " + textureName);
            }

            sprite.name = "RTLC " + textureName + " ru-RU";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            replacements[original] = sprite;
            replacementSources[sprite] = original;
            LogOnce(textureName, $"Localized image {textureName} from {Files[textureName]}.");
            return sprite;
        }

        private Texture2D GetReplacementTexture(string textureName, Texture original)
        {
            if (textures.TryGetValue(textureName, out Texture2D cached) && cached != null)
                return cached;

            string path = "web-assets/" + Files[textureName];
            var asset = payload.Require<TextAsset>(path);

            bool isLinear = LinearTextureNames.Contains(textureName);
            bool mipChain = original is Texture2D originalTexture && originalTexture.mipmapCount > 1;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain, isLinear);
            texture.name = "RTLC " + textureName + " ru-RU";
            texture.hideFlags = HideFlags.HideAndDontSave;
            if (!ImageConversion.LoadImage(texture, asset.bytes, false))
            {
                UnityEngine.Object.Destroy(texture);
                throw new InvalidDataException("Unity could not decode " + path);
            }

            // The standalone title logo can use a larger canvas. Atlas replacements must still match.
            if (textureName != "logo_desktop" && original != null &&
                (texture.width != original.width || texture.height != original.height))
            {
                int width = texture.width;
                int height = texture.height;
                UnityEngine.Object.Destroy(texture);
                throw new InvalidDataException($"Localized image is {width}x{height}, but {textureName} is {original.width}x{original.height}.");
            }

            if (original != null)
            {
                texture.filterMode = original.filterMode;
                texture.wrapMode = original.wrapMode;
                texture.anisoLevel = original.anisoLevel;
            }
            textures.Add(textureName, texture);
            return texture;
        }

        private void LogOnce(string key, string message)
        {
            if (logged.Add(key))
                log.LogInfo(message);
        }

        public void Dispose()
        {
            originalSprites.Clear();
            originalOverrides.Clear();
            originalRawTextures.Clear();
            originalMaterialTextures.Clear();
            foreach (Sprite sprite in replacements.Values)
                if (sprite != null)
                    UnityEngine.Object.Destroy(sprite);
            replacements.Clear();
            replacementSources.Clear();
            foreach (Texture2D texture in textures.Values)
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
            textures.Clear();
            logged.Clear();
        }
    }

    [HarmonyPatch(typeof(Image), "set_sprite")]
    internal static class RussianSpriteAssignmentPatch
    {
        private static void Prefix(Image __instance, ref Sprite value) =>
            Plugin.Instance?.SpriteOverrides?.BeforeSpriteAssignment(__instance, ref value, false);

        private static void Postfix(Image __instance) =>
            RussianTitleLogo.ResizeDesktopLogo(__instance.rectTransform, __instance.overrideSprite?.texture);
    }

    [HarmonyPatch(typeof(Image), "set_overrideSprite")]
    internal static class RussianOverrideSpriteAssignmentPatch
    {
        private static void Prefix(Image __instance, ref Sprite value) =>
            Plugin.Instance?.SpriteOverrides?.BeforeSpriteAssignment(__instance, ref value, true);

        private static void Postfix(Image __instance) =>
            RussianTitleLogo.ResizeDesktopLogo(__instance.rectTransform, __instance.overrideSprite?.texture);
    }

    [HarmonyPatch(typeof(Image), "OnEnable")]
    internal static class RussianSpriteEnablePatch
    {
        private static void Postfix(Image __instance) => Plugin.Instance?.SpriteOverrides?.OnImageEnabled(__instance);
    }

    [HarmonyPatch(typeof(RawImage), "set_texture")]
    internal static class RussianRawImageTextureAssignmentPatch
    {
        private static void Prefix(RawImage __instance, ref Texture value) =>
            Plugin.Instance?.SpriteOverrides?.BeforeTextureAssignment(__instance, ref value);

        private static void Postfix(RawImage __instance) =>
            RussianTitleLogo.ResizeDesktopLogo(__instance.rectTransform, __instance.texture);
    }
}
