using UnityEngine;
using UnityEngine.UI;
using Tiizino.Audio;

namespace Tiizino.Gameplay
{
    /// <summary>
    /// Builds a lightweight 3D bridge scene from primitive meshes at runtime.
    /// Attach to an empty GameObject in a URP scene and press Play.
    /// </summary>
    public class BridgeStageController : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private Color grassColor = new Color(0.105f, 0.31f, 0.29f);
        [SerializeField] private Color waterColor = new Color(0.055f, 0.34f, 0.48f);
        [SerializeField] private AudioClipSpeechService speechService;
        [Header("Music (assign a licensed child-friendly loop in the Inspector)")]
        [SerializeField] private AudioClip backgroundMusic;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.22f;
        [SerializeField] private bool musicEnabledByDefault = true;
        private AudioSource musicSource;
        private bool musicEnabled;
        [SerializeField] private TextAsset questionBankJson;
        [Tooltip("Optional font asset with Persian glyphs. Proper RTL shaping still requires a shaping solution.")]
        [SerializeField] private Font uiFont;

        private string[] prompts =
        {
            "قرمز، آبی، قرمز، آبی، بعدی کدام است؟",
            "دایره، دایره، ستاره، دایره، دایره، بعدی کدام است؟",
            "۱، ۲، ۳، بعدی کدام عدد است؟",
            "کدام مورد با بقیه فرق دارد؟"
        };
        private string[][] options =
        {
            new[] { "قرمز", "سبز", "زرد" },
            new[] { "ستاره", "مربع", "دایره" },
            new[] { "۴", "۵", "۲" },
            new[] { "سیب", "گلابی", "صندلی" }
        };
        private int[] answers = { 0, 0, 0, 2 };
        private string[] audioPhraseIds =
        {
            "grade1_bridge_q1", "grade1_bridge_q2",
            "grade1_bridge_q3", "grade1_bridge_q4"
        };
        private string[] explanations =
        {
            "آفرین! الگو یکی در میان تکرار می‌شود.",
            "در هر گروه دو دایره و بعد یک ستاره داریم.",
            "هر بار یک عدد به عدد قبلی اضافه می‌شود.",
            "صندلی وسیله است؛ بقیه میوه‌اند."
        };

        private int questionIndex;
        private int stars;
        private int currentLevelIndex;
        private string[] levelNames = { "نجات پل جنگل" };
        private int[] levelEndIndices;
        private Text levelText;
        private Text promptText;
        private Text feedbackText;
        private Button[] answerButtons;
        private Button continueLevelButton;
        private GameObject bridgeRoot;
        private GameObject titleScreenRoot;
        private GameObject[] planks;
        private Transform mascotTransform;
        private Vector3 mascotBasePosition;
        private Transform cinematicCamera;
        private Vector3 cameraLookTarget = new Vector3(0f, 0.25f, 0f);
        private Vector3 cameraBasePosition;

        private void Start()
        {
            if (buildOnStart) ShowGradeSelection();
        }

