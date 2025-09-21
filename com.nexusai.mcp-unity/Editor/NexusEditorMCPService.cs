using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Linq;

namespace NexusAIConnect
{
    /// <summary>
    /// Editor専用MCPサービス - シーンに依存しない独立したサービス
    /// PlayMode切り替えやシーン変更に影響されない
    /// </summary>
    [InitializeOnLoad]
    public static class NexusEditorMCPService
    {
        private static ClientWebSocket webSocket;
        private static CancellationTokenSource cancellationTokenSource;
        private static bool isConnected = false;
        private static Queue<MCPMessage> messageQueue = new Queue<MCPMessage>();
        private static string serverUrl = null; // 動的に設定される
        private static bool isInitialized = false;
        private static bool shouldReconnect = true;
        private static int reconnectAttempts = 0;
        private static int maxReconnectAttempts = 5;
        private static float lastReconnectTime = 0;
        private static float lastConnectionCheckTime = 0;
        private static bool isReconnecting = false;
        private static int reconnectPhase = 0; // 0: 待機, 1: 軽いテスト(2秒), 2: 本格再接続(5秒), 3: 失敗リトライ(10秒)
        
        // Play Mode自動再接続関連
        private static bool wasConnectedBeforePlayMode = false;
        private static bool enableAutoReconnect = true;
        private static string connectionStateKey = "NexusMCP_ConnectionState";
        private static string autoReconnectKey = "NexusMCP_AutoReconnect";

        public static bool IsConnected => isConnected && webSocket != null && webSocket.State == WebSocketState.Open;
        
        public static string GetServerUrl() => serverUrl ?? "ws://localhost:8080";
        
        public static event Action<string> OnMessageReceived;
        public static event Action OnConnected;
        public static event Action OnDisconnected;
        public static event Action<string> OnError;

        [Serializable]
        public class MCPMessage
        {
            public string type;
            public string id;
            public string requestId;
            public string operation;
            public string provider;
            public string content;
            public Dictionary<string, object> parameters;
            public string tool;
            public string command;
            public object data;
        }

        static NexusEditorMCPService()
        {
            Initialize();
        }

