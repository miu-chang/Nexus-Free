using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace NexusAIConnect
{
    /// <summary>
    /// エディターモード用のWebSocketクライアント
    /// MCPサーバーとの通信を管理
    /// </summary>
    public class NexusWebSocketClient
    {
        private ClientWebSocket webSocket;
        private CancellationTokenSource cancellationTokenSource;
        private bool isConnected = false;
        private Queue<string> messageQueue = new Queue<string>();
        private bool shouldReconnect = true;
        private int reconnectAttempts = 0;
        private const int maxReconnectAttempts = 5;
        private const int reconnectDelay = 3000; // 3秒
        private string serverUrl = "ws://localhost:8080";
        
        public bool IsConnected => isConnected;
        public event Action<string> OnMessageReceived;
        public event Action OnConnected;
        public event Action OnDisconnected;
        
        private static NexusWebSocketClient instance;
        public static NexusWebSocketClient Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new NexusWebSocketClient();
                }
                return instance;
            }
        }
        
        public async Task<bool> Connect(string url = "ws://localhost:8080")
        {
            shouldReconnect = true;
            reconnectAttempts = 0;
            
            while (shouldReconnect && reconnectAttempts < maxReconnectAttempts)
            {
                try
                {
                    Debug.Log($"[Nexus WebSocket] Connecting to {url}... (Attempt {reconnectAttempts + 1})");
                    
                    webSocket = new ClientWebSocket();
                    cancellationTokenSource = new CancellationTokenSource();
                    
                    await webSocket.ConnectAsync(new Uri(url), cancellationTokenSource.Token);
                    isConnected = true;
                    reconnectAttempts = 0; // 成功したらリセット
                    
                    Debug.Log("[Nexus WebSocket] Connected successfully!");
                    OnConnected?.Invoke();
                    
                    // メッセージ受信ループを開始
                    _ = Task.Run(async () => await ReceiveLoop());
                    
                    // ハートビートを開始
                    _ = Task.Run(async () => await HeartbeatLoop());
                    
                    // Unity側の準備完了を通知
                    await SendMessage(new { type = "unity_ready", version = "1.0.0" });
                    
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Nexus WebSocket] Connection failed: {e.Message}");
                    isConnected = false;
                    reconnectAttempts++;
                    
                    if (reconnectAttempts < maxReconnectAttempts && shouldReconnect)
                    {
                        Debug.Log($"[Nexus WebSocket] Retrying in {reconnectDelay / 1000} seconds...");
                        await Task.Delay(reconnectDelay);
                    }
                }
            }
            
            return false;
        }
        
        private async Task ReceiveLoop()
        {
            var buffer = new byte[4096];
            
            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    var result = await webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), 
                        cancellationTokenSource.Token
                    );
                    
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Debug.Log($"[Nexus WebSocket] Received: {message}");
                        
                        lock (messageQueue)
                        {
                            messageQueue.Enqueue(message);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nexus WebSocket] Receive error: {e.Message}");
            }
            finally
            {
                isConnected = false;
                OnDisconnected?.Invoke();
            }
        }
        
        public void ProcessMessages()
        {
            lock (messageQueue)
            {
                while (messageQueue.Count > 0)
                {
                    var message = messageQueue.Dequeue();
                    OnMessageReceived?.Invoke(message);
                    
                    try
                    {
                        var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);
                        if (data != null && data.ContainsKey("type"))
                        {
                            ProcessUnityCommand(data);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Nexus WebSocket] Message processing error: {e.Message}");
                    }
                }
            }
        }
        
        public event Action<string, string> OnClaudeResponse; // sessionId, message
        public event Action<string> OnChatStatusUpdate; // status message
        
        private void ProcessUnityCommand(Dictionary<string, object> data)
        {
            var type = data["type"].ToString();
            
            if (type == "unity_operation")
            {
                var command = data.ContainsKey("command") ? data["command"].ToString() : "";
                var parameters = data.ContainsKey("parameters") ? data["parameters"] as Newtonsoft.Json.Linq.JObject : null;
                
                Debug.Log($"[Nexus WebSocket] Executing Unity command: {command}");
                
                // エディターモードでUnity操作を実行
                EditorApplication.delayCall += () =>
                {
                    ExecuteUnityOperation(command, parameters);
                };
            }
            else if (type == "claude_response")
            {
                // Claude Desktopからのリアルタイム応答
                var responseData = data.ContainsKey("data") ? data["data"] as Newtonsoft.Json.Linq.JObject : null;
                if (responseData != null)
                {
                    var message = responseData.Value<string>("message") ?? "";
                    var sessionId = responseData.Value<string>("sessionId") ?? "";
                    var responseType = responseData.Value<string>("responseType") ?? "response";
                    
                    Debug.Log($"[Nexus WebSocket] Claude response received: {message}");
                    
                    // メインスレッドでイベント発火
                    EditorApplication.delayCall += () =>
                    {
                        OnClaudeResponse?.Invoke(sessionId, message);
                    };
                }
            }
            else if (type == "chat_initiated")
            {
                // チャット開始通知
                var chatData = data.ContainsKey("data") ? data["data"] as Newtonsoft.Json.Linq.JObject : null;
                if (chatData != null)
                {
                    var status = chatData.Value<string>("status") ?? "処理中...";
                    
                    Debug.Log($"[Nexus WebSocket] Chat initiated: {status}");
                    
                    EditorApplication.delayCall += () =>
                    {
                        OnChatStatusUpdate?.Invoke(status);
                    };
                }
            }
        }
        
        private async void ExecuteUnityOperation(string command, Newtonsoft.Json.Linq.JObject parameters)
        {
            Debug.Log($"[Nexus WebSocket] Executing Unity operation: {command}");
            Debug.Log($"[Nexus WebSocket] Parameters: {parameters?.ToString()}");
            
            var operationId = parameters?.Value<string>("operationId") ?? Guid.NewGuid().ToString();
            var response = new Dictionary<string, object>
            {
                ["type"] = "operation_response",
                ["operationId"] = operationId,
                ["command"] = command
            };
            
            try
            {
                var operation = new NexusUnityOperation
                {
                    type = ConvertCommandToOperationType(command),
                    parameters = new Dictionary<string, string>()
                };
                
                // パラメータを変換
                if (parameters != null)
                {
                    foreach (var prop in parameters.Properties())
                    {
                        if (prop.Name == "operationId") continue;
                        
                        var value = prop.Value;
                        
                        if (value is Newtonsoft.Json.Linq.JObject jObj)
                        {
                            // ネストされたオブジェクト（Vector3など）の処理
                            if (jObj.ContainsKey("x") && jObj.ContainsKey("y") && jObj.ContainsKey("z"))
                            {
                                operation.parameters[prop.Name] = $"{jObj["x"]},{jObj["y"]},{jObj["z"]}";
                            }
                            else if (jObj.ContainsKey("x") && jObj.ContainsKey("y"))
                            {
                                operation.parameters[prop.Name] = $"{jObj["x"]},{jObj["y"]}";
                            }
                            else
                            {
                                operation.parameters[prop.Name] = value.ToString();
                            }
                        }
                        else
                        {
                            operation.parameters[prop.Name] = value.ToString();
                        }
                    }
                }
                
                // 実行
                string result = "";
                bool success = true;
                
                // 情報取得系コマンドの処理
                switch (operation.type)
                {
                    case "GET_SCENE_INFO":
                    case "GET_CAMERA_INFO":
                    // State Inspector features disabled in UI Edition
                    case "GET_TERRAIN_INFO":
                    case "GET_LIGHTING_INFO":
                    case "GET_MATERIAL_INFO":
                    case "GET_UI_INFO":
                    case "GET_PHYSICS_INFO":
                    case "GET_GAMEOBJECT_DETAILS":
                    case "GET_PROJECT_STATS":
                        result = JsonConvert.SerializeObject(new
                        {
                            success = false,
                            message = "State inspection features are not available in UI Edition"
                        });
                        break;
                    default:
                        // 通常の操作
                        var executor = new NexusUnityExecutor();
                        result = executor.ExecuteOperation(operation).Result;
                        
                        // エラーチェック
                        if (result.StartsWith("Error") || result.Contains("not found") || result.Contains("failed"))
                        {
                            success = false;
                        }
                        break;
                }
                
                response["success"] = success;
                response["result"] = result;
                
                Debug.Log($"[Nexus WebSocket] Operation result: {result}");
            }
            catch (Exception e)
            {
                response["success"] = false;
                response["error"] = e.Message;
                response["stackTrace"] = e.StackTrace;
                response["errorType"] = e.GetType().Name;
                
                Debug.LogError($"[Nexus WebSocket] Operation execution error: {e.Message}\n{e.StackTrace}");
            }
            
            // 結果をMCPサーバーに送信
            await SendMessage(response);
        }
        
        private string ConvertCommandToOperationType(string command)
        {
            switch (command)
            {
                case "create_ui":
                    return "CREATE_UI";
                case "create_gameobject":
                    return "CREATE_GAMEOBJECT";
                case "set_transform":
                    return "SET_PROPERTY";
                case "setup_camera":
                    return "SETUP_CAMERA";
                case "create_particle_system":
                    return "CREATE_PARTICLE_SYSTEM";
                case "setup_navmesh":
                    return "SETUP_NAVMESH";
                case "create_audio_mixer":
                    return "CREATE_AUDIO_MIXER";
                case "undo":
                    return "UNDO";
                case "redo":
                    return "REDO";
                case "get_history":
                    return "GET_HISTORY";
                    
                // 情報取得系
                case "get_scene_info":
                    return "GET_SCENE_INFO";
                case "get_camera_info":
                    return "GET_CAMERA_INFO";
                case "get_terrain_info":
                    return "GET_TERRAIN_INFO";
                case "get_lighting_info":
                    return "GET_LIGHTING_INFO";
                case "get_material_info":
                    return "GET_MATERIAL_INFO";
                case "get_ui_info":
                    return "GET_UI_INFO";
                case "get_physics_info":
                    return "GET_PHYSICS_INFO";
                case "get_gameobject_details":
                    return "GET_GAMEOBJECT_DETAILS";
                case "list_assets":
                    return "LIST_ASSETS";
                case "get_project_stats":
                    return "GET_PROJECT_STATS";
                    
                default:
                    return command.ToUpper();
            }
        }
        
        public async Task SendMessage(object data)
        {
            if (!isConnected || webSocket.State != WebSocketState.Open)
            {
                Debug.LogWarning("[Nexus WebSocket] Cannot send message: not connected");
                return;
            }
            
            try
            {
                var json = JsonConvert.SerializeObject(data);
                var bytes = Encoding.UTF8.GetBytes(json);
                
                await webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    cancellationTokenSource.Token
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nexus WebSocket] Send error: {e.Message}");
            }
        }
        
        private async Task HeartbeatLoop()
        {
            while (isConnected && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // 30秒ごとにハートビート送信
                    await Task.Delay(30000, cancellationTokenSource.Token);
                    
                    if (webSocket.State == WebSocketState.Open)
                    {
                        await SendMessage(new { type = "heartbeat", timestamp = DateTime.Now.Ticks });
                    }
                    else
                    {
                        Debug.LogWarning("[Nexus WebSocket] Connection lost during heartbeat");
                        isConnected = false;
                        OnDisconnected?.Invoke();
                        
                        // 再接続を試行
                        if (shouldReconnect)
                        {
                            _ = Task.Run(async () => await Connect());
                        }
                        break;
                    }
                }
                catch (Exception e)
                {
                    if (!cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        Debug.LogError($"[Nexus WebSocket] Heartbeat error: {e.Message}");
                    }
                    break;
                }
            }
        }
        
        public async Task Disconnect()
        {
            shouldReconnect = false; // 自動再接続を無効化
            
            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                try
                {
                    await webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure, 
                        "Closing", 
                        cancellationTokenSource.Token
                    );
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Nexus WebSocket] Disconnect error: {e.Message}");
                }
            }
            
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            isConnected = false;
        }

        /// <summary>
        /// サーバーURLを設定
        /// </summary>
        public void SetServerUrl(string url)
        {
            serverUrl = url;
            Debug.Log($"[Nexus WebSocket] Server URL changed to: {url}");
        }
    }
    
    /// <summary>
    /// WebSocketクライアントの更新を管理
    /// </summary>
    [InitializeOnLoad]
    public static class NexusWebSocketUpdater
    {
        static NexusWebSocketUpdater()
        {
            EditorApplication.update += Update;
        }
        
        private static void Update()
        {
            NexusWebSocketClient.Instance?.ProcessMessages();
        }
    }
}