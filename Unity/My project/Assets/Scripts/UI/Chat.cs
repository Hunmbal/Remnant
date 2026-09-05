using System.Collections.Generic;
using UnityEngine;

// Minecraft-style chat. Currently handles only system commands (start with "/").
// Commands:
//   /gs d, /gamestate default   -> set game state to Default
//   /gs b, /gamestate builder   -> set game state to Practice (Builder)
//   /map save <name>            -> save the region between the 2 Builder Blocks
//   /map load <name>            -> load a saved arena, replacing the world
//   /map fill hand | /map fill <id> -> fill the region with the hand block or an id
//   /map copy | /map paste      -> copy region to clipboard, paste it at the
//                                  lowest Builder Block (WorldEdit style)
//   /map list | /map new        -> list saved maps / reset to the default arena
//
// Open the input with Enter (empty input). Enter submits, Esc closes.
public class Chat : MonoBehaviour
{
    [Header("Chat Display")]
    [Range(0f, 1f)]
    public float chatOpacity = 0.5f;
    [Range(0f, 1f)]
    public float typingBarOpacity = 0.65f;

    [Tooltip("Max lines of history kept in memory.")]
    public int maxHistory = 200;
    [Tooltip("Number of recent messages drawn on screen.")]
    public int visibleLines = 7;
    [Tooltip("Width (px) of the chat + typing bar.")]
    public float chatWidth = 460f;
    public int fontSize = 14;

    [Header("Colors")]
    public Color textColor = Color.white;
    public Color commandColor = new Color(1f, 0.84f, 0.3f);

    [Header("Position")]
    [Tooltip("Distance from bottom-left of the screen.")]
    public float margin = 10f;

    // Public state so other HUD (e.g. crosshair) can hide while typing.
    public static bool IsOpen => isInputOpen;

    // While the chat input is open, ALL game input (movement, hotbar, inventory,
    // game-state toggle...) is disabled. Other scripts early-return on this once
    // at the top of their Update instead of guarding individual keys.
    public static bool IsLockingInput => isInputOpen;

    struct Msg
    {
        public string text;
        public bool isCommand; // command output drawn in commandColor
        public Msg(string t, bool c) { text = t; isCommand = c; }
    }

    static readonly List<Msg> history = new List<Msg>();

    static bool isInputOpen;
    string inputText = "";

    GUIStyle textStyle;
    GUIStyle inputStyle;
    Texture2D bgTex;
    Texture2D inputBgTex;

    Player player;

    void Awake()
    {
        player = GetComponent<Player>();
        UpdateMaxHistory();
    }

