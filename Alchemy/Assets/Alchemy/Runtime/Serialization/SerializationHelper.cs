#if ALCHEMY_SUPPORT_SERIALIZATION
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Serialization.Json;

namespace Alchemy.Serialization.Internal
{
    public static class SerializationHelper
    {
        public static string ToJson<T>(T target, IList<UnityEngine.Object> unityObjectReferences)
        {
            if (target == null)
            {
                return string.Empty;
            }

            if (target is UnityEngine.Object unityObject)
            {
                var index = unityObjectReferences.IndexOf(unityObject);
                if (index == -1)
                {
                    unityObjectReferences.Add(unityObject);
                    index = unityObjectReferences.Count - 1;
                }
                return index.ToString();
            }

            return JsonSerialization.ToJson(target, new JsonSerializationParameters()
            {
                UserDefinedAdapters = new()
                {
                    new AlchemyJsonAdapter(unityObjectReferences)
                },
                SerializedType = target.GetType()
            });
        }

        public static T FromJson<T>(string json, IList<UnityEngine.Object> unityObjectReferences)
        {
            if (string.IsNullOrWhiteSpace(json)) return default;
            return ModifiedFromJson<T>(json, new JsonSerializationParameters()
            {
                UserDefinedAdapters = new()
                {
                    new AlchemyJsonAdapter(unityObjectReferences)
                },
                SerializedType = typeof(T)
            });
        }

        public static void FromJsonOverride<T>(string json, ref T container, IList<UnityEngine.Object> unityObjectReferences)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            ModifiedFromJsonOverride(json, ref container, new JsonSerializationParameters()
            {
                UserDefinedAdapters = new()
                {
                    new AlchemyJsonAdapter(unityObjectReferences)
                }
            });
        }

        /// <summary>
        /// Fixed a bug in the Unity.Serialization package (crash when executed at certain times)
        /// Reference: https://forum.unity.com/threads/about-the-com-unity-serialization-package.1512431/
        /// </summary>
        static unsafe T ModifiedFromJson<T>(string json, JsonSerializationParameters parameters = default)
        {
            fixed (char* buffer = json)
            {
                using var reader = new SerializedObjectReader(buffer, json.Length, GetDefaultConfigurationForString(json, parameters));
                reader.Read(out var view);
                return JsonSerialization.FromJson<T>(view, parameters);
            }
        }

        static unsafe void ModifiedFromJsonOverride<T>(string json, ref T container, JsonSerializationParameters parameters = default)
        {
            fixed (char* buffer = json)
            {
                using var reader = new SerializedObjectReader(buffer, json.Length, GetDefaultConfigurationForString(json, parameters));
                reader.Read(out var view);
                JsonSerialization.FromJsonOverride(view, ref container, parameters);
            }
        }

        /// copied from internal method in JsonSerialization
        static SerializedObjectReaderConfiguration GetDefaultConfigurationForString(string json, JsonSerializationParameters parameters = default)
        {
            var configuration = SerializedObjectReaderConfiguration.Default;

            configuration.UseReadAsync = false;
            configuration.ValidationType = parameters.DisableValidation ? JsonValidationType.None : parameters.Simplified ? JsonValidationType.Simple : JsonValidationType.Standard;
            configuration.BlockBufferSize = math.max(json.Length * sizeof(char), 16);
            configuration.TokenBufferSize = math.max(json.Length / 2, 16);
            configuration.OutputBufferSize = math.max(json.Length * sizeof(char), 16);
            configuration.StripStringEscapeCharacters = parameters.StringEscapeHandling;

            return configuration;
        }
    }
}
#endif
