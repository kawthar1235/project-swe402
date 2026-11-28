using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class WoodenBox : MonoBehaviour, IInteractable
{
    [Header("Existing objects")]
    public GameManager gameManager;
    public GameObject key;
    public Camera playerCamera;
    public Transform keypadRoot;

    [Header("Two empty position markers")]
    public Transform boxViewPoint;
    public Transform displayPoint;

    [Header("Puzzle")]
    public string correctCode = "7382";
    public float interactionDistance = 3f;
    public float closeUpFOV = 45f;
    public float displayWorldWidth = 0.1f;

    [Header("Optional sounds - beeps are generated if empty")]
    public AudioClip clickSound;
    public AudioClip wrongSound;
    public AudioClip breakSound;

    private string enteredCode = "";
    private bool inspecting;
    private bool solved;
    private int openedFrame;
    private AudioSource speaker;
    private AudioClip generatedClick;
    private AudioClip generatedWrong;
    private GUIStyle displayStyle;
    private CameraFollow follow;
    private PlayerMovement movement;
    private PlayerInteraction interaction;
    private Rigidbody playerBody;
    private bool followEnabled, movementEnabled, interactionEnabled, bodyWasKinematic;
    private Vector3 cameraPosition;
    private Quaternion cameraRotation;
    private float cameraFOV;
    private CursorLockMode oldLock;
    private bool oldCursorVisible;

    void Awake()
    {
        if (key != null) key.SetActive(false);
        ChestPuzzleTools.EnsureCollider(gameObject);
        speaker = GetComponent<AudioSource>();
        if (speaker == null) speaker = gameObject.AddComponent<AudioSource>();
        speaker.playOnAwake = false;
        speaker.spatialBlend = 0f;
        speaker.Stop();
        generatedClick = ChestPuzzleTools.Tone("Keypad Click", 1100f, 0.055f);
        generatedWrong = ChestPuzzleTools.Tone("Wrong Code", 150f, 0.35f);
    }
    void Start()
    {
        playerCamera = ChestPuzzleTools.ResolveCamera(playerCamera);
        if (keypadRoot != null)
        {
            foreach (Transform child in keypadRoot.GetComponentsInChildren<Transform>(true))
            {
                bool enter = child.name == "bttnEnter";
                bool digit = child.name.Length == 5 && child.name.StartsWith("bttn") &&
                    child.name[4] >= '0' && child.name[4] <= '9';
                if (!enter && !digit) continue;
                ChestKeypadButton button = child.GetComponent<ChestKeypadButton>();
                if (button == null) button = child.gameObject.AddComponent<ChestKeypadButton>();
                button.Configure(this, enter ? "" : child.name.Substring(4), enter);
            }
        }
    }
    void Update()
    {
        if (solved || playerCamera == null) return;
        if (inspecting)
        {
            if (Time.frameCount == openedFrame) return;
            if (ChestPuzzleTools.EscapePressed()) { ExitView(); return; }
            if (!ChestPuzzleTools.Clicked()) return;
            if (ChestPuzzleTools.Raycast(playerCamera, 100f, out RaycastHit hit))
            {
                ChestKeypadButton button = hit.collider.GetComponentInParent<ChestKeypadButton>();
                if (button != null && button.Owner == this) { button.Press(); return; }
                if (Contains(hit.transform)) return;
            }
            ExitView();
            return;
        }
        if (ChestPuzzleTools.Inspecting || !ChestPuzzleTools.Clicked()) return;
        if (ChestPuzzleTools.Raycast(playerCamera, interactionDistance, out RaycastHit target) &&
            Contains(target.transform)) Interact();
    }
    private bool Contains(Transform target)
    {
        return target.IsChildOf(transform) ||
            (keypadRoot != null && target.IsChildOf(keypadRoot));
    }
   public string GetMessage()
{
    return "";
}
    public void Interact()
    {
        if (solved || inspecting || ChestPuzzleTools.Inspecting) return;
        if (playerCamera == null || boxViewPoint == null || keypadRoot == null ||
            displayPoint == null || key == null || key.GetComponent<KeyPickup>() == null)
        {
            Debug.LogWarning("Box: assign Camera, Box View Point, Keypad Root, Display Point and Key (with KeyPickup).", this);
            return;
        }
        Transform player = ChestPuzzleTools.Player(playerCamera);
        follow = playerCamera.GetComponent<CameraFollow>();
        movement = player != null ? player.GetComponent<PlayerMovement>() : null;
        interaction = player != null ? player.GetComponent<PlayerInteraction>() : null;
        playerBody = player != null ? player.GetComponent<Rigidbody>() : null;
        cameraPosition = playerCamera.transform.position;
        cameraRotation = playerCamera.transform.rotation;
        cameraFOV = playerCamera.fieldOfView;
        // Capture cursor BEFORE CameraFollow.OnDisable can change it.
        oldLock = Cursor.lockState;
        oldCursorVisible = Cursor.visible;
        followEnabled = follow != null && follow.enabled;
        movementEnabled = movement != null && movement.enabled;
        interactionEnabled = interaction != null && interaction.enabled;
        if (follow != null) follow.enabled = false;
        if (movement != null) movement.enabled = false;
        if (interaction != null)
        {
            interaction.enabled = false;
            if (interaction.messageText != null) interaction.messageText.text = "";
        }
        if (playerBody != null)
        {
            bodyWasKinematic = playerBody.isKinematic;
            if (!bodyWasKinematic)
            {
                playerBody.linearVelocity = Vector3.zero;
                playerBody.angularVelocity = Vector3.zero;
                playerBody.isKinematic = true;
            }
        }
        inspecting = true;
        ChestPuzzleTools.InspectingBox = this;
        openedFrame = Time.frameCount;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PositionCamera();
    }
    void LateUpdate()
    {
        if (inspecting) PositionCamera();
    }
    private void PositionCamera()
    {
        playerCamera.transform.SetPositionAndRotation(boxViewPoint.position, boxViewPoint.rotation);
        playerCamera.fieldOfView = closeUpFOV;
    }
    private void ExitView()
    {
        if (!inspecting) return;
        inspecting = false;
        if (ChestPuzzleTools.InspectingBox == this) ChestPuzzleTools.InspectingBox = null;
        if (playerCamera != null)
        {
            playerCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
            playerCamera.fieldOfView = cameraFOV;
        }
        if (playerBody != null) playerBody.isKinematic = bodyWasKinematic;
        if (movement != null) movement.enabled = movementEnabled;
        if (interaction != null) interaction.enabled = interactionEnabled;
        if (follow != null) follow.enabled = followEnabled;
        Cursor.lockState = oldLock;
        Cursor.visible = oldCursorVisible;
    }
    public void EnterNumber(string number)
    {
        if (!inspecting || solved || enteredCode.Length >= 4 || number == null ||
            number.Length != 1 || number[0] < '0' || number[0] > '9') return;
        enteredCode += number;
        speaker.PlayOneShot(clickSound != null ? clickSound : generatedClick);
    }
    public void SubmitCode()
    {
        if (!inspecting || solved) return;
        bool correct = enteredCode.Length == 4 && enteredCode == correctCode;
        enteredCode = ""; // Enter always clears the screen, including incomplete entries.
        speaker.PlayOneShot(clickSound != null ? clickSound : generatedClick);
        if (!correct)
        {
            speaker.PlayOneShot(wrongSound != null ? wrongSound : generatedWrong);
            return;
        }
        solved = true;
        StartCoroutine(BreakBox());
    }
   private IEnumerator BreakBox()
{
    ChestBoxEffects effects = GetComponent<ChestBoxEffects>();

    if (effects == null)
        effects = gameObject.AddComponent<ChestBoxEffects>();

    // Use the assigned recording instead of generated noise.
    PlayBreakSoundSegment();

    effects.Shatter(false);

    foreach (LODGroup group in GetComponentsInChildren<LODGroup>())
        group.enabled = false;

    foreach (Renderer part in GetComponentsInChildren<Renderer>())
    {
        if (key != null && part.transform.IsChildOf(key.transform))
            continue;

        part.enabled = false;
    }

    foreach (Collider col in GetComponentsInChildren<Collider>())
    {
        if (key != null && col.transform.IsChildOf(key.transform))
            continue;

        col.enabled = false;
    }

    if (keypadRoot != null)
        keypadRoot.gameObject.SetActive(false);

    yield return new WaitForSeconds(0.45f);

    ExitView();

    yield return new WaitForSeconds(0.45f);

    if (key != null)
    {
        key.transform.SetParent(null, true);
        key.SetActive(true);
        key.GetComponent<KeyPickup>().Reveal();
    }
}
    void OnGUI()
    {
        if (!inspecting || solved || displayPoint == null || enteredCode.Length == 0) return;
        Vector3 screen = playerCamera.WorldToScreenPoint(displayPoint.position);
        if (screen.z <= 0f) return;
        Vector3 edge = playerCamera.WorldToScreenPoint(displayPoint.position +
            playerCamera.transform.right * displayWorldWidth);
        float width = Mathf.Abs(edge.x - screen.x);
        if (displayStyle == null)
        {
            displayStyle = new GUIStyle(GUI.skin.label);
            displayStyle.alignment = TextAnchor.MiddleCenter;
            displayStyle.normal.textColor = Color.black;
        }
        displayStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(width * 0.22f), 10, 42);
        float height = displayStyle.fontSize * 1.5f;
        GUI.Label(new Rect(screen.x - width / 2f, Screen.height - screen.y - height / 2f,
            width, height), enteredCode, displayStyle);
    }
    void OnDisable() { ExitView(); }
    void OnDestroy()
    {
        if (generatedClick != null) Destroy(generatedClick);
        if (generatedWrong != null) Destroy(generatedWrong);
    }
    private void PlayBreakSoundSegment()
{
    if (breakSound == null)
        return;

    const float start = 3f;
    float end = Mathf.Min(5f, breakSound.length);

    if (end <= start)
    {
        Debug.LogWarning(
            "Break Sound must be longer than 3 seconds.",
            this);
        return;
    }

    float duration = end - start;

    // A separate source avoids cutting off other chest sounds.
    GameObject soundObject =
        new GameObject("Chest Break Sound");

    soundObject.transform.position = transform.position;

    AudioSource source =
        soundObject.AddComponent<AudioSource>();

    source.playOnAwake = false;
    source.loop = false;
    source.clip = breakSound;
    source.pitch = 1f;

    source.volume = speaker != null
        ? speaker.volume * 0.65f
        : 0.65f;

    source.spatialBlend = speaker != null
        ? speaker.spatialBlend
        : 0f;

    if (speaker != null)
        source.outputAudioMixerGroup = speaker.outputAudioMixerGroup;

    source.time = start;

    double playAt = AudioSettings.dspTime + 0.05;

    source.PlayScheduled(playAt);
    source.SetScheduledEndTime(playAt + duration);

    Destroy(soundObject, duration + 0.2f);
}
}
