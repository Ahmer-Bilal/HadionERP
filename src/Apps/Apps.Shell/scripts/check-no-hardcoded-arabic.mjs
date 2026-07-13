#!/usr/bin/env node
// Enforces the same "no hardcoded translatable text" rule as the backend guardrail
// (tests/ArchitectureTests/Platform.ArchitectureTests/NoHardcodedTranslatableTextTests.cs), for the
// frontend. Parses every .ts/.tsx file under src/ AND under src/Platform/Platform.UI (the design system,
// which is frontend code too — see PROGRESS.md, Platform.UI entry) with the TypeScript compiler API (an
// existing devDependency, not a new one) and fails if a string/template/JSX-text literal contains Arabic
// script outside the allow-list below. Comments are not visited by forEachChild, so — same as the C#
// version — a comment mentioning example Arabic text is not flagged.
//
// Added 2026-07-13 because the backend already caught this mistake once during development and the
// frontend had no equivalent automated check — see PROGRESS.md.

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import ts from "typescript";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

// Two roots: Apps.Shell's own src/ and the shared Platform.UI design system (which lives outside this
// app's folder but is frontend code consumed via the @platform/ui alias). Both are scanned so a hardcoded
// string can't slip in through the design system any more than through an app component.
const shellSrcRoot = path.resolve(__dirname, "..", "src");
const platformUiRoot = path.resolve(__dirname, "..", "..", "..", "Platform", "Platform.UI");
const scanRoots = [shellSrcRoot, platformUiRoot];

// Relative to the file's own scan root, forward slashes. Adding an entry here is a deliberate, reviewed
// decision — it will show up in a diff, never a silent workaround.
const ALLOWED_FILES = new Set([
  "i18n/content.ts", // the one designated place for translatable display text (see that file's own comment)
  "i18n/languageNames.ts", // fixed language autonyms, not translations — see that file's own comment
]);

const ARABIC_SCRIPT = /[؀-ۿݐ-ݿࢠ-ࣿﭐ-﷿ﹰ-﻿]/;

function collectSourceFiles(dir, files = []) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      collectSourceFiles(fullPath, files);
    } else if (/\.(ts|tsx)$/.test(entry.name)) {
      files.push(fullPath);
    }
  }
  return files;
}

function isTextLiteralNode(node) {
  return (
    ts.isStringLiteral(node) ||
    node.kind === ts.SyntaxKind.NoSubstitutionTemplateLiteral ||
    node.kind === ts.SyntaxKind.TemplateHead ||
    node.kind === ts.SyntaxKind.TemplateMiddle ||
    node.kind === ts.SyntaxKind.TemplateTail ||
    node.kind === ts.SyntaxKind.JsxText
  );
}

function checkFile(filePath, root) {
  const relativePath = path.relative(root, filePath).replace(/\\/g, "/");
  const isAllowed = ALLOWED_FILES.has(relativePath);
  const text = fs.readFileSync(filePath, "utf8");
  const sourceFile = ts.createSourceFile(
    filePath,
    text,
    ts.ScriptTarget.Latest,
    true,
    filePath.endsWith(".tsx") ? ts.ScriptKind.TSX : ts.ScriptKind.TS,
  );

  const violations = [];

  function visit(node) {
    if (isTextLiteralNode(node) && !isAllowed && ARABIC_SCRIPT.test(node.text)) {
      const { line } = sourceFile.getLineAndCharacterOfPosition(node.getStart());
      violations.push(`${relativePath}:${line + 1}: literal "${node.text.trim()}" contains Arabic script.`);
    }
    ts.forEachChild(node, visit);
  }

  visit(sourceFile);
  return violations;
}

const allViolations = scanRoots.flatMap((root) =>
  fs.existsSync(root) ? collectSourceFiles(root).flatMap((f) => checkFile(f, root)) : [],
);

if (allViolations.length > 0) {
  console.error("Found hardcoded Arabic text outside the allowed content files:\n");
  for (const violation of allViolations) {
    console.error(" - " + violation);
  }
  console.error(
    "\nMove the text into src/i18n/content.ts and look it up via t(key, language) instead. " +
      "If this really is a fixed/structural value (not translatable content), add it to " +
      "ALLOWED_FILES in scripts/check-no-hardcoded-arabic.mjs with a justification.",
  );
  process.exit(1);
} else {
  console.log("OK: no hardcoded Arabic text found outside the allowed content files.");
}
