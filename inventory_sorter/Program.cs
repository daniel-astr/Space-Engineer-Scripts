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

         // Blocks
        public List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
        public SortedDictionary<string, List<IMyCargoContainer>> container_dict = new SortedDictionary<string, List<IMyCargoContainer>>();
        public List<IMyCargoContainer> unusedContainers = new List<IMyCargoContainer>();
        public List<IMyAssembler> assembler_list = new List<IMyAssembler>();
        public List<IMyRefinery> refinery_list = new List<IMyRefinery>();
        public List<IMyShipConnector> ejector_list = new List<IMyShipConnector>();
        public List<IMyTextPanel> panels = new List<IMyTextPanel>();
        public IMyTimerBlock timer;

        public IMyBlockGroup group;

        public Dictionary<string, string> relativeType = new Dictionary<string, string>();
        public List<string> CargoPrefix;

        public SortedList<string, ItemClass> componentClassList = new SortedList<string, ItemClass>();
        public SortedList<string, ItemClass> ingotClassList = new SortedList<string, ItemClass>();
        public SortedList<string, ItemClass> oreClassList = new SortedList<string, ItemClass>();
        public SortedDictionary<string, int> blockCount = new SortedDictionary<string, int>();

        public string groupName;
        public double max_gravel;
        public double currentGravel;
        public bool automatic_update;
        public bool addContainerWhenNeeded;

        public float timerDelay;
        public Program()
        {
            groupName = "invManageGroup"; //all blocks from this group will be managed by the script
            automatic_update = true; //if you timer block is in the group the script will run every 100 tics
            max_gravel = 100000.0f; // here you are free to change how much gravel you want to keep, you need a connector to be able to remove gravel through this script
            addContainerWhenNeeded = true;

            timerDelay = 8.0f; //8.0f is 8 seconds. Can be changed to whatever.

            currentGravel = 0.0f;

            CargoPrefix = new List<string> { "Ore", "Ingot", "Component" };
            setupRelativeType();

            group = GridTerminalSystem.GetBlockGroupWithName(groupName);
            if(group == null)
            {
                Echo("No group found, returning");
                Echo("Once you've added a group recompile script and run again");
                return;

            }
            updateBlocks();

            if (automatic_update && timer != null)
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
            else
                Runtime.UpdateFrequency = UpdateFrequency.Once;
        }
        private void setupRelativeType()
        {
            List<string> typeIds = new List<string> { "MyObjectBuilder_Ore", "MyObjectBuilder_Ingot", "" };
            for(int i=0; i<CargoPrefix.Count; ++i)
            {
                relativeType.Add(typeIds[i], CargoPrefix[i]);
            }
        }
        public string cargoType(string typeString)//item.type.Typeid;
        {
            if (relativeType.ContainsKey(typeString))
                return relativeType[typeString];
            else
                return relativeType[""]; //components
                
        }
        public void updateBlocks()
        {
            List<IMyAssembler> tempAssemblers = new List<IMyAssembler>();
            List<IMyRefinery> tempRefineries = new List<IMyRefinery>();
            List<IMyCargoContainer> tempContainers = new List<IMyCargoContainer>();
            List<IMyShipConnector> tempEjector = new List<IMyShipConnector>();
            List<IMyTimerBlock> tempTimers = new List<IMyTimerBlock>();
            List<IMyTextPanel> tempPanels = new List<IMyTextPanel>();
            group.GetBlocksOfType<IMyAssembler>(tempAssemblers);
            group.GetBlocksOfType<IMyRefinery>(tempRefineries);
            group.GetBlocksOfType<IMyCargoContainer>(tempContainers);
            group.GetBlocksOfType<IMyShipConnector>(tempEjector);
            group.GetBlocksOfType<IMyTimerBlock>(tempTimers);
            group.GetBlocksOfType<IMyTextPanel>(tempPanels);
            if(tempTimers.Count > 0){
                if (timer != tempTimers[0])
                {
                    timer = tempTimers[0];
                    Echo("Timer block detected, program is set to run of the timer instead of automatic update");
                }
                timer.StartCountdown();
            }
            setupAssemblers(tempAssemblers);
            setupRefineries(tempRefineries);
            setupContainers(tempContainers);
            setupPanels(tempPanels);
            setupEjectors(tempEjector); 
        }
        public void setupPanels(List<IMyTextPanel> tempPanels)
        {
            if(tempPanels.Count == 0)
            {
                panels.Clear();
                return;
            }
            if(!(tempPanels.SequenceEqual(panels)))
            {
                foreach (IMyTextPanel panel in panels)
                {
                    if (!(tempPanels.Contains(panel)))
                    {
                        panels.Remove(panel);
                    }
                }
                foreach (IMyTextPanel panel in tempPanels)
                {
                    if (!(panels.Contains(panel)))
                    {
                        panels.Add(panel);
                    }

                }
            }
        }

        public void setupAssemblers(List<IMyAssembler> tempAssemblers)
        {
            if(tempAssemblers.Count == 0)
            {
                assembler_list.Clear();
                return;
            }
            if(!(tempAssemblers.SequenceEqual(assembler_list)))
            {
                foreach (IMyAssembler assembler in assembler_list)
                {
                    if (!(tempAssemblers.Contains(assembler)))
                    {
                        assembler_list.Remove(assembler);
                    }
                }
                foreach (IMyAssembler assembler in tempAssemblers)
                {
                    if (!(assembler_list.Contains(assembler)))
                    {
                        assembler.CustomName = "Assembler " + (assembler_list.Count + 1).ToString();
                        assembler_list.Add(assembler);
                    }

                }
            }
        }
        public void setupRefineries(List<IMyRefinery> tempRefineries)
        {
            if(tempRefineries.Count == 0)
            {
                refinery_list.Clear();
                return;
            }
            if(!(tempRefineries.SequenceEqual(refinery_list)))
            {
                foreach (IMyRefinery refinery in refinery_list)
                {
                    if (!(tempRefineries.Contains(refinery)))
                    {
                        refinery_list.Remove(refinery);
                    }
                }
                foreach (IMyRefinery refinery in tempRefineries)
                {
                    if (!(refinery_list.Contains(refinery)))
                    {
                        refinery.CustomName = "Refinery " + (refinery_list.Count + 1).ToString();
                        refinery_list.Add(refinery);
                    }

                }
            }
        }
        public void setupContainers(List<IMyCargoContainer> tempContainers)
        {
            List<IMyCargoContainer> currentContainers = new List<IMyCargoContainer>();
            foreach (string tKey in container_dict.Keys)
            {
                foreach (IMyCargoContainer container in container_dict[tKey])
                {
                    currentContainers.Add(container);
                }
            }
            foreach (IMyCargoContainer container in unusedContainers)
            {
                currentContainers.Add(container);
            }
            if (!(tempContainers.SequenceEqual(currentContainers))){
                container_dict.Clear();
                unusedContainers.Clear();
                currentContainers.Clear();
            }
            if(container_dict.Keys.Count < 3)  
            {
                container_dict.Clear();
                for (int i = 0; container_dict.Keys.Count != 3 && i < 3; ++i)
                {
                    setupContainerDict(tempContainers[i]);
                    tempContainers.Remove(tempContainers[i]);
                }
            }

            bool inAny = false;
            foreach(IMyCargoContainer container in tempContainers)
            {
                if (!unusedContainers.Contains(container)){
                    foreach(string tKey in container_dict.Keys)
                    {
                        if (container_dict[tKey].Contains(container)){
                            inAny = true;
                        }
                    }
                    if (!(inAny)) //if the container is not added yet, break and add it.
                    {
                        container.CustomName = "Extra Cargo Container " + (unusedContainers.Count + 1).ToString();
                        unusedContainers.Add(container);
                    }
                    inAny = false;
                }
            }
        }
        public void setupContainerDict(IMyCargoContainer container)
        {
            bool found = false;
            List<IMyCargoContainer> tempList = new List<IMyCargoContainer>();
            IMyInventory tempInv = container.GetInventory();
            string volumeString = "";
            if ((float)tempInv.CurrentVolume > 0.0f)
                volumeString = " (" + (((float)tempInv.CurrentVolume / (float)tempInv.MaxVolume) * 100.0f).ToString("N1") + "%)";
            else
                volumeString = " (0%)";

            foreach(string vString in CargoPrefix)
            {
                if (!(container_dict.ContainsKey(vString)))
                {
                    container.CustomName = vString + " Cargo Container 01" + volumeString;
                    tempList.Add(container);
                    container_dict.Add(vString, tempList);
                    found = true;
                    break;
                }
            } 
            if(!(found))
                Echo("Dict already contains the proper key.\n" + container.CustomName + " is lost");
        }
        public void setupEjectors(List<IMyShipConnector> tempEjectors)
        {
            if(tempEjectors.Count == 0)
            {
                ejector_list.Clear();
                return;
            }
            if(!(tempEjectors.SequenceEqual(ejector_list)))
            {
                foreach (IMyShipConnector ejector in ejector_list)
                {
                    if (!(tempEjectors.Contains(ejector)))
                    {
                        ejector_list.Remove(ejector);
                    }
                }
                foreach (IMyShipConnector ejector in tempEjectors)
                {
                    if (!(ejector_list.Contains(ejector)))
                    {
                        ejector_list.Add(ejector);
                    }

                }
            }
        }
        public void moveItems()
        {
            IMyInventory inInventory;
            string lastType = "";
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            List<MyInventoryItem> tempItems = new List<MyInventoryItem>();
            
            foreach(IMyAssembler assembler in assembler_list)
            {
                inInventory = assembler.InputInventory;
                if (((float)inInventory.CurrentVolume / (float)inInventory.MaxVolume) > 0.6)
                {
                    inInventory.GetItems(items);
                    while(items.Count > 0)
                    {
                        lastType = cargoType(items[0].Type.TypeId);
                        inInventory.GetItems(tempItems, itm => (cargoType(itm.Type.TypeId) == lastType));
                        moveItemType(inInventory, tempItems);
                        tempItems.Clear();
                        items.RemoveAll(itm => (cargoType(itm.Type.TypeId) == lastType));
                    }
                    items.Clear();
                }
                inInventory = assembler.OutputInventory;
                inInventory.GetItems(items);
                while(items.Count > 0)
                {
                    lastType = cargoType(items[0].Type.TypeId);
                    inInventory.GetItems(tempItems, itm => (cargoType(itm.Type.TypeId) == lastType));
                    moveItemType(inInventory, tempItems);
                    tempItems.Clear();
                    items.RemoveAll(itm => (cargoType(itm.Type.TypeId) == lastType));
                }
                items.Clear();
            }
            foreach(IMyRefinery refinery in refinery_list)
            {
                inInventory = refinery.InputInventory;
                if( ((float)inInventory.CurrentVolume/(float)inInventory.MaxVolume) > 0.6){
                    inInventory.GetItems(items);
                    while(items.Count > 0)
                    {
                        lastType = cargoType(items[0].Type.TypeId);
                        inInventory.GetItems(tempItems, itm => (cargoType(itm.Type.TypeId) == lastType));
                        moveItemType(inInventory, tempItems);
                        tempItems.Clear();
                        items.RemoveAll(itm => (cargoType(itm.Type.TypeId) == lastType));
                    }
                    items.Clear();
                }
                inInventory = refinery.OutputInventory;
                inInventory.GetItems(items);
                while(items.Count > 0)
                {
                    lastType = cargoType(items[0].Type.TypeId);
                    inInventory.GetItems(tempItems, itm => (cargoType(itm.Type.TypeId) == lastType));
                    moveItemType(inInventory, tempItems);
                    tempItems.Clear();
                    items.RemoveAll(itm => (cargoType(itm.Type.TypeId) == lastType));
                }
                items.Clear();
            }
            foreach(IMyCargoContainer container in unusedContainers)
            {
                inInventory = container.GetInventory();
                inInventory.GetItems(items);
                while(items.Count > 0)
                {
                    lastType = cargoType(items[0].Type.TypeId);
                    inInventory.GetItems(tempItems, itm => (cargoType(itm.Type.TypeId) == lastType));
                    moveItemType(inInventory, tempItems);
                    tempItems.Clear();
                    items.RemoveAll(itm => (cargoType(itm.Type.TypeId) == lastType));
                }
                items.Clear();
            }
            foreach(string tKey in container_dict.Keys)
            {
                foreach(IMyCargoContainer container in container_dict[tKey])
                {
                    inInventory = container.GetInventory();
                    inInventory.GetItems(items);
                    while(items.Count > 0)
                    {
                        lastType = cargoType(items[0].Type.TypeId);
                        inInventory.GetItems(tempItems, itm => (cargoType(itm.Type.TypeId) == lastType));
                        moveItemType(inInventory, tempItems);
                        tempItems.Clear();
                        items.RemoveAll(itm => (cargoType(itm.Type.TypeId) == lastType));
                    }
                    items.Clear();
                }
            }
        }
        public void moveItemType(IMyInventory inInventory, List<MyInventoryItem> items)
        {

            IMyInventory outInventory;
            MyInventoryItem tempItem;
            int itemCount = items.Count;
            bool isFull = true;
            
            if (!(container_dict.ContainsKey(cargoType(items[0].Type.TypeId)))) {
                Echo("Trying to move item when there is no containers available");
                return;
            }
            for (int i = 0; i < itemCount; ++i)
            {
                tempItem = items[i];
                foreach (IMyCargoContainer container in container_dict[cargoType(items[i].Type.TypeId)])
                {
                    outInventory = container.GetInventory();
                    if(((float)outInventory.CurrentVolume / (float)outInventory.MaxVolume) < 0.9)
                        isFull = false;

                    if (inInventory.CanTransferItemTo(outInventory, items[i].Type))
                    {
                        inInventory.TransferItemTo(outInventory, items[i]);
                    }
                    else
                    {
                        Echo("Can't transfer items to " + container.CustomName + " make sure the conveyor system is working");
                    }
                    if(tempItem != items[i])
                    {
                        itemCount--;
                        i--;
                        break;
                    }
                }
                if (isFull)
                    addNewContainer(cargoType(tempItem.Type.TypeId));
                isFull = true;
            }
        }
        public void updateContainers()
        {
            bool full = true;
            IMyInventory tempInv;
            string volumeString = "";
            foreach(string tKey in container_dict.Keys)
            {
                foreach(IMyCargoContainer container in container_dict[tKey])
                {
                    tempInv = container.GetInventory();
                    if ((float)tempInv.CurrentVolume == 0.0f)
                        volumeString = "(0%)";
                    else
                        volumeString = "(" + (((float)tempInv.CurrentVolume / (float)tempInv.MaxVolume) * 100.0f).ToString("N1") + "%)";

                    if(!(((float)tempInv.CurrentVolume / (float)tempInv.MaxVolume) > 0.9f))
                        full = false;
                    container.CustomName = container.CustomName.Substring(0, container.CustomName.IndexOf("(")) +
                        volumeString;
                }
                if (full)
                {
                    addNewContainer(tKey);
                }
            }
        }
        public void addNewContainer(string tKey)
        {
            IMyCargoContainer tempCargo;
            IMyCargoContainer tempFromDict;
            IMyInventory tempInv;
            string volumeString = "";

            tempFromDict = container_dict[tKey][0];
            if (unusedContainers.Count > 0)
            {    
                tempCargo = unusedContainers[0];
                tempInv = tempCargo.GetInventory();

                if ((float)tempInv.CurrentVolume == 0.0f)
                    volumeString = "(0%)";
                else
                    volumeString = "(" + (((float)tempInv.CurrentVolume / (float)tempInv.MaxVolume) * 100.0f).ToString("N1") + "%)";

                tempCargo.CustomName = tempFromDict.CustomName.Substring(0, tempFromDict.CustomName.IndexOf("0") + 1) +
                    (container_dict[tKey].Count + 1) + volumeString;

                container_dict[tKey].Add(tempCargo);
                unusedContainers.Remove(tempCargo);
            }
            else
                Echo("No free containers and inventories are full, add new containers");
        }
        
        public void CountItems()
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(blocks);

            IMyInventory tempInv;
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            string typeName; 
            foreach(IMyTerminalBlock block in blocks)
            {
                if (block.HasInventory)
                {
                    tempInv = block.GetInventory();
                    tempInv.GetItems(items);
                    foreach(MyInventoryItem item in items)
                    {
                        typeName = item.Type.TypeId;
                        if (cargoType(typeName) == CargoPrefix[0])
                            addOreCount(item);
                        else if (cargoType(typeName) == CargoPrefix[1])
                            addIngotCount(item);
                        else if (cargoType(typeName) == CargoPrefix[2])
                            addComponentCount(item);
                    }
                    items.Clear();
                }
            }

        }

        public void addIngotCount(MyInventoryItem item)
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

        public void addOreCount(MyInventoryItem item)
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
        public void addComponentCount(MyInventoryItem item)
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
        public void resetCounts()
        {
            for(int i=0; i<ingotClassList.Count; ++i)
            {
                ingotClassList.Values[i].itemCount = 0;
                if (ingotClassList.Values[i].itemSubType == "Stone")
                    currentGravel = 0.0f;
            }

            for(int i=0; i<oreClassList.Count; ++i)
                oreClassList.Values[i].itemCount = 0;
            for (int i = 0; i < componentClassList.Count; ++i)
                componentClassList.Values[i].itemCount = 0;
        }
        public string customValues(string data, List<string> values)
        {
            foreach (string cmp in values)
            {
                if (data == cmp)
                    return cmp;
                //return true;
            }
            return "";
        }
        public string smallestKey(SortedDictionary<string, int> dict)
        {
            return dict.MinBy(i => i.Value).Key;
        }
        public int smallestValue(SortedDictionary<string, int> dict)
        {
            return dict.Min(i => i.Value);
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

        public void populateBlockCount()
        {

            blocks.Clear();
            group.GetBlocks(blocks);
            blockCount.Clear();
            foreach (IMyTerminalBlock block in blocks)
            {
                if (blockCount.ContainsKey(block.BlockDefinition.SubtypeId))
                    blockCount[block.BlockDefinition.SubtypeId]++;
                else
                {
                    blockCount.Add(block.BlockDefinition.SubtypeId, 1);
                }
            }
        }

        public void assessGravel()

        {

            if (currentGravel <= max_gravel)
            {
                return;
            }

            List<IMyCargoContainer> containers = container_dict[cargoType("MyObjectBuilder_Ingot")];
            IMyInventory from_inventory; //ingot containers
            IMyInventory to_inventory; //ejectors
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            int invCount = 0;
            foreach (IMyCargoContainer container in containers)
            {
                from_inventory = container.GetInventory();

                from_inventory.GetItems(items, it => (it.Type.SubtypeId == "Stone" && it.Type.TypeId == "MyObjectBuilder_Ingot"));
                invCount = items.Count;
                for (int i = 0; i < invCount; ++i)
                {
                    foreach (IMyShipConnector ejector in ejector_list)
                    {
                        ejector.ThrowOut = true;
                        ejector.Enabled = true;
                        ejector.CustomName = "Ejector " + (i + 1);
                        to_inventory = ejector.GetInventory();
                        if ((double)items[i].Amount > max_gravel - (currentGravel))
                        {
                            from_inventory.TransferItemTo(to_inventory, items[i]);
                        }
                        else
                        {
                            return;
                        }
                        currentGravel -= to_inventory.ItemCount; //assumes the ejector has had time to empty all stone already
                        if (currentGravel <= max_gravel)
                        {
                            break;
                        }
                    }
                }
                items.Clear();
            }
        }
        public void Main(string argument, UpdateType updatesource)
        {
            //check unused containers and all containers before moving items
            //so the program can warn if there is no space before trying to moive item s


            //updateBlocks();
            if(group == null)
            {
                Echo("no group found, add a group and restart the script");
                return; 
            }

            updateBlocks();
            if(container_dict.Count < 3)
            {
                Echo("Not enough containers added, the script need at least three to function");
                return;
            }
            moveItems();
            updateContainers();
            CountItems();
            OutputItemCounts();
            populateBlockCount();
            resetCounts();
            foreach (string key in blockCount.Keys)
            {
                Echo(key + ": " + blockCount[key].ToString());
            }

            if (ejector_list.Count > 0)
                assessGravel();
        }
    }
}