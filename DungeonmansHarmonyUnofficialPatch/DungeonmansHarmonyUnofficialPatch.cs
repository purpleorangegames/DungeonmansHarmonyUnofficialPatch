using DungeonMans;
using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Harmony;

namespace UnofficialPatch
{
    using Extension;

    public class DungeonmansUnofficialPatch : Mod
    {
        public DungeonmansUnofficialPatch()
        {
            var harmony = HarmonyInstance.Create("net.pog.dungeonmans.mod.unnoficialpatch");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }
    
    [HarmonyPatch(typeof(ui_mouseFrame_DragAndDrop))]
    [HarmonyPatch("UpdateHotbarButtonHeldItemQuantity")]
    static class UnofficialPatch_UpdateHotbarButtonHeldItemQuantity_Patch
    {
        //Makes map always show quantity of 1 since it is unique
        static void Postfix(ui_mouseFrame_DragAndDrop __instance)
        {
            if (__instance.strHeldArchetypeOrID == "adventure_map")
            {
                __instance.iNumHeldItems = 1;
            }
        }
    }

    [HarmonyPatch(typeof(ui_mouseFrame_DragAndDrop))]
    [HarmonyPatch("OnMouseUp")]
    static class UnofficialPatch_OnMouseUp_Patch
    {
        //On the function consumables don't set the containedItem, so here we set it so the map isn't consumed when clicked on the hotbar
        static bool Prefix(ui_mouseFrame_DragAndDrop __instance)
        {
            dmItem aux = Traverse.Create(__instance).Field("theInput").GetValue<dmInput>().mouseHeldItem;
            if (aux != null)
            {
                if (aux.ArchetypeName == "adventure_map")
                {
                    __instance.containedItem = aux;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(dmGame))]
    [HarmonyPatch("CheckControllerInputForGame")]
    //private void (PlayerIndex _playerIndex, GameTime gameTime)
    static class UnofficialPatch_CheckControllerInputForGame_Patch
    {
        //Fixes when a map is activated in the hotbar using the keyboard, so it doesn't remove it from the inventory just because it is a consumable
        static bool Prefix(dmGame __instance)
        {
            if (__instance.dungeonMans.iCurrentAP > 0 && !__instance.bShowMasteriesMode && !__instance.bInventoryMode && !__instance.bSelectPowerMode)
            {
                for (int j = 0; j < 10; j++)
                {
                    if (!__instance.theInput.ExecutedActions.Contains((ActionButton)(53 + j)))
                    {
                        continue;
                    }
                    int num = j;
                    if (__instance.theInput.bCtrlIsDown)
                    {
                        num += 10;
                    }
                    else if (__instance.theInput.bShiftIsDown)
                    {
                        num += 20;
                    }
                    else if (__instance.theInput.bAltIsDown)
                    {
                        num += 30;
                    }
                    dmItem swapItem = __instance.theUIFrames.GetHeldItemFromHotbar(num);
                    if (swapItem != null)
                    {
                        if (swapItem is dmConsumable && __instance.dungeonMans.iCurrentAP >= 100 && __instance.gamePhase == GameTurnPhase.WaitForHeroAction)
                        {
                            __instance.dungeonMans.UseConsumable(swapItem as dmConsumable, __instance);
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(dmCreature))]
    [HarmonyPatch("PurgeAllRemovedItemsFromInventory")]
    static class UnofficialPatch_PurgeAllRemovedItemsFromInventory_Patch
    {
        //Clears the hotbar after using a map and before the map is actually consumed
        static void Prefix(ui_mouseFrame_DragAndDrop __instance)
        {
            for (int i = 0; i < dmUtilities.theGame.theUIFrames.hotbarArrayLength(); i++)
            {
                dmItem hotBarItem = dmUtilities.theGame.theUIFrames.GetHeldItemFromHotbar(i);
                if (hotBarItem != null && hotBarItem.ArchetypeName == "adventure_map" && hotBarItem.bRemove == true)
                {
                    dmUtilities.theGame.theUIFrames.RemoveHeldItemFromHotbar(i);
                    dmUtilities.theGame.theUIFrames.ClearHotbarSlot(i);
                }
            }
        }
    }


    [HarmonyPatch(typeof(dmAdventureMap))]
    [HarmonyPatch("GenerateGoalLocation")]
    static class UnofficialPatch_GenerateGoalLocation_Patch
    {
        //New map generation code that looks for all possible locations and randomly pick one from a list
        static void Postfix(dmAdventureMap __instance, bool __result)
        {
            dmGame theGame = dmUtilities.theGame;
            int[] array = new int[5]
            {
                45,
                50,
                58,
                69,
                79
            };
            int maxRange = array[Traverse.Create(__instance).Field("itemTier").GetValue<int>() - 1];

            List<LocValue> possibleLocations = new List<LocValue>();
            int maxDontUseThis = maxRange - 12;
            Rectangle dontUseInsideRectangle = new Rectangle(0, 0, maxDontUseThis, maxDontUseThis);
            for (int i = 0; i < maxRange; i++)
            {
                for (int j = 0; j < maxRange; j++)
                {
                    LocValue locValue = new LocValue(i, j);
                    if (!dontUseInsideRectangle.Contains(locValue.X, locValue.Y))
                    {
                        WorldTile tile = theGame.theWorld.GetTile(locValue);

                        bool newTileAlreadyUsed = false;
                        foreach (dmObject item in tile.ContainedObjects())
                        {
                            if (item is dmGateway)
                            {
                                newTileAlreadyUsed = true;
                            }
                        }

                        if ((!__instance.bForceSpecificDungeon || tile.iTerrain == TerrainType.TT_PLAINS)
                            && (tile.iTerrain == TerrainType.TT_PLAINS || tile.iTerrain == TerrainType.TT_FOREST || tile.iTerrain == TerrainType.TT_BADLANDS)
                            && !newTileAlreadyUsed)
                        {
                            possibleLocations.Add(locValue);
                        }
                    }
                }
            }

            if (possibleLocations.Count > 0)
            {
                __instance.goalLocation = possibleLocations[dmUtilities.RandomInt() % possibleLocations.Count];
                __result = true;
            }
            else
            {
                __result = false;
            }
        }
    }
    
    
    [HarmonyPatch(typeof(dmUtilities))]
    [HarmonyPatch("AdventureMap_SpawnAtLocation")]
    [HarmonyPatch(new Type[] { typeof(dmAdventureMap) })]
    static class UnofficialPatch_AdventureMap_SpawnAtLocation_Patch
    {
        //Fixes the map dungeon location when the map is used and consumed, expands the search range if necessary
        static void Prefix(dmAdventureMap mapItem)
        {
            LocValue goalLocation =  mapItem.goalLocation;
            bool tileAlreadyUsed = false;
            WorldTile checkTile = dmUtilities.theGame.theWorld.GetTile(goalLocation);
            foreach (dmObject item in checkTile.ContainedObjects())
            {
                if (item is dmGateway)
                {
                    tileAlreadyUsed = true;
                }
            }
            if (tileAlreadyUsed)
            {
                int range = 2;
                bool locationFoundCanStop = false;
                while (!locationFoundCanStop)
                {
                    List<LocValue> possibleLocations = new List<LocValue>();
                    for (int i = goalLocation.X - range; i < goalLocation.X + range; i++)
                    {
                        for (int j = goalLocation.Y - range; j < goalLocation.Y + range; j++)
                        {
                            LocValue locValue = new LocValue(i, j);
                            WorldTile tile = dmUtilities.theGame.theWorld.GetTile(locValue);

                            bool newTileAlreadyUsed = false;
                            foreach (dmObject item in tile.ContainedObjects())
                            {
                                if (item is dmGateway)
                                {
                                    newTileAlreadyUsed = true;
                                }
                            }
                            if ((tile.iTerrain == TerrainType.TT_PLAINS || tile.iTerrain == TerrainType.TT_FOREST || tile.iTerrain == TerrainType.TT_BADLANDS)
                             && !newTileAlreadyUsed)
                            {
                                possibleLocations.Add(locValue);
                            }
                        }
                    }
                    if (possibleLocations.Count > 0)
                    {
                        goalLocation = possibleLocations[dmUtilities.RandomInt() % possibleLocations.Count];
                        locationFoundCanStop = true;
                    }
                    if (possibleLocations.Count < 5)
                    {
                        range += 1;
                        if (range > 20)
                            locationFoundCanStop = true;
                    }
                }
            }
            mapItem.goalLocation = goalLocation;
        }
    }

    [HarmonyPatch(typeof(wg2DRenderer))]
    [HarmonyPatch("LoadTextures")]
    static class UnofficialPatch_LoadTexture_Patch
    {
        //Uses different icon texture with fixed The Way Home square non passive icon
        static void Postfix(wg2DRenderer __instance)
        {
            __instance.setNewPowerIconTexture(__instance.GetTextureByName("textures/power_icons_2").Texture);
        }
    }

    [HarmonyPatch(typeof(dmPerk))]
    [HarmonyPatch("BuildFromEntityDef")]
    [HarmonyPatch(new Type[] { typeof(dvDefInfo) })]
    static class UnofficialPatch_dmPerk_InitFromArchetype_Patch
    {
        //Fixes "The Way Home"("Pathfinder") perk to be active instead of passive since you use it to go back to the academy
        static void Postfix(dmPerk __instance, dvDefInfo def)
        {
            string strKey = null;
            string strValue = null;
            def.ResetCounter();
            while (def.GetKeyAndValue(ref strKey, ref strValue))
            {
                strKey = strKey.ToLowerInvariant();
                if (strValue == "Pathfinder")
                {
                    __instance.bPassive = false;
                }
                def.Advance();
            }
        }
    }

    [HarmonyPatch(typeof(dmSpecialPower))]
    [HarmonyPatch("InitFromArchetype")]
    [HarmonyPatch(new Type[] { typeof(dmGeneratorData) })]
    static class UnofficialPatch_dmSpecialPower_InitFromArchetype_Patch
    {
        //Uses different icon texture with fixed The Way Home square non passive icon
        static void Postfix(dmSpecialPower __instance, dmGeneratorData _arc)
        {
            if (__instance.strPowerName == "The Way Home" && __instance.vPowerIconIndex.X == 0)
            {
                __instance.vPowerIconIndex.X = 5;
                __instance.vPowerIconIndex.Y = 1;
            }
        }
    }
}

namespace Extension
{
    public static class Extension
    {
        public static T GetFieldValue<T>(this object obj, string name)
        {
            var field = obj.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)field?.GetValue(obj);
        }
        
        public static void SetFieldValue<T>(this object obj, string name, T newValue)
        {
            var field = obj.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(obj, newValue);
        }

        public static int hotbarArrayLength(this dmUIMouseFrameManager myInterface)
        {
            return myInterface.GetFieldValue<ui_mouseFrame_DragAndDrop[][]>("hotbarArray").Length * 10;
        }

        public static void setNewPowerIconTexture(this wg2DRenderer myInterface, Texture2D newTexture)
        {
            myInterface.SetFieldValue<Texture2D>("iconTexture",newTexture);
        }
    }
}
