using KeepCoding;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HexNullTPScript : TPScript<HexNullScript>
{
    // pretty much just yoinked this part from hexOrbits with a few modifications
    public override IEnumerator ProcessTwitchCommand(string command)
    {
        string[] split = command.Split();

        if (IsMatch(split[0], "reset"))
        {
            yield return null;
            if (split.Length != 1)
                yield return SendToChatError("Too many parameters!");
            else
                Module.Screen.OnInteract();
        }

        else if (IsMatch(split[0], "press"))
        {
            yield return null;
            const string validChars = "01lr";

            if (split.Length != 2)
                yield return SendToChatError(split.Length < 2 ? "You need to specify an input!" : "Too many parameters!");
            else if (split[1].Length != 3)
                yield return SendToChatError("Expected 3 inputs as the parameter.");
            else if (split[1].Any(c => !validChars.Contains(c.ToLower())))
                yield return SendToChatError("Expected all characters to be 0/L or 1/R!");
            else
            {
                int firstPress = validChars.IndexOf(split[1][0].ToLower()) % 2,
                    secondPress = validChars.IndexOf(split[1][1].ToLower()) % 2,
                    thirdPress = validChars.IndexOf(split[1][2].ToLower()) % 2;

                StartCoroutine(PushButtons(firstPress, secondPress, thirdPress));
            }

        }
    }

    public override IEnumerator TwitchHandleForcedSolve()
    {
        /*
        this autosolve is way more complex than hexOrbits' "press two buttons, solve module" - let's explain it:
        step 1: all required digits are parsed into an array, which can have digits removed from it
        step 2: entity position is taken, and compared against the current array - if the entity needs to move, press the monitor and repeat
        step 3: remove the entity position from the array, and go to any position still in the array (take the position, convert to binary, parse into !# press)
        remove player position from the array, and repeat once from step 2 - module will be solved in ~1.5 second from command usage

        oh god, i have to make this autosolve from any player position...
        
        edit: it's fine! :) - genuinely though, just perform an XOR operation on each index between player submission and solution, and you know exactly what to change
        */

        List<int> requiredIndexes = new List<int> { };
        for (int i = 0; i <= 7; i++)
        {
            if (Module.submission[i] != Module.solution[i])
                requiredIndexes.Add(i);
        }

        while (requiredIndexes.Count > 0)
        {
            Module.Screen.OnInteract();

            if (requiredIndexes.Contains(Module.entityPosition))
            {
                requiredIndexes.Remove(Module.entityPosition);

                int[] binaryParse = ToBinary(requiredIndexes[0]);

                Module.Buttons[binaryParse[0]].OnInteract();
                yield return new WaitForSecondsRealtime(0.2f);
                Module.Buttons[binaryParse[1]].OnInteract();
                yield return new WaitForSecondsRealtime(0.2f);
                Module.Buttons[binaryParse[2]].OnInteract();
                yield return new WaitForSecondsRealtime(0.2f);

                requiredIndexes.RemoveAt(0);
            }
            yield return new WaitForSecondsRealtime(0.1f);
        }
        Module.Screen.OnInteract();
        int[] binarySolve = ToBinary(Module.entityPosition);
        StartCoroutine(PushButtons(binarySolve[0], binarySolve[1], binarySolve[2]));
        while (!Module.IsSolved)
            yield return true;
    }

    private IEnumerator PushButtons(int firstPress, int secondPress, int thirdPress)
    {
        Module.Buttons[firstPress].OnInteract();
        yield return new WaitForSecondsRealtime(0.2f);
        Module.Buttons[secondPress].OnInteract();
        yield return new WaitForSecondsRealtime(0.2f);
        Module.Buttons[thirdPress].OnInteract();
        yield return new WaitForSecondsRealtime(0.2f);
    }

    private int[] ToBinary(int input)
    {
        return new int[3] { input / 4, (input / 2) % 2, input % 2};
    }
}
