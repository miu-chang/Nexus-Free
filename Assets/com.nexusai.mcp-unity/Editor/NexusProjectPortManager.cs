using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

namespace NexusAIConnect
{
    /// <summary>
    /// プロジェクトごとのポート管理システム
    /// 各Unityプロジェクトに固有のポートを割り当て、管理する
    /// </summary>
    [InitializeOnLoad]
    public static class NexusProjectPortManager
    {
        private static string projectId;
        private static int assignedPort = -1;
        private static readonly string MAPPING_FILE_PATH;
        
        // プロジェクト・ポートマッピング情報
        [Serializable]
        public class ProjectPortMapping
        {
            public Dictionary<string, ProjectInfo> projects = new Dictionary<string, ProjectInfo>();
        }
        
        [Serializable]
        public class ProjectInfo
        {
            public string projectName;
            public string projectPath;
            public int port;
            public DateTime lastUpdated;
            public bool isActive;
        }
        
        static NexusProjectPortManager()
        {
            // マッピングファイルのパス（ユーザーホームディレクトリに保存）
            string homeDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
            MAPPING_FILE_PATH = Path.Combine(homeDir, ".config", "nexus", "project_port_mapping.json");
            
            // 初期化
            Initialize();
        }
        
        private static void Initialize()
        {
            // プロジェクトIDを生成（プロジェクトパスのハッシュ値）
            string projectPath = Application.dataPath;
            projectId = GetProjectId(projectPath);
            
            Debug.Log($"[Nexus Port Manager] Project ID: {projectId}");
            Debug.Log($"[Nexus Port Manager] Project Path: {projectPath}");
            
            // ポートを割り当て
            AssignPort();
            
            // 定期的に状態を更新
            EditorApplication.update += UpdateProjectStatus;
            EditorApplication.quitting += OnEditorQuitting;
        }
        
        /// <summary>
        /// プロジェクトIDを生成
        /// </summary>
        private static string GetProjectId(string projectPath)
        {
            // プロジェクトパスからユニークなIDを生成
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(projectPath);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant().Substring(0, 8);
            }
        }
        
        /// <summary>
        /// ポートを割り当て
        /// </summary>
        private static void AssignPort()
        {
            var mapping = LoadMapping();
            
            // 既存のポート割り当てを確認
            if (mapping.projects.ContainsKey(projectId))
            {
                var info = mapping.projects[projectId];
                assignedPort = info.port;
                info.lastUpdated = DateTime.Now;
                info.isActive = true;
                info.projectName = PlayerSettings.productName;
                Debug.Log($"[Nexus Port Manager] Using existing port: {assignedPort}");
            }
            else
            {
                // 新規にポートを割り当て
                assignedPort = FindAvailablePort(mapping);
                mapping.projects[projectId] = new ProjectInfo
                {
                    projectName = PlayerSettings.productName,
                    projectPath = Application.dataPath,
                    port = assignedPort,
                    lastUpdated = DateTime.Now,
                    isActive = true
                };
                Debug.Log($"[Nexus Port Manager] Assigned new port: {assignedPort}");
            }
            
            SaveMapping(mapping);
            
            // NexusEditorMCPServiceにポートを通知
            UpdateMCPServicePort();
        }
        
        /// <summary>
        /// 利用可能なポートを探す
        /// </summary>
        private static int FindAvailablePort(ProjectPortMapping mapping)
        {
            int[] candidatePorts = { 8080, 8081, 8082, 8083, 8084, 8085, 8086, 8087, 8088, 8089 };
            
            foreach (int port in candidatePorts)
            {
                bool isUsed = false;
                foreach (var project in mapping.projects.Values)
                {
                    if (project.port == port && project.isActive)
                    {
                        isUsed = true;
                        break;
                    }
                }
                
                if (!isUsed)
                {
                    return port;
                }
            }
            
            // すべて使用中の場合は8090から順に探す
            return 8090 + mapping.projects.Count;
        }
        
        /// <summary>
        /// MCPサービスにポートを設定
        /// </summary>
        private static void UpdateMCPServicePort()
        {
            if (assignedPort > 0)
            {
                string serverUrl = $"ws://localhost:{assignedPort}";
                NexusEditorMCPService.SetServerUrl(serverUrl);
                Debug.Log($"[Nexus Port Manager] Updated MCP Service URL: {serverUrl}");
            }
        }
        
        /// <summary>
        /// プロジェクトの状態を更新
        /// </summary>
        private static void UpdateProjectStatus()
        {
            // 5分ごとに状態を更新
            if (EditorApplication.timeSinceStartup % 300 < 1)
            {
                var mapping = LoadMapping();
                if (mapping.projects.ContainsKey(projectId))
                {
                    mapping.projects[projectId].lastUpdated = DateTime.Now;
                    mapping.projects[projectId].isActive = true;
                    SaveMapping(mapping);
                }
            }
        }
        
