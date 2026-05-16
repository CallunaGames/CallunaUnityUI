using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Calluna.UI.Tests
{
    public class ColorStyleSettingsTests
    {
        // ── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a ColorStyleSettings ScriptableObject and injects a pre-built
        /// settings list via reflection (the field is private and serialized).
        /// </summary>
        private static ColorStyleSettings MakeSettings(
            IEnumerable<(ColorStyleId id, Color color)> entries)
        {
            var so = ScriptableObject.CreateInstance<ColorStyleSettings>();

            // Build the inner ColorStyleSetting list via reflection.
            var settingType = typeof(ColorStyleSettings).GetNestedType(
                "ColorStyleSetting", BindingFlags.Public | BindingFlags.NonPublic);

            var list = (System.Collections.IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(settingType));

            foreach ((ColorStyleId id, Color color) in entries)
            {
                object entry = Activator.CreateInstance(settingType);

                // ColorStyleSetting.Style has a backing field generated from [field: SerializeField].
                FieldInfo styleField = settingType.GetField(
                    "<Style>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo colorField = settingType.GetField(
                    "<Color>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);

                styleField.SetValue(entry, id);
                colorField.SetValue(entry, color);

                list.Add(entry);
            }

            FieldInfo settingsField = typeof(ColorStyleSettings).GetField(
                "_settings", BindingFlags.Instance | BindingFlags.NonPublic);
            settingsField.SetValue(so, list);

            return so;
        }

        private static ColorStyleId MakeId()
        {
            return ScriptableObject.CreateInstance<ColorStyleId>();
        }

        // ── TryGetColorOf ────────────────────────────────────────────────────────

        [Test]
        [Description("TryGetColorOf with a matching style => returns true and the configured color?")]
        public void ColorStyleSettings_TryGetColorOf_StyleExists_ReturnsTrueAndCorrectColor()
        {
            ColorStyleId id = MakeId();
            var expected = new Color(0.1f, 0.2f, 0.3f, 1f);

            var settings = MakeSettings(new[] { (id, expected) });

            bool result = settings.TryGetColorOf(id, out Color actual);

            Assert.IsTrue(result);
            Assert.AreEqual(expected, actual);

            UnityEngine.Object.DestroyImmediate(id);
            UnityEngine.Object.DestroyImmediate(settings);
        }

        [Test]
        [Description("TryGetColorOf with an ID not in the list => returns false and default Color?")]
        public void ColorStyleSettings_TryGetColorOf_StyleMissing_ReturnsFalse()
        {
            ColorStyleId registeredId = MakeId();
            ColorStyleId unknownId    = MakeId();

            var settings = MakeSettings(new[] { (registeredId, Color.red) });

            bool result = settings.TryGetColorOf(unknownId, out Color actual);

            Assert.IsFalse(result);
            Assert.AreEqual(default(Color), actual);

            UnityEngine.Object.DestroyImmediate(registeredId);
            UnityEngine.Object.DestroyImmediate(unknownId);
            UnityEngine.Object.DestroyImmediate(settings);
        }

        [TestCase(0f, 0f, 0f)]
        [TestCase(1f, 0f, 0f)]
        [TestCase(0f, 1f, 0f)]
        [TestCase(0.5f, 0.5f, 0.5f)]
        [Description("TryGetColorOf with a matching style => out-color components match the configured values?")]
        public void ColorStyleSettings_TryGetColorOf_StyleExists_OutColorMatchesComponents(
            float r, float g, float b)
        {
            ColorStyleId id = MakeId();
            var expected = new Color(r, g, b, 1f);

            var settings = MakeSettings(new[] { (id, expected) });

            settings.TryGetColorOf(id, out Color actual);

            Assert.AreEqual(expected.r, actual.r, 0.0001f);
            Assert.AreEqual(expected.g, actual.g, 0.0001f);
            Assert.AreEqual(expected.b, actual.b, 0.0001f);

            UnityEngine.Object.DestroyImmediate(id);
            UnityEngine.Object.DestroyImmediate(settings);
        }

        // ── GetColorOf ───────────────────────────────────────────────────────────

        [Test]
        [Description("GetColorOf with a matching style => returns the configured color without throwing?")]
        public void ColorStyleSettings_GetColorOf_StyleExists_ReturnsColor()
        {
            ColorStyleId id = MakeId();
            var expected = new Color(0.4f, 0.5f, 0.6f, 1f);

            var settings = MakeSettings(new[] { (id, expected) });

            Color actual = settings.GetColorOf(id);

            Assert.AreEqual(expected, actual);

            UnityEngine.Object.DestroyImmediate(id);
            UnityEngine.Object.DestroyImmediate(settings);
        }

        [TestCase(0.25f, 0.5f, 0.75f)]
        [TestCase(1f,    1f,   1f)]
        [TestCase(0f,    0f,   0f)]
        [Description("GetColorOf with several distinct configured colors => each returned color matches?")]
        public void ColorStyleSettings_GetColorOf_StyleExists_ReturnsCorrectColorForDistinctValues(
            float r, float g, float b)
        {
            ColorStyleId id = MakeId();
            var expected = new Color(r, g, b, 1f);

            var settings = MakeSettings(new[] { (id, expected) });

            Color actual = settings.GetColorOf(id);

            Assert.AreEqual(expected.r, actual.r, 0.0001f);
            Assert.AreEqual(expected.g, actual.g, 0.0001f);
            Assert.AreEqual(expected.b, actual.b, 0.0001f);

            UnityEngine.Object.DestroyImmediate(id);
            UnityEngine.Object.DestroyImmediate(settings);
        }

        [Test]
        [Description("GetColorOf with an ID not in the list => throws ArgumentException?")]
        public void ColorStyleSettings_GetColorOf_StyleMissing_ThrowsArgumentException()
        {
            ColorStyleId registeredId = MakeId();
            ColorStyleId unknownId    = MakeId();

            var settings = MakeSettings(new[] { (registeredId, Color.white) });

            Assert.Throws<ArgumentException>(() => settings.GetColorOf(unknownId));

            UnityEngine.Object.DestroyImmediate(registeredId);
            UnityEngine.Object.DestroyImmediate(unknownId);
            UnityEngine.Object.DestroyImmediate(settings);
        }

        [Test]
        [Description("GetColorOf on an empty settings list => throws ArgumentException?")]
        public void ColorStyleSettings_GetColorOf_EmptySettings_ThrowsArgumentException()
        {
            ColorStyleId id = MakeId();

            var settings = MakeSettings(Array.Empty<(ColorStyleId, Color)>());

            Assert.Throws<ArgumentException>(() => settings.GetColorOf(id));

            UnityEngine.Object.DestroyImmediate(id);
            UnityEngine.Object.DestroyImmediate(settings);
        }

        // ── Multi-entry list ─────────────────────────────────────────────────────

        [Test]
        [Description("TryGetColorOf with multiple entries => returns the color for the correct ID?")]
        public void ColorStyleSettings_TryGetColorOf_MultipleEntries_ReturnsColorForMatchingId()
        {
            ColorStyleId idA = MakeId();
            ColorStyleId idB = MakeId();
            ColorStyleId idC = MakeId();

            var settings = MakeSettings(new[]
            {
                (idA, Color.red),
                (idB, Color.green),
                (idC, Color.blue),
            });

            settings.TryGetColorOf(idB, out Color actual);

            Assert.AreEqual(Color.green, actual);

            UnityEngine.Object.DestroyImmediate(idA);
            UnityEngine.Object.DestroyImmediate(idB);
            UnityEngine.Object.DestroyImmediate(idC);
            UnityEngine.Object.DestroyImmediate(settings);
        }
    }
}
