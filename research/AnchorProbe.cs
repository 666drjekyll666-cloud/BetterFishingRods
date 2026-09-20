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

namespace BetterFishingRodsAnchorResearch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class AnchorProbe : BaseUnityPlugin
    {
        public const string PluginGuid = "nikich.betterfishingrods.anchorprobe";
        public const string PluginName = "Better Fishing Rods Anchor Probe";
        public const string PluginVersion = "0.2.2";

        private const BindingFlags AllInstance =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AllStatic =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private const byte AlphaThreshold = 24;
        private const int EdgeBandPixels = 14;

        internal static AnchorProbe Instance;

        private Harmony _harmony;
        private StreamWriter _writer;
        private Type _fishingGuiType;
        private FieldInfo _stateField;
        private FieldInfo _distanceField;
        private FieldInfo _canTakeOutField;
        private FieldInfo _spotField;
        private readonly HashSet<string> _capturedDistanceSprites =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<int> _completedDistances = new HashSet<int>();
        private int _captureGeneration;

        private void Awake()
        {
            Instance = this;

            try
            {
                var logPath = Path.Combine(
                    Paths.BepInExRootPath,
                    "BetterFishingRodsAnchorProbe-0.2.2.log");

                _writer = new StreamWriter(logPath, false) { AutoFlush = true };

                Write("=== Better Fishing Rods Anchor Probe 0.2.2 ===");
                Write("GeneratedUtc=" + DateTime.UtcNow.ToString("O"));
                Write("ApplicationVersion=" + Application.version);
                Write("UnityVersion=" + Application.unityVersion);

                _fishingGuiType = AccessTools.TypeByName("FishingGUI");
                if (_fishingGuiType == null)
                {
                    Write("FATAL FishingGUI type not found.");
                    return;
                }

                _stateField = _fishingGuiType.GetField("_state", AllInstance);
                _distanceField = _fishingGuiType.GetField("_throwing_distance_int", AllInstance);
                _canTakeOutField = _fishingGuiType.GetField("can_take_out", AllInstance);
                _spotField = _fishingGuiType.GetField("_fishing_spot_wgo", AllInstance);

                if (_stateField == null || _distanceField == null || _canTakeOutField == null)
                {
                    Write("FATAL expected FishingGUI fields are missing.");
                    return;
                }

                _harmony = new Harmony(PluginGuid);
                PatchThrowingAnimationExit();
                PatchChangeState();

                Write("ProbeReady=True");
                Write("GameplayChanges=False");
                Write("Instruction=At one fishing spot, perform one cast at each of the three cast distances. Return this log only.");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                SafeWrite("FATAL Awake: " + ex);
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
                SafeWrite("WARN Unpatch: " + ex.Message);
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

            var postfix = typeof(AnchorProbe).GetMethod(
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

            var postfix = typeof(AnchorProbe).GetMethod(
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
                if (fishing == null
                    || !self.IsState(fishing, "WaitingForBite")
                    || !ReadBool(self._canTakeOutField, fishing))
                    return;

                self._captureGeneration++;
                var generation = self._captureGeneration;
                self.StartCoroutine(self.CaptureWaitingSprites(fishing, generation));
            }
            catch (Exception ex)
            {
                self.Write("ERROR ThrowingOnStateExitPostfix " + ex);
            }
        }

        private static void ChangeStatePostfix(object __instance)
        {
            var self = Instance;
            if (self == null || __instance == null)
                return;

            try
            {
                var state = self.ReadState(__instance);
                if (string.Equals(state, "WaitingForBite", StringComparison.Ordinal))
                    return;

                if (string.Equals(state, "WaitingForPulling", StringComparison.Ordinal)
                    || string.Equals(state, "TakingOut", StringComparison.Ordinal))
                {
                    var distance = ReadInt(self._distanceField, __instance);
                    if (distance > 0)
                    {
                        self._completedDistances.Add(distance);
                        self.Write("DISTANCE_COMPLETE distance=" + distance
                            + " covered=" + string.Join(",", self._completedDistances.OrderBy(x => x).Select(x => x.ToString()).ToArray()));
                    }
                }
            }
            catch (Exception ex)
            {
                self.Write("ERROR ChangeStatePostfix " + ex);
            }
        }

        private IEnumerator CaptureWaitingSprites(object fishingGui, int generation)
        {
            yield return null;

            var distance = ReadInt(_distanceField, fishingGui);
            var isToRight = ReadBoolFieldByName(fishingGui, "is_to_right");
            var spot = DescribeSpot(fishingGui);

            Write("CAST_BEGIN distance=" + distance
                + " is_to_right=" + isToRight
                + " spot=" + spot
                + " playerScale=" + DescribePlayerScale());

            var start = Time.realtimeSinceStartup;
            var asciiWritten = false;

            while (generation == _captureGeneration
                && IsState(fishingGui, "WaitingForBite")
                && Time.realtimeSinceStartup - start < 3.0f)
            {
                var bobber = FindBobber();
                var renderer = bobber == null ? null : bobber.GetComponent<SpriteRenderer>();
                var sprite = renderer == null ? null : renderer.sprite;

                if (bobber != null && renderer != null && sprite != null)
                {
                    var key = distance + "|" + sprite.name;
                    if (_capturedDistanceSprites.Add(key))
                    {
                        DumpBobberTransform(distance, isToRight, bobber, renderer, sprite);
                        DumpSpriteGeometry(distance, sprite, renderer);

                        try
                        {
                            var analysis = AnalyzeSpritePixels(sprite);
                            DumpPixelAnalysis(distance, sprite, analysis, !asciiWritten);
                            asciiWritten = true;
                        }
                        catch (Exception ex)
                        {
                            Write("PIXEL_ANALYSIS_ERROR distance=" + distance
                                + " sprite=" + sprite.name
                                + " " + ex.GetType().Name + ":" + ex.Message);
                        }
                    }
                }

                yield return null;
            }

            Write("CAST_CAPTURE_END distance=" + distance
                + " uniqueSprites=" + _capturedDistanceSprites.Count(k => k.StartsWith(distance + "|", StringComparison.Ordinal))
                + " elapsed=" + (Time.realtimeSinceStartup - start).ToString("F3", CultureInfo.InvariantCulture));
        }

        private void DumpBobberTransform(
            int distance,
            bool isToRight,
            Transform bobber,
            SpriteRenderer renderer,
            Sprite sprite)
        {
            var player = GameObject.Find("Player(Clone)");

            Write("BOBBER_TRANSFORM distance=" + distance
                + " direction=" + (isToRight ? "right" : "left")
                + " sprite=" + sprite.name
                + " path=" + HierarchyPath(bobber)
                + " localPos=" + Vec3(bobber.localPosition)
                + " worldPos=" + Vec3(bobber.position)
                + " localScale=" + Vec3(bobber.localScale)
                + " lossyScale=" + Vec3(bobber.lossyScale)
                + " playerLocalScale=" + (player == null ? "<null>" : Vec3(player.transform.localScale))
                + " rendererBoundsCenter=" + Vec3(renderer.bounds.center)
                + " rendererBoundsSize=" + Vec3(renderer.bounds.size)
                + " flipX=" + renderer.flipX
                + " flipY=" + renderer.flipY);
        }

        private void DumpSpriteGeometry(int distance, Sprite sprite, SpriteRenderer renderer)
        {
            var vertices = sprite.vertices;
            var min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            for (var i = 0; i < vertices.Length; i++)
            {
                min = Vector2.Min(min, vertices[i]);
                max = Vector2.Max(max, vertices[i]);
            }

            Write("SPRITE_GEOMETRY distance=" + distance
                + " sprite=" + sprite.name
                + " rect=" + RectText(sprite.rect)
                + " pivotPx=" + Vec2(sprite.pivot)
                + " ppu=" + sprite.pixelsPerUnit.ToString("F3", CultureInfo.InvariantCulture)
                + " boundsCenter=" + Vec3(sprite.bounds.center)
                + " boundsSize=" + Vec3(sprite.bounds.size)
                + " vertices=" + vertices.Length
                + " vertexMin=" + Vec2(min)
                + " vertexMax=" + Vec2(max)
                + " packed=" + sprite.packed
                + " packingMode=" + sprite.packingMode
                + " packingRotation=" + sprite.packingRotation);
        }

        private PixelAnalysis AnalyzeSpritePixels(Sprite sprite)
        {
            Rect textureRect;
            try
            {
                textureRect = sprite.textureRect;
            }
            catch
            {
                textureRect = sprite.rect;
            }

            var texture = ReadTexture(sprite.texture);
            try
            {
                var x0 = Mathf.Clamp(Mathf.RoundToInt(textureRect.x), 0, texture.width - 1);
                var y0 = Mathf.Clamp(Mathf.RoundToInt(textureRect.y), 0, texture.height - 1);
                var width = Mathf.Clamp(Mathf.RoundToInt(textureRect.width), 1, texture.width - x0);
                var height = Mathf.Clamp(Mathf.RoundToInt(textureRect.height), 1, texture.height - y0);

                var pixels = texture.GetPixels32();
                var mask = new bool[width * height];
                var minX = width;
                var minY = height;
                var maxX = -1;
                var maxY = -1;
                var count = 0;

                for (var y = 0; y < height; y++)
                {
                    var srcY = y0 + y;
                    for (var x = 0; x < width; x++)
                    {
                        var px = pixels[srcY * texture.width + (x0 + x)];
                        if (px.a < AlphaThreshold)
                            continue;

                        mask[y * width + x] = true;
                        count++;
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }

                var components = FindComponents(mask, width, height);
                var left = EdgeCentroid(mask, width, height, minX, Math.Min(maxX, minX + EdgeBandPixels - 1));
                var right = EdgeCentroid(mask, width, height, Math.Max(minX, maxX - EdgeBandPixels + 1), maxX);

                return new PixelAnalysis
                {
                    Width = width,
                    Height = height,
                    Mask = mask,
                    AlphaCount = count,
                    MinX = minX,
                    MinY = minY,
                    MaxX = maxX,
                    MaxY = maxY,
                    LeftEdge = left,
                    RightEdge = right,
                    Components = components
                };
            }
            finally
            {
                Destroy(texture);
            }
        }

        private void DumpPixelAnalysis(
            int distance,
            Sprite sprite,
            PixelAnalysis a,
            bool includeAscii)
        {
            Write("PIXEL_BOUNDS distance=" + distance
                + " sprite=" + sprite.name
                + " crop=" + a.Width + "x" + a.Height
                + " alphaCount=" + a.AlphaCount
                + " alphaBounds=[" + a.MinX + "," + a.MinY + "]-[" + a.MaxX + "," + a.MaxY + "]"
                + " leftEdgeCentroid=" + Vec2(a.LeftEdge)
                + " rightEdgeCentroid=" + Vec2(a.RightEdge));

            var ordered = a.Components
                .OrderByDescending(c => c.Count)
                .Take(12)
                .ToArray();

            for (var i = 0; i < ordered.Length; i++)
            {
                var c = ordered[i];
                Write("COMPONENT distance=" + distance
                    + " sprite=" + sprite.name
                    + " rank=" + (i + 1)
                    + " pixels=" + c.Count
                    + " bounds=[" + c.MinX + "," + c.MinY + "]-[" + c.MaxX + "," + c.MaxY + "]"
                    + " centroid=" + Vec2(c.Centroid));
            }

            var pivot = sprite.pivot;
            var leftDistance = Math.Abs(a.LeftEdge.x - pivot.x);
            var rightDistance = Math.Abs(a.RightEdge.x - pivot.x);
            var candidate = leftDistance >= rightDistance ? a.LeftEdge : a.RightEdge;
            var local = new Vector2(
                (candidate.x - pivot.x) / sprite.pixelsPerUnit,
                (candidate.y - pivot.y) / sprite.pixelsPerUnit);

            Write("OUTER_EDGE_CANDIDATE distance=" + distance
                + " sprite=" + sprite.name
                + " chosen=" + (leftDistance >= rightDistance ? "left" : "right")
                + " pixel=" + Vec2(candidate)
                + " localFromPivot=" + Vec2(local)
                + " status=research-candidate-not-production");

            if (!includeAscii)
                return;

            Write("ALPHA_MAP_BEGIN distance=" + distance + " sprite=" + sprite.name);
            foreach (var line in BuildAsciiMap(a.Mask, a.Width, a.Height, 60, 36))
                Write("MAP " + line);
            Write("ALPHA_MAP_END distance=" + distance + " sprite=" + sprite.name);
        }

        private static Texture2D ReadTexture(Texture2D source)
        {
            if (source == null)
                throw new InvalidOperationException("sprite texture is null");

            if (source.isReadable)
            {
                var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                readable.SetPixels32(source.GetPixels32());
                readable.Apply(false, false);
                return readable;
            }

            var previous = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(
                source.width,
                source.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default);

            try
            {
                Graphics.Blit(source, rt);
                RenderTexture.active = rt;

                var readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
                readable.Apply(false, false);
                return readable;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        private static List<ComponentInfo> FindComponents(bool[] mask, int width, int height)
        {
            var visited = new bool[mask.Length];
            var result = new List<ComponentInfo>();
            var queue = new Queue<int>();

            for (var start = 0; start < mask.Length; start++)
            {
                if (!mask[start] || visited[start])
                    continue;

                visited[start] = true;
                queue.Enqueue(start);

                var count = 0;
                var minX = width;
                var minY = height;
                var maxX = -1;
                var maxY = -1;
                double sumX = 0;
                double sumY = 0;

                while (queue.Count > 0)
                {
                    var index = queue.Dequeue();
                    var y = index / width;
                    var x = index - y * width;

                    count++;
                    sumX += x;
                    sumY += y;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;

                    for (var dy = -1; dy <= 1; dy++)
                    {
                        for (var dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0)
                                continue;

                            var nx = x + dx;
                            var ny = y + dy;
                            if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                                continue;

                            var ni = ny * width + nx;
                            if (!mask[ni] || visited[ni])
                                continue;

                            visited[ni] = true;
                            queue.Enqueue(ni);
                        }
                    }
                }

                result.Add(new ComponentInfo
                {
                    Count = count,
                    MinX = minX,
                    MinY = minY,
                    MaxX = maxX,
                    MaxY = maxY,
                    Centroid = new Vector2(
                        (float)(sumX / count),
                        (float)(sumY / count))
                });
            }

            return result;
        }

        private static Vector2 EdgeCentroid(
            bool[] mask,
            int width,
            int height,
            int xMin,
            int xMax)
        {
            if (xMax < xMin)
                return new Vector2(float.NaN, float.NaN);

            double sumX = 0;
            double sumY = 0;
            var count = 0;

            for (var y = 0; y < height; y++)
            {
                for (var x = xMin; x <= xMax; x++)
                {
                    if (!mask[y * width + x])
                        continue;

                    sumX += x;
                    sumY += y;
                    count++;
                }
            }

            return count == 0
                ? new Vector2(float.NaN, float.NaN)
                : new Vector2((float)(sumX / count), (float)(sumY / count));
        }

        private static IEnumerable<string> BuildAsciiMap(
            bool[] mask,
            int width,
            int height,
            int outWidth,
            int outHeight)
        {
            for (var oy = outHeight - 1; oy >= 0; oy--)
            {
                var chars = new char[outWidth];

                var y0 = oy * height / outHeight;
                var y1 = Math.Max(y0 + 1, (oy + 1) * height / outHeight);

                for (var ox = 0; ox < outWidth; ox++)
                {
                    var x0 = ox * width / outWidth;
                    var x1 = Math.Max(x0 + 1, (ox + 1) * width / outWidth);
                    var total = 0;
                    var active = 0;

                    for (var y = y0; y < y1; y++)
                    {
                        for (var x = x0; x < x1; x++)
                        {
                            total++;
                            if (mask[y * width + x])
                                active++;
                        }
                    }

                    var ratio = total == 0 ? 0f : (float)active / total;
                    chars[ox] = ratio <= 0f ? ' '
                        : ratio < 0.15f ? '.'
                        : ratio < 0.45f ? '+'
                        : ratio < 0.75f ? '*'
                        : '#';
                }

                yield return new string(chars);
            }
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
            return fishingProperty == null ? null : fishingProperty.GetValue(me, null);
        }

        private bool IsState(object fishingGui, string expected)
        {
            return string.Equals(ReadState(fishingGui), expected, StringComparison.Ordinal);
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

        private static string DescribePlayerScale()
        {
            var player = GameObject.Find("Player(Clone)");
            return player == null ? "<null>" : Vec3(player.transform.localScale);
        }

        private static int ReadInt(FieldInfo field, object instance)
        {
            if (field == null || instance == null)
                return -1;

            try { return Convert.ToInt32(field.GetValue(instance)); }
            catch { return -1; }
        }

        private static bool ReadBool(FieldInfo field, object instance)
        {
            if (field == null || instance == null)
                return false;

            try { return Convert.ToBoolean(field.GetValue(instance)); }
            catch { return false; }
        }

        private static bool ReadBoolFieldByName(object instance, string fieldName)
        {
            if (instance == null)
                return false;

            var field = instance.GetType().GetField(fieldName, AllInstance);
            return ReadBool(field, instance);
        }

        private static string Vec2(Vector2 v)
        {
            return "("
                + v.x.ToString("F3", CultureInfo.InvariantCulture) + ","
                + v.y.ToString("F3", CultureInfo.InvariantCulture) + ")";
        }

        private static string Vec3(Vector3 v)
        {
            return "("
                + v.x.ToString("F3", CultureInfo.InvariantCulture) + ","
                + v.y.ToString("F3", CultureInfo.InvariantCulture) + ","
                + v.z.ToString("F3", CultureInfo.InvariantCulture) + ")";
        }

        private static string RectText(Rect r)
        {
            return "("
                + r.x.ToString("F1", CultureInfo.InvariantCulture) + ","
                + r.y.ToString("F1", CultureInfo.InvariantCulture) + ","
                + r.width.ToString("F1", CultureInfo.InvariantCulture) + ","
                + r.height.ToString("F1", CultureInfo.InvariantCulture) + ")";
        }

        private void Write(string message)
        {
            Logger.LogInfo(message);
            if (_writer != null)
                _writer.WriteLine(message);
        }

        private void SafeWrite(string message)
        {
            try { Write(message); } catch { }
        }

        private static string FormatMethod(MethodBase method)
        {
            return method.DeclaringType.FullName + "." + method.Name + "("
                + string.Join(",", method.GetParameters()
                    .Select(p => p.ParameterType.FullName + " " + p.Name)
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

        private sealed class PixelAnalysis
        {
            public int Width;
            public int Height;
            public bool[] Mask;
            public int AlphaCount;
            public int MinX;
            public int MinY;
            public int MaxX;
            public int MaxY;
            public Vector2 LeftEdge;
            public Vector2 RightEdge;
            public List<ComponentInfo> Components;
        }

        private sealed class ComponentInfo
        {
            public int Count;
            public int MinX;
            public int MinY;
            public int MaxX;
            public int MaxY;
            public Vector2 Centroid;
        }
    }
}
