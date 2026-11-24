using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class Gramophone : MonoBehaviour, IInteractable
{
    [Header("References")]
    public GameManager gameManager;
    public Camera playerCamera;
    public PickUpRecord pickupRecord;
    public Transform recordPoint;

    [Header("Audio")]
    public ReverseAudioPuzzle audioPuzzle;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Interaction")]
    public float interactionDistance = 3f;

    [Header("Small playback buttons")]
    public Transform controlsPoint;
    public float buttonWorldSize = 0.06f;

    private bool installed;

    void Start()
    {
        if (playerCamera == null && pickupRecord != null)
            playerCamera = pickupRecord.playerCamera;

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (gameManager == null && pickupRecord != null)
            gameManager = pickupRecord.gameManager;

        if (audioPuzzle == null)
            audioPuzzle = GetComponent<ReverseAudioPuzzle>();
    }

    void Update()
    {
        if (audioPuzzle != null)
            audioPuzzle.Volume = volume;

        if (gameManager != null)
        {
            gameManager.recordPlaying =
                audioPuzzle != null && audioPuzzle.IsPlaying;
        }

        // »⁄œ  —ﬂÌ» «·ﬁ—’° «· ‘€Ì· „‰ «·√ÌﬁÊ‰« .
        if (installed || playerCamera == null || !LeftClicked())
            return;

        if (pickupRecord == null || !pickupRecord.IsHeld)
            return;

        Ray ray = Cursor.lockState == CursorLockMode.Locked
            ? playerCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f))
            : playerCamera.ScreenPointToRay(MousePosition());

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.GetComponentInParent<Gramophone>() == this)
                Interact();
        }
    }

    public string GetMessage()
    {
        if (installed) return "";

        return pickupRecord != null && pickupRecord.IsHeld
            ? "Click to insert record"
            : "";
    }

    public void Interact()
    {
        if (installed) return;

        if (pickupRecord == null || !pickupRecord.IsHeld)
            return;

        if (recordPoint == null)
        {
            Debug.LogWarning("Assign Record Point on the gramophone.");
            return;
        }

        pickupRecord.PlaceInGramophone(recordPoint);
        installed = true;
    }

    private void StopAudio()
    {
        if (audioPuzzle != null)
            audioPuzzle.StopAudio();

        if (gameManager != null)
            gameManager.recordPlaying = false;
    }

    private void PlayAudio(bool reverse)
    {
        if (!installed) return;

        if (audioPuzzle == null)
        {
            Debug.LogWarning("Assign Audio Puzzle on the gramophone.");
            return;
        }

        audioPuzzle.Volume = volume;

        bool started = reverse
            ? audioPuzzle.PlayReversed()
            : audioPuzzle.PlayNormal();

        if (!started) return;

        if (gameManager != null)
        {
            gameManager.recordPlaying = true;

            if (reverse)
                gameManager.recordFixed = true;
        }
    }

    void OnGUI()
    {
        if (!installed || playerCamera == null || recordPoint == null)
            return;

        Vector3 worldPosition;

        if (controlsPoint != null)
        {
            worldPosition = controlsPoint.position;
        }
        else
        {
            worldPosition = recordPoint.position
                + playerCamera.transform.right * 0.28f
                + Vector3.up * 0.1f;
        }

        Vector3 screen =
            playerCamera.WorldToScreenPoint(worldPosition);

        if (screen.z <= 0f) return;

        Vector3 edge = playerCamera.WorldToScreenPoint(
            worldPosition +
            playerCamera.transform.right *
            Mathf.Max(buttonWorldSize, 0.001f)
        );

        float size = Mathf.Clamp(
            Mathf.Abs(edge.x - screen.x),
            16f,
            48f
        );

        float gap = size * 0.15f;
        float totalWidth = size * 3f + gap * 2f;

        float x = screen.x - totalWidth / 2f;
        float y = Screen.height - screen.y - size / 2f;

        Rect reverseRect = new Rect(
            x, y, size, size
        );

        Rect stopRect = new Rect(
            x + size + gap, y, size, size
        );

        Rect playRect = new Rect(
            x + (size + gap) * 2f, y, size, size
        );

        bool canClick =
            Cursor.lockState != CursorLockMode.Locked;

        if (DrawRadioButton(reverseRect, 0, canClick))
            PlayAudio(true);

        if (DrawRadioButton(stopRect, 1, canClick))
            StopAudio();

        if (DrawRadioButton(playRect, 2, canClick))
            PlayAudio(false);
    }

    private bool DrawRadioButton(
        Rect rect,
        int icon,
        bool canClick)
    {
        bool hovered = canClick &&
            rect.Contains(Event.current.mousePosition);

        Color previousColor = GUI.color;

        GUI.color = hovered
            ? new Color(1f, 0.8f, 0.35f)
            : Color.white;

        float border = 1.5f;

        GUI.DrawTexture(
            new Rect(rect.x, rect.y, rect.width, border),
            Texture2D.whiteTexture
        );

        GUI.DrawTexture(
            new Rect(
                rect.x,
                rect.yMax - border,
                rect.width,
                border),
            Texture2D.whiteTexture
        );

        GUI.DrawTexture(
            new Rect(rect.x, rect.y, border, rect.height),
            Texture2D.whiteTexture
        );

        GUI.DrawTexture(
            new Rect(
                rect.xMax - border,
                rect.y,
                border,
                rect.height),
            Texture2D.whiteTexture
        );

        Rect symbol = new Rect(
            rect.x + rect.width * 0.25f,
            rect.y + rect.height * 0.25f,
            rect.width * 0.5f,
            rect.height * 0.5f
        );

        if (icon == 0)
        {
            float half = symbol.width * 0.48f;

            DrawTriangle(
                new Rect(
                    symbol.x,
                    symbol.y,
                    half,
                    symbol.height),
                true
            );

            DrawTriangle(
                new Rect(
                    symbol.xMax - half,
                    symbol.y,
                    half,
                    symbol.height),
                true
            );
        }
        else if (icon == 1)
        {
            GUI.DrawTexture(
                symbol,
                Texture2D.whiteTexture
            );
        }
        else
        {
            DrawTriangle(symbol, false);
        }

        GUI.color = previousColor;

        return canClick &&
            GUI.Button(
                rect,
                GUIContent.none,
                GUIStyle.none
            );
    }

    private void DrawTriangle(Rect rect, bool pointsLeft)
    {
        const int columns = 20;

        float columnWidth = rect.width / columns;

        for (int i = 0; i < columns; i++)
        {
            float t = (i + 0.5f) / columns;

            float height = rect.height *
                (pointsLeft ? t : 1f - t);

            GUI.DrawTexture(
                new Rect(
                    rect.x + i * columnWidth,
                    rect.center.y - height / 2f,
                    columnWidth + 0.5f,
                    height),
                Texture2D.whiteTexture
            );
        }
    }

    private static bool LeftClicked()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    private static Vector2 MousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    void OnDisable()
    {
        StopAudio();
    }
}