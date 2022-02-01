using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;
using MSebastien.Core.Singletons;

public class Logger : Singleton<Logger>
{
    [SerializeField]
    private TextMeshProUGUI debugAreaText = null;

    [SerializeField]
    private bool enableDebug = false;

    [SerializeField]
    private int maxLines = 15;

    public void Awake()
    {
        if(debugAreaText == null)
        {
            debugAreaText = GetComponent<TextMeshProUGUI>();
        }

        debugAreaText.text = string.Empty;
    }

    public void OnEnable()
    {
        debugAreaText.enabled = enableDebug;
        enabled = enableDebug;

        if(enabled)
        {
            debugAreaText.text += $"<color=\"white\">{DateTime.Now.ToString("HH:mm:ss.fff")} {this.GetType().Name} enabled.</color>\n";
        }
    }

    public void LogInfo(string message)
    {
        ClearLines();
        debugAreaText.text += $"<color=\"green\">{DateTime.Now.ToString("HH:mm:ss.fff")} {message}</color>\n";
    }

    public void LogWarning(string message)
    {
        ClearLines();
        debugAreaText.text += $"<color=\"yellow\">{DateTime.Now.ToString("HH:mm:ss.fff")} {message}</color>\n";
    }

    public void LogError(string message)
    {
        ClearLines();
        debugAreaText.text += $"<color=\"red\">{DateTime.Now.ToString("HH:mm:ss.fff")} {message}</color>\n";
    }

    public void ClearLines()
    {
        if(debugAreaText.text.Split('\n').Length >= maxLines)
        {
            debugAreaText.text = string.Empty;
        }
    }
}
