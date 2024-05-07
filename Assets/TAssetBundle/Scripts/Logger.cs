using System;
using UnityEngine;

namespace TAssetBundle
{
    public static class Logger
    {
        public const string LogPrefix = "[TAssetBundle] ";
        public static Action<string> LogHandler = Debug.Log;
        public static Action<string> WarningHandler = Debug.LogWarning;
        public static Action<string> ErrorHandler = Debug.LogError;

        public static void Log(string message)
        {
            LogHandler(LogPrefix + message);
        }

        public static void Warning(string message)
        {
            WarningHandler(LogPrefix + message);
        }

        public static void Error(string message)
        {
            ErrorHandler(LogPrefix + message);
        }
    }

}