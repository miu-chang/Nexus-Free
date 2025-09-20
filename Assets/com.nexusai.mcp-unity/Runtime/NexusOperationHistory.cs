using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json;

namespace NexusAIConnect
{
    /// <summary>
    /// Unity操作の履歴管理とUndo/Redo機能
    /// </summary>
    public class NexusOperationHistory
    {
        private static NexusOperationHistory instance;
        public static NexusOperationHistory Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new NexusOperationHistory();
                }
                return instance;
            }
        }

        private Stack<OperationRecord> undoStack = new Stack<OperationRecord>();
        private Stack<OperationRecord> redoStack = new Stack<OperationRecord>();
        private const int maxHistorySize = 50;

        public event Action OnHistoryChanged;

        [Serializable]
        public class OperationRecord
        {
            public string id;
            public string operationType;
            public string description;
            public DateTime timestamp;
            public Dictionary<string, object> parameters;
            public OperationState beforeState;
            public OperationState afterState;
            public bool canUndo;
            public string error;
        }

        [Serializable]
        public class OperationState
        {
            public List<GameObjectState> gameObjects = new List<GameObjectState>();
            public Dictionary<string, string> globalState = new Dictionary<string, string>();
        }

        [Serializable]
        public class GameObjectState
        {
            public string name;
            public string path;
            public bool active;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public List<ComponentState> components = new List<ComponentState>();
        }

        [Serializable]
        public class ComponentState
        {
            public string type;
            public Dictionary<string, object> properties = new Dictionary<string, object>();
        }

        /// <summary>
        /// 操作を記録
        /// </summary>
        public void RecordOperation(string operationType, string description, 
            Dictionary<string, object> parameters, Action operation)
        {
            var record = new OperationRecord
            {
                id = Guid.NewGuid().ToString(),
                operationType = operationType,
                description = description,
                timestamp = DateTime.Now,
                parameters = parameters,
                canUndo = true
            };

            // 操作前の状態をキャプチャ
            record.beforeState = CaptureState(parameters);

            try
            {
                // 操作を実行
                operation?.Invoke();

                // 操作後の状態をキャプチャ
                record.afterState = CaptureState(parameters);

                // 履歴に追加
                AddToHistory(record);

                Debug.Log($"[Operation History] Recorded: {description}");
            }
            catch (Exception e)
            {
                record.error = e.Message;
                record.canUndo = false;
                Debug.LogError($"[Operation History] Operation failed: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// 現在の状態をキャプチャ
        /// </summary>
        private OperationState CaptureState(Dictionary<string, object> parameters)
        {
            var state = new OperationState();

            // 対象のGameObjectを特定
            if (parameters != null && parameters.ContainsKey("target"))
            {
                var targetName = parameters["target"].ToString();
                var target = GameObject.Find(targetName);
                
                if (target != null)
                {
                    state.gameObjects.Add(CaptureGameObjectState(target));
                }
            }

            // 新規作成されたオブジェクトの場合
            if (parameters != null && parameters.ContainsKey("name"))
            {
                var name = parameters["name"].ToString();
                var obj = GameObject.Find(name);
                
                if (obj != null)
                {
                    state.gameObjects.Add(CaptureGameObjectState(obj));
                }
            }

            // グローバル状態（シーン設定など）
            state.globalState["activeScene"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            return state;
        }

        /// <summary>
        /// GameObjectの状態をキャプチャ
        /// </summary>
        private GameObjectState CaptureGameObjectState(GameObject obj)
        {
            var state = new GameObjectState
            {
                name = obj.name,
                path = GetFullPath(obj),
                active = obj.activeSelf,
                position = obj.transform.position,
                rotation = obj.transform.rotation,
                scale = obj.transform.localScale
            };

            // コンポーネントの状態
            foreach (var component in obj.GetComponents<Component>())
            {
                if (component == null || component is Transform) continue;

                var compState = new ComponentState
                {
                    type = component.GetType().FullName
                };

                // 主要なコンポーネントのプロパティを保存
                if (component is Rigidbody rb)
                {
                    compState.properties["mass"] = rb.mass;
                    compState.properties["useGravity"] = rb.useGravity;
                    compState.properties["isKinematic"] = rb.isKinematic;
                }
                else if (component is Collider col)
                {
                    compState.properties["isTrigger"] = col.isTrigger;
                    compState.properties["enabled"] = col.enabled;
                }
                else if (component is Renderer rend)
                {
                    compState.properties["enabled"] = rend.enabled;
                    compState.properties["materialCount"] = rend.sharedMaterials.Length;
                }

                state.components.Add(compState);
            }

            return state;
        }

        /// <summary>
        /// 履歴に追加
        /// </summary>
        private void AddToHistory(OperationRecord record)
        {
            undoStack.Push(record);
            redoStack.Clear(); // 新しい操作があったらRedoスタックをクリア

            // 最大履歴数を超えたら古いものを削除
            while (undoStack.Count > maxHistorySize)
            {
                var oldRecords = undoStack.ToArray();
                undoStack.Clear();
                for (int i = 1; i < oldRecords.Length; i++)
                {
                    undoStack.Push(oldRecords[i]);
                }
            }

            OnHistoryChanged?.Invoke();
        }

        /// <summary>
        /// Undo実行
        /// </summary>
        public bool Undo()
        {
            if (undoStack.Count == 0) return false;

            var record = undoStack.Pop();
            
            if (!record.canUndo)
            {
                Debug.LogWarning($"[Operation History] Cannot undo: {record.description}");
                return false;
            }

            try
            {
                // 状態を復元
                RestoreState(record.beforeState);

                // Redoスタックに追加
                redoStack.Push(record);

                Debug.Log($"[Operation History] Undo: {record.description}");
                OnHistoryChanged?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Operation History] Undo failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Redo実行
        /// </summary>
        public bool Redo()
        {
            if (redoStack.Count == 0) return false;

            var record = redoStack.Pop();

            try
            {
                // 状態を復元
                RestoreState(record.afterState);

                // Undoスタックに追加
                undoStack.Push(record);

                Debug.Log($"[Operation History] Redo: {record.description}");
                OnHistoryChanged?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Operation History] Redo failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 状態を復元
        /// </summary>
        private void RestoreState(OperationState state)
        {
            if (state == null) return;

            foreach (var objState in state.gameObjects)
            {
                var obj = GameObject.Find(objState.path);
                
                if (obj != null)
                {
                    // Transform復元
                    obj.transform.position = objState.position;
                    obj.transform.rotation = objState.rotation;
                    obj.transform.localScale = objState.scale;
                    obj.SetActive(objState.active);

                    // コンポーネント状態の復元
                    foreach (var compState in objState.components)
                    {
                        var component = obj.GetComponent(compState.type);
                        if (component != null)
                        {
                            RestoreComponentState(component, compState.properties);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// コンポーネントの状態を復元
        /// </summary>
        private void RestoreComponentState(Component component, Dictionary<string, object> properties)
        {
            if (component is Rigidbody rb && properties != null)
            {
                if (properties.ContainsKey("mass")) rb.mass = Convert.ToSingle(properties["mass"]);
                if (properties.ContainsKey("useGravity")) rb.useGravity = Convert.ToBoolean(properties["useGravity"]);
                if (properties.ContainsKey("isKinematic")) rb.isKinematic = Convert.ToBoolean(properties["isKinematic"]);
            }
            else if (component is Collider col && properties != null)
            {
                if (properties.ContainsKey("isTrigger")) col.isTrigger = Convert.ToBoolean(properties["isTrigger"]);
                if (properties.ContainsKey("enabled")) col.enabled = Convert.ToBoolean(properties["enabled"]);
            }
            else if (component is Renderer rend && properties != null)
            {
                if (properties.ContainsKey("enabled")) rend.enabled = Convert.ToBoolean(properties["enabled"]);
            }
        }

        /// <summary>
        /// 履歴をクリア
        /// </summary>
        public void ClearHistory()
        {
            undoStack.Clear();
            redoStack.Clear();
            OnHistoryChanged?.Invoke();
        }

        /// <summary>
        /// 履歴情報を取得
        /// </summary>
        public string GetHistoryInfo()
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine($"📝 操作履歴");
            info.AppendLine($"Undo可能: {undoStack.Count}個");
            info.AppendLine($"Redo可能: {redoStack.Count}個");

            if (undoStack.Count > 0)
            {
                info.AppendLine("\n最近の操作:");
                var recent = undoStack.ToArray();
                for (int i = 0; i < Math.Min(5, recent.Length); i++)
                {
                    var record = recent[i];
                    info.AppendLine($"  - {record.description} ({record.timestamp:HH:mm:ss})");
                }
            }

            return info.ToString();
        }

        /// <summary>
        /// 履歴をJSON形式でエクスポート
        /// </summary>
        public string ExportHistory()
        {
            var history = new
            {
                undoStack = undoStack.ToArray(),
                redoStack = redoStack.ToArray(),
                exportTime = DateTime.Now
            };

            return JsonConvert.SerializeObject(history, Formatting.Indented);
        }

        private string GetFullPath(GameObject obj)
        {
            var path = obj.name;
            var parent = obj.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }

        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;
        
        // チェックポイント機能
        private Dictionary<string, CheckpointData> checkpoints = new Dictionary<string, CheckpointData>();
        
        [Serializable]
        public class CheckpointData
        {
            public string name;
            public string description;
            public DateTime timestamp;
            public Stack<OperationRecord> undoStackSnapshot;
            public Stack<OperationRecord> redoStackSnapshot;
        }
        
        /// <summary>
        /// チェックポイントを作成
        /// </summary>
        public bool CreateCheckpoint(string name, string description = "")
        {
            try
            {
                var checkpoint = new CheckpointData
                {
                    name = name,
                    description = description,
                    timestamp = DateTime.Now,
                    undoStackSnapshot = new Stack<OperationRecord>(undoStack.Reverse()),
                    redoStackSnapshot = new Stack<OperationRecord>(redoStack.Reverse())
                };
                
                checkpoints[name] = checkpoint;
                
                Debug.Log($"[Operation History] Checkpoint '{name}' created");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Operation History] Failed to create checkpoint: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// チェックポイントから復元
        /// </summary>
        public bool RestoreCheckpoint(string name)
        {
            try
            {
                if (!checkpoints.ContainsKey(name))
                {
                    Debug.LogWarning($"[Operation History] Checkpoint '{name}' not found");
                    return false;
                }
                
                var checkpoint = checkpoints[name];
                
                // スタックを復元
                undoStack = new Stack<OperationRecord>(checkpoint.undoStackSnapshot.Reverse());
                redoStack = new Stack<OperationRecord>(checkpoint.redoStackSnapshot.Reverse());
                
                OnHistoryChanged?.Invoke();
                
                Debug.Log($"[Operation History] Restored to checkpoint '{name}' ({checkpoint.timestamp})");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Operation History] Failed to restore checkpoint: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 利用可能なチェックポイント一覧を取得
        /// </summary>
        public string GetCheckpoints()
        {
            if (checkpoints.Count == 0)
                return "No checkpoints available";
                
            var result = new System.Text.StringBuilder();
            result.AppendLine("📍 Available Checkpoints:");
            
            foreach (var kvp in checkpoints.OrderByDescending(x => x.Value.timestamp))
            {
                var cp = kvp.Value;
                result.AppendLine($"  - {cp.name} ({cp.timestamp:yyyy-MM-dd HH:mm:ss})");
                if (!string.IsNullOrEmpty(cp.description))
                    result.AppendLine($"    {cp.description}");
            }
            
            return result.ToString();
        }
    }
}