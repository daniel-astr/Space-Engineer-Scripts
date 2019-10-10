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
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.
        // 
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set Runtime.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from. Be aware that the
            // updateSource is a  bitfield  and might contain more than 
            // one update type.
            // 
            // The method itself is required, but the arguments above
            // can be removed if not needed.
        }
    }
}
namespace IngameScript
{
    /*
    * Script to cycle airlocks
    * 
    * Script is meant to cycle airlocks with doors of any normal type.
    * 
    * Usage:
    * Create a group with one timer block, the doors, vents, and optionally lights you wish to use. 
    * The vents should be connected to oxygen tanks that are not completely full.
    * It's preferable if you have a light outside every door with "In" and "Out" in their names. 
    * Doors also need to have "In" or "Out" in their name. The "Out" and "In" are arbitrary but should be consistent between all doors and lights.
    * 
    * Once set up:
    * 1. Add script to programming block
    * 
    * 2. Have button panel on the inside, outside and indoors area. Connect one button to run the programming block
    * with the argument "cycle <group_name> <direction>"
    * <group_name> should be the group name you've used for all equipment without the '<' and '>'.
    * <direction> is the direction you are coming from and should be either "In", "Out" or "Inside".
    * It is case sensitive. a normal argument looks like "cycle AirlockMain In" without the quotation marks.
    * 
    * 3. The timer block should be set to run the programming block after 2 seconds with the argument <group_name> where <group_name> is replaced with the name of the group used.

    */
    partial class Program : MyGridProgram
    {

        MyCommandLine _commandLine = new MyCommandLine();
        Dictionary<string, Action> _commands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);
        int count;

        //public Program()
        public Program()
        {
            _commands["cycle"] = Cycle; //add more, reset and whatever
            count = 0;
            // _commands["AirSwitch"] = AirSwitch;
            // _commands["OpenDoor"] = OpenDoor;
        }
        public void OpenDoor(List<IMyDoor> doors, List<IMyInteriorLight> lights, List<IMyAirVent> vents, IMyTimerBlock timer, string direction)
        {

            if (direction == "Out") //twice as  many if statements as needed, here because of previous bugs
            {
                foreach (IMyAirVent vent in vents)
                {
                    vent.Depressurize = true;
                }

            }
            else //assumes direction is "In"
            {
                foreach (IMyAirVent vent in vents)
                {
                    vent.Depressurize = false;
                }

            }

            if (direction == "In" && vents[0].Status != VentStatus.Pressurized)
            {
                if (count <= 2)
                {
                    timer.CustomData = $"OpenDoor {direction}";
                    timer.StartCountdown();
                    Echo("pressurizing...");
                    count++;
                    return;


                }
                else
                {
                    Echo("taking too long to pressurize, opening doors");
                    count = 0;
                }

            }

            if (direction == "Inside")
            {
                Echo("Error: Inside as direction in OpenDoor()");
                return;
            }

            Lights_Done(lights, direction);



            foreach (IMyDoor door in doors)
            {
                if (door.CustomName.Contains("In"))
                {
                    if (direction == "In")
                    {
                        door.Enabled = true;
                        door.OpenDoor();
                    }
                }
                else
                {
                    if (direction == "Out")
                    {
                        door.Enabled = true;
                        door.OpenDoor();
                    }
                    //Echo($"Door is not in, has {door.CustomName} as name");
                }

            }

        }

        public void AirSwitch(List<IMyDoor> doors, List<IMyInteriorLight> lights, List<IMyAirVent> vents, IMyTimerBlock timer, string direction)
        {

            foreach (IMyDoor door in doors)
            {
                if (door.Enabled == true)
                {
                    door.Enabled = false;
                }
            }

            foreach (IMyAirVent vent in vents)
            {
                if (direction == "Out")
                {
                    vent.Depressurize = true;
                }
                else if (direction == "In")
                {
                    vent.Depressurize = true;
                }
                else if (direction == "Inside" && vent.GetOxygenLevel() > 0.1)
                {
                    vent.Depressurize = true;
                    direction = "Out";
                }
                else
                {
                    vent.Depressurize = false;
                    direction = "In";
                }

            }

            timer.CustomData = $"OpenDoor {direction}";
            timer.StartCountdown(); //The timer will have the group name added

        }

        public void Lights_Cycle(List<IMyInteriorLight> lights) //in_light and out_list are relative. the one called as in_light will always be red until air_pressure is flipped
        {
            foreach (IMyInteriorLight light in lights)
            {
                light.Color = Color.Red;
            }

        }

