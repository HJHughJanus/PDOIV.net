using GTA;
using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Reflection;

namespace PDOIV.net
{
    class PedClass
    {
        public int iCombatDeathCounter, iFireDeathCounter;
        public bool bAllowedToDie;
    }
    public class PedDamageOverhaulIV : Script
    {
        public bool SetKey(int toKey, out Keys key)
        {
            switch (toKey)
            {
                case 1:
                    key = Keys.F1;
                    return true;
                case 2:
                    key = Keys.F2;
                    return true;
                case 3:
                    key = Keys.F3;
                    return true;
                case 4:
                    key = Keys.F4;
                    return true;
                case 5:
                    key = Keys.F5;
                    return true;
                case 6:
                    key = Keys.F6;
                    return true;
                case 7:
                    key = Keys.F7;
                    return true;
                case 8:
                    key = Keys.F8;
                    return true;
                case 9:
                    key = Keys.F9;
                    return true;
                case 10:
                    key = Keys.F10;
                    return true;
                case 11:
                    key = Keys.F11;
                    return true;
                case 12:
                    key = Keys.F12;
                    return true;
                default:
                    key = Keys.None;
                    return false;
            }
        }
        public bool GetIniFile(string path, out Dictionary<string, string> dict)
        {
            if (File.Exists(path))
            {
                dict = new Dictionary<string, string>();
                foreach (string line in File.ReadLines(path))
                {
                    if (!(line[0].Equals(";") || line[0].Equals("[") || line[0].Equals(" ")))
                    {
                        var keyValue = line.Split(new[] { '=' }, 2);                        
                        if (keyValue.Length == 2)
                        {
                            dict.Add(keyValue[0], keyValue[1]);
                        }
                    }
                }
                return true;
            }
            else
            {
                dict = new Dictionary<string, string>();
                return false;
            }
        }

        bool bPDOEnabled = true, bPDODisabledDuringMissions = false, bIniFound = false, bShowNPCInfo = false, bLastTarget_IsAlive, bLastTarget_IsInjured, bLastTarget_IsRagdoll;
        Dictionary<Ped, PedClass> dPedMap = new Dictionary<Ped, PedClass>();
        Dictionary<string, string> dPDOIni;
        List<Ped> lPedsToRemove = new List<Ped>();
        int iIntervalValue = 250, iUpperHealthThreshold = -45, iLowerHealthThreshold = -80, iMaxCombatDeaths = 10, iMaxFireDeaths = 17, iLoopsDone = 0, iClearDictAfterLoopsDone = 100, iClearingsDone = 0, iLastTarget_Health;
        Keys kPDOToggleKey = Keys.F9, kShowNPCInfoToggleKey = Keys.F8;
        Ped pLastTarget;
        string sTempIniValue;

        public PedDamageOverhaulIV()
        {
            var workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Substring(6);
            var IniFileName = "PedDamageOverhaulIV.NET.ini";
            var IniPath = $@"{workingDirectory}\{IniFileName}";
            if (File.Exists(IniPath))
            {
                if (GetIniFile(IniPath, out dPDOIni))
                {
                    bIniFound = true;
                    if (dPDOIni.TryGetValue("bEnablePDO", out sTempIniValue))
                    {
                        bPDOEnabled = Boolean.Parse(sTempIniValue);
                    }
                    if (dPDOIni.TryGetValue("iUpperHealthThreshold", out sTempIniValue))
                    {
                        iUpperHealthThreshold = Int32.Parse(sTempIniValue);
                    }
                    if (dPDOIni.TryGetValue("iLowerHealthThreshold", out sTempIniValue))
                    {
                        iLowerHealthThreshold = Int32.Parse(sTempIniValue);
                    }
                    if (dPDOIni.TryGetValue("iMaxCombatHealthResets", out sTempIniValue))
                    {
                        iMaxCombatDeaths = Int32.Parse(sTempIniValue);
                    }
                    if (dPDOIni.TryGetValue("iMaxFireHealthResets", out sTempIniValue))
                    {
                        iMaxFireDeaths = Int32.Parse(sTempIniValue);
                    }
                    if (dPDOIni.TryGetValue("iTickInterval", out sTempIniValue))
                    {
                        iIntervalValue = Int32.Parse(sTempIniValue);
                    }
                    if (dPDOIni.TryGetValue("iClearNPCsAfterTickIntervals", out sTempIniValue))
                    {
                        iClearDictAfterLoopsDone = Int32.Parse(sTempIniValue);
                    }
                    if (dPDOIni.TryGetValue("bDisablePDODuringMissions", out sTempIniValue))
                    {
                        bPDODisabledDuringMissions = Boolean.Parse(sTempIniValue);
                    }
                    if (dPDOIni.TryGetValue("bShowNPCInfo", out sTempIniValue))
                    {
                        bShowNPCInfo = Boolean.Parse(sTempIniValue);
                    }
                    if (dPDOIni.TryGetValue("iShowNPCInfoToggleKey", out sTempIniValue))
                    {
                        int tempKey = Int32.Parse(sTempIniValue);
                        Keys key;
                        if (SetKey(tempKey, out key))
                        {
                            kShowNPCInfoToggleKey = key;
                        }
                    }
                    if (dPDOIni.TryGetValue("iPDOToggleKey", out sTempIniValue))
                    {
                        int tempKey = Int32.Parse(sTempIniValue);
                        Keys key;
                        if (SetKey(tempKey, out key))
                        {
                            kPDOToggleKey = key;
                        }
                    }
                }
            }

            Interval = iIntervalValue;
            BindKey(kPDOToggleKey, new KeyPressDelegate(TogglePDO));
            BindKey(kShowNPCInfoToggleKey, new KeyPressDelegate(ToggleShowNPCInfo));
            this.Tick += new EventHandler(this.PedDamageOverhaulIV_Tick);
        }

