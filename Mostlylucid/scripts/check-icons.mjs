// Verifies our icon usage against the installed boxicons package.
//
// Two checks:
//   1. Every hardcoded codepoint in easymde-overrides.css renders the glyph its class name claims.
//      These silently drifted once already, leaving the whole editor toolbar showing wrong icons.
//   2. Every bx-* class referenced in views, scripts and blog posts actually exists.
//
// Exits non-zero on any mismatch so a bad upgrade fails the build rather than shipping.

import { readFileSync, existsSync, readdirSync } from "node:fs";
import { dirname, resolve, relative, sep } from "node:path";
import { fileURLToPath } from "node:url";

const projectRoot = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const boxiconsCss = resolve(projectRoot, "node_modules/boxicons/css/boxicons.min.css");

if (!existsSync(boxiconsCss)) {
    console.error("boxicons is not installed - run npm install");
    process.exit(1);
}

const bx = readFileSync(boxiconsCss, "utf8");

// class -> codepoint, for every glyph the font defines
const codepointByClass = new Map();
for (const m of bx.matchAll(/\.(bxs?-[a-z0-9-]+):before\{content:"\\([0-9a-f]+)"\}/g)) {
    codepointByClass.set(m[1], m[2]);
}

// Every class boxicons defines at all, including non-glyph modifiers (bx-spin, bx-sm, bx-ul...).
const knownClasses = new Set();
for (const m of bx.matchAll(/\.(bxs?-[a-z0-9-]+)(?=[,{:\s])/g)) knownClasses.add(m[1]);

const problems = [];

// --- 1. editor toolbar codepoints -------------------------------------------------
const overridePath = resolve(projectRoot, "src/css/easymde-overrides.css");
const override = readFileSync(overridePath, "utf8");

// Classes we define ourselves count as valid too - e.g. bx-quote-left has no regular-weight
// glyph in boxicons, so the override maps it onto the solid one.
for (const m of override.matchAll(/\.(bxs?-[a-z0-9-]+)::before/g)) knownClasses.add(m[1]);

for (const m of override.matchAll(/\.(bx-[a-z0-9-]+)::before\s*\{\s*content:\s*"\\([0-9a-f]+)"/g)) {
    const [, cls, codepoint] = m;
    const expected = codepointByClass.get(cls) ?? codepointByClass.get(cls.replace("bx-", "bxs-"));

    if (!expected) {
        problems.push(`easymde-overrides.css: ${cls} does not exist in boxicons`);
    } else if (expected !== codepoint) {
        const renders = [...codepointByClass].find(([, cp]) => cp === codepoint)?.[0] ?? "nothing";
        problems.push(
            `easymde-overrides.css: ${cls} uses \\${codepoint} (renders ${renders}); should be \\${expected}`);
    }
}

// --- 2. classes referenced across the site ----------------------------------------
// Hand-rolled walk rather than fs.globSync: the Docker image runs Node 20, which does not
// have it, so using it here broke the image build while passing locally.
function collect(dir, extension, recurse, found = []) {
    let entries;
    try {
        entries = readdirSync(dir, { withFileTypes: true });
    } catch {
        return found; // directory absent in this build context
    }

    for (const entry of entries) {
        const full = resolve(dir, entry.name);
        if (entry.isDirectory()) {
            if (recurse && entry.name !== "node_modules") collect(full, extension, recurse, found);
        } else if (entry.name.endsWith(extension)) {
            found.push(full);
        }
    }
    return found;
}

const sources = [
    ["Views", ".cshtml", true],
    ["EmailSubscription", ".cshtml", true],
    ["src/js", ".js", true],
    ["src/css", ".css", true],
    ["Markdown", ".md", false]
];

const used = new Map(); // class -> first file that referenced it
for (const [dir, extension, recurse] of sources) {
    for (const full of collect(resolve(projectRoot, dir), extension, recurse)) {
        // Skip the file we generate from the package itself.
        if (full.endsWith("boxicons.gen.css")) continue;

        const file = relative(projectRoot, full).split(sep).join("/");
        const text = readFileSync(full, "utf8");
        for (const m of text.matchAll(/\bbxs?-[a-z0-9-]+/g)) {
            if (!used.has(m[0])) used.set(m[0], file);
        }
    }
}

// A bad class in a blog post shows one missing glyph; failing the build (and so the Docker
// image) over a typo in prose is worse than the typo. Code gets held to a harder standard.
const warnings = [];
for (const [cls, file] of used) {
    if (knownClasses.has(cls)) continue;
    const message = `${file}: unknown icon class ${cls}`;
    if (file.startsWith("Markdown/")) warnings.push(message);
    else problems.push(message);
}

// --- report -----------------------------------------------------------------------
for (const warning of warnings) console.warn(`  warning: ${warning}`);

if (problems.length > 0) {
    console.error(`Icon check failed (${problems.length} problem(s)):`);
    for (const problem of problems) console.error(`  - ${problem}`);
    process.exit(1);
}

console.log(
    `Icon check passed: ${used.size} classes referenced, all valid` +
    `${warnings.length > 0 ? ` (${warnings.length} warning(s) in blog content)` : ""}.`);
