﻿using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.IO;
using UnityEngine;
using RWCustom;

/* Author: Deltatime
/* Allows for the creation of configurable arena dividers through text files
/* To add an arenapack, add a text file with the names of the arenas on every line in Levels/Packs (works with CRS too!)
/* Also has values such as isCRSActive which is true when CRS is found and has CustomRegions.CustomRegions.Mod.CustomWorldMod.activatedPacks */

namespace RegionKit {
    public class CustomArenaDivisions {
        #region hooksAndLogs

        public static bool IsCRSactive { get; private set; } //Whenever the Assembly "CustomRegions" is in the enviroment and the required things from it can be called. DO NOT EDIT THIS VALUE.
        public static bool LateHooksApplied { get; private set; } //Whenever things are hooked in rainworld_start. DO NOT EDIT THIS VALUE.

        public static void Patch() {
            On.RainWorld.Start += RainWorld_Start;
            On.MultiplayerUnlocks.LevelListSortNumber += MultiplayerUnlocks_LevelListSortNumber;
        }

        public static void LatePatch() {
            if (IsCRSactive) {
                On.Menu.MultiplayerMenu.ctor += MultiplayerMenu_Ctor_CRS;
            } else {
                On.Menu.MultiplayerMenu.ctor += MultiplayerMenu_Ctor_NoCRS;
            }
            LateHooksApplied = true;
        }

        public static void Disable() {
            On.MultiplayerUnlocks.LevelListSortNumber -= MultiplayerUnlocks_LevelListSortNumber;
            On.RainWorld.Start -= RainWorld_Start;
            if (LateHooksApplied) {
                if (IsCRSactive) {
                    On.Menu.MultiplayerMenu.ctor -= MultiplayerMenu_Ctor_CRS;
                } else {
                    On.Menu.MultiplayerMenu.ctor -= MultiplayerMenu_Ctor_NoCRS;
                }
            }
        }

        public static void LogError(string message) {
            Debug.Log("[ERROR] Region-Kit:CustomArenaDivisions - " + message);
        }

        public static void Log(string message) {
            Debug.Log("Region-Kit:CustomArenaDivisions - " + message);
        }
        #endregion hooksAndLogs

