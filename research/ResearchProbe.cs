using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        public const string PluginVersion = "0.1.1";

        private const BindingFlags AllInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags AllStatic = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        internal static ResearchProbe Instance;

        private Harmony _harmony;
        private StreamWriter _writer;
        private Type _fishingGuiType;
        private int _waitingGeneration;

        private void Awake()
        {
            Instance = this;

            try
            {
                var path = Path.Combine(Paths.BepInExRootPath, "BetterFishingRodsResearchProbe-0.1.1.log");
                _writer = new StreamWriter(path, false) { AutoFlush = true };

                Write("=== Better Fishing Rods Research Probe 0.1.1 ===");
                Write("GeneratedUtc=" + DateTime.UtcNow.ToString("O"));
                Write("ApplicationVersion=" + Application.version);
                Write("UnityVersion=" + Application.unityVersion);

                _fishingGuiType = AccessTools.TypeByName("FishingGUI");
                if (_fishingGuiType == null)
                {
                    Write("FATAL: FishingGUI type not found.");
                    return;
                }

                Write("FishingGUI.Assembly=" + DescribeAssembly(_fishingGuiType.Assembly));

                _harmony = new Harmony(PluginGuid);
                PatchChangeState();
                PatchThrowingAnimationExit();

                Write("ProbeReady=True");
                Write("Instruction=Perform one normal complete fishing cast and return this log file.");
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

        private void PatchChangeState()
        {
            var postfix = typeof(ResearchProbe).GetMethod(
                nameof(ChangeStatePostfix),
                BindingFlags.Static | BindingFlags.NonPublic);

            foreach (var method in _fishingGuiType.GetMethods(AllInstance)
                .Where(m => m.Name == "ChangeState"))
            {
                try
                {
                    _harmony.Patch(method, postfix: new HarmonyMethod(postfix));
                    Write("PATCH_OK " + FormatMethod(method));
                }
                catch (Exception ex)
                {
                    Write("PATCH_FAIL " + FormatMethod(method) + " :: " + ex);
                }
            }
        }

        private void PatchThrowingAnimationExit()
        {
            var type = AccessTools.TypeByName("FishingThrowingAnim");
            if (type == null)
            {
                Write("PATCH_MISSING FishingThrowingAnim");
                return;
            }

            Write("FishingThrowingAnim.Assembly=" + DescribeAssembly(type.Assembly));

            var prefix = typeof(ResearchProbe).GetMethod(
                nameof(ThrowingOnStateExitPrefix),
                BindingFlags.Static | BindingFlags.NonPublic);
            var postfix = typeof(ResearchProbe).GetMethod(
                nameof(ThrowingOnStateExitPostfix),
                BindingFlags.Static | BindingFlags.NonPublic);

            var methods = type.GetMethods(AllInstance)
                .Where(m => m.Name == "OnStateExit")
                .ToArray();

            if (methods.Length == 0)
            {
                Write("PATCH_MISSING FishingThrowingAnim.OnStateExit");
                return;
            }

            foreach (var method in methods)
            {
                try
                {
                    _harmony.Patch(method, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
                    Write("PATCH_OK " + FormatMethod(method));
                }
                catch (Exception ex)
                {
                    Write("PATCH_FAIL " + FormatMethod(method) + " :: " + ex);
                }
            }
        }

        private static void ChangeStatePostfix(object __instance, object[] __args)
        {
            var self = Instance;
            if (self == null || __instance == null)
                return;

            try
            {
                var target = DescribeArg(__args, 0);
                var state = self.ReadFieldText(__instance, "_state");

                self.Write("EVENT ChangeState POST t=" + Time.realtimeSinceStartup.ToString("F3")
                    + " state=" + state + " target=" + target);

                if (target.IndexOf("WaitingForBite", StringComparison.OrdinalIgnoreCase) >= 0
                    && state.IndexOf("WaitingForBite", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    self._waitingGeneration++;
                    var generation = self._waitingGeneration;

                    self.Write("WAIT_SELECTED gen=" + generation
                        + " t=" + Time.realtimeSinceStartup.ToString("F3")
                        + " wait=" + self.ReadFloatField(__instance, "_waiting_for_bite_delay").ToString("F6")
                        + " can_take_out=" + self.ReadBoolField(__instance, "can_take_out")
                        + " rod=" + self.ReadFieldText(__instance, "_equipped_fishing_rod")
                        + " fish_def=" + self.ReadFieldText(__instance, "_fish_def"));

                    self.StartCoroutine(self.ObserveCountdown(__instance, generation));
                }

                if (target.IndexOf("WaitingForPulling", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    self.Write("BITE_WINDOW t=" + Time.realtimeSinceStartup.ToString("F3")
                        + " waiting_for_pulling_time=" + self.ReadFloatField(__instance, "_waiting_for_pulling_time").ToString("F6")
                        + " fish=" + self.ReadFieldText(__instance, "_fish")
                        + " preset=" + self.ReadFieldText(__instance, "_fish_preset"));
                    self.DumpFishingVisuals("BITE_EVENT");
                }

                if (target.IndexOf("Pulling", StringComparison.OrdinalIgnoreCase) >= 0
                    || target.IndexOf("TakingOut", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    self.Write("STATE_DETAIL target=" + target
                        + " fish=" + self.ReadFieldText(__instance, "_fish")
                        + " rod=" + self.ReadFieldText(__instance, "_equipped_fishing_rod")
                        + " success=" + self.ReadBoolField(__instance, "is_success_fishing"));
                }
            }
            catch (Exception ex)
            {
                self.Write("ERROR ChangeStatePostfix " + ex);
            }
        }

        private static void ThrowingOnStateExitPrefix()
        {
            var self = Instance;
            if (self == null)
                return;

            self.LogThrowingExit("PRE");
        }

        private static void ThrowingOnStateExitPostfix()
        {
            var self = Instance;
            if (self == null)
                return;

            self.LogThrowingExit("POST");
        }

        private void LogThrowingExit(string phase)
        {
            try
            {
                var fishing = GetCurrentFishingGui();

                Write("EVENT FishingThrowingAnim.OnStateExit " + phase
                    + " t=" + Time.realtimeSinceStartup.ToString("F3")
                    + " fishing=" + (fishing == null ? "<null>" : "found")
                    + " can_take_out=" + (fishing == null ? "<n/a>" : ReadBoolField(fishing, "can_take_out").ToString())
                    + " wait=" + (fishing == null ? "<n/a>" : ReadFloatField(fishing, "_waiting_for_bite_delay").ToString("F6")));

                if (phase == "POST" && fishing != null)
                    DumpFishingVisuals("THROWING_ANIM_EXIT");
            }
            catch (Exception ex)
            {
                Write("ERROR ThrowingOnStateExit " + phase + " " + ex);
            }
        }

        private IEnumerator ObserveCountdown(object fishingGui, int generation)
        {
            var guardFrames = 0;

            while (generation == _waitingGeneration
                && IsState(fishingGui, "WaitingForBite")
                && !ReadBoolField(fishingGui, "can_take_out"))
            {
                if (++guardFrames > 3600)
                {
                    Write("COUNTDOWN_ABORT gen=" + generation + " reason=settle-timeout");
                    yield break;
                }

                yield return null;
            }

            if (generation != _waitingGeneration || !IsState(fishingGui, "WaitingForBite"))
            {
                Write("COUNTDOWN_ABORT gen=" + generation + " reason=state-changed-before-start");
                yield break;
            }

            var initial = ReadFloatField(fishingGui, "_waiting_for_bite_delay");
            if (initial <= 0f || float.IsNaN(initial))
            {
                Write("COUNTDOWN_ABORT gen=" + generation + " reason=nonpositive-wait value=" + initial);
                yield break;
            }

            Write("COUNTDOWN_START gen=" + generation
                + " t=" + Time.realtimeSinceStartup.ToString("F3")
                + " wait=" + initial.ToString("F6")
                + " fish_def=" + ReadFieldText(fishingGui, "_fish_def"));
            DumpFishingVisuals("COUNTDOWN_START");

            var thresholds = new[] { 0.75f, 0.50f, 0.25f, 0.10f, 0.03f };
            var next = 0;

            while (generation == _waitingGeneration && IsState(fishingGui, "WaitingForBite"))
            {
                var remaining = ReadFloatField(fishingGui, "_waiting_for_bite_delay");
                var ratio = remaining / initial;

                while (next < thresholds.Length && ratio <= thresholds[next])
                {
                    var label = "COUNTDOWN_" + (thresholds[next] * 100f).ToString("F0") + "_PERCENT_REMAINING";
                    Write(label
                        + " gen=" + generation
                        + " t=" + Time.realtimeSinceStartup.ToString("F3")
                        + " remaining=" + remaining.ToString("F6")
                        + " ratio=" + ratio.ToString("F4"));
                    DumpFishingVisuals(label);
                    next++;
                }

                yield return null;
            }

            Write("COUNTDOWN_END gen=" + generation
                + " t=" + Time.realtimeSinceStartup.ToString("F3")
                + " state=" + ReadFieldText(fishingGui, "_state")
                + " remaining=" + ReadFloatField(fishingGui, "_waiting_for_bite_delay").ToString("F6"));
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

        private bool IsState(object instance, string stateName)
        {
            return ReadFieldText(instance, "_state")
                .IndexOf(stateName, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool ReadBoolField(object instance, string name)
        {
            if (instance == null)
                return false;

            var field = instance.GetType().GetField(name, AllInstance);
            if (field == null)
                return false;

            try { return Convert.ToBoolean(field.GetValue(instance)); }
            catch { return false; }
        }

        private float ReadFloatField(object instance, string name)
        {
            if (instance == null)
                return float.NaN;

            var field = instance.GetType().GetField(name, AllInstance);
            if (field == null)
                return float.NaN;

            try { return Convert.ToSingle(field.GetValue(instance)); }
            catch { return float.NaN; }
        }

        private string ReadFieldText(object instance, string name)
        {
            if (instance == null)
                return "<null-instance>";

            var field = instance.GetType().GetField(name, AllInstance);
            if (field == null)
                return "<missing>";

            try { return DescribeObject(field.GetValue(instance)); }
            catch (Exception ex) { return "<error:" + ex.Message + ">"; }
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

                    foreach (var component in go.GetComponents<Component>())
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
                        details.Add(name + "=" + Convert.ToString(
                            field.GetValue(value),
                            System.Globalization.CultureInfo.InvariantCulture));
                    }
                    catch { }
                }

                var property = type.GetProperty(name, AllInstance);
                if (property != null && property.GetIndexParameters().Length == 0 && property.CanRead)
                {
                    try
                    {
                        details.Add(name + "=" + Convert.ToString(
                            property.GetValue(value, null),
                            System.Globalization.CultureInfo.InvariantCulture));
                    }
                    catch { }
                }
            }

            return type.FullName + (details.Count == 0 ? "" : "{" + string.Join(";", details.ToArray()) + "}");
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
                + string.Join(",", method.GetParameters().Select(
                    p => p.ParameterType.FullName + " " + p.Name).ToArray())
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
    }
}
