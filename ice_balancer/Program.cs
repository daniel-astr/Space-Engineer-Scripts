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
        
        public List<IMyGasGenerator> gasGenerators = new List<IMyGasGenerator>();
        public List<IMyGasTank> gasTanks = new List<IMyGasTank>();
        public List<IMyCargoContainer> containers = new List<IMyCargoContainer>();

        public double cargoIce;
        //private static double iceSize = 0.37f;
        private double targetVolume;
        
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            GridTerminalSystem.GetBlocksOfType<IMyGasTank>(gasTanks);
            GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(gasGenerators);
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(containers);

            cargoIce = 0.0f;

            foreach(IMyGasGenerator generator in gasGenerators)
            {
                generator.UseConveyorSystem = false;
                generator.Enabled = true;
            }

            foreach(IMyGasTank tank in gasTanks)
            {
                tank.AutoRefillBottles = true;
            }


        }

        bool fullGas()
        {
            foreach(IMyGasTank tank in gasTanks)
            {
                if((float)tank.FilledRatio < 0.95f)
                {
                    return false;
                }
            }
            return true;
        }

        bool haveIce()
        {
            IMyInventory tempInv;
            List<MyInventoryItem> items = new List<MyInventoryItem>();

            foreach(IMyGasGenerator generator in gasGenerators)
            {
                tempInv = generator.GetInventory();
                tempInv.GetItems(items, it => (it.Type.SubtypeId == "Ice"));
                if(items.Count != 0)
                {
                    return true;
                }
                items.Clear();

            }
            foreach(IMyCargoContainer container in containers)
            {
                tempInv = container.GetInventory();
                tempInv.GetItems(items, it => (it.Type.SubtypeId == "Ice"));
                if(items.Count != 0)
                {
                    return true;
                }
                items.Clear();

            }
            return false;
        }
        bool haveIce(IMyTerminalBlock block)
        {
            IMyInventory tempInv;
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            if(block.HasInventory) {
                tempInv = block.GetInventory();
                tempInv.GetItems(items, it => (it.Type.SubtypeId == "Ice"));
                if(items.Count != 0)
                {
                    foreach(MyInventoryItem item in items)
                    {
                        cargoIce += (double)item.Amount;
                    }
                    return true;
                }
            }
            else
            {
                Echo("Error: checking inventory of blocks without inventory");
                return false;
            }
            return false;
        }

        public void updateBlockLists()
        {
            List<IMyGasGenerator> tempGenerators = new List<IMyGasGenerator>();
            List<IMyGasTank> tempGasTanks = new List<IMyGasTank>();
            List<IMyCargoContainer> tempContainers = new List<IMyCargoContainer>();

            GridTerminalSystem.GetBlocksOfType<IMyGasTank>(tempGasTanks);
            GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(tempGenerators);
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(tempContainers);
            if (!tempGasTanks.SequenceEqual(gasTanks))
            {
                foreach (IMyGasTank tank in gasTanks)
                    if (!tempGasTanks.Contains(tank))//tank has been removed from the group
                        gasTanks.Remove(tank);

                foreach (IMyGasTank tank in tempGasTanks)
                    if (!gasTanks.Contains(tank))
                    {
                        tank.Enabled = true;
                        tank.AutoRefillBottles = true;
                        gasTanks.Add(tank);
                    }

            }

            if (!tempGenerators.SequenceEqual(gasGenerators)){
                foreach (IMyGasGenerator generator in gasGenerators)
                    if (!tempGenerators.Contains(generator))
                        gasGenerators.Remove(generator);

                foreach (IMyGasGenerator generator in tempGenerators)
                    if (!gasGenerators.Contains(generator))
                    {
                        generator.UseConveyorSystem = false;
                        generator.Enabled = true;
                        gasGenerators.Add(generator);

                    }
            }

            if (!tempContainers.SequenceEqual(containers))
            {

                foreach (IMyCargoContainer container in containers)
                    if (!tempContainers.Contains(container))
                        containers.Remove(container);

                foreach (IMyCargoContainer container in tempContainers)
                    if (!containers.Contains(container))
                        containers.Add(container);
            }

        }

        public List<IMyCargoContainer> getIceContainers()
        {
            List<IMyCargoContainer> tempContainers = new List<IMyCargoContainer>();

            foreach(IMyCargoContainer container in containers)
            {
                if (haveIce((container as IMyTerminalBlock)))
                    tempContainers.Add(container);
            }

            return tempContainers;
        }

        public IMyInventory getEmptyInventory()
        {
            IMyInventory tempInv;
            IMyInventory backUp = null;
            foreach(IMyCargoContainer container in containers)
            {
                tempInv = container.GetInventory();
                if (((float)tempInv.CurrentVolume / (float)tempInv.MaxVolume) > 0.80)
                    return tempInv;
                else if (!tempInv.IsFull)
                    backUp = tempInv;
            }
            if (backUp != null)
                Echo("All containers are nearly full");
            return backUp;

        }

        public List<MyInventoryItem> getIce(IMyInventory inv)
        {
            List<MyInventoryItem> items = new List<MyInventoryItem>();
            inv.GetItems(items, it => (it.Type.SubtypeId == "Ice"));
            return items;
        }
        public void fillGenerators()
        {

            List<IMyCargoContainer> iceContainers = getIceContainers();

            IMyInventory inventory;
            IMyInventory outInventory;
            List<MyInventoryItem> items = new List<MyInventoryItem>();

            double amount = 0.0f;

            foreach(IMyGasGenerator generator in gasGenerators)
            {
                inventory = generator.GetInventory();
                if ((double)inventory.CurrentVolume >= targetVolume)
                {
                    if ((outInventory = getEmptyInventory()) != null)
                    {
                        items = getIce(inventory);
                        foreach (MyInventoryItem item in items) {
                            amount = ((double)inventory.MaxVolume * 1000.0f) - targetVolume;
                            if (inventory.CanTransferItemTo(outInventory, item.Type))
                            {
                                inventory.TransferItemTo(outInventory, item, (MyFixedPoint)amount);
                                break;
                            }
                            else
                            {
                                Echo("Can't transfer items. Fix your converyor system.");
                            }
                        }
                    }
                    else
                        Echo("Error: tried to transfer from " + generator.CustomName + " but all containers are full");
                }
                else
                {
                    foreach (IMyCargoContainer container in iceContainers)
                    {
                        outInventory = container.GetInventory();
                        items = getIce(outInventory);
                        amount = (targetVolume - ((double)inventory.CurrentVolume*1000));

                        foreach (MyInventoryItem item in items)
                        {
                            if (outInventory.CanTransferItemTo(inventory, item.Type))
                            {
                                inventory.TransferItemFrom(outInventory, item, (MyFixedPoint)amount);
                                break;
                            }
                            else
                            {
                                Echo("Can't transfer items. Fix your converyor system.");
                            }
                        }

                        if(((double)inventory.CurrentVolume >= targetVolume && cargoIce < 10.0f))
                        {
                            break;
                        }


                    }

                }
                items.Clear();

            }

        }
        public void Main(string argument, UpdateType updateSource)
        {

            if (!(targetVolume > 1.0f)) {
               IMyInventory tempInv;
                if (gasGenerators.Count > 0)
                {
                    tempInv = gasGenerators[0].GetInventory();
                    targetVolume = (double)tempInv.MaxVolume * 0.9f*1000.0f;
                }
                else
                {
                    Echo("Script can't work without at least one H2/O2 Generator"); 
                    return;
                }
            }
            updateBlockLists();
            if(!fullGas() && haveIce())
            {
                fillGenerators();
            }

            cargoIce = 0.0f;


        }
    }
}
