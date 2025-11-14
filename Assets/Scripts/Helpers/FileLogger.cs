using UnityEngine;
using System.IO;
using System;

/// <summary>
/// Utility for logging messages to a file instead of console.
/// Useful for debugging issues that require detailed logs.
/// </summary>
public static class FileLogger
{
    private static string logFilePath;
    private static bool initialized = false;

    /// <summary>
    /// Initialize the logger with a specific log file path.
    /// If not called, will auto-initialize on first log.
    /// </summary>
    public static void Initialize(string filename = "game_debug.log")
    {
        if (initialized)
            return;

        logFilePath = Path.Combine(Application.persistentDataPath, filename);
        
        // Clear previous log on initialization
        try
        {
            File.WriteAllText(logFilePath, $"=== Log started at {DateTime.Now} ===\n");
            initialized = true;
            Debug.Log($"[FileLogger] Logging to: {logFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FileLogger] Failed to initialize log file: {e.Message}");
        }
    }

    /// <summary>
    /// Log a message to the file with timestamp.
    /// </summary>
    public static void Log(string message, string category = "")
    {
        if (!initialized)
            Initialize();

        try
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string logEntry = string.IsNullOrEmpty(category) 
                ? $"[{timestamp}] {message}\n"
                : $"[{timestamp}] [{category}] {message}\n";
            
            File.AppendAllText(logFilePath, logEntry);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FileLogger] Failed to write log: {e.Message}");
        }
    }

    /// <summary>
    /// Get the current log file path.
    /// </summary>
    public static string GetLogPath()
    {
        if (!initialized)
            Initialize();
        return logFilePath;
    }

    /// <summary>
    /// Clear the log file.
    /// </summary>
    public static void Clear()
    {
        if (!initialized)
            Initialize();

        try
        {
            File.WriteAllText(logFilePath, $"=== Log cleared at {DateTime.Now} ===\n");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FileLogger] Failed to clear log: {e.Message}");
        }
    }
}
