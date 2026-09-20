using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace BetterFishingRodsVisualResearch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class VisualPrototype : BaseUnityPlugin
    {
        public const string PluginGuid = "nikich.betterfishingrods.visualprototype";
        public const string PluginName = "Better Fishing Rods Visual Prototype";
        public const string PluginVersion = "0.2.0";

        private const BindingFlags AllInstance =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AllStatic =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private const int RingWidth = 64;
        private const int RingHeight = 32;
        private const float RingStartScale = 1.55f;
        private const float RingEndScale = 0.08f;

        internal static VisualPrototype Instance;

        private Harmony _harmony;
        private StreamWriter _writer;

        private Type _fishingGuiType;
        private FieldInfo _waitField;
        private FieldInfo _stateField;
        private FieldInfo _canTakeOutField;

        private GameObject _ringObject;
        private SpriteRenderer _ringRenderer;
        private Sprite _ringSprite;
        private Texture2D _ringTexture;
        private Coroutine _ringCoroutine;
        private int _ringGeneration;

        private void Awake()
        {
            Instance = this;

            try
            {
                var logPath = Path.Combine(
                    Paths.BepInExRootPath,
                    "BetterFishingRodsVisualPrototype-0.2.0.log");

                _writer = new StreamWriter(logPath, false);
                _writer.AutoFlush = true;

                Write("=== Better Fishing Rods Visual Prototype 0.2.0 ===");
                Write("GeneratedUtc=" + DateTime.UtcNow.ToString("O"));
                Write("ApplicationVersion=" + Application.version);
                Write("UnityVersion=" + Application.unityVersion);

                _fishingGuiType = AccessTools.TypeByName("FishingGUI");
                if (_fishingGuiType == null)
                {
                    Write("FATAL FishingGUI type not found.");
                    return;
                }

                _waitField = _fishingGuiType.GetField("_waiting_for_bite_delay", AllInstance);
                _stateField = _fishingGuiType.GetField("_state", AllInstance);
                _canTakeOutField = _fishingGuiType.GetField("can_take_out", AllInstance);

                if (_waitField == null || _stateField == null || _canTakeOutField == null)
                {
                    Write("FATAL expected FishingGUI fields are missing.");
                    return;
                }

                _harmony = new Harmony(PluginGuid);
                PatchThrowingAnimationExit();
                PatchChangeState();

                Write("PrototypeReady=True");
                Write("GameplayChanges=False");
                Write("Purpose=visual countdown ring only");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                SafeWrite("FATAL Awake: " + ex);
            }
        }

        private void OnDestroy()
        {
            StopRing("plugin-destroy");

            try
            {
                if (_harmony != null)
                    _harmony.UnpatchSelf();
            }
            catch (Exception ex)
            {
                SafeWrite("WARN Unpatch: " + ex.Message);
            }

            if (_ringObject != null)
                Destroy(_ringObject);

            if (_ringSprite != null)
                Destroy(_ringSprite);

            if (_ringTexture != null)
                Destroy(_ringTexture);

            if (_writer != null)
            {
                _writer.Dispose();
                _writer = null;
            }

            if (ReferenceEquals(Instance, this))
                Instance = null;
        }

        private void PatchThrowingAnimationExit()
        {
            var type = AccessTools.TypeByName("FishingThrowingAnim");
            if (type == null)
            {
                Write("FATAL FishingThrowingAnim type not found.");
                return;
            }

            var target = type.GetMethods(AllInstance)
                .FirstOrDefault(m =>
                    m.DeclaringType == type &&
                    m.Name == "OnStateExit" &&
                    m.GetParameters().Length == 3);

            if (target == null)
            {
                Write("FATAL FishingThrowingAnim.OnStateExit override not found.");
                return;
            }

            var postfix = typeof(VisualPrototype).GetMethod(
                nameof(ThrowingOnStateExitPostfix),
                BindingFlags.Static | BindingFlags.NonPublic);

            _harmony.Patch(target, postfix: new HarmonyMethod(postfix));
            Write("PATCH_OK " + FormatMethod(target));
        }

        private void PatchChangeState()
        {
            var target = _fishingGuiType.GetMethods(AllInstance)
                .FirstOrDefault(m =>
                    m.DeclaringType == _fishingGuiType &&
                    m.Name == "ChangeState" &&
                    m.GetParameters().Length == 1);

            if (target == null)
            {
                Write("FATAL FishingGUI.ChangeState not found.");
                return;
            }

            var postfix = typeof(VisualPrototype).GetMethod(
                nameof(ChangeStatePostfix),
                BindingFlags.Static | BindingFlags.NonPublic);

            _harmony.Patch(target, postfix: new HarmonyMethod(postfix));
            Write("PATCH_OK " + FormatMethod(target));
        }

        private static void ThrowingOnStateExitPostfix()
        {
            var self = Instance;
            if (self == null)
                return;

            try
            {
                var fishing = self.GetCurrentFishingGui();
                if (fishing == null)
                {
                    self.Write("RING_SKIP reason=no-fishing-gui");
                    return;
                }

                if (!self.IsState(fishing, "WaitingForBite"))
                {
                    self.Write("RING_SKIP reason=not-waiting-for-bite state="
                        + self.ReadState(fishing));
                    return;
                }

                if (!self.ReadBool(self._canTakeOutField, fishing))
                {
                    self.Write("RING_SKIP reason=can-take-out-false");
                    return;
                }

                self.StartRing(fishing);
            }
            catch (Exception ex)
            {
                self.Write("ERROR ThrowingOnStateExitPostfix " + ex);
            }
        }

        private static void ChangeStatePostfix(object __instance, object[] __args)
        {
            var self = Instance;
            if (self == null || __instance == null)
                return;

            try
            {
                var state = self.ReadState(__instance);

                if (!string.Equals(state, "WaitingForBite", StringComparison.Ordinal))
                    self.StopRing("state=" + state);
            }
            catch (Exception ex)
            {
                self.Write("ERROR ChangeStatePostfix " + ex);
            }
        }

        private void StartRing(object fishingGui)
        {
            StopRing("restart");

            var initialWait = ReadFloat(_waitField, fishingGui);
            if (float.IsNaN(initialWait) || initialWait <= 0f)
            {
                Write("RING_SKIP reason=invalid-wait value=" + initialWait);
                return;
            }

            var bobber = FindBobber();
            if (bobber == null)
            {
                Write("RING_SKIP reason=bobber-not-found");
                return;
            }

            var bobberRenderer = bobber.GetComponent<SpriteRenderer>();
            if (bobberRenderer == null || bobberRenderer.sprite == null)
            {
                Write("RING_SKIP reason=bobber-renderer-missing");
                return;
            }

            EnsureRingObject(bobber.transform, bobberRenderer);

            _ringGeneration++;
            var generation = _ringGeneration;

            _ringObject.SetActive(true);
            ApplyRingVisual(1f);

            var sprite = bobberRenderer.sprite;
            Write("RING_START gen=" + generation
                + " wait=" + initialWait.ToString("F6")
                + " bobberSprite=" + sprite.name
                + " ppu=" + sprite.pixelsPerUnit.ToString("F3")
                + " rect=" + sprite.rect
                + " bounds=" + sprite.bounds.size
                + " ringStartScale=" + RingStartScale.ToString("F3"));

            _ringCoroutine = StartCoroutine(
                AnimateRing(fishingGui, initialWait, generation));
        }

        private IEnumerator AnimateRing(
            object fishingGui,
            float initialWait,
            int generation)
        {
            while (generation == _ringGeneration
                && IsState(fishingGui, "WaitingForBite"))
            {
                var remaining = ReadFloat(_waitField, fishingGui);
                if (float.IsNaN(remaining))
                {
                    StopRing("wait-field-invalid");
                    yield break;
                }

                var ratio = Mathf.Clamp01(remaining / initialWait);
                ApplyRingVisual(ratio);

                yield return null;
            }

            if (generation == _ringGeneration)
                StopRing("native-wait-ended");
        }

        private void ApplyRingVisual(float remainingRatio)
        {
            if (_ringObject == null || _ringRenderer == null)
                return;

            var scale = Mathf.Lerp(
                RingEndScale,
                RingStartScale,
                Mathf.Clamp01(remainingRatio));

            _ringObject.transform.localScale =
                new Vector3(scale, scale, 1f);

            var color = _ringRenderer.color;
            color.a = Mathf.Lerp(0.90f, 0.58f, Mathf.Clamp01(remainingRatio));
            _ringRenderer.color = color;
        }

        private void StopRing(string reason)
        {
            _ringGeneration++;

            if (_ringCoroutine != null)
            {
                StopCoroutine(_ringCoroutine);
                _ringCoroutine = null;
            }

            if (_ringObject != null && _ringObject.activeSelf)
            {
                _ringObject.SetActive(false);
                Write("RING_STOP reason=" + reason);
            }
        }

        private void EnsureRingObject(
            Transform bobber,
            SpriteRenderer bobberRenderer)
        {
            if (_ringObject == null)
            {
                _ringObject = new GameObject("BetterFishingRods_CountdownRing");
                _ringRenderer = _ringObject.AddComponent<SpriteRenderer>();

                var ppu = bobberRenderer.sprite.pixelsPerUnit;
                if (ppu <= 0f)
                    ppu = 100f;

                _ringTexture = CreateRingTexture();
                _ringSprite = Sprite.Create(
                    _ringTexture,
                    new Rect(0f, 0f, RingWidth, RingHeight),
                    new Vector2(0.5f, 0.5f),
                    ppu,
                    0u,
                    SpriteMeshType.FullRect);

                _ringSprite.name = "BetterFishingRods_CountdownRing";
                _ringRenderer.sprite = _ringSprite;
                _ringRenderer.color = new Color(0.72f, 0.93f, 1f, 0.58f);

                _ringObject.SetActive(false);
            }

            if (_ringObject.transform.parent != bobber)
                _ringObject.transform.SetParent(bobber, false);

            _ringObject.transform.localPosition = Vector3.zero;
            _ringObject.transform.localRotation = Quaternion.identity;

            _ringRenderer.sortingLayerID = bobberRenderer.sortingLayerID;
            _ringRenderer.sortingOrder = bobberRenderer.sortingOrder - 1;
        }

        private static Texture2D CreateRingTexture()
        {
            var texture = new Texture2D(
                RingWidth,
                RingHeight,
                TextureFormat.RGBA32,
                false);

            texture.name = "BetterFishingRods_CountdownRing_Texture";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[RingWidth * RingHeight];
            var clear = new Color32(255, 255, 255, 0);
            var solid = new Color32(255, 255, 255, 255);

            for (var y = 0; y < RingHeight; y++)
            {
                for (var x = 0; x < RingWidth; x++)
                {
                    var nx = ((x + 0.5f) - RingWidth * 0.5f)
                        / (RingWidth * 0.5f - 1f);
                    var ny = ((y + 0.5f) - RingHeight * 0.5f)
                        / (RingHeight * 0.5f - 1f);

                    var distance = Mathf.Sqrt(nx * nx + ny * ny);

                    pixels[y * RingWidth + x] =
                        distance >= 0.84f && distance <= 1.00f
                            ? solid
                            : clear;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private Transform FindBobber()
        {
            var player = GameObject.Find("Player(Clone)");
            if (player == null)
                return null;

            return player.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t =>
                    string.Equals(t.name, "bobber", StringComparison.OrdinalIgnoreCase));
        }

        private object GetCurrentFishingGui()
        {
            var type = AccessTools.TypeByName("GUIElements");
            if (type == null)
                return null;

            object me = null;

            var meField = type.GetField("me", AllStatic);
            if (meField != null)
                me = meField.GetValue(null);

            if (me == null)
            {
                var meProperty = type.GetProperty("me", AllStatic);
                if (meProperty != null)
                    me = meProperty.GetValue(null, null);
            }

            if (me == null)
                return null;

            var fishingField = type.GetField("fishing", AllInstance);
            if (fishingField != null)
                return fishingField.GetValue(me);

            var fishingProperty = type.GetProperty("fishing", AllInstance);
            return fishingProperty == null
                ? null
                : fishingProperty.GetValue(me, null);
        }

        private bool IsState(object fishingGui, string expected)
        {
            return string.Equals(
                ReadState(fishingGui),
                expected,
                StringComparison.Ordinal);
        }

        private string ReadState(object fishingGui)
        {
            if (_stateField == null || fishingGui == null)
                return "<unknown>";

            var value = _stateField.GetValue(fishingGui);
            return value == null ? "<null>" : value.ToString();
        }

        private static float ReadFloat(FieldInfo field, object instance)
        {
            if (field == null || instance == null)
                return float.NaN;

            try
            {
                return Convert.ToSingle(field.GetValue(instance));
            }
            catch
            {
                return float.NaN;
            }
        }

        private static bool ReadBool(FieldInfo field, object instance)
        {
            if (field == null || instance == null)
                return false;

            try
            {
                return Convert.ToBoolean(field.GetValue(instance));
            }
            catch
            {
                return false;
            }
        }

        private void Write(string message)
        {
            Logger.LogInfo(message);

            if (_writer != null)
                _writer.WriteLine(message);
        }

        private void SafeWrite(string message)
        {
            try
            {
                Write(message);
            }
            catch
            {
            }
        }

        private static string FormatMethod(MethodBase method)
        {
            return method.DeclaringType.FullName + "." + method.Name + "("
                + string.Join(
                    ",",
                    method.GetParameters()
                        .Select(p => p.ParameterType.FullName + " " + p.Name)
                        .ToArray())
                + ")";
        }
    }
}
