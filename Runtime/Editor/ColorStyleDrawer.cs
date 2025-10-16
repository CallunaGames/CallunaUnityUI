using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Calluna.UI.Editor
{
    [CustomPropertyDrawer(typeof(ColorStyle))]
    public class ColorStyleDrawer : PropertyDrawer
    {
        private class EnumCache
        {
            public Type EnumType;
            public string DisplayName;
            public string[] Names;
            public int[] Values;
            public GUIContent[] Options;
        }

        private static EnumCache _cache;
        private static bool _hasWarnedMultiple;
        private static bool _hasWarnedNone;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            EnsureCache();

            // Height: main line + optional help box
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            if (_cache == null)
            {
                // room for help box
                return line + spacing + EditorGUIUtility.singleLineHeight * 2f;
            }

            if (_hasWarnedMultiple)
            {
                return line + spacing + EditorGUIUtility.singleLineHeight * 2f;
            }

            return line;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnsureCache();

            // Find the backing int field
            SerializedProperty enumValueProp = property.FindPropertyRelative("<EnumValue>k__BackingField");
            if (enumValueProp == null)
            {
                // In case the auto-property backing name differs (older Unity), try by name.
                enumValueProp = property.FindPropertyRelative("EnumValue");
            }

            if (enumValueProp == null)
            {
                EditorGUI.HelpBox(position, "Could not find 'EnumValue' int field.", MessageType.Error);
                return;
            }

            // Draw label as a foldout-less property line
            Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

            if (_cache == null)
            {
                // Fallback: show int field and a help box explaining why
                EditorGUI.PropertyField(line, enumValueProp, label);
                Rect help = position;
                help.yMin = line.yMax + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.HelpBox(help,
                    "No enum type with [ColorStyleAttribute] was found.\n" +
                    "Define a custom enum and decorate it with [ColorStyleAttribute].",
                    MessageType.Info);
                return;
            }

            using (new EditorGUI.PropertyScope(position, label, property))
            {
                // Current stored int
                int current = enumValueProp.intValue;

                // Map current int to index in enum values; if not found, keep current as custom entry
                int index = Array.IndexOf(_cache.Values, current);
                if (index < 0) index = 0; // default to first entry if unknown

                // Compose label with enum type for clarity when hovering
                var fieldLabel = new GUIContent(
                    label.text,
                    $"Enum source: {_cache.DisplayName}");

                int newIndex = EditorGUI.Popup(line, fieldLabel, index, _cache.Options);

                if (newIndex != index && newIndex >= 0 && newIndex < _cache.Values.Length)
                {
                    enumValueProp.intValue = _cache.Values[newIndex];
                    enumValueProp.serializedObject.ApplyModifiedProperties();
                }

                // Optional warning if multiple enums found
                if (_hasWarnedMultiple)
                {
                    var help = position;
                    help.yMin = line.yMax + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.HelpBox(help,
                        $"Multiple enums with [ColorStyleAttribute] were found. Using '{_cache.DisplayName}'.",
                        MessageType.Warning);
                }
            }
        }

        /// <summary>
        /// Build (or validate) the cache of the enum marked with [ColorStyleAttribute].
        /// </summary>
        private static void EnsureCache()
        {
            if (_cache != null) return;

            List<Type> enums = FindEnumsWithColorStyleAttribute().ToList();

            if (enums.Count == 0)
            {
                _cache = null;
                if (!_hasWarnedNone)
                {
                    _hasWarnedNone = true;
                    // One-time log to avoid spam
                    Debug.Log(
                        $"{nameof(ColorStyleDrawer)}: No enum type marked with [ColorStyleAttribute] was found. " +
                        "Define one like:\n" +
                        "[ColorStyle] public enum MyColorStyles { Primary, Secondary, ... }");
                }

                return;
            }

            if (enums.Count > 1 && !_hasWarnedMultiple)
            {
                _hasWarnedMultiple = true;
                Debug.LogWarning(
                    $"{nameof(ColorStyleDrawer)}: Multiple enums marked with [ColorStyleAttribute] were found. " +
                    "Using the first one found.");
            }

            var enumType = enums[0];

            string[] names = Enum.GetNames(enumType);
            Array valuesArray = Enum.GetValues(enumType);

            int[] values = new int[valuesArray.Length];
            for (int i = 0; i < valuesArray.Length; i++)
                values[i] = (int)Convert.ChangeType(valuesArray.GetValue(i), typeof(int));

            var options = new GUIContent[names.Length];
            for (int i = 0; i < names.Length; i++)
                options[i] = new GUIContent(ObjectNames.NicifyVariableName(names[i]));

            _cache = new EnumCache
            {
                EnumType = enumType,
                DisplayName = enumType.FullName,
                Names = names,
                Values = values,
                Options = options
            };
        }

        /// <summary>
        /// Find all enum types in loaded assemblies that are decorated with [ColorStyleAttribute].
        /// </summary>
        private static IEnumerable<Type> FindEnumsWithColorStyleAttribute()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // Filter out dynamic and editor-only assemblies that may throw
            foreach (var assembly in assemblies)
            {
                foreach (Type type in FindEnumsWithColorStyleAttribute(assembly))
                {
                    yield return type;
                }
            }
        }

        private static IEnumerable<Type> FindEnumsWithColorStyleAttribute(Assembly assembly)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types.Where(t => t != null).ToArray();
            }
            catch
            {
                yield break;
            }

            string attributeName = nameof(ColorStyleAttribute);
            string endName = $".{nameof(ColorStyleAttribute)}";
            foreach (Type type in types)
            {
                if (type is not { IsEnum: true }) continue;

                // Check for [ColorStyleAttribute]
                var hasAttr = type.GetCustomAttributes(inherit: false)
                    .Any(a => string.Equals(a.GetType().Name, attributeName, StringComparison.Ordinal) ||
                              // Support namespaces: match by full name end
                              a.GetType().FullName?.EndsWith(endName, StringComparison.Ordinal) == true);
                if (hasAttr)
                    yield return type;
            }
        }
    }
}