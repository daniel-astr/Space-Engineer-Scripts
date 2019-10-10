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
        string groupName;
        IMyBlockGroup group = null;
        List<IMyCargoContainer> containers = new List<IMyCargoContainer>();
        List<IMyAssembler> assemblers = new List<IMyAssembler>();
        List<IMyRefinery> refineries = new List<IMyRefinery>();
        public Program() {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            groupName = "outputGroup";
            group = GridTerminalSystem.GetBlockGroupWithName(groupName);
            if(group == null)
            {
                Echo("No group with the name " + groupName + " exists");
                return;
            }

        }
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
        }
        public void emptyProdUnits()
        {
            IMyInventory outputInventory;
            IMyInventory cargoInventory;
            IMyInventory inputInventory;
            List<MyInventoryItem> outputItems = new List<MyInventoryItem>();
            List<MyInventoryItem> inputItems = new List<MyInventoryItem>();
    
            foreach(IMyAssembler assembler in assemblers)
            {
                outputInventory = assembler.OutputInventory;
                outputInventory.GetItems(outputItems);
                inputInventory = assembler.InputInventory;
                if(((float)inputInventory.CurrentVolume/(float)inputInventory.MaxVolume) > 0.6f)
                {
                    clearProdInput(inputInventory); 
                }
                foreach(MyInventoryItem item in outputItems)
                {
                    foreach(IMyCargoContainer container in containers)
                    {

                        cargoInventory = container.GetInventory();
                        if(cargoInventory.IsFull)
                        {
                            break;
                        }
                        outputInventory.TransferItemTo(cargoInventory, item);
                    }
                }
                outputItems.Clear();
            }
            foreach (IMyRefinery refinery in refineries)
            {
                outputInventory = refinery.OutputInventory;
                outputInventory.GetItems(outputItems);

                inputInventory = refinery.InputInventory;
                if(((float)inputInventory.CurrentVolume/(float)inputInventory.MaxVolume) > 0.6f)
                {
                    clearProdInput(inputInventory); 
                }

                foreach(MyInventoryItem item in outputItems)
                {
                    foreach(IMyCargoContainer container in containers)
                    {

                        cargoInventory = container.GetInventory();
                        if(cargoInventory.IsFull)
                        {
                            break;
                        }
                        outputInventory.TransferItemTo(cargoInventory, item);
                    }
                }
                outputItems.Clear();
            }

        }
        public bool containersFull()
        {
            List<bool> fullContainer = new List<bool>();
            IMyInventory inventory;
            foreach(IMyCargoContainer container in containers)
            {
                inventory = container.GetInventory();
                fullContainer.Add(inventory.IsFull);
            }
            foreach(bool full in fullContainer)
            {
                if(full == false)
                {
                    return false;
                }
            }
            return true;
        }
        public void Main(string argument, UpdateType updateSource)
        {

            if(group == null)
            {
                Echo("No group with the name " + groupName + " exists");
                return;
            }
            group.GetBlocksOfType<IMyCargoContainer>(containers);
            if(containers.Count < 1)
            {
                Echo("No containers added to the group\nThere is no space for the refineries and assemblers to empty to");
            }
            group.GetBlocksOfType<IMyAssembler>(assemblers);
            group.GetBlocksOfType<IMyRefinery>(refineries);
            emptyProdUnits();
            if (containersFull()) {
                Echo("Containers are full, add more containers");
            }
            Echo("Script running...");
            Echo("Assemblers: " + assemblers.Count.ToString());
            Echo("Refineries: " + refineries.Count.ToString());
            Echo("Containers: " + containers.Count.ToString());

        }
    }
}
