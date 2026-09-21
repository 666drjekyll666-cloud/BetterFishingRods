using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BiteCountdownAnchorAuditResearch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class AnchorAuditProbe : BaseUnityPlugin
    {
        public const string PluginGuid = "nikich.bitecountdown.anchorauditprobe";
        public const string PluginName = "Bite Countdown Anchor Audit Probe";
        public const string PluginVersion = "0.3.0";

        private const BindingFlags AllInstance =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AllStatic =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private const int AcceptedOffsetXPixels = 2;
        private const int AcceptedOffsetYPixels = -2;

        internal static AnchorAuditProbe Instance;

        private Harmony _harmony;
        private StreamWriter _writer;
        private Type _fishingGuiType;
        private FieldInfo _stateField;
        private FieldInfo _distanceField;
        private FieldInfo _canTakeOutField;
        private FieldInfo _spotField;
        private int _sample;

        private void Awake()
        {
            Instance = this;

            try
            {
                var logPath = Path.Combine(
                    Paths.BepInExRootPath,
                    "BiteCountdownAnchorAuditProbe-0.3.0.log");

                _writer = new StreamWriter(logPath, false) { AutoFlush = true };

                Write("=== Bite Countdown Anchor Audit Probe 0.3.0 ===");
                Write("GeneratedUtc=" + DateTime.UtcNow.ToString("O"));
                Write("ApplicationVersion=" + Application.version);
                Write("UnityVersion=" + Application.unityVersion);
                Write("ResearchOnly=True GameplayChanges=False SaveWrites=False");

                _fishingGuiType = AccessTools.TypeByName("FishingGUI");
                if (_fishingGuiType == null)
                    throw new InvalidOperationException("FishingGUI type was not found.");

                _stateField = _fishingGuiType.GetField("_state", AllInstance);
                _distanceField = _fishingGuiType.GetField(
                    "_throwing_distance_int",
                    AllInstance);
                _canTakeOutField = _fishingGuiType.GetField(
                    "can_take_out",
                    AllInstance);
                _spotField = _fishingGuiType.GetField(
                    "_fishing_spot_wgo",
                    AllInstance);

                if (_stateField == null || _canTakeOutField == null)
                {
                    throw new InvalidOperationException(
                        "Expected FishingGUI lifecycle fields were not found.");
                }

                PatchThrowingAnimationExit();

                Write("ProbeReady=True");
                Write(
                    "Instruction=Keep Bite Countdown 1.0.1 installed. "
                    + "At each fishing spot, make one cast at the same distance "
                    + "(middle preferred), wait until the countdown ring appears, "
                    + "then move to the next spot. Return this log.");
            }
            catch (Exception ex)
            {
                SafeWrite(
                    "FATAL stage=Awake type=" + ex.GetType().Name
                    + " message=" + Quote(ex.Message));
                Logger.LogError(ex);
            }
        }

        private void OnDestroy()
        {
            try
            {
                if (_harmony != null)
                    _harmony.UnpatchSelf();
            }
            catch (Exception ex)
            {
                SafeWrite(
                    "WARN stage=Unpatch type=" + ex.GetType().Name
                    + " message=" + Quote(ex.Message));
            }

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
                throw new InvalidOperationException(
                    "FishingThrowingAnim type was not found.");
            }

            var target = type.GetMethods(AllInstance)
                .FirstOrDefault(m =>
                    m.DeclaringType == type
                    && m.Name == "OnStateExit"
                    && m.GetParameters().Length == 3);

            if (target == null)
            {
                throw new MissingMethodException(
                    "FishingThrowingAnim.OnStateExit override was not found.");
            }

            var postfix = typeof(AnchorAuditProbe).GetMethod(
                nameof(ThrowingOnStateExitPostfix),
                BindingFlags.Static | BindingFlags.NonPublic);

            _harmony = new Harmony(PluginGuid);
            _harmony.Patch(target, postfix: new HarmonyMethod(postfix));

            Write("PatchOk=" + Quote(FormatMethod(target)));
        }

        private static void ThrowingOnStateExitPostfix()
        {
            var self = Instance;
            if (self == null)
                return;

            try
            {
                var fishing = self.GetCurrentFishingGui();
                if (fishing == null
                    || !self.IsState(fishing, "WaitingForBite")
                    || !ReadBool(self._canTakeOutField, fishing))
                {
                    return;
                }

                self.StartCoroutine(self.CaptureAfterIdleFrame(fishing));
            }
            catch (Exception ex)
            {
                self.SafeWrite(
                    "ERROR stage=ThrowExit type=" + ex.GetType().Name
                    + " message=" + Quote(ex.Message));
            }
        }

        private IEnumerator CaptureAfterIdleFrame(object fishingGui)
        {
            // Production 1.0.1 also waits one frame here so the native idle
            // bobber sprite is active before its mesh center is read.
            yield return null;

            if (!IsState(fishingGui, "WaitingForBite")
                || !ReadBool(_canTakeOutField, fishingGui))
            {
                yield break;
            }

            try
            {
                CaptureSnapshot(fishingGui);
            }
            catch (Exception ex)
            {
                SafeWrite(
                    "ERROR stage=Capture type=" + ex.GetType().Name
                    + " message=" + Quote(ex.Message));
            }
        }

        private void CaptureSnapshot(object fishingGui)
        {
            var player = GameObject.Find("Player(Clone)");
            var bobber = FindBobber(player);
            if (bobber == null)
            {
                Write("ANCHOR_PROBE_ERROR reason=bobber_not_found");
                return;
            }

            var renderer = bobber.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null)
            {
                Write("ANCHOR_PROBE_ERROR reason=bobber_sprite_not_found");
                return;
            }

            var sprite = renderer.sprite;
            var vertices = sprite.vertices;
            if (vertices == null || vertices.Length == 0)
            {
                Write("ANCHOR_PROBE_ERROR reason=sprite_vertices_missing");
                return;
            }

            var min = vertices[0];
            var max = vertices[0];
            for (var i = 1; i < vertices.Length; i++)
            {
                min = Vector2.Min(min, vertices[i]);
                max = Vector2.Max(max, vertices[i]);
            }

            var rawMeshCenter = (min + max) * 0.5f;
            var productionMeshCenter = rawMeshCenter;
            if (renderer.flipX)
                productionMeshCenter.x = -productionMeshCenter.x;
            if (renderer.flipY)
                productionMeshCenter.y = -productionMeshCenter.y;

            var ppu = sprite.pixelsPerUnit;
            var effectivePpu = ppu > 0f ? ppu : 48f;

            var lossy = bobber.lossyScale;
            var xDirection = lossy.x < 0f ? -1f : 1f;
            var yDirection = lossy.y < 0f ? -1f : 1f;

            var productionOffsetLocal = new Vector2(
                AcceptedOffsetXPixels * xDirection / effectivePpu,
                AcceptedOffsetYPixels * yDirection / effectivePpu);

            var productionAnchorLocal =
                productionMeshCenter + productionOffsetLocal;

            var rawMeshWorld = bobber.TransformPoint(
                new Vector3(rawMeshCenter.x, rawMeshCenter.y, 0f));
            var productionMeshWorld = bobber.TransformPoint(
                new Vector3(
                    productionMeshCenter.x,
                    productionMeshCenter.y,
                    0f));
            var productionAnchorWorld = bobber.TransformPoint(
                new Vector3(
                    productionAnchorLocal.x,
                    productionAnchorLocal.y,
                    0f));

            var camera = Camera.main;
            var rawMeshScreen = camera == null
                ? new Vector3(float.NaN, float.NaN, float.NaN)
                : camera.WorldToScreenPoint(rawMeshWorld);
            var productionMeshScreen = camera == null
                ? new Vector3(float.NaN, float.NaN, float.NaN)
                : camera.WorldToScreenPoint(productionMeshWorld);
            var productionAnchorScreen = camera == null
                ? new Vector3(float.NaN, float.NaN, float.NaN)
                : camera.WorldToScreenPoint(productionAnchorWorld);
            var rendererBoundsScreen = camera == null
                ? new Vector3(float.NaN, float.NaN, float.NaN)
                : camera.WorldToScreenPoint(renderer.bounds.center);

            _sample++;

            var scene = SceneManager.GetActiveScene().name;
            var direction = ReadBoolFieldByName(fishingGui, "is_to_right")
                ? "right"
                : "left";
            var distance = ReadInt(_distanceField, fishingGui);
            var spot = DescribeSpot(fishingGui);

            Write(
                "ANCHOR_PROBE"
                + " sample=" + _sample
                + " scene=" + Quote(scene)
                + " spot=" + Quote(spot)
                + " direction=" + direction
                + " distance=" + distance
                + " playerWorld=" + Vec3(player == null
                    ? new Vector3(float.NaN, float.NaN, float.NaN)
                    : player.transform.position)
                + " playerLocalScale=" + Vec3(player == null
                    ? new Vector3(float.NaN, float.NaN, float.NaN)
                    : player.transform.localScale)
                + " playerLossyScale=" + Vec3(player == null
                    ? new Vector3(float.NaN, float.NaN, float.NaN)
                    : player.transform.lossyScale)
                + " bobberPath=" + Quote(HierarchyPath(bobber))
                + " bobberLocal=" + Vec3(bobber.localPosition)
                + " bobberWorld=" + Vec3(bobber.position)
                + " bobberLocalScale=" + Vec3(bobber.localScale)
                + " bobberLossyScale=" + Vec3(lossy)
                + " sprite=" + Quote(sprite.name)
                + " flipX=" + renderer.flipX
                + " flipY=" + renderer.flipY
                + " ppu=" + F(ppu)
                + " vertexMin=" + Vec2(min)
                + " vertexMax=" + Vec2(max)
                + " rawMeshCenterLocal=" + Vec2(rawMeshCenter)
                + " productionMeshCenterLocal=" + Vec2(productionMeshCenter)
                + " productionOffsetLocal=" + Vec2(productionOffsetLocal)
                + " productionAnchorLocal=" + Vec2(productionAnchorLocal)
                + " rawMeshWorld=" + Vec3(rawMeshWorld)
                + " productionMeshWorld=" + Vec3(productionMeshWorld)
                + " productionAnchorWorld=" + Vec3(productionAnchorWorld)
                + " rendererBoundsWorld=" + Vec3(renderer.bounds.center)
                + " rawMeshScreen=" + Vec3(rawMeshScreen)
                + " productionMeshScreen=" + Vec3(productionMeshScreen)
                + " productionAnchorScreen=" + Vec3(productionAnchorScreen)
                + " productionAnchorScreenRounded=" + Vec2(
                    new Vector2(
                        Mathf.Round(productionAnchorScreen.x),
                        Mathf.Round(productionAnchorScreen.y)))
                + " rendererBoundsScreen=" + Vec3(rendererBoundsScreen)
                + " cameraPixelSize=" + (camera == null
                    ? "<null>"
                    : camera.pixelWidth + "x" + camera.pixelHeight)
                + " cameraOrthoSize=" + (camera == null
                    ? "NaN"
                    : F(camera.orthographicSize)));
        }

        private Transform FindBobber(GameObject player)
        {
            if (player == null)
                return null;

            return player.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t =>
                    string.Equals(
                        t.name,
                        "bobber",
                        StringComparison.OrdinalIgnoreCase));
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

        private string DescribeSpot(object fishingGui)
        {
            if (_spotField == null || fishingGui == null)
                return "<missing>";

            var spot = _spotField.GetValue(fishingGui);
            if (spot == null)
                return "<null>";

            var field = spot.GetType().GetField("obj_id", AllInstance);
            if (field != null)
            {
                var value = field.GetValue(spot);
                return value == null ? "<null-id>" : value.ToString();
            }

            var property = spot.GetType().GetProperty("obj_id", AllInstance);
            if (property != null && property.CanRead)
            {
                var value = property.GetValue(spot, null);
                return value == null ? "<null-id>" : value.ToString();
            }

            return spot.ToString();
        }

        private static int ReadInt(FieldInfo field, object instance)
        {
            if (field == null || instance == null)
                return -1;

            try
            {
                return Convert.ToInt32(field.GetValue(instance));
            }
            catch
            {
                return -1;
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

        private static bool ReadBoolFieldByName(
            object instance,
            string fieldName)
        {
            if (instance == null)
                return false;

            var field = instance.GetType().GetField(fieldName, AllInstance);
            return ReadBool(field, instance);
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

        private static string F(float value)
        {
            return value.ToString("F6", CultureInfo.InvariantCulture);
        }

        private static string Vec2(Vector2 value)
        {
            return "(" + F(value.x) + "," + F(value.y) + ")";
        }

        private static string Vec3(Vector3 value)
        {
            return "("
                + F(value.x) + ","
                + F(value.y) + ","
                + F(value.z) + ")";
        }

        private static string Quote(string value)
        {
            if (value == null)
                return "<null>";

            return value
                .Replace(' ', '_')
                .Replace('\t', '_')
                .Replace('\r', '_')
                .Replace('\n', '_');
        }

        private static string FormatMethod(MethodBase method)
        {
            return method.DeclaringType.FullName
                + "."
                + method.Name
                + "("
                + string.Join(
                    ",",
                    method.GetParameters()
                        .Select(p =>
                            p.ParameterType.FullName + " " + p.Name)
                        .ToArray())
                + ")";
        }

        private static string HierarchyPath(Transform transform)
        {
            var names = new List<string>();
            var current = transform;

            while (current != null)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names.ToArray());
        }
    }
}
