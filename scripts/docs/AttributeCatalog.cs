using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Alchemy.Docs;

internal static class AttributeCatalog
{
    static readonly Regex PascalToKebab = new(
        "(?<!^)([A-Z])",
        RegexOptions.Compiled);

    public static IReadOnlyList<AttributeInfo> Load(RepoPaths paths)
    {
        var groupBaseDocs = LoadPropertyGroupDocs(paths.PropertyGroupAttribute);
        var results = new List<AttributeInfo>();
        var errors = new List<string>();

        foreach (var sourcePath in new[] { paths.InspectorAttributes, paths.GroupAttributes })
        {
            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(sourcePath));
            var root = tree.GetCompilationUnitRoot();
            foreach (var type in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var name = type.Identifier.Text;
                if (!name.EndsWith("Attribute", StringComparison.Ordinal) ||
                    name == "PropertyGroupAttribute")
                {
                    continue;
                }

                if (!type.Modifiers.Any(SyntaxKind.PublicKeyword))
                {
                    continue;
                }

                if (!type.Modifiers.Any(SyntaxKind.SealedKeyword))
                {
                    errors.Add(
                        $"public attribute {name} must be sealed to be documented ({sourcePath})");
                    continue;
                }

                var display = name[..^"Attribute".Length];
                var slug = ToSlug(display);
                var summary = GetXmlSummary(type);
                var notes = GetXmlNotes(type);
                var parameters = CollectParameters(type);
                if (InheritsPropertyGroup(type))
                {
                    parameters = EnsureInheritedParameter(parameters, "groupPath", groupBaseDocs.GroupPath);
                    parameters = EnsureInheritedParameter(parameters, "order", groupBaseDocs.Order);
                }

                var category = GetXmlElementText(type, "alchemy-attr-category");
                if (string.IsNullOrWhiteSpace(category))
                {
                    errors.Add($"missing <alchemy-attr-category> on {name} in {sourcePath}");
                    continue;
                }

                results.Add(new AttributeInfo(
                    name,
                    display,
                    slug,
                    display + "Test",
                    category.Trim(),
                    summary,
                    notes,
                    parameters,
                    sourcePath));
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        }

        // Preserve first-seen category order from source files, then sort names within a category.
        var categoryOrder = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var attribute in results)
        {
            if (!categoryOrder.ContainsKey(attribute.Category))
            {
                categoryOrder[attribute.Category] = categoryOrder.Count;
            }
        }

