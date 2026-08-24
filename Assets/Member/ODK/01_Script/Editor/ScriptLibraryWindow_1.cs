using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[Serializable]
public class ScriptBundleManifest
{
    public string[] files;
}

public class ScriptLibraryWindow : EditorWindow
{
    const string PrefsKey = "ScriptLibrary.RootPath";

    string libraryRoot;
    Vector2 scroll;
    List<string> files = new List<string>();
    List<string> bundleNames = new List<string>();
    HashSet<string> selected = new HashSet<string>();
    string searchText = "";
    UnityEngine.Object registerTarget;
    string registerSubfolder = "";
    string newBundleName = "";
    DefaultAsset targetFolder;
    bool autoNamespace = true;

    [MenuItem("Tools/Script Library/Open Window")]
    public static void Open()
    {
        GetWindow<ScriptLibraryWindow>("Script Library");
    }

    void OnEnable()
    {
        libraryRoot = EditorPrefs.GetString(PrefsKey, DefaultRoot());
        if (!Directory.Exists(libraryRoot))
            Directory.CreateDirectory(libraryRoot);
        Refresh();
    }

    static string DefaultRoot()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "UnityScriptLibrary");
    }

    void Refresh()
    {
        files = Directory.GetFiles(libraryRoot, "*.cs", SearchOption.AllDirectories).OrderBy(f => f).ToList();
        selected.RemoveWhere(f => !files.Contains(f));
        var bundlesDir = Path.Combine(libraryRoot, "_Bundles");
        bundleNames.Clear();
        if (Directory.Exists(bundlesDir))
            bundleNames = Directory.GetFiles(bundlesDir, "*.json").Select(Path.GetFileNameWithoutExtension).OrderBy(n => n).ToList();
    }

    static string ExtractClassName(string content)
    {
        var m = Regex.Match(content, @"(?:class|struct|interface)\s+([A-Za-z_][A-Za-z0-9_]*)");
        return m.Success ? m.Groups[1].Value : null;
    }

    static string ExtractNamespace(string content)
    {
        var m = Regex.Match(content, @"namespace\s+([A-Za-z0-9_.]+)\s*(\{|;)");
        return m.Success ? m.Groups[1].Value : null;
    }

    string DetectTargetNamespace(string destFolderAssetPath)
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        var destFolderFull = Path.Combine(projectRoot, destFolderAssetPath);

        if (Directory.Exists(destFolderFull))
        {
            var siblingNamespaces = new HashSet<string>();
            foreach (var f in Directory.GetFiles(destFolderFull, "*.cs", SearchOption.TopDirectoryOnly))
            {
                var ns = ExtractNamespace(File.ReadAllText(f));
                if (!string.IsNullOrEmpty(ns))
                    siblingNamespaces.Add(ns);
            }
            if (siblingNamespaces.Count == 1)
                return siblingNamespaces.First();
        }

        var assetsRoot = Application.dataPath;
        var dir = destFolderFull;
        while (!string.IsNullOrEmpty(dir) && dir.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase))
        {
            if (Directory.Exists(dir))
            {
                var asmdefs = Directory.GetFiles(dir, "*.asmdef");
                if (asmdefs.Length > 0)
                {
                    var json = File.ReadAllText(asmdefs[0]);
                    var m = Regex.Match(json, "\"rootNamespace\"\\s*:\\s*\"([^\"]*)\"");
                    if (m.Success && !string.IsNullOrEmpty(m.Groups[1].Value))
                        return m.Groups[1].Value;
                }
            }
            if (string.Equals(dir, assetsRoot, StringComparison.OrdinalIgnoreCase))
                break;
            var parent = Directory.GetParent(dir);
            dir = parent != null ? parent.FullName : null;
        }
        return null;
    }

    static string RewriteNamespace(string content, string targetNamespace)
    {
        var match = Regex.Match(content, @"namespace\s+([A-Za-z0-9_.]+)\s*(\{|;)");
        if (match.Success)
        {
            var current = match.Groups[1].Value;
            if (current == targetNamespace)
                return content;
            return content.Substring(0, match.Groups[1].Index) + targetNamespace + content.Substring(match.Groups[1].Index + match.Groups[1].Length);
        }

        var normalized = content.Replace("\r\n", "\n");
        var lines = normalized.Split('\n');
        int headerEnd = 0;
        while (headerEnd < lines.Length && (lines[headerEnd].TrimStart().StartsWith("using ") || string.IsNullOrWhiteSpace(lines[headerEnd])))
            headerEnd++;

        var header = string.Join("\n", lines.Take(headerEnd));
        var body = string.Join("\n", lines.Skip(headerEnd));
        var indented = string.Join("\n", body.Split('\n').Select(l => string.IsNullOrWhiteSpace(l) ? l : "\t" + l));

        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(header))
            sb.Append(header).Append("\n\n");
        sb.Append("namespace ").Append(targetNamespace).Append("\n{\n");
        sb.Append(indented);
        if (!indented.EndsWith("\n"))
            sb.Append("\n");
        sb.Append("}\n");
        return sb.ToString();
    }

    bool HasDuplicateClass(string content, string destFullPath, out string conflictRelPath)
    {
        conflictRelPath = null;
        var className = ExtractClassName(content);
        if (string.IsNullOrEmpty(className))
            return false;
        var destFull = Path.GetFullPath(destFullPath);
        foreach (var f in Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories))
        {
            if (string.Equals(Path.GetFullPath(f), destFull, StringComparison.OrdinalIgnoreCase))
                continue;
            var c = File.ReadAllText(f);
            if (ExtractClassName(c) == className)
            {
                conflictRelPath = "Assets" + f.Substring(Application.dataPath.Length).Replace('\\', '/');
                return true;
            }
        }
        return false;
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("라이브러리 폴더", libraryRoot);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("폴더 변경"))
        {
            var picked = EditorUtility.OpenFolderPanel("Script Library Root", libraryRoot, "");
            if (!string.IsNullOrEmpty(picked))
            {
                libraryRoot = picked;
                EditorPrefs.SetString(PrefsKey, libraryRoot);
                Refresh();
            }
        }
        if (GUILayout.Button("폴더 열기"))
            EditorUtility.RevealInFinder(libraryRoot);
        if (GUILayout.Button("새로고침"))
            Refresh();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("스크립트 등록", EditorStyles.boldLabel);
        registerTarget = EditorGUILayout.ObjectField("등록할 스크립트", registerTarget, typeof(MonoScript), false);
        registerSubfolder = EditorGUILayout.TextField("하위 폴더(선택)", registerSubfolder);
        if (GUILayout.Button("라이브러리에 등록") && registerTarget is MonoScript ms)
            RegisterScript(ms, registerSubfolder);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("번들 등록", EditorStyles.boldLabel);
        newBundleName = EditorGUILayout.TextField("번들 이름", newBundleName);
        if (GUILayout.Button("Project에서 선택한 스크립트로 번들 만들기"))
            RegisterBundleFromSelection(newBundleName);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("등록된 번들", EditorStyles.boldLabel);
        foreach (var b in bundleNames)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(b);
            if (GUILayout.Button("프로젝트에 추가", GUILayout.Width(110)))
                AddBundleToProject(b);
            if (GUILayout.Button("삭제", GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("삭제", "번들 " + b + " 을(를) 삭제할까요? (파일 자체는 남습니다)", "삭제", "취소"))
                {
                    File.Delete(Path.Combine(libraryRoot, "_Bundles", b + ".json"));
                    Refresh();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("추가할 위치", EditorStyles.boldLabel);
        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("대상 폴더", targetFolder, typeof(DefaultAsset), false);
        if (GUILayout.Button("현재 선택된 폴더 사용"))
        {
            var sel = Selection.activeObject as DefaultAsset;
            if (sel != null && Directory.Exists(AssetDatabase.GetAssetPath(sel)))
                targetFolder = sel;
            else
                EditorUtility.DisplayDialog("알림", "Project 창에서 폴더를 선택한 상태에서 눌러주세요.", "확인");
        }
        autoNamespace = EditorGUILayout.ToggleLeft("네임스페이스 자동 맞추기 (주변 스크립트 / asmdef 기준)", autoNamespace);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("등록된 스크립트", EditorStyles.boldLabel);
        searchText = EditorGUILayout.TextField("검색", searchText);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("선택 항목 프로젝트에 추가"))
            foreach (var f in selected.ToList())
                AddToProject(f);
        if (GUILayout.Button("선택 해제"))
            selected.Clear();
        EditorGUILayout.EndHorizontal();

        var filtered = string.IsNullOrEmpty(searchText)
            ? files
            : files.Where(f => f.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var f in filtered)
        {
            var rel = f.Substring(libraryRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            EditorGUILayout.BeginHorizontal();
            bool isSel = selected.Contains(f);
            bool newSel = EditorGUILayout.Toggle(isSel, GUILayout.Width(20));
            if (newSel != isSel)
            {
                if (newSel) selected.Add(f);
                else selected.Remove(f);
            }
            EditorGUILayout.LabelField(rel);
            if (GUILayout.Button("추가", GUILayout.Width(60)))
                AddToProject(f);
            if (GUILayout.Button("동기화", GUILayout.Width(60)))
                SyncFromProject(f);
            if (GUILayout.Button("삭제", GUILayout.Width(50)))
            {
                if (EditorUtility.DisplayDialog("삭제", rel + " 를 라이브러리에서 삭제할까요?", "삭제", "취소"))
                {
                    File.Delete(f);
                    Refresh();
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    void RegisterScript(MonoScript script, string subfolder)
    {
        var assetPath = AssetDatabase.GetAssetPath(script);
        var fullSource = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        var destDir = string.IsNullOrEmpty(subfolder) ? libraryRoot : Path.Combine(libraryRoot, subfolder);
        Directory.CreateDirectory(destDir);
        var destPath = Path.Combine(destDir, Path.GetFileName(fullSource));
        if (File.Exists(destPath) && !EditorUtility.DisplayDialog("덮어쓰기", Path.GetFileName(destPath) + " 가 이미 라이브러리에 있습니다. 덮어쓸까요?", "덮어쓰기", "취소"))
            return;
        File.Copy(fullSource, destPath, true);
        Refresh();
    }

    void RegisterBundleFromSelection(string bundleName)
    {
        if (string.IsNullOrEmpty(bundleName))
        {
            EditorUtility.DisplayDialog("알림", "번들 이름을 입력하세요.", "확인");
            return;
        }
        var scripts = Selection.objects.OfType<MonoScript>().ToList();
        if (scripts.Count == 0)
        {
            EditorUtility.DisplayDialog("알림", "Project 창에서 스크립트를 먼저 선택하세요.", "확인");
            return;
        }
        var bundleDir = Path.Combine(libraryRoot, bundleName);
        Directory.CreateDirectory(bundleDir);
        var relPaths = new List<string>();
        foreach (var ms in scripts)
        {
            var assetPath = AssetDatabase.GetAssetPath(ms);
            var fullSource = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
            var fileName = Path.GetFileName(fullSource);
            var dest = Path.Combine(bundleDir, fileName);
            File.Copy(fullSource, dest, true);
            relPaths.Add(bundleName + "/" + fileName);
        }
        var bundlesDir = Path.Combine(libraryRoot, "_Bundles");
        Directory.CreateDirectory(bundlesDir);
        var manifestPath = Path.Combine(bundlesDir, bundleName + ".json");
        File.WriteAllText(manifestPath, JsonUtility.ToJson(new ScriptBundleManifest { files = relPaths.ToArray() }, true));
        Refresh();
    }

    void AddBundleToProject(string bundleName)
    {
        var manifestPath = Path.Combine(libraryRoot, "_Bundles", bundleName + ".json");
        if (!File.Exists(manifestPath))
            return;
        var manifest = JsonUtility.FromJson<ScriptBundleManifest>(File.ReadAllText(manifestPath));
        foreach (var rel in manifest.files)
            AddToProject(Path.Combine(libraryRoot, rel));
    }

    void AddToProject(string sourceFullPath)
    {
        string destFolderAssetPath = targetFolder != null ? AssetDatabase.GetAssetPath(targetFolder) : "Assets";
        if (!Directory.Exists(destFolderAssetPath))
            destFolderAssetPath = "Assets";
        var fileName = Path.GetFileName(sourceFullPath);
        var destProjectRelative = destFolderAssetPath + "/" + fileName;
        var destFull = Path.Combine(Directory.GetParent(Application.dataPath).FullName, destProjectRelative);

        var content = File.ReadAllText(sourceFullPath);

        if (autoNamespace)
        {
            var targetNamespace = DetectTargetNamespace(destFolderAssetPath);
            if (!string.IsNullOrEmpty(targetNamespace))
                content = RewriteNamespace(content, targetNamespace);
        }

        if (HasDuplicateClass(content, destFull, out var conflictRelPath))
        {
            if (!EditorUtility.DisplayDialog("클래스명 중복", "이미 같은 이름의 클래스가 존재합니다:\n" + conflictRelPath + "\n\n그래도 추가할까요?", "추가", "취소"))
                return;
        }

        if (File.Exists(destFull) && !EditorUtility.DisplayDialog("덮어쓰기", destProjectRelative + " 가 이미 있습니다. 덮어쓸까요?", "덮어쓰기", "취소"))
            return;

        File.WriteAllText(destFull, content);
        AssetDatabase.ImportAsset(destProjectRelative, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
    }

    void SyncFromProject(string libraryFile)
    {
        var fileName = Path.GetFileName(libraryFile);
        var matches = Directory.GetFiles(Application.dataPath, fileName, SearchOption.AllDirectories);
        if (matches.Length == 0)
        {
            EditorUtility.DisplayDialog("동기화", "현재 프로젝트에서 동일한 파일명을 찾을 수 없습니다.", "확인");
            return;
        }
        if (matches.Length == 1)
        {
            DoSync(libraryFile, matches[0]);
            return;
        }
        var menu = new GenericMenu();
        foreach (var m in matches)
        {
            var relM = "Assets" + m.Substring(Application.dataPath.Length).Replace('\\', '/');
            var captured = m;
            menu.AddItem(new GUIContent(relM), false, () => DoSync(libraryFile, captured));
        }
        menu.ShowAsContext();
    }

    void DoSync(string libraryFile, string projectFile)
    {
        var libContent = File.ReadAllText(libraryFile);
        var projContent = File.ReadAllText(projectFile);
        if (libContent == projContent)
        {
            EditorUtility.DisplayDialog("동기화", "이미 최신 상태입니다.", "확인");
            return;
        }
        if (EditorUtility.DisplayDialog("동기화", "프로젝트의 최신 내용으로 라이브러리를 업데이트할까요?", "업데이트", "취소"))
        {
            File.Copy(projectFile, libraryFile, true);
            Refresh();
        }
    }

    [MenuItem("Assets/Script Library/선택한 스크립트 등록")]
    static void RegisterSelectedFromContext()
    {
        var root = EditorPrefs.GetString(PrefsKey, DefaultRoot());
        Directory.CreateDirectory(root);
        foreach (var obj in Selection.objects)
        {
            if (obj is MonoScript ms)
            {
                var assetPath = AssetDatabase.GetAssetPath(ms);
                var fullSource = Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
                var destPath = Path.Combine(root, Path.GetFileName(fullSource));
                File.Copy(fullSource, destPath, true);
            }
        }
        var win = GetWindow<ScriptLibraryWindow>("Script Library", false);
        win.Refresh();
    }

    [MenuItem("Assets/Script Library/선택한 스크립트 등록", true)]
    static bool ValidateRegisterSelectedFromContext()
    {
        return Selection.objects.Any(o => o is MonoScript);
    }
}
