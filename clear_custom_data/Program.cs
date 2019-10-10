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



        Dictionary<string, int> containerCount = new Dictionary<string, int>();

        string groupName;
        string gridName;
        string tempString;
        bool checkGrid;
        public Program()
        {
            groupName = "InvManageGroup";
            gridName = ""; //main grid name
            tempString = "";
            checkGrid = false;
        }

        public void Save()
        {
        }
        

        public void clearCargoInfo(List<IMyCargoContainer> containers) //could change to IMyTerminalBlock and make it general
        {
            containerCount.Clear();

            foreach (IMyCargoContainer container in containers)
            {
                if(checkGrid && !(container.CubeGrid.ToString() == gridName))//if only grab from same grid but container is not the same grid
                {
                    continue; //then ignore below and go to next container
                }
                Echo("Clearing container " + container.CustomName);
                tempString = container.BlockDefinition.SubtypeId;
                container.CustomData = "";

                if (containerCount.ContainsKey(tempString))
                    containerCount[tempString]++;
                else
                    containerCount.Add(tempString, 1);

                container.CustomName = tempString + " " + containerCount[tempString];
                Echo("New name: " + container.CustomName);
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            List<IMyTextPanel> panels = new List<IMyTextPanel>();
            List<IMyCargoContainer> containers = new List<IMyCargoContainer>();
            IMyBlockGroup group;

            group = GridTerminalSystem.GetBlockGroupWithName(groupName);
            if (group != null)
            {
                group.GetBlocks(blocks);
                foreach(IMyTerminalBlock block in blocks)
                {
                    checkGrid = true;
                    gridName = block.CubeGrid.ToString();
                    Echo("will only take blocks from grid " + gridName);
                    break;
                }
            }
            else
            {

            }

            GridTerminalSystem.GetBlocksOfType(panels, blk => (blk is IMyTextPanel));
            foreach(IMyTextPanel panel in panels)
            {
                panel.CustomData = "";
            }
            Echo("cleared custom data from " + panels.Count.ToString() + " panels.");
            GridTerminalSystem.GetBlocksOfType(containers, blk => (blk is IMyCargoContainer));
            clearCargoInfo(containers);  
        }
    }
}
