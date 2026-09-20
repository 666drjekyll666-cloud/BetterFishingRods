using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace BetterFishingRodsResearch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class ResearchProbe : BaseUnityPlugin
    {
        public const string PluginGuid = "nikich.betterfishingrods.researchprobe";
        public const string PluginName = "Better Fishing Rods Research Probe";
        public const string PluginVersion = "0.1.0";

        private const BindingFlags AllInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Dictionary<short, OpCode> OpCodesByValue = BuildOpCodeMap();

        internal static ResearchProbe Instance;

        private Harmony _harmony;
        private StreamWriter _writer;
        private Type _fishingGuiType;
        private int _castGeneration;

        private void Awake()
        {
            Instance = this;

            try
            {
                var path = Path.Combine(Paths.BepInExRootPath, "BetterFishingRodsResearchProbe-0.1.0.log");
                _writer = new StreamWriter(path, false);
                _writer.AutoFlush = true;

                Write("=== Better Fishing Rods Research Probe 0.1.0 ===");
                Write("GeneratedUtc=" + DateTime.UtcNow.ToString("O"));
                Write("ApplicationVersion=" + Application.version);
                Write("UnityVersion=" + Application.unityVersion);

                _fishingGuiType = AccessTools.TypeByName("FishingGUI");
                if (_fishingGuiType == null)
                {
                    Write("FATAL: FishingGUI type not found. No patches applied.");
                    return;
                }

                Write("FishingGUI.Assembly=" + DescribeAssembly(_fishingGuiType.Assembly));
                DumpRelevantMethodBodies();

                _harmony = new Harmony(PluginGuid);
                PatchNamedMethods("GetRandomFish", null, nameof(GetRandomFishPostfix));
                PatchNamedMethods("ChangeState", nameof(ChangeStatePrefix), nameof(ChangeStatePostfix));

                Write("ProbeReady=True");
                Write("Instruction=Perform one normal fishing cast and return this log file.");
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

        private void PatchNamedMethods(string methodName, string prefixName, string postfixName)
        {
            var methods = _fishingGuiType.GetMethods(AllInstance)
                .Where(m => string.Equals(m.Name, methodName, StringComparison.Ordinal))
                .ToArray();

            if (methods.Length == 0)
            {
                Write("PATCH_MISSING " + methodName);
                return;
            }

            MethodInfo prefix = null;
            MethodInfo postfix = null;
            if (prefixName != null)
                prefix = typeof(ResearchProbe).GetMethod(prefixName, BindingFlags.Static | BindingFlags.NonPublic);
            if (postfixName != null)
                postfix = typeof(ResearchProbe).GetMethod(postfixName, BindingFlags.Static | BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                try
                {
                    _harmony.Patch(
                        method,
                        prefix == null ? null : new HarmonyMethod(prefix),
                        postfix == null ? null : new HarmonyMethod(postfix));
                    Write("PATCH_OK " + FormatMethod(method));
                }
                catch (Exception ex)
                {
                    Write("PATCH_FAIL " + FormatMethod(method) + " :: " + ex);
                }
            }
        }

        private static void GetRandomFishPostfix(object __instance, object __result, object[] __args)
        {
            var self = Instance;
            if (self == null)
                return;

            try
            {
                self._castGeneration++;
                var generation = self._castGeneration;
                float resolvedWait = -1f;

                if (__args != null && __args.Length > 0 && __args[0] != null)
                    resolvedWait = Convert.ToSingle(__args[0]);

                self.Write("EVENT GetRandomFish gen=" + generation
                    + " t=" + Time.realtimeSinceStartup.ToString("F3")
                    + " resolvedWait=" + resolvedWait.ToString("F3")
                    + " result=" + self.DescribeObject(__result));

                self.DumpFishingFields(__instance, "AFTER_GET_RANDOM_FISH");
                self.DumpFishingVisuals("AFTER_GET_RANDOM_FISH");

                if (resolvedWait > 0f)
                {
                    self.StartCoroutine(self.SnapshotAfter(
                        Math.Max(0f, resolvedWait - 1.0f),
                        generation,
                        __instance,
                        "PRE_BITE_MINUS_1_0S"));

                    self.StartCoroutine(self.SnapshotAfter(
                        Math.Max(0f, resolvedWait - 0.20f),
                        generation,
                        __instance,
                        "PRE_BITE_MINUS_0_20S"));
                }
            }
            catch (Exception ex)
            {
                self.Write("ERROR GetRandomFishPostfix " + ex);
            }
        }

        private static void ChangeStatePrefix(object __instance, object[] __args)
        {
            var self = Instance;
            if (self == null)
                return;

            try
            {
                self.Write("EVENT ChangeState PRE t=" + Time.realtimeSinceStartup.ToString("F3")
                    + " current=" + self.ReadFieldValue(__instance, "_state")
                    + " target=" + DescribeArg(__args, 0));
            }
            catch (Exception ex)
            {
                self.Write("ERROR ChangeStatePrefix " + ex);
            }
        }

        private static void ChangeStatePostfix(object __instance, object[] __args)
        {
            var self = Instance;
            if (self == null)
                return;

            try
            {
                var target = DescribeArg(__args, 0);
                self.Write("EVENT ChangeState POST t=" + Time.realtimeSinceStartup.ToString("F3")
                    + " state=" + self.ReadFieldValue(__instance, "_state")
                    + " target=" + target);

                if (target.IndexOf("WaitingForBite", StringComparison.OrdinalIgnoreCase) >= 0
                    || target.IndexOf("WaitingForPulling", StringComparison.OrdinalIgnoreCase) >= 0
                    || target.IndexOf("Pulling", StringComparison.OrdinalIgnoreCase) >= 0
                    || target.IndexOf("TakingOut", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    self.DumpFishingFields(__instance, "STATE_" + target);
                    self.DumpFishingVisuals("STATE_" + target);
                }
            }
            catch (Exception ex)
            {
                self.Write("ERROR ChangeStatePostfix " + ex);
            }
        }

        private IEnumerator SnapshotAfter(float seconds, int generation, object fishingGui, string label)
        {
            if (seconds > 0f)
                yield return new WaitForSeconds(seconds);

            if (generation != _castGeneration)
                yield break;

            Write("SCHEDULED_SNAPSHOT " + label
                + " gen=" + generation
                + " t=" + Time.realtimeSinceStartup.ToString("F3")
                + " state=" + ReadFieldValue(fishingGui, "_state"));
            DumpFishingFields(fishingGui, label);
            DumpFishingVisuals(label);
        }

        private void DumpFishingFields(object instance, string label)
        {
            if (instance == null)
            {
                Write("FIELDS " + label + " instance=<null>");
                return;
            }

            Write("FIELDS_BEGIN " + label);

            var fields = instance.GetType().GetFields(AllInstance)
                .Where(f =>
                {
                    var n = f.Name.ToLowerInvariant();
                    return n.Contains("fish")
                        || n.Contains("bait")
                        || n.Contains("rod")
                        || n.Contains("wait")
                        || n.Contains("pull")
                        || n.Contains("throw")
                        || n == "_state";
                })
                .OrderBy(f => f.Name)
                .ToArray();

            foreach (var field in fields)
            {
                object value;
                try
                {
                    value = field.GetValue(instance);
                }
                catch (Exception ex)
                {
                    Write("  " + field.Name + "=<read-error:" + ex.Message + ">");
                    continue;
                }

                Write("  " + field.FieldType.FullName + " " + field.Name + "=" + DescribeObject(value));
            }

            Write("FIELDS_END " + label);
        }

        private void DumpFishingVisuals(string label)
        {
            try
            {
                var player = GameObject.Find("Player(Clone)");
                if (player == null)
                {
                    Write("VISUALS " + label + " player=<not-found>");
                    return;
                }

                var interesting = player.GetComponentsInChildren<Transform>(true)
                    .Where(t =>
                    {
                        var n = t.name.ToLowerInvariant();
                        return n == "bobber"
                            || n == "water_fx"
                            || n == "fishing fx"
                            || n.Contains("fishadow")
                            || n.Contains("fish_shadow");
                    })
                    .OrderBy(t => HierarchyPath(t))
                    .ToArray();

                Write("VISUALS_BEGIN " + label + " count=" + interesting.Length);

                foreach (var transform in interesting)
                {
                    var go = transform.gameObject;
                    Write("  GO path=" + HierarchyPath(transform)
                        + " activeSelf=" + go.activeSelf
                        + " activeInHierarchy=" + go.activeInHierarchy
                        + " localPos=" + transform.localPosition
                        + " localScale=" + transform.localScale);

                    var components = go.GetComponents<Component>();
                    foreach (var component in components)
                    {
                        if (component == null)
                            continue;

                        var sr = component as SpriteRenderer;
                        if (sr != null)
                        {
                            Write("    SpriteRenderer enabled=" + sr.enabled
                                + " sprite=" + (sr.sprite == null ? "<null>" : sr.sprite.name)
                                + " color=" + sr.color
                                + " sortingLayer=" + sr.sortingLayerName
                                + " sortingOrder=" + sr.sortingOrder);
                            continue;
                        }

                        var animator = component as Animator;
                        if (animator != null)
                        {
                            Write("    Animator enabled=" + animator.enabled
                                + " layers=" + animator.layerCount
                                + " speed=" + animator.speed);

                            for (var layer = 0; layer < animator.layerCount; layer++)
                            {
                                try
                                {
                                    var state = animator.GetCurrentAnimatorStateInfo(layer);
                                    var clips = animator.GetCurrentAnimatorClipInfo(layer);
                                    Write("      layer=" + layer
                                        + " stateHash=" + state.fullPathHash
                                        + " normalizedTime=" + state.normalizedTime.ToString("F3")
                                        + " clips=" + string.Join(",", clips.Select(c => c.clip == null ? "<null>" : c.clip.name).ToArray()));
                                }
                                catch (Exception ex)
                                {
                                    Write("      animator-layer-error=" + ex.Message);
                                }
                            }
                            continue;
                        }

                        Write("    Component " + component.GetType().FullName);
                    }
                }

                Write("VISUALS_END " + label);
            }
            catch (Exception ex)
            {
                Write("VISUALS_ERROR " + label + " " + ex);
            }
        }

        private void DumpRelevantMethodBodies()
        {
            Write("IL_DUMP_BEGIN");

            var methods = _fishingGuiType.GetMethods(AllInstance)
                .Where(m =>
                {
                    var n = m.Name;
                    return n.IndexOf("Bait", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("Waiting", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("Fish", StringComparison.OrdinalIgnoreCase) >= 0
                        || n == "ChangeState"
                        || n == "UpdateTakingOut";
                })
                .OrderBy(m => m.Name)
                .ThenBy(m => m.GetParameters().Length)
                .ToArray();

            foreach (var method in methods)
                DumpMethodBody(method);

            Write("IL_DUMP_END");
        }

        private void DumpMethodBody(MethodInfo method)
        {
            Write("METHOD " + FormatMethod(method));

            MethodBody body;
            try
            {
                body = method.GetMethodBody();
            }
            catch (Exception ex)
            {
                Write("  BODY_ERROR " + ex.Message);
                return;
            }

            if (body == null)
            {
                Write("  <no-body>");
                return;
            }

            var il = body.GetILAsByteArray();
            if (il == null)
            {
                Write("  <no-il>");
                return;
            }

            var module = method.Module;
            var p = 0;

            while (p < il.Length)
            {
                var offset = p;
                OpCode op;

                var first = il[p++];
                if (first == 0xFE)
                {
                    if (p >= il.Length)
                        break;
                    var key = unchecked((short)(0xFE00 | il[p++]));
                    if (!OpCodesByValue.TryGetValue(key, out op))
                    {
                        Write("  IL_" + offset.ToString("X4") + " <unknown-opcode>");
                        break;
                    }
                }
                else
                {
                    if (!OpCodesByValue.TryGetValue(first, out op))
                    {
                        Write("  IL_" + offset.ToString("X4") + " <unknown-opcode>");
                        break;
                    }
                }

                string operand;
                try
                {
                    operand = ReadOperand(il, ref p, op.OperandType, module);
                }
                catch (Exception ex)
                {
                    operand = "<operand-error:" + ex.Message + ">";
                    p = il.Length;
                }

                Write("  IL_" + offset.ToString("X4") + " " + op.Name + (string.IsNullOrEmpty(operand) ? "" : " " + operand));
            }
        }

        private static string ReadOperand(byte[] il, ref int p, OperandType type, Module module)
        {
            switch (type)
            {
                case OperandType.InlineNone:
                    return "";
                case OperandType.ShortInlineI:
                    return ((sbyte)il[p++]).ToString();
                case OperandType.InlineI:
                    return ReadInt32(il, ref p).ToString();
                case OperandType.InlineI8:
                    return ReadInt64(il, ref p).ToString();
                case OperandType.ShortInlineR:
                    return ReadSingle(il, ref p).ToString("R");
                case OperandType.InlineR:
                    return ReadDouble(il, ref p).ToString("R");
                case OperandType.ShortInlineVar:
                    return il[p++].ToString();
                case OperandType.InlineVar:
                    return ReadUInt16(il, ref p).ToString();
                case OperandType.ShortInlineBrTarget:
                {
                    var delta = (sbyte)il[p++];
                    return "IL_" + (p + delta).ToString("X4");
                }
                case OperandType.InlineBrTarget:
                {
                    var delta = ReadInt32(il, ref p);
                    return "IL_" + (p + delta).ToString("X4");
                }
                case OperandType.InlineSwitch:
                {
                    var count = ReadInt32(il, ref p);
                    var basePos = p + count * 4;
                    var targets = new string[count];
                    for (var i = 0; i < count; i++)
                    {
                        var delta = ReadInt32(il, ref p);
                        targets[i] = "IL_" + (basePos + delta).ToString("X4");
                    }
                    return string.Join(",", targets);
                }
                case OperandType.InlineString:
                {
                    var token = ReadInt32(il, ref p);
                    try { return """ + module.ResolveString(token) + """; }
                    catch { return "string-token=0x" + token.ToString("X8"); }
                }
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineType:
                case OperandType.InlineTok:
                {
                    var token = ReadInt32(il, ref p);
                    try
                    {
                        var member = module.ResolveMember(token);
                        return member == null ? "token=0x" + token.ToString("X8") : member.ToString();
                    }
                    catch
                    {
                        return "token=0x" + token.ToString("X8");
                    }
                }
                case OperandType.InlineSig:
                {
                    var token = ReadInt32(il, ref p);
                    return "sig-token=0x" + token.ToString("X8");
                }
                default:
                    return "<unsupported:" + type + ">";
            }
        }

        private string DescribeObject(object value)
        {
            if (value == null)
                return "<null>";

            var type = value.GetType();

            if (type.IsPrimitive || value is string || type.IsEnum || value is decimal)
                return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);

            var details = new List<string>();
            foreach (var name in new[]
            {
                "id", "item_id", "fish_preset", "value", "durability",
                "catch_time", "zero_progress_fail_time", "efficiency", "tier"
            })
            {
                var field = type.GetField(name, AllInstance);
                if (field != null)
                {
                    try
                    {
                        details.Add(name + "=" + Convert.ToString(field.GetValue(value), System.Globalization.CultureInfo.InvariantCulture));
                    }
                    catch { }
                }

                var property = type.GetProperty(name, AllInstance);
                if (property != null && property.GetIndexParameters().Length == 0 && property.CanRead)
                {
                    try
                    {
                        details.Add(name + "=" + Convert.ToString(property.GetValue(value, null), System.Globalization.CultureInfo.InvariantCulture));
                    }
                    catch { }
                }
            }

            return type.FullName + (details.Count == 0 ? "" : "{" + string.Join(";", details.ToArray()) + "}");
        }

        private object ReadFieldValue(object instance, string fieldName)
        {
            if (instance == null)
                return "<null-instance>";

            var field = instance.GetType().GetField(fieldName, AllInstance);
            if (field == null)
                return "<missing>";

            try
            {
                return DescribeObject(field.GetValue(instance));
            }
            catch (Exception ex)
            {
                return "<error:" + ex.Message + ">";
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
            try { Write(message); } catch { }
        }

        private static string DescribeArg(object[] args, int index)
        {
            if (args == null || index < 0 || index >= args.Length || args[index] == null)
                return "<null>";
            return args[index].ToString();
        }

        private static string DescribeAssembly(Assembly assembly)
        {
            if (assembly == null)
                return "<null>";
            var name = assembly.GetName();
            return name.Name + " " + name.Version + " MVID=" + assembly.ManifestModule.ModuleVersionId;
        }

        private static string FormatMethod(MethodBase method)
        {
            return method.DeclaringType.FullName + "." + method.Name + "("
                + string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName + " " + p.Name).ToArray())
                + ") -> " + (method is MethodInfo ? ((MethodInfo)method).ReturnType.FullName : "System.Void");
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

        private static Dictionary<short, OpCode> BuildOpCodeMap()
        {
            var result = new Dictionary<short, OpCode>();
            foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType != typeof(OpCode))
                    continue;
                var op = (OpCode)field.GetValue(null);
                result[op.Value] = op;
            }
            return result;
        }

        private static int ReadInt32(byte[] data, ref int p)
        {
            var value = BitConverter.ToInt32(data, p);
            p += 4;
            return value;
        }

        private static long ReadInt64(byte[] data, ref int p)
        {
            var value = BitConverter.ToInt64(data, p);
            p += 8;
            return value;
        }

        private static ushort ReadUInt16(byte[] data, ref int p)
        {
            var value = BitConverter.ToUInt16(data, p);
            p += 2;
            return value;
        }

        private static float ReadSingle(byte[] data, ref int p)
        {
            var value = BitConverter.ToSingle(data, p);
            p += 4;
            return value;
        }

        private static double ReadDouble(byte[] data, ref int p)
        {
            var value = BitConverter.ToDouble(data, p);
            p += 8;
            return value;
        }
    }
}
