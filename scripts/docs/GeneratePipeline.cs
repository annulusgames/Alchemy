namespace Alchemy.Docs;

internal static class GeneratePipeline
{
    public static async Task<int> RunAsync(
        RepoPaths paths,
        bool dryRun,
        bool noCapture,
        CancellationToken cancellationToken)
    {
        var attributes = AttributeCatalog.Load(paths);
        var samples = SampleExtractor.Load(paths);
        var hardErrors = new List<string>();
        var warnings = new List<string>();

        var localeFiles = DocLanguage.Localized
            .ToDictionary(
                language => language,
                language => I18nStore.LoadOrEmpty(paths, language),
                StringComparer.OrdinalIgnoreCase);

        foreach (var attribute in attributes)
        {
            if (string.IsNullOrWhiteSpace(attribute.Summary))
            {
                hardErrors.Add($"missing /// <summary> on {attribute.TypeName}");
            }

            if (!samples.TryGetValue(attribute.SampleTypeName, out var sampleInfo))
            {
                hardErrors.Add(
                    $"missing [DocumentationSample] type {attribute.SampleTypeName} for {attribute.TypeName}");
            }
            else if (!sampleInfo.HasDocumentRegion)
            {
                hardErrors.Add(
                    $"missing #region {SampleExtractor.DocumentRegion} in {attribute.SampleTypeName}");
            }

            foreach (var language in DocLanguage.Localized)
            {
                if (!localeFiles[language].Entries.ContainsKey(attribute.TypeName))
                {
                    warnings.Add(
                        $"missing i18n entry {attribute.TypeName} in {Relative(paths, I18nStore.PathFor(paths, language))} (will seed from {DocLanguage.Source})");
                }
            }

            var capture = sampleInfo?.Capture ?? true;
            if (capture && PageGenerator.FindImages(paths, attribute.Slug).Count == 0)
            {
                warnings.Add($"no images for {attribute.Slug}");
            }
        }

        foreach (var warning in warnings)
        {
            Console.Error.WriteLine("warning: " + warning);
        }

        if (hardErrors.Count > 0)
        {
            foreach (var error in hardErrors)
            {
                Console.Error.WriteLine("error: " + error);
            }

            return 1;
        }

        var planned = new List<GeneratedFile>();
        foreach (var language in DocLanguage.All)
        {
            Directory.CreateDirectory(paths.AttributesDir(language));
        }

        // Seed missing attribute entries into each locale file (EN stubs), then plan one write per language.
        foreach (var language in DocLanguage.Localized)
        {
            var file = localeFiles[language];
            var mutated = false;
            foreach (var attribute in attributes)
            {
                if (file.Entries.ContainsKey(attribute.TypeName))
                {
                    continue;
                }

                file.Entries[attribute.TypeName] = I18nStore.StubFromEnglish(attribute);
                mutated = true;
            }

            if (mutated || !File.Exists(I18nStore.PathFor(paths, language)))
            {
                planned.Add(new GeneratedFile(
                    I18nStore.PathFor(paths, language),
                    I18nStore.Serialize(file.Entries)));
            }
        }

        foreach (var attribute in attributes)
        {
            samples.TryGetValue(attribute.SampleTypeName, out var sample);
            var locales = new Dictionary<string, LocaleI18n?>(StringComparer.OrdinalIgnoreCase);

            foreach (var language in DocLanguage.Localized)
            {
                locales[language] = I18nStore.TryGet(localeFiles[language], attribute);
            }

            foreach (var language in DocLanguage.All)
            {
                locales.TryGetValue(language, out var locale);
                planned.Add(new GeneratedFile(
                    Path.Combine(paths.AttributesDir(language), attribute.Slug + ".md"),
                    PageGenerator.Build(attribute, sample, locale, paths, language)));
            }
        }

        foreach (var language in DocLanguage.All)
        {
            var tocPath = paths.TocPath(language);
            planned.Add(new GeneratedFile(
                tocPath,
                TocUpdater.Update(File.ReadAllText(tocPath), attributes, language)));
        }

        var wouldChange = false;
        foreach (var file in planned)
        {
            var existing = File.Exists(file.Path) ? File.ReadAllText(file.Path) : null;
            if (!string.Equals(existing, file.Content, StringComparison.Ordinal))
            {
                wouldChange = true;
                Console.WriteLine(
                    (dryRun ? "would update: " : "update: ") + Relative(paths, file.Path));
                if (!dryRun)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(file.Path)!);
                    File.WriteAllText(file.Path, file.Content);
                }
            }
        }

        // Delete obsolete generated attribute pages not in catalog.
        foreach (var language in DocLanguage.All)
        {
            var dir = paths.AttributesDir(language);
            if (!Directory.Exists(dir))
            {
                continue;
            }

            var keep = attributes.Select(a => a.Slug + ".md").ToHashSet(StringComparer.Ordinal);
            foreach (var file in Directory.EnumerateFiles(dir, "*.md"))
            {
                var name = Path.GetFileName(file);
                if (keep.Contains(name) || !PageGenerator.IsAutoGenerated(file))
                {
                    continue;
                }

                wouldChange = true;
                Console.WriteLine((dryRun ? "would delete: " : "delete: ") + Relative(paths, file));
                if (!dryRun)
                {
                    File.Delete(file);
                }
            }
        }

        if (dryRun)
        {
            return wouldChange ? 1 : 0;
        }

        if (!noCapture)
        {
            var capture = new UnityCapture(paths);
            await capture.CaptureAsync(attributes, samples, cancellationToken);

            // Refresh pages so new images are linked.
            foreach (var attribute in attributes)
            {
                samples.TryGetValue(attribute.SampleTypeName, out var sample);
                foreach (var language in DocLanguage.All)
                {
                    var locale = DocLanguage.IsSource(language)
                        ? null
                        : I18nStore.TryGet(localeFiles[language], attribute);
                    File.WriteAllText(
                        Path.Combine(paths.AttributesDir(language), attribute.Slug + ".md"),
                        PageGenerator.Build(attribute, sample, locale, paths, language));
                }
            }
        }

        Console.WriteLine($"Generated documentation for {attributes.Count} attributes.");
        return 0;
    }

    static string Relative(RepoPaths paths, string path) =>
        Path.GetRelativePath(paths.Root, path).Replace('\\', '/');
}
