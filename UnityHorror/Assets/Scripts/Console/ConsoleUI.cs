using TMPro;
using UnityEngine;

public class ConsoleUI : MonoBehaviour
{
    public TMP_InputField ConsoleInput;
    public TextMeshProUGUI ConsoleOutput;
    public GameObject ConsoleBG;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DontDestroyOnLoad(this);
        Console.consoleUI = this;
        Console.HideConsole();
    }

    // Update is called once per frame
    void Update()
    {
        Console.Update();
    }
}
