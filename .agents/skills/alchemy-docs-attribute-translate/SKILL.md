---
name: alchemy-docs-attribute-translate
description: >-
  Translate or update Japanese Alchemy attribute documentation from English truth.
  Use when the user asks to update JA attribute docs, translate attribute summaries,
  refresh scripts/docs/resources/i18n/ja.json, or sync Japanese Inspector attribute pages after EN XML changes.
---

# Alchemy attribute docs (EN → JA)

## Truth sources

- **English**: `/// <summary>` / param XML on attributes in
  `Alchemy/Assets/Alchemy/Runtime/Inspector/InspectorAttributes.cs` and `GroupAttributes.cs`
  (also reflected in generated `docs/articles/en/attributes/{slug}.md`).
- **Localized**: `scripts/docs/resources/i18n/{lang}.json` only (`ja.json` today; codes from `DocLanguage.All`),
  one file with an `attributes` map keyed by type name (e.g. `"ButtonAttribute": { ... }`). Missing or blank JA fields
  fall back to English on generated pages.
- **Never** hand-edit generated `docs/articles/ja/attributes/*.md`.

## i18n file shape

```json
{
  "attributes": {
    "ButtonAttribute": {
      "summary": "日本語の説明文。"
    },
    "HelpBoxAttribute": {
      "summary": "フィールドの上にメモや警告を追加します。",
      "notes": [
        { "type": "WARNING", "body": "注意書き（任意）" }
      ],
      "params": {
        "message": "パラメータの説明"
      }
    }
  }
}
```

Omit `params` / `notes` when unused. Param keys must match documented constructor parameter names (or property names for named-argument attributes). EN notes come from `<alchemy-attr-note type="WARNING">` on the attribute. Keep Japanese as UTF-8 text (do not `\u`-escape).

## Workflow

1. Read EN summary/params from XML or `docs/articles/en/attributes/{slug}.md`.
2. Update the attribute entry in `scripts/docs/resources/i18n/ja.json` with natural, concise Japanese matching existing docs tone.
3. Regenerate pages:

```sh
dotnet run --project scripts/docs -- generate --no-capture
```

4. Use full `generate` (no `--no-capture`) only when Inspector screenshots should refresh (requires Unity CLI + `tests/versions/Unity6000.3`).

## Style

- Keep summaries short (one or two sentences).
- Do not invent API behavior absent from EN.
- Prefer terminology already used in neighboring JA attribute pages.
