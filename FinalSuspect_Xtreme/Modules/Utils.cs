using AmongUs.Data;
using AmongUs.GameOptions;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Playables;
using static FinalSuspect_Xtreme.Translator;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace FinalSuspect_Xtreme;

public static class Utils
{
    private static readonly DateTime timeStampStartTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static long TimeStamp => (long)(DateTime.Now.ToUniversalTime() - timeStampStartTime).TotalSeconds;
    public static long GetTimeStamp(DateTime? dateTime = null) => (long)((dateTime ?? DateTime.Now).ToUniversalTime() - timeStampStartTime).TotalSeconds;

    public static Vector2 LocalPlayerLastTp;
    public static bool LocationLocked = false;
    public static ClientData GetClientById(int id)
    {
        try
        {
            var client = AmongUsClient.Instance.allClients.ToArray().Where(cd => cd.Id == id).FirstOrDefault();
            return client;
        }
        catch
        {
            return null;
        }
    }
    public static string GetRoleName(RoleTypes role, bool forUser = true)
    {
        return GetRoleString(Enum.GetName(typeof(RoleTypes), role), forUser);
    }
    public static Color GetRoleColor(RoleTypes role)
    {
        Main.roleColors.TryGetValue(role, out var hexColor);
        _ = ColorUtility.TryParseHtmlString(hexColor, out Color c);
        return c;
    }
    public static string GetRoleColorCode(RoleTypes role)
    {
         Main.roleColors.TryGetValue(role, out var hexColor);
        return hexColor;
    }
    public static string GetRoleInfoForVanilla(this RoleTypes role, bool InfoLong = false)
    {
        if (role is RoleTypes.Crewmate or RoleTypes.Impostor)
            InfoLong = false;

        var text = role.ToString();

        var Info = "Blurb" + (InfoLong ? "Long" : "");

        return GetString($"{text}{Info}");
    }

