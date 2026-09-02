using System.Collections.Generic;
using UnityEngine;

// Minecraft-style chat. Currently handles only system commands (start with "/").
// Commands:
//   /gs d, /gamestate default   -> set game state to Default
//   /gs b, /gamestate builder   -> set game state to Practice (Builder)
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
}
