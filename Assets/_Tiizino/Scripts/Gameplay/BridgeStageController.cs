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
        [SerializeField] private Color grassColor = new Color(0.22f, 0.55f, 0.28f);
        [SerializeField] private Color waterColor = new Color(0.12f, 0.58f, 0.82f);
        [SerializeField] private AudioClipSpeechService speechService;
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
        private GameObject[] planks;

        private void Start()
        {
            if (buildOnStart) BuildStage();
        }

        [ContextMenu("Build Tiizino Bridge Stage")]
        public void BuildStage()
        {
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
            LoadQuestionBank();

            ClearOldStage();
            CreateWorld();
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
                    new Color(0.85f, 0.55f, 0.24f));
                planks[i].SetActive(false);
            }

            // Friendly low-poly mascot stand-in, made only from primitive meshes.
            GameObject mascot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mascot.name = "Tiizino Mascot Placeholder";
            mascot.transform.SetParent(bridgeRoot.transform, false);
            mascot.transform.localPosition = new Vector3(-3.0f, 0.65f, -1.5f);
            mascot.transform.localScale = new Vector3(0.9f, 0.9f, 0.75f);
            SetColor(mascot, new Color(1f, 0.62f, 0.12f));

            GameObject cap = MakeCube("Mascot Cap Placeholder",
                new Vector3(-3f, 1.18f, -1.5f), new Vector3(0.8f, 0.12f, 0.65f),
                new Color(0.08f, 0.12f, 0.28f));
            cap.transform.SetParent(bridgeRoot.transform, true);

            // Small decorative trees on both banks.
            for (int i = 0; i < 6; i++)
            {
                float x = i % 2 == 0 ? -4f : 4f;
                float z = -2.7f + (i / 2) * 2.5f;
                GameObject trunk = MakeCube("Tree Trunk " + i,
                    new Vector3(x, 0.45f, z), new Vector3(0.25f, 1.4f, 0.25f),
                    new Color(0.35f, 0.2f, 0.1f));
                GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.name = "Tree Crown " + i;
                crown.transform.SetParent(bridgeRoot.transform, false);
                crown.transform.localPosition = new Vector3(x, 1.45f, z);
                crown.transform.localScale = Vector3.one * 1.1f;
                SetColor(crown, new Color(0.12f, 0.38f, 0.18f));
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
            cam.transform.rotation = Quaternion.Euler(32f, 0f, 0f);
            cam.fieldOfView = 48f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.58f, 0.82f, 0.96f);

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

            levelText = CreateText(canvasObject.transform, "Level", new Vector2(0.5f, 0.94f),
                new Vector2(0.88f, 0.045f), 24, TextAnchor.MiddleCenter);
            promptText = CreateText(canvasObject.transform, "Prompt", new Vector2(0.5f, 0.84f),
                new Vector2(0.88f, 0.15f), 32, TextAnchor.MiddleCenter);
            feedbackText = CreateText(canvasObject.transform, "Feedback", new Vector2(0.5f, 0.70f),
                new Vector2(0.85f, 0.06f), 26, TextAnchor.MiddleCenter);
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
                    feedbackText.text = "همهٔ لول‌ها تمام شدند. ستاره‌ها: " + stars + " از " + prompts.Length;
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
