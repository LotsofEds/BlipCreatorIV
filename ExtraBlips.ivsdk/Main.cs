using CCL.GTAIV;
using IVSDKDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using IVSDKDotNet;
using static IVSDKDotNet.Native.Natives;
using CCL;
using CCL.GTAIV;
using IVSDKDotNet.Enums;
using System.Runtime;
using System.IO;
using System.Drawing;
using static System.Windows.Forms.AxHost;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace ExtraBlips.ivsdk
{
    public class Main : Script
    {
        public static IVPed PlayerPed { get; set; }
        public static int PlayerIndex { get; set; }
        public static int PlayerHandle { get; set; }
        public static Vector3 PlayerPos { get; set; }

        private static readonly List<string> fileNames = new List<string>();
        private static readonly List<Vector3> blipLocations = new List<Vector3>();
        private static List<NativeBlip> newBlips = new List<NativeBlip>();
        private static readonly List<string> locNames = new List<string>();
        private static List<bool> blipUnlock = new List<bool>();

        private static List<int> blipIcon = new List<int>();
        private static List<string> blipNames = new List<string>();
        private static List<int> blipColors = new List<int>();
        private static List<float> blipScale = new List<float>();
        private static List<int> blipAlpha = new List<int>();
        private static List<int> blipDisplay = new List<int>();
        private static List<uint> blipRoom = new List<uint>();

        private static List<bool> blipIsland = new List<bool>();
        private static List<bool> blipInterior = new List<bool>();
        private static List<bool> blipNear = new List<bool>();

        private static int pKey;
        private static uint pRoom;

        private static SettingsFile settings;
        private static bool hasBlipped;
        private static bool debug;
        public Main()
        {
            Uninitialize += Main_Uninitialize;
            Initialized += Main_Initialized;
            IngameStartup += Main_IngameStartup;
            Tick += Main_Tick;
        }

        private void Main_IngameStartup(object sender, EventArgs e)
        {
            hasBlipped = false;
            newBlips.Clear();
        }

        private void Main_Uninitialize(object sender, EventArgs e)
        {
            if (newBlips != null && newBlips.Count > 0)
            {
                foreach (var blip in newBlips)
                {
                    blip?.Delete();
                }
                newBlips.Clear();
            }
        }
        private void Main_Initialized(object sender, EventArgs e)
        {
            hasBlipped = false;
            fileNames.Clear();
            newBlips.Clear();

            locNames.Clear();
            blipLocations.Clear();
            blipIcon.Clear();
            blipNames.Clear();
            blipColors.Clear();
            blipScale.Clear();
            blipAlpha.Clear();
            blipDisplay.Clear();
            blipRoom.Clear();

            blipIsland.Clear();
            blipInterior.Clear();
            blipNear.Clear();

            blipUnlock.Clear();

            string filePath = string.Format("{0}\\IVSDKDotNet\\scripts\\ExtraBlips\\", IVGame.GameStartupPath); ;
            string[] configFiles = System.IO.Directory.GetFiles(filePath);
            foreach (string fileName in configFiles)
            {
                fileNames.Add(fileName);
                settings = new SettingsFile(fileName);
                settings.Load();

                string locName = settings.GetValue("CONFIG", "ID", "");
                Vector3 loc = settings.GetVector3("CONFIG", "Location", Vector3.Zero);
                int icon = settings.GetInteger("CONFIG", "Icon", 0);
                string name = settings.GetValue("CONFIG", "Name", "");
                int color = settings.GetInteger("CONFIG", "Color", 0);
                float scale = settings.GetFloat("CONFIG", "Scale", 1.0f);
                int alpha = settings.GetInteger("CONFIG", "Alpha", 0);
                int disp = settings.GetInteger("CONFIG", "Display", 0);
                uint room = settings.GetUInteger("CONFIG", "InteriorModel", 0);

                bool island = settings.GetBoolean("CONFIG", "LockedToIsland", false);
                bool interior = settings.GetBoolean("CONFIG", "LockedToInterior", false);
                bool near = settings.GetBoolean("CONFIG", "ShowOnRadarOnlyWhenNear", false);

                locNames.Add(locName);
                blipLocations.Add(loc);
                blipIcon.Add(icon);
                blipNames.Add(name);
                blipColors.Add(color);
                blipScale.Add(scale);
                blipAlpha.Add(alpha);
                blipDisplay.Add(disp);
                blipRoom.Add(room);

                blipIsland.Add(island);
                blipInterior.Add(interior);
                blipNear.Add(near);
            }
            LoadINI(Settings);
        }
        private void LoadINI(SettingsFile setting)
        {
            setting.Load();
            debug = setting.GetBoolean("MAIN", "Debug", false);

        }
        private bool isUnlocked(SettingsFile setting, string name)
        {
            if (!setting.DoesKeyExists(IVGenericGameStorage.ValidSaveName, name + "Unlocked"))
            {
                setting.AddKeyToSection(IVGenericGameStorage.ValidSaveName, name + "Unlocked");
                setting.SetBoolean(IVGenericGameStorage.ValidSaveName, name + "Unlocked", false);
            }
            setting.Save();
            setting.Load();

            if (setting.GetBoolean(IVGenericGameStorage.ValidSaveName, name + "Unlocked", false))
                return true;
            else
                return false;

        }
        private void SaveINI(SettingsFile setting, string name, bool locked)
        {
            if (!setting.DoesKeyExists(IVGenericGameStorage.ValidSaveName, name + "Unlocked"))
                setting.AddKeyToSection(IVGenericGameStorage.ValidSaveName, name + "Unlocked");
            setting.SetBoolean(IVGenericGameStorage.ValidSaveName, name + "Unlocked", locked);
            setting.Save();
        }
        private void Main_Tick(object sender, EventArgs e)
        {
            PlayerPed = IVPed.FromUIntPtr(IVPlayerInfo.FindThePlayerPed());
            PlayerHandle = PlayerPed.GetHandle();
            PlayerPos = PlayerPed.Matrix.Pos;

            if (!hasBlipped)
            {
                hasBlipped = true;
                blipUnlock.Clear();
                foreach (Vector3 location in blipLocations)
                {
                    var blip = NativeBlip.AddBlip(location);

                    bool locked = isUnlocked(Settings, locNames[blipLocations.IndexOf(location)]);
                    blipUnlock.Add(locked);

                    if (blipNear[blipLocations.IndexOf(location)])
                        blip.ShowOnlyWhenNear = true;
                    else
                        blip.ShowOnlyWhenNear = false;

                    blip.Icon = (BlipIcon)blipIcon[blipLocations.IndexOf(location)];
                    blip.Name = blipNames[blipLocations.IndexOf(location)];
                    if (blipScale[blipLocations.IndexOf(location)] != -1)
                        blip.Scale = blipScale[blipLocations.IndexOf(location)];
                    if (blipColors[blipLocations.IndexOf(location)] != -1)
                        blip.Color = (eBlipColor)blipColors[blipLocations.IndexOf(location)];
                    if (blipAlpha[blipLocations.IndexOf(location)] != -1)
                        blip.Transparency = blipAlpha[blipLocations.IndexOf(location)];

                    blip.Display = eBlipDisplay.BLIP_DISPLAY_HIDDEN;

                    newBlips.Add(blip);
                }
            }

            if (debug)
            {
                GET_KEY_FOR_CHAR_IN_ROOM(PlayerHandle, out pRoom);
                IVGame.ShowSubtitleMessage("InteriorModel:" + pRoom.ToString() + "   SaveGame:" + IVGenericGameStorage.ValidSaveName.ToString());
            }

            foreach (NativeBlip blip in newBlips)
            {
                if (blip.Display == eBlipDisplay.BLIP_DISPLAY_HIDDEN)
                {
                    if (blipUnlock[newBlips.IndexOf(blip)])
                        blip.Display = (eBlipDisplay)blipDisplay[newBlips.IndexOf(blip)];

                    else
                    {
                        if (!blipInterior[newBlips.IndexOf(blip)])
                        {
                            if (!blipIsland[newBlips.IndexOf(blip)])
                                blipUnlock[newBlips.IndexOf(blip)] = true;

                            else
                            {
                                uint mapArea = GET_MAP_AREA_FROM_COORDS(blip.Position);
                                int mapUnlock = GET_INT_STAT(363);
                                if (mapArea <= mapUnlock)
                                    blipUnlock[newBlips.IndexOf(blip)] = true;
                            }
                        }
                        else
                        {
                            if (!blipIsland[newBlips.IndexOf(blip)])
                            {
                                GET_INTERIOR_AT_COORDS(blip.Position.X, blip.Position.Y, blip.Position.Z, out int iKey);
                                GET_INTERIOR_AT_COORDS(PlayerPos.X, PlayerPos.Y, PlayerPos.Z, out pKey);
                                GET_KEY_FOR_CHAR_IN_ROOM(PlayerHandle, out pRoom);

                                if (pRoom == blipRoom[newBlips.IndexOf(blip)] && iKey == pKey)
                                    blipUnlock[newBlips.IndexOf(blip)] = true;
                            }

                            else
                            {
                                uint mapArea = GET_MAP_AREA_FROM_COORDS(blip.Position);
                                int mapUnlock = GET_INT_STAT(363);
                                if (mapArea <= mapUnlock)
                                {
                                    GET_INTERIOR_AT_COORDS(blip.Position.X, blip.Position.Y, blip.Position.Z, out int iKey);
                                    GET_INTERIOR_AT_COORDS(PlayerPos.X, PlayerPos.Y, PlayerPos.Z, out pKey);
                                    GET_KEY_FOR_CHAR_IN_ROOM(PlayerHandle, out pRoom);

                                    if (pRoom == blipRoom[newBlips.IndexOf(blip)] && iKey == pKey)
                                        blipUnlock[newBlips.IndexOf(blip)] = true;
                                }
                            }
                        }
                    }
                }
                if (DID_SAVE_COMPLETE_SUCCESSFULLY() && GET_IS_DISPLAYINGSAVEMESSAGE())
                    SaveINI(Settings, locNames[newBlips.IndexOf(blip)], blipUnlock[newBlips.IndexOf(blip)]);
            }
        }
    }
}
