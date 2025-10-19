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
        private static string _enumIndexBacking;
        private static string _enumNameBacking;
        private static string _enumValueBacking;

        private static EnumCache[] _cache;
        private static bool _hasWarnedNone;
        private static bool _createdNames = false;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            UpdatePropertyNames();
            EnsureCache();
            SerializedProperty enumIndexProp = property.FindPropertyRelative(_enumIndexBacking);
            SerializedProperty enumNameProp = property.FindPropertyRelative(_enumNameBacking);
            UpdateSelectedEnum(enumNameProp, enumIndexProp);

            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float lineAndSpace = line + spacing;
            float size = 2 * lineAndSpace;
            size += AddEnumSelectionSize();
            size += AddRoomForHelpBox(enumIndexProp);
            return size;
        }

        private float AddEnumSelectionSize()
        {
            return _cache.Length > 1 ? EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing : 0;
        }

        private float AddRoomForHelpBox(SerializedProperty enumIndexProp)
        {
            bool hasHelpBox = _cache.Length == 0 || _cache[enumIndexProp.intValue].EnumType.Attribute.IsExample;
            return hasHelpBox ? EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing : 0;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            UpdatePropertyNames();
            EnsureCache();
            
            SerializedProperty enumValueProp = property.FindPropertyRelative(_enumValueBacking);
            DrawHeader(position, label.text);

            if (CreateNoEnumHelpBox(position, label, enumValueProp))
            {
                return;
            }

            SerializedProperty enumIndexProp = property.FindPropertyRelative(_enumIndexBacking);
            SerializedProperty enumNameProp = property.FindPropertyRelative(_enumNameBacking);
            UpdateSelectedEnum(enumNameProp, enumIndexProp);
            CreatePropertyScope(position, label, property, enumIndexProp, enumNameProp, enumValueProp);
        }

        private void CreatePropertyScope(Rect position, GUIContent label, SerializedProperty property, 
            SerializedProperty enumIndexProp, SerializedProperty enumNameProp, SerializedProperty enumValueProp)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            using (new EditorGUI.PropertyScope(position, label, property))
            { 
                Rect enumRect = new Rect(position.x, position.y + spacing + line, position.width,
                    EditorGUIUtility.singleLineHeight);
                Rect enumValueRect = new Rect(position.x, enumRect.y + spacing + line, position.width,
                    EditorGUIUtility.singleLineHeight);
                bool shallDrawEnumDropdown = _cache.Length > 1;
                int currentEnumIndex = shallDrawEnumDropdown ? CreateEnumDropdown(enumIndexProp, enumNameProp, enumRect) : 0;
                Rect lineRect = shallDrawEnumDropdown ? enumValueRect : enumRect;
                CreateEnumValueDropdown(enumValueProp, currentEnumIndex, lineRect, label.text);
            }
        }

        private void UpdateSelectedEnum(SerializedProperty enumName, SerializedProperty enumIndex)
        {
            int currentEnumIndex = enumIndex.intValue;
            EnumCache selectedCache = currentEnumIndex < _cache.Length ? _cache[currentEnumIndex] : null;
            if (selectedCache == null || selectedCache.EnumType.EnumType.FullName != enumName.stringValue)
            {
                EnumCache matchingCache =
                    _cache.FirstOrDefault(e => e.EnumType.EnumType.FullName == enumName.stringValue);
                int index = Array.IndexOf(_cache, matchingCache);
                if (index != currentEnumIndex)
                {
                    enumIndex.intValue = index >= 0 ? index : 0;
                    enumIndex.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private void DrawHeader(Rect position, string label)
        {
            Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(headerRect, label, EditorStyles.boldLabel);
        }

        private bool CreateNoEnumHelpBox(Rect position, GUIContent label, SerializedProperty enumValueProp)
        {
            if (_cache.Length > 0)
                return false;
            
            float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            Rect enumRect = new Rect(position.x, y, position.width,
                EditorGUIUtility.singleLineHeight);
            // Draw label as a foldout-less property line
            EditorGUI.PropertyField(enumRect, enumValueProp, label);
            Rect help = position;
            help.yMin = enumRect.yMax + EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.HelpBox(help,
                $"No enum type with [{nameof(ColorStyleAttribute)}] was found.\n" +
                $"Define a custom enum and decorate it with [{nameof(ColorStyleAttribute)}].",
                MessageType.Info);
            return true;
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

        private static void UpdatePropertyNames()
        {
            if(_createdNames)
                return;
            
            _createdNames = true;
            _enumIndexBacking = $"<{nameof(ColorStyle.SelectedEnumIndex)}>k__BackingField";
            _enumNameBacking = $"<{nameof(ColorStyle.SelectedEnumName)}>k__BackingField";
            _enumValueBacking = $"<{nameof(ColorStyle.EnumValue)}>k__BackingField";
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