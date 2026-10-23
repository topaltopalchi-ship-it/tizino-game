import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

const html = readFileSync(new URL("../Preview/index.html", import.meta.url), "utf8");
const check = (condition, message) => assert.ok(condition, message);

check(/<html[^>]*lang="fa"[^>]*dir="rtl"/i.test(html), "Preview must declare Persian RTL layout");
check((html.match(/<script\b/g) ?? []).length === 1, "Expected one inline script");
check((html.match(/<\/script>/g) ?? []).length === 1, "Inline script must close");
check(!/(?:src|href)\s*=\s*["']https?:\/\//i.test(html), "Preview should not require external assets");
check(html.includes('id="bridgeCount"'), "Bridge counter element is missing");
check(html.includes('$("bridgeCount").textContent=fa(stars)'), "Bridge counter must update with progress");
const renderBody = html.match(/function render\(\)\{([\s\S]*?)\nfunction choose\(/)?.[1] ?? "";
check(renderBody.indexOf('$("bridgeCount").textContent=fa(stars)') >= 0, "Render must update bridge counter");
check(renderBody.indexOf('$("bridgeCount").textContent=fa(stars)') < renderBody.indexOf("if(!q)"), "Bridge counter must update before the stage-complete early return");
check(renderBody.includes('$("repeat").disabled=!q'), "Repeat-question control should be disabled after stage completion");
check(renderBody.startsWith('locked=false;$("repeat").disabled=index>=questions.length;'),
  "Render must disable the repeat-question control before showing the completed-stage screen");
check(html.includes("restart") || html.includes("شروع دوباره"), "Restart control should be present");
check(html.includes("feedback"), "Answer feedback region should be present");
check(html.includes('role="status" aria-live="polite"'), "Feedback should be announced to assistive technology");
check(html.includes(".answer:focus-visible"), "Keyboard focus styling is missing");
check(html.includes("prefers-reduced-motion:reduce"), "Reduced-motion preference should be respected");

const script = html.match(/<script\b[^>]*>([\s\S]*?)<\/script>/i)?.[1];
check(Boolean(script), "Could not extract the inline script");
// Syntax-only compilation: does not execute browser DOM code.
new Function(script);
const questionCount = (script.match(/\{\s*p\s*:/g) ?? []).length;
check(questionCount === 4, `Expected 4 sample questions; found ${questionCount}`);


check(html.includes('id="repeat"'), "Repeat-question control is missing");
check(html.includes('$("repeat").disabled=!q||locked'), "Repeat-question control must be disabled while an answer is resolving or after stage completion");
check(html.includes("نمایش دوبارهٔ سؤال"), "Repeat-question control should describe its visual action clearly");
check(html.includes('$("feedback").textContent="سؤال دوباره نمایش داده شد؛ پاسخ را با دقت انتخاب کن."'), "Repeat-question action should provide clear feedback without implying unavailable speech audio");
check(html.includes('$("restart").addEventListener("click",()=>{if(pendingTurnTimer!==null){clearTimeout(pendingTurnTimer);pendingTurnTimer=null}index=0;stars=0;locked=false;render()})'), "Restart action should cancel pending callbacks and reset game state");
check(script.includes("choice===q.a"), "Answer selection should compare against the answer key");
check(script.includes('locked=true;$("repeat").disabled=true;'), "Resolving an answer must disable repeat-question interaction");
check(script.includes('pendingTurnTimer=null;locked=false;$("repeat").disabled=false;'), "Retry state must re-enable repeat-question interaction");
check(script.includes("pendingTurnTimer") && script.includes("clearTimeout(pendingTurnTimer)"), "Restart must cancel any pending answer timer before resetting progress");
check(script.includes("pendingTurnTimer=null;index++;render()"), "A pending correct-answer timer must clear its handle before advancing");
check(script.includes("document.querySelectorAll(\".answer\").forEach(b=>b.disabled=false)"), "Wrong answers should allow another attempt");

const questionLines = script.split("\n").filter(line => /^\{p:/.test(line.trim()));
check(questionLines.length === 4, `Expected 4 question records; found ${questionLines.length}`);
for (const [i, line] of questionLines.entries()) {
  const options = line.match(/o:\[(.*?)\],a:(\d+),e:/);
  check(Boolean(options), `Question ${i + 1} is missing options or answer index`);
  const optionCount = (options[1].match(/"/g) ?? []).length / 2;
  const answerIndex = Number(options[2]);
  check(optionCount === 3, `Question ${i + 1} must have 3 options`);
  check(Number.isInteger(answerIndex) && answerIndex >= 0 && answerIndex < optionCount,
    `Question ${i + 1} answer index is out of range`);
}

console.log("Preview smoke checks passed (static checks and JavaScript syntax only).");

const levelBankPath = new URL("../Assets/_Tiizino/Data/Questions/Stage01_FourLevels_DRAFT.json", import.meta.url);
const levelBank = JSON.parse(readFileSync(levelBankPath, "utf8"));
const resourceBankPath = new URL("../Assets/_Tiizino/Resources/Stage01_FourLevels_DRAFT.json", import.meta.url);
const resourceBank = JSON.parse(readFileSync(resourceBankPath, "utf8"));
check(JSON.stringify(resourceBank) === JSON.stringify(levelBank),
  "Unity Resources question bank must match the canonical draft bank");
check(levelBank.stageId === "stage01_bridge", "Four-level bank must identify stage01_bridge");
check(Array.isArray(levelBank.levels) && levelBank.levels.length === 4, "Draft bank must contain exactly four levels");
for (const [levelIndex, level] of levelBank.levels.entries()) {
  check(level.levelId === levelIndex + 1, `Level ${levelIndex + 1} must have a sequential levelId`);
  check(typeof level.name === "string" && level.name.trim().length > 0, `Level ${levelIndex + 1} needs a name`);
  check(typeof level.skill === "string" && level.skill.trim().length > 0,
    `Level ${levelIndex + 1} needs a named learning skill`);
  check(Array.isArray(level.questions) && level.questions.length >= 2, `Level ${levelIndex + 1} needs at least two questions`);
  for (const [questionIndex, question] of level.questions.entries()) {
    check(typeof question.prompt === "string" && question.prompt.trim().length > 0,
      `Level ${levelIndex + 1}, question ${questionIndex + 1} needs a prompt`);
    check(Array.isArray(question.options) && question.options.length === 3,
      `Level ${levelIndex + 1}, question ${questionIndex + 1} must have three options`);
    check(question.options.every(option => typeof option === "string" && option.trim().length > 0),
      `Level ${levelIndex + 1}, question ${questionIndex + 1} has an empty option`);
    check(new Set(question.options).size === question.options.length,
      `Level ${levelIndex + 1}, question ${questionIndex + 1} has duplicate answer options`);
    const normalizedOptions = question.options.map(option => option.normalize("NFKC").trim().replace(/\s+/gu, " ").toLocaleUpperCase("fa"));
    check(new Set(normalizedOptions).size === normalizedOptions.length,
      `Level ${levelIndex + 1}, question ${questionIndex + 1} has duplicate options after Unicode/whitespace normalization`);
    check(Number.isInteger(question.difficulty) && question.difficulty >= 1 && question.difficulty <= 3,
      `Level ${levelIndex + 1}, question ${questionIndex + 1} needs difficulty from 1 to 3`);
    check(Number.isInteger(question.correctOptionIndex)
      && question.correctOptionIndex >= 0 && question.correctOptionIndex < question.options.length,
      `Level ${levelIndex + 1}, question ${questionIndex + 1} has an invalid answer index`);
    check(typeof question.explanation === "string" && question.explanation.trim().length > 0,
      `Level ${levelIndex + 1}, question ${questionIndex + 1} needs feedback text`);
  }
}
const expectedLevelEnds = [];
let cumulativeQuestionCount = 0;
for (const level of levelBank.levels) {
  cumulativeQuestionCount += level.questions.length;
  expectedLevelEnds.push(cumulativeQuestionCount);
}
check(expectedLevelEnds.length === 4 && expectedLevelEnds.every((end, i) => end === [10, 20, 30, 40][i]),
  "Expanded stage draft should produce cumulative level ends [10, 20, 30, 40]");
check(cumulativeQuestionCount === 40, "Expanded stage draft must contain forty questions total");
check(levelBank.levels.every(level => level.questions.length === 10),
  "Each of the four stage levels must contain ten questions in this draft");
check(levelBank.levels.every(level => typeof level.skill === "string" && level.skill.trim()),
  "Every level must declare its learning skill");

const normalizePrompt = prompt => prompt.normalize("NFKC").trim().replace(/\s+/gu, " ").toLocaleUpperCase("fa");
check(normalizePrompt("  سؤال\t   آزمایشی \n") === normalizePrompt("سؤال آزمایشی"),
  "Prompt normalization must collapse repeated spaces, tabs, and newlines");
const normalizedPrompts = new Set();
for (const [levelIndex, level] of levelBank.levels.entries()) {
  for (const [questionIndex, question] of level.questions.entries()) {
    const compactPrompt = normalizePrompt(question.prompt);
    check(!normalizedPrompts.has(compactPrompt),
      `Level ${levelIndex + 1}, question ${questionIndex + 1} repeats an earlier prompt after Unicode/whitespace normalization`);
    normalizedPrompts.add(compactPrompt);
  }
}
check(normalizedPrompts.size === cumulativeQuestionCount,
  "Every question across all four levels must have a distinct normalized prompt");
console.log("Four-level draft question bank structure and boundaries validated.");

const controllerPath = new URL("../Assets/_Tiizino/Scripts/Gameplay/BridgeStageController.cs", import.meta.url);
const controller = readFileSync(controllerPath, "utf8");
const editorPath = new URL("../Assets/Editor/TiizinoProjectValidator.cs", import.meta.url);
const editor = readFileSync(editorPath, "utf8");
check(controller.includes('Resources.Load<TextAsset>("Stage01_FourLevels_DRAFT")'),
  "Unity controller should load the packaged four-level bank when no asset is assigned");
check(controller.includes("private Text levelText;"), "Unity controller must declare its level label field");
check(controller.includes("private Button continueLevelButton;"), "Unity controller must declare its level continuation button");
check(controller.includes("private string[] levelNames"), "Unity controller must retain level names");
check(controller.includes("private int[] levelEndIndices"), "Unity controller must retain level boundaries");
check(controller.includes('CreateButton(canvasObject.transform, "Continue Level"'), "Unity UI must create the continue-level button");
check(controller.includes("labelRect.anchorMin = Vector2.zero;")
  && controller.includes("labelRect.anchorMax = Vector2.one;")
  && controller.includes("labelRect.offsetMin = Vector2.zero;")
  && controller.includes("labelRect.offsetMax = Vector2.zero;"),
  "Button label rect should stretch to fit the button without canvas-sized overflow");
check(controller.includes("CurrentLevelEnd()"), "Unity controller must track the end of each level");
check(controller.includes("contains duplicate options"), "Runtime question loading must reject duplicate answer choices");
check(controller.includes("duplicates an earlier prompt") && controller.includes("seenPrompts.Add(normalizedPrompt)"), "Runtime question loading must reject repeated prompts after whitespace normalization");
check(controller.includes("NormalizeOption(flattened[i].options[optionIndex])")
  && controller.includes("System.StringComparison.OrdinalIgnoreCase"),
  "Runtime question loading must reject duplicate answer choices after whitespace normalization and case folding");
check(controller.includes("private static string NormalizeOption(string option)"),
  "Runtime option normalization helper is missing");
check(editor.includes("NormalizeOption(question.options[0])")
  && editor.includes("StringComparison.OrdinalIgnoreCase"),
  "Editor validation must use the same normalized, case-insensitive option comparison");
check(editor.includes("option.Normalize(System.Text.NormalizationForm.FormKC)")
  && controller.includes("option.Normalize(System.Text.NormalizationForm.FormKC)"),
  "Editor and runtime option validation must normalize Unicode compatibility forms consistently");
check(controller.includes('Tiizino.MusicEnabled'), "Music preference must persist between game sessions");
check(controller.includes('musicEnabled ? "موسیقی: روشن" : "موسیقی: خاموش"'), "Music toggle label must reflect the current preference");
check(controller.includes("private void OnDisable()") && controller.includes("speechService?.Stop();"), "Disabling or rebuilding the stage must stop question voice playback");
check(controller.includes("if (musicSource != null) musicSource.Stop();"), "Disabling the controller must stop music on its generated child AudioSource");
check(controller.includes("private void OnEnable()") && controller.includes("musicEnabled && musicSource != null && !musicSource.isPlaying") && controller.includes("musicSource.Play();"), "Re-enabling the controller must resume background music when the saved preference is on");
check(controller.includes("if (button != null && button.gameObject.activeInHierarchy)") && controller.includes("button.interactable = true;"), "Re-enabling the stage must not leave answer controls locked after a cancelled callback");
check(controller.includes("private void ClearOldStage()"), "Stage cleanup method is missing");
check(controller.includes("private bool currentQuestionResolved;"), "Unity controller must track whether the current question already awarded a reward");
const rebuildStart = controller.indexOf("public void BuildStage()");
const rebuildEnd = controller.indexOf("private sealed class QuestionBank", rebuildStart);
const rebuildBody = rebuildStart >= 0 && rebuildEnd > rebuildStart ? controller.slice(rebuildStart, rebuildEnd) : "";
check(rebuildBody.includes("currentQuestionResolved = false;"), "Rebuilding the stage must clear resolved-answer state from the previous run");
check(controller.includes("if (currentQuestionResolved || questionIndex < 0"), "Repeated answer callbacks must not award duplicate stars for a resolved question");
check(controller.includes("if (!currentQuestionResolved && answerButtons != null)"), "Disabling after a correct answer must not reopen its answer controls");
check(controller.includes("currentQuestionResolved = false;") && controller.includes("NextQuestion();"), "Re-enabling after a cancelled correct-answer callback must advance the resolved question");
check(controller.includes("ContinueToNextLevel()"), "Unity controller must provide explicit next-level flow");
check(controller.includes("parsedLevelEnds.Add(flattened.Count)"), "Unity controller must derive level boundaries from question data");
check(controller.includes("HashSet<int> seenLevelIds")
  && controller.includes("level.levelId < 1 || !seenLevelIds.Add(level.levelId)"),
  "Runtime question-bank loading must reject missing, non-positive, or duplicate level IDs");
check(controller.includes("string.IsNullOrWhiteSpace(level.name) || string.IsNullOrWhiteSpace(level.skill)")
  && controller.includes("is missing its name, skill, or questions"),
  "Runtime question-bank loading must reject levels missing required name or skill metadata");
check(controller.includes("لول ") && controller.includes("از "), "Unity controller should display current level progress");
check(controller.includes("پرسش ") && controller.includes("questionsInLevel") && controller.includes("questionInLevel"), "Unity UI should show the current question number within each level");
check(controller.includes('Tiizino.Grade" + grade + ".BestStars'), "Unity controller must persist a separate best-star record for each grade");
check(controller.includes("TextAnchor.MiddleRight"), "Persian gameplay text should use right alignment");
check(controller.includes("بهترین رکورد"), "Grade selection should show the saved best score");
check(!controller.includes("PlayCompletionChime"), "No stage completion chime should be added");
const chooseAnswerStart = controller.indexOf("private void ChooseAnswer(int choice)");
const chooseAnswerEnd = controller.indexOf("private void AllowRetry()", chooseAnswerStart);
const chooseAnswerBody = chooseAnswerStart >= 0 && chooseAnswerEnd > chooseAnswerStart
  ? controller.slice(chooseAnswerStart, chooseAnswerEnd) : "";
check(chooseAnswerBody.length > 0, "Unity controller must contain the answer selection handler");
check(chooseAnswerBody.includes("if (correct)") && chooseAnswerBody.includes("stars++"),
  "Correct answers should award a star");
check(chooseAnswerBody.includes("Invoke(nameof(NextQuestion), 1.8f)")
  && chooseAnswerBody.includes("Invoke(nameof(AllowRetry), 1.8f)"),
  "Correct answers advance while incorrect answers enter retry flow");
check(chooseAnswerBody.includes("feedbackText.text = \"یک بار دیگر فکر کن! به الگو و گزینه‌ها دقت کن.\""),
  "Incorrect-answer feedback should encourage another attempt without revealing the answer");

const questionData = readFileSync(new URL("../Assets/_Tiizino/Scripts/Gameplay/QuestionData.cs", import.meta.url), "utf8");
check(questionData.includes("HashSet<string> normalizedOptions"), "Runtime question validation must reject duplicate normalized choices");
check(questionData.includes("NormalizationForm.FormKC"), "Runtime choice normalization must handle compatibility-equivalent characters");
check(questionData.includes("string.IsNullOrWhiteSpace(explanation)"), "Runtime question validation must require explanatory feedback");

console.log("Four-level Unity controller structure checks passed (static checks only).");

const bootstrap = readFileSync(new URL("../Assets/Editor/TiizinoSceneBootstrap.cs", import.meta.url), "utf8");
check(bootstrap.includes('MenuItem("Tools/Tiizino/Create Stage 01 Scene")'),
  "Unity editor must expose the documented scene creation menu");
check(bootstrap.includes("AssetDatabase.Refresh();"),
  "Scene bootstrap must refresh the AssetDatabase after creating the scene folder");
check(bootstrap.includes("stage.AddComponent<BridgeStageController>()")
  && bootstrap.includes('"/Stage01_Bridge.unity"'),
  "Scene bootstrap must attach the gameplay controller and save to the documented path");
check(bootstrap.includes("EditorBuildSettings.scenes = scenes.ToArray();"),
  "Scene bootstrap must register the stage in Build Settings");

const validator = readFileSync(new URL("../Assets/Editor/TiizinoProjectValidator.cs", import.meta.url), "utf8");
check(validator.includes('MenuItem("Tools/Tiizino/Validate Project Setup")'),
  "Unity editor must expose a setup validation command");
check(validator.includes("NormalizePrompt") && validator.includes("HashSet<string> seenPrompts"), "Unity setup validator must reject duplicate prompts across levels");
check(validator.includes('normalized.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)')
  && validator.includes('NormalizationForm.FormKC')
  && validator.includes('ToLowerInvariant()'),
  "Unity prompt normalizer must collapse whitespace and normalize Unicode/casing");
check(validator.includes('private static string NormalizeOption(string option)')
  && validator.includes('StringComparison.OrdinalIgnoreCase'),
  "Unity setup validator must normalize answer options before comparing them");
check(validator.includes("NormalizationForm.FormKC") && validator.includes("ToLowerInvariant()"), "Duplicate prompt detection must normalize Unicode and casing");
check(validator.includes("NormalizeOption(question.options[0])")
  && validator.includes("StringComparison.OrdinalIgnoreCase"),
  "Unity setup validator must reject duplicate answer choices after normalization");
check(validator.includes("Grade1_QuestionBank.json")
  && validator.includes("Grade2_QuestionBank.json")
  && validator.includes("correctOptionIndex")
  && validator.includes("EditorBuildSettings.scenes"),
  "Setup validator must check both starter-grade question banks and build scene settings");
check(validator.includes("does not compile the project or verify Android/device behavior"),
  "Setup validator must not imply it replaces a real Unity/device build");
const manifest = JSON.parse(readFileSync(new URL("../Packages/manifest.json", import.meta.url), "utf8"));
check(manifest.dependencies["com.unity.ugui"] === "2.0.0",
  "Unity package manifest must include the UI package");
check(manifest.dependencies["com.unity.render-pipelines.universal"] === "17.0.3",
  "Unity package manifest must include URP");
check(readFileSync(new URL("../ProjectSettings/ProjectVersion.txt", import.meta.url), "utf8")
  .includes("m_EditorVersion: 6000.0.32f1"),
  "Unity project must pin its editor version");
const setupGuide = readFileSync(new URL("../Docs/UNITY_SETUP_FA.md", import.meta.url), "utf8");
check(setupGuide.includes("Android Build Support")
  && setupGuide.includes("Android SDK & NDK Tools")
  && setupGuide.includes("JDK"),
  "Unity setup guide must document Android build prerequisites");
check(setupGuide.includes("تست‌های GitHub Actions فقط تست‌های ایستای فایل‌ها هستند"),
  "Setup guide must clearly distinguish static checks from real Android builds");
console.log("Unity project scaffold and Android setup documentation checks passed (static checks only).");


check(readFileSync(new URL("../Assets/_Tiizino/Scripts/Gameplay/QuestionData.cs", import.meta.url), "utf8").includes("StringComparer.OrdinalIgnoreCase"), "Runtime question model must reject case-insensitive duplicate options");
check(controller.includes("flattened[i].prompt.Normalize(System.Text.NormalizationForm.FormKC)") && controller.includes("ToUpperInvariant()"), "Runtime prompt deduplication must normalize Unicode and casing");

for (const grade of [1, 2]) {
  const bankPath = new URL(`../Assets/_Tiizino/Resources/Grade${grade}_QuestionBank.json`, import.meta.url);
  const bank = JSON.parse(readFileSync(bankPath, "utf8"));
  check(Array.isArray(bank.levels) && bank.levels.length > 0,
    `Grade ${grade} bank must contain at least one level`);
  const gradePrompts = new Set();
  const levelIds = new Set();
  for (const [levelIndex, level] of bank.levels.entries()) {
    check(Number.isInteger(level.levelId) && level.levelId >= 1,
      `Grade ${grade}, level ${levelIndex + 1} must have a positive integer levelId`);
    check(!levelIds.has(level.levelId),
      `Grade ${grade} has duplicate levelId ${level.levelId}`);
    levelIds.add(level.levelId);
    check(typeof level.name === "string" && level.name.trim().length > 0
      && typeof level.skill === "string" && level.skill.trim().length > 0,
      `Grade ${grade}, level ${levelIndex + 1} must name its level and learning skill`);
    check(Array.isArray(level.questions) && level.questions.length > 0,
      `Grade ${grade}, level ${levelIndex + 1} must contain questions`);
    for (const [questionIndex, question] of level.questions.entries()) {
      const label = `Grade ${grade}, level ${levelIndex + 1}, question ${questionIndex + 1}`;
      check(typeof question.prompt === "string" && question.prompt.trim().length > 0,
        `${label} must have a prompt`);
      check(Array.isArray(question.options) && question.options.length === 3
        && question.options.every(option => typeof option === "string" && option.trim().length > 0),
        `${label} must have three non-empty answer options`);
      const normalizedOptions = question.options.map(option =>
        option.normalize("NFKC").trim().replace(/\s+/gu, " ").toLocaleUpperCase("fa"));
      check(new Set(normalizedOptions).size === normalizedOptions.length,
        `${label} has duplicate answer options after normalization`);
      check(Number.isInteger(question.correctOptionIndex)
        && question.correctOptionIndex >= 0 && question.correctOptionIndex < question.options.length,
        `${label} has an invalid answer index`);
      check(typeof question.explanation === "string" && question.explanation.trim().length > 0,
        `${label} must include explanatory feedback`);
      const normalizedPrompt = normalizePrompt(question.prompt);
      check(!gradePrompts.has(normalizedPrompt), `${label} repeats a prompt within its grade bank`);
      gradePrompts.add(normalizedPrompt);
    }
  }
}



check(readFileSync(new URL("../Assets/Editor/TiizinoProjectValidator.cs", import.meta.url), "utf8")
  .includes("question.difficulty < 1 || question.difficulty > 3"),
  "Unity setup validator must reject question difficulty outside the supported 1-3 range");
check(readFileSync(new URL("../Assets/_Tiizino/Scripts/Gameplay/QuestionData.cs", import.meta.url), "utf8")
  .includes("if (difficulty < 1 || difficulty > 3"),
  "Runtime question model must reject difficulty outside the supported 1-3 range");

const sceneBuilderPath = new URL("../Assets/Editor/TiizinoSceneBuilder.cs", import.meta.url);
const sceneBuilder = readFileSync(sceneBuilderPath, "utf8");
check(sceneBuilder.includes('MenuItem("Tools/Tiizino/Create Stage 01 Scene")'),
  "Unity editor scene creation menu item is missing");
check(sceneBuilder.includes('AddComponent<BridgeStageController>()'),
  "Generated stage scene must include BridgeStageController");
check(sceneBuilder.includes('EditorSceneManager.SaveScene(scene, ScenePath)'),
  "Scene builder must save the generated stage scene");
check(sceneBuilder.includes('new EditorBuildSettingsScene(scenePath, true)'),
  "Scene builder must register and enable Stage 01 in Build Settings");

check(sceneBuilder.includes('EnsureFolder("Assets/_Tiizino", "Scenes")'),
  "Scene builder must create the output folder before saving the generated scene");
check(sceneBuilder.includes("if (!found)") && sceneBuilder.includes("scenes.Add(scene);"),
  "Scene builder must add Stage 01 without dropping unrelated build scenes");
check(sceneBuilder.includes("AssetDatabase.IsValidFolder(path)"),
  "Scene builder must avoid recreating an existing scene folder");
const validatorPath = new URL("../Assets/Editor/TiizinoProjectValidator.cs", import.meta.url);
const validator = readFileSync(validatorPath, "utf8");
check(validator.includes("public int levelId;"),
  "Unity project validator must deserialize each level ID");
check(validator.includes("HashSet<int> seenLevelIds")
  && validator.includes("level.levelId < 1 || !seenLevelIds.Add(level.levelId)"),
  "Unity project validator must reject missing, non-positive, or duplicate level IDs");

const gradePreviewPath = new URL("../Preview/grades.html", import.meta.url);
const gradePreview = readFileSync(gradePreviewPath, "utf8");
check(gradePreview.includes('../Assets/_Tiizino/Resources/Grade1_QuestionBank.json')
  && gradePreview.includes('../Assets/_Tiizino/Resources/Grade2_QuestionBank.json'),
  "Playable grade preview must load both official question banks");
check(gradePreview.includes('$("grade1").addEventListener("click",()=>loadGrade(1))')
  && gradePreview.includes('$("grade2").addEventListener("click",()=>loadGrade(2))'),
  "Playable grade preview must allow selecting either grade");
check(gradePreview.includes("function choose(choice)")
  && gradePreview.includes("q.correctOptionIndex")
  && gradePreview.includes("q.explanation"),
  "Playable grade preview must check answers and show explanatory feedback");
const gradePreviewScript = gradePreview.split("<script>")[1]?.split("</script>")[0];
check(Boolean(gradePreviewScript), "Could not extract playable grade preview script");
new Function(gradePreviewScript);

check(gradePreview.includes('id="questionCount"')
  && gradePreview.includes('level.name+" ("'),
  "Playable grade preview must show the current level name and question progress");