        private void Update()
        {
            // A restrained idle motion adds life without sound or distracting effects.
            if (mascotTransform != null)
            {
                Vector3 p = mascotBasePosition;
                p.y += Mathf.Sin(Time.time * 1.45f) * 0.055f;
                mascotTransform.localPosition = p;
                mascotTransform.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 0.7f) * 4f, 0f);
            }

            // Tiizino Ribbon-Cam: slow, asymmetric drift around the stage.
            // Keep this independent of the mascot so camera motion always runs.
            if (cinematicCamera != null)
            {
                float t = Time.time;
                Vector3 desired = cameraBasePosition;
                desired.x += Mathf.Sin(t * 0.19f) * 1.15f + Mathf.Sin(t * 0.071f) * 0.28f;
                desired.y += Mathf.Sin(t * 0.13f + 0.8f) * 0.24f;
                desired.z += Mathf.Cos(t * 0.16f) * 0.55f;
                cinematicCamera.position = Vector3.Lerp(cinematicCamera.position, desired, Time.deltaTime * 0.7f);

                Vector3 look = cameraLookTarget + new Vector3(
                    Mathf.Sin(t * 0.12f) * 0.22f,
                    0f,
                    Mathf.Cos(t * 0.15f) * 0.16f);
                Quaternion targetRotation = Quaternion.LookRotation(look - cinematicCamera.position, Vector3.up);
                cinematicCamera.rotation = Quaternion.Slerp(
                    cinematicCamera.rotation, targetRotation, Time.deltaTime * 0.8f);
            }
        }

        private void ShowGradeSelection()
        {
            if (titleScreenRoot != null) Destroy(titleScreenRoot);
            titleScreenRoot = new GameObject("Tiizino Grade Selection");
            Canvas canvas = titleScreenRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = titleScreenRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            titleScreenRoot.AddComponent<GraphicRaycaster>();

            UnityEngine.EventSystems.EventSystem eventSystem =
                FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventObject = new GameObject("EventSystem");
                eventSystem = eventObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
            }
            if (eventSystem.GetComponent<UnityEngine.EventSystems.BaseInputModule>() == null)
                eventSystem.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Text title = CreateMenuText(titleScreenRoot.transform, "Tiizino Title",
                new Vector2(0.5f, 0.88f), new Vector2(900f, 150f), 54);
            title.text = "تیزینو\nماجراجویی و معما";
            Text subtitle = CreateMenuText(titleScreenRoot.transform, "Grade Prompt",
                new Vector2(0.5f, 0.76f), new Vector2(900f, 100f), 32);
            subtitle.text = "پایهٔ تحصیلی‌ات را انتخاب کن";

            for (int grade = 1; grade <= 6; grade++)
            {
                int capturedGrade = grade;
                int column = (grade - 1) % 2;
                int row = (grade - 1) / 2;
                Vector2 anchor = new Vector2(column == 0 ? 0.28f : 0.72f, 0.62f - row * 0.16f);
                Button button = CreateMenuButton(titleScreenRoot.transform, "Grade " + grade,
                    anchor, new Vector2(390f, 130f));
                Text label = button.GetComponentInChildren<Text>();
                if (grade <= 2)
                {
                    int bestStars = PlayerPrefs.GetInt("Tiizino.Grade" + grade + ".BestStars", 0);
                    label.text = "پایهٔ " + ToPersianDigits(grade)
                        + "\nبهترین رکورد: " + ToPersianDigits(bestStars);
                }
                else
                {
                    label.text = "پایهٔ " + ToPersianDigits(grade) + "\nبه‌زودی";
                }
                if (grade <= 2)
                {
                    button.onClick.AddListener(() => SelectGradeAndStart(capturedGrade));
                }
                else
                {
                    button.interactable = false;
                    button.GetComponent<Image>().color = new Color(0.48f, 0.50f, 0.56f);
                }
            }

            Text note = CreateMenuText(titleScreenRoot.transform, "Available Grades Note",
                new Vector2(0.5f, 0.08f), new Vector2(920f, 110f), 24);
            note.text = "فعلاً پایه‌های اول و دوم قابل بازی هستند؛ پایه‌های ۳ تا ۶ بعداً فعال می‌شوند.";
        }

        private void SelectGradeAndStart(int grade)
        {
            PlayerPrefs.SetInt("Tiizino.SelectedGrade", Mathf.Clamp(grade, 1, 2));
            PlayerPrefs.Save();
            questionBankJson = null;
            BuildStage();
        }

        private static string ToPersianDigits(int number)
        {
            return number.ToString().Replace("0", "۰").Replace("1", "۱")
                .Replace("2", "۲").Replace("3", "۳").Replace("4", "۴")
                .Replace("5", "۵").Replace("6", "۶").Replace("7", "۷")
                .Replace("8", "۸").Replace("9", "۹");
        }

        private Text CreateMenuText(Transform parent, string name, Vector2 anchor,
            Vector2 size, int fontSize)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            Text text = go.AddComponent<Text>();
            text.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = fontSize;
            text.text = name;
            return text;
        }

        private Button CreateMenuButton(Transform parent, string name, Vector2 anchor, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.98f, 0.68f, 0.18f);
            Button button = go.AddComponent<Button>();
            Text label = CreateMenuText(go.transform, "Label", new Vector2(0.5f, 0.5f),
                new Vector2(size.x - 20f, size.y - 20f), 34);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 10f);
            labelRect.offsetMax = new Vector2(-10f, -10f);
            label.color = new Color(0.12f, 0.12f, 0.2f);
            return button;
        }

        [ContextMenu("Build Tiizino Bridge Stage")]
        public void BuildStage()
        {
            if (titleScreenRoot != null)
            {
                Destroy(titleScreenRoot);
                titleScreenRoot = null;
            }
            // Reset runtime state so rebuilding the stage always starts cleanly.
            CancelInvoke();
            questionIndex = 0;
            stars = 0;
            currentLevelIndex = 0;
            levelNames = new[] { "نجات پل جنگل" };
            levelEndIndices = null;
            levelText = null;
            continueLevelButton = null;
            promptText = null;
            feedbackText = null;
            answerButtons = null;
            planks = null;
            if (speechService == null) speechService = GetComponent<AudioClipSpeechService>();
            ResetBuiltInQuestionBank();
            if (questionBankJson == null)
            {
                // Select the grade-specific original bank when a grade was chosen in the title flow.
                int selectedGrade = PlayerPrefs.GetInt("Tiizino.SelectedGrade", 1);
                string resourceName = selectedGrade == 2
                    ? "Grade2_QuestionBank"
                    : "Grade1_QuestionBank";
                questionBankJson = Resources.Load<TextAsset>(resourceName);
                if (questionBankJson == null)
                    questionBankJson = Resources.Load<TextAsset>("Stage01_FourLevels_DRAFT");
            }
            LoadQuestionBank();

            ClearOldStage();
            CreateWorld();
            SetupBackgroundMusic();
            CreateCameraAndLight();
            CreateUI();
            ShowQuestion();
        }

        [System.Serializable]
        private sealed class QuestionBank
        {
            public QuestionData[] questions;
            public LevelData[] levels;
        }

        [System.Serializable]
        private sealed class LevelData
        {
            public int levelId;
            public string name;
            public string skill;
            public QuestionData[] questions;
        }

        private void ResetBuiltInQuestionBank()
        {
            prompts = new[]
            {
                "قرمز، آبی، قرمز، آبی، بعدی کدام است؟",
                "دایره، دایره، ستاره، دایره، دایره، بعدی کدام است؟",
                "۱، ۲، ۳، بعدی کدام عدد است؟",
                "کدام مورد با بقیه فرق دارد؟"
            };
            options = new[]
            {
                new[] { "قرمز", "سبز", "زرد" },
                new[] { "ستاره", "مربع", "دایره" },
                new[] { "۴", "۵", "۲" },
                new[] { "سیب", "گلابی", "صندلی" }
            };
            answers = new[] { 0, 0, 0, 2 };
            audioPhraseIds = new[]
            {
                "grade1_bridge_q1", "grade1_bridge_q2",
                "grade1_bridge_q3", "grade1_bridge_q4"
            };
            explanations = new[]
            {
                "آفرین! الگو یکی در میان تکرار می‌شود.",
                "در هر گروه دو دایره و بعد یک ستاره داریم.",
                "هر بار یک عدد به عدد قبلی اضافه می‌شود.",
                "صندلی وسیله است؛ بقیه میوه‌اند."
            };
        }

        private void LoadQuestionBank()
        {
            if (questionBankJson == null) return;

            try
            {
                QuestionBank bank = JsonUtility.FromJson<QuestionBank>(questionBankJson.text);
                if (bank == null)
                {
                    Debug.LogWarning("Tiizino: question bank is empty; using built-in sample questions.", this);
                    return;
                }

                // Accept the original flat format and the new four-level format.
                System.Collections.Generic.List<QuestionData> flattened =
                    new System.Collections.Generic.List<QuestionData>();
                System.Collections.Generic.List<string> parsedLevelNames =
                    new System.Collections.Generic.List<string>();
                System.Collections.Generic.List<int> parsedLevelEnds =
                    new System.Collections.Generic.List<int>();
                if (bank.levels != null && bank.levels.Length > 0)
                {
                    for (int levelIndex = 0; levelIndex < bank.levels.Length; levelIndex++)
                    {
                        LevelData level = bank.levels[levelIndex];
                        if (level == null || level.questions == null || level.questions.Length == 0)
                        {
                            Debug.LogWarning("Tiizino: level " + (levelIndex + 1) + " has no questions; using built-in sample questions.", this);
                            return;
                        }
                        parsedLevelNames.Add(string.IsNullOrWhiteSpace(level.name)
                            ? "لول " + (levelIndex + 1) : level.name);
                        foreach (QuestionData question in level.questions)
                            flattened.Add(question);
                        parsedLevelEnds.Add(flattened.Count);
                    }
                }
                else if (bank.questions != null)
                {
                    flattened.AddRange(bank.questions);
                }

                if (flattened.Count == 0)
                {
                    Debug.LogWarning("Tiizino: question bank is empty; using built-in sample questions.", this);
                    return;
                }

                for (int i = 0; i < flattened.Count; i++)
                {
                    if (flattened[i] == null || !flattened[i].IsValid())
                    {
                        Debug.LogWarning("Tiizino: invalid question at JSON index " + i + "; using built-in sample questions.", this);
                        return;
                    }
                    for (int optionIndex = 0; optionIndex < flattened[i].options.Length; optionIndex++)
                    {
                        for (int previousIndex = 0; previousIndex < optionIndex; previousIndex++)
                        {
                            if (string.Equals(flattened[i].options[optionIndex], flattened[i].options[previousIndex],
                                System.StringComparison.Ordinal))
                            {
                                Debug.LogWarning("Tiizino: question " + i + " contains duplicate options; using built-in sample questions.", this);
                                return;
                            }
                        }
                    }
                    if (i > 0 && flattened[i].options.Length != flattened[0].options.Length)
                    {
                        Debug.LogWarning("Tiizino: every question must have the same number of options; using built-in sample questions.", this);
                        return;
                    }
                }

                int count = flattened.Count;
                if (parsedLevelNames.Count > 0)
                {
                    levelNames = parsedLevelNames.ToArray();
                    levelEndIndices = parsedLevelEnds.ToArray();
                }
                else
                {
                    levelNames = new[] { "نجات پل جنگل" };
                    levelEndIndices = new[] { count };
                }
                prompts = new string[count];
                options = new string[count][];
                answers = new int[count];
                audioPhraseIds = new string[count];
                explanations = new string[count];

                for (int i = 0; i < count; i++)
                {
                    QuestionData question = flattened[i];
                    prompts[i] = question.prompt;
                    options[i] = question.options;
                    answers[i] = question.correctOptionIndex;
                    audioPhraseIds[i] = question.audioPhraseId;
                    explanations[i] = question.explanation ?? string.Empty;
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("Tiizino: could not read question bank JSON. Using built-in sample questions. " + exception.Message, this);
            }
        }

        private void ClearOldStage()
        {
            Transform old = transform.Find("GeneratedStage");
            if (old != null) Destroy(old.gameObject);
        }

        private void SetupBackgroundMusic()
        {
            musicEnabled = PlayerPrefs.GetInt("Tiizino.MusicEnabled", musicEnabledByDefault ? 1 : 0) == 1;
            if (musicSource == null)
            {
                musicSource = GetComponent<AudioSource>();
                if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            }
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = Mathf.Clamp01(musicVolume);
            musicSource.clip = backgroundMusic;
            if (backgroundMusic != null && musicEnabled) musicSource.Play();
            else musicSource.Stop();
        }

        private void ToggleBackgroundMusic()
        {
            musicEnabled = !musicEnabled;
            PlayerPrefs.SetInt("Tiizino.MusicEnabled", musicEnabled ? 1 : 0);
            PlayerPrefs.Save();
            if (musicSource == null || backgroundMusic == null) return;
            if (musicEnabled)
            {
                musicSource.volume = Mathf.Clamp01(musicVolume);
                if (!musicSource.isPlaying) musicSource.Play();
            }
            else musicSource.Stop();
        }

        private void CreateWorld()
        {
            bridgeRoot = new GameObject("GeneratedStage");
            bridgeRoot.transform.SetParent(transform, false);

            MakeCube("Forest Ground Left", new Vector3(-4f, -0.35f, 0f),
                new Vector3(5f, 0.7f, 8f), grassColor);
            MakeCube("Forest Ground Right", new Vector3(4f, -0.35f, 0f),
                new Vector3(5f, 0.7f, 8f), grassColor);
            MakeCube("Magic River", new Vector3(0f, -0.05f, 0f),
                new Vector3(3.2f, 0.15f, 8f), waterColor);

            planks = new GameObject[prompts.Length];
            for (int i = 0; i < planks.Length; i++)
            {
                planks[i] = MakeCube("Bridge Plank " + (i + 1),
                    new Vector3(-1.2f + i * 0.8f, 0.18f, 0f),
                    new Vector3(0.7f, 0.22f, 1.5f),
                    new Color(0.91f, 0.57f, 0.25f));
                planks[i].SetActive(false);
            }

            // Friendly low-poly mascot stand-in, made only from primitive meshes.
            GameObject mascot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mascot.name = "Tiizino Mascot Placeholder";
            mascot.transform.SetParent(bridgeRoot.transform, false);
            mascot.transform.localPosition = new Vector3(-3.0f, 0.65f, -1.5f);
            mascot.transform.localScale = new Vector3(0.9f, 0.9f, 0.75f);
            SetColor(mascot, new Color(0.95f, 0.36f, 0.22f));
            mascotTransform = mascot.transform;
            mascotBasePosition = mascot.transform.localPosition;

            GameObject cap = MakeCube("Mascot Cap Placeholder",
                new Vector3(-3f, 1.18f, -1.5f), new Vector3(0.8f, 0.12f, 0.65f),
                new Color(0.10f, 0.15f, 0.23f));
            cap.transform.SetParent(bridgeRoot.transform, true);

            // Bright stepping-stone accents make the route easier for young players to read.
            for (int i = 0; i < 8; i++)
            {
                float x = i % 2 == 0 ? -2.1f : 2.1f;
                float z = -3.1f + (i / 2) * 1.9f;
                GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                stone.name = "River Pebble " + i;
                stone.transform.SetParent(bridgeRoot.transform, false);
                stone.transform.localPosition = new Vector3(x, 0.02f, z);
                stone.transform.localScale = new Vector3(0.34f, 0.12f, 0.24f);
                SetColor(stone, i % 2 == 0 ? new Color(0.95f, 0.65f, 0.30f) : new Color(0.22f, 0.66f, 0.68f));
            }

            // Small decorative trees on both banks.
            for (int i = 0; i < 6; i++)
            {
                float x = i % 2 == 0 ? -4f : 4f;
                float z = -2.7f + (i / 2) * 2.5f;
                GameObject trunk = MakeCube("Tree Trunk " + i,
                    new Vector3(x, 0.45f, z), new Vector3(0.25f, 1.4f, 0.25f),
                    new Color(0.25f, 0.16f, 0.13f));
                GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.name = "Tree Crown " + i;
                crown.transform.SetParent(bridgeRoot.transform, false);
                crown.transform.localPosition = new Vector3(x, 1.45f, z);
                crown.transform.localScale = Vector3.one * 1.1f;
                SetColor(crown, new Color(0.07f, 0.25f, 0.22f));
            }
        }

        private GameObject MakeCube(string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(bridgeRoot.transform, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            SetColor(go, color);
            return go;
        }

        private static void SetColor(GameObject go, Color color)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer == null) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader != null)
            {
                MaterialPropertyBlock properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", color);
                properties.SetColor("_Color", color);
                renderer.SetPropertyBlock(properties);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private void CreateCameraAndLight()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cam = cameraObject.AddComponent<Camera>();
            }
            cam.transform.position = new Vector3(0f, 8.5f, -12f);
            cam.transform.rotation = Quaternion.LookRotation(cameraLookTarget - cam.transform.position, Vector3.up);
            cameraBasePosition = cam.transform.position;
            cinematicCamera = cam.transform;
            cam.fieldOfView = 43f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.075f, 0.13f, 0.19f);

            if (FindObjectOfType<Light>() == null)
            {
                GameObject lightObject = new GameObject("Tiizino Sun");
                Light sun = lightObject.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 1.15f;
                lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            }
        }

        private void CreateUI()
        {
            GameObject canvasObject = new GameObject("Tiizino UI");
            canvasObject.transform.SetParent(bridgeRoot.transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasObject.AddComponent<GraphicRaycaster>();

            UnityEngine.EventSystems.EventSystem eventSystem =
                FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventObject = new GameObject("EventSystem");
                eventSystem = eventObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
            }

            // Ensure an existing EventSystem can process the legacy UI buttons.
            if (eventSystem.GetComponent<UnityEngine.EventSystems.BaseInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Button musicButton = CreateButton(canvasObject.transform, "Music Toggle", new Vector2(0.5f, 0.985f),
                new Vector2(0.30f, 0.04f));
            musicButton.GetComponentInChildren<Text>().text = "موسیقی: روشن/خاموش";
            musicButton.onClick.AddListener(ToggleBackgroundMusic);

            // Persistent in-game grade switch for the two prepared starter banks.
            Button gradeOneButton = CreateButton(canvasObject.transform, "Grade 1", new Vector2(0.27f, 0.925f),
                new Vector2(0.38f, 0.04f));
            gradeOneButton.GetComponentInChildren<Text>().text = "پایهٔ اول";
            gradeOneButton.onClick.AddListener(() => SelectGrade(1));
            Button gradeTwoButton = CreateButton(canvasObject.transform, "Grade 2", new Vector2(0.73f, 0.925f),
                new Vector2(0.38f, 0.04f));
            gradeTwoButton.GetComponentInChildren<Text>().text = "پایهٔ دوم";
            gradeTwoButton.onClick.AddListener(() => SelectGrade(2));

            levelText = CreateText(canvasObject.transform, "Level", new Vector2(0.5f, 0.875f),
                new Vector2(0.88f, 0.045f), 24, TextAnchor.MiddleRight);
            promptText = CreateText(canvasObject.transform, "Prompt", new Vector2(0.5f, 0.84f),
                new Vector2(0.88f, 0.15f), 32, TextAnchor.MiddleRight);
            feedbackText = CreateText(canvasObject.transform, "Feedback", new Vector2(0.5f, 0.70f),
                new Vector2(0.85f, 0.06f), 26, TextAnchor.MiddleRight);
            Button repeatButton = CreateButton(canvasObject.transform, "Repeat Question",
                new Vector2(0.5f, 0.62f), new Vector2(0.55f, 0.055f));
            repeatButton.GetComponentInChildren<Text>().text = "شنیدن دوبارهٔ سؤال";
            repeatButton.onClick.AddListener(RepeatCurrentQuestion);

            continueLevelButton = CreateButton(canvasObject.transform, "Continue Level",
                new Vector2(0.5f, 0.25f), new Vector2(0.65f, 0.075f));
            continueLevelButton.GetComponentInChildren<Text>().text = "رفتن به لول بعدی";
            continueLevelButton.onClick.AddListener(ContinueToNextLevel);
            continueLevelButton.gameObject.SetActive(false);

            int optionCount = options.Length > 0 && options[0] != null ? options[0].Length : 0;
            answerButtons = new Button[optionCount];
            for (int i = 0; i < answerButtons.Length; i++)
            {
                int captured = i;
                Button button = CreateButton(canvasObject.transform, "Answer " + i,
                    new Vector2(0.5f, 0.49f - i * 0.105f), new Vector2(0.7f, 0.085f));
                button.onClick.AddListener(() => ChooseAnswer(captured));
                answerButtons[i] = button;
            }
        }

        private void SelectGrade(int grade)
        {
            PlayerPrefs.SetInt("Tiizino.SelectedGrade", Mathf.Clamp(grade, 1, 2));
            PlayerPrefs.Save();
            // Clear any Inspector override so the selected grade-specific resource is loaded.
            questionBankJson = null;
            BuildStage();
        }

        private Text CreateText(Transform parent, string name, Vector2 anchor, Vector2 size,
            int fontSize, TextAnchor alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(1000f * size.x, 1900f * size.y);
            rect.anchoredPosition = Vector2.zero;
            Text text = go.AddComponent<Text>();
            text.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 18;
            text.resizeTextMaxSize = fontSize;
            text.supportRichText = false;
            text.text = name;
            return text;
        }

        private Button CreateButton(Transform parent, string name, Vector2 anchor, Vector2 size)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = new Vector2(900f * size.x, 1900f * size.y);
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.98f, 0.68f, 0.18f);
            Button button = go.AddComponent<Button>();
            Text label = CreateText(go.transform, "Label", new Vector2(0.5f, 0.5f),
                new Vector2(0.9f, 0.8f), 30, TextAnchor.MiddleCenter);
            // Button labels must fit the button rect, not use canvas-sized dimensions.
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.color = new Color(0.12f, 0.12f, 0.2f);
            label.text = "گزینه";
            return button;
        }

        private int CurrentLevelEnd()
        {
            if (levelEndIndices == null || levelEndIndices.Length == 0) return prompts.Length;
            return levelEndIndices[Mathf.Clamp(currentLevelIndex, 0, levelEndIndices.Length - 1)];
        }

        private void ShowQuestion()
        {
            if (levelText != null)
                levelText.text = "لول " + (currentLevelIndex + 1) + " از " + levelNames.Length + " — " + levelNames[currentLevelIndex];
            if (questionIndex >= CurrentLevelEnd())
            {
                foreach (Button button in answerButtons) button.gameObject.SetActive(false);
                if (continueLevelButton != null) continueLevelButton.gameObject.SetActive(currentLevelIndex + 1 < levelNames.Length);
                if (currentLevelIndex + 1 < levelNames.Length)
                {
                    promptText.text = "آفرین! این لول را تمام کردی.";
                    feedbackText.text = "ستاره‌های جمع‌شده: " + stars;
                }
                else
                {
                    promptText.text = "آفرین! پل جنگل نجات پیدا کرد!";
                    int grade = Mathf.Clamp(PlayerPrefs.GetInt("Tiizino.SelectedGrade", 1), 1, 2);
                    string bestKey = "Tiizino.Grade" + grade + ".BestStars";
                    int bestStars = PlayerPrefs.GetInt(bestKey, 0);
                    bool newRecord = stars > bestStars;
                    if (newRecord)
                    {
                        bestStars = stars;
                        PlayerPrefs.SetInt(bestKey, bestStars);
                        PlayerPrefs.Save();
                    }
                    feedbackText.text = "ستاره‌های این بازی: " + stars + " از " + prompts.Length
                        + " | بهترین رکورد: " + bestStars
                        + (newRecord ? " — رکورد تازه!" : string.Empty);
                }
                return;
            }
            if (continueLevelButton != null) continueLevelButton.gameObject.SetActive(false);
            promptText.text = prompts[questionIndex];
            feedbackText.text = "بهترین پاسخ را انتخاب کن";
            SpeakCurrentQuestion();
            for (int i = 0; i < answerButtons.Length; i++)
            {
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].interactable = true;
                answerButtons[i].GetComponentInChildren<Text>().text = options[questionIndex][i];
            }
        }

        private void ContinueToNextLevel()
        {
            if (currentLevelIndex + 1 >= levelNames.Length || questionIndex < CurrentLevelEnd()) return;
            currentLevelIndex++;
            ShowQuestion();
        }

        public void RepeatCurrentQuestion()
        {
            SpeakCurrentQuestion();
        }

        private void SpeakCurrentQuestion()
        {
            if (speechService == null || questionIndex < 0
                || questionIndex >= audioPhraseIds.Length)
                return;

            speechService.Speak(audioPhraseIds[questionIndex]);
        }

        private void ChooseAnswer(int choice)
        {
            if (questionIndex < 0 || questionIndex >= prompts.Length
                || choice < 0 || options[questionIndex] == null
                || choice >= options[questionIndex].Length) return;
            speechService?.Stop();
            bool correct = choice == answers[questionIndex];
            foreach (Button button in answerButtons) button.interactable = false;

            if (correct)
            {
                stars++;
                if (questionIndex < planks.Length) planks[questionIndex].SetActive(true);
                feedbackText.text = "آفرین! " + explanations[questionIndex];
            }
            else
            {
                // Keep the explanation hidden until the child answers correctly.
                feedbackText.text = "یک بار دیگر فکر کن! به الگو و گزینه‌ها دقت کن.";
            }

            if (correct)
            {
                Invoke(nameof(NextQuestion), 1.8f);
            }
            else
            {
                Invoke(nameof(AllowRetry), 1.8f);
            }
        }

        private void AllowRetry()
        {
            if (questionIndex >= prompts.Length) return;
            feedbackText.text = "دوباره امتحان کن؛ تو می‌تونی!";
            foreach (Button button in answerButtons) button.interactable = true;
        }

        private void NextQuestion()
        {
            questionIndex++;
            ShowQuestion();
        }
    }
}
