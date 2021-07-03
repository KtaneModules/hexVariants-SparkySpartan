using KeepCoding;
using System.Collections;
using System.Linq;
using UnityEngine;

public class HexOrbitsTPScript : TPScript<HexOrbitsScript>
{
    public override IEnumerator ProcessTwitchCommand(string command)
    {
        string[] split = command.Split();

        if (IsMatch(split[0], "press"))
        {
            yield return null;
            if (split.Length != 1)
                yield return SendToChatError("Too many parameters!");
            else
                Module.Screen.OnInteract();
        }

        else if (IsMatch(split[0], "cycle"))
        {
            yield return null;
            if (split.Length > 2)
                yield return SendToChatError("Too many parameters!");

            else
            {
                int time = 3;

                if (split.Skip(1).ToArray().ToNumbers(min: 0) != null)
                    time = split.Skip(1).ToArray().ToNumbers()[0];

                for (int i = 0; i < 3; i++)
                {
                    Module.Screen.OnInteract();
                    yield return new WaitForSecondsRealtime(Mathf.Clamp(time, 1, 10));
                }
                Module.Screen.OnInteract();
            }
        }

        else if (IsMatch(split[0], "submit"))
        {
            yield return null;
            const string validChars = "urdl";

            if (split.Length != 2)
                yield return SendToChatError(split.Length < 2 ? "You need to specify an input!" : "Too many parameters!");
            else if (split[1].Length != 2)
                yield return SendToChatError("Expected 2 inputs as the parameter.");
            else if (split[1].Any(c => !validChars.Contains(c.ToLower())))
                yield return SendToChatError("Expected both characters to be L, D, U, or R!");
            else
            {
                int firstPress = validChars.IndexOf(split[1][0].ToLower()),
                    secondPress = validChars.IndexOf(split[1][1].ToLower());

                StartCoroutine(PushButtons(firstPress, secondPress));
            }
        }
    }

    public override IEnumerator TwitchHandleForcedSolve()
    {
        StartCoroutine(PushButtons(Module.stageValues[4] / 4, Module.stageValues[4] % 4));
        while (!Module.IsSolved)
            yield return true;
    }

    private IEnumerator PushButtons(int firstPress, int secondPress)
    {
        Module.Buttons[firstPress].OnInteract();
        yield return new WaitForSecondsRealtime(0.5f);
        Module.Buttons[secondPress].OnInteract();
    }
}

