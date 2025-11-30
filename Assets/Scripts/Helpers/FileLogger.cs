using UnityEngine;
using System.IO;
using System;

/// <summary>
/// Utility for logging messages to a file instead of console.
/// Useful for debugging issues that require detailed logs.
/// </summary>
public static class FileLogger
{
    private static readonly object FileLock = new object();
    private static string logFilePath;
    private static bool initialized = false;
    private static string lastErrorMessage = string.Empty;

    /// <summary>
    /// Initialize the logger with a specific log file path.
    /// If not called, will auto-initialize on first log.
    /// </summary>
    public static void Initialize(string filename = "game_debug.log")
    {
        if (initialized)
            return;

        try
        {
            string projectRoot = Application.dataPath;
            if (!string.IsNullOrEmpty(projectRoot))
            {
                projectRoot = Path.GetDirectoryName(projectRoot);
            }
            else
            {
                projectRoot = Directory.GetCurrentDirectory();
            }

            string logsDirectory = Path.Combine(projectRoot ?? string.Empty, "Logs");
            Directory.CreateDirectory(logsDirectory);

            logFilePath = Path.Combine(logsDirectory, filename);

            lock (FileLock)
            {
                File.WriteAllText(logFilePath, $"=== Log started at {DateTime.Now} ===\n");
            }

            initialized = true;
            lastErrorMessage = string.Empty;
        }
        catch (Exception e)
        {
            lastErrorMessage = e.Message;
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

            lock (FileLock)
            {
                File.AppendAllText(logFilePath, logEntry);
            }
        }
        catch (Exception e)
        {
            lastErrorMessage = e.Message;
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
            lock (FileLock)
            {
                File.WriteAllText(logFilePath, $"=== Log cleared at {DateTime.Now} ===\n");
            }
        }
        catch (Exception e)
        {
            lastErrorMessage = e.Message;
        }
    }

    /// <summary>
    /// Retrieve the last error message encountered by the logger (if any).
    /// </summary>
    public static string GetLastError() => lastErrorMessage;
}
