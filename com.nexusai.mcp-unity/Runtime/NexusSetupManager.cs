using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace NexusAIConnect
{
    /// <summary>
    /// MCPサーバーのワンタッチセットアップマネージャー
    /// Git、Node.js、npmの自動インストールと各種AI設定まで行う
    /// </summary>
    public class NexusMCPSetupManager
    {
        private static NexusMCPSetupManager instance;
        private static int mcpServerProcessId = -1;
        public static NexusMCPSetupManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new NexusMCPSetupManager();
                return instance;
            }
        }

        private string projectPath;
        private string mcpServerPath;
        private string toolsPath;
        
        public class SetupStatus
        {
            public bool isGitInstalled;
            public bool isNodeInstalled;
            public bool isNpmInstalled;
            public bool isMCPInstalled;
            public bool isConfigured;
            public string gitVersion;
            public string nodeVersion;
            public string npmVersion;
            public List<string> installedTools = new List<string>();
            public Dictionary<string, bool> aiConfigurations = new Dictionary<string, bool>();
        }

        private SetupStatus currentStatus = new SetupStatus();

        private NexusMCPSetupManager()
        {
            projectPath = Application.dataPath.Replace("/Assets", "");
            mcpServerPath = Path.Combine(projectPath, "MCPServer");
            toolsPath = Path.Combine(projectPath, "Tools");
        }

        /// <summary>
        /// セットアップ状態をチェック
        /// </summary>
        public async Task<SetupStatus> CheckSetupStatus()
        {
            currentStatus = new SetupStatus();
            
            // Git チェック
            currentStatus.gitVersion = await CheckCommand("git", "--version");
            currentStatus.isGitInstalled = !string.IsNullOrEmpty(currentStatus.gitVersion);
            
            // Node.js チェック
            currentStatus.nodeVersion = await CheckCommand("node", "--version");
            currentStatus.isNodeInstalled = !string.IsNullOrEmpty(currentStatus.nodeVersion);
            
            // npm チェック
            currentStatus.npmVersion = await CheckCommand("npm", "--version");
            currentStatus.isNpmInstalled = !string.IsNullOrEmpty(currentStatus.npmVersion);
            
            // MCPサーバーディレクトリチェック
            currentStatus.isMCPInstalled = Directory.Exists(mcpServerPath) && 
                                         File.Exists(Path.Combine(mcpServerPath, "package.json"));
            
            // AI設定チェック
            CheckAIConfigurations();
            
            return currentStatus;
        }

        /// <summary>
        /// ワンタッチセットアップ実行
        /// </summary>
        public async Task<bool> RunCompleteSetup(Action<string> progressCallback = null)
        {
            try
            {
                progressCallback?.Invoke("セットアップを開始します...");
                
                // 1. 必要なツールのインストール
                if (!currentStatus.isGitInstalled)
                {
                    progressCallback?.Invoke("Git をインストールしています...");
                    await InstallGit();
                }
                
                if (!currentStatus.isNodeInstalled || !currentStatus.isNpmInstalled)
                {
                    progressCallback?.Invoke("Node.js と npm をインストールしています...");
                    await InstallNodeJS();
                }
                
                // 2. MCPサーバーのセットアップ
                progressCallback?.Invoke("MCPサーバーを構築しています...");
                await SetupMCPServer();
                
                // 3. 依存関係のインストール
                progressCallback?.Invoke("依存関係をインストールしています...");
                await InstallDependencies();
                
                // 4. Unity連携ツールのインストール
                progressCallback?.Invoke("Unity連携ツールをセットアップしています...");
                await SetupUnityTools();
                
                // 5. AI設定
                progressCallback?.Invoke("AI設定を構成しています...");
                await ConfigureAIServices();
                
                // 6. 設定ファイルの生成
                progressCallback?.Invoke("設定ファイルを生成しています...");
                await GenerateConfigFiles();
                
                progressCallback?.Invoke("セットアップが完了しました！");
                
                // 最終状態をチェック
                await CheckSetupStatus();
                
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MCP Setup] セットアップエラー: {e.Message}");
                progressCallback?.Invoke($"エラー: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gitの自動インストール
        /// </summary>
        private Task InstallGit()
        {
            return Task.Run(async () =>
            {
                var platform = Application.platform;
                
                if (platform == RuntimePlatform.OSXEditor)
                {
                    // macOS - Homebrew経由でインストール
                    var hasHomebrew = await CheckCommand("brew", "--version");
                if (string.IsNullOrEmpty(hasHomebrew))
                {
                    // Homebrewのインストールはユーザーに委ねる
                    EditorUtility.DisplayDialog(
                        "Homebrewが必要です",
                        "Homebrewがインストールされていません。\nhttps://brew.sh からインストールしてください。",
                        "OK"
                    );
                    Application.OpenURL("https://brew.sh");
                    throw new Exception("Homebrewがインストールされていません");
                }
                
                await RunCommand("brew", "install git");
            }
            else if (platform == RuntimePlatform.WindowsEditor)
            {
                // Windows - Git for Windowsをダウンロード
                var gitInstallerPath = Path.Combine(toolsPath, "GitInstaller.exe");
                
                if (!Directory.Exists(toolsPath))
                    Directory.CreateDirectory(toolsPath);
                
                // Git for Windowsをダウンロード
                // 注: GitのダウンロードページはHTMLなので、直接ダウンロードURLを使用
                using (var client = new System.Net.WebClient())
                {
                    // 最新の2.51.0バージョンを使用
                    var gitDownloadUrl = "https://github.com/git-for-windows/git/releases/download/v2.51.0.windows.1/Git-2.51.0-64-bit.exe";
                    await client.DownloadFileTaskAsync(gitDownloadUrl, gitInstallerPath);
                }
                
                // サイレントインストール
                await RunCommand(gitInstallerPath, "/VERYSILENT /NORESTART");
            }
            
                // パスを更新
                RefreshEnvironmentPath();
            });
        }

        /// <summary>
        /// Node.jsの自動インストール
        /// </summary>
        private Task InstallNodeJS()
        {
            return Task.Run(async () =>
            {
                var platform = Application.platform;
            
            if (platform == RuntimePlatform.OSXEditor)
            {
                // macOS - Homebrew経由
                await RunCommand("brew", "install node");
            }
            else if (platform == RuntimePlatform.WindowsEditor)
            {
                // Windows - Node.jsインストーラーをダウンロード
                var nodeInstallerPath = Path.Combine(toolsPath, "NodeInstaller.msi");
                
                if (!Directory.Exists(toolsPath))
                    Directory.CreateDirectory(toolsPath);
                
                // Node.js LTS v22.11.0をダウンロード
                using (var client = new System.Net.WebClient())
                {
                    await client.DownloadFileTaskAsync(
                        "https://nodejs.org/dist/v22.11.0/node-v22.11.0-x64.msi",
                        nodeInstallerPath
                    );
                }
                
                // サイレントインストール
                await RunCommand("msiexec", $"/i \"{nodeInstallerPath}\" /qn");
            }
            
                RefreshEnvironmentPath();
            });
        }

        /// <summary>
        /// MCPサーバーのセットアップ
        /// </summary>
        private Task SetupMCPServer()
        {
            return Task.Run(() =>
            {
                // MCPサーバーディレクトリを作成
            if (!Directory.Exists(mcpServerPath))
            {
                Directory.CreateDirectory(mcpServerPath);
            }
            
            // package.jsonを生成（ESモジュール対応）
            var packageJson = @"{
  ""name"": ""unity-mcp-server"",
  ""version"": ""1.0.0"",
  ""description"": ""MCP Server for Unity Integration"",
  ""main"": ""index.js"",
  ""type"": ""module"",
  ""scripts"": {
    ""start"": ""node index.js"",
    ""dev"": ""nodemon index.js""
  },
  ""dependencies"": {
    ""@modelcontextprotocol/sdk"": ""^1.18.1"",
    ""express"": ""^4.18.2"",
    ""ws"": ""^8.13.0"",
    ""cors"": ""^2.8.5"",
    ""dotenv"": ""^16.0.3""
  },
  ""devDependencies"": {
    ""nodemon"": ""^3.0.1""
  }
}";
            
            File.WriteAllText(Path.Combine(mcpServerPath, "package.json"), packageJson);
            
            // MCPサーバーのメインファイルを生成（ESモジュール対応）
            var serverCode = @"import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import express from 'express';
import cors from 'cors';
import { WebSocketServer } from 'ws';
import dotenv from 'dotenv';

dotenv.config();

const app = express();
app.use(cors());
app.use(express.json());

// MCPサーバーの初期化
const server = new Server(
    {
        name: 'unity-mcp-server',
        version: '1.0.0',
    },
    {
        capabilities: {
            tools: {},
        },
    }
);

// Unity操作ツール
server.setRequestHandler('tools/list', async () => {
    return {
        tools: [
            {
                name: 'unity_create',
                description: 'Create Unity GameObjects and components',
                inputSchema: {
                    type: 'object',
                    properties: {
                        objectType: { type: 'string' },
                        name: { type: 'string' },
                        position: { 
                            type: 'object',
                            properties: {
                                x: { type: 'number' },
                                y: { type: 'number' },
                                z: { type: 'number' }
                            }
                        }
                    },
                },
            },
        ],
    };
});

server.setRequestHandler('tools/call', async (request) => {
    if (request.params.name === 'unity_create') {
        return await sendUnityCommand('create', request.params.arguments);
    }
    throw new Error(`Unknown tool: ${request.params.name}`);
});

// WebSocketサーバー
const wss = new WebSocketServer({ port: 8080 });

wss.on('connection', (ws) => {
    console.log('Unity client connected');
    
    ws.on('message', (message) => {
        console.log('Received from Unity:', message.toString());
    });
});

async function sendUnityCommand(command, params) {
    // Unityクライアントに送信
    const message = JSON.stringify({ command, params });
    wss.clients.forEach(client => {
        if (client.readyState === WebSocket.OPEN) {
            client.send(message);
        }
    });
    return { success: true };
}

// HTTPエンドポイント
app.post('/api/chat', async (req, res) => {
    try {
        const { message } = req.body;
        // 簡単なエコーレスポンス
        res.json({ response: `Unity MCP Server received: ${message}` });
    } catch (error) {
        res.status(500).json({ error: error.message });
    }
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
    console.log(`Unity MCP Server running on port ${PORT}`);
    console.log(`WebSocket server running on port 8080`);
});

// MCP サーバーをStdio経由で起動
async function runMCPServer() {
    const transport = new StdioServerTransport();
    await server.connect(transport);
    console.log('MCP Server connected via stdio');
}

// 両方のサーバーを起動
runMCPServer().catch(console.error);
";
            
            File.WriteAllText(Path.Combine(mcpServerPath, "index.js"), serverCode);
            
            // .envファイルを生成
            var envContent = @"PORT=3000
CLAUDE_API_KEY=
GEMINI_API_KEY=
OPENAI_API_KEY=
";
            
                File.WriteAllText(Path.Combine(mcpServerPath, ".env"), envContent);
            });
        }

        /// <summary>
        /// 依存関係のインストール
        /// </summary>
        private async Task InstallDependencies()
        {
            // MCPサーバーディレクトリで npm install を実行
            await RunCommand("npm", "install", mcpServerPath);
            
            // MCP関連ツールのインストール
            // Claude CLIは公式にはnpmパッケージとして存在しないのでコメントアウト
            // await RunCommand("npm", "install -g claude-cli");
        }

        /// <summary>
        /// Unity連携ツールのセットアップ
        /// </summary>
        private async Task SetupUnityTools()
        {
            // Unity WebSocketクライアントを生成
            var clientCode = @"using System;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json;

namespace NexusAIConnect.MCP
{
    public class MCPUnityClient : MonoBehaviour
    {
        private WebSocket ws;
        private string serverUrl = ""ws://localhost:8080"";
        private Queue<MCPCommand> commandQueue = new Queue<MCPCommand>();
        
        [Serializable]
        public class MCPCommand
        {
            public string command;
            public Dictionary<string, object> parameters;
        }
        
        void Start()
        {
            ConnectToMCPServer();
        }
        
        void ConnectToMCPServer()
        {
            ws = new WebSocket(serverUrl);
            
            ws.OnOpen += (sender, e) => {
                Debug.Log(""[MCP Client] Connected to MCP Server"");
            };
            
            ws.OnMessage += (sender, e) => {
                var command = JsonConvert.DeserializeObject<MCPCommand>(e.Data);
                commandQueue.Enqueue(command);
            };
            
            ws.OnError += (sender, e) => {
                Debug.LogError($""[MCP Client] WebSocket error: {e.Message}"");
            };
            
            ws.Connect();
        }
        
        void Update()
        {
            while (commandQueue.Count > 0)
            {
                var command = commandQueue.Dequeue();
                ExecuteCommand(command);
            }
        }
        
        void ExecuteCommand(MCPCommand command)
        {
            switch (command.command)
            {
                case ""create"":
                    CreateGameObject(command.parameters);
                    break;
                // 他のコマンドを追加
            }
        }
        
        void CreateGameObject(Dictionary<string, object> parameters)
        {
            var name = parameters.ContainsKey(""name"") ? parameters[""name""].ToString() : ""GameObject"";
            var go = new GameObject(name);
            
            if (parameters.ContainsKey(""position""))
            {
                // 位置を設定
            }
            
            Debug.Log($""[MCP Client] Created GameObject: {name}"");
        }
        
        void OnDestroy()
        {
            if (ws != null && ws.IsAlive)
            {
                ws.Close();
            }
        }
    }
}";
            
            var clientPath = Path.Combine(Application.dataPath, "Nexus", "Scripts", "MCP", "MCPUnityClient.cs");
            var mcpDir = Path.GetDirectoryName(clientPath);
            
            if (!Directory.Exists(mcpDir))
            {
                Directory.CreateDirectory(mcpDir);
            }
            
            File.WriteAllText(clientPath, clientCode);
            
            // 必要なパッケージをインストール
            await InstallUnityPackage("com.endel.nativewebsocket", "1.1.4");
            await InstallUnityPackage("com.unity.nuget.newtonsoft-json", "3.2.1");
        }

        /// <summary>
        /// AI設定の自動構成
        /// </summary>
        private async Task ConfigureAIServices()
        {
            var configPath = Path.Combine(mcpServerPath, "ai-config");
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }
            
            // Claude設定
            var claudeConfig = @"{
  ""provider"": ""anthropic"",
  ""model"": ""claude-3-opus-20240229"",
  ""temperature"": 0.7,
  ""max_tokens"": 4096,
  ""tools"": [""unity_create"", ""unity_modify"", ""unity_delete""]
}";
            File.WriteAllText(Path.Combine(configPath, "claude.json"), claudeConfig);
            
            // Gemini設定
            var geminiConfig = @"{
  ""provider"": ""google"",
  ""model"": ""gemini-pro"",
  ""temperature"": 0.8,
  ""tools"": [""unity_create"", ""unity_modify""]
}";
            File.WriteAllText(Path.Combine(configPath, "gemini.json"), geminiConfig);
            
            // Copilot設定
            var copilotConfig = @"{
  ""provider"": ""github"",
  ""model"": ""gpt-4"",
  ""temperature"": 0.5,
  ""tools"": [""unity_create"", ""unity_modify"", ""code_generation""]
}";
            File.WriteAllText(Path.Combine(configPath, "copilot.json"), copilotConfig);
            
            currentStatus.aiConfigurations["Claude"] = true;
            currentStatus.aiConfigurations["Gemini"] = true;
            currentStatus.aiConfigurations["Copilot"] = true;
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// 設定ファイルの生成
        /// </summary>
        private async Task GenerateConfigFiles()
        {
            // MCP設定ファイル
            var mcpConfig = @"{
  ""servers"": {
    ""unity"": {
      ""command"": ""node"",
      ""args"": [""index.js""],
      ""cwd"": """ + mcpServerPath.Replace("\\", "/") + @""",
      ""env"": {
        ""NODE_ENV"": ""production""
      }
    }
  },
  ""tools"": {
    ""unity_create"": {
      ""description"": ""Create Unity GameObjects""
    },
    ""unity_modify"": {
      ""description"": ""Modify Unity GameObjects""
    },
    ""unity_delete"": {
      ""description"": ""Delete Unity GameObjects""
    }
  }
}";
            
            // MCP設定ファイルはプロジェクト内に保存
            var mcpConfigPath = Path.Combine(mcpServerPath, "mcp-config.json");
            File.WriteAllText(mcpConfigPath, mcpConfig);
            
            // 参照用にドキュメントリンクを含むREADMEを生成
            var readmeContent = @"# Unity MCP Server

