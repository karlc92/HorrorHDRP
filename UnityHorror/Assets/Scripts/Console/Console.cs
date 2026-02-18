using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public static class Console
{

    public static List<string> Output = new List<string>();
    private static bool showing = false;
    public static ConsoleUI consoleUI;


    public static string GetOutputString()
    {
        string output = "";
        foreach (string s in Output)
        {
            output += s + "\n";
        }
        return output;
    }

    public static bool IsShowing()
    {
        if (consoleUI == null)
            return false;

        return showing;
    }

    public static void Clear()
    {
        if (consoleUI == null) return;

        consoleUI.ConsoleOutput.text = "";
    }

    public static void ShowConsole()
    {
        if (consoleUI == null) return;

        consoleUI.ConsoleBG.gameObject.SetActive(true);
        consoleUI.ConsoleInput.gameObject.SetActive(true);
        consoleUI.ConsoleOutput.gameObject.SetActive(true);
        consoleUI.ConsoleInput.text = "";
        consoleUI.ConsoleInput.ActivateInputField();
        showing = true;
    }

    public static void HideConsole()
    {
        if (consoleUI == null) return;

        consoleUI.ConsoleBG.gameObject.SetActive(false);
        consoleUI.ConsoleInput.gameObject.SetActive(false);
        consoleUI.ConsoleOutput.gameObject.SetActive(false);
        showing = false;
    }

    static void ParseLine(string input)
    {
        if (input.Contains("?sensitivity"))
        {
            var parse = input.Replace(" ", "");
            parse = parse.Replace("?sensitivity", "");
            if (parse == "")
            {
                Print("Sensitivity is " + Game.Settings.MouseSensitivity);
            }
            else
            {
                try
                {
                    float value = float.Parse(parse, CultureInfo.InvariantCulture.NumberFormat);

                    Print("Set sensitivity to " + value);
                    Game.Settings.MouseSensitivity = value;
                }
                catch
                {
                    Print("Invalid input for sensitivity (usage: sensitivity <float>)");
                }
            }
        }
        else if (input.Contains("?volume"))
        {
            var parse = input.Replace(" ", "");
            parse = parse.Replace("?volume", "");
            if (parse == "")
            {
                Print("Master volume is " + (Game.Settings.MasterVolume * 100));
            }
            else
            {
                try
                {
                    int value = int.Parse(parse, CultureInfo.InvariantCulture.NumberFormat);
                    float valueScaled = value * 0.01f;
                    Print("Set master volume to " + value);
                    Game.Settings.MasterVolume = valueScaled;
                }
                catch
                {
                    Print("Invalid input for master volume (usage: volume <int>)");
                }
            }
        }
        else if (input == "?quit" || input == "?exit")
        {
            Application.Quit();
        }
        else if (input.Contains("?clear"))
        {
            consoleUI.ConsoleOutput.text = "";
            Output.Clear();
        }
        else
        {
            Print("Unknown command. ");
        }
    }

    public static void Print(string input)
    {
        if (consoleUI == null) return;

        Debug.Log(input);

        var clearBlanks = input.Replace(" ", "");
        clearBlanks = clearBlanks.Replace(">><color=white>", "");
        clearBlanks = clearBlanks.Replace("</color>", "");
        if (clearBlanks == "") return;


        Output.Add(input); 
        if (Output.Count > 30)
        {
            Output.RemoveAt(0);
        }

        consoleUI.ConsoleOutput.text = GetOutputString();
    }

    public static void Update()
    {

        if (!IsShowing())
        {
            if (Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame) ShowConsole();

            return;

        }
        else
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) HideConsole();
            if (Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame) HideConsole();
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                if (consoleUI.ConsoleInput.text != "")
                {
                    Print(">><color=white>" + consoleUI.ConsoleInput.text + "</color>");
                    ParseLine("?" + consoleUI.ConsoleInput.text);
                    consoleUI.ConsoleInput.text = "";
                }
                consoleUI.ConsoleInput.ActivateInputField();
            }
        }
    }

}