        private void PedDamageOverhaulIV_Tick(object sender, EventArgs e)
        {
            bool bGreenLight = true;
            
            if (bPDODisabledDuringMissions)
            {
                if (Player.isOnMission)
                {
                    bGreenLight = false;
                }
            }

            if (bPDOEnabled && bGreenLight)
            {
                int iArraySize = 1024;
                Ped[] aAllPeds = World.GetPeds(Player.Character.Position, 5000f, iArraySize);

                for (int i = 0; i < aAllPeds.Length; i++)
                {
                    if (!dPedMap.ContainsKey(aAllPeds[i]))
                    {
                        PedClass p = new PedClass();
                        p.bAllowedToDie = false;
                        p.iCombatDeathCounter = 0;
                        p.iFireDeathCounter = 0;
                        dPedMap.Add(aAllPeds[i], p);
                    }
                }

                foreach (KeyValuePair<Ped, PedClass> ped in dPedMap)
                {
                    if (ped.Key.Exists())
                    {
                        if (ped.Key.isAlive && ped.Key != Player.Character)
                        {
                            if (!ped.Value.bAllowedToDie)
                            {
                                if (ped.Key.isOnFire)
                                {
                                    if (ped.Key.Health <= iLowerHealthThreshold)
                                    {
                                        ped.Key.Health = iUpperHealthThreshold;
                                        ped.Key.isOnFire = true;
                                        ped.Value.iFireDeathCounter++;
                                        if (ped.Value.iFireDeathCounter >= iMaxFireDeaths)
                                        {
                                            ped.Value.bAllowedToDie = true;
                                        }
                                    }
                                }
                                else
                                {
                                    if (ped.Key.Health <= iUpperHealthThreshold)
                                    {
                                        if (ped.Key.Health <= iLowerHealthThreshold)
                                        {
                                            ped.Key.Health = iUpperHealthThreshold;
                                            ped.Value.iCombatDeathCounter++;
                                            if (ped.Value.iCombatDeathCounter >= iMaxCombatDeaths)
                                            {
                                                ped.Value.bAllowedToDie = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        lPedsToRemove.Add(ped.Key);
                    }
                }

                if (bShowNPCInfo)
                {
                    if (Player.GetTargetedPed() != null)
                    {
                        pLastTarget = Player.GetTargetedPed();
                    }
                    if (pLastTarget != null)
                    {
                        iLastTarget_Health = pLastTarget.Health;
                        bLastTarget_IsAlive = pLastTarget.isAlive;
                        bLastTarget_IsInjured = pLastTarget.isInjured;
                        bLastTarget_IsRagdoll = pLastTarget.isRagdoll;
                    }
                    Game.DisplayText("NPC Health: " + iLastTarget_Health + "\nNPC IsAlive: " + bLastTarget_IsAlive + "\nNPC IsInjured: " + bLastTarget_IsInjured + "\nNPC IsRagdoll: " + bLastTarget_IsRagdoll);
                }

                iLoopsDone++;

                if (iLoopsDone % iClearDictAfterLoopsDone == 0)
                {
                    foreach (Ped ped in lPedsToRemove)
                    {
                        dPedMap.Remove(ped);
                    }
                    iClearingsDone++;
                    lPedsToRemove.Clear();
                }
            }
        }

        private void ToggleShowNPCInfo()
        {
            bShowNPCInfo = !bShowNPCInfo;
        }

        private void TogglePDO()
        {
            bPDOEnabled = !bPDOEnabled;
            if (bPDOEnabled)
            {
                if (bIniFound)
                {
                    Game.DisplayText("PDO enabled.\nini-File found.");
                }
                else
                {
                    Game.DisplayText("PDO enabled.\nini-File not found. Default values loaded.");
                }
            }
            else
            {
                Game.DisplayText("PDO disabled!");
            }
        }
    }
}