        //need to have direction for lights done to change all but opposite direction green
        public void Lights_Done(List<IMyInteriorLight> lights, string direction) //in_light and out_list are relative. the one called as in_light will always be red until air_pressure is flipped
        {
            if (String.Compare(direction, "Out") == 1)
            { //flip value of direction for easier programming. not guaranteed to work
                direction = "In";
            }
            else
            {
                direction = "Out";
            }
            foreach (IMyInteriorLight light in lights)
            {
                if (!light.CustomName.Contains(direction))
                {
                    light.Color = Color.Green;
                }
            }

        }
        public void Cycle()
        {

            // Check if the "emergency" switch is set.
            string direction = _commandLine.Argument(2);

            // Argument no. 1 is the name of the airlock to cycle
            string airlockName = _commandLine.Argument(1);

            IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(airlockName);

            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

            List<IMyDoor> doors = new List<IMyDoor>();
            List<IMyInteriorLight> lights = new List<IMyInteriorLight>(); //all lights change color to red, for OpenDoors() all lights will be green except for the one outside the door at the opposite direction ie out -> in
            List<IMyAirVent> vents = new List<IMyAirVent>();
            IMyTimerBlock timer = null;

            group.GetBlocks(blocks);
            foreach (var block in blocks)
            {
                if (block is IMyInteriorLight)
                {
                    lights.Add(block as IMyInteriorLight);
                }
                else if (block is IMyAirVent)
                {
                    vents.Add(block as IMyAirVent);
                }
                else if (block is IMyDoor)
                {
                    doors.Add(block as IMyDoor);
                }
                else if (block is IMyTimerBlock)
                {
                    timer = block as IMyTimerBlock;
                }
                else
                {
                    Echo($"supposed object not added is of type: {block.BlockDefinition}");
                }
            }

            if (vents.Count == 0) //todo add support for N amount of vents
            {
                Echo("No vent found");
                return;
            }

            if (doors.Count < 2) //todo add support for any type of door both in and out
            {
                if (doors.Count == 0)
                {
                    Echo("No doors found");
                }
                else
                {
                    Echo("Not enough doors found");
                }
                return;
            }
            if (timer == null)
            {
                return;
            }
            if (lights.Count > 0)
            {
                Lights_Cycle(lights);
            }
            else
            {
                Echo("No lights detected, returnning");
                return;
            }


            foreach (IMyDoor door in doors)
            {
                if (door.Status != DoorStatus.Closed && door.Status != DoorStatus.Closing)
                {
                    door.CloseDoor();
                }

            }

            timer.CustomData = $"AirSwitch {direction}";
            timer.StartCountdown(); //The timer will have the group name added            
        }




        public void Main(string argument)
        {
            if (_commandLine.TryParse(argument))
            {
                Action commandAction;

                // Retrieve the first argument. Switches are ignored.
                string command = _commandLine.Argument(0);
                if (command == null)
                {
                    Echo("No command specified");
                }
                else if (_commands.TryGetValue(_commandLine.Argument(0), out commandAction))
                {
                    // We have found a command. Invoke it.
                    commandAction();
                }
                else //else there should be a group with a timer block as argument.
                {


                    IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(command);

                    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

                    List<IMyDoor> doors = new List<IMyDoor>();
                    List<IMyInteriorLight> lights = new List<IMyInteriorLight>(); //all lights change color to red, for OpenDoors() all lights will be green except for the one outside the door at the opposite direction ie out -> in
                    List<IMyAirVent> vents = new List<IMyAirVent>();
                    IMyTimerBlock timer = null;

                    group.GetBlocks(blocks);


                    foreach (var block in blocks)
                    {
                        if (block is IMyInteriorLight)
                        {
                            lights.Add(block as IMyInteriorLight);
                        }
                        else if (block is IMyAirVent)
                        {
                            vents.Add(block as IMyAirVent);
                        }
                        else if (block is IMyDoor)
                        {
                            doors.Add(block as IMyDoor);
                        }
                        else if (block is IMyTimerBlock)
                        {
                            timer = block as IMyTimerBlock;
                        }
                        else
                        {
                            Echo($"supposed object not added is of type: {block.BlockDefinition}");
                        }
                    }
                    if (timer == null)
                    {
                        Echo("no timer found, returning");
                        return;
                    }
                    String[] arguments = timer.CustomData.Split();

                    if (arguments.Length != 2)
                    {
                        Echo("custom data is not with two inputs");
                        if (arguments.Length > 0)
                        {
                            foreach (String argu in arguments)
                            {
                                Echo($"Inputs: {argu}");
                            }
                        }
                        return;
                    }

                    if (String.Compare(arguments[0], "AirSwitch") == 0)
                    {
                        AirSwitch(doors, lights, vents, timer, arguments[1]);
                    }
                    else if (String.Compare(arguments[0], "OpenDoor") == 0 && String.Compare(arguments[1], "Inside") != 0)
                    {
                        OpenDoor(doors, lights, vents, timer, arguments[1]);
                    }
                    else if (String.Compare(arguments[1], "In") == 0 && string.Compare(arguments[1], "Inside") == 0 && string.Compare(arguments[1], "Out") == 0)
                    {
                        Echo("Invalid direction, returning");
                        return;
                    }
                    else
                    {
                        Echo("boy you fucked up with the commands");
                    }



                }
            }

        }

    }

}
