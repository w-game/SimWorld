using System;
using UnityEngine;

public class Log
{
    public static void LogInfo(string module, string message)
    {
        Debug.Log($"[INFO] <{module}> |{DateTime.Now}| {message}");
    }

    public static void LogWarning(string message)
    {
        Debug.LogWarning($"[WARNING] {message}");
    }

    public static void LogError(string message)
    {
        Debug.LogError($"[ERROR] {message}");
    }
}