## Documentation
- MCP Documentation: https://modelcontextprotocol.io/docs
- MCP Specification: https://modelcontextprotocol.io/specification/2025-06-18
- MCP SDK: https://www.npmjs.com/package/@modelcontextprotocol/sdk

## API Keys
- Claude API: https://console.anthropic.com/
- Gemini API: https://aistudio.google.com/app/apikey
- OpenAI API: https://platform.openai.com/api-keys

## Setup
1. Run `npm install` to install dependencies
2. Configure API keys in .env file
3. Run `npm start` to start the server
";
            File.WriteAllText(Path.Combine(mcpServerPath, "README.md"), readmeContent);
            
            // スタートアップスクリプト
            var startScript = @"#!/bin/bash
cd """ + mcpServerPath + @"""
npm start
";
            
            var startScriptPath = Path.Combine(mcpServerPath, "start.sh");
            File.WriteAllText(startScriptPath, startScript);
            
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.LinuxEditor)
            {
                await RunCommand("chmod", $"+x \"{startScriptPath}\"");
            }
        }

        /// <summary>
        /// コマンド実行ヘルパー
        /// </summary>
        private async Task<string> CheckCommand(string command, string args)
        {
            try
            {
                var resolvedCommand = ResolveCommandPath(command);
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = resolvedCommand,
                        Arguments = args,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();
                
                return output.Trim();
            }
            catch
            {
                return "";
            }
        }
        
        private string ResolveCommandPath(string command)
        {
            // まずwhichコマンドで検索（最も確実）
            try
            {
                var whichResult = RunWhichCommand(command);
                if (!string.IsNullOrEmpty(whichResult) && File.Exists(whichResult))
                {
                    Debug.Log($"[MCP Setup] {command}を検出: {whichResult}");
                    return whichResult;
                }
            }
            catch { }
            
            // 一般的なコマンドパスを検索
            var commonPaths = new[]
            {
                "/opt/homebrew/bin",      // Apple Silicon Homebrew
                "/usr/local/bin",         // Intel Homebrew / 手動インストール
                "/usr/bin",               // システム標準
                "/bin",                   // システム基本
                "/usr/local/Cellar",      // Homebrew Cellar直接
                "/Users/" + Environment.UserName + "/.nvm/versions/node", // NVM
                "/Users/" + Environment.UserName + "/.volta/bin",         // Volta
                "/Users/" + Environment.UserName + "/.npm-global/bin"     // npm global
            };
            
            foreach (var basePath in commonPaths)
            {
                if (Directory.Exists(basePath))
                {
                    // 直接チェック
                    var directPath = Path.Combine(basePath, command);
                    if (File.Exists(directPath))
                    {
                        Debug.Log($"[MCP Setup] {command}を検出: {directPath}");
                        return directPath;
                    }
                    
                    // サブディレクトリも検索（NVM等のバージョン管理対応）
                    try
                    {
                        var subdirs = Directory.GetDirectories(basePath);
                        foreach (var subdir in subdirs)
                        {
                            var binPath = Path.Combine(subdir, "bin", command);
                            if (File.Exists(binPath))
                            {
                                Debug.Log($"[MCP Setup] {command}を検出: {binPath}");
                                return binPath;
                            }
                        }
                    }
                    catch { }
                }
            }
            
            // 環境変数PATHから検索
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                var separator = Application.platform == RuntimePlatform.WindowsEditor ? ';' : ':';
                foreach (var path in pathEnv.Split(separator))
                {
                    if (!string.IsNullOrEmpty(path))
                    {
                        var fullPath = Path.Combine(path.Trim(), command);
                        if (File.Exists(fullPath))
                        {
                            Debug.Log($"[MCP Setup] {command}をPATHで検出: {fullPath}");
                            return fullPath;
                        }
                    }
                }
            }
            
            Debug.LogWarning($"[MCP Setup] {command}が見つかりません");
            return command; // 見つからない場合は元のコマンド名を返す
        }
        
        private string RunWhichCommand(string command)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "/usr/bin/which",
                        Arguments = command,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                var output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
                
                return process.ExitCode == 0 ? output : "";
            }
            catch
            {
                return "";
            }
        }
        
        private async Task<bool> RunCommand(string command, string args, string workingDir = null)
        {
            try
            {
                var resolvedCommand = ResolveCommandPath(command);
                Debug.Log($"[MCP Setup] 実行: {resolvedCommand} {args}");
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = resolvedCommand,
                        Arguments = args,
                        WorkingDirectory = workingDir ?? projectPath,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                // 環境変数PATHを適切に設定
                SetupEnvironmentPath(process.StartInfo);
                
                process.Start();
                
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                
                process.WaitForExit();
                
                if (!string.IsNullOrEmpty(output))
                    Debug.Log($"[MCP Setup] {output}");
                    
                if (!string.IsNullOrEmpty(error))
                    Debug.LogWarning($"[MCP Setup] {error}");
                
                return process.ExitCode == 0;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MCP Setup] Command error: {e.Message}");
                return false;
            }
        }
        
        private void RefreshEnvironmentPath()
        {
            // 環境変数を再読み込み
            var path = Environment.GetEnvironmentVariable("PATH");
            Environment.SetEnvironmentVariable("PATH", path, EnvironmentVariableTarget.Process);
        }
        
        /// <summary>
        /// MCPサーバーをバックグラウンドで起動
        /// </summary>
        private async Task StartMCPServerBackground(string npmPath)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = npmPath,
                        Arguments = "start",
                        WorkingDirectory = mcpServerPath,
                        UseShellExecute = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };
                
                // 環境変数PATHを適切に設定
                SetupEnvironmentPath(process.StartInfo);
                
                Debug.Log($"[MCP Setup] バックグラウンドでMCPサーバーを起動: {npmPath} start");
                process.Start();
                
                // プロセスの開始を少し待つ
                await Task.Delay(1000);
                
                mcpServerProcessId = process.Id;
                Debug.Log($"[MCP Setup] MCPサーバープロセスID: {process.Id}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MCP Setup] バックグラウンド起動エラー: {e.Message}");
                
                // フォールバック: nodeで直接起動
                try
                {
                    var nodePath = ResolveCommandPath("node");
                    var nodeProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = nodePath,
                            Arguments = "index.js",
                            WorkingDirectory = mcpServerPath,
                            UseShellExecute = false,
                            RedirectStandardOutput = false,
                            RedirectStandardError = false,
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        }
                    };
                    
                    SetupEnvironmentPath(nodeProcess.StartInfo);
                    Debug.Log($"[MCP Setup] フォールバック: {nodePath} index.js");
                    nodeProcess.Start();
                    await Task.Delay(1000);
                }
                catch (Exception nodeEx)
                {
                    Debug.LogError($"[MCP Setup] Node.js直接起動も失敗: {nodeEx.Message}");
                }
            }
        }
        
        private void CheckAIConfigurations()
        {
            // AI設定ファイルの存在をチェック
            var configPath = Path.Combine(mcpServerPath, "ai-config");
            if (Directory.Exists(configPath))
            {
                currentStatus.aiConfigurations["Claude"] = File.Exists(Path.Combine(configPath, "claude.json"));
                currentStatus.aiConfigurations["Gemini"] = File.Exists(Path.Combine(configPath, "gemini.json"));
                currentStatus.aiConfigurations["Copilot"] = File.Exists(Path.Combine(configPath, "copilot.json"));
            }
        }
        
        private async Task InstallUnityPackage(string packageId, string version)
        {
            var manifestPath = Path.Combine(projectPath, "Packages", "manifest.json");
            if (File.Exists(manifestPath))
            {
                var manifest = File.ReadAllText(manifestPath);
                var packageLine = $"\"{packageId}\": \"{version}\"";
                
                if (!manifest.Contains(packageId))
                {
                    // manifest.jsonに追加
                    manifest = manifest.Replace("\"dependencies\": {", $"\"dependencies\": {{\n    {packageLine},");
                    File.WriteAllText(manifestPath, manifest);
                    
                    AssetDatabase.Refresh();
                }
            }
            
            await Task.CompletedTask;
        }
        
        /// <summary>
        /// MCPサーバーを起動
        /// </summary>
        public async Task<bool> StartMCPServer()
        {
            try
            {
                Debug.Log("[MCP Setup] Checking for existing MCP server...");
                
                // MCPサーバーは起動せず、既存のサーバーに接続するだけ
                bool serverExists = await CheckPortListening(8080);
                
                if (serverExists)
                {
                    Debug.Log("[MCP Setup] ✅ Found MCP server on port 8080 - Unity will connect as client");
                    mcpServerProcessId = -1; // 外部プロセスなのでIDは保持しない
                    return true;
                }
                else
                {
                    Debug.LogWarning("[MCP Setup] ❌ No MCP server found on port 8080");
                    Debug.LogWarning("[MCP Setup] Please start Claude Desktop or another AI application first");
                    
                    // Unity側ではMCPサーバーを起動しない
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MCP Setup] Failed to start server: {e.Message}");
                return false;
            }
        }
        
        private async Task<bool> CheckPortListening(int port)
        {
            try
            {
                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    var result = await CheckCommand("netstat", $"-an | findstr :{port}");
                    return !string.IsNullOrEmpty(result);
                }
                else
                {
                    var result = await CheckCommand("lsof", $"-i :{port}");
                    return !string.IsNullOrEmpty(result);
                }
            }
            catch
            {
                return false;
            }
        }
        
        private void SetupEnvironmentPath(ProcessStartInfo startInfo)
        {
            // 現在のPATHを取得
            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
            
            // 追加すべきパス
            var additionalPaths = new[]
            {
                "/opt/homebrew/bin",
                "/usr/local/bin",
                "/usr/bin",
                "/bin"
            };
            
            var pathList = new List<string>();
            
            // 追加パスを最初に追加（優先度を高くする）
            foreach (var path in additionalPaths)
            {
                if (Directory.Exists(path) && !pathList.Contains(path))
                {
                    pathList.Add(path);
                }
            }
            
            // 既存のPATHを追加
            if (!string.IsNullOrEmpty(currentPath))
            {
                var separator = Application.platform == RuntimePlatform.WindowsEditor ? ';' : ':';
                foreach (var path in currentPath.Split(separator))
                {
                    if (!string.IsNullOrEmpty(path.Trim()) && !pathList.Contains(path.Trim()))
                    {
                        pathList.Add(path.Trim());
                    }
                }
            }
            
            // 新しいPATHを設定
            var newPath = string.Join(Application.platform == RuntimePlatform.WindowsEditor ? ";" : ":", pathList);
            startInfo.EnvironmentVariables["PATH"] = newPath;
            
            Debug.Log($"[MCP Setup] 設定したPATH: {newPath}");
        }
        
        /// <summary>
        /// 既存のMCPサーバーをクリーンアップ
        /// </summary>
        private async Task CleanupExistingServers()
        {
            try
            {
                // まずポートが使用中かチェック
                bool needsCleanup = false;
                
                for (int port = 3000; port <= 3010; port++)
                {
                    if (await CheckPortListening(port))
                    {
                        needsCleanup = true;
                        Debug.Log($"[MCP Setup] ポート {port} が使用中です");
                        break;
                    }
                }
                
                if (await CheckPortListening(8080))
                {
                    needsCleanup = true;
                    Debug.Log("[MCP Setup] ポート 8080 が使用中です");
                }
                
                if (needsCleanup)
                {
                    Debug.Log("[MCP Setup] 既存のサーバープロセスをクリーンアップします");
                    await StopMCPServer();
                    
                    // ポートが解放されるまで待機
                    await Task.Delay(2000);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MCP Setup] クリーンアップ時の警告: {e.Message}");
            }
        }
        
        /// <summary>
        /// MCPサーバーを停止
        /// </summary>
        public async Task<bool> StopMCPServer()
        {
            try
            {
                Debug.Log("[MCP Setup] MCPサーバーを停止中...");
                
                // まず保存されたプロセスIDでkillを試みる
                if (mcpServerProcessId > 0)
                {
                    try
                    {
                        var killByIdProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "/bin/kill",
                                Arguments = $"-9 {mcpServerProcessId}",
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        killByIdProcess.Start();
                        killByIdProcess.WaitForExit();
                        Debug.Log($"[MCP Setup] プロセス {mcpServerProcessId} をkillしました");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[MCP Setup] プロセスID killエラー: {e.Message}");
                    }
                }
                
                // 念のためpkillでnode.jsプロセスを終了
                await Task.Run(() =>
                {
                    try
                    {
                        var killProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "/usr/bin/pkill",
                                Arguments = "-f \"node.*index.js\"",
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true
                            }
                        };
                        killProcess.Start();
                        killProcess.WaitForExit();
                        
                        Debug.Log("[MCP Setup] pkillコマンド実行完了");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[MCP Setup] pkill実行時の警告: {e.Message}");
                    }
                });
                
                // プロセスが完全に終了するまで待機
                await Task.Delay(1000);
                
                // ポートが解放されたことを確認
                bool stillRunning = await CheckPortListening(3000);
                if (!stillRunning)
                {
                    Debug.Log("[MCP Setup] MCPサーバーが正常に停止しました");
                    return true;
                }
                else
                {
                    Debug.LogWarning("[MCP Setup] MCPサーバーがまだ実行中の可能性があります");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[MCP Setup] サーバー停止エラー: {e.Message}");
                return false;
            }
        }
    }
}