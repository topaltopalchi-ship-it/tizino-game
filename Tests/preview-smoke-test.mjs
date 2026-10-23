import assert from "node:assert/strict";
import { readFileSync } from "node:fs";

const html = readFileSync(new URL("../Preview/index.html", import.meta.url), "utf8");
const check = (condition, message) => assert.ok(condition, message);

check(/<html[^>]*lang="fa"[^>]*dir="rtl"/i.test(html), "Preview must declare Persian RTL layout");
check((html.match(/<script\b/g) ?? []).length === 1, "Expected one inline script");
check((html.match(/<\/script>/g) ?? []).length === 1, "Inline script must close");
check(!/(?:src|href)\s*=\s*["']https?:\/\//i.test(html), "Preview should not require external assets");
check(html.includes('id="bridgeCount"'), "Bridge counter element is missing");
check(html.includes('$("bridgeCount").textContent=fa(stars)'), "Bridge counter must update with progress");
check(html.includes("restart") || html.includes("شروع دوباره"), "Restart control should be present");
check(html.includes("feedback"), "Answer feedback region should be present");

const script = html.match(/<script\b[^>]*>([\s\S]*?)<\/script>/i)?.[1];
check(Boolean(script), "Could not extract the inline script");
// Syntax-only compilation: does not execute browser DOM code.
new Function(script);
const questionCount = (script.match(/\{\s*p\s*:/g) ?? []).length;
check(questionCount === 4, `Expected 4 sample questions; found ${questionCount}`);

console.log("Preview smoke checks passed (static checks and JavaScript syntax only).");