        /// <summary>
        /// エディター終了時の処理
        /// </summary>
        private static void OnEditorQuitting()
        {
            var mapping = LoadMapping();
            if (mapping.projects.ContainsKey(projectId))
            {
                mapping.projects[projectId].isActive = false;
                SaveMapping(mapping);
            }
            
            EditorApplication.update -= UpdateProjectStatus;
        }
        
        /// <summary>
        /// マッピング情報を読み込み
        /// </summary>
        private static ProjectPortMapping LoadMapping()
        {
            try
            {
                if (File.Exists(MAPPING_FILE_PATH))
                {
                    string json = File.ReadAllText(MAPPING_FILE_PATH);
                    
                    // JSON文字列を検証・クリーニング
                    json = json.Trim();
                    if (string.IsNullOrEmpty(json))
                    {
                        Debug.LogWarning("[Nexus Port Manager] Mapping file is empty, creating new mapping.");
                        return new ProjectPortMapping();
                    }
                    
                    // JSON構文チェック
                    if (!json.StartsWith("{") || !json.EndsWith("}"))
                    {
                        Debug.LogError($"[Nexus Port Manager] Invalid JSON format in mapping file. Content: {json.Substring(0, Math.Min(100, json.Length))}...");
                        return new ProjectPortMapping();
                    }
                    
                    var result = JsonConvert.DeserializeObject<ProjectPortMapping>(json);
                    return result ?? new ProjectPortMapping();
                }
            }
            catch (JsonException jsonEx)
            {
                Debug.LogError($"[Nexus Port Manager] JSON parsing error: {jsonEx.Message}");
                Debug.LogError($"[Nexus Port Manager] Recreating mapping file due to corruption.");
                
                // 破損したファイルをバックアップして新しく作成
                try
                {
                    string backupPath = MAPPING_FILE_PATH + ".backup";
                    File.Move(MAPPING_FILE_PATH, backupPath);
                    Debug.Log($"[Nexus Port Manager] Corrupted file backed up to: {backupPath}");
                }
                catch { }
                
                return new ProjectPortMapping();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nexus Port Manager] Failed to load mapping: {e.Message}");
            }
            
            return new ProjectPortMapping();
        }
        
        /// <summary>
        /// マッピング情報を保存
        /// </summary>
        private static void SaveMapping(ProjectPortMapping mapping)
        {
            try
            {
                // 古いエントリーをクリーンアップ（30日以上更新されていないもの）
                var keysToRemove = new List<string>();
                foreach (var kvp in mapping.projects)
                {
                    if ((DateTime.Now - kvp.Value.lastUpdated).TotalDays > 30)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
                
                foreach (var key in keysToRemove)
                {
                    mapping.projects.Remove(key);
                }
                
                // ディレクトリを作成
                string directory = Path.GetDirectoryName(MAPPING_FILE_PATH);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // 保存
                string json = JsonConvert.SerializeObject(mapping, Formatting.Indented);
                File.WriteAllText(MAPPING_FILE_PATH, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nexus Port Manager] Failed to save mapping: {e.Message}");
            }
        }
        
        /// <summary>
        /// 現在のプロジェクトのポートを取得
        /// </summary>
        public static int GetAssignedPort()
        {
            return assignedPort;
        }
        
        /// <summary>
        /// 現在のプロジェクトIDを取得
        /// </summary>
        public static string GetProjectId()
        {
            return projectId;
        }
        
        /// <summary>
        /// マッピング情報を表示（デバッグ用）
        /// </summary>
        // [MenuItem("Nexus/Show Port Mapping")] // NexusMenuManager.cs に移動
        public static void ShowPortMapping()
        {
            var mapping = LoadMapping();
            
            System.Text.StringBuilder info = new System.Text.StringBuilder();
            info.AppendLine("🔌 Nexus Project Port Mapping");
            info.AppendLine("================================");
            info.AppendLine($"Current Project ID: {projectId}");
            info.AppendLine($"Assigned Port: {assignedPort}");
            info.AppendLine("\nAll Projects:");
            
            foreach (var kvp in mapping.projects)
            {
                var project = kvp.Value;
                info.AppendLine($"\n📁 {project.projectName}");
                info.AppendLine($"   ID: {kvp.Key}");
                info.AppendLine($"   Port: {project.port}");
                info.AppendLine($"   Path: {project.projectPath}");
                info.AppendLine($"   Active: {project.isActive}");
                info.AppendLine($"   Last Updated: {project.lastUpdated}");
            }
            
            Debug.Log(info.ToString());
            EditorUtility.DisplayDialog("Port Mapping", info.ToString(), "OK");
        }
    }
}