    public static void KickPlayer(int playerId, bool ban, string reason)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        OnPlayerLeftPatch.Add(playerId);
        //_ = new LateTask(() =>
        //{
        AmongUsClient.Instance.KickPlayer(playerId, ban);
        //}, Math.Max(AmongUsClient.Instance.Ping / 500f, 1f), "Kick Player");

        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetKickReason, SendOption.Reliable, -1);
        writer.Write(GetString($"DCNotify.{reason}"));
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static string PadRightV2(this object text, int num)
    {
        int bc = 0;
        var t = text.ToString();
        foreach (char c in t) bc += Encoding.GetEncoding("UTF-8").GetByteCount(c.ToString()) == 1 ? 1 : 2;
        return t?.PadRight(Mathf.Max(num - (bc - t.Length), 0));
    }
    public static void DumpLog(bool popup = false)
    {
        string f = $"{Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}/FinalSuspect_Xtreme-logs/";
        string t = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
        string filename = $"{f}FinalSuspect_Xtreme-v{Main.ShowVersion}-{t}.log";
        if (!Directory.Exists(f)) Directory.CreateDirectory(f);
        FileInfo file = new(@$"{Environment.CurrentDirectory}/BepInEx/LogOutput.log");
        file.CopyTo(@filename);
        if (PlayerControl.LocalPlayer != null)
        {
            if (popup) PlayerControl.LocalPlayer.ShowPopUp(string.Format(GetString("Message.DumpfileSaved"), $"FinalSuspect_Xtreme - v{Main.ShowVersion}-{t}.log"));
            else AddChatMessage(string.Format(GetString("Message.DumpfileSaved"), $"FinalSuspect_Xtreme - v{Main.ShowVersion}-{t}.log"));
        }
        ProcessStartInfo psi = new ProcessStartInfo("Explorer.exe")
        { Arguments = "/e,/select," + @filename.Replace("/", "\\") };
        Process.Start(psi);
    }
    public static void OpenDirectory(string path)
    {
        var startInfo = new ProcessStartInfo(path)
        {
            UseShellExecute = true,
        };
        Process.Start(startInfo);
    }
    public static string SummaryTexts(byte id)
    {

        var datas = PlayerData.AllPlayerData;
        var thisdata = datas[id];

        var builder = new StringBuilder();
        var longestNameByteCount =
            PlayerData.AllPlayerData.Values.Select(data => data.PlayerName.GetByteCount()).OrderByDescending(byteCount => byteCount).FirstOrDefault();
        var pos = Math.Min(((float)longestNameByteCount / 2) + 1.5f, 11.5f);


        var colorId = thisdata.PlayerColor;
        builder.Append(ColorString(Palette.PlayerColors[colorId], thisdata.PlayerName));
        builder.AppendFormat("<pos={0}em>", pos).Append(GetProgressText(id)).Append("</pos>");

        //pos += 4f;
        pos += DestroyableSingleton<TranslationController>.Instance.currentLanguage.languageID == SupportedLangs.English ? 8f : 4.5f;

        builder.AppendFormat("<pos={0}em>", pos);
        var oldrole = thisdata.roleWhenAlive;
        builder.Append(ColorString(GetRoleColor(oldrole), GetString($"{oldrole}")));
        if (thisdata.Dead && !thisdata.Disconnected)
        {
            var role = thisdata.roleAfterDead;
            if (role != oldrole)
                builder.Append($"=> {ColorString(GetRoleColor(role), GetRoleString($"{role}"))}");
        }
        builder.Append("</pos>");

        return builder.ToString();
    }

    public static string RemoveHtmlTags(this string str) => Regex.Replace(str, "<[^>]*?>", string.Empty);
    public static string RemoveHtmlTagsExcept(this string str, string exceptionLabel) => Regex.Replace(str, "<(?!/*" + exceptionLabel + ")[^>]*?>", string.Empty);
    public static string RemoveColorTags(this string str) => Regex.Replace(str, "</?color(=#[0-9a-fA-F]*)?>", "");
    
    public static Dictionary<string, Sprite> CachedSprites = new();

    public static Sprite LoadSprite(string path, float pixelsPerUnit = 1f)
    {
        try
        {
            if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var sprite)) return sprite;
            Texture2D texture = LoadTextureFromResources(path);
            sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            return CachedSprites[path + pixelsPerUnit] = sprite;
        }
        catch
        {
            Logger.Error($"读入Texture失败：{path}", "LoadImage");
        }
        return null;
    }

    public static Texture2D LoadTextureFromResources(string path)
    {
        try
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            using MemoryStream ms = new();
            stream.CopyTo(ms);
            ImageConversion.LoadImage(texture, ms.ToArray(), false);
            return texture;
        }
        catch
        {
            Logger.Error($"读入Texture失败：{path}", "LoadImage");
        }
        return null;
    }
    public static string ColorString(Color32 color, string str) => $"<color=#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>{str}</color>";
    /// <summary>
    /// Darkness:１の比率で黒色と元の色を混ぜる。マイナスだと白色と混ぜる。
    /// </summary>
    public static Color ShadeColor(this Color color, float Darkness = 0)
    {
        bool IsDarker = Darkness >= 0; //黒と混ぜる
        if (!IsDarker) Darkness = -Darkness;
        float Weight = IsDarker ? 0 : Darkness; //黒/白の比率
        float R = (color.r + Weight) / (Darkness + 1);
        float G = (color.g + Weight) / (Darkness + 1);
        float B = (color.b + Weight) / (Darkness + 1);
        return new Color(R, G, B, color.a);
    }

    /// <summary>
    /// 乱数の簡易的なヒストグラムを取得する関数
    /// <params name="nums">生成した乱数を格納したint配列</params>
    /// <params name="scale">ヒストグラムの倍率 大量の乱数を扱う場合、この値を下げることをお勧めします。</params>
    /// </summary>

    public static bool TryCast<T>(this Il2CppObjectBase obj, out T casted)
    where T : Il2CppObjectBase
    {
        casted = obj.TryCast<T>();
        return casted != null;
    }

    private const string ActiveSettingsSize = "70%";
    private const string ActiveSettingsLineHeight = "55%";

    public static bool AmDev() => IsDev(EOSManager.Instance.FriendCode);
    public static bool IsDev(this PlayerControl pc) => IsDev(pc.FriendCode);
    public static bool IsDev(string friendCode) => friendCode
        is "teamelder#5856" //Slok
        or "canneddrum#2370" //喜
        ;
    public static void AddChatMessage(string text, string title = "")
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = PlayerControl.LocalPlayer;
        if (title == "") title = "<color=#aaaaff>" + GetString("DefaultSystemMessageTitle") + "</color>";
        var name = player.Data.PlayerName;
        player.SetName(title + '\0');
        DestroyableSingleton<HudManager>.Instance?.Chat?.AddChat(player, text);
        player.SetName(name);
    }

    private static Dictionary<byte, PlayerControl> cachedPlayers = new();

    public static PlayerControl GetPlayerById(int playerId) => GetPlayerById((byte)playerId);
    public static PlayerControl GetPlayerById(byte playerId)
    {
        if (cachedPlayers.TryGetValue(playerId, out var cachedPlayer) && cachedPlayer != null)
        {
            return cachedPlayer;
        }
        var player = Main.AllPlayerControls.Where(pc => pc.PlayerId == playerId).FirstOrDefault();
        if (player == null) player = PlayerData.AllPlayerData[playerId].Player;
        cachedPlayers[playerId] = player;
        return player;
    }

    //public static string GetVitalText(byte playerId, bool RealKillerColor = false, bool summary = false)
    //{
    //    var state = PlayerState.GetByPlayerId(playerId);
    //    string deathReason = state.IsDead ? GetString("DeathReason." + state.DeathReason) : (summary ? "" : GetString("Alive"));
    //    if (RealKillerColor)
    //    {
    //        var KillerId = state.GetRealKiller();
    //        Color color = KillerId != byte.MaxValue ? Main.PlayerColors[KillerId] : GetRoleColor(CustomRoles.MedicalExaminer);
    //        if (state.DeathReason is CustomDeathReason.Disconnected or CustomDeathReason.Vote) color = new Color32(255, 255, 255, 60);
    //        deathReason = ColorString(color, deathReason);
    //    }
    //    return deathReason;
    //}

    public static string GetProgressText(PlayerControl pc = null)
    {
        
        var dead = pc.Data.IsDead;

        var enable = !pc.IsImpostor() && (pc == PlayerControl.LocalPlayer ||
                (PlayerControl.LocalPlayer.Data.IsDead && dead && PlayerControl.LocalPlayer.Data.Role.Role is RoleTypes.GuardianAngel) ||
                (PlayerControl.LocalPlayer.Data.IsDead && PlayerControl.LocalPlayer.Data.Role.Role is not RoleTypes.GuardianAngel));


        pc ??= PlayerControl.LocalPlayer;
        var comms = IsActive(SystemTypes.Comms);
        string text = GetProgressText(pc.PlayerId, comms);
        return enable? text : "";
    }
    private static string GetProgressText(byte playerId, bool comms = false)
    {
        var ProgressText = new StringBuilder();
        ProgressText.Append(GetTaskProgressText(playerId, comms));
        return ProgressText.ToString();
    }
    public static string GetTaskProgressText(byte playerId, bool comms = false)
    {
        var state = PlayerData.AllPlayerData[playerId];
        if (state.IsImpostor) return "";
        Color TextColor;
        var TaskCompleteColor = Color.green; //タスク完了後の色
        var NonCompleteColor = Color.yellow; //カウントされない人外は白色

        var NormalColor = state.TotalTaskCount == state.CompleteTaskCount ? TaskCompleteColor : NonCompleteColor;

        TextColor = comms ? Color.gray : NormalColor;
        string Completed = comms ? "?" : $"{state.CompleteTaskCount}";
        return ColorString(TextColor, $"({Completed}/{state.TotalTaskCount})");

    }
    public static bool IsActive(SystemTypes type)
    {
        // ないものはfalse
        if (!ShipStatus.Instance.Systems.ContainsKey(type))
        {
            return false;
        }
        int mapId = Main.NormalOptions.MapId;
        switch (type)
        {
            case SystemTypes.Electrical:
                {
                    var SwitchSystem = ShipStatus.Instance.Systems[type].Cast<SwitchSystem>();
                    return SwitchSystem != null && SwitchSystem.IsActive;
                }
            case SystemTypes.Reactor:
                {
                    if (mapId == 2) return false;
                    else
                    {
                        var ReactorSystemType = ShipStatus.Instance.Systems[type].Cast<ReactorSystemType>();
                        return ReactorSystemType != null && ReactorSystemType.IsActive;
                    }
                }
            case SystemTypes.Laboratory:
                {
                    if (mapId != 2) return false;
                    var ReactorSystemType = ShipStatus.Instance.Systems[type].Cast<ReactorSystemType>();
                    return ReactorSystemType != null && ReactorSystemType.IsActive;
                }
            case SystemTypes.LifeSupp:
                {
                    if (mapId is 2 or 4) return false;
                    var LifeSuppSystemType = ShipStatus.Instance.Systems[type].Cast<LifeSuppSystemType>();
                    return LifeSuppSystemType != null && LifeSuppSystemType.IsActive;
                }
            case SystemTypes.Comms:
                {
                    if (mapId is 1 or 5)
                    {
                        var HqHudSystemType = ShipStatus.Instance.Systems[type].Cast<HqHudSystemType>();
                        return HqHudSystemType != null && HqHudSystemType.IsActive;
                    }
                    else
                    {
                        var HudOverrideSystemType = ShipStatus.Instance.Systems[type].Cast<HudOverrideSystemType>();
                        return HudOverrideSystemType != null && HudOverrideSystemType.IsActive;
                    }
                }
            case SystemTypes.HeliSabotage:
                {
                    var HeliSabotageSystem = ShipStatus.Instance.Systems[type].Cast<HeliSabotageSystem>();
                    return HeliSabotageSystem != null && HeliSabotageSystem.IsActive;
                }
            case SystemTypes.MushroomMixupSabotage:
                {
                    var mushroomMixupSabotageSystem = ShipStatus.Instance.Systems[type].TryCast<MushroomMixupSabotageSystem>();
                    return mushroomMixupSabotageSystem != null && mushroomMixupSabotageSystem.IsActive;
                }
            default:
                return false;
        }
    }

}