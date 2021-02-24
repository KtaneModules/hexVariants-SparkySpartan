using EmikBaseModules;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class HexOrbitsTPScript : TPScript {

    public HexOrbitsScript Hex;
    internal override ModuleScript ModuleScript
    {
        get
        {
            return Hex;
        }
    }

    internal override string TwitchHelpMessage { get { return @" !{0} press (Presses the screen once) | !{0} cycle <#> (Cycles through the four patterns, pressing the screen every # seconds (max 20, default 10)) | !{0} submit <##> (where # is LDUR, example: !{0} LD presses left, then Down respectively)"; } }

    internal override IEnumerator ProcessTwitchCommand(string command)
    {
        string[] split = command.Split();

        if (IsMatch(split[0], "press"))
        {
            yield return null;
            if (split.Length != 1)
                yield return SendToChatError("Too many parameters!");
            else
                Hex.Screen.OnInteract();
        }

        else if (IsMatch(split[0], "cycle"))
        {
            yield return null;
            if (split.Length > 2)
                yield return SendToChatError("Too many parameters!");

            else
            {
                int time = 10;

                try
                {
                    if (split.Skip(1).ToArray().ToNumbers(min: 0) != null)
                        time = split.Skip(1).ToArray().ToNumbers()[0];
                }
                catch { }

                for (int i = 0; i < 3; i++)
                {
                    Hex.Screen.OnInteract();
                    yield return new WaitForSecondsRealtime(Math.Min(time, 20));
                }
                Hex.Screen.OnInteract();
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

                yield return PushButtons(firstPress, secondPress);
            }
        }
    }

    internal override IEnumerator TwitchHandleForcedSolve()
    {
        yield return PushButtons(Hex.stageValues[4] / 4, Hex.stageValues[4] % 4);
    }

    private IEnumerator PushButtons(int firstPress, int secondPress)
    {
        Hex.Buttons[firstPress].OnInteract();
        yield return new WaitForSecondsRealtime(0.5f);
        Hex.Buttons[secondPress].OnInteract();
    }
}
