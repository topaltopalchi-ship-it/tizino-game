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
check(html.includes('addEventListener("click",restart)') || html.includes('addEventListener("click",()=>{index=0'), "Restart action should be wired");
check(script.includes("choice===q.a"), "Answer selection should compare against the answer key");
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
  check(Array.isArray(level.questions) && level.questions.length >= 2, `Level ${levelIndex + 1} needs at least two questions`);
  for (const [questionIndex, question] of level.questions.entries()) {
    check(typeof question.prompt === "string" && question.prompt.trim().length > 0,
      `Level ${levelIndex + 1}, question ${questionIndex + 1} needs a prompt`);
    check(Array.isArray(question.options) && question.options.length === 3,
      `Level ${levelIndex + 1}, question ${questionIndex + 1} must have three options`);
    check(question.options.every(option => typeof option === "string" && option.trim().length > 0),
      `Level ${levelIndex + 1}, question ${questionIndex + 1} has an empty option`);
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
check(expectedLevelEnds.length === 4 && expectedLevelEnds.every((end, i) => end === [2, 4, 6, 8][i]),
  "Current draft bank should produce cumulative level ends [2, 4, 6, 8]");
console.log("Four-level draft question bank structure and boundaries validated.");

const controllerPath = new URL("../Assets/_Tiizino/Scripts/Gameplay/BridgeStageController.cs", import.meta.url);
const controller = readFileSync(controllerPath, "utf8");
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
check(controller.includes("ContinueToNextLevel()"), "Unity controller must provide explicit next-level flow");
check(controller.includes("parsedLevelEnds.Add(flattened.Count)"), "Unity controller must derive level boundaries from question data");
check(controller.includes("لول ") && controller.includes("از "), "Unity controller should display current level progress");
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

console.log("Four-level Unity controller structure checks passed (static checks only).");

