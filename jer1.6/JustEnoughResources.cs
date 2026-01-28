using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace JustEnoughResources
{
    [StaticConstructorOnStartup]
    public static class JustEnoughResourcesMod
    {
        private static BuildableDef entDef;
        private static List<Designator> elements;
        private static Designator_Build instance_build;
        private static Designator_Dropdown instance_dropdown;
        private static MethodBase mGetMethod_Replacemant = AccessTools.Method(typeof(JustEnoughResourcesMod), nameof(JustEnoughResourcesMod.CapitalizeFirst_fix_patch_build));

        static JustEnoughResourcesMod()
        {
            var harmony = new Harmony("orbittwz.justenoughresources");
            harmony.Patch(AccessTools.Method(typeof(Designator_Build), nameof(Designator_Build.ProcessInput)),
                prefix: new HarmonyMethod(typeof(JustEnoughResourcesMod), nameof(Designator_Build_Prefix)));
            harmony.Patch(AccessTools.Method(typeof(Designator_Build), nameof(Designator_Build.ProcessInput)),
                transpiler: new HarmonyMethod(typeof(JustEnoughResourcesMod), nameof(Designator_Build_Transpiler)));
            harmony.Patch(AccessTools.Method(typeof(Designator_Dropdown), "SetupFloatMenu"),
                prefix: new HarmonyMethod(typeof(JustEnoughResourcesMod), nameof(Designator_Dropdown_Prefix)));
            harmony.Patch(AccessTools.Method(typeof(Designator_Dropdown), "SetupFloatMenu"),
                transpiler: new HarmonyMethod(typeof(JustEnoughResourcesMod), nameof(Designator_Dropdown_Transpiler)));
            harmony.PatchAll();
        }

        private static void Designator_Build_Prefix(BuildableDef ___entDef, Designator_Build __instance)
        {
            entDef = ___entDef;
            instance_build = __instance;
        }

        private static void Designator_Dropdown_Prefix(List<Designator> ___elements, Designator_Dropdown __instance)
        {
            elements = ___elements;
            instance_dropdown = __instance;
        }

        private static IEnumerable<CodeInstruction> Designator_Build_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            foreach (var code in codes)
                if (code.Calls(((Func<string, string>)GenText.CapitalizeFirst).Method))
                {
                    yield return code;
                    yield return new(OpCodes.Ldloc_S, 4);
                    yield return new(OpCodes.Call, mGetMethod_Replacemant);
                }
                else
                    yield return code;
        }

        private static IEnumerable<CodeInstruction> Designator_Dropdown_Transpiler(IEnumerable<CodeInstruction> instructions) =>
            instructions.MethodReplacer(AccessTools.Constructor(typeof(FloatMenuOption), new Type[] { typeof(string), typeof(Action), typeof(ThingDef) , typeof(ThingStyleDef),
                typeof(bool), typeof(MenuOptionPriority), typeof(Action<Rect>), typeof(Thing), typeof(float) , typeof(Func<Rect, bool>),
                typeof(WorldObject), typeof(bool), typeof(int), typeof(int?) }),
                AccessTools.Method(typeof(JustEnoughResourcesMod), nameof(Constructor_fix)));

        private static FloatMenuOption Constructor_fix(string label, Action action, ThingDef shownItemForIcon, ThingStyleDef thingStyle = null,
            bool forceBasicStyle = false, MenuOptionPriority priority = MenuOptionPriority.Default, Action<Rect> mouseoverGuiAction = null,
            Thing revalidateClickTarget = null, float extraPartWidth = 0f, Func<Rect, bool> extraPartOnGUI = null, WorldObject revalidateWorldClickTarget = null,
            bool playSelectionSound = true, int orderInPriority = 0, int? graphicIndexOverride = null)
        {
            int i;
            for (i = 0; i < elements.Count; i++)
                if (elements[i].LabelCap.ToString().Equals(label))
                    break;
            shownItemForIcon.costList = new List<ThingDefCountClass>();
            shownItemForIcon.costList.AddRange(GetCost(elements[i]));
            var result = new FloatMenuOption(label.CapitalizeFirst_fix_patch_dropdown(shownItemForIcon), action, shownItemForIcon, null, false,
                MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, null);
            return result;
        }

        private static List<ThingDefCountClass> GetCost(Designator des)
        {
            List<ThingDefCountClass> result = new List<ThingDefCountClass>();
            Designator_Place designator_Place = des as Designator_Place;
            if (designator_Place != null)
            {
                BuildableDef placingDef = designator_Place.PlacingDef;
                if (placingDef.CostList != null && placingDef.CostList.Count > 0)
                    foreach (ThingDefCountClass t in placingDef.CostList)
                        result.Add(t);
            }
            return result;
        }

        private static string CapitalizeFirst_fix_patch_build(this string str, ThingDef def)
        {
            if (str.NullOrEmpty())
                return str;
            if (char.IsUpper(str[0]))
                return MakeColor(ColorForItem_build(def), 1, str);
            if (str.Length == 1)
                return MakeColor(ColorForItem_build(def), 1, str).ToUpper();
            int num = str.FirstLetterBetweenTags();
            if (num == 0)
                return MakeColor(ColorForItem_build(def), 1, char.ToUpper(str[num]).ToString() + str.Substring(num + 1));
            return MakeColor(ColorForItem_build(def), 1, str.Substring(0, num) + char.ToUpper(str[num]).ToString() + str.Substring(num + 1));
        }

        private static string CapitalizeFirst_fix_patch_dropdown(this string str, ThingDef def)
        {
            if (str.NullOrEmpty())
                return str;
            if (char.IsUpper(str[0]))
                return MakeColor(ColorForItem_dropdown(def), 1, str);
            if (str.Length == 1)
                return MakeColor(ColorForItem_dropdown(def), 1, str).ToUpper();
            int num = str.FirstLetterBetweenTags();
            if (num == 0)
                return MakeColor(ColorForItem_dropdown(def), 1, char.ToUpper(str[num]).ToString() + str.Substring(num + 1));
            return MakeColor(ColorForItem_dropdown(def), 1, str.Substring(0, num) + char.ToUpper(str[num]).ToString() + str.Substring(num + 1));
        }

        private static int ColorForItem_build(ThingDef def)
        {
            int result0 = 0, result1 = 0, result2 = 0, result4 = 0, materialsMin = 0;
            List<ThingDefCountClass> list = entDef.CostListAdjusted(def, false);
            for (int i = 0; i < list.Count; i++)
            {
                ThingDefCountClass thingDefCountClass = list[i];
                if (thingDefCountClass.thingDef != null && thingDefCountClass.thingDef.resourceReadoutPriority != ResourceCountPriority.Uncounted)
                    if (instance_build.Map.resourceCounter.GetCount(thingDefCountClass.thingDef) == 0)
                        result0++;
                    else if (instance_build.Map.resourceCounter.GetCount(thingDefCountClass.thingDef) < thingDefCountClass.count)
                        result1++;
                    else if (instance_build.Map.resourceCounter.GetCount(thingDefCountClass.thingDef) >= thingDefCountClass.count &&
                        instance_build.Map.resourceCounter.GetCount(thingDefCountClass.thingDef) < thingDefCountClass.count * 2)
                        result2++;
                    else if (instance_build.Map.resourceCounter.GetCount(thingDefCountClass.thingDef) >= thingDefCountClass.count * 2)
                    {
                        result4++;
                        if (materialsMin == 0)
                            materialsMin = Mathf.RoundToInt(instance_build.Map.resourceCounter.GetCount(thingDefCountClass.thingDef) / thingDefCountClass.count);
                        else
                            materialsMin = Mathf.Min(materialsMin, Mathf.RoundToInt(instance_build.Map.resourceCounter.GetCount(thingDefCountClass.thingDef)
                                / thingDefCountClass.count));
                    }
            }
            if (result0 > 0 && result1 == 0 && result2 == 0 && result4 == 0)
                return -1;
            else if (result1 > 0 || (result0 > 0 && result2 > 0) || (result0 > 0 && result4 > 0))
                return 0;
            else if (result2 > 0 && result0 == 0 && result1 == 0)
                return 1;
            else if (result4 > 0 && result0 == 0 && result1 == 0 && result2 == 0)
                return materialsMin;
            return -1;
        }

        private static int ColorForItem_dropdown(ThingDef def)
        {
            int result0 = 0, result1 = 0, result2 = 0, result4 = 0, materialsMin = 0;
            List<ThingDefCountClass> list = def.CostList;
            for (int i = 0; i < list.Count; i++)
            {
                ThingDefCountClass thingDefCountClass = list[i];
                if (thingDefCountClass.thingDef != null && thingDefCountClass.thingDef.resourceReadoutPriority != ResourceCountPriority.Uncounted)
                    if (instance_dropdown.Map.resourceCounter.GetCount(thingDefCountClass.thingDef) == 0)
                        result0++;
                    else if (instance_dropdown.Map.resourceCounter.GetCount(thingDefCountClass.thingDef) < thingDefCountClass.count)
                        result1++;
                    else if (instance_dropdown.Map.resourceCounter.GetCount(thingDefCountClass.thingDef) >= thingDefCountClass.count &&
                        instance_dropdown.Map.resourceCounter.GetCount(thingDefCountClass.thingDef) < thingDefCountClass.count * 2)
                        result2++;
                    else if (instance_dropdown.Map.resourceCounter.GetCount(thingDefCountClass.thingDef) >= thingDefCountClass.count * 2)
                    {
                        result4++;
                        if (materialsMin == 0)
                            materialsMin = Mathf.RoundToInt(instance_dropdown.Map.resourceCounter.GetCount(thingDefCountClass.thingDef) / thingDefCountClass.count);
                        else
                            materialsMin = Mathf.Min(materialsMin, Mathf.RoundToInt(instance_dropdown.Map.resourceCounter.GetCount(thingDefCountClass.thingDef)
                                / thingDefCountClass.count));
                    }
            }
            if (result0 > 0 && result1 == 0 && result2 == 0 && result4 == 0)
                return -1;
            else if (result1 > 0 || (result0 > 0 && result2 > 0) || (result0 > 0 && result4 > 0))
                return 0;
            else if (result2 > 0 && result0 == 0 && result1 == 0)
                return 1;
            else if (result4 > 0 && result0 == 0 && result1 == 0 && result2 == 0)
                return materialsMin;
            return -1;
        }

        private static string MakeColor(int got, int needed, string s)
        {
            if (got >= 2 * needed)
                return $"<color=#{"97B7EF"}>" + s + " x" + got + "</color>";
            return $"<color=#{(got < 0 ? "999999" : got == 0 ? "EAFF00" : got == needed ? "BCF994" : "97B7EF")}>" + s + "</color>";
        }
        // 999999 is gray , EAFF00 is yellow, BCF994 is green, 97B7EF is blue.
        // if there are no resources at all, gray is used.
        // if there are some resources but not all, yellow is used.
        // if there are resources, green is used.
        // if there are more than twice the resources, blue is used and also returning a suffix how many items you can build.
    }
}