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

        bool bPDOEnabled = true;
        Dictionary<Ped, PedClass> dPedMap = new Dictionary<Ped, PedClass>();
        Dictionary<string, string> dPDOIni;
        List<Ped> lPedsToRemove = new List<Ped>();
        int iIntervalValue = 250, iUpperHealthThreshold = -45, iLowerHealthThreshold = -80, iMaxCombatDeaths = 10, iMaxFireDeaths = 17, iLoopsDone = 0, iClearDictAfterLoopsDone = 100, iClearingsDone = 0;
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
                }
            }

            Interval = iIntervalValue;
            BindKey(Keys.F9, new KeyPressDelegate(TogglePDO));
            this.Tick += new EventHandler(this.PedDamageOverhaulIV_Tick);
        }

        private void PedDamageOverhaulIV_Tick(object sender, EventArgs e)
        {
            if (bPDOEnabled)
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

        private void TogglePDO()
        {
            bPDOEnabled = !bPDOEnabled;
            if (bPDOEnabled)
            {
                Game.DisplayText("PDO enabled.");
            }
            else
            {
                Game.DisplayText("PDO disabled!");
            }
        }
    }
}