        private static void Initialize()
        {
            if (isInitialized) return;
            
            
            // 利用可能なポートを自動検出
            DetectAndSetAvailablePort();
            
            // EditorApplication イベントを購読
            EditorApplication.update += Update;
            EditorApplication.quitting += OnEditorQuitting;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            // コンパイル完了イベントを購読
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            
            // 設定を読み込み
            enableAutoReconnect = EditorPrefs.GetBool(autoReconnectKey, true);
            
            // 遅延自動接続（5秒後）- closed状態でも確実に接続
            EditorApplication.delayCall += () =>
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000); // 5秒待機
                    if (enableAutoReconnect && !isConnected)
                    {
                        try
                        {
                            await ConnectToMCPServer();
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[Nexus MCP] 初回自動接続失敗: {e.Message}");
                            isConnected = false;
                            // 失敗時は段階的再接続が引き継ぐ
                            OnConnectionLost();
                        }
                    }
                });
            };
            
            isInitialized = true;
        }

        /// <summary>
        /// 利用可能なポートを検出してサーバーURLを設定
        /// </summary>
        private static void DetectAndSetAvailablePort()
        {
            try
            {
                // MCPサーバーが使用している可能性の高いポートを順番に確認
                int[] candidatePorts = { 8081, 8080, 8082, 8083, 8084 };
                
                
                foreach (int port in candidatePorts)
                {
                    
                    if (IsPortInUse(port))
                    {
                        string testUrl = $"ws://localhost:{port}";
                        serverUrl = testUrl;
                        
                        // Claude Desktop設定も自動更新
                        UpdateClaudeDesktopConfigForPort(port);
                        break;
                    }
                }
                
                // ポートが見つからない場合はデフォルトを設定
                if (serverUrl == null)
                {
                    serverUrl = "ws://localhost:8080";
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Nexus Editor MCP] Port detection failed: {e.Message}, using default port");
                serverUrl = "ws://localhost:8080";
            }
        }

        /// <summary>
        /// MCPサーバーがそのポートで応答するかチェック
        /// </summary>
        private static bool IsPortInUse(int port)
        {
            try
            {
                // 単純なTCP接続テストでMCPサーバーの存在を確認
                using (var client = new System.Net.Sockets.TcpClient())
                {
                    var result = client.BeginConnect("localhost", port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(500));
                    
                    if (success && client.Connected)
                    {
                        client.Close();
                        return true; // MCPサーバーが応答した
                    }
                    
                    return false; // 接続できない
                }
            }
            catch
            {
                return false; // エラーの場合は使用不可とみなす
            }
        }

        private static void Update()
        {
            // メインスレッドでメッセージ処理
            while (messageQueue.Count > 0)
            {
                var message = messageQueue.Dequeue();
                ProcessMessage(message);
            }
            
            // 段階的再接続処理
            if (!enableAutoReconnect || EditorApplication.isCompiling || isReconnecting)
                return;
                
            float currentTime = Time.realtimeSinceStartup;
            
            // WebSocketがclosed状態の場合も再接続対象に含める
            bool needsReconnection = !isConnected || 
                                   (webSocket != null && (webSocket.State == WebSocketState.Closed || 
                                                        webSocket.State == WebSocketState.Aborted ||
                                                        webSocket.State == WebSocketState.None));
            
            if (needsReconnection)
            {
                switch (reconnectPhase)
                {
                    case 0: // 切断検出後、2秒待機してから軽いテスト
                        if (currentTime - lastConnectionCheckTime > 2f)
                        {
                            reconnectPhase = 1;
                            lastReconnectTime = currentTime;
                            _ = Task.Run(LightConnectionTest);
                        }
                        break;
                        
                    case 1: // 軽いテストから5秒後に本格再接続
                        if (currentTime - lastReconnectTime > 5f)
                        {
                            reconnectPhase = 2;
                            lastReconnectTime = currentTime;
                            _ = Task.Run(FullReconnectAttempt);
                        }
                        break;
                        
                    case 2: // 本格再接続失敗時、10秒後にリトライ
                        if (currentTime - lastReconnectTime > 10f)
                        {
                            reconnectPhase = 3;
                            lastReconnectTime = currentTime;
                            _ = Task.Run(RetryReconnect);
                        }
                        break;
                        
                    case 3: // リトライ後、さらに10秒待ってフェーズ1に戻る
                        if (currentTime - lastReconnectTime > 10f)
                        {
                            reconnectPhase = 1;
                            lastReconnectTime = currentTime;
                        }
                        break;
                }
            }
            else
            {
                // 接続成功時はフェーズをリセット
                reconnectPhase = 0;
                
                // 接続中でもWebSocket状態をチェック（Closed状態になったら即座に再接続開始）
                if (webSocket != null && webSocket.State == WebSocketState.Closed)
                {
                    OnConnectionLost();
                }
            }
        }

        private static void OnEditorQuitting()
        {
            DisconnectFromMCPServer();
            
            // イベント購読解除
            EditorApplication.update -= Update;
            EditorApplication.quitting -= OnEditorQuitting;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!enableAutoReconnect) return;

            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    // Edit Mode終了時 - 接続状態を保存
                    wasConnectedBeforePlayMode = IsConnected;
                    if (wasConnectedBeforePlayMode)
                    {
                        EditorPrefs.SetBool(connectionStateKey, true);
                        EditorPrefs.SetString(connectionStateKey + "_ServerUrl", serverUrl);
                    }
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    
                    // Play Modeでも接続が切れていたら再接続
                    if (wasConnectedBeforePlayMode && !IsConnected)
                    {
                        Debug.Log("[Nexus Editor MCP] 🔄 Play Modeで接続が切れています。再接続します...");
                        EditorApplication.delayCall += () =>
                        {
                            _ = Task.Run(async () => await ConnectToMCPServer());
                        };
                    }
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    Debug.Log("[Nexus Editor MCP] ⏹️ Play Modeを終了しています...");
                    // Play Mode終了前に現在の接続状態を保存
                    if (IsConnected)
                    {
                        EditorPrefs.SetBool(connectionStateKey, true);
                        EditorPrefs.SetString(connectionStateKey + "_ServerUrl", serverUrl);
                    }
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    // Edit Mode復帰時 - 自動再接続
                    if (EditorPrefs.GetBool(connectionStateKey, false))
                    {
                        var savedServerUrl = EditorPrefs.GetString(connectionStateKey + "_ServerUrl", "");
                        if (!string.IsNullOrEmpty(savedServerUrl))
                        {
                            serverUrl = savedServerUrl;
                        }

                        Debug.Log("[Nexus Editor MCP] ⏹️ Play Mode終了。自動再接続を開始します...");
                        
                        // 少し遅延して再接続
                        EditorApplication.delayCall += () =>
                        {
                            _ = Task.Run(async () => await ConnectToMCPServer());
                            EditorPrefs.DeleteKey(connectionStateKey);
                            EditorPrefs.DeleteKey(connectionStateKey + "_ServerUrl");
                        };
                    }
                    break;
            }
        }
        
        /// <summary>
        /// コンパイル完了時のハンドラー
        /// </summary>
        private static void OnCompilationFinished(object context)
        {
            if (!enableAutoReconnect) return;
            
            Debug.Log("[Nexus MCP] 🔨 コンパイル完了を検知 - 高速再接続を開始");
            
            // コンパイル完了から0.5秒後に再接続
            EditorApplication.delayCall += () =>
            {
                if (!isConnected || (webSocket != null && webSocket.State != WebSocketState.Open))
                {
                    // 現在の再接続フェーズをリセットして即座に再接続
                    reconnectPhase = 0;
                    lastConnectionCheckTime = 0;
                    lastReconnectTime = 0;
                    
                    // 高速再接続を実行
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(500); // 0.5秒待機
                        
                        try
                        {
                            await ConnectToMCPServer();
                            Debug.Log("[Nexus MCP] ⚡ コンパイル後の高速再接続成功！");
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[Nexus MCP] コンパイル後の再接続失敗: {e.Message}");
                            // 失敗時は通常の段階的再接続にフォールバック
                            OnConnectionLost();
                        }
                    });
                }
            };
        }

        public static async Task ConnectToMCPServer()
        {
            try
            {
                if (isConnected)
                {
                    Debug.Log("[Nexus Editor MCP] Already connected");
                    return;
                }

                // serverUrlがnullの場合は再検出
                if (serverUrl == null)
                {
                    DetectAndSetAvailablePort();
                }
                
                Debug.Log($"[Nexus Editor MCP] Connecting to MCP Server: {serverUrl} (Attempt {reconnectAttempts + 1})");
                
                // 既存の接続をクリーンアップ
                if (webSocket != null)
                {
                    webSocket.Dispose();
                }
                if (cancellationTokenSource != null)
                {
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();
                }
                
                webSocket = new ClientWebSocket();
                cancellationTokenSource = new CancellationTokenSource();
                
                await webSocket.ConnectAsync(new Uri(serverUrl), cancellationTokenSource.Token);
                
                Debug.Log($"[Nexus Editor MCP] WebSocket State after connect: {webSocket.State}");
                
                isConnected = true;
                reconnectAttempts = 0; // 成功時にリセット
                OnConnected?.Invoke();
                
                Debug.Log("[Nexus Editor MCP] Connected to MCP Server successfully");
                
                // 接続確認メッセージを送信
                await SendConnectionPing();
                
                // メッセージリスナー開始
                _ = Task.Run(async () => await ListenForMessages());
            }
            catch (Exception e)
            {
                reconnectAttempts++;
                Debug.LogError($"[Nexus Editor MCP] Failed to connect (attempt {reconnectAttempts}): {e.Message}");
                OnError?.Invoke(e.Message);
                isConnected = false;
                
                // 接続失敗時は自動再接続を開始
                OnConnectionLost();
                
                if (reconnectAttempts >= maxReconnectAttempts)
                {
                    Debug.LogError("[Nexus Editor MCP] Max reconnection attempts reached. Please check MCP server status.");
                }
            }
        }

        private static async Task ListenForMessages()
        {
            Debug.Log("[Nexus Editor MCP] Starting message listener");
            
            var buffer = new byte[1024 * 16]; // バッファサイズを増加
            
            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var messageBuffer = new List<byte>();
                    WebSocketReceiveResult result;
                    
                    // メッセージ全体を受信するまでループ
                    do
                    {
                        var segment = new ArraySegment<byte>(buffer);
                        result = await webSocket.ReceiveAsync(segment, cancellationTokenSource.Token);
                        
                        if (result.Count > 0)
                        {
                            messageBuffer.AddRange(buffer.Take(result.Count));
                        }
                    } 
                    while (!result.EndOfMessage);
                    
                    if (result.MessageType == WebSocketMessageType.Text && messageBuffer.Count > 0)
                    {
                        var messageText = Encoding.UTF8.GetString(messageBuffer.ToArray());
                        Debug.Log($"[Nexus Editor MCP] ⚡ RAW MESSAGE RECEIVED: {messageText}");
                        
                        try
                        {
                            var message = JsonConvert.DeserializeObject<MCPMessage>(messageText);
                            if (message != null)
                            {
                                // メインスレッドで処理するためにキューに追加
                                messageQueue.Enqueue(message);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"[Nexus Editor MCP] Failed to parse message: {e.Message}");
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Debug.Log("[Nexus Editor MCP] WebSocket closed by server");
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nexus Editor MCP] Message listener error: {e.Message}");
                
                // WebSocket例外の場合は自動再接続を試行
                if (e is WebSocketException || e.Message.Contains("WebSocket"))
                {
                    Debug.Log("[Nexus Editor MCP] WebSocket error detected, will attempt reconnection");
                }
            }
            finally
            {
                // WebSocketが正常に閉じられていない場合のみ切断処理
                if (webSocket?.State != WebSocketState.Open)
                {
                    OnConnectionLost();
                    OnDisconnected?.Invoke();
                }
            }
        }

        private static void ProcessMessage(MCPMessage message)
        {
            Debug.Log($"[Nexus Editor MCP] Processing message type: {message.type}, tool: {message.tool}, command: {message.command}");
            
            switch (message.type)
            {
                case "unity_operation":
                case "tool_call":
                    ExecuteUnityOperation(message);
                    break;
                    
                case "ai_response":
                    OnMessageReceived?.Invoke(message.content);
                    break;
                    
                case "error":
                    Debug.LogWarning($"[Nexus Editor MCP] {message.content}");
                    OnMessageReceived?.Invoke($"❗ {message.content}");
                    break;
                    
                default:
                    Debug.Log($"[Nexus Editor MCP] Unknown message type: {message.type}");
                    break;
            }
        }

        private static void ExecuteUnityOperation(MCPMessage message)
        {
            Debug.Log($"[Nexus Editor MCP] Executing Unity operation: {message.operation ?? message.tool} with command: {message.command}");
            
            try
            {
                // MCPツール名をUnity操作にマッピング
                string operationType = message.operation ?? message.command ?? message.tool ?? "";
                
                // ツール名を既存のオペレーションタイプに変換
                operationType = ConvertMCPToolToOperation(operationType);
                
                Debug.Log($"[Nexus Editor MCP] Converted operation type: {operationType}");
                
                var operation = new NexusUnityOperation
                {
                    type = operationType,
                    parameters = new Dictionary<string, string>()
                };

                // パラメーターの変換
                if (message.parameters != null)
                {
                    foreach (var kvp in message.parameters)
                    {
                        if (kvp.Value != null)
                        {
                            // ネストされたオブジェクトの処理
                            if (kvp.Value is Newtonsoft.Json.Linq.JObject jObj)
                            {
                                // Vector3のような構造体の処理
                                if (jObj.ContainsKey("x") && jObj.ContainsKey("y") && jObj.ContainsKey("z"))
                                {
                                    operation.parameters[kvp.Key] = $"{jObj["x"]},{jObj["y"]},{jObj["z"]}";
                                }
                                else if (jObj.ContainsKey("x") && jObj.ContainsKey("y"))
                                {
                                    operation.parameters[kvp.Key] = $"{jObj["x"]},{jObj["y"]}";
                                }
                                else if (jObj.ContainsKey("r") && jObj.ContainsKey("g") && jObj.ContainsKey("b"))
                                {
                                    operation.parameters[kvp.Key] = $"{jObj["r"]},{jObj["g"]},{jObj["b"]}";
                                }
                                else
                                {
                                    operation.parameters[kvp.Key] = jObj.ToString();
                                }
                            }
                            else
                            {
                                operation.parameters[kvp.Key] = kvp.Value.ToString();
                            }
                        }
                    }
                }

                Debug.Log($"[Nexus Editor MCP] About to execute operation with parameters: {operation.parameters.Count}");
                foreach (var param in operation.parameters)
                {
                    Debug.Log($"[Nexus Editor MCP] Parameter: {param.Key} = '{param.Value}'");
                }
                
                // Unity操作を実行（メインスレッド上で同期実行）
                ExecuteOperationAsync(operation, message.requestId ?? message.id);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nexus Editor MCP] Unity operation error: {e.Message}");
                _ = SendOperationResult(message.requestId ?? message.id, false, $"Error: {e.Message}");
            }
        }

        private static async void ExecuteOperationAsync(NexusUnityOperation operation, string messageId)
        {
            try
            {
                var executor = new NexusUnityExecutor();
                string result = await executor.ExecuteOperation(operation);
                bool success = !result.StartsWith("Error:") && !result.StartsWith("Failed:");
                
                Debug.Log($"[Nexus Editor MCP] Operation result: {result}");
                Debug.Log($"[Nexus Editor MCP] Operation success: {success}");
                
                // 結果をMCPサーバーに送信
                await SendOperationResult(messageId, success, result);
                
                // 結果をログに出力
                if (success)
                {
                    Debug.Log($"[Nexus Editor MCP] SUCCESS: {result}");
                }
                else
                {
                    Debug.LogError($"[Nexus Editor MCP] FAILED: {result}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nexus Editor MCP] Async operation error: {e.Message}");
                await SendOperationResult(messageId, false, $"Error: {e.Message}");
            }
        }

        private static async Task SendConnectionPing()
        {
            try
            {
                var pingMessage = new MCPMessage
                {
                    type = "ping",
                    id = Guid.NewGuid().ToString(),
                    content = "Unity Editor connected"
                };
                
                var json = JsonConvert.SerializeObject(pingMessage);
                var buffer = Encoding.UTF8.GetBytes(json);
                
                await webSocket.SendAsync(
                    new ArraySegment<byte>(buffer), 
                    WebSocketMessageType.Text, 
                    true, 
                    CancellationToken.None
                );
                
                Debug.Log("[Nexus Editor MCP] Sent connection ping");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Nexus Editor MCP] Failed to send ping: {e.Message}");
            }
        }

        private static async Task SendOperationResult(string messageId, bool success, string result)
        {
            if (!IsConnected) return;

            object structuredData = null;
            string displayContent = result;
            
            // JSONパースを試行して構造化データとして送信
            try
            {
                // 結果がJSONの場合は構造化データとして送信
                if (result.TrimStart().StartsWith("{") || result.TrimStart().StartsWith("["))
                {
                    structuredData = JsonConvert.DeserializeObject(result);
                    displayContent = success ? "構造化データを取得しました" : result;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Nexus Editor MCP] JSON parse failed: {e.Message}");
            }

            // MCPプロトコルに従って、結果をcontentフィールドに格納
            var response = new MCPMessage
            {
                type = "operation_result",
                id = messageId,
                requestId = messageId, // 互換性のため両方設定
                content = result, // 元の結果（JSON文字列）をそのまま返す
                data = new { 
                    success = success,
                    result = structuredData ?? result,
                    error = success ? null : result
                }
            };

            try
            {
                var json = JsonConvert.SerializeObject(response, Formatting.Indented);
                var buffer = Encoding.UTF8.GetBytes(json);
                
                await webSocket.SendAsync(
                    new ArraySegment<byte>(buffer), 
                    WebSocketMessageType.Text, 
                    true, 
                    cancellationTokenSource.Token
                );
                
                Debug.Log($"[Nexus Editor MCP] Sent operation result: {success}");
                Debug.Log($"[Nexus Editor MCP] Response JSON: {json}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nexus Editor MCP] Failed to send operation result: {e.Message}");
            }
        }

        public static void DisconnectFromMCPServer()
        {
            try
            {
                shouldReconnect = false; // 手動切断時は自動再接続を停止
                
                if (webSocket != null && isConnected)
                {
                    isConnected = false;
                    cancellationTokenSource?.Cancel();
                    
                    if (webSocket.State == WebSocketState.Open)
                    {
                        webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
                    }
                    
                    webSocket.Dispose();
                    webSocket = null;
                    
                    OnDisconnected?.Invoke();
                    Debug.Log("[Nexus Editor MCP] Disconnected from MCP Server");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nexus Editor MCP] Error during disconnect: {e.Message}");
            }
        }

        public static void SetServerUrl(string url)
        {
            serverUrl = url;
            Debug.Log($"[Nexus Editor MCP] Server URL changed to: {url}");
        }

        public static async void ReconnectToMCPServer()
        {
            shouldReconnect = true; // 再接続を有効化
            reconnectAttempts = 0; // カウンターリセット
            
            DisconnectFromMCPServer();
            shouldReconnect = true; // Disconnectでfalseになるので再度有効化
            
            await Task.Delay(1000); // 1秒待機
            await ConnectToMCPServer();
        }

        private static string ConvertMCPToolToOperation(string mcpTool)
        {
            switch (mcpTool)
            {
                // GameObject操作
                case "unity_create_gameobject":
                case "create_gameobject":
                    return "CREATE_GAMEOBJECT";
                    
                case "unity_update_gameobject":
                case "update_gameobject":
                    return "UPDATE_GAMEOBJECT";
                    
                case "unity_delete_gameobject":
                case "delete_gameobject":
                    return "DELETE_GAMEOBJECT";
                    
                case "unity_set_transform":
                case "set_transform":
                    return "SET_TRANSFORM";
                    
                // コンポーネント
                case "unity_add_component":
                case "add_component":
                    return "ADD_COMPONENT";
                    
                case "unity_update_component":
                case "update_component":
                    return "UPDATE_COMPONENT";
                    
                // UI
                case "unity_create_ui":
                case "create_ui":
                    return "CREATE_UI";
                    
                // 地形
                case "unity_create_terrain":
                case "create_terrain":
                    return "CREATE_TERRAIN";
                    
                case "unity_modify_terrain":
                case "modify_terrain":
                    return "MODIFY_TERRAIN";
                    
                // カメラ
                case "unity_setup_camera":
                case "setup_camera":
                    return "SETUP_CAMERA";
                    
                // 配置
                case "unity_place_objects":
                case "place_objects":
                    return "PLACE_OBJECTS";
                    
                // ライティング
                case "unity_setup_lighting":
                case "setup_lighting":
                    return "SETUP_LIGHTING";
                    
                // マテリアル
                case "unity_create_material":
                case "create_material":
                    return "CREATE_MATERIAL";
                    
                // プレハブ
                case "unity_create_prefab":
                case "create_prefab":
                    return "CREATE_PREFAB";
                    
                // スクリプト
                case "unity_create_script":
                case "create_script":
                    return "CREATE_SCRIPT";
                    
                // シーン
                case "unity_manage_scene":
                case "manage_scene":
                    return "MANAGE_SCENE";
                    
                // アニメーション
                case "unity_create_animation":
                case "create_animation":
                    return "CREATE_ANIMATION";
                    
                // 物理
                case "unity_setup_physics":
                case "setup_physics":
                    return "SETUP_PHYSICS";
                    
                // パーティクル・VFX
                case "unity_create_particle_system":
                case "create_particle_system":
                    return "CREATE_PARTICLE_SYSTEM";
                    
                // ナビゲーション
                case "unity_setup_navmesh":
                case "setup_navmesh":
                    return "SETUP_NAVMESH";
                    
                // オーディオ
                case "unity_create_audio_mixer":
                case "create_audio_mixer":
                    return "CREATE_AUDIO_MIXER";
                    
                // 操作履歴・Undo/Redo
                case "unity_get_operation_history":
                    return "GET_OPERATION_HISTORY";
                    
                case "unity_undo_operation":
                    return "UNDO_OPERATION";
                    
                case "unity_redo_operation":
                    return "REDO_OPERATION";
                    
                case "unity_create_checkpoint":
                    return "CREATE_CHECKPOINT";
                    
                case "unity_restore_checkpoint":
                    return "RESTORE_CHECKPOINT";
                    
                // リアルタイムイベント監視
                case "unity_monitor_play_state":
                    return "MONITOR_PLAY_STATE";
                    
                case "unity_monitor_file_changes":
                    return "MONITOR_FILE_CHANGES";
                    
                case "unity_monitor_compile":
                    return "MONITOR_COMPILE";
                    
                case "unity_subscribe_events":
                    return "SUBSCRIBE_EVENTS";
                    
                case "unity_get_events":
                    return "GET_EVENTS";
                    
                case "unity_get_monitoring_status":
                    return "GET_MONITORING_STATUS";
                    
                // プロジェクト設定系
                case "unity_get_build_settings":
                    return "GET_BUILD_SETTINGS";
                    
                case "unity_get_player_settings":
                    return "GET_PLAYER_SETTINGS";
                    
                case "unity_get_quality_settings":
                    return "GET_QUALITY_SETTINGS";
                    
                case "unity_get_input_settings":
                    return "GET_INPUT_SETTINGS";
                    
                case "unity_get_physics_settings":
                    return "GET_PHYSICS_SETTINGS";
                    
                case "unity_get_project_summary":
                    return "GET_PROJECT_SUMMARY";
                    
                // アセット管理
                case "unity_list_assets":
                case "unity_list_project_assets":
                case "list_assets":
                    return "LIST_ASSETS";
                    
                // フォルダ管理
                case "unity_check_folder":
                case "check_folder":
                    return "CHECK_FOLDER";
                    
                case "unity_create_folder":
                case "create_folder":
                    return "CREATE_FOLDER";
                    
                case "unity_list_folders":
                case "list_folders":
                    return "LIST_FOLDERS";
                    
                // 新しいツール群
                case "unity_duplicate_gameobject":
                case "duplicate_gameobject":
                    return "DUPLICATE_GAMEOBJECT";
                    
                case "unity_find_gameobjects_by_component":
                case "find_gameobjects_by_component":
                case "find_by_component":
                    return "FIND_BY_COMPONENT";
                    
                case "unity_cleanup_empty_objects":
                case "cleanup_empty_objects":
                    return "CLEANUP_EMPTY_OBJECTS";
                    
                case "unity_group_gameobjects":
                case "group_gameobjects":
                    return "GROUP_GAMEOBJECTS";
                    
                case "unity_rename_asset":
                case "rename_asset":
                    return "RENAME_ASSET";
                    
                case "unity_move_asset":
                case "move_asset":
                    return "MOVE_ASSET";
                    
                case "unity_delete_asset":
                case "delete_asset":
                    return "DELETE_ASSET";
                    
                case "unity_pause_scene":
                case "pause_scene":
                    return "PAUSE_SCENE";
                    
                case "unity_find_missing_references":
                case "find_missing_references":
                    return "FIND_MISSING_REFERENCES";
                
                case "unity_optimize_textures_batch":
                case "optimize_textures_batch":
                    return "OPTIMIZE_TEXTURES_BATCH";
                
                case "unity_analyze_draw_calls":
                case "analyze_draw_calls":
                    return "ANALYZE_DRAW_CALLS";
                
                case "unity_create_project_snapshot":
                case "create_project_snapshot":
                    return "CREATE_PROJECT_SNAPSHOT";
                
                case "unity_analyze_dependencies":
                case "analyze_dependencies":
                    return "ANALYZE_DEPENDENCIES";
                
                case "unity_export_project_structure":
                case "export_project_structure":
                    return "EXPORT_PROJECT_STRUCTURE";
                
                case "unity_validate_naming_conventions":
                case "validate_naming_conventions":
                    return "VALIDATE_NAMING_CONVENTIONS";
                
                case "unity_extract_all_text":
                case "extract_all_text":
                    return "EXTRACT_ALL_TEXT";
                
                case "unity_batch_rename":
                case "batch_rename":
                    return "BATCH_RENAME";
                
                case "unity_batch_import_settings":
                case "batch_import_settings":
                    return "BATCH_IMPORT_SETTINGS";
                
                case "unity_batch_prefab_update":
                case "batch_prefab_update":
                    return "BATCH_PREFAB_UPDATE";
                
                case "unity_find_unused_assets":
                case "find_unused_assets":
                    return "FIND_UNUSED_ASSETS";
                
                case "unity_estimate_build_size":
                case "estimate_build_size":
                    return "ESTIMATE_BUILD_SIZE";
                
                case "unity_performance_report":
                case "performance_report":
                    return "PERFORMANCE_REPORT";
                
                case "unity_auto_organize_folders":
                case "auto_organize_folders":
                    return "AUTO_ORGANIZE_FOLDERS";
                
                case "unity_generate_lod":
                case "generate_lod":
                    return "GENERATE_LOD";
                
                case "unity_auto_atlas_textures":
                case "auto_atlas_textures":
                    return "AUTO_ATLAS_TEXTURES";
                    
                // ゲーム開発特化機能
                case "unity_create_game_controller":
                case "create_game_controller":
                    return "CREATE_GAME_CONTROLLER";
                    
                case "unity_setup_input_system":
                case "setup_input_system":
                    return "SETUP_INPUT_SYSTEM";
                    
                case "unity_create_state_machine":
                case "create_state_machine":
                    return "CREATE_STATE_MACHINE";
                    
                case "unity_setup_inventory_system":
                case "setup_inventory_system":
                    return "SETUP_INVENTORY_SYSTEM";
                    
                // プロトタイピング機能
                case "unity_create_game_template":
                case "create_game_template":
                    return "CREATE_GAME_TEMPLATE";
                    
                case "unity_quick_prototype":
                case "quick_prototype":
                    return "QUICK_PROTOTYPE";
                    
                // AI・機械学習関連
                case "unity_setup_ml_agent":
                case "setup_ml_agent":
                    return "SETUP_ML_AGENT";
                    
                case "unity_create_neural_network":
                case "create_neural_network":
                    return "CREATE_NEURAL_NETWORK";
                    
                case "unity_setup_behavior_tree":
                case "setup_behavior_tree":
                    return "SETUP_BEHAVIOR_TREE";
                    
                case "unity_create_ai_pathfinding":
                case "create_ai_pathfinding":
                    return "CREATE_AI_PATHFINDING";
                    
                // スクリプト編集機能
                case "unity_modify_script":
                case "modify_script":
                    return "MODIFY_SCRIPT";
                    
                case "unity_edit_script_line":
                case "edit_script_line":
                    return "EDIT_SCRIPT_LINE";
                    
                case "unity_add_script_method":
                case "add_script_method":
                    return "ADD_SCRIPT_METHOD";
                    
                case "unity_update_script_variable":
                case "update_script_variable":
                    return "UPDATE_SCRIPT_VARIABLE";
                    
                // デバッグ・テストツール
                case "unity_control_game_speed":
                case "control_game_speed":
                    return "CONTROL_GAME_SPEED";
                    
                case "unity_profile_performance":
                case "profile_performance":
                    return "PROFILE_PERFORMANCE";
                    
                case "unity_debug_draw":
                case "debug_draw":
                    return "DEBUG_DRAW";
                    
                case "unity_run_tests":
                case "run_unity_tests":
                    return "RUN_UNITY_TESTS";
                    
                case "unity_manage_breakpoints":
                case "manage_breakpoints":
                    return "MANAGE_BREAKPOINTS";
                    
                // アニメーション系ツール
                case "unity_create_animator_controller":
                case "create_animator_controller":
                    return "CREATE_ANIMATOR_CONTROLLER";
                    
                case "unity_add_animation_state":
                case "add_animation_state":
                    return "ADD_ANIMATION_STATE";
                    
                case "unity_create_animation_clip":
                case "create_animation_clip":
                    return "CREATE_ANIMATION_CLIP";
                    
                case "unity_setup_blend_tree":
                case "setup_blend_tree":
                    return "SETUP_BLEND_TREE";
                    
                case "unity_add_animation_transition":
                case "add_animation_transition":
                    return "ADD_ANIMATION_TRANSITION";
                    
                case "unity_setup_animation_layer":
                case "setup_animation_layer":
                    return "SETUP_ANIMATION_LAYER";
                    
                case "unity_create_animation_event":
                case "create_animation_event":
                    return "CREATE_ANIMATION_EVENT";
                    
                case "unity_setup_avatar":
                case "setup_avatar":
                    return "SETUP_AVATAR";
                    
                case "unity_create_timeline":
                case "create_timeline":
                    return "CREATE_TIMELINE";
                    
                case "unity_bake_animation":
                case "bake_animation":
                    return "BAKE_ANIMATION";
                    
                // その他
                case "unity_search":
                case "search_objects":
                    return "SEARCH_OBJECTS";
                    
                case "unity_console":
                case "console_operation":
                    return "CONSOLE_OPERATION";
                    
                case "unity_analyze_console_logs":
                case "analyze_console_logs":
                    return "ANALYZE_CONSOLE_LOGS";
                    
                default:
                    return mcpTool.ToUpper();
            }
        }

        /// <summary>
        /// MCP Service status for debugging
        /// </summary>
        // [MenuItem("Nexus/AI接続状態を確認", false, 2)] // NexusMenuManager.cs に移動
        public static void ShowMCPStatus()
        {
            string connectionStatus = IsConnected ? "✅ 接続中" : "❌ 切断中";
            string webSocketState = webSocket?.State.ToString() ?? "null";
            
            string status = $@"🔗 AI 接続状態

{connectionStatus}

詳細情報:
• 初期化済み: {(isInitialized ? "✅" : "❌")}
• サーバーURL: {serverUrl ?? "未設定"}
• メッセージキュー: {messageQueue.Count} 件
• WebSocket状態: {webSocketState}
• 再接続試行: {reconnectAttempts}/{maxReconnectAttempts}
• 自動再接続: {(shouldReconnect ? "有効" : "無効")}

問題がある場合は「AI接続を修復」をお試しください。";

            Debug.Log(status);
            EditorUtility.DisplayDialog("AI 接続状態", status, "OK");
        }
        
        /// <summary>
        /// ツールバー用の接続状態表示
        /// </summary>
        // [MenuItem("Tools/AI接続状態", false, 1)]
        public static void QuickStatus()
        {
            string status = IsConnected ? "✅ AI: 接続中" : "❌ AI: 切断中";
            Debug.Log($"[Nexus MCP] {status}");
            EditorUtility.DisplayDialog("接続状態", status, "OK");
        }

        /// <summary>
        /// Manual reconnect for debugging
        /// </summary>
        // [MenuItem("Nexus/AI再接続", false, 1)] // NexusMenuManager.cs に移動
        public static void ManualReconnect()
        {
            Debug.Log("[Nexus Editor MCP] Manual reconnect requested");
            
            // ユーザーに確認ダイアログを表示
            bool reconnect = EditorUtility.DisplayDialog(
                "AI接続を修復",
                "Claudeなどとの接続を再確立します。\n\n" +
                "以下の場合に使用してください：\n" +
                "• AIからの応答がない\n" +
                "• UnityツールがOperation timeoutになる\n" +
                "• 接続が不安定になった\n\n" +
                "再接続を実行しますか？",
                "再接続する",
                "キャンセル"
            );
            
            if (reconnect)
            {
                ReconnectToMCPServer();
                EditorUtility.DisplayDialog(
                    "再接続完了",
                    "Claudeなどとの接続を再確立しました。\n" +
                    "コンソールログで接続状態を確認してください。",
                    "OK"
                );
            }
        }
        
        /// <summary>
        /// シンプルな再接続ボタン（ツールバー用）
        /// </summary>
        // [MenuItem("Tools/🔗 AI再接続", false, 0)]
        public static void QuickReconnect()
        {
            Debug.Log("[Nexus Editor MCP] Quick reconnect requested");
            ReconnectToMCPServer();
        }
        
        /// <summary>
        /// 自動再接続設定の切り替え
        /// </summary>
        // [MenuItem("Nexus/⚙️ 自動再接続 ON/OFF", false, 3)] // NexusMenuManager.cs に移動
        public static void ToggleAutoReconnect()
        {
            enableAutoReconnect = !enableAutoReconnect;
            EditorPrefs.SetBool(autoReconnectKey, enableAutoReconnect);
            
            string status = enableAutoReconnect ? "有効" : "無効";
            string message = $"自動再接続機能を{status}にしました。\n\n" +
                           "有効時：Play Mode終了後に自動的にAI接続を復帰\n" +
                           "無効時：手動で再接続ボタンを使用";
            
            Debug.Log($"[Nexus Editor MCP] Auto-reconnect: {status}");
            EditorUtility.DisplayDialog("自動再接続設定", message, "OK");
        }
        
        // [MenuItem("Nexus/⚙️ 自動再接続 ON/OFF", true)] // NexusMenuManager.cs に移動
        public static bool ToggleAutoReconnectValidate()
        {
            Menu.SetChecked("Nexus/⚙️ 自動再接続 ON/OFF", enableAutoReconnect);
            return true;
        }

        /// <summary>
        /// Claude Desktop設定ファイルのポートを更新
        /// </summary>
        private static void UpdateClaudeDesktopConfigForPort(int newPort)
        {
            try
            {
                string configPath = System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                    "Library", "Application Support", "Claude", "claude_desktop_config.json"
                );
                
                if (!System.IO.File.Exists(configPath))
                {
                    Debug.LogWarning($"[Nexus Editor MCP] Claude Desktop設定ファイルが見つかりません: {configPath}");
                    return;
                }

                string configContent = System.IO.File.ReadAllText(configPath);
                
                // WebSocketポートを更新（複数のパターンに対応）
                bool updated = false;
                string newPattern = $"ws://localhost:{newPort}";
                
                // 既知のポートパターンをすべてチェック
                string[] oldPatterns = {
                    "ws://localhost:8080",
                    "ws://localhost:8081", 
                    "ws://localhost:8082",
                    "ws://localhost:8083",
                    "ws://localhost:8084"
                };
                
                foreach (string oldPattern in oldPatterns)
                {
                    if (configContent.Contains(oldPattern) && oldPattern != newPattern)
                    {
                        configContent = configContent.Replace(oldPattern, newPattern);
                        updated = true;
                        Debug.Log($"[Nexus Editor MCP] 🔄 Claude Desktop設定を自動更新: {oldPattern} → {newPattern}");
                    }
                }
                
                if (updated)
                {
                    
                    // バックアップを作成
                    string backupPath = configPath + $".backup_{System.DateTime.Now:yyyyMMdd_HHmmss}";
                    System.IO.File.Copy(configPath, backupPath);
                    
                    // 新しい設定を書き込み
                    System.IO.File.WriteAllText(configPath, configContent);
                    
                    Debug.Log($"[Nexus Editor MCP] 🔄 Claude Desktop設定を自動更新しました");
                    Debug.Log($"[Nexus Editor MCP] 📁 バックアップ: {backupPath}");
                    Debug.Log($"[Nexus Editor MCP] ⚠️ Claude Desktopの再起動が必要です");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Nexus Editor MCP] Claude Desktop設定更新エラー: {e.Message}");
            }
        }
        
        
        /// <summary>
        /// 軽い接続テスト（WebSocket状態確認のみ）
        /// </summary>
        private static async Task LightConnectionTest()
        {
            try
            {
                if (webSocket?.State == WebSocketState.Open)
                {
                    // WebSocketは開いているが実際の通信をテスト
                    var testMessage = JsonConvert.SerializeObject(new { type = "ping", id = "test" });
                    var buffer = Encoding.UTF8.GetBytes(testMessage);
                    await webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    
                    // 接続確認成功
                    isConnected = true;
                    reconnectPhase = 0;
                    return;
                }
                else if (webSocket?.State == WebSocketState.Closed || webSocket?.State == WebSocketState.Aborted)
                {
                    // Closed状態の場合は即座に本格再接続へ
                    Debug.Log("[Nexus MCP] WebSocket is closed, skipping to full reconnection");
                    reconnectPhase = 2;
                    lastReconnectTime = Time.realtimeSinceStartup;
                    return;
                }
            }
            catch (Exception)
            {
                // 軽いテスト失敗、次のフェーズに進む
            }
        }
        
        /// <summary>
        /// 本格的な再接続試行
        /// </summary>
        private static async Task FullReconnectAttempt()
        {
            isReconnecting = true;
            try
            {
                // 既存接続をクリーンアップ
                if (webSocket != null)
                {
                    try { webSocket.Dispose(); } catch { }
                    webSocket = null;
                }
                
                await Task.Delay(1000); // 1秒待機
                
                // 新しい接続を試行
                await ConnectToMCPServer();
                
                if (isConnected)
                {
                    reconnectPhase = 0;
                    Debug.Log("[Nexus MCP] 🔄 自動再接続成功");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Nexus MCP] 再接続失敗: {e.Message}");
            }
            finally
            {
                isReconnecting = false;
            }
        }
        /// <summary>
        /// 失敗後のリトライ
        /// </summary>
        private static async Task RetryReconnect()
        {
            isReconnecting = true;
            try
            {
                // ポートを再スキャンして再接続
                DetectAndSetAvailablePort();
                await Task.Delay(500);
                await ConnectToMCPServer();
                
                if (isConnected)
                {
                    reconnectPhase = 0;
                    Debug.Log("[Nexus MCP] 🔄 リトライ再接続成功");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Nexus MCP] リトライ失敗: {e.Message}");
            }
            finally
            {
                isReconnecting = false;
            }
        }
        
        /// <summary>
        /// 接続切断を検出した時の初期化
        /// </summary>
        private static void OnConnectionLost()
        {
            isConnected = false;
            lastConnectionCheckTime = Time.realtimeSinceStartup;
            reconnectPhase = 0;
        }
    }
}