    void BuildStyles()
    {
        textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = fontSize;
        textStyle.wordWrap = true;
        textStyle.alignment = TextAnchor.LowerLeft;

        inputStyle = new GUIStyle(GUI.skin.textField);
        inputStyle.fontSize = fontSize + 2;
        inputStyle.padding = new RectOffset(8, 8, 6, 6);
        inputStyle.alignment = TextAnchor.MiddleLeft;

        bgTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 1f));
        bgTex.Apply();

        inputBgTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        inputBgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 1f));
        inputBgTex.Apply();
    }

    void Update()
    {
        // Open the typing bar with Enter (empty input; player types their own "/").
        if (!isInputOpen)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OpenInput("");
            }
        }
    }

    void OpenInput(string prefill)
    {
        isInputOpen = true;
        inputText = prefill;

        if (player != null) player.EnableLook(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseInput()
    {
        isInputOpen = false;
        inputText = "";

        if (player != null) player.EnableLook(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Submit()
    {
        string line = inputText.Trim();
        if (line.Length > 0)
            HandleCommand(line);

        CloseInput();
    }

    // UI: render the typing field and keep it focused every frame so keys go
    // straight into the bar without clicking. FocusControl() only works once the
    // named control exists, so refresh focus on each OnGUI pass.
    void DrawInputField()
    {
        GUI.SetNextControlName("ChatInput");
        inputText = GUI.TextField(InputRect, inputText, inputStyle);

        if (GUI.GetNameOfFocusedControl() != "ChatInput")
            GUI.FocusControl("ChatInput");
    }

    void OnGUI()
    {
        GUI.color = Color.white;

        // GUIStyle/GUI.skin can only be touched from inside OnGUI, so build lazily.
        if (textStyle == null)
            BuildStyles();

        if (isInputOpen)
        {
            // Handle Enter/Esc before the text field consumes the key event.
            HandleInputEvents();
            DrawHistory();
            DrawTypingBar();
        }
        else
        {
            DrawHistory();
        }
    }

    void DrawHistory()
    {
        if (history.Count == 0) return;

        // Measure the history block.
        float lineHeight = textStyle.lineHeight + 2f;
        int shown = Mathf.Min(visibleLines, history.Count);
        float totalH = shown * lineHeight;
        Rect box = new Rect(margin, Screen.height - margin - totalH, chatWidth, totalH);

        float alpha = chatOpacity;
        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.DrawTexture(box, bgTex);
        GUI.color = Color.white;

        // Draw the most recent lines, oldest on top (like Minecraft).
        int start = history.Count - shown;
        for (int i = 0; i < shown; i++)
        {
            Msg m = history[start + i];
            Rect lineRect = new Rect(box.x + 6f, box.y + i * lineHeight, box.width - 12f, lineHeight);
            GUI.color = m.isCommand ? commandColor : textColor;
            GUI.Label(lineRect, m.text, textStyle);
        }
        GUI.color = Color.white;
    }

    void DrawTypingBar()
    {
        Rect inputRect = InputRect;

        float alpha = typingBarOpacity;
        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.DrawTexture(inputRect, inputBgTex);
        GUI.color = Color.white;

        DrawInputField();
    }

    Rect InputRect
    {
        get
        {
            float lineHeight = textStyle.lineHeight + 2f;
            int shown = Mathf.Min(visibleLines, history.Count);
            float totalH = shown * lineHeight;
            float y = Screen.height - margin - totalH;
            if (history.Count == 0) y = Screen.height - margin;
            y -= textStyle.lineHeight + 8f; // sit just above the history
            return new Rect(margin, y, chatWidth, textStyle.lineHeight + 14f);
        }
    }

    void HandleInputEvents()
    {
        Event e = Event.current;
        if (e == null || e.type != EventType.KeyDown) return;

        if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
        {
            Submit();
            e.Use();
        }
        else if (e.keyCode == KeyCode.Escape)
        {
            CloseInput();
            e.Use();
        }
    }

    // Kept in sync with the instance's maxHistory so the static Log() can trim.
    static int _maxHistory = 200;

    void UpdateMaxHistory() => _maxHistory = Mathf.Max(1, maxHistory);

    // Public API for other systems to post a system message to the chat.
    public static void Log(string message)
    {
        history.Add(new Msg(message, false));
        while (history.Count > _maxHistory)
            history.RemoveAt(0);
    }

    void HandleCommand(string raw)
    {
        string cmd = raw;
        string arg = "";
        int space = raw.IndexOf(' ');
        if (space >= 0)
        {
            cmd = raw.Substring(0, space).Trim();
            arg = raw.Substring(space + 1).Trim();
        }
        cmd = cmd.ToLowerInvariant();
        string argRaw = arg;
        arg = arg.ToLowerInvariant();
        string result = null;

        if (cmd == "/gs" || cmd == "/gamestate" || cmd == "/gsd" || cmd == "/gsb")
        {
            if (arg == "d" || arg == "default")
            {
                PlayerStateManager.State = PlayerState.Default;
                result = "Game state set to Default.";
            }
            else if (arg == "b" || arg == "builder")
            {
                PlayerStateManager.State = PlayerState.Practice;
                result = "Game state set to Builder (Practice).";
            }
            else if (cmd == "/gsd")
            {
                PlayerStateManager.State = PlayerState.Default;
                result = "Game state set to Default.";
            }
            else if (cmd == "/gsb")
            {
                PlayerStateManager.State = PlayerState.Practice;
                result = "Game state set to Builder (Practice).";
            }
            else
            {
                result = "Usage: /gs <d|b>  or  /gamestate <default|builder>";
            }
        }
        else if (cmd == "/team")
        {
            if (string.IsNullOrEmpty(arg) || arg == "ffa" || arg == "none")
            {
                PlayerData.Team = null;
                MoveToTeamSpawn();
                result = "Team cleared — spawning on the FFA slot.";
            }
            else if (TryParsePlayerTeam(arg, out PlayerTeam team))
            {
                PlayerData.Team = team;
                MoveToTeamSpawn();
                result = "Team set to " + team + ".";
            }
            else
            {
                result = "Usage: /team <ffa|red|blue|green|yellow>";
            }
        }
        else if (cmd == "/map")
        {
            // Split the tail ("save foo" / "fill hand" / "list" / ...).
            string rest = argRaw.Trim();
            string sub = "";
            string subArg = "";
            int sp = rest.IndexOf(' ');
            if (sp >= 0)
            {
                sub = rest.Substring(0, sp).Trim();
                subArg = rest.Substring(sp + 1).Trim();
            }
            else
            {
                sub = rest;
            }
            string subL = sub.ToLowerInvariant();

            if (subL == "save")
            {
                result = subArg.Length == 0 ? "Usage: /map save <name>" : MapSaver.Save(subArg);
            }
            else if (subL == "load")
            {
                result = subArg.Length == 0 ? "Usage: /map load <name>" : MapSaver.Load(subArg);
            }
            else if (subL == "spawn")
            {
                // /map spawn <map_name> <ffa|red|blue|green|yellow>
                int sp2 = subArg.IndexOf(' ');
                string mapName = sp2 >= 0 ? subArg.Substring(0, sp2).Trim() : subArg;
                string teamStr = sp2 >= 0 ? subArg.Substring(sp2 + 1).Trim() : "";

                if (subArg.Length == 0 || sp2 < 0)
                {
                    result = "Usage: /map spawn <map_name> <ffa|red|blue|green|yellow>";
                }
                else if (!TryParseSpawnTeam(teamStr, out SpawnTeam team))
                {
                    result = "Unknown team '" + teamStr + "'. Use ffa, red, blue, green, or yellow.";
                }
                else
                {
                    Vector3 feet = player != null ? player.transform.position : Vector3.zero;
                    result = MapSaver.SetSpawn(mapName, team, feet);
                }
            }
            else if (subL == "fill")
            {
                result = MapSaver.Fill(subArg, player);
            }
            else if (subL == "paste")
            {
                result = MapSaver.Paste();
            }
            else if (subL == "copy")
            {
                result = MapSaver.Copy();
            }
            else if (subL == "list")
            {
                string[] files = MapSaver.List();
                if (files.Length == 0)
                {
                    result = "No saved maps yet.";
                }
                else
                {
                    string[] names = new string[files.Length];
                    for (int i = 0; i < files.Length; i++)
                        names[i] = System.IO.Path.GetFileNameWithoutExtension(files[i]);
                    result = "Maps: " + string.Join(", ", names);
                }
            }
            else if (subL == "new")
            {
                ArenaGenerator arena = Object.FindObjectOfType<ArenaGenerator>();
                if (arena == null)
                {
                    result = "No arena found.";
                }
                else
                {
                    arena.ResetToDefault();
                    result = "World reset to the default arena.";
                }
            }
            else
            {
                result = "Usage: /map save <name> | /map load <name> | /map spawn <name> <ffa|red|blue|green|yellow> | /map fill <hand|id> | /map copy | /map paste | /map list | /map new";
            }
        }
        else
        {
            result = "Unknown command: " + raw;
        }

        // Echo what the player typed, then the command outcome (colored).
        history.Add(new Msg("> " + raw, false));
        history.Add(new Msg(result, true));
        while (history.Count > _maxHistory)
            history.RemoveAt(0);
    }

    static bool TryParsePlayerTeam(string s, out PlayerTeam team)
    {
        switch (s.Trim().ToLowerInvariant())
        {
            case "red": team = PlayerTeam.Red; return true;
            case "blue": team = PlayerTeam.Blue; return true;
            case "green": team = PlayerTeam.Green; return true;
            case "yellow": team = PlayerTeam.Yellow; return true;
            default: team = PlayerTeam.Red; return false;
        }
    }

    // Hop the player to the current map's spawn for their (new) team.
    void MoveToTeamSpawn()
    {
        if (player == null) return;
        ArenaGenerator arena = Object.FindObjectOfType<ArenaGenerator>();
        if (arena != null && arena.TryGetSpawn(PlayerData.Team, out Vector3 spawnPos))
            player.transform.position = spawnPos;
    }

    static bool TryParseSpawnTeam(string s, out SpawnTeam team)
    {
        switch (s.Trim().ToLowerInvariant())
        {
            case "ffa": team = SpawnTeam.FFA; return true;
            case "red": team = SpawnTeam.Red; return true;
            case "blue": team = SpawnTeam.Blue; return true;
            case "green": team = SpawnTeam.Green; return true;
            case "yellow": team = SpawnTeam.Yellow; return true;
            default: team = SpawnTeam.FFA; return false;
        }
    }
}
