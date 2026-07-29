using System;
using UnityEditor;
using UnityEngine;

namespace Alchemy.Editor
{
    // Rererence: https://www.foundations.unity.com/fundamentals/color-palette

    internal static class EditorColors
    {
        static Color GetColor(string htmlColor)
        {
            if (!ColorUtility.TryParseHtmlString(htmlColor, out var color)) throw new ArgumentException();
            return color;
        }

        public static Color DefaultBackground
        {
            get
            {
                if (EditorGUIUtility.isProSkin) return GetColor("#282828");
                else return GetColor("#A5A5A5");
            }
        }

        public static Color HighlightBackgroundInactive
        {
            get
            {
                if (EditorGUIUtility.isProSkin) return GetColor("#4D4D4D");
                else return GetColor("#AEAEAE");
            }
        }

        public static Color HighlightBackground
        {
            get
            {
                if (EditorGUIUtility.isProSkin) return GetColor("#2C5D87");
                else return GetColor("#3A72B0");
            }
        }

        public static Color WindowBackground
        {
            get
            {
                if (EditorGUIUtility.isProSkin) return GetColor("#383838");
                else return GetColor("#C8C8C8");
            }
        }

        public static Color InspectorTitlebarBorder
        {
            get
            {
                if (EditorGUIUtility.isProSkin) return GetColor("#1A1A1A");
                else return GetColor("#7F7F7F");
            }
        }

        public static Color DefaultText
        {
            get
            {
                if (EditorGUIUtility.isProSkin) return GetColor("#D2D2D2");
                else return GetColor("#090909");
            }
        }
    }
}
