using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Playing, Won, Lost }

// Tracks waves, knockouts, lives, and game state. Put this on an empty "GameManager" object.
// Win by clearing the final wave; lose by running out of lives.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Rules")]
    [Tooltip("Clear this many waves to win.")]
    public int wavesToWin = 5;
    public int startingLives = 3;

    public GameState State { get; private set; } = GameState.Playing;
    public int Knockouts { get; private set; }
    public int Lives { get; private set; }
    public int CurrentWave { get; private set; }

    int waveTotal;
    int waveKnockouts;

    string flashMessage = "";
    float flashUntil;

    void Awake()
    {
        Instance = this;
        Lives = startingLives;
        Time.timeScale = 1f; // reset in case we restarted from a paused state
    }

    // Called by OpponentSpawner at the start of each wave.
    public void StartWave(int wave, int opponentCount)
    {
        CurrentWave = wave;
        waveTotal = opponentCount;
        waveKnockouts = 0;

        if (wave == wavesToWin) Flash($"FINAL WAVE! {opponentCount} opponents");
        else if (wave > 1) Flash($"Wave {wave}: {opponentCount} opponents, tougher and faster!");
        else Flash($"Wave 1: {opponentCount} opponents");
    }

    // Called by OpponentSpawner when every opponent in the wave is gone.
    public void WaveCleared()
    {
        if (State != GameState.Playing) return;

        Debug.Log($"Wave {CurrentWave} cleared!");
        if (CurrentWave >= wavesToWin)
            SetState(GameState.Won);
        else
            Flash($"Wave {CurrentWave} cleared! Get ready...");
    }

    public void AddKnockout()
    {
        if (State != GameState.Playing) return;

        Knockouts++;
        waveKnockouts++;
        Debug.Log($"Knockout! Total: {Knockouts}");
    }

    public void LoseLife()
    {
        if (State != GameState.Playing) return;

        Lives--;
        Debug.Log($"Player knocked out! Lives left: {Lives}");
        Flash("You were knocked out of the arena! -1 life");

        if (Lives <= 0)
            SetState(GameState.Lost);
    }

    void SetState(GameState newState)
    {
        State = newState;
        Debug.Log("Game state: " + newState);
        Time.timeScale = 0f; // freezes physics, movement, and spawning
    }

    public void Flash(string msg)
    {
        flashMessage = msg;
        flashUntil = Time.time + 2f;
    }

    void Update()
    {
        if (State != GameState.Playing && Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Placeholder UI. Swap for TextMeshPro later if you want.
    void OnGUI()
    {
        var hud = new GUIStyle(GUI.skin.label) { fontSize = 24 };
        string waveText = CurrentWave > 0
            ? $"Wave: {CurrentWave} / {wavesToWin}   ({waveKnockouts}/{waveTotal} knocked out)"
            : "Get ready...";
        GUI.Label(new Rect(20, 20, 700, 40), waveText, hud);
        GUI.Label(new Rect(20, 55, 400, 40), $"Lives: {Lives}   Total KOs: {Knockouts}", hud);
        GUI.Label(new Rect(20, 90, 600, 40), "WASD: move   Space: shove", hud);

        var center = new GUIStyle(GUI.skin.label)
        { fontSize = 36, alignment = TextAnchor.MiddleCenter };
        var box = new Rect(0, Screen.height / 2f - 100, Screen.width, 200);

        if (State == GameState.Won)
            GUI.Label(box, $"YOU WIN!\nYou survived all {wavesToWin} waves ({Knockouts} knockouts).\nPress R to restart", center);
        else if (State == GameState.Lost)
            GUI.Label(box, $"YOU LOSE\nYou made it to wave {CurrentWave}.\nPress R to restart", center);
        else if (Time.time < flashUntil)
            GUI.Label(new Rect(0, 140, Screen.width, 50), flashMessage, center);
    }
}