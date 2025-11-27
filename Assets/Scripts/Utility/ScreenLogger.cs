using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Kelas untuk menampilkan log di layar game.
/// Panggil dengan: ScreenLogger.Log("Pesan", ScreenLogger.LogType.Success);
/// Tekan tombol 'D' untuk menampilkan/sembunyikan log.
/// </summary>
public class ScreenLogger : MonoBehaviour
{
    public enum LogType
    {
        Info,
        Success,
        Error
    }

    private static List<LogEntry> logs = new List<LogEntry>();
    private static bool showLogs = false;
    private static GUIStyle textStyle;
    private static Vector2 scrollPosition;

    private const float LOG_WINDOW_WIDTH = 450f;
    private const float LOG_WINDOW_HEIGHT = 300f;

    private struct LogEntry
    {
        public string message;
        public LogType type;
        public LogEntry(string msg, LogType t)
        {
            message = msg;
            type = t;
        }
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (textStyle == null)
        {
            textStyle = new GUIStyle
            {
                fontSize = 14,
                normal = { textColor = Color.white }
            };
        }
    }

    void Update()
    {
        // Toggle tampilan log dengan tombol D
        if (Input.GetKeyDown(KeyCode.L))
        {
            showLogs = !showLogs;
        }
    }

    void OnGUI()
    {
        if (!showLogs) return;

        // Use constants for window dimensions
        GUILayout.BeginArea(new Rect(10, 10, LOG_WINDOW_WIDTH, LOG_WINDOW_HEIGHT), GUI.skin.box);
        GUILayout.Label("<b><size=16>📜 DEBUG LOGGER</size></b>", textStyle);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        // Cache colors to avoid creating new Color objects in a loop
        Color successColor = Color.green;
        Color errorColor = Color.red;
        Color infoColor = Color.yellow;

        foreach (var entry in logs)
        {
            switch (entry.type)
            {
                case LogType.Success:
                    textStyle.normal.textColor = successColor;
                    break;
                case LogType.Error:
                    textStyle.normal.textColor = errorColor;
                    break;
                default:
                    textStyle.normal.textColor = infoColor;
                    break;
            }

            GUILayout.Label($"[{entry.type}] {entry.message}", textStyle);
        }
        GUILayout.EndScrollView();

        if (GUILayout.Button("Clear Logs")) logs.Clear();
        GUILayout.EndArea();
    }

    // ======================================
    // STATIC METHOD UNTUK DIGUNAKAN DIMANA SAJA
    // ======================================
    public static void Log(string message, LogType type = LogType.Info)
    {
        logs.Add(new LogEntry(message, type));
        Debug.Log($"[ScreenLogger] {type}: {message}");
    }

    // Pastikan hanya ada satu instance di scene
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeLogger()
    {
        if (FindObjectOfType<ScreenLogger>() == null)
        {
            var obj = new GameObject("ScreenLogger");
            obj.AddComponent<ScreenLogger>();
        }
    }
}