        return results
            .OrderBy(a => categoryOrder[a.Category])
            .ThenBy(a => a.DisplayName, StringComparer.Ordinal)
            .ToArray();
    }

    static bool InheritsPropertyGroup(ClassDeclarationSyntax type) =>
        type.BaseList?.Types.Any(t =>
            t.Type.ToString().Contains("PropertyGroupAttribute", StringComparison.Ordinal))
        ?? false;

    static List<AttributeDocMember> EnsureInheritedParameter(
        List<AttributeDocMember> parameters,
        string name,
        string? summary)
    {
        var index = parameters.FindIndex(p =>
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            parameters.Add(new AttributeDocMember(name, summary));
            return parameters;
        }

        if (string.IsNullOrWhiteSpace(parameters[index].Summary))
        {
            parameters[index] = parameters[index] with { Summary = summary };
        }

        return parameters;
    }

    static (string? GroupPath, string? Order) LoadPropertyGroupDocs(string path)
    {
        const string defaultGroupPath =
            "Specifies the path of the group. Groups can be nested using `/`.";
        const string defaultOrder =
            "Drawing order among sibling groups. Lower values are drawn first.";

        if (!File.Exists(path))
        {
            return (defaultGroupPath, defaultOrder);
        }

        var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(path));
        var type = tree.GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(t => t.Identifier.Text == "PropertyGroupAttribute");

        string? SummaryFor(string propertyName) =>
            GetXmlSummary(type?.Members
                .OfType<PropertyDeclarationSyntax>()
                .FirstOrDefault(p => p.Identifier.Text == propertyName));

        return (
            SummaryFor("GroupPath") ?? defaultGroupPath,
            SummaryFor("Order") ?? defaultOrder);
    }

    static List<AttributeDocMember> CollectParameters(ClassDeclarationSyntax type)
    {
        var ctors = type.Members
            .OfType<ConstructorDeclarationSyntax>()
            .Where(c => c.Modifiers.Any(SyntaxKind.PublicKeyword))
            .ToArray();

        var ctorParams = ctors
            .SelectMany(c => c.ParameterList.Parameters.Select(p => (Ctor: c, Param: p)))
            .Where(x => x.Param.Identifier.Text.Length > 0)
            .ToArray();

        if (ctorParams.Length > 0)
        {
            var members = new List<AttributeDocMember>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (ctor, param) in ctorParams)
            {
                var name = param.Identifier.Text;
                if (!seen.Add(name))
                {
                    continue;
                }

                var summary = GetCtorParamSummary(ctor, name)
                              ?? FindMemberSummary(type, name);
                members.Add(new AttributeDocMember(name, summary));
            }

            return members;
        }

        // Named-argument style attributes (parameterless ctor + settable properties).
        var propertyMembers = new List<AttributeDocMember>();
        var seenProps = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in type.Members.OfType<PropertyDeclarationSyntax>())
        {
            if (!property.Modifiers.Any(SyntaxKind.PublicKeyword))
            {
                continue;
            }

            var name = property.Identifier.Text;
            if (!seenProps.Add(name))
            {
                continue;
            }

            propertyMembers.Add(new AttributeDocMember(name, GetXmlSummary(property)));
        }

        foreach (var field in type.Members.OfType<FieldDeclarationSyntax>())
        {
            if (!field.Modifiers.Any(SyntaxKind.PublicKeyword))
            {
                continue;
            }

            foreach (var variable in field.Declaration.Variables)
            {
                var name = variable.Identifier.Text;
                if (!seenProps.Add(name))
                {
                    continue;
                }

                propertyMembers.Add(new AttributeDocMember(name, GetXmlSummary(field)));
            }
        }

        return propertyMembers;
    }

    static string? GetCtorParamSummary(ConstructorDeclarationSyntax ctor, string paramName)
    {
        var trivia = ctor.GetLeadingTrivia()
            .Select(t => t.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .LastOrDefault();
        if (trivia is null)
        {
            return null;
        }

        foreach (var element in trivia.Content.OfType<XmlElementSyntax>())
        {
            if (element.StartTag.Name.ToString() != "param")
            {
                continue;
            }

            var value = GetXmlAttributeValue(element.StartTag, "name");
            if (value is null || !value.Equals(paramName, StringComparison.Ordinal))
            {
                continue;
            }

            return NormalizeDocText(element.Content.ToString());
        }

        return null;
    }

    static string? FindMemberSummary(ClassDeclarationSyntax type, string paramName)
    {
        var candidates = new[]
        {
            paramName,
            ToPascal(paramName),
            ToPascal(paramName) + "Text",
            ToPascal(paramName) + "Style",
        };

        foreach (var candidate in candidates.Distinct(StringComparer.Ordinal))
        {
            var property = type.Members
                .OfType<PropertyDeclarationSyntax>()
                .FirstOrDefault(p =>
                    p.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                    p.Identifier.Text.Equals(candidate, StringComparison.OrdinalIgnoreCase));
            if (property is not null)
            {
                return GetXmlSummary(property);
            }

            foreach (var field in type.Members.OfType<FieldDeclarationSyntax>())
            {
                if (!field.Modifiers.Any(SyntaxKind.PublicKeyword))
                {
                    continue;
                }

                foreach (var variable in field.Declaration.Variables)
                {
                    if (variable.Identifier.Text.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        return GetXmlSummary(field);
                    }
                }
            }
        }

        return null;
    }

    static string ToPascal(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        return char.ToUpperInvariant(name[0]) + name[1..];
    }

    static string? GetXmlSummary(MemberDeclarationSyntax? member) =>
        GetXmlElementText(member, "summary");

    static IReadOnlyList<AttributeNote> GetXmlNotes(MemberDeclarationSyntax member)
    {
        var trivia = member.GetLeadingTrivia()
            .Select(t => t.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .LastOrDefault();
        if (trivia is null)
        {
            return [];
        }

        var notes = new List<AttributeNote>();
        foreach (var element in trivia.Content.OfType<XmlElementSyntax>())
        {
            if (element.StartTag.Name.ToString() != "alchemy-attr-note")
            {
                continue;
            }

            var type = GetXmlAttributeValue(element.StartTag, "type") ?? "NOTE";
            var body = NormalizeDocText(element.Content.ToString());
            if (string.IsNullOrWhiteSpace(body))
            {
                continue;
            }

            notes.Add(new AttributeNote(type, body));
        }

        return notes;
    }

    static string? GetXmlAttributeValue(XmlElementStartTagSyntax startTag, string attributeName)
    {
        foreach (var attribute in startTag.Attributes)
        {
            switch (attribute)
            {
                case XmlNameAttributeSyntax nameAttr
                    when attributeName.Equals("name", StringComparison.Ordinal) &&
                         nameAttr.Name.ToString() == "name":
                    return nameAttr.Identifier.ToString();
                case XmlTextAttributeSyntax textAttr
                    when textAttr.Name.ToString() == attributeName:
                    return textAttr.TextTokens.ToString();
            }
        }

        return null;
    }

    static string? GetXmlElementText(MemberDeclarationSyntax? member, string elementName)
    {
        if (member is null)
        {
            return null;
        }

        var trivia = member.GetLeadingTrivia()
            .Select(t => t.GetStructure())
            .OfType<DocumentationCommentTriviaSyntax>()
            .LastOrDefault();
        if (trivia is null)
        {
            return null;
        }

        var element = trivia.Content
            .OfType<XmlElementSyntax>()
            .FirstOrDefault(e => e.StartTag.Name.ToString() == elementName);
        if (element is null)
        {
            return null;
        }

        return NormalizeDocText(element.Content.ToString());
    }

    static string NormalizeDocText(string raw)
    {
        var lines = raw
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Select(line =>
            {
                var trimmed = line.TrimStart();
                return trimmed.StartsWith("///", StringComparison.Ordinal)
                    ? trimmed[3..].TrimStart()
                    : trimmed;
            })
            .ToArray();

        var text = string.Join(" ", lines.Select(l => l.Trim())).Trim();
        var sb = new StringBuilder();
        var prevSpace = false;
        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!prevSpace)
                {
                    sb.Append(' ');
                    prevSpace = true;
                }
            }
            else
            {
                sb.Append(ch);
                prevSpace = false;
            }
        }

        return DocXmlToMarkdown(sb.ToString().Trim());
    }

    static string DocXmlToMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        text = Regex.Replace(
            text,
            """<see\s+href="([^"]+)"\s*>(.*?)</see>""",
            "[$2]($1)",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        text = Regex.Replace(
            text,
            """<c>(.*?)</c>""",
            "`$1`",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        return text
            .Replace("&lt;", "<", StringComparison.Ordinal)
            .Replace("&gt;", ">", StringComparison.Ordinal)
            .Replace("&amp;", "&", StringComparison.Ordinal)
            .Replace("&quot;", "\"", StringComparison.Ordinal);
    }

    public static string ToSlug(string displayName) =>
        PascalToKebab.Replace(displayName, "-$1").ToLowerInvariant();
}
