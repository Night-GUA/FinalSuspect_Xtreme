﻿using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using static Il2CppSystem.Net.Http.Headers.Parser;
using static FinalSuspect_Xtreme.Translator;

namespace FinalSuspect_Xtreme;

[HarmonyPatch(typeof(CreditsController))]
public class CreditsControllerPatch
{
    private static List<CreditsController.CreditStruct> GetModCredits()
    {
        var devList = new List<string>()
            {
                //$"<color=#bd262a><size=150%>{GetString("FromChina")}</size></color>",
                //XtremeWave
                $"<size=120%><color={Main.ModColor}>{Main.ModName}</color></size>",
                $"<color=#fffcbe>By</color> <color={Main.TeamColor}>XtremeWave</color>",
                //$"喜 - {GetString("Collaborators")}",
                //$"Slok7565 - {GetString("Collaborators")}",
                //$"Zeyan - {GetString("Collaborators")}",
                //$"玖咪 - {GetString("PullRequester")}",
                //$"杰慕斯 - {GetString("PullRequester")}",
                //$"caaattt - {GetString("Art")}",
                //$"小黄117 - {GetString("Art")}",
                //$"QingFeng - {GetString("PullRequester")}",
                //$"中立小黑 - {GetString("PullRequester")}",
                //$"㍿ - {GetString("Innovation")}",
                //$"Hartex - {GetString("Promotion")}",
                "",
                "",
                //Others
                $"<size=120%>{GetString("Contributors")}</size>",
                $"KARPED1EM - {GetString("Creater")}",
                $"Niko233 - {GetString("Contributor")}",
                //$"Moe - {GetString("Contributor")}",
                //$"ryuk - {GetString("Contributor")}",
                //$"Gurge44 - {GetString("Contributor")}",
                $"Amongus(水木年华) - {GetString("Contributor")}",
                //$"Lonnie - {GetString("Contributor")}",
                $"Yu(Night_瓜) - {GetString("Contributor")}",
                $"天寸梦初 - {GetString("Contributor")}",
                //$"Commandf1(in TONX) - {GetString("Contributor")}",
                //$"SolarFlare(in TONX) - {GetString("Contributor")}",
                //$"Mousse(in TONX) - {GetString("Contributor")}",
            };
        var acList = new List<string>()
            {
                //Mods
                $"{GetString("TownOfHost")}",
                $"{GetString("TownOfNext")}",
                $"{GetString("TownOfHost_Y")}",
                $"{GetString("TownOfHost-TheOtherRoles")}",
                $"{GetString("SuperNewRoles")}",
                $"{GetString("TownOfHostRe-Edited")}",
                $"{GetString("TownOfHostEnhanced")}",
                $"{GetString("TownOfHosEdited_PLUS")}",
                $"{GetString("TownOfHosEdited_Niko")}",
                $"{GetString("To_Hope")}",
                $"{GetString("Project-Lotus")}",

                // Sponsor

                //Discord Server Booster
            };

        var credits = new List<CreditsController.CreditStruct>();

        AddPersonToCredits(devList);
        AddSpcaeToCredits();

        //AddTitleToCredits(GetString("Translator"));
        //AddSpcaeToCredits();

        //AddTitleToCredits(GetString("Acknowledgement"));
        //AddPersonToCredits(acList);
        //AddSpcaeToCredits();

        return credits;

        void AddSpcaeToCredits()
        {
            AddTitleToCredits(string.Empty);
        }
        void AddTitleToCredits(string title)
        {
            credits.Add(new()
            {
                format = "title",
                columns = new[] { title },
            });
        }
        void AddPersonToCredits(List<string> list)
        {
            foreach (var line in list)
            {
                var cols = line.Split(" - ").ToList();
                if (cols.Count < 2) cols.Add(string.Empty);
                credits.Add(new()
                {
                    format = "person",
                    columns = cols.ToArray(),
                });
            }
        }
    }

    [HarmonyPatch(nameof(CreditsController.AddCredit)), HarmonyPrefix]
    public static void AddCreditPrefix(CreditsController __instance, [HarmonyArgument(0)] CreditsController.CreditStruct originalCredit)
    {
        if (originalCredit.columns[0] != "logoImage") return;

        foreach (var credit in GetModCredits())
        {
            __instance.AddCredit(credit);
            __instance.AddFormat(credit.format);
        }
    }
}