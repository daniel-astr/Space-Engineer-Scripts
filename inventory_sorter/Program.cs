using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
 /*
 *
 * .
 * The script will automatically name containers for Ore, Ingots and the rest in Component containers.
 * It will also empty assemblers and refineries from their output inventories to at appropriate containers.
 * It can also eject excess gravel.
 * Further still it can list all your stocks on LCD text panels.
 * The sript assumes all inventories are connected by the conveyor system.
 * This script is inferior to Isy's Inventory Manager but still works for many applications.
 * 
 * 
 * *** USAGE ***
 * 1. create a group named "invManageGroup" without the quotation marks and add any containers, assemblers, refineries, 
 * connectors, LCD panels you want. You can change the group name to whatever you want in the Program() constructor method.
 * The script will automatically add new blocks you add to the group as long as you haven't set the script to run only once.
 * 2. If you want the script to automatically update you can either add a timer to the group and set it to run the script
 * as often you want with no arguments or you can change "automatic_update" to "true" in the Program() constructor method.
 * 3. If you add any connectors they will eject any excess gravel you have, the amount is defined by max_gravel in the Program()
 * constructor method. If you are in space remember to have a gravity generator pushing away all the excess gravel or
 * the gravel will stop the connector from ejecting if you are ejecting large amount and your settings allow loose items.
 * 4. If you wish to have LCD panels too easily see your stocks you can let the script assign what any LCD panel show
 * or you can enter "show ore", "show ingot" or "show component" without the quotation marks into custom data of the block
 * to force a panel to show a specific type of data.
 */
    partial class Program : MyGridProgram
    {


        /*
        *
        * .
        * The script will automatically name containers for Ore, Ingots and the rest in Component containers.
        * It will also empty assemblers and refineries from their output inventories to at appropriate containers.
        * It can also eject excess gravel.
        * Further still it can list all your stocks on LCD text panels.
        * The sript assumes all inventories are connected by the conveyor system.
        * This script is inferior to Isy's Inventory Manager but still works for many applications.
        *
        *
        * *** USAGE ***
        * 1. create a group named "invManageGroup" without the quotation marks and add any containers, assemblers, refineries,
        * connectors, LCD panels you want. You can change the group name to whatever you want in the Program() constructor method.
        * The script will automatically add new blocks you add to the group as long as you haven't set the script to run only once.
        * As it is now the script will only add containers as needed and will warn when you are running out of containers.
        * If you want to change this behaviour to allocate all available containers from the start change addContainerWhenNeeded to false.
        * 2. If you want the script to automatically update you can either add a timer to the group and set it to run the script
        * as often you want with no arguments or you can change "automatic_update" to "true" in the Program() constructor method.
        * 3. If you add any connectors they will eject any excess gravel you have, the amount is defined by max_gravel in the Program()
        * constructor method. If you are in space remember to have a gravity generator pushing away all the excess gravel or
        * the gravel will stop the connector from ejecting if you are ejecting large amount and your settings allow loose items.
        * 4. If you wish to have LCD panels too easily see your stocks you can let the script assign what any LCD panel show
        * or you can enter "show ore", "show ingot" or "show component" without the quotation marks into custom data of the block
        * to force a panel to show a specific type of data.
        */
        public class ItemClass //stores the amount of all items
        {
            public string itemType = "", itemSubType = "";
            public double itemCount = 0;
        }

        public SortedDictionary<string, List<IMyTerminalBlock>> blockDict = new SortedDictionary<string, List<IMyTerminalBlock>>();
        public IMyBlockGroup group;
        public string groupName;
        public int groupSize;
        bool automatic_update;
        bool addContainerWhenNeeded;

        public SortedList<string, ItemClass> ingotClassList = new SortedList<string, ItemClass>();
        public SortedList<string, ItemClass> componentClassList = new SortedList<string, ItemClass>();
        public SortedList<string, ItemClass> oreClassList = new SortedList<string, ItemClass>();

        public double max_gravel;
        public double currentGravel;

        public SortedDictionary<string, int> blockCount = new SortedDictionary<string, int>();

        public SortedDictionary<String, List<IMyTerminalBlock>> container_dicts = new SortedDictionary<string, List<IMyTerminalBlock>>();
        public SortedDictionary<String, List<IMyTerminalBlock>> production_dicts = new SortedDictionary<string, List<IMyTerminalBlock>>();
        public List<IMyShipConnector> ejectors = new List<IMyShipConnector>();
        public List<IMyTextPanel> panels = new List<IMyTextPanel>();
        public IMyTimerBlock timer = null;

        public List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
        public Dictionary<string, string> relativeType = new Dictionary<string, string>();
        public List<IMyTerminalBlock> availableContainers = new List<IMyTerminalBlock>();
        List<string> cargoPrefix;


        public Program()
        {
            groupName = "invManageGroup"; //all blocks from this group will be managed by the script
            automatic_update = true; //if you timer block is in the group the script will run every 100 tics
            max_gravel = 100000.0f; // here you are free to change how much gravel you want to keep, you need a connector to be able to remove gravel through this script
            addContainerWhenNeeded = true;

            group = GridTerminalSystem.GetBlockGroupWithName(groupName);
            if(group == null)
            {
                Echo("No group found, returning");
                Echo("Once you've added a group recompile script and run again");
                return;

            }
            currentGravel = 0.0f;
            cargoPrefix = new List<string> { "Ore", "Ingot", "Component" };
            groupSize = 0;
        }

        public string returnDest(string typeString)
        {
            if (relativeType.ContainsKey(typeString))
            {
                return relativeType[typeString];
            }
            else
            {
                return relativeType[""]; //return component cargos
            }
        }
        public void firstContainers(IMyTerminalBlock block)
        {

            IMyInventory tempInv;
            List<IMyTerminalBlock> tempList = new List<IMyTerminalBlock>();
            string volumeString = "";
            if (container_dicts.Keys.Count > 3)
            {
                Echo("Something is terribly wrong with the script, don't trust anything...");
                return;
            }
            else
            {
                tempInv = block.GetInventory();
                if ((float)tempInv.CurrentVolume > 0.0f)
                    volumeString = " (" + (((float)tempInv.CurrentVolume / (float)tempInv.MaxVolume) * 100.0f).ToString("N1") + "%)";
                else
                    volumeString = " (0%)"; 
                if (!(container_dicts.ContainsKey(cargoPrefix[0] + block.BlockDefinition.TypeId.ToString())))
                {
                    block.CustomName = cargoPrefix[0] + " Cargo Container 01" + volumeString;
                    tempList.Add(block);
                    container_dicts[cargoPrefix[0] + block.BlockDefinition.TypeId.ToString()] = tempList;
                    relativeType["MyObjectBuilder_Ore"] = cargoPrefix[0] + block.BlockDefinition.TypeId.ToString();

                }
                else if (!(container_dicts.ContainsKey(cargoPrefix[1] + block.BlockDefinition.TypeId.ToString())))
                {
                    block.CustomName = cargoPrefix[1] + " Container 01" + volumeString;
                    tempList.Add(block);
                    container_dicts[cargoPrefix[1] + block.BlockDefinition.TypeId.ToString()] = tempList;
                    relativeType["MyObjectBuilder_Ingot"] = cargoPrefix[1] + block.BlockDefinition.TypeId.ToString();

                }
                else if (!(container_dicts.ContainsKey(cargoPrefix[2] + block.BlockDefinition.TypeId.ToString())))
                {

                    block.CustomName = cargoPrefix[2] + " Container 01" + volumeString;
                    tempList.Add(block);
                    container_dicts[cargoPrefix[2] + block.BlockDefinition.TypeId.ToString()] = tempList;
                    relativeType[""] = cargoPrefix[2] + block.BlockDefinition.TypeId.ToString();
                }
                else
                {
                    Echo("Should never be here in firstContainer");
                }
            }
        }

        public void firstProdUnit(IMyTerminalBlock prodUnit)
        {
            List<IMyTerminalBlock> tempList = new List<IMyTerminalBlock>();
            if (!(production_dicts.ContainsKey(prodUnit.BlockDefinition.TypeId.ToString())))
            {
                tempList.Add(prodUnit);
                production_dicts[prodUnit.BlockDefinition.TypeId.ToString()] = tempList;
            }
            else
            {
                Echo("should not be here");
            }

        }
        public string getSmallestCargo() //no error handling also assume all three cargo groups are already populated
        {
            double smallestInv = 0.0;
            double tempInv = 0.0;
            string retV = "";
            foreach (string key in container_dicts.Keys)
            {
                foreach (IMyCargoContainer container in container_dicts[key])
                {
                    tempInv += (double)container.GetInventory().MaxVolume;
                }
                if (tempInv < smallestInv || smallestInv == 0.0)
                {
                    smallestInv = tempInv;
                    retV = key;
                }
                tempInv = 0;
            }
            if (retV == "")
            {
                Echo("error, incorrect type of return value for getSmallestCargo()");
            }
            return retV;
        }

        public void checkContainers()
        {
            IMyInventory tempInv;
            bool noSpace;
            foreach(string key in container_dicts.Keys)
            {
                noSpace = true;
                foreach(IMyTerminalBlock block in container_dicts[key])
                {
                    tempInv = block.GetInventory();
                    if (!(((float)tempInv.CurrentVolume / (float)tempInv.MaxVolume) > 0.9f)) //if there is space in a container that group does not need a new container
                    {
                        noSpace = false;
                        break;
                    }
                }
                if (noSpace)
                {
                    addContainer(availableContainers[0], key);
                    availableContainers.RemoveAt(0);
                }
            }
            if(availableContainers.Count < 1)
            {
                Echo("All containers are in use, add more containers");
            }
        }
        public void addContainer(IMyTerminalBlock block, string key)
        {
            string tempString = "";
            IMyInventory tempInv;
            tempInv = block.GetInventory();

            if ((float)tempInv.CurrentVolume > 0.0f)
                tempString = " (" + (((float)tempInv.CurrentVolume / (float)tempInv.MaxVolume) * 100.0f).ToString("N1") + "%)";
            else
                tempString = " (0%)"; 
            if (container_dicts[key].Count >= 10) 
            {
                block.CustomName = container_dicts[key][0].CustomName.Substring(0, container_dicts[key][0].CustomName.IndexOfAny("0123456789".ToCharArray())) +
                (container_dicts[key].Count + 1) + tempString;
            }
            else
            {
                block.CustomName = container_dicts[key][0].CustomName.Substring(0, container_dicts[key][0].CustomName.IndexOf("0") + 1) +
                (container_dicts[key].Count + 1) + tempString;
            }
            container_dicts[key].Add(block);

        }
        public void populateDicts()
        {

            container_dicts.Clear();
            production_dicts.Clear();
            panels.Clear();
            ejectors.Clear();
            timer = null;

            List<IMyTerminalBlock> tempBlocks = new List<IMyTerminalBlock>();
            IMyInventory tempInv;
            group.GetBlocks(tempBlocks);
            groupSize = tempBlocks.Count;
            string tempString = "";
            string volumeString = "";

            foreach(IMyTerminalBlock block in tempBlocks)
            {
                if(block is IMyCargoContainer) //switch to switch? (pun intended)
                {
                    if (container_dicts.Keys.Count != 3)
                    {
                        firstContainers(block);
                    } else if (addContainerWhenNeeded == true)
                    {
                        if (!availableContainers.Contains(block))
                        {
                            block.CustomName = "Extra Container " + (availableContainers.Count + 1).ToString();
                            availableContainers.Add(block);
                        }
                        continue;
                    }
                    else
                    {
                        tempInv = block.GetInventory();
                        tempString = getSmallestCargo();

                        if ((float)tempInv.CurrentVolume > 0.0f)
                            volumeString = " (" + (((float)tempInv.CurrentVolume / (float)tempInv.MaxVolume) * 100.0f).ToString("N1") + "%)";
                        else
                            volumeString = " (0%)"; 

                        if (container_dicts[tempString].Count >= 10) //super ugly
                        {
                            block.CustomName = container_dicts[tempString][0].CustomName.Substring(0, container_dicts[tempString][0].CustomName.IndexOfAny("0123456789".ToCharArray()))+
                            (container_dicts[tempString].Count + 1) + volumeString;
                        } else
                        {
                            block.CustomName = container_dicts[tempString][0].CustomName.Substring(0, container_dicts[tempString][0].CustomName.IndexOf("0") + 1) +
                            (container_dicts[tempString].Count + 1) + volumeString;
                        }
                        container_dicts[getSmallestCargo()].Add(block);
                    }

                } else if (block is IMyAssembler || block is IMyRefinery) //adds both 
                {
                    if(!production_dicts.ContainsKey(block.BlockDefinition.TypeId.ToString())) //does not differntiate between basic adn real version of assemblers and refineries
                    {
                        firstProdUnit(block);
                    } else
                    {
                        production_dicts[block.BlockDefinition.TypeId.ToString()].Add(block);
                    }

                } else if (block is IMyShipConnector)
                {
                    ejectors.Add(block as IMyShipConnector);

                } else if (block is IMyTextPanel)
                {
                    panels.Add(block as IMyTextPanel);
                    
                } else if (block is IMyTimerBlock)
                {
                    if (timer == null)
                        timer = (block as IMyTimerBlock);
                    else
                    {
                        Echo("There should only be one timer in the group");
                    }
                }
                else 
                {
                    Echo(block.CustomName + "is of the wrong type");
                    Echo("Allowed types are Cargo Containers, Assemblers, Refineries, LCD Panels and Ship Connector");
                }
            }
        }
        public void moveItems()
        {
            List<IMyTerminalBlock> tempCargos;
            IMyInventory tempInventory;
            IMyInventory inventory;
            List<MyInventoryItem> items = new List<MyInventoryItem>();

            string subType = "";
            string ignoreIf = "";
            int iCount;
            foreach(string key in container_dicts.Keys)
            {
                foreach(string tKey in relativeType.Keys)
                {
                    if (relativeType[tKey] == key)
                    {
                        ignoreIf = tKey;
                    }
                }
                foreach(IMyTerminalBlock container in container_dicts[key])
                {
                    inventory = container.GetInventory();
                    inventory.GetItems(items);
                    iCount = items.Count;
                    for (int i = 0; i < iCount; ++i)
                    {

                        if (items[i].Type.SubtypeId.ToString() != ignoreIf)
                        {

                            subType = items[i].Type.SubtypeId;
                            tempCargos = container_dicts[returnDest(items[i].Type.TypeId)];
                            foreach (IMyTerminalBlock tempContainer in tempCargos)
                            {

                                tempInventory = tempContainer.GetInventory();
                                inventory.TransferItemTo(tempInventory, items[i]);
                                if(subType != items[i].Type.SubtypeId.ToString())
                                {
                                    --iCount;
                                    --i;
                                }
                            }
                        }
                    }

                    subType = "";
                    items.Clear();
                }
                ignoreIf = "";

            }
            foreach(string key in production_dicts.Keys)
            {
                foreach(IMyTerminalBlock prodUnit in production_dicts[key])
                {
                    if (prodUnit is IMyRefinery)
                    {
                        inventory = (prodUnit as IMyRefinery).OutputInventory;
                    }
                    else if (prodUnit is IMyAssembler)
                    {
                        inventory = (prodUnit as IMyAssembler).OutputInventory;
                    } else
                    {
                        inventory = prodUnit.GetInventory(); //something is wrong if it's here but at least it will work
                    }
                    inventory.GetItems(items);
                    iCount = items.Count;
                    for (int i = 0; i < iCount; ++i)
                    {
                        subType = items[i].Type.SubtypeId.ToString();
                        tempCargos = container_dicts[returnDest(items[i].Type.TypeId)];
                        foreach (IMyTerminalBlock tempContainer in tempCargos)
                        {
                            tempInventory = tempContainer.GetInventory();
                            inventory.TransferItemTo(tempInventory, items[i]);
                            if(subType != items[i].Type.SubtypeId.ToString())
                            {
                                subType = "";
                                --iCount;
                                --i;
                            }
                        }
                    }
                    items.Clear();
                }
            }
        }
        public void moveItems(bool checkAdditional)
        {
            if (!checkAdditional)
                return;
            IMyInventory tempInventory;
            IMyInventory inventory;
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            List<IMyTerminalBlock> tempCargos = new List<IMyTerminalBlock>();

            foreach (string key in container_dicts.Keys)
            {
                foreach (IMyTerminalBlock container in availableContainers)
                {
                    inventory = container.GetInventory();
                    inventory.GetItems(items);

                    foreach (MyInventoryItem item in items)
                    {
                        tempCargos = container_dicts[returnDest(item.Type.TypeId)];
                        foreach (IMyTerminalBlock tempBlock in tempCargos)
                        {
                            tempInventory = tempBlock.GetInventory();
                            inventory.TransferItemTo(tempInventory, item);
                        }

                    }
                }
            }
        }
        public void AddIngotCount(MyInventoryItem item)
        {
            string itemSubType = item.Type.SubtypeId;
            if (!ingotClassList.ContainsKey(itemSubType))
            {
                ingotClassList[itemSubType] = new ItemClass();
                ingotClassList[itemSubType].itemType = item.Type.TypeId;
                ingotClassList[itemSubType].itemSubType = itemSubType;
            }
            ingotClassList[itemSubType].itemCount += (double)item.Amount;
        }

        public void AddOreCount(MyInventoryItem item)
        {
            string itemSubType = item.Type.SubtypeId;
            if (!oreClassList.ContainsKey(itemSubType))
            {
                oreClassList[itemSubType] = new ItemClass();
                oreClassList[itemSubType].itemType = item.Type.TypeId;
                oreClassList[itemSubType].itemSubType = itemSubType;
            }
            oreClassList[itemSubType].itemCount += (double)item.Amount;
        }


        public void AddComponentCount(MyInventoryItem item)
        {
            string itemSubType = item.Type.SubtypeId;
            if (!componentClassList.ContainsKey(itemSubType))
            {
                componentClassList[itemSubType] = new ItemClass();
                componentClassList[itemSubType].itemType = item.Type.TypeId;
                componentClassList[itemSubType].itemSubType = itemSubType;
            }
            componentClassList[itemSubType].itemCount += (double)item.Amount;
        }


        public void CountItems()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(blocks);

            IMyInventory inventory;
            List<MyInventoryItem> items = new List<MyInventoryItem>();


            foreach(IMyTerminalBlock block in blocks)
            {
                if (block.HasInventory)
                {
                    inventory = block.GetInventory();
                    inventory.GetItems(items);
                    foreach(MyInventoryItem item in items){
                        if(returnDest(item.Type.TypeId).Substring(0,3) == "Ore")
                        {
                            AddOreCount(item);
                        } else if (returnDest(item.Type.TypeId).Substring(0,3) == "Ing")
                        {
                            if (!(block is IMyShipConnector))
                            {
                                AddIngotCount(item);
                            }
                        } else if (returnDest(item.Type.TypeId).Substring(0,3) == "Com")
                        {
                            AddComponentCount(item);
                        } else
                        {
                            Echo(returnDest(item.Type.TypeId).Substring(0, 3));
                            Echo("impossible type of item in CountItems()");
                        }
                    }
                    items.Clear();
                }
            }
        }
        public string customValues(string data, List<string> values)
        {
            foreach(string cmp in values)
            {
                if (data == cmp)
                    return cmp;
                    //return true;
            }
            return "";
        }
        public int smallestValue(SortedDictionary<string, int> dict)
        {
            return dict.Min(i => i.Value);
        }
        
        public string smallestKey(SortedDictionary<string, int> dict)
        {
            return dict.MinBy(i => i.Value).Key;
        }
        public void checkLCDs()
        {
            List<string> customDatas = new List<string> { "show ore", "show ingot", "show component" };
            List<string> customNames = new List<string> { "Ore LCD Panel ", "Ingot LCD Panel ", "Component LCD Panel " };
            SortedDictionary<string, int> showAndCount = new SortedDictionary<string, int>();
            SortedDictionary<string, string> customToName = new SortedDictionary<string, string>();

            for(int i=0; i<customDatas.Count; ++i)
            {
                if(customDatas.Count != customNames.Count)
                {
                    Echo("Error: checkLCDs() have the wrong lists");
                    return;
                }

                showAndCount.Add(customDatas[i], 0);
                customToName.Add(customDatas[i], customNames[i]);
            }
            string tKey;// = smallestKey(showAndCount);
            int smallestCount;// = smallestValue(showAndCount);
            foreach(IMyTextPanel panel in panels)
            {
                panel.ContentType = ContentType.TEXT_AND_IMAGE;
                panel.Enabled = true;

                if ((tKey = customValues(panel.CustomData, customDatas)) != "") //if panel is already set up with custom data.
                {

                    showAndCount[tKey]++;
                    panel.CustomName = customToName[tKey] + showAndCount[tKey].ToString(); //maybe make customToName and showAndCount into one dict
                }
                else
                {
                    tKey = smallestKey(showAndCount);
                    smallestCount = smallestValue(showAndCount);
                    panel.CustomData = tKey;
                    showAndCount[tKey]++;
                    panel.CustomName = customToName[tKey] + showAndCount[tKey].ToString();
                }
            } 
        }
        public void OutputItemCounts()
        {
            checkLCDs();
            string oreText = "";
            string ingotText = "";
            string componentText = "";

            for (int i = 0; i < ingotClassList.Count; i++)
            {
                ingotText += (string)(ingotClassList.Values[i].itemSubType + ": " + ingotClassList.Values[i].itemCount.ToString("N2") + "\n");
                if (ingotClassList.Values[i].itemSubType == "Stone")
                {
                    currentGravel = ingotClassList.Values[i].itemCount;
                }
            }
            for (int i = 0; i < oreClassList.Count; i++)
            {
                oreText += (string)(oreClassList.Values[i].itemSubType + ": " + oreClassList.Values[i].itemCount.ToString("N2") + "\n");
            }
            for (int i = 0; i < componentClassList.Count; i++)
            {
                componentText += (string)(componentClassList.Values[i].itemSubType + ": " + componentClassList.Values[i].itemCount.ToString("N0") + "   ");
                if (i % 3 == 0)
                    componentText += "\n";
            }

            foreach (IMyTextPanel panel in panels)
            {
                if (panel.CustomData == "show ore")
                {
                    panel.WritePublicTitle("Ore List");
                    panel.WriteText(oreText);
                }
                else if (panel.CustomData == "show ingot")
                {

                    panel.WritePublicTitle("Ingot List");
                    panel.WriteText(ingotText);
                }
                else if (panel.CustomData == "show component")
                {
                    panel.WritePublicTitle("Component List");
                    panel.WriteText(componentText);
                }
                else
                {
                    Echo("panels not initialized correctly");
                }
            }
        }
           
        public void ResetCounts()
        {
            for (int i = 0; i < ingotClassList.Count; i++)
            {
                ingotClassList.Values[i].itemCount = 0;
                if(ingotClassList.Values[i].itemSubType == "Stone")
                {
                currentGravel = ingotClassList.Values[i].itemCount;
                }

            }

            for (int i = 0; i < oreClassList.Count; i++)
            {
                oreClassList.Values[i].itemCount = 0;
            }

            for (int i = 0; i < componentClassList.Count; i++)
            {
                componentClassList.Values[i].itemCount = 0;
            }
        }

        /*
        public void clearProdInput(IMyInventory inputInventory)
        {
            List<MyInventoryItem> inputItems = new List<MyInventoryItem>();
            IMyInventory cargoInventory;
            inputInventory.GetItems(inputItems);

            foreach(MyInventoryItem item in inputItems)
            {
                foreach (IMyCargoContainer container in containers)
                {
                    cargoInventory = container.GetInventory();
                    if (cargoInventory.IsFull)
                    {
                        break;
                    }
                    inputInventory.TransferItemTo(cargoInventory, item);
                }
            }
        }*/
        public void assessGravel()

        {

            if(currentGravel <= max_gravel)
            {
                return;
            }

            List<IMyTerminalBlock> containers = container_dicts[returnDest("MyObjectBuilder_Ingot")];
            IMyInventory from_inventory; //ingot containers
            IMyInventory to_inventory; //ejectors
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            int invCount = 0;

            foreach(IMyTerminalBlock container in containers)
            {
                from_inventory = container.GetInventory();

                from_inventory.GetItems(items, it => (it.Type.SubtypeId == "Stone" && it.Type.TypeId == "MyObjectBuilder_Ingot"));
                invCount = items.Count;
                for(int i = 0; i < invCount; ++i)
                {
                    foreach(IMyShipConnector ejector in ejectors)
                    {
                        ejector.ThrowOut = true;
                        ejector.Enabled = true;
                        ejector.CustomName = "Ejector " + (i + 1);
                        to_inventory = ejector.GetInventory();
                        if ((double)items[i].Amount > max_gravel - (currentGravel)) 
                        {
                            from_inventory.TransferItemTo(to_inventory, items[i]);
                        } else
                        {
                            return;
                        }
                        currentGravel -= to_inventory.ItemCount; //assumes the ejector has had time to empty all stone already
                        if(currentGravel <= max_gravel)
                        {
                            break;
                        }
                    }

                }
                items.Clear();

            }
        }

        public void updateNames()
        {
            IMyInventory tempInv;
            foreach(string key in container_dicts.Keys)
            {
                foreach(IMyTerminalBlock block in container_dicts[key])
                {
                    tempInv = block.GetInventory();
                    if ((float)tempInv.CurrentVolume == 0.0f)
                    {
                        block.CustomName = block.CustomName.Substring(0, block.CustomName.IndexOf("(")) +  "(0%)";
                    }
                    else {
                        block.CustomName = block.CustomName.Substring(0, block.CustomName.IndexOf("(")) + "(" + 
                        ((((float)tempInv.CurrentVolume)/(float)tempInv.MaxVolume) * 100.0f).ToString("N1") + "%)";
                    }
                }

            }
        } 
        public void populateBlockCount(List<IMyTerminalBlock> blocks)
        {

            blockCount.Clear();
            foreach(IMyTerminalBlock block in blocks)
            {
                if (blockCount.ContainsKey(block.BlockDefinition.SubtypeId))
                    blockCount[block.BlockDefinition.SubtypeId]++;
                else
                {
                    blockCount.Add(block.BlockDefinition.SubtypeId, 1);
                }
            }
        }
        public void Main(string argument, UpdateType updatesource)
        {

            List<IMyTerminalBlock> temp = new List<IMyTerminalBlock>();
            if(group == null)
            {
                Echo("No group found, returning");
                Echo("Once you've added a group recompile script and run again");
                return;

            }
            group.GetBlocks(temp);
            if (groupSize != temp.Count )
            {
                availableContainers.Clear();
                populateDicts();
                populateBlockCount(temp);

                List<IMyAssembler> assemblers = new List<IMyAssembler>();
                group.GetBlocksOfType(assemblers, blk => (blk is IMyAssembler));
                if (assemblers != null && assemblers.Count > 1)
                {
                    assemblers[0].Enabled = true;
                    assemblers[0].CooperativeMode = false;
                    for (int i = 1; i < assemblers.Count; i++)
                    {
                        assemblers[i].Enabled = true;
                        assemblers[i].CooperativeMode = true;
                    }
                    assemblers.Clear();
                }
            }
            temp.Clear();

            moveItems();              
            ResetCounts();
            CountItems();
            OutputItemCounts();
            if (addContainerWhenNeeded)
            {
                moveItems(true);
                checkContainers();
            }

            Echo("script is running...");
            foreach(string key in blockCount.Keys)
            {
                Echo(key + ": " + blockCount[key].ToString());
            }
            if (addContainerWhenNeeded)
            {
                Echo("Available containers not in use: " + availableContainers.Count.ToString());
            }
            if (ejectors.Count > 0)
                assessGravel();

            updateNames();
            if (timer != null)
            {
                timer.StartCountdown();
            }
            else if (automatic_update)
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
            } else
            {
                Echo("No timer detected and automatic update not requested, script runs only once");
            }
            
        }
    }
}