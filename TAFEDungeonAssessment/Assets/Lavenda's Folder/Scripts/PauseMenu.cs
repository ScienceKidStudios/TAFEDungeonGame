using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
public class PauseMenu : MonoBehaviour
{
    public static bool Paused = false;
    public GameObject PauseMenuCanvas;
    void Start()
    {
        Time.timeScale = 1f;
    }
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Paused)
            {
                Play();
            }
            else
            {
                Stop();
            }
        }
    }
    void Stop()
    {
        PauseMenuCanvas.SetActive(true);
        Time.timeScale = 0f;
        Paused = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    public void Play()
    {
        PauseMenuCanvas.SetActive(false);
        Time.timeScale = 1f;
        Paused = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public void MainMenuButton()
    {
        Time.timeScale = 1f;
        Paused = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }
}