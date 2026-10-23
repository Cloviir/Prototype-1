using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Playing, Won, Lost }

// Tracks knockouts, lives, and game state. Put this on an empty "GameManager" object.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Rules")]
    public int knockoutsToWin = 5;
    public int startingLives = 3;

    [Header("Focused Variation: heavier opponents")]
    public int knockoutsBeforeHeavy = 2;
    public float heavyMassMultiplier = 2.5f;

    public GameState State { get; private set; } = GameState.Playing;
    public int Knockouts { get; private set; }
    public int Lives { get; private set; }

    string flashMessage = "";
    float flashUntil;

    void Awake()
    {
        Instance = this;
        Lives = startingLives;
        Time.timeScale = 1f; // reset in case we restarted from a paused state
    }

    // Opponents read this when they spawn (the focused variation).
    public bool HeavyOpponentsActive => Knockouts >= knockoutsBeforeHeavy;

    public void AddKnockout()
    {
        if (State != GameState.Playing) return;

        Knockouts++;
        Debug.Log($"Knockout! {Knockouts}/{knockoutsToWin}");

        if (Knockouts == knockoutsBeforeHeavy)
            Flash("Heavier opponents incoming!");

        if (Knockouts >= knockoutsToWin)
            SetState(GameState.Won);
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
        GUI.Label(new Rect(20, 20, 400, 40), $"Knockouts: {Knockouts} / {knockoutsToWin}", hud);
        GUI.Label(new Rect(20, 55, 400, 40), $"Lives: {Lives}", hud);
        GUI.Label(new Rect(20, 90, 600, 40), "WASD: move   Space: shove", hud);

        var center = new GUIStyle(GUI.skin.label)
        { fontSize = 36, alignment = TextAnchor.MiddleCenter };
        var box = new Rect(0, Screen.height / 2f - 100, Screen.width, 200);

        if (State == GameState.Won)
            GUI.Label(box, $"YOU WIN!\nYou knocked out {knockoutsToWin} opponents.\nPress R to restart", center);
        else if (State == GameState.Lost)
            GUI.Label(box, "YOU LOSE\nYou were knocked out of the arena too many times.\nPress R to restart", center);
        else if (Time.time < flashUntil)
            GUI.Label(new Rect(0, 140, Screen.width, 50), flashMessage, center);
    }
}