        #region hookDefinitions
        /* Alters the return value if orig returns 0 and levelname maches a valid arenapack */
        public static int MultiplayerUnlocks_LevelListSortNumber(On.MultiplayerUnlocks.orig_LevelListSortNumber orig, MultiplayerUnlocks self, string levelName) {
            int unlockBatch = orig(self, levelName);
            if (unlockBatch != 0) {
                return unlockBatch;
            }
            unlockBatch = GetCustomLevelPackIndex(levelName, self);
            return unlockBatch;
        }
        /* Remove lines marked with DEBUG when they are no longer needed */
        public static void MultiplayerMenu_Ctor_CRS(On.Menu.MultiplayerMenu.orig_ctor orig, Menu.MultiplayerMenu self, ProcessManager manager) {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch(); //DEBUG
            watch.Start(); //DEBUG
            string path = Custom.RootFolderDirectory() + "Levels" + Path.DirectorySeparatorChar + "Packs" + Path.DirectorySeparatorChar;
            int packIndex = 1;
            packIndex = LoadArenaPacksFromDirectory(path, packIndex);
            for (int crLoop = 0; crLoop < CustomRegions.Mod.CustomWorldMod.activatedPacks.Count; ++crLoop) {
                path = Custom.RootFolderDirectory() + "Mods" + Path.DirectorySeparatorChar + "CustomResources" + Path.DirectorySeparatorChar + CustomRegions.Mod.CustomWorldMod.activatedPacks.Values.ElementAt(crLoop) + Path.DirectorySeparatorChar + "Levels" + Path.DirectorySeparatorChar + "Packs" + Path.DirectorySeparatorChar;
                packIndex = LoadArenaPacksFromDirectory(path, packIndex);
            }
            orig(self, manager);
            watch.Stop(); //DEBUG
            Log($"Finished initializing multiplayerMenu (CRS) in {watch.ElapsedMilliseconds}ms"); //DEBUG
        }
        /* Remove lines marked with DEBUG when they are no longer needed */
        public static void MultiplayerMenu_Ctor_NoCRS(On.Menu.MultiplayerMenu.orig_ctor orig, Menu.MultiplayerMenu self, ProcessManager manager) {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch(); //DEBUG
            watch.Start(); //DEBUG
            LoadArenaPacksFromDirectory(Custom.RootFolderDirectory() + "Levels" + Path.DirectorySeparatorChar + "Packs" + Path.DirectorySeparatorChar, 1);
            orig(self, manager);
            watch.Stop(); //DEBUG
            Log($"Finished initializing multiplayerMenu (No CRS) in {watch.ElapsedMilliseconds}ms"); //DEBUG
        }
        /* Checks for CRS and sets isCRSactive to the result, sets the static variables for this class.
        /* Remove lines marked with DEBUG when they are no longer needed */
        public static void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self) {
            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch(); //DEBUG
            watch.Start(); //DEBUG
            Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly a in assemblies) {
                if ((a.GetName().Name ?? string.Empty) == "CustomRegions" && a.GetName().Version?.Minor >= 8) {
                    IsCRSactive = true;
                    if (a.GetType("CustomRegions.Mod.CustomWorldMod")?.GetMember("activatedPacks") == null) {
                        LogError("Could not find member in type : CustomRegions.Mod.CustomWorldMod..activatedPacks");
                        IsCRSactive = false;
                    }
                }
            }
            if (!IsCRSactive) {
                Log("Failed to find CRS Assembly");
            }
            LatePatch();
            customArenaPacks = new Dictionary<string, int>();
            watch.Stop(); //DEBUG
            Log("Finished seeking CRS assembly in : " + watch.ElapsedMilliseconds + "ms"); //DEBUG
            orig.Invoke(self);
        }
        #endregion hookDefinitions

        public static Dictionary<string, int> customArenaPacks;

        /*Gets the pack index of a level if that level is included in an arenapack, returns 0 if the arena is not included in any packs.*/
        public static int GetCustomLevelPackIndex(string levelName, MultiplayerUnlocks multiplayerUnlocks) {
            if (multiplayerUnlocks == null || levelName == null) {
                LogError("GetCustomLevelPackIndex - One of the parameters is null");
            }
            if (customArenaPacks == null) {
                LogError("CustomArenaPacks dictionary is null");
                return 0;
            }
            if (customArenaPacks.TryGetValue(levelName, out int value)) {
                return value + multiplayerUnlocks.unlockedBatches.Count;
            }
            return 0;
        }

        /* PacksDirectory must have a path separator at the end
        /* Returns the incremented startingPackIndex
        /* The first PackIndex should be 1, since it is added to the amount of packs in the base game.
        /* Remove lines marked with DEBUG when they are no longer needed */
        public static int LoadArenaPacksFromDirectory(string packsDirectory, int startingPackIndex) {
            if (packsDirectory == null) {
                LogError("LoadArenaPacksFromDirectory - string parameter is null");
                return startingPackIndex;
            }
            if (customArenaPacks == null) {
                LogError("CustomArenaPacks dictionary is null");
                return startingPackIndex;
            }
            int packIndex = startingPackIndex;
            Log("Packs Directory: " + packsDirectory);
            if (Directory.Exists(packsDirectory)) {
                string[] files = Directory.GetFiles(packsDirectory);
                if (files != null) {
                    for (int i = 0; i < files.Length; ++i) {
                        if (File.Exists(files[i])) {
                            string[] fileLines = File.ReadAllLines(files[i]);
                            if (fileLines != null && fileLines.Length > 0) {
                                for (int l = 0; l < fileLines.Length; ++l) {
                                    if (!customArenaPacks.ContainsKey(fileLines[l])) {
                                        customArenaPacks.Add(fileLines[l], packIndex);
                                        Log($"Added arena {fileLines[l]} to pack {packIndex}."); //DEBUG
                                    } else {
                                        LogError($"Arena is already being used in a pack! Pack {files[i]} will not have the arena [{fileLines[l]}]");
                                    }
                                }
                                ++packIndex;
                            } else {
                                Log($"File {files[i]} does not have any lines");
                            }
                        } else {
                            LogError("Could not read file: " + files[i]);
                        }
                    }
                }
            } else {
                return packIndex;
            }
            return packIndex;
        }

    }
}