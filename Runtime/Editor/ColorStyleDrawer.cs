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
        private const string _enumIndexBacking = "<SelectedEnumIndex>k__BackingField";
        private const string _enumNameBacking = "<SelectedEnumName>k__BackingField";
        private const string _enumValueBacking = "<EnumValue>k__BackingField";

        private static EnumCache[] _cache;
        private static bool _hasWarnedNone;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            EnsureCache();

            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float size = 2 * line + 2 * spacing;

            if (_cache.Length > 1)
            {
                size += line + spacing;
            }

            var enumIndexProp = property.FindPropertyRelative(_enumIndexBacking);
            EnumCache selectedCache = enumIndexProp.intValue < _cache.Length ? _cache[enumIndexProp.intValue] : null;
            if (_cache.Length == 0 ||
                selectedCache != null && selectedCache.EnumType.Attribute.IsExample)
            {
                // room for help box
                size += spacing + line * 2f;
            }

            return size;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EnsureCache();

            SerializedProperty enumIndexProp = property.FindPropertyRelative(_enumIndexBacking);
            SerializedProperty enumValueProp = property.FindPropertyRelative(_enumValueBacking);
            SerializedProperty enumNameProp = property.FindPropertyRelative(_enumNameBacking);

            if (enumValueProp == null || enumIndexProp == null || enumNameProp == null)
            {
                EditorGUI.HelpBox(position, "Could not find backing fields.", MessageType.Error);
                return;
            }

            // Draw label as a foldout-less property line
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect enumRect = new Rect(position.x, headerRect.y + spacing + line, position.width,
                EditorGUIUtility.singleLineHeight);
            Rect enumValueRect = new Rect(position.x, enumRect.y + spacing + line, position.width,
                EditorGUIUtility.singleLineHeight);

            // Draw header
            EditorGUI.LabelField(headerRect, label.text, EditorStyles.boldLabel);

            if (_cache.Length == 0)
            {
                // Fallback: show int field and a help box explaining why
                EditorGUI.PropertyField(enumRect, enumValueProp, label);
                Rect help = position;
                help.yMin = enumRect.yMax + EditorGUIUtility.standardVerticalSpacing;
                EditorGUI.HelpBox(help,
                    $"No enum type with [{nameof(ColorStyleAttribute)}] was found.\n" +
                    $"Define a custom enum and decorate it with [{nameof(ColorStyleAttribute)}].",
                    MessageType.Info);
                return;
            }
            
            int currentEnumIndex = enumIndexProp.intValue;
            EnumCache selectedCache = currentEnumIndex < _cache.Length ? _cache[currentEnumIndex] : null;
            if (selectedCache == null || selectedCache.EnumType.EnumType.FullName != enumNameProp.stringValue)
            {
                EnumCache matchingCache =
                    _cache.FirstOrDefault(e => e.EnumType.EnumType.FullName == enumNameProp.stringValue);
                int index = Array.IndexOf(_cache, matchingCache);
                if (index >= 0 && index != currentEnumIndex)
                {
                    enumIndexProp.intValue = index;
                    enumIndexProp.serializedObject.ApplyModifiedProperties();
                }
            }

            using (new EditorGUI.PropertyScope(position, label, property))
            {
                bool shallDrawEnumDropdown = _cache.Length > 1;
                currentEnumIndex = shallDrawEnumDropdown ? CreateEnumDropdown(enumIndexProp, enumNameProp, enumRect) : 0;
                Rect lineRect = shallDrawEnumDropdown ? enumValueRect : enumRect;
                CreateEnumValueDropdown(enumValueProp, currentEnumIndex, lineRect, label.text);
            }
        }

        private int CreateEnumDropdown(SerializedProperty enumIndexProp, SerializedProperty enumNameProp, Rect line)
        {
            int currentEnumIndex = enumIndexProp.intValue;
            EnumCache selectedCache = _cache[currentEnumIndex];

            GUIContent fieldLabel = new GUIContent(
                "Enum source",
                $"Selected enum marked with [{nameof(ColorStyleAttribute)}]: {selectedCache.DisplayName}");

            int newIndex = EditorGUI.Popup(line, fieldLabel, currentEnumIndex,
                _cache.Select(e => new GUIContent(e.DisplayName)).ToArray());

            if (newIndex != currentEnumIndex)
            {
                enumIndexProp.intValue = newIndex >= 0 && newIndex < _cache.Length ? newIndex : 0;
                enumNameProp.stringValue = _cache[enumIndexProp.intValue].EnumType.EnumType.FullName;
                enumIndexProp.serializedObject.ApplyModifiedProperties();
            }

            return enumIndexProp.intValue;
        }

        private void CreateEnumValueDropdown(SerializedProperty enumValueProp,
            int currentEnumIndex, Rect lineRect, string label)
        {
            EnumCache selectedCache = _cache[currentEnumIndex];

            // Map current int to index in enum values; if not found, keep current as custom entry
            int index = Array.IndexOf(selectedCache.Values, enumValueProp.intValue);
            if (index < 0) index = 0; // default to first entry if unknown

            // Compose label with enum type for clarity when hovering
            var fieldLabel = new GUIContent(
                label,
                $"Enum source: {selectedCache.DisplayName}");

            int newIndex = EditorGUI.Popup(lineRect, fieldLabel, index, selectedCache.Options);

            if (newIndex != index && newIndex >= 0 && newIndex < selectedCache.Values.Length)
            {
                enumValueProp.intValue = selectedCache.Values[newIndex];
                enumValueProp.serializedObject.ApplyModifiedProperties();
            }

            if (selectedCache.EnumType.Attribute.IsExample)
            {
                float line = EditorGUIUtility.singleLineHeight;
                float spacing = EditorGUIUtility.standardVerticalSpacing;
                var helpRect = new Rect(lineRect.x, lineRect.yMax + spacing, lineRect.width, line * 2);
                EditorGUI.HelpBox(helpRect,
                    $"Using example enum with [{nameof(ColorStyleAttribute)}]. Consider defining a custom one.",
                    MessageType.Warning);
            }
        }

        /// <summary>
        /// Build (or validate) the cache of the enum marked with [ColorStyleAttribute].
        /// </summary>
        private static void EnsureCache()
        {
            if (_cache != null && _cache.Length != 0) return;

            List<StyleEnumInfo> enums = FindEnumsWithColorStyleAttribute().ToList();

            if (enums.Count == 0)
            {
                _cache = Array.Empty<EnumCache>();
                if (!_hasWarnedNone)
                {
                    _hasWarnedNone = true;
                    // One-time log to avoid spam
                    Debug.Log(
                        $"{nameof(ColorStyleDrawer)}: No enum type marked with [{nameof(ColorStyleAttribute)}] was found. " +
                        "Define one like:\n" +
                        "[ColorStyle] public enum MyColorStyles { Primary, Secondary, ... }");
                }

                return;
            }

            _cache = new EnumCache[enums.Count];
            int index = 0;
            foreach (StyleEnumInfo enumInfo in enums.Where(e => !e.Attribute.IsExample))
            {
                _cache[index] = CreateEnumCache(enumInfo);
                index++;
            }

            foreach (StyleEnumInfo enumInfo in enums.Where(e => e.Attribute.IsExample))
            {
                _cache[index] = CreateEnumCache(enumInfo);
                index++;
            }
        }

        /// <summary>
        /// Find all enum types in loaded assemblies that are decorated with [ColorStyleAttribute].
        /// </summary>
        private static IEnumerable<StyleEnumInfo> FindEnumsWithColorStyleAttribute()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            // Filter out dynamic and editor-only assemblies that may throw
            foreach (var assembly in assemblies)
            {
                foreach (StyleEnumInfo type in FindEnumsWithColorStyleAttribute(assembly))
                {
                    yield return type;
                }
            }
        }

        private static IEnumerable<StyleEnumInfo> FindEnumsWithColorStyleAttribute(Assembly assembly)
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

            foreach (Type type in types)
            {
                if (type is not { IsEnum: true }) continue;

                // Check for [ColorStyleAttribute]
                if (type.GetCustomAttributes(inherit: false)
                        .FirstOrDefault(a => a is ColorStyleAttribute) is ColorStyleAttribute styleAttribute)
                    yield return new StyleEnumInfo() { EnumType = type, Attribute = styleAttribute };
            }
        }

        private static EnumCache CreateEnumCache(StyleEnumInfo enumInfo)
        {
            string[] names = Enum.GetNames(enumInfo.EnumType);
            Array valuesArray = Enum.GetValues(enumInfo.EnumType);

            int[] values = new int[valuesArray.Length];
            for (int i = 0; i < valuesArray.Length; i++)
                values[i] = (int)Convert.ChangeType(valuesArray.GetValue(i), typeof(int));

            GUIContent[] options = new GUIContent[names.Length];
            for (int i = 0; i < names.Length; i++)
                options[i] = new GUIContent(ObjectNames.NicifyVariableName(names[i]));

            return new EnumCache
            {
                EnumType = enumInfo,
                DisplayName = enumInfo.EnumType.FullName,
                Names = names,
                Values = values,
                Options = options
            };
        }

        private struct StyleEnumInfo
        {
            public ColorStyleAttribute Attribute;
            public Type EnumType;
        }

        private class EnumCache
        {
            public StyleEnumInfo EnumType;
            public string DisplayName;
            public string[] Names;
            public int[] Values;
            public GUIContent[] Options;
        }
    }
}