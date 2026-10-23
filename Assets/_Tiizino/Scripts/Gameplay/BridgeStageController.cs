using UnityEngine;
using UnityEngine.UI;

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

        private readonly string[] prompts =
        {
            "قرمز، آبی، قرمز، آبی، بعدی کدام است؟",
            "دایره، دایره، ستاره، دایره، دایره، بعدی کدام است؟",
            "۱، ۲، ۳، بعدی کدام عدد است؟",
            "کدام مورد با بقیه فرق دارد؟"
        };
        private readonly string[][] options =
        {
            new[] { "قرمز", "سبز", "زرد" },
            new[] { "ستاره", "مربع", "دایره" },
            new[] { "۴", "۵", "۲" },
            new[] { "سیب", "گلابی", "صندلی" }
        };
        private readonly int[] answers = { 0, 0, 0, 2 };
        private readonly string[] explanations =
        {
            "آفرین! الگو یکی در میان تکرار می‌شود.",
            "در هر گروه دو دایره و بعد یک ستاره داریم.",
            "هر بار یک عدد به عدد قبلی اضافه می‌شود.",
            "صندلی وسیله است؛ بقیه میوه‌اند."
        };

        private int questionIndex;
        private int stars;
        private Text promptText;
        private Text feedbackText;
        private Button[] answerButtons;
        private GameObject bridgeRoot;
        private GameObject[] planks;

        private void Start()
        {
            if (buildOnStart) BuildStage();
        }

        [ContextMenu("Build Tiizino Bridge Stage")]
        public void BuildStage()
        {
            ClearOldStage();
            CreateWorld();
            CreateCameraAndLight();
            CreateUI();
            ShowQuestion();
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

            planks = new GameObject[4];
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
                Material material = new Material(shader);
                material.color = color;
                renderer.material = material;
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
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject eventObject = GameObject.Find("EventSystem");
            if (eventObject == null)
            {
                eventObject = new GameObject("EventSystem");
                eventObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            promptText = CreateText(canvasObject.transform, "Prompt", new Vector2(0.5f, 0.86f),
                new Vector2(0.88f, 0.15f), 32, TextAnchor.MiddleCenter);
            feedbackText = CreateText(canvasObject.transform, "Feedback", new Vector2(0.5f, 0.66f),
                new Vector2(0.85f, 0.08f), 26, TextAnchor.MiddleCenter);
            answerButtons = new Button[3];
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
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
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

        private void ShowQuestion()
        {
            if (questionIndex >= prompts.Length)
            {
                promptText.text = "آفرین! پل جنگل نجات پیدا کرد!";
                feedbackText.text = "ستاره‌های به‌دست‌آمده: " + stars + " از " + prompts.Length;
                foreach (Button button in answerButtons) button.gameObject.SetActive(false);
                return;
            }

            promptText.text = prompts[questionIndex];
            feedbackText.text = "بهترین پاسخ را انتخاب کن";
            for (int i = 0; i < answerButtons.Length; i++)
            {
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].interactable = true;
                answerButtons[i].GetComponentInChildren<Text>().text = options[questionIndex][i];
            }
        }

        private void ChooseAnswer(int choice)
        {
            if (questionIndex >= prompts.Length) return;
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
                feedbackText.text = "یک بار دیگر فکر کن! " + explanations[questionIndex];
            }

            // Keep the first prototype simple: proceed after either answer.
            Invoke(nameof(NextQuestion), 1.8f);
        }

        private void NextQuestion()
        {
            questionIndex++;
            ShowQuestion();
        }
    }
}
