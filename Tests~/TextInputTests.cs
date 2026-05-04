using System;
using System.Globalization;
using System.Reflection;
using Calluna.DI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Calluna.UI.Tests
{
    public class TextInputTests
    {
        private GameObject _root;
        private FakeStringInput _input;
        private TMP_InputField _tmpField;
        private Observable<string> _observable;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("TestTextInput");
            _tmpField = _root.AddComponent<TMP_InputField>();
            _input = _root.AddComponent<FakeStringInput>();
            _observable = new Observable<string>();
            _observable.SetValueWithoutNotify("hello");
            SetField(_input, "_inputField", _tmpField);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        // ── TextInputVisualArgs ──────────────────────────────────────────────────

        [Test]
        public void TextInputVisualArgs_Constructor_StoresTargetAndColor()
        {
            var go = new GameObject();
            var graphic = go.AddComponent<Image>();
            var args = new TextInputVisualArgs(graphic, Color.red);

            Assert.AreEqual(graphic, args.Target);
            Assert.AreEqual(Color.red, args.InvalidColor);

            Object.DestroyImmediate(go);
        }

        // ── Prohibit empty — OnValueChanged mode ─────────────────────────────────

        [Test]
        public void TextInput_OnValueChanged_ProhibitEmpty_BlankInput_DoesNotWriteObservable()
        {
            SetField(_input, "_isEmptyProhibited", true);
            InitInput(null);

            string valueBefore = _observable.Value;
            _tmpField.onValueChanged.Invoke(string.Empty);

            Assert.AreEqual(valueBefore, _observable.Value);
        }

        [Test]
        public void TextInput_OnValueChanged_ProhibitEmpty_WhitespaceInput_DoesNotWriteObservable()
        {
            SetField(_input, "_isEmptyProhibited", true);
            InitInput(null);

            string valueBefore = _observable.Value;
            _tmpField.onValueChanged.Invoke("   ");

            Assert.AreEqual(valueBefore, _observable.Value);
        }

        [Test]
        public void TextInput_OnValueChanged_ProhibitEmpty_NonBlankInput_WritesObservable()
        {
            SetField(_input, "_isEmptyProhibited", true);
            InitInput(null);

            _tmpField.onValueChanged.Invoke("world");

            Assert.AreEqual("world", _observable.Value);
        }

        [Test]
        public void TextInput_OnValueChanged_DefaultMode_BlankInput_WritesObservable()
        {
            InitInput(null);

            _tmpField.onValueChanged.Invoke(string.Empty);

            Assert.AreEqual(string.Empty, _observable.Value);
        }

        // ── Update mode: OnSubmit ────────────────────────────────────────────────

        [Test]
        public void TextInput_OnValueChanged_OnSubmitMode_DoesNotWriteObservable()
        {
            SetField(_input, "_updateMode", TextInputUpdateMode.OnSubmit);
            InitInput(null);

            string valueBefore = _observable.Value;
            _tmpField.onValueChanged.Invoke("world");

            Assert.AreEqual(valueBefore, _observable.Value);
        }

        [Test]
        public void TextInput_OnEndEdit_OnSubmitMode_NonBlankInput_WritesObservable()
        {
            SetField(_input, "_updateMode", TextInputUpdateMode.OnSubmit);
            InitInput(null);

            _tmpField.onEndEdit.Invoke("submitted");

            Assert.AreEqual("submitted", _observable.Value);
        }

        // ── Prohibit empty — OnEndEdit ───────────────────────────────────────────

        [Test]
        public void TextInput_OnEndEdit_ProhibitEmpty_BlankInput_DoesNotWriteObservable()
        {
            SetField(_input, "_isEmptyProhibited", true);
            SetField(_input, "_updateMode", TextInputUpdateMode.OnSubmit);
            InitInput(null);

            string valueBefore = _observable.Value;
            _tmpField.onEndEdit.Invoke(string.Empty);

            Assert.AreEqual(valueBefore, _observable.Value);
        }

        [Test]
        public void TextInput_OnEndEdit_ProhibitEmpty_NonBlankInput_WritesObservable()
        {
            SetField(_input, "_isEmptyProhibited", true);
            SetField(_input, "_updateMode", TextInputUpdateMode.OnSubmit);
            InitInput(null);

            _tmpField.onEndEdit.Invoke("world");

            Assert.AreEqual("world", _observable.Value);
        }

        // ── Visual feedback ──────────────────────────────────────────────────────

        [Test]
        public void TextInput_VisualArgs_NotBound_NoExceptionOnBlankInput()
        {
            SetField(_input, "_isEmptyProhibited", true);
            InitInput(null);

            Assert.DoesNotThrow(() => _tmpField.onValueChanged.Invoke(string.Empty));
        }

        [Test]
        public void TextInput_VisualArgs_Bound_BlankInput_AppliesInvalidColor()
        {
            var graphicGO = new GameObject("Graphic");
            var graphic = graphicGO.AddComponent<Image>();
            var originalColor = Color.white;
            graphic.color = originalColor;
            var args = new TextInputVisualArgs(graphic, Color.red);

            SetField(_input, "_isEmptyProhibited", true);
            InitInput(args);

            _tmpField.onValueChanged.Invoke(string.Empty);

            Assert.AreEqual(Color.red, graphic.color);

            Object.DestroyImmediate(graphicGO);
        }

        [Test]
        public void TextInput_VisualArgs_Bound_NonBlankInput_RestoresOriginalColor()
        {
            var graphicGO = new GameObject("Graphic");
            var graphic = graphicGO.AddComponent<Image>();
            graphic.color = Color.white;
            var args = new TextInputVisualArgs(graphic, Color.red);

            SetField(_input, "_isEmptyProhibited", true);
            InitInput(args);

            _tmpField.onValueChanged.Invoke(string.Empty);   // turns red
            _tmpField.onValueChanged.Invoke("text");          // restores

            Assert.AreEqual(Color.white, graphic.color);

            Object.DestroyImmediate(graphicGO);
        }

        [Test]
        public void TextInput_ParsingFailed_InvalidInput_EventFired()
        {
            var failGO  = new GameObject("FailingInput");
            var tmpField = failGO.AddComponent<TMP_InputField>();
            var failing  = failGO.AddComponent<FakeFailingStringInput>();
            SetField(failing, "_inputField", tmpField);

            bool eventFired = false;
            failing.ParsingFailed += _ => eventFired = true;

            failing.DoInject(new FakeResolver(_observable, null));
            failing.DoInitialize();
            tmpField.onValueChanged.Invoke("bad");

            Assert.IsTrue(eventFired, "ParsingFailed must fire when TryParseInput returns false");

            Object.DestroyImmediate(failGO);
        }

        [Test]
        public void TextInput_OnClean_WithVisualArgs_RestoresOriginalColor()
        {
            var graphicGO = new GameObject("Graphic");
            var graphic   = graphicGO.AddComponent<Image>();
            graphic.color = Color.white;
            var args      = new TextInputVisualArgs(graphic, Color.red);

            SetField(_input, "_isEmptyProhibited", true);
            InitInput(args);

            _tmpField.onValueChanged.Invoke(string.Empty); // turns red
            _input.DoClean();                               // must restore original color

            Assert.AreEqual(Color.white, graphic.color);

            Object.DestroyImmediate(graphicGO);
        }

        [Test]
        public void TextInput_OnEndEdit_ProhibitEmpty_BlankInput_WithVisualArgs_RestoresColor()
        {
            var graphicGO = new GameObject("Graphic");
            var graphic   = graphicGO.AddComponent<Image>();
            graphic.color = Color.white;
            var args      = new TextInputVisualArgs(graphic, Color.red);

            SetField(_input, "_isEmptyProhibited", true);
            SetField(_input, "_updateMode", TextInputUpdateMode.OnSubmit);
            InitInput(args);

            _tmpField.onValueChanged.Invoke(string.Empty); // turns red
            _tmpField.onEndEdit.Invoke(string.Empty);       // blank + prohibit → reverts display and restores color

            Assert.AreEqual(Color.white, graphic.color,
                "OnEndEdit with blank prohibited input must restore the original graphic color");

            Object.DestroyImmediate(graphicGO);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private void InitInput(TextInputVisualArgs visualArgs)
        {
            _input.DoInject(new FakeResolver(_observable, visualArgs));
            _input.DoInitialize();
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var type = target.GetType();
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
            throw new ArgumentException($"Field '{fieldName}' not found on {target.GetType().Name} or any base type.");
        }

        // ── Inner types ──────────────────────────────────────────────────────────

        private class FakeStringInput : TextInput<string>
        {
            public void DoInject(Resolver resolver) => OnInject(resolver);
            public void DoInitialize() => OnInitialize();
            public void DoClean() => OnClean();

            protected override bool TryParseInput(string input, out string result)
            {
                result = input;
                return true;
            }

            // TMP_InputField.SetTextWithoutNotify requires a full TMP mesh hierarchy that
            // doesn't exist in EditMode tests — stub it out at the seam.
            protected override void ApplyDisplayValue(string text) { }
        }

        private class FakeFailingStringInput : TextInput<string>
        {
            public void DoInject(Resolver resolver) => OnInject(resolver);
            public void DoInitialize() => OnInitialize();

            protected override bool TryParseInput(string input, out string result)
            {
                result = default;
                return false;
            }

            protected override void ApplyDisplayValue(string text) { }
        }

        private class FakeResolver : Resolver
        {
            private readonly Observable<string> _observable;
            private readonly TextInputVisualArgs _visualArgs;

            public FakeResolver(Observable<string> observable, TextInputVisualArgs visualArgs)
            {
                _observable = observable;
                _visualArgs = visualArgs;
            }

            TContract Resolver.Resolve<TContract>()
            {
                if (typeof(TContract) == typeof(Observable<string>))
                    return (TContract)(object)_observable;
                throw new InvalidOperationException($"FakeResolver: unexpected Resolve<{typeof(TContract).Name}>");
            }

            TContract Resolver.ResolveOptional<TContract>()
            {
                if (typeof(TContract) == typeof(CultureInfo))
                    return default;
                if (typeof(TContract) == typeof(TextInputVisualArgs))
                    return (TContract)(object)_visualArgs;
                return default;
            }

            TContract Resolver.Resolve<TContract>(IComparable iD) => throw new NotImplementedException();
            TContract Resolver.Resolve<TContract>(BindingKey bindingKey) => throw new NotImplementedException();
            TContract Resolver.ResolveOptional<TContract>(IComparable iD) => throw new NotImplementedException();
            TContract Resolver.ResolveOptional<TContract>(BindingKey bindingKey) => throw new NotImplementedException();
            bool Resolver.IsResolvable(BindingKey bindingKey) => throw new NotImplementedException();
        }
    }
}
