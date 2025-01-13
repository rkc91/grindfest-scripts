// ReSharper disable RedundantUsingDirective
using GrindFest;
using GrindFest.Characters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Scripts.States;



namespace Scripts.Utilities
{
    public static class GeneralUtilities
    {
        // navigate to a given FlagBehavior.
        // return: a bool indicating whether flag has been reached.
        public static bool NavigateToFlag(FlagBehaviour flagBehaviour, AutomaticHero hero)
        {
            // check distance to flag
            if (Vector3.Distance(flagBehaviour!.transform.position,
                    hero.transform.position) > 5)
            {
                hero.GoTo(flagBehaviour.transform.position);
                return false; // not close enough
            }
            
            hero.Say($"Arrived at Flag: {flagBehaviour.Index}," +
                $" {flagBehaviour.name.Replace("Flag: ", "")}");
            
            return true;
        }
        
        // returns the appropriate response state to a given sayCommand
        public static States HandleSay(string sayString, AutomaticHero hero)
        {
            // split input string into command and arguments
            var splitStrings = sayString.ToLower().Split(' ');
            
            // first substring is the command to be executed
            switch (splitStrings[0])
            {
                case "stop":
                {
                    hero.StopAllCoroutines(); // stop all behavior
                
                    return Stop;
                }

                case "goto":
                {
                    var flagIndex = int.Parse(splitStrings[1]) - 1; // extract value and adjust for 0 init 
                
                    if (FlagBehaviour.Flags.Count == 0)
                    {
                        Debug.Log("Error: No flags placed.");
                        return Stop;
                    }

                    if (flagIndex < FlagBehaviour.Flags.Count) return Navigate;
                    Debug.Log("Error: No flag exists at this index.");
                    return Stop;

                    //hero._destinationFlag = FlagBehaviour.Flags[flagIndex];

                }

                case "start":
                {
                    return Initial;
                }

                default:
                {
                    Debug.Log($"Error: Unknown command: {sayString}");
                    return Stop;
                }
            }
        }
    }
    
    
}

