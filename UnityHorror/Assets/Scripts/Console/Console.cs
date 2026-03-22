using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;

public static class Console
{
    public static List<string> Output = new List<string>();
    static bool showing;
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
                Print("Sensitivity is " + Game.Settings.MouseSensitivity.ToString(CultureInfo.InvariantCulture));
            }
            else if (float.TryParse(parse, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                Game.Settings.MouseSensitivity = value;
                Print("Set sensitivity to " + value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                Print("Invalid input for sensitivity (usage: sensitivity <float>)");
            }
        }
        else if (input.Contains("?volume"))
        {
            var parse = input.Replace(" ", "");
            parse = parse.Replace("?volume", "");
            if (parse == "")
            {
                Print("Master volume is " + (Game.Settings.MasterVolume * 100f).ToString("0.##", CultureInfo.InvariantCulture));
            }
            else if (int.TryParse(parse, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                Game.Settings.MasterVolume = value * 0.01f;
                Print("Set master volume to " + value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                Print("Invalid input for master volume (usage: volume <int>)");
            }
        }
        else if (input.Contains("?subtitles"))
        {
            var parse = input.Replace(" ", "");
            parse = parse.Replace("?subtitles", "");
            if (parse == "")
            {
                Print("Subtitles is " + Game.Settings.Subtitles);
            }
            else if (bool.TryParse(parse, out bool value))
            {
                Game.Settings.Subtitles = value;
                Print("Set subtitles to " + value);
            }
            else
            {
                Print("Invalid input for subtitles (usage: subtitles <true|false>)");
            }
        }
        else if (input.Contains("?language"))
        {
            var parse = input.Replace(" ", "");
            parse = parse.Replace("?language", "");
            if (parse == "")
            {
                Print("Language is " + Game.Settings.Language);
            }
            else if (Enum.TryParse(parse, true, out Languages value))
            {
                Game.Settings.Language = value;
                Print("Set language to " + value);
            }
            else
            {
                Print("Invalid input for language (usage: language <language>)");
            }
        }
        else if (input.Contains("?nonenglishaudio"))
        {
            var parse = input.Replace(" ", "");
            parse = parse.Replace("?nonenglishaudio", "");
            if (parse == "")
            {
                Print("NonEnglishAudio is " + Game.Settings.UseNonEnglishDialogueAudio);
            }
            else if (bool.TryParse(parse, out bool value))
            {
                Game.Settings.UseNonEnglishDialogueAudio = value;
                Print("Set nonenglishaudio to " + value);
            }
            else
            {
                Print("Invalid input for nonenglishaudio (usage: nonenglishaudio <true|false>)");
            }
        }
        else if (input.Contains("?fps"))
        {
            var parse = input.Replace(" ", "");
            parse = parse.Replace("?fps", "");
            if (parse == "")
            {
                Print("Fps is " + Game.Settings.Video.TargetFrameRate.ToString(CultureInfo.InvariantCulture));
            }
            else if (int.TryParse(parse, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                Game.Settings.Video.TargetFrameRate = value;
                Print("Set fps to " + value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                Print("Invalid input for fps (usage: fps <int>)");
            }
        }
        else if (input.Contains("?resolution"))
        {
            var parse = input.Replace(" ", "");
            parse = parse.Replace("?resolution", "");
            if (parse == "")
            {
                int width = Game.Settings.Video.ResolutionWidth > 0 ? Game.Settings.Video.ResolutionWidth : Screen.currentResolution.width;
                int height = Game.Settings.Video.ResolutionHeight > 0 ? Game.Settings.Video.ResolutionHeight : Screen.currentResolution.height;
                Print("Resolution is " + width + "x" + height);
            }
            else
            {
                string[] parts = parse.Split('x');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int width) &&
                    int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int height))
                {
                    Game.Settings.Video.ResolutionWidth = width;
                    Game.Settings.Video.ResolutionHeight = height;
                    Print("Set resolution to " + width + "x" + height);
                }
                else
                {
                    Print("Invalid input for resolution (usage: resolution <width>x<height>)");
                }
            }
        }
        else if (input.Contains("?refreshrate"))
        {
            var parse = input.Replace(" ", "");
            parse = parse.Replace("?refreshrate", "");
            if (parse == "")
            {
                int refreshRate = Game.Settings.Video.RefreshRate > 0 ? Game.Settings.Video.RefreshRate : Screen.currentResolution.refreshRate;
                Print("RefreshRate is " + refreshRate.ToString(CultureInfo.InvariantCulture));
            }
            else if (int.TryParse(parse, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                Game.Settings.Video.RefreshRate = value;
                Print("Set refreshrate to " + value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                Print("Invalid input for refreshrate (usage: refreshrate <int>)");
            }
        }
        else if (input.Contains("?fullscreenmode"))
        {
            var parse = input.Replace(" ", "");
            parse = parse.Replace("?fullscreenmode", "");
            if (parse == "")
            {
                Print("FullscreenMode is " + Game.Settings.Video.FullScreenMode);
            }
            else if (Enum.TryParse(parse, true, out FullScreenMode value))
            {
                Game.Settings.Video.FullScreenMode = value;
                Print("Set fullscreenmode to " + value);
            }
            else
            {
                Print("Invalid input for fullscreenmode (usage: fullscreenmode <mode>)");
            }
        }
        else if (input.Contains("?vsync"))
        {
            var parse = input.Replace(" ", "");
            parse = parse.Replace("?vsync", "");
            if (parse == "")
            {
                Print("VSync is " + Game.Settings.Video.UseVSync);
            }
            else if (bool.TryParse(parse, out bool value))
            {
                Game.Settings.Video.UseVSync = value;
                Print("Set vsync to " + value);
            }
            else
            {
                Print("Invalid input for vsync (usage: vsync <true|false>)");
            }
        }
        else if (input.Contains("?vsynccount"))
        {
            var parse = input.Replace(" ", "");
            parse = parse.Replace("?vsynccount", "");
            if (parse == "")
            {
                Print("VSyncCount is " + Game.Settings.Video.VSyncCount.ToString(CultureInfo.InvariantCulture));
            }
            else if (int.TryParse(parse, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                Game.Settings.Video.VSyncCount = value;
                Print("Set vsynccount to " + value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                Print("Invalid input for vsynccount (usage: vsynccount <int>)");
            }
        }
        else if (input.Contains("?renderscale"))
        {
            var parse = input.Replace(" ", "");
            parse = parse.Replace("?renderscale", "");
            if (parse == "")
            {
                Print("RenderScale is " + Game.Settings.Video.RenderScale.ToString(CultureInfo.InvariantCulture));
            }
            else if (float.TryParse(parse, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                Game.Settings.Video.RenderScale = value;
                Print("Set renderscale to " + value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                Print("Invalid input for renderscale (usage: renderscale <float>)");
            }
        }
        else if (input.Contains("?texturequality"))
        {
            var parse = input.Replace(" ", "");
            parse = parse.Replace("?texturequality", "");
            if (parse == "")
            {
                Print("TextureQuality is " + Game.Settings.Video.TextureQuality);
            }
            else if (Enum.TryParse(parse, true, out TextureQuality value))
            {
                Game.Settings.Video.TextureQuality = value;
                Print("Set texturequality to " + value);
            }
            else
            {
                Print("Invalid input for texturequality (usage: texturequality <quality>)");
            }
        }
        else if (input.Contains("?shadowquality"))
        {
            var parse = input.Replace(" ", "");
            parse = parse.Replace("?shadowquality", "");
            if (parse == "")
            {
                Print("ShadowQuality is " + Game.Settings.Video.ShadowQuality);
            }
            else if (Enum.TryParse(parse, true, out ShadowQuality value))
            {
                Game.Settings.Video.ShadowQuality = value;
                Print("Set shadowquality to " + value);
            }
            else
            {
                Print("Invalid input for shadowquality (usage: shadowquality <quality>)");
            }
        }
        else if (input.Contains("?anisotropicfiltering"))
        {
            var parse = input.Replace(" ", "");
            parse = parse.Replace("?anisotropicfiltering", "");
            if (parse == "")
            {
                Print("AnisotropicFiltering is " + Game.Settings.Video.AnisotropicFiltering);
            }
            else if (Enum.TryParse(parse, true, out AnisotropicFiltering value))
            {
                Game.Settings.Video.AnisotropicFiltering = value;
                Print("Set anisotropicfiltering to " + value);
            }
            else
            {
                Print("Invalid input for anisotropicfiltering (usage: anisotropicfiltering <mode>)");
            }
        }
        else if (input.StartsWith("?bind", StringComparison.OrdinalIgnoreCase))
        {
            string parse = input.Substring(5).Trim();
            if (parse == "")
            {
                return;
            }

            string[] parts = parse.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                if (TryGetBindingPathFromKey(parts[0], out string keyPath))
                {
                    string command = GetCommandForBindingPath(keyPath);
                    Print(parts[0].ToLowerInvariant() + " is " + (command ?? "unbound"));
                }
                else
                {
                    Print("Invalid input for bind (usage: bind <key> <command>)");
                }
            }
            else if (parts.Length >= 2)
            {
                if (TryGetBindingPathFromKey(parts[0], out string keyPath))
                {
                    string command = parts[1].ToLowerInvariant();
                    if (TrySetBinding(command, keyPath))
                    {
                        Print("Set bind " + parts[0].ToLowerInvariant() + " to " + command);
                    }
                    else
                    {
                        Print("Invalid input for bind (usage: bind <key> <command>)");
                    }
                }
                else
                {
                    Print("Invalid input for bind (usage: bind <key> <command>)");
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

    static bool TrySetBinding(string command, string keyPath)
    {
        if (command == "moveforward")
        {
            Game.Settings.Input.MoveUp = keyPath;
            return true;
        }
        if (command == "movebackward")
        {
            Game.Settings.Input.MoveDown = keyPath;
            return true;
        }
        if (command == "moveleft")
        {
            Game.Settings.Input.MoveLeft = keyPath;
            return true;
        }
        if (command == "moveright")
        {
            Game.Settings.Input.MoveRight = keyPath;
            return true;
        }
        if (command == "sprint")
        {
            Game.Settings.Input.SprintKeyboard = keyPath;
            return true;
        }
        if (command == "crouch")
        {
            Game.Settings.Input.CrouchKeyboard = keyPath;
            return true;
        }
        if (command == "interact")
        {
            Game.Settings.Input.InteractKeyboard = keyPath;
            return true;
        }
        if (command == "drop")
        {
            Game.Settings.Input.DropKeyboard = keyPath;
            return true;
        }
        if (command == "jump")
        {
            Game.Settings.Input.JumpKeyboard = keyPath;
            return true;
        }

        return false;
    }

    static string GetCommandForBindingPath(string keyPath)
    {
        if (string.Equals(Game.Settings.Input.MoveUp, keyPath, StringComparison.OrdinalIgnoreCase)) return "moveforward";
        if (string.Equals(Game.Settings.Input.MoveDown, keyPath, StringComparison.OrdinalIgnoreCase)) return "movebackward";
        if (string.Equals(Game.Settings.Input.MoveLeft, keyPath, StringComparison.OrdinalIgnoreCase)) return "moveleft";
        if (string.Equals(Game.Settings.Input.MoveRight, keyPath, StringComparison.OrdinalIgnoreCase)) return "moveright";
        if (string.Equals(Game.Settings.Input.SprintKeyboard, keyPath, StringComparison.OrdinalIgnoreCase)) return "sprint";
        if (string.Equals(Game.Settings.Input.CrouchKeyboard, keyPath, StringComparison.OrdinalIgnoreCase)) return "crouch";
        if (string.Equals(Game.Settings.Input.InteractKeyboard, keyPath, StringComparison.OrdinalIgnoreCase)) return "interact";
        if (string.Equals(Game.Settings.Input.DropKeyboard, keyPath, StringComparison.OrdinalIgnoreCase)) return "drop";
        if (string.Equals(Game.Settings.Input.JumpKeyboard, keyPath, StringComparison.OrdinalIgnoreCase)) return "jump";
        return null;
    }

    static bool TryGetBindingPathFromKey(string key, out string keyPath)
    {
        keyPath = null;
        string normalized = key.Trim().ToLowerInvariant().Replace("_", "").Replace("-", "");

        if (normalized == "space" || normalized == "spacebar") keyPath = "<Keyboard>/space";
        else if (normalized == "shift" || normalized == "leftshift" || normalized == "lshift") keyPath = "<Keyboard>/leftShift";
        else if (normalized == "rightshift" || normalized == "rshift") keyPath = "<Keyboard>/rightShift";
        else if (normalized == "ctrl" || normalized == "control" || normalized == "leftctrl" || normalized == "lctrl") keyPath = "<Keyboard>/leftCtrl";
        else if (normalized == "rightctrl" || normalized == "rctrl") keyPath = "<Keyboard>/rightCtrl";
        else if (normalized == "alt" || normalized == "leftalt" || normalized == "lalt") keyPath = "<Keyboard>/leftAlt";
        else if (normalized == "rightalt" || normalized == "ralt") keyPath = "<Keyboard>/rightAlt";
        else if (normalized == "enter" || normalized == "return") keyPath = "<Keyboard>/enter";
        else if (normalized == "escape" || normalized == "esc") keyPath = "<Keyboard>/escape";
        else if (normalized == "backquote" || normalized == "tilde") keyPath = "<Keyboard>/backquote";
        else if (normalized == "up" || normalized == "uparrow") keyPath = "<Keyboard>/upArrow";
        else if (normalized == "down" || normalized == "downarrow") keyPath = "<Keyboard>/downArrow";
        else if (normalized == "left" || normalized == "leftarrow") keyPath = "<Keyboard>/leftArrow";
        else if (normalized == "right" || normalized == "rightarrow") keyPath = "<Keyboard>/rightArrow";
        else if (normalized.Length == 1 && char.IsLetterOrDigit(normalized[0])) keyPath = "<Keyboard>/" + normalized;
        else if (normalized.Length > 1 && normalized[0] == 'f' && int.TryParse(normalized.Substring(1), out int fn) && fn >= 1 && fn <= 12) keyPath = "<Keyboard>/f" + fn.ToString(CultureInfo.InvariantCulture);

        return keyPath != null;
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
