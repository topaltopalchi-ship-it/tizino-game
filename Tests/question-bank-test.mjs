import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

const banks = [
  { grade: 1, path: "Assets/_Tiizino/Resources/Grade1_QuestionBank.json" },
  { grade: 2, path: "Assets/_Tiizino/Resources/Grade2_QuestionBank.json" },
];

for (const { grade, path } of banks) {
  const bank = JSON.parse(readFileSync(new URL("../" + path, import.meta.url), "utf8"));
  assert.ok(bank.stageId, `Grade ${grade}: stageId is required`);
  assert.ok(Array.isArray(bank.levels) && bank.levels.length >= 3,
    `Grade ${grade}: expected at least three levels`);

  const seenLevelIds = new Set();
  let totalQuestions = 0;
  let expectedOptionCount = null;
  for (const [levelIndex, level] of bank.levels.entries()) {
    assert.ok(level.name?.trim(), `Grade ${grade}, level ${levelIndex + 1}: name is required`);
    assert.ok(level.skill?.trim(), `Grade ${grade}, level ${levelIndex + 1}: skill is required`);
    if (level.levelId != null) {
      assert.ok(!seenLevelIds.has(level.levelId),
        `Grade ${grade}: duplicate levelId ${level.levelId}`);
      seenLevelIds.add(level.levelId);
    }
    assert.ok(Array.isArray(level.questions) && level.questions.length > 0,
      `Grade ${grade}, level ${levelIndex + 1}: questions are required`);

    for (const [questionIndex, q] of level.questions.entries()) {
      const label = `Grade ${grade}, level ${levelIndex + 1}, question ${questionIndex + 1}`;
      assert.ok(q.prompt?.trim(), `${label}: prompt is required`);
      assert.ok(Array.isArray(q.options) && q.options.length >= 2, `${label}: at least two options required`);
      assert.ok(q.options.every(option => typeof option === "string" && option.trim()),
        `${label}: every option must be non-empty text`);
      assert.ok(new Set(q.options).size === q.options.length, `${label}: duplicate answer options`);
      if (expectedOptionCount == null) expectedOptionCount = q.options.length;
      assert.equal(q.options.length, expectedOptionCount,
        `${label}: option count must match other questions because the runtime UI uses a shared button row`);
      assert.ok(Number.isInteger(q.correctOptionIndex)
        && q.correctOptionIndex >= 0 && q.correctOptionIndex < q.options.length,
        `${label}: correctOptionIndex is outside the options array`);
      assert.ok(typeof q.explanation === "string" && q.explanation.trim(),
        `${label}: explanation is required`);
      totalQuestions++;
    }
  }
  assert.ok(totalQuestions >= 12, `Grade ${grade}: expected at least 12 starter questions, got ${totalQuestions}`);
  console.log(`Grade ${grade}: ${bank.levels.length} levels, ${totalQuestions} questions validated.`);
}
