using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Gramophone : MonoBehaviour, IInteractable
{
    [Header("References")]
    public GameManager gameManager;
    public Camera playerCamera;
    public PickUpRecord pickupRecord;
    public Transform recordPoint;

    [Header("Puzzle audio")]
    public ReverseAudioPuzzle audioPuzzle;

    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Interaction")]
    public float interactionDistance = 3f;

    [Header("Playback buttons")]
    public Transform controlsPoint;
    public Transform playbackButton;
    public float buttonWorldSize = 0.06f;

    [Header("Record animation")]
    public float rotationSpeed = 150f;

    [Header("Music without a record")]
    [Range(0f, 1f)]
    public float emptyMusicVolume = 0.18f;

    private bool installed;
    private bool reversePlaying;
    private int lastActionFrame = -1;

    private AudioSource emptySpeaker;
    private AudioClip emptyNormal;
    private AudioClip emptyReverse;

    private Material redMaterial;
    private Material greenMaterial;

    private readonly List<OutlinePart> outlines =
        new List<OutlinePart>();

    private sealed class OutlinePart
    {
        public Renderer source;
        public Renderer outline;
        public Vector3 center;
        public bool button;
    }

    private void Start()
    {
        if (playerCamera == null && pickupRecord != null)
            playerCamera = pickupRecord.playerCamera;

        playerCamera =
            ChestPuzzleTools.ResolveCamera(playerCamera);

        if (gameManager == null && pickupRecord != null)
            gameManager = pickupRecord.gameManager;

        if (audioPuzzle == null)
            audioPuzzle = GetComponent<ReverseAudioPuzzle>();

        // Find the existing physical button automatically.
        if (playbackButton == null)
        {
            foreach (Transform child in
                GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Equals(
                    "button",
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    playbackButton = child;
                    break;
                }
            }
        }

        SetupColliders();
        SetupEmptyAudio();
        CreateOutlines();
    }

    private void SetupColliders()
    {
        bool bodyColliderFound = false;

        foreach (Collider col in GetComponents<Collider>())
        {
            if (col.enabled && !col.isTrigger)
            {
                bodyColliderFound = true;
                break;
            }
        }

        if (!bodyColliderFound)
        {
            Bounds bounds =
                ChestPuzzleTools.LocalBounds(transform);

            BoxCollider box =
                gameObject.AddComponent<BoxCollider>();

            box.center = bounds.center;
            box.size = Vector3.Max(
                bounds.size, Vector3.one * 0.01f);
        }

        if (playbackButton == null)
            return;

        Collider existing =
            playbackButton.GetComponent<Collider>();

        if (existing == null)
        {
            Bounds bounds =
                ChestPuzzleTools.LocalBounds(playbackButton);

            BoxCollider box =
                playbackButton.gameObject.AddComponent<BoxCollider>();

            box.center = bounds.center;
            box.size = Vector3.Max(
                bounds.size, Vector3.one * 0.008f);
        }
        else
        {
            existing.enabled = true;
            existing.isTrigger = false;
        }
    }

    private void SetupEmptyAudio()
    {
        emptySpeaker = gameObject.AddComponent<AudioSource>();
        emptySpeaker.playOnAwake = false;
        emptySpeaker.loop = true;
        emptySpeaker.spatialBlend = 1f;
        emptySpeaker.minDistance = 1f;
        emptySpeaker.maxDistance = 9f;
        emptySpeaker.dopplerLevel = 0f;
    }

    private bool IsPlaying()
    {
        return installed
            ? audioPuzzle != null && audioPuzzle.IsPlaying
            : emptySpeaker != null && emptySpeaker.isPlaying;
    }

    private void Update()
    {
        if (playerCamera == null)
        {
            playerCamera =
                ChestPuzzleTools.ResolveCamera(playerCamera);
        }

        if (audioPuzzle != null)
            audioPuzzle.Volume = volume;

        if (emptySpeaker != null)
        {
            emptySpeaker.volume = volume * emptyMusicVolume;

            // Slight speed instability for an old-record sound.
            emptySpeaker.pitch =
                1f + Mathf.Sin(Time.time * 4.1f) * 0.004f;
        }

        // Background music without the puzzle record
        // must not count as solving or playing the clue.
        if (gameManager != null)
        {
            gameManager.recordPlaying =
                installed &&
                audioPuzzle != null &&
                audioPuzzle.IsPlaying;
        }

        if (installed &&
            IsPlaying() &&
            pickupRecord != null &&
            recordPoint != null)
        {
            float direction = reversePlaying ? -1f : 1f;

            pickupRecord.transform.RotateAround(
                recordPoint.position,
                recordPoint.up,
                rotationSpeed * direction * Time.deltaTime);
        }

        if (playerCamera == null ||
            ChestPuzzleTools.Inspecting ||
            Time.timeScale == 0f)
        {
            SetOutlines(false, false);
            return;
        }

        bool overControls = TryGetControls(
            out Rect reverseRect,
            out Rect stopRect,
            out Rect playRect);

        Vector2 pointer = GuiPointer();

        bool reverseHovered =
            overControls && reverseRect.Contains(pointer);

        bool stopHovered =
            overControls && stopRect.Contains(pointer);

        bool playHovered =
            overControls && playRect.Contains(pointer);

        bool overAnyButton =
            reverseHovered || stopHovered || playHovered;

        bool bodyHovered = false;
        bool physicalButtonHovered = false;

        if (!overAnyButton &&
            ChestPuzzleTools.Raycast(
                playerCamera,
                interactionDistance,
                out RaycastHit hit))
        {
            if (hit.collider.GetComponentInParent<Gramophone>() == this)
            {
                physicalButtonHovered =
                    IsPhysicalButton(hit.transform);

                bodyHovered = !physicalButtonHovered;
            }
        }

        SetOutlines(
            bodyHovered,
            physicalButtonHovered || overAnyButton);

        if (!ChestPuzzleTools.Clicked())
            return;

        if (reverseHovered)
        {
            PlayAudio(true);
            return;
        }

        if (stopHovered)
        {
            StopAudio();
            return;
        }

        if (playHovered)
        {
            PlayAudio(false);
            return;
        }

        if (bodyHovered || physicalButtonHovered)
            Interact();
    }

    private bool IsPhysicalButton(Transform target)
    {
        return playbackButton != null &&
            target.IsChildOf(playbackButton);
    }

    public string GetMessage()
    {
        return "";
    }

    public void Interact()
    {
        if (playerCamera == null ||
            ChestPuzzleTools.Inspecting ||
            Time.timeScale == 0f)
            return;

        // The on-screen controls handle their own clicks.
        if (TryGetControls(
                out Rect reverseRect,
                out Rect stopRect,
                out Rect playRect))
        {
            Vector2 pointer = GuiPointer();

            if (reverseRect.Contains(pointer) ||
                stopRect.Contains(pointer) ||
                playRect.Contains(pointer))
                return;
        }

        if (!ChestPuzzleTools.Raycast(
                playerCamera,
                interactionDistance,
                out RaycastHit hit))
            return;

        if (hit.collider.GetComponentInParent<Gramophone>() != this)
            return;

        if (IsPhysicalButton(hit.transform))
        {
            if (IsPlaying())
                StopAudio();
            else
                PlayAudio(false);

            return;
        }

        // Clicking the body inserts a held record.
        if (installed ||
            pickupRecord == null ||
            !pickupRecord.IsHeld)
            return;

        if (recordPoint == null)
        {
            Debug.LogWarning(
                "Gramophone: assign Record Point.",
                this);
            return;
        }

        if (!BeginAction())
            return;

        StopAllPlayback();

        pickupRecord.PlaceInGramophone(recordPoint);
        installed = true;

        SetOutlines(false, false);
    }

    private bool BeginAction()
    {
        if (lastActionFrame == Time.frameCount)
            return false;

        lastActionFrame = Time.frameCount;
        return true;
    }

    private void PlayAudio(bool reverse)
    {
        if (!BeginAction())
            return;

        if (installed)
        {
            if (audioPuzzle == null)
            {
                Debug.LogWarning(
                    "Gramophone: assign Audio Puzzle.",
                    this);
                return;
            }

            if (emptySpeaker != null)
                emptySpeaker.Stop();

            audioPuzzle.Volume = volume;

            bool started = reverse
                ? audioPuzzle.PlayReversed()
                : audioPuzzle.PlayNormal();

            if (!started)
                return;

            reversePlaying = reverse;

            if (gameManager != null)
            {
                gameManager.recordPlaying = true;

                if (reverse)
                    gameManager.recordFixed = true;
            }
        }
        else
        {
            if (emptySpeaker == null)
                return;

            EnsureOldMusic();

            reversePlaying = reverse;

            emptySpeaker.Stop();
            emptySpeaker.clip = reverse ? emptyReverse : emptyNormal;
            emptySpeaker.volume = volume * emptyMusicVolume;
            emptySpeaker.Play();
        }
    }

    private void StopAudio()
    {
        if (!BeginAction())
            return;

        StopAllPlayback();
    }

    private void StopAllPlayback()
    {
        if (audioPuzzle != null)
            audioPuzzle.StopAudio();

        if (emptySpeaker != null)
            emptySpeaker.Stop();

        if (gameManager != null)
            gameManager.recordPlaying = false;
    }

    private void EnsureOldMusic()
    {
        if (emptyNormal != null)
            return;

        const int rate = 22050;
        const float beat = 0.55f;

        // Original low-register melody generated for this prop.
        float[] notes =
        {
            146.83f, 174.61f, 164.81f, 138.59f,
            146.83f, 130.81f, 110.00f, 138.59f,
            146.83f, 164.81f, 130.81f, 116.54f,
            110.00f, 138.59f, 130.81f, 110.00f
        };

        float duration = notes.Length * beat;

        float[] samples = new float[
            Mathf.RoundToInt(rate * duration)];

        System.Random random = new System.Random();
        float filtered = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;
            int note = Mathf.Min(
                Mathf.FloorToInt(t / beat), notes.Length - 1);

            float noteTime = t - note * beat;
            float frequency = notes[note];

            float wobble =
                1f + Mathf.Sin(t * 3.7f) * 0.003f;

            float phase =
                2f * Mathf.PI * frequency * noteTime * wobble;

            float tone =
                Mathf.Sin(phase) * 0.65f +
                Mathf.Sin(phase * 2f) * 0.18f +
                Mathf.Sin(phase * 3f) * 0.07f;

            float attack =
                Mathf.Clamp01(noteTime / 0.045f);

            float release =
                Mathf.Clamp01((beat - noteTime) / 0.1f);

            float envelope =
                attack * release *
                Mathf.Exp(-noteTime * 1.5f);

            float noise =
                (float)(random.NextDouble() * 2.0 - 1.0);

            filtered = Mathf.Lerp(filtered, noise, 0.12f);

            float crackle =
                random.NextDouble() < 0.0006
                ? noise * 0.07f
                : 0f;

            float loopFade = Mathf.Min(
                Mathf.Clamp01(t / 0.04f),
                Mathf.Clamp01((duration - t) / 0.04f));

            samples[i] =
                (tone * envelope * 0.35f +
                 filtered * 0.012f +
                 crackle) * loopFade;
        }

        emptyNormal = AudioClip.Create(
            "Generated Old Gramophone Music",
            samples.Length, 1, rate, false);

        emptyNormal.SetData(samples, 0);

        System.Array.Reverse(samples);

        emptyReverse = AudioClip.Create(
            "Generated Old Music Reversed",
            samples.Length, 1, rate, false);

        emptyReverse.SetData(samples, 0);
    }

    private Vector2 GuiPointer()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Vector3 center = playerCamera.ViewportToScreenPoint(
                new Vector3(0.5f, 0.5f, 0f));

            return new Vector2(center.x, Screen.height - center.y);
        }

        Vector2 mouse = ChestPuzzleTools.MousePosition();
        return new Vector2(mouse.x, Screen.height - mouse.y);
    }

    private bool TryGetControls(
        out Rect reverseRect,
        out Rect stopRect,
        out Rect playRect)
    {
        reverseRect = default;
        stopRect = default;
        playRect = default;

        if (playerCamera == null ||
            ChestPuzzleTools.Inspecting ||
            Time.timeScale == 0f)
            return false;

        if (Vector3.Distance(
                playerCamera.transform.position,
                transform.position) > interactionDistance + 0.4f)
            return false;

        Vector3 position;

        if (controlsPoint != null)
        {
            position = controlsPoint.position;
        }
        else if (playbackButton != null)
        {
            position = playbackButton.position +
                Vector3.up * 0.09f;
        }
        else if (recordPoint != null)
        {
            position = recordPoint.position +
                playerCamera.transform.right * 0.25f +
                Vector3.up * 0.1f;
        }
        else
        {
            return false;
        }

        Vector3 screen =
            playerCamera.WorldToScreenPoint(position);

        if (screen.z <= 0f || !ControlsVisible(position))
            return false;

        Vector3 edge = playerCamera.WorldToScreenPoint(
            position +
            playerCamera.transform.right *
            Mathf.Max(buttonWorldSize, 0.001f));

        float size = Mathf.Clamp(
            Mathf.Abs(edge.x - screen.x), 24f, 46f);

        float gap = size * 0.15f;
        float width = size * 3f + gap * 2f;

        float x = screen.x - width * 0.5f;
        float y = Screen.height - screen.y - size * 0.5f;

        reverseRect = new Rect(x, y, size, size);

        stopRect = new Rect(
            x + size + gap, y, size, size);

        playRect = new Rect(
            x + (size + gap) * 2f, y, size, size);

        return true;
    }

    private bool ControlsVisible(Vector3 position)
    {
        Ray ray = playerCamera.ScreenPointToRay(
            playerCamera.WorldToScreenPoint(position));

        float distance =
            Vector3.Distance(ray.origin, position) + 0.03f;

        Transform player = ChestPuzzleTools.Player(playerCamera);

        RaycastHit[] hits = Physics.RaycastAll(
            ray, distance, ~0, QueryTriggerInteraction.Ignore);

        float closest = float.PositiveInfinity;
        Collider nearest = null;

        foreach (RaycastHit hit in hits)
        {
            if (player != null && hit.transform.IsChildOf(player))
                continue;

            if (hit.distance >= closest)
                continue;

            closest = hit.distance;
            nearest = hit.collider;
        }

        return nearest == null ||
            nearest.GetComponentInParent<Gramophone>() == this;
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint)
            return;

        if (!TryGetControls(
                out Rect reverseRect,
                out Rect stopRect,
                out Rect playRect))
            return;

        Vector2 pointer = GuiPointer();

        DrawControl(
            reverseRect,
            0,
            reverseRect.Contains(pointer),
            IsPlaying() && reversePlaying);

        DrawControl(
            stopRect,
            1,
            stopRect.Contains(pointer),
            false);

        DrawControl(
            playRect,
            2,
            playRect.Contains(pointer),
            IsPlaying() && !reversePlaying);
    }

    private void DrawControl(
        Rect rect, int icon, bool hovered, bool active)
    {
        Color previous = GUI.color;

        GUI.color = new Color(0.03f, 0.025f, 0.02f, 0.8f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);

        GUI.color = hovered
            ? Color.green
            : active
                ? new Color(0.45f, 0.85f, 0.5f)
                : new Color(0.85f, 0.85f, 0.8f);

        const float border = 1.5f;

        GUI.DrawTexture(
            new Rect(rect.x, rect.y, rect.width, border),
            Texture2D.whiteTexture);

        GUI.DrawTexture(
            new Rect(rect.x, rect.yMax - border, rect.width, border),
            Texture2D.whiteTexture);

        GUI.DrawTexture(
            new Rect(rect.x, rect.y, border, rect.height),
            Texture2D.whiteTexture);

        GUI.DrawTexture(
            new Rect(rect.xMax - border, rect.y, border, rect.height),
            Texture2D.whiteTexture);

        if (icon == 1)
        {
            GUI.DrawTexture(
                new Rect(
                    rect.center.x - rect.width * 0.17f,
                    rect.center.y - rect.height * 0.17f,
                    rect.width * 0.34f,
                    rect.height * 0.34f),
                Texture2D.whiteTexture);
        }
        else
        {
            DrawCircularArrow(rect, icon == 2);
        }

        GUI.color = previous;
    }

    private void DrawCircularArrow(Rect rect, bool clockwise)
    {
        Vector2 center = rect.center;
        float radius = rect.width * 0.27f;
        float direction = clockwise ? 1f : -1f;
        float start = clockwise ? 45f : 135f;

        const int segments = 28;

        Vector2 previous =
            CirclePoint(center, radius, start);

        for (int i = 1; i <= segments; i++)
        {
            float angle =
                start + direction * 270f * i / segments;

            Vector2 next =
                CirclePoint(center, radius, angle);

            DrawLine(previous, next, 2f);
            previous = next;
        }

        float endAngle =
            (start + direction * 270f) * Mathf.Deg2Rad;

        Vector2 tangent = new Vector2(
            -Mathf.Sin(endAngle),
            Mathf.Cos(endAngle)) * direction;

        Vector2 radial = new Vector2(
            Mathf.Cos(endAngle),
            Mathf.Sin(endAngle));

        DrawLine(
            previous,
            previous - tangent * radius * 0.5f +
            radial * radius * 0.35f,
            2f);

        DrawLine(
            previous,
            previous - tangent * radius * 0.5f -
            radial * radius * 0.35f,
            2f);
    }

    private static Vector2 CirclePoint(
        Vector2 center, float radius, float degrees)
    {
        float angle = degrees * Mathf.Deg2Rad;

        return center + new Vector2(
            Mathf.Cos(angle),
            Mathf.Sin(angle)) * radius;
    }

    private static void DrawLine(
        Vector2 start, Vector2 end, float thickness)
    {
        Matrix4x4 previous = GUI.matrix;

        float angle = Mathf.Atan2(
            end.y - start.y,
            end.x - start.x) * Mathf.Rad2Deg;

        GUIUtility.RotateAroundPivot(angle, start);

        GUI.DrawTexture(
            new Rect(
                start.x,
                start.y - thickness * 0.5f,
                Vector2.Distance(start, end),
                thickness),
            Texture2D.whiteTexture);

        GUI.matrix = previous;
    }

    private void CreateOutlines()
    {
        Shader shader =
            Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
            return;

        redMaterial = MakeOutlineMaterial(
            shader, Color.red, CullMode.Front);

        greenMaterial = MakeOutlineMaterial(
            shader, Color.green, CullMode.Off);

        MeshFilter[] sources =
            GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter source in sources)
        {
            MeshRenderer original =
                source.GetComponent<MeshRenderer>();

            if (original == null || source.sharedMesh == null)
                continue;

            if (pickupRecord != null &&
                source.transform.IsChildOf(pickupRecord.transform))
                continue;

            bool button = playbackButton != null &&
                source.transform.IsChildOf(playbackButton);

            GameObject copy = new GameObject(
                button ? "Button Green Outline" : "Gramophone Red Outline");

            copy.layer = source.gameObject.layer;
            copy.transform.SetParent(source.transform, false);

            float scale = button ? 1.09f : 1.025f;
            Vector3 center = source.sharedMesh.bounds.center;

            copy.transform.localScale = Vector3.one * scale;
            copy.transform.localPosition = center * (1f - scale);

            copy.AddComponent<MeshFilter>().sharedMesh =
                source.sharedMesh;

            MeshRenderer renderer =
                copy.AddComponent<MeshRenderer>();

            Material[] materials = new Material[
                Mathf.Max(source.sharedMesh.subMeshCount, 1)];

            for (int i = 0; i < materials.Length; i++)
                materials[i] = button ? greenMaterial : redMaterial;

            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;

            outlines.Add(new OutlinePart
            {
                source = original,
                outline = renderer,
                center = center,
                button = button
            });
        }
    }

    private Material MakeOutlineMaterial(
        Shader shader, Color color, CullMode cull)
    {
        Material material = new Material(shader);
        material.mainTexture = Texture2D.whiteTexture;
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Cull", (float)cull);
        material.renderQueue = (int)RenderQueue.Geometry + 1;

        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);

        if (material.HasProperty("_ZWriteControl"))
            material.SetFloat("_ZWriteControl", 2f);

        return material;
    }

    private void SetOutlines(bool bodyHovered, bool buttonHovered)
    {
        foreach (OutlinePart part in outlines)
        {
            if (part.outline == null)
                continue;

            bool visible =
                (part.button ? buttonHovered : bodyHovered) &&
                part.source != null &&
                part.source.enabled &&
                part.source.gameObject.activeInHierarchy;

            part.outline.enabled = visible;

            if (visible && part.button && playerCamera != null)
            {
                Vector3 position =
                    part.source.transform.TransformPoint(
                        part.center * (1f - 1.09f));

                Vector3 away =
                    part.source.bounds.center -
                    playerCamera.transform.position;

                part.outline.transform.position =
                    position + away.normalized * 0.001f;
            }
        }
    }

    private void OnDisable()
    {
        StopAllPlayback();
        SetOutlines(false, false);
    }

    private void OnDestroy()
    {
        if (emptyNormal != null)
            Destroy(emptyNormal);

        if (emptyReverse != null)
            Destroy(emptyReverse);

        if (redMaterial != null)
            Destroy(redMaterial);

        if (greenMaterial != null)
            Destroy(greenMaterial);

        foreach (OutlinePart part in outlines)
        {
            if (part.outline != null)
                Destroy(part.outline.gameObject);
        }

        if (emptySpeaker != null)
            Destroy(emptySpeaker);
    }
}