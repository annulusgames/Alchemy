using System.Text;

namespace Alchemy.Docs;

internal static class DisplayNames
{
    /// <summary>
    /// "OnValueChanged" → "On Value Changed"
    /// </summary>
    public static string ToTitle(string displayName)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < displayName.Length; i++)
        {
            var ch = displayName[i];
            if (i > 0 && char.IsUpper(ch) && !char.IsUpper(displayName[i - 1]))
            {
                sb.Append(' ');
            }
            else if (i > 0 &&
                     char.IsUpper(ch) &&
                     i + 1 < displayName.Length &&
                     char.IsLower(displayName[i + 1]) &&
                     char.IsUpper(displayName[i - 1]))
            {
                sb.Append(' ');
            }

            sb.Append(ch);
        }

        return sb.ToString();
    }
}
