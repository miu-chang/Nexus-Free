using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

namespace NexusAIConnect
{
    /// <summary>
    /// MCP Server Setup and Local CLI Management Window
    /// One-touch MCP server construction and AI tool configuration
    /// </summary>
    public class NexusMCPSetupWindow : EditorWindow
    {
        [MenuItem("Nexus/Nexus Setup", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<NexusMCPSetupWindow>("Nexus Setup");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        private NexusMCPSetupManager mcpSetupManager;
        private NexusMCPSetupManager.SetupStatus mcpStatus;
        private Vector2 scrollPosition;
        private bool mcpServerRunning = false;
        
        // Tabs
        private int selectedTab = 0;
        private string[] tabNames = new string[] { "AI Connection", "Help" };
        
        // MCP Configuration
        private int mcpPort = 3000;
        private int wsPort = 8080;
        
        // Animation
        private bool isConnecting = false;
        private float animationTime = 0f;
        private const float CONNECTION_TIMEOUT = 60f; // 60 second timeout
        
        // Configuration state management
        private bool mcpConfigured = false;
        private string[] connectingMessages = new string[] 
        {
            "Preparing AI Connection",
            "Starting MCP Server",
            "Establishing connection with desktop AI apps",
            "Auto-generating configuration files",
            "AI Connection setup almost complete"
        };
        
        private GUIStyle headerStyle;
        private GUIStyle setupButtonStyle;
        private GUIStyle statusStyle;
        
        private async void OnEnable()
        {
            mcpSetupManager = NexusMCPSetupManager.Instance;
            await RefreshStatus();
            
            // Force English tab names (override any cached values)
            tabNames = new string[] { "AI Connection", "Help" };
        }
        
        private void InitializeStyles()
        {
            headerStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.2f, 0.6f, 1f) }
            };
            
            setupButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(20, 20, 10, 10),
                normal = { background = CreateColorTexture(new Color(0.2f, 0.6f, 1f)) },
                hover = { background = CreateColorTexture(new Color(0.3f, 0.7f, 1f)) },
                active = { background = CreateColorTexture(new Color(0.1f, 0.5f, 0.9f)) }
            };
            
            statusStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 14,
                padding = new RectOffset(10, 10, 10, 10),
                wordWrap = true
            };
        }
        
        private Texture2D CreateColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
        
        private void OnGUI()
        {
            if (headerStyle == null)
                InitializeStyles();
            
            // アニメーション更新
            if (isConnecting)
            {
                animationTime += Time.deltaTime;
                Repaint(); // アニメーション用に再描画
            }
            
            DrawHeader();
            
            // タブ描画
            selectedTab = GUILayout.Toolbar(selectedTab, tabNames, GUILayout.Height(30));
            EditorGUILayout.Space(10);
            
            switch (selectedTab)
            {
                case 0:
                    DrawAIConnectionTab();
                    break;
                case 1:
                    DrawHelpTab();
                    break;
            }
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            GUILayout.Label("Nexus MCP Server Setup", headerStyle, GUILayout.Height(40));
            EditorGUILayout.Space(10);
            
            // 簡潔なステータス表示
            if (mcpStatus != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                var statusText = "";
                var statusColor = Color.gray;
                
                if (mcpServerRunning)
                {
                    statusText = "MCP Server Running";
                    statusColor = Color.green;
                }
                else if (mcpStatus.isMCPInstalled)
                {
                    statusText = "AI Connection Ready";
                    statusColor = new Color(0.2f, 0.8f, 0.2f);
                }
                else
                {
                    statusText = "Initial Setup";
                    statusColor = new Color(0.5f, 0.5f, 0.5f);
                }
                
                var oldColor = GUI.contentColor;
                GUI.contentColor = statusColor;
                GUILayout.Label(statusText, EditorStyles.boldLabel);
                GUI.contentColor = oldColor;
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(5);
        }
        
        private void DrawAIConnectionTab()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // ワンクリック起動
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Start AI Connection", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Connect with desktop AI apps like Claude, Gemini, and ChatGPT", EditorStyles.wordWrappedLabel);
            
            EditorGUILayout.Space(10);
            
            // 連携中アニメーション表示
            if (isConnecting)
            {
                DrawConnectingAnimation();
            }
            else if (!mcpServerRunning)
            {
                // 連携前の案内とボタン
                EditorGUILayout.LabelField("Please launch Claude Desktop or ChatGPT first", EditorStyles.helpBox);
                EditorGUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Claude Desktop"))
                {
                    Application.OpenURL("https://claude.ai/download");
                }
                if (GUILayout.Button("ChatGPT Desktop"))
                {
                    Application.OpenURL("https://chatgpt.com/");
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(10);
                
                // 2段階ボタン
                if (!mcpConfigured)
                {
                    // MCP設定ボタン（最初に押す）
                    var oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.2f, 0.6f, 0.8f);
                    if (GUILayout.Button("1. Complete MCP Setup", setupButtonStyle, GUILayout.Height(50)))
                    {
                        ConfigureMCP();
                    }
                    GUI.backgroundColor = oldColor;
                    
                    EditorGUILayout.Space(5);
                    
                    // AI連携ボタン（無効状態）
                    GUI.enabled = false;
                    GUI.backgroundColor = Color.gray;
                    GUILayout.Button("2. Start AI Connection (Complete setup first)", setupButtonStyle, GUILayout.Height(50));
                    GUI.backgroundColor = oldColor;
                    GUI.enabled = true;
                }
                else
                {
                    // MCP設定完了表示と再設定ボタン
                    EditorGUILayout.BeginHorizontal();
                    
                    var oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
                    GUILayout.Button("✓ MCP Setup Complete", setupButtonStyle, GUILayout.Height(40));
                    GUI.backgroundColor = oldColor;
                    
                    // 再設定ボタン
                    GUI.backgroundColor = new Color(0.8f, 0.6f, 0.2f);
                    if (GUILayout.Button("🔄 Reconfigure", GUILayout.Width(80), GUILayout.Height(40)))
                    {
                        ResetMCPConfiguration();
                    }
                    GUI.backgroundColor = oldColor;
                    
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.Space(5);
                    
                    // AI連携ボタン（有効状態）
                    GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
                    if (GUILayout.Button("2. Start AI Connection", setupButtonStyle, GUILayout.Height(60)))
                    {
                        StartAIConnection();
                    }
                    GUI.backgroundColor = oldColor;
                }
            }
            else
            {
                // 停止ボタン
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
                if (GUILayout.Button("Stop AI Connection", setupButtonStyle, GUILayout.Height(60)))
                {
                    StopAIConnection();
                }
                GUI.backgroundColor = oldColor;
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(20);
            
            // 接続状況
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Connection Status", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            var statusColor = mcpServerRunning ? Color.green : Color.red;
            var statusText = mcpServerRunning ? "Connection Ready" : "Stopped";
            
            var oldTextColor = GUI.contentColor;
            GUI.contentColor = statusColor;
            EditorGUILayout.LabelField(statusText, EditorStyles.boldLabel);
            GUI.contentColor = oldTextColor;
            
            if (mcpServerRunning)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Complete Auto-Setup Finished!", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("✅ MCP configuration files auto-generated");
                EditorGUILayout.LabelField("✅ Open Claude Desktop to use Unity MCP tools");
                EditorGUILayout.LabelField("✅ Control Unity with natural language!");
                
                EditorGUILayout.Space(10);
                if (GUILayout.Button("Usage Guide", GUILayout.Height(35)))
                {
                    ShowUsageGuide();
                }
            }
            
            EditorGUILayout.EndVertical();
            
            // 自動生成された接続設定
            if (mcpServerRunning)
            {
                EditorGUILayout.Space(20);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("Connection Settings (Auto-generated)", EditorStyles.boldLabel);
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField("MCP Server: localhost:8080");
                EditorGUILayout.LabelField("Tool Name: unity");
                
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("📖 Usage:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("1. Open ChatGPT or Claude Desktop");
                EditorGUILayout.LabelField("2. Start a new chat");
                EditorGUILayout.LabelField("3. Tips for using tools:");
                EditorGUILayout.LabelField("   • Include words like \"tools\" or \"unity\"");
                EditorGUILayout.LabelField("   Example: \"Create a red cube with Unity tools\"");
                EditorGUILayout.LabelField("4. AI uses MCP tools to control Unity automatically");
                
                EditorGUILayout.EndVertical();
            }
            
            // Pro版の宣伝
            EditorGUILayout.Space(20);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("🚀 Upgrade to Nexus Pro", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Get the complete Unity AI development suite:", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("✅ All 147+ tools - Complete Unity control");
            EditorGUILayout.LabelField("✅ Regular bug fixes and updates");
            EditorGUILayout.LabelField("✅ Advanced features - Animation, Audio, Physics");
            EditorGUILayout.LabelField("✅ Priority support");
            EditorGUILayout.LabelField("✅ Commercial license");
            
            EditorGUILayout.Space(10);
            
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.6f, 0.2f);
            if (GUILayout.Button("🔥 Get Nexus Pro", GUILayout.Height(40)))
            {
                Application.OpenURL("https://assetstore.unity.com/packages/tools/nexus-pro");
            }
            GUI.backgroundColor = oldColor;
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
        }
        
        
        private void DrawServerManagementTab()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("MCP Server Management", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            // サーバー状態
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Server Status:");
            
            var statusColor = mcpServerRunning ? Color.green : Color.red;
            var statusText = mcpServerRunning ? "● Running" : "● Stopped";
            
            var oldColor = GUI.contentColor;
            GUI.contentColor = statusColor;
            GUILayout.Label(statusText);
            GUI.contentColor = oldColor;
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // コントロールボタン
            EditorGUILayout.BeginHorizontal();
            
            EditorGUI.BeginDisabledGroup(!mcpStatus?.isMCPInstalled ?? true);
            
            if (!mcpServerRunning)
            {
                if (GUILayout.Button("▶️ Start Server", GUILayout.Height(30)))
                {
                    StartMCPServer();
                }
            }
            else
            {
                if (GUILayout.Button("⏹️ Stop Server", GUILayout.Height(30)))
                {
                    StopMCPServer();
                }
            }
            
            if (GUILayout.Button("🔄 Restart", GUILayout.Height(30)))
            {
                RestartMCPServer();
            }
            
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(20);
            
            // サーバー設定
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Server Settings:", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("MCP Port:", GUILayout.Width(100));
            mcpPort = EditorGUILayout.IntField(mcpPort);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("WebSocket Port:", GUILayout.Width(100));
            wsPort = EditorGUILayout.IntField(wsPort);
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.indentLevel--;
            
            // 接続情報
            if (mcpStatus?.isMCPInstalled ?? false)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Connection Info:", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"MCP: localhost:{mcpPort}");
                EditorGUILayout.LabelField($"WebSocket: ws://localhost:{wsPort}");
                EditorGUILayout.LabelField($"Accessible from Desktop AI");
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
            
            // ログビューア
            EditorGUILayout.Space(20);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Server Log", EditorStyles.boldLabel);
            
            // ここにサーバーログを表示
            EditorGUILayout.TextArea("Server logs will be displayed here...", GUILayout.Height(200));
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawHelpTab()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("AI Connection Help", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);
            
            // 簡単な説明
            DrawHelpSection("What is Nexus AI Connect?",
                "A tool that connects Unity with desktop AI apps like Claude, Gemini, and ChatGPT.\n" +
                "No complex setup required - get started with just one click.");
            
            // 使い方
            DrawHelpSection("How to Use",
                "1. Click 'Start AI Connection' in the 'AI Connection' tab\n" +
                "2. Open desktop Claude/Gemini/ChatGPT app\n" +
                "3. AI app will auto-connect to localhost:3000\n" +
                "4. Start using AI in Unity!");
            
            // 対応AIアプリ
            DrawHelpSection("Supported AI Apps",
                "• Claude Desktop (Recommended)\n" +
                "• ChatGPT Desktop\n" +
                "• Gemini Desktop\n" +
                "• Other MCP-compatible AI apps");
            
            // トラブルシューティング
            DrawHelpSection("Troubleshooting",
                "• Error when clicking 'Start AI Connection'\n" +
                "  → Launch Unity with administrator privileges\n\n" +
                "• Cannot connect from AI app\n" +
                "  → Check firewall settings\n\n" +
                "• Connection succeeds but cannot control Unity\n" +
                "  → Verify 'Start AI Connection' is activated in Unity");
            
            // リンク
            EditorGUILayout.Space(20);
            if (GUILayout.Button("Download Claude Desktop"))
            {
                Application.OpenURL("https://claude.ai/download");
            }
            
            if (GUILayout.Button("Download ChatGPT Desktop"))
            {
                Application.OpenURL("https://openai.com/chatgpt/download/");
            }
            
            if (GUILayout.Button("Download Gemini Desktop"))
            {
                Application.OpenURL("https://gemini.google.com/app");
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawStatusItem(string label, bool isInstalled, string version)
        {
            EditorGUILayout.BeginHorizontal();
            
            var icon = isInstalled ? "✅" : "❌";
            var color = isInstalled ? Color.green : Color.red;
            
            var oldColor = GUI.contentColor;
            GUI.contentColor = color;
            GUILayout.Label(icon, GUILayout.Width(20));
            GUI.contentColor = oldColor;
            
            EditorGUILayout.LabelField(label, GUILayout.Width(100));
            
            if (!string.IsNullOrEmpty(version))
            {
                EditorGUILayout.LabelField(version, EditorStyles.miniLabel);
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawHelpSection(string title, string content)
        {
            GUILayout.Label(title, EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField(content, EditorStyles.wordWrappedLabel);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(10);
        }
        
        private void DrawConnectingAnimation()
        {
            // アニメーション用のボックス
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // 回転するスピナー
            var spinnerChars = new string[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
            var spinnerIndex = (int)(animationTime * 10) % spinnerChars.Length;
            
            // 現在のメッセージ（ループしないように固定）
            var messageIndex = Mathf.Min((int)(animationTime / 3), connectingMessages.Length - 1);
            var currentMessage = connectingMessages[messageIndex];
            
            // アニメーション表示
            var animatedText = $"{spinnerChars[spinnerIndex]} {currentMessage}...";
            
            var centeredStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16
            };
            
            GUILayout.Label(animatedText, centeredStyle, GUILayout.Height(60));
            
            // プログレスバー（最後のメッセージでは100%にする）
            var progress = (messageIndex + 1f) / connectingMessages.Length;
            var rect = GUILayoutUtility.GetRect(0, 4, GUILayout.ExpandWidth(true));
            EditorGUI.ProgressBar(rect, progress, "");
            
            // キャンセルボタンを追加
            EditorGUILayout.Space(10);
            if (GUILayout.Button("⏹️ Cancel", GUILayout.Height(30)))
            {
                isConnecting = false;
                Repaint();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private async void StartAIConnection()
        {
            try
            {
                
                // アニメーション開始
                isConnecting = true;
                animationTime = 0f;
                Repaint();
                
                // 必要に応じて自動セットアップ
                if (mcpSetupManager != null)
                {
                    var status = await mcpSetupManager.CheckSetupStatus();
                    if (!status.isMCPInstalled)
                    {
                        await mcpSetupManager.RunCompleteSetup();
                    }
                }
                
                // MCPサーバー接続確認（実際にはサーバーは起動せず、既存のサーバーをチェックするだけ）
                mcpServerRunning = await mcpSetupManager.StartMCPServer();
                
                // アニメーション停止
                isConnecting = false;
                
                if (mcpServerRunning)
                {
                    // デスクトップAI用の設定ファイルを自動生成
                    GenerateDesktopAIConfigs();
                    
                    EditorUtility.DisplayDialog("AI Connection Ready", 
                        "Connection completed successfully!\n\n" +
                        "Unity tools are now available in your AI app.\n" +
                        "Type \"tools\" or \"unity\" to start using them.", 
                        "OK");
                }
                else
                {
                    Debug.LogError("[Nexus] ❌ Failed to start AI connection");
                    
                    // MCPサーバーが見つからない場合の詳細なガイド
                    EditorUtility.DisplayDialog(
                        "MCP Server Not Found",
                        "To start AI connection, please launch Claude Desktop first.\n\n" +
                        "Steps:\n" +
                        "1. Launch Claude Desktop app\n" +
                        "2. Wait a moment, then click 'Start AI Connection' again\n\n" +
                        "Note: Unity operates as an MCP server client.",
                        "OK");
                }
                
                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nexus] AI connection error: {e.Message}");
                isConnecting = false; // アニメーション停止
                mcpServerRunning = false;
                Repaint();
            }
        }
        
        private void StopAIConnection()
        {
            mcpServerRunning = false;
            Repaint();
        }
        
        private void ShowUsageGuide()
        {
            EditorUtility.DisplayDialog(
                "How to Use Unity MCP Tools",
                "Tips for Reliable Tool Usage\n\n" +
                "1. Launch ChatGPT or Claude Desktop\n\n" +
                "2. Start a new chat\n\n" + 
                "3. First, ask the AI:\n" +
                "   • \"What tools are available?\"\n" +
                "   • \"What can Unity tools do?\"\n\n" +
                "4. Then give specific instructions:\n" +
                "   • \"Use Unity tools to create a red cube\"\n" +
                "   • \"Add a Player controller using tools\"\n\n" +
                "※ Including words like \"tools\" or \"unity\" helps\n" +
                "  the AI use the tools more effectively!",
                "OK"
            );
        }
        
        private void ShowAIAppsDialog()
        {
            var option = EditorUtility.DisplayDialogComplex(
                "Select Desktop AI App",
                "Which AI app would you like to use?\n\n" +
                "ChatGPT: World's most popular AI\n" +
                "Claude: AI with advanced code understanding\n\n" +
                "※MCP-compatible version required",
                "ChatGPT",
                "Claude Desktop", 
                "Cancel"
            );
            
            switch (option)
            {
                case 0: // ChatGPT
                    Application.OpenURL("https://chatgpt.com/");
                    break;
                case 1: // Claude
                    Application.OpenURL("https://claude.ai/download");
                    break;
            }
        }
        
        private void GenerateDesktopAIConfigs()
        {
            try
            {
                var detectedAIs = DetectInstalledAIs();
                var configuredCount = 0;
                
                foreach (var ai in detectedAIs)
                {
                    switch (ai.ToLower())
                    {
                        case "claude":
                            if (GenerateClaudeConfig()) configuredCount++;
                            break;
                        case "chatgpt":
                            if (GenerateChatGPTConfig()) configuredCount++;
                            break;
                        case "gemini":
                            if (GenerateGeminiConfig()) configuredCount++;
                            break;
                    }
                }
                
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nexus] Config file generation error: {e.Message}");
            }
        }
        
        private string GetMCPServerPath()
        {
            // まずNexus-Free版のパスを確認
            string nexusFreePath = Path.Combine(Application.dataPath, "Nexus-Free", "com.nexusai.mcp-unity", "MCPServer", "index.js");
            if (File.Exists(nexusFreePath))
            {
                Debug.Log($"[Nexus] Found MCP Server in Nexus-Free: {nexusFreePath}");
                return nexusFreePath;
            }
            
            // 次に通常版のパスを確認
            string standardPath = Path.Combine(Application.dataPath, "com.nexusai.mcp-unity", "MCPServer", "index.js");
            if (File.Exists(standardPath))
            {
                Debug.Log($"[Nexus] Found MCP Server in standard location: {standardPath}");
                return standardPath;
            }
            
            // 最後にプロジェクトルートのMCPServerを確認
            string projectRootPath = Path.Combine(Application.dataPath.Replace("/Assets", ""), "MCPServer", "index.js");
            if (File.Exists(projectRootPath))
            {
                Debug.Log($"[Nexus] Found MCP Server in project root: {projectRootPath}");
                return projectRootPath;
            }
            
            Debug.LogError("[Nexus] MCP Server not found in any expected location!");
            return projectRootPath; // デフォルトとして返す
        }

        private List<string> DetectInstalledAIs()
        {
            var installedAIs = new List<string>();
            
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                // macOS アプリケーション検出
                var appsDir = "/Applications";
                if (Directory.Exists($"{appsDir}/Claude.app")) installedAIs.Add("Claude");
                if (Directory.Exists($"{appsDir}/ChatGPT.app")) installedAIs.Add("ChatGPT");
                if (Directory.Exists($"{appsDir}/Gemini.app")) installedAIs.Add("Gemini");
                
                // Homebrew cask インストール検出
                var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (Directory.Exists($"{homeDir}/Applications/Claude.app")) installedAIs.Add("Claude");
                if (Directory.Exists($"{homeDir}/Applications/ChatGPT.app")) installedAIs.Add("ChatGPT");
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                // Windows インストール検出
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                
                if (Directory.Exists($"{programFiles}/Claude") || Directory.Exists($"{localAppData}/Claude")) 
                    installedAIs.Add("Claude");
                if (Directory.Exists($"{programFiles}/ChatGPT") || Directory.Exists($"{localAppData}/ChatGPT")) 
                    installedAIs.Add("ChatGPT");
            }
            
            return installedAIs.Distinct().ToList();
        }
        
        private bool GenerateClaudeConfig()
        {
            try
            {
                var claudeConfigDir = DetectClaudeConfigPath();
                if (string.IsNullOrEmpty(claudeConfigDir))
                {
                    return false;
                }
            
            if (!Directory.Exists(claudeConfigDir))
            {
                Directory.CreateDirectory(claudeConfigDir);
            }
            
            var configPath = Path.Combine(claudeConfigDir, "claude_desktop_config.json");
            
            // 既存設定を読み込み
            dynamic existingConfig = null;
            if (File.Exists(configPath))
            {
                try
                {
                    var existingJson = File.ReadAllText(configPath);
                    existingConfig = JsonConvert.DeserializeObject(existingJson);
                }
                catch (Exception e)
                {
                }
            }
            
            // Unity MCPサーバー設定 - Nexus-Free版のパスを検出
            string mcpServerPath = GetMCPServerPath();
            var unityMcpServer = new
            {
                command = "node",
                args = new[] { mcpServerPath },
                env = new { }
            };
            
            // 既存設定とマージ
            dynamic claudeConfig;
            if (existingConfig?.mcpServers != null)
            {
                // 既存のmcpServersを保持してunityサーバーを追加
                var mcpServers = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    JsonConvert.SerializeObject(existingConfig.mcpServers));
                mcpServers["unity"] = unityMcpServer;
                
                claudeConfig = new
                {
                    mcpServers = mcpServers
                };
                
                // 他の既存設定も保持
                var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    JsonConvert.SerializeObject(existingConfig));
                configDict["mcpServers"] = mcpServers;
                claudeConfig = configDict;
            }
            else
            {
                // 新規作成
                claudeConfig = new
                {
                    mcpServers = new Dictionary<string, object>
                    {
                        ["unity"] = unityMcpServer
                    }
                };
            }
            
            File.WriteAllText(configPath, JsonConvert.SerializeObject(claudeConfig, Newtonsoft.Json.Formatting.Indented));
            
            return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nexus] Claude config error: {e.Message}");
                return false;
            }
        }
        
        private string DetectClaudeConfigPath()
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                // macOS Claude設定パス候補
                var candidates = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Claude"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "com.anthropic.claudefordesktop"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "claude")
                };
                
                foreach (var path in candidates)
                {
                    var parentDir = Path.GetDirectoryName(path);
                    if (Directory.Exists(parentDir))
                    {
                        return path;
                    }
                }
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Claude");
            }
            
            return null;
        }
        
        private bool GenerateGeminiConfig()
        {
            try
            {
                var geminiConfigDir = DetectGeminiConfigPath();
                if (string.IsNullOrEmpty(geminiConfigDir))
                {
                    return false;
                }
            
            if (!Directory.Exists(geminiConfigDir))
            {
                Directory.CreateDirectory(geminiConfigDir);
            }
            
            var geminiConfig = new
            {
                servers = new[]
                {
                    new
                    {
                        name = "unity",
                        url = "http://localhost:3000",
                        type = "mcp"
                    }
                }
            };
            
            var configPath = Path.Combine(geminiConfigDir, "config.json");
            File.WriteAllText(configPath, JsonConvert.SerializeObject(geminiConfig, Newtonsoft.Json.Formatting.Indented));
            
            return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nexus] Gemini config error: {e.Message}");
                return false;
            }
        }
        
        private string DetectGeminiConfigPath()
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                var candidates = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "gemini"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Gemini")
                };
                
                foreach (var path in candidates)
                {
                    var parentDir = Path.GetDirectoryName(path);
                    if (Directory.Exists(parentDir))
                    {
                        return path;
                    }
                }
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Gemini");
            }
            
            return null;
        }
        
        private bool GenerateChatGPTConfig()
        {
            try
            {
                var chatgptConfigDir = DetectChatGPTConfigPath();
                if (string.IsNullOrEmpty(chatgptConfigDir))
                {
                    return false;
                }
            
            if (!Directory.Exists(chatgptConfigDir))
            {
                Directory.CreateDirectory(chatgptConfigDir);
            }
            
            var configPath = Path.Combine(chatgptConfigDir, "config.json");
            
            // 既存設定を読み込み
            dynamic existingConfig = null;
            if (File.Exists(configPath))
            {
                try
                {
                    var existingJson = File.ReadAllText(configPath);
                    existingConfig = JsonConvert.DeserializeObject(existingJson);
                }
                catch (Exception e)
                {
                }
            }
            
            // Unity MCPサーバー設定 - Nexus-Free版のパスを検出
            string mcpServerPath = GetMCPServerPath();
            var unityMcpServer = new
            {
                command = "node",
                args = new[] { mcpServerPath },
                env = new { }
            };
            
            // 既存設定とマージ
            dynamic chatgptConfig;
            if (existingConfig?.mcpServers != null)
            {
                // 既存のmcpServersを保持してunityサーバーを追加
                var mcpServers = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    JsonConvert.SerializeObject(existingConfig.mcpServers));
                mcpServers["unity"] = unityMcpServer;
                
                // 他の既存設定も保持
                var configDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    JsonConvert.SerializeObject(existingConfig));
                configDict["mcpServers"] = mcpServers;
                chatgptConfig = configDict;
            }
            else
            {
                // 新規作成
                chatgptConfig = new
                {
                    mcpServers = new Dictionary<string, object>
                    {
                        ["unity"] = unityMcpServer
                    }
                };
            }
            
            File.WriteAllText(configPath, JsonConvert.SerializeObject(chatgptConfig, Newtonsoft.Json.Formatting.Indented));
            
            return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nexus] ChatGPT config error: {e.Message}");
                return false;
            }
        }
        
        private string DetectChatGPTConfigPath()
        {
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                var candidates = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "com.openai.chat"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "ChatGPT"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "chatgpt")
                };
                
                foreach (var path in candidates)
                {
                    var parentDir = Path.GetDirectoryName(path);
                    if (Directory.Exists(parentDir))
                    {
                        return path;
                    }
                }
            }
            else if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ChatGPT");
            }
            
            return null;
        }
        
        private async Task RefreshStatus()
        {
            mcpStatus = await mcpSetupManager.CheckSetupStatus();
            Repaint();
        }
        
        private async void CheckMCPStatus()
        {
            await RefreshStatus();
        }
        
        private void SaveMCPSettings()
        {
            // .envファイルに保存
            var envPath = Path.Combine(Application.dataPath.Replace("/Assets", ""), "MCPServer", ".env");
            var envContent = new List<string>
            {
                $"PORT={mcpPort}",
                $"WS_PORT={wsPort}"
            };
            
            System.IO.File.WriteAllLines(envPath, envContent);
        }
        
        
        private async void StartMCPServer()
        {
            mcpServerRunning = await mcpSetupManager.StartMCPServer();
            Repaint();
        }
        
        private void StopMCPServer()
        {
            // サーバー停止の実装
            mcpServerRunning = false;
            Repaint();
        }
        
        private async void RestartMCPServer()
        {
            StopMCPServer();
            await Task.Delay(1000);
            await Task.Run(() => StartMCPServer());
        }
        
        private void ConfigureMCP()
        {
            try
            {
                
                // MCP設定ファイルの生成
                GenerateDesktopAIConfigs();
                
                // MCPサーバーの初期化
                if (mcpSetupManager == null)
                {
                    mcpSetupManager = NexusMCPSetupManager.Instance;
                }
                
                // 設定完了フラグを立てる
                mcpConfigured = true;
                
                
                EditorUtility.DisplayDialog(
                    "MCP Setup Complete",
                    "MCP setup has been completed successfully.\n\n" +
                    "⚠️ Important: To apply the settings, please close and restart any running AI apps (ChatGPT Desktop, Claude Desktop, etc.).\n\n" +
                    "After restarting, the 'Start AI Connection' button will become available.",
                    "OK"
                );
                
                Repaint();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nexus] MCP setup error: {e.Message}");
                EditorUtility.DisplayDialog(
                    "MCP Setup Error",
                    $"An error occurred during MCP setup:\n{e.Message}",
                    "OK"
                );
            }
        }
        
        private void ResetMCPConfiguration()
        {
            var confirmed = EditorUtility.DisplayDialog(
                "Reset MCP Configuration",
                "Would you like to reset the MCP configuration and reconfigure?\n\n" +
                "Current AI connection will also be stopped.",
                "Reset",
                "Cancel"
            );
            
            if (confirmed)
            {
                // AI連携を停止
                if (mcpServerRunning)
                {
                    StopAIConnection();
                }
                
                // 設定フラグをリセット
                mcpConfigured = false;
                mcpSetupManager = null;
                
                
                EditorUtility.DisplayDialog(
                    "Configuration Reset Complete",
                    "MCP configuration has been reset.\n" +
                    "Please reconfigure using the '1. Complete MCP Setup' button.",
                    "OK"
                );
                
                Repaint();
            }
        }
    }
}