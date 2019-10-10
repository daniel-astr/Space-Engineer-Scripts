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
     * Script to rotate solar panels towards the sun
     * when attaced to a rotor system that is in turn attached to another rotor system
     * two groups are needed but an arbitrary amount of rotors can be in each group.
     * first group named "rotors x-1" or whatever you've changed the name to in the Program() constructor
     * and the second group named "rotors x-2" or whatever you've changed the name to in the Program() constructor.
     * Make sure each rotor has the 0 degree angle at the same postion when building.
     * 
     * In this early version that is more of a proof of concept the solar panels
     * only rotate 90 degrees at a time.
     * 
     * you need to set up solar panels that are relatively unobstructed from sunlight with their backside covered by blocks in all six possible cardinal and vertical directions.
     * add those solar panels to a group named "solar direction" or whatever you've changed the named to in the Program() constructor.
     * Below heere are the names suggested for a specific rotation of the rotors. 
     *  (1,0,0)  0, 0
        (-1,0,0) 0, 0
        (0,1,0)  90, 00
        (0,-1,0) 90, 00
        (0,0,1)  00, 90
        (0,0,-1) 00, 90
        */
    partial class Program : MyGridProgram
    {

        public string rotors_1, rotors_2, solar_direction;
        IMyTimerBlock timer;
        public Program()
        {
            //group names
            rotors_1 = "rotors x-1";
            rotors_2 = "rotors x-2";
            solar_direction = "solar direction";
            timer = GridTerminalSystem.GetBlockWithName("Timer Solar") as IMyTimerBlock;
        }

        public void RotatePanels(List<IMySolarPanel> panels, List<IMyMotorAdvancedStator> first_rotors, List<IMyMotorAdvancedStator> second_rotors)
        {
            var highest = 0.0;
            var name_highest = "";

            foreach (var panel in panels)
            {
                if (panel.MaxOutput >= highest)
                {
                    highest = panel.MaxOutput;
                    name_highest = panel.CustomName;
                }

            }
            if (name_highest.Length >= 10)
            {
                name_highest = name_highest.Substring(6, 4);
                Echo($"name coord: {name_highest}");
                /*Echo($"first_rotor length: {first_rotors.Count}");
                foreach (IMyMotorAdvancedStator rotor in second_rotors)
                {
                    Echo($"second_rotor length: {rotor.CustomName}");
                }*/
            }
            else
            {
                Echo("Error: too short name for solar panel - Aborting");
                return;
            }
            int name_length = first_rotors[0].CustomName.Length;
            //Echo($"{first_rotors[0].CustomName.Substring(name_length - 3, 3)}");
            //Echo($"{first_rotors[0].CustomName}");
            if (string.Compare(name_highest.Substring(0, 1), "9") == 0) //rotate the base rotors
            {
                foreach (IMyMotorAdvancedStator rotor in first_rotors)
                {
                    //Echo($"Rotor name: {rotor.CustomName.Substring(name_length - 3, 3)}");
                    if (rotor.CustomName.Substring(name_length - 3, 3) == "Dec")
                    {

                        Echo($"Changed velocity of {rotor.CustomName} to -1.0");
                        rotor.TargetVelocityRPM = (float)-1.0;
                    }
                    else
                    {

                        Echo($"Changed velocity of {rotor.CustomName} to 1.0");
                        rotor.TargetVelocityRPM = (float)1.0;

                    }


                }

            }
            else
            {
                //Echo($"Changed velocity of {first_rotors.Count}");
                foreach (IMyMotorAdvancedStator rotor in first_rotors)
                {
                    //Echo($"Rotor name: {rotor.CustomName.Substring(name_length - 3, 3)}");
                    if (rotor.CustomName.Substring(name_length - 3, 3) == "Dec")
                    {

                        Echo($"Changed velocity of {rotor.CustomName} to 1.0");
                        rotor.TargetVelocityRPM = (float)1.0;

                    }
                    else
                    {

                        Echo($"Changed velocity of {rotor.CustomName} to -1.0");
                        rotor.TargetVelocityRPM = (float)-1.0;

                    }


                }

            }
            Echo($"Changed velocity of rotors");
            name_highest = name_highest.Substring(name_highest.IndexOf(',') + 1, 1);

            if (string.Compare(name_highest.Substring(0, 1), "9") == 0) //rotate the base rotors
            {

                foreach (IMyMotorAdvancedStator rotor in second_rotors)
                {
                    //Echo($"Rotor name: {rotor.CustomName.Substring(name_length - 3, 3)}");
                    if (rotor.CustomName.Substring(name_length - 3, 3) == "Dec")
                    {
                        rotor.TargetVelocityRPM = (float)-1.0;

                    }
                    else
                    {
                        rotor.TargetVelocityRPM = (float)1.0;

                    }


                }

            }
            else
            {
                foreach (IMyMotorAdvancedStator rotor in second_rotors)
                {
                    //Echo($"Rotor name: {rotor.CustomName.Substring(name_length - 3, 3)}");
                    if (rotor.CustomName.Substring(name_length - 3, 3) == "Dec")
                    {

                        rotor.TargetVelocityRPM = (float)1.0;

                    }
                    else
                    {

                        rotor.TargetVelocityRPM = (float)-1.0;

                    }


                }

            }
        }
        public void Main()
        {
            IMyBlockGroup first_rotor_group = GridTerminalSystem.GetBlockGroupWithName(rotors_1);
            IMyBlockGroup second_rotor_group = GridTerminalSystem.GetBlockGroupWithName(rotors_2);
            IMyBlockGroup solar_level = GridTerminalSystem.GetBlockGroupWithName(solar_direction);

            List<IMyMotorAdvancedStator> first_rotors = new List<IMyMotorAdvancedStator>();
            List<IMyMotorAdvancedStator> second_rotors = new List<IMyMotorAdvancedStator>();
            List<IMySolarPanel> panel_level = new List<IMySolarPanel>();

            foreach (var temp in new List<IMyBlockGroup> { first_rotor_group, second_rotor_group, solar_level })
            {
                if (temp == null)
                {
                    Echo("A need group does not exist, aborting");
                    return;
                }
                /*else
                {
                    Echo($"{temp.Name} initialized...");
                }*/
            }

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            first_rotor_group.GetBlocks(blocks);
            foreach (var block in blocks)
            {
                if (block is IMyMotorAdvancedStator)
                {
                    first_rotors.Add(block as IMyMotorAdvancedStator);
                }
                else
                {
                    Echo($"Wrong type of item in {first_rotor_group.Name}");
                }
            }
            blocks.Clear();
            second_rotor_group.GetBlocks(blocks);

            foreach (var block in blocks)
            {
                if (block is IMyMotorAdvancedStator)
                {
                    second_rotors.Add(block as IMyMotorAdvancedStator);
                }
                else
                {
                    Echo($"Wrong type of item in {second_rotor_group.Name}");
                }
            }
            blocks.Clear();
            solar_level.GetBlocks(blocks);

            foreach (var block in blocks)
            {
                if (block is IMySolarPanel)
                {
                    panel_level.Add(block as IMySolarPanel);
                }
                else
                {
                    Echo($"Wrong type of item in {solar_level.Name}");
                }
            }
            blocks.Clear();

            RotatePanels(panel_level, first_rotors, second_rotors);
            timer.StartCountdown();

        }
    }
}
