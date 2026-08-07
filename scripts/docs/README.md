# Alchemy attribute docs generator

Fully generates `docs/articles/{lang}/attributes/*.md` and Attributes TOC sections for each code in `DocLanguage.All` (`en`, `ja` today) from:

- `///` XML on attributes in `Alchemy/Assets/Alchemy/Runtime/Inspector/`
- `[DocumentationSample]` types under `tests/Alchemy.Tests/Assets/Alchemy.Tests.EditorUI/`
  with a `#region document` … `#endregion` block (emitted as DocFX `[!code-csharp[](...#document)]`)
- Localized copy in `scripts/docs/resources/i18n/{lang}.json` (one file per locale;
  attribute entries live under top-level `attributes`, keyed by type name).
  Missing/blank fields fall back to English XML.
  - To add a locale: extend `DocLanguage.All` (+ toc marker / table header), add
  `docs/articles/{lang}/` and `resources/i18n/{lang}.json`.
- Screenshots in `docs/images/generated/img-attribute-{slug}*.png`

## Usage

```sh
# Docs + Captures Unity Inspector 
dotnet run --project scripts/docs -- generate

# Docs only
dotnet run --project scripts/docs -- generate --no-capture

# Check whether generated files would change (no writes, no Unity)
dotnet run --project scripts/docs -- generate --dry-run
```

## Requirements
- Unity 6000.3.21f1
- .NET 10 or later

## Sample convention

```csharp
[DocumentationSample] // use Capture = false to skip Unity screenshots
public class TitleTest : MonoBehaviour
{
    [Order(-1)]
    [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
    [HideLabel]
    public int __docCaptureStart;

    #region document
    [Title("Title1")]
    public float foo;
    // ...
    #endregion

    [Order(int.MaxValue)]
    [HorizontalLine(DocumentationCapture.CyanR, DocumentationCapture.CyanG, DocumentationCapture.CyanB)]
    [HideLabel]
    public int __docCaptureEnd;
}
```

- `#region document` is what DocFX embeds via `[!code-csharp[](...#document)]`.
- Cyan `__docCaptureStart` / `__docCaptureEnd` lines mark Inspector screenshot crop bounds (outside the region).
- `__docCaptureStart` uses `[Order(-1)]` so it stays above sample members.
- `__docCaptureEnd` uses `[Order(int.MaxValue)]` so it stays below sample members.

## Localization

Edit `scripts/docs/resources/i18n/{lang}.json`, then run `generate --no-capture`.  
Use the `.agents/skills/alchemy-docs-attribute-translate` skill for translation.
