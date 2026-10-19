using CCL.GTAIV;
using IVSDKDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static IVSDKDotNet.Native.Natives;
using CCL;
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

        private static List<BlipInfo> blipList = new List<BlipInfo>();

        private static bool hasBlipped;
        private static bool debug;
        private static uint gTimer;
        private static uint fTimer;
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
        }
        private void Main_Uninitialize(object sender, EventArgs e)
        {
            for (int i = 0; i < blipList.Count; i++)
            {
                if (DOES_BLIP_EXIST(blipList[i].BlipID))
                    REMOVE_BLIP(blipList[i].BlipID);
            }
        }
        private void Main_Initialized(object sender, EventArgs e)
        {
            hasBlipped = false;

            blipList.Clear();

            string filePath = string.Format("{0}\\IVSDKDotNet\\scripts\\ExtraBlips\\", IVGame.GameStartupPath); ;
            string[] configFiles = System.IO.Directory.GetFiles(filePath);
            foreach (string fileName in configFiles)
            {
                SettingsFile settings = new SettingsFile(fileName);
                settings.Load();

                BlipInfo newBlip = new BlipInfo();

                newBlip.LocID = settings.GetValue("CONFIG", "ID", "");
                newBlip.Location = settings.GetVector3("CONFIG", "Location", Vector3.Zero);
                newBlip.Icon = settings.GetInteger("CONFIG", "Icon", 0);
                newBlip.Name = settings.GetValue("CONFIG", "Name", "");
                newBlip.Color = settings.GetInteger("CONFIG", "Color", 0);
                newBlip.Scale = settings.GetFloat("CONFIG", "Scale", 1.0f);
                newBlip.Alpha = settings.GetInteger("CONFIG", "Alpha", 0);
                newBlip.Display = settings.GetInteger("CONFIG", "Display", 0);
                newBlip.Room = settings.GetUInteger("CONFIG", "InteriorModel", 0);

                newBlip.Island = settings.GetBoolean("CONFIG", "LockedToIsland", false);
                newBlip.Interior = settings.GetBoolean("CONFIG", "LockedToInterior", false);
                newBlip.Near = settings.GetBoolean("CONFIG", "ShowOnRadarOnlyWhenNear", false);
                newBlip.MissionHide = settings.GetBoolean("CONFIG", "HideWhenOnAMission", false);
                newBlip.ShowDist = settings.GetFloat("CONFIG", "AppearDistance", -1);
                newBlip.Distance2D = settings.GetBoolean("CONFIG", "IsDistance2D", false);

                newBlip.IVStat = settings.GetInteger("CONFIG", "StatToCheckIV", -1);
                newBlip.IVStatAmt = settings.GetFloat("CONFIG", "StatAmountIV", -1);
                newBlip.TLADStat = settings.GetInteger("CONFIG", "StatToCheckTLAD", -1);
                newBlip.TLADStatAmt = settings.GetFloat("CONFIG", "StatAmountTLAD", -1);
                newBlip.TBOGTStat = settings.GetInteger("CONFIG", "StatToCheckTBOGT", -1);
                newBlip.TBOGTStatAmt = settings.GetFloat("CONFIG", "StatAmountTBOGT", -1);

                newBlip.Unlocked = false;
                newBlip.BlipID = -1;

                blipList.Add(newBlip);
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
            PlayerIndex = (int)GET_PLAYER_ID();
            PlayerPos = PlayerPed.Matrix.Pos;

            if (PlayerPed == null)
                return;

            GET_GAME_TIMER(out gTimer);

            CreateBlips();
            ProcessDebug();
            ProcessBlips();
        }
        private void CreateBlips()
        {
            if (hasBlipped)
                return;

            fTimer = 0;

            for (int i = 0; i < blipList.Count; i++)
            {
                if (DOES_BLIP_EXIST(blipList[i].BlipID))
                    REMOVE_BLIP(blipList[i].BlipID);

                var blip = NativeBlip.AddBlip(blipList[i].Location);

                bool locked = isUnlocked(Settings, blipList[i].LocID);

                if (blipList[i].Near)
                    blip.ShowOnlyWhenNear = true;
                else
                    blip.ShowOnlyWhenNear = false;

                blip.Icon = (BlipIcon)blipList[i].Icon;
                blip.Name = blipList[i].Name;
                if (blipList[i].Scale != -1)
                    blip.Scale = blipList[i].Scale;
                if (blipList[i].Color != -1)
                    blip.Color = (eBlipColor)blipList[i].Color;
                if (blipList[i].Alpha != -1)
                    blip.Transparency = blipList[i].Alpha;

                blip.Display = eBlipDisplay.BLIP_DISPLAY_HIDDEN;

                blipList[i].BlipID = blip.Handle;
            }
            hasBlipped = true;
        }
        private void ProcessDebug()
        {
            if (!debug)
                return;

            GET_KEY_FOR_CHAR_IN_ROOM(PlayerHandle, out uint pRoom);
            IVGame.ShowSubtitleMessage("InteriorModel:" + pRoom.ToString() + "   SaveGame:" + IVGenericGameStorage.ValidSaveName.ToString());
        }
        private void ProcessBlips()
        {
            for (int i = 0; i < blipList.Count; i++)
            {
                if (blipList[i].Unlocked)
                {
                    if (ProcessDistance(i) && gTimer >= fTimer + 1500 && (!blipList[i].MissionHide || CAN_PLAYER_START_MISSION(Main.PlayerIndex)))
                        CHANGE_BLIP_DISPLAY(blipList[i].BlipID, (uint)blipList[i].Display);
                    else if (GET_BLIP_INFO_ID_DISPLAY(blipList[i].BlipID) != (uint)eBlipDisplay.BLIP_DISPLAY_HIDDEN)
                        CHANGE_BLIP_DISPLAY(blipList[i].BlipID, (uint)eBlipDisplay.BLIP_DISPLAY_HIDDEN);
                }
                else
                {
                    if (ProcessIslandBlip(i) && ProcessInteriorBlip(i) && ProcessMissionLock(i))
                        blipList[i].Unlocked = true;
                }
                if (DID_SAVE_COMPLETE_SUCCESSFULLY() && GET_IS_DISPLAYINGSAVEMESSAGE())
                    SaveINI(Settings, blipList[i].LocID, blipList[i].Unlocked);
            }
        }
        private bool ProcessMissionLock(int i)
        {
            uint currEp = GET_CURRENT_EPISODE();

            if (currEp == 0 && blipList[i].IVStat < 0)
                return true;
            else if (currEp == 1 && blipList[i].TLADStat < 0)
                return true;
            else if (currEp == 2 && blipList[i].TBOGTStat < 0)
                return true;

            else
            {
                if (currEp == 0 && (GET_FLOAT_STAT(blipList[i].IVStat) >= blipList[i].IVStatAmt
                    || GET_INT_STAT(blipList[i].IVStat) >= blipList[i].IVStatAmt))
                    return true;
                else if (currEp == 1 && (GET_FLOAT_STAT(blipList[i].TLADStat) >= blipList[i].TLADStatAmt
                    || GET_INT_STAT(blipList[i].TLADStat) >= blipList[i].TLADStatAmt))
                    return true;
                else if (currEp == 2 && (GET_FLOAT_STAT(blipList[i].TBOGTStat) >= blipList[i].TBOGTStatAmt
                    || GET_INT_STAT(blipList[i].TBOGTStat) >= blipList[i].TBOGTStatAmt))
                    return true;
            }
            return false;
        }
        private bool ProcessIslandBlip(int i)
        {
            if (!blipList[i].Island)
                return true;

            else
            {
                uint mapArea = GET_MAP_AREA_FROM_COORDS(blipList[i].Location);
                int mapUnlock = GET_INT_STAT(363);
                if (mapArea <= mapUnlock)
                    return true;
            }
            return false;
        }
        private bool ProcessInteriorBlip(int i)
        {
            if (!blipList[i].Interior)
                return true;
            else
            {
                GET_INTERIOR_AT_COORDS(blipList[i].Location.X, blipList[i].Location.Y, blipList[i].Location.Z, out int iKey);
                GET_INTERIOR_AT_COORDS(PlayerPos.X, PlayerPos.Y, PlayerPos.Z, out int pKey);
                GET_KEY_FOR_CHAR_IN_ROOM(PlayerHandle, out uint pRoom);

                if (pRoom == blipList[i].Room && iKey == pKey)
                {
                    if (!blipList[i].Unlocked)
                        GET_GAME_TIMER(out fTimer);
                    return true;
                }
            }
            return false;
        }
        private bool ProcessDistance(int i)
        {
            if (blipList[i].ShowDist < 0)
                return true;

            else if (blipList[i].Distance2D)
            {
                if (LOCATE_CHAR_ANY_MEANS_2D(Main.PlayerHandle, blipList[i].Location.X, blipList[i].Location.Y,
                    blipList[i].ShowDist, blipList[i].ShowDist, false))
                    return true;
            }
            else
            {
                if (LOCATE_CHAR_ANY_MEANS_3D(Main.PlayerHandle, blipList[i].Location.X, blipList[i].Location.Y, blipList[i].Location.Z,
                    blipList[i].ShowDist, blipList[i].ShowDist, blipList[i].ShowDist, false))
                    return true;
            }

            return false;
        }
    }
    public class BlipInfo
    {
        public Vector3 Location {  get; set; }
        public int BlipID { get; set; }
        public bool Unlocked { get; set; }

        public string LocID { get; set; }
        public int Icon { get; set; }
        public string Name { get; set; }
        public int Color { get; set; }
        public float Scale { get; set; }
        public int Alpha { get; set; }
        public int Display { get; set; }
        public uint Room { get; set; }

        public bool Island { get; set; }
        public bool Interior { get; set; }
        public bool Near { get; set; }
        public bool MissionHide { get; set; }
        public float ShowDist { get; set; }
        public bool Distance2D { get; set; }
        public int IVStat { get; set; }
        public float IVStatAmt { get; set; }
        public int TLADStat { get; set; }
        public float TLADStatAmt { get; set; }
        public int TBOGTStat { get; set; }
        public float TBOGTStatAmt { get; set; }
        public BlipInfo() { }
    }
}
