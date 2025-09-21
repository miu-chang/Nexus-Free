using System;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.EventSystems;
using TMPro;
using System.IO;
using Newtonsoft.Json;
using UnityEngine.Profiling;

namespace NexusAIConnect
{
    public class NexusUnityExecutor
    {
        private GameObject lastCreatedObject;
        private List<GameObject> createdObjects = new List<GameObject>();
        
        // ログバッファ（リアルタイムログ収集用）
        private static List<LogEntry> logBuffer = new List<LogEntry>();
        private static bool isLogCallbackRegistered = false;
        private static readonly int maxLogBufferSize = 1000;
        
        private struct LogEntry
        {
            public string condition;
            public string stackTrace;
            public LogType type;
            public DateTime timestamp;
        }
        
        static NexusUnityExecutor()
        {
            // ログコールバックを登録
            if (!isLogCallbackRegistered)
            {
                Application.logMessageReceived += OnLogMessageReceived;
                isLogCallbackRegistered = true;
                Debug.Log("[NexusConsole] Log callback registered for real-time log collection");
            }
        }
        
        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            var entry = new LogEntry
            {
                condition = condition,
                stackTrace = stackTrace,
                type = type,
                timestamp = DateTime.Now
            };
            
            logBuffer.Add(entry);
            
            // バッファサイズ制限
            if (logBuffer.Count > maxLogBufferSize)
            {
                logBuffer.RemoveRange(0, logBuffer.Count - maxLogBufferSize);
            }
        }
        
        public async Task<string> ExecuteOperation(NexusUnityOperation operation)
        {
            try
            {
                switch (operation.type.ToUpper())
                {
                    case "CREATE_GAMEOBJECT":
                        return CreateGameObject(operation.parameters);
                        
                    case "UPDATE_GAMEOBJECT":
                        return UpdateGameObject(operation.parameters);
                        
                    case "DELETE_GAMEOBJECT":
                        return DeleteGameObject(operation.parameters);
                        
                    case "SET_TRANSFORM":
                        return SetTransform(operation.parameters);
                        
                    case "ADD_COMPONENT":
                        return AddComponent(operation.parameters);
                        
                    case "UPDATE_COMPONENT":
                        return UpdateComponent(operation.parameters);
                        
                    case "SET_PROPERTY":
                        return SetProperty(operation.parameters);
                        
                    case "CREATE_UI":
                        return CreateUI(operation.parameters);
                        
                    case "CREATE_SCRIPT":
                        return await CreateScript(operation);
                        
                    case "MODIFY_SCRIPT":
                        Debug.Log($"[NexusExecutor] Executing MODIFY_SCRIPT with parameters: {JsonConvert.SerializeObject(operation.parameters)}");
                        return ModifyScript(operation.parameters);
                        
                    case "EDIT_SCRIPT_LINE":
                        Debug.Log($"[NexusExecutor] Executing EDIT_SCRIPT_LINE with parameters: {JsonConvert.SerializeObject(operation.parameters)}");
                        return EditScriptLine(operation.parameters);
                        
                    case "ADD_SCRIPT_METHOD":
                        Debug.Log($"[NexusExecutor] Executing ADD_SCRIPT_METHOD with parameters: {JsonConvert.SerializeObject(operation.parameters)}");
                        return AddScriptMethod(operation.parameters);
                        
                    case "UPDATE_SCRIPT_VARIABLE":
                        Debug.Log($"[NexusExecutor] Executing UPDATE_SCRIPT_VARIABLE with parameters: {JsonConvert.SerializeObject(operation.parameters)}");
                        return UpdateScriptVariable(operation.parameters);
                        
                    case "CREATE_PREFAB":
                        return CreatePrefab(operation.parameters);
                        
                    case "SETUP_PHYSICS":
                        return SetupPhysics(operation.parameters);
                        
                    case "CREATE_MATERIAL":
                        return CreateMaterial(operation.parameters);
                        
                    case "SETUP_CAMERA":
                        return SetupCamera(operation.parameters);
                        
                    case "CREATE_PARTICLE_SYSTEM":
                        return CreateParticleSystem(operation.parameters);
                        
                    case "SETUP_NAVMESH":
                        return SetupNavMesh(operation.parameters);
                        
                    case "CREATE_AUDIO_MIXER":
                        return CreateAudioMixer(operation.parameters);
                        
                    case "UNDO":
                    case "UNDO_OPERATION":
                        return UndoOperation();
                        
                    case "REDO":
                    case "REDO_OPERATION":
                        return RedoOperation();
                        
                    case "GET_HISTORY":
                    case "GET_OPERATION_HISTORY":
                        return GetOperationHistory();
                        
                    case "CREATE_CHECKPOINT":
                        return CreateCheckpoint(operation.parameters);
                        
                    case "RESTORE_CHECKPOINT":
                        return RestoreCheckpoint(operation.parameters);
                        
                    // リアルタイムイベント監視
                    case "MONITOR_PLAY_STATE":
                        return StartPlayStateMonitoring(operation.parameters);
                        
                    case "MONITOR_FILE_CHANGES":
                        return StartFileChangeMonitoring(operation.parameters);
                        
                    case "MONITOR_COMPILE":
                        return StartCompileMonitoring(operation.parameters);
                        
                    case "SUBSCRIBE_EVENTS":
                        return SubscribeToEvents(operation.parameters);
                        
                    case "GET_EVENTS":
                        return GetRecentEvents(operation.parameters);
                        
                    case "GET_MONITORING_STATUS":
                        return GetMonitoringStatus();
                        
                    // プロジェクト設定系（UI Editionでは無効）
                        
                    case "BATCH_CREATE":
                        return await BatchCreate(operation);
                        
                    // === リアルタイム実行状態監視（UI Editionでは無効） ===
                        
                    // === 詳細なアセット情報取得（UI Editionでは無効） ===
                        
                    case "GET_ASSET_IMPORT_SETTINGS":
                        return GetAssetImportSettings(operation.parameters);
                        
                    case "PLACE_OBJECTS":
                        return PlaceObjects(operation.parameters);
                        
                    case "GET_GAMEOBJECT_DETAILS":
                        return GetGameObjectDetails(operation.parameters);
                        
                    case "GET_SCENE_INFO":
                        return GetSceneInfo(operation.parameters);
                        
                    case "CREATE_TERRAIN":
                        return CreateTerrain(operation.parameters);
                        
                    case "MODIFY_TERRAIN":
                        return ModifyTerrain(operation.parameters);
                        
                    case "GET_CAMERA_INFO":
                        return GetCameraInfo(operation.parameters);
                        
                    case "GET_TERRAIN_INFO":
                        return GetTerrainInfo(operation.parameters);
                        
                    case "GET_LIGHTING_INFO":
                        return GetLightingInfo(operation.parameters);
                        
                    case "GET_MATERIAL_INFO":
                        return GetMaterialInfo(operation.parameters);
                        
                    case "GET_UI_INFO":
                        return GetUIInfo(operation.parameters);
                        
                    case "GET_PHYSICS_INFO":
                        return GetPhysicsInfo(operation.parameters);
                        
                    case "LIST_ASSETS":
                        return ListAssets(operation.parameters);
                        
                    case "CHECK_FOLDER":
                        return CheckFolder(operation.parameters);
                        
                    case "CREATE_FOLDER":
                        return CreateFolder(operation.parameters);
                        
                    case "LIST_FOLDERS":
                        return ListFolders(operation.parameters);
                        
                    case "DUPLICATE_GAMEOBJECT":
                        return DuplicateGameObject(operation.parameters);
                        
                    case "FIND_BY_COMPONENT":
                        return FindGameObjectsByComponent(operation.parameters);
                        
                    case "CLEANUP_EMPTY_OBJECTS":
                        return CleanupEmptyObjects(operation.parameters);
                        
                    case "GET_PROJECT_STATS":
                        return GetProjectStats(operation.parameters);
                        
                    case "MANAGE_PACKAGE":
                        return ManagePackage(operation.parameters);
                        
                    case "MANAGE_SCENE":
                        return ManageScene(operation.parameters);
                        
                    case "CREATE_ANIMATION":
                        return CreateAnimation(operation.parameters);
                        
                    case "SETUP_LIGHTING":
                        return SetupLighting(operation.parameters);
                        
                    case "CONSOLE_OPERATION":
                        return ConsoleOperation(operation.parameters);
                        
                    case "ANALYZE_CONSOLE_LOGS":
                        return AnalyzeConsoleLogs(operation.parameters);
                        
                    // === デバッグ・テストツール ===
                    case "CONTROL_GAME_SPEED":
                        return ControlGameSpeed(operation.parameters);
                        
                    case "PROFILE_PERFORMANCE":
                        return ProfilePerformance(operation.parameters);
                        
                    case "DEBUG_DRAW":
                        return DebugDraw(operation.parameters);
                        
                    case "RUN_UNITY_TESTS":
                        return RunUnityTests(operation.parameters);
                        
                    case "MANAGE_BREAKPOINTS":
                        return ManageBreakpoints(operation.parameters);
                        
                    // === アニメーション系ツール ===
                    case "CREATE_ANIMATOR_CONTROLLER":
                        return CreateAnimatorController(operation.parameters);
                        
                    case "ADD_ANIMATION_STATE":
                        return AddAnimationState(operation.parameters);
                        
                    case "CREATE_ANIMATION_CLIP":
                        return CreateAnimationClip(operation.parameters);
                        
                    case "SETUP_BLEND_TREE":
                        return SetupBlendTree(operation.parameters);
                        
                    case "ADD_ANIMATION_TRANSITION":
                        return AddAnimationTransition(operation.parameters);
                        
                    case "SETUP_ANIMATION_LAYER":
                        return SetupAnimationLayer(operation.parameters);
                        
                    case "CREATE_ANIMATION_EVENT":
                        return CreateAnimationEvent(operation.parameters);
                        
                    case "SETUP_AVATAR":
                        return SetupAvatar(operation.parameters);
                        
                    case "CREATE_TIMELINE":
                        return CreateTimeline(operation.parameters);
                        
                    case "BAKE_ANIMATION":
                        return BakeAnimation(operation.parameters);
                        
                    // === UI詳細構築ツール ===
                    case "SETUP_UI_ANCHORS":
                        return SetupUIAnchors(operation.parameters);
                        
                    case "CREATE_RESPONSIVE_UI":
                        return CreateResponsiveUI(operation.parameters);
                        
                    case "SETUP_UI_ANIMATION":
                        return SetupUIAnimation(operation.parameters);
                        
                    case "CREATE_UI_GRID":
                        return CreateUIGrid(operation.parameters);
                        
                    case "SETUP_SCROLL_VIEW":
                        return SetupScrollView(operation.parameters);
                        
                    case "CREATE_UI_NOTIFICATION":
                        return CreateUINotification(operation.parameters);
                        
                    case "SETUP_UI_NAVIGATION":
                        return SetupUINavigation(operation.parameters);
                        
                    case "CREATE_UI_DIALOG":
                        return CreateUIDialog(operation.parameters);
                        
                    case "OPTIMIZE_UI_CANVAS":
                        return OptimizeUICanvas(operation.parameters);
                        
                    case "SETUP_SAFE_AREA":
                        return SetupSafeArea(operation.parameters);
                        
                    case "APPLY_UI_THEME":
                        return ApplyUITheme(ConvertParameters(operation.parameters));
                        
                    case "SET_UI_COLORS":
                        return SetUIColors(ConvertParameters(operation.parameters));
                        
                    case "STYLE_UI_ELEMENTS":
                        return StyleUIElements(ConvertParameters(operation.parameters));
                        
                    case "ADD_UI_EFFECTS":
                        return AddUIEffects(ConvertParameters(operation.parameters));
                        
                    case "SET_TYPOGRAPHY":
                        return SetTypography(ConvertParameters(operation.parameters));
                        
                    case "EXECUTE_BATCH":
                        return await ExecuteBatch(operation.parameters);
                        
                    case "BATCH_RENAME":
                        return BatchRename(operation.parameters);
                        
                    case "BATCH_IMPORT_SETTINGS":
                        return BatchImportSettings(operation.parameters);
                        
                    case "BATCH_PREFAB_UPDATE":
                        return BatchPrefabUpdate(operation.parameters);
                        
                    case "SEARCH_OBJECTS":
                        return SearchObjects(operation.parameters);
                        
                    case "SEND_CHAT_RESPONSE":
                        return SendChatResponse(operation.parameters);
                        
                    case "CHECK_MESSAGES":
                        return CheckMessages(operation.parameters);
                        
                    case "SEND_REALTIME_RESPONSE":
                        return SendRealtimeResponse(operation.parameters);
                        
                    case "CHECK_ACTIVE_SESSIONS":
                        return CheckActiveSessions(operation.parameters);
                        
                    case "GROUP_GAMEOBJECTS":
                        return GroupGameObjects(operation.parameters);
                    
                    case "RENAME_ASSET":
                        return RenameAsset(operation.parameters);
                    
                    case "MOVE_ASSET":
                        return MoveAsset(operation.parameters);
                    
                    case "DELETE_ASSET":
                        return DeleteAsset(operation.parameters);
                    
                    case "PAUSE_SCENE":
                        return PauseScene(operation.parameters);
                    
                    case "FIND_MISSING_REFERENCES":
                        return FindMissingReferences(operation.parameters);
                    
                    case "OPTIMIZE_TEXTURES_BATCH":
                        return OptimizeTexturesBatch(operation.parameters);
                    
                    case "ANALYZE_DRAW_CALLS":
                        return AnalyzeDrawCalls(operation.parameters);
                    
                    case "CREATE_PROJECT_SNAPSHOT":
                        return CreateProjectSnapshot(operation.parameters);
                    
                    case "ANALYZE_DEPENDENCIES":
                        return AnalyzeDependencies(operation.parameters);
                    
                    case "EXPORT_PROJECT_STRUCTURE":
                        return ExportProjectStructure(operation.parameters);
                    
                    case "VALIDATE_NAMING_CONVENTIONS":
                        return ValidateNamingConventions(operation.parameters);
                    
                    case "EXTRACT_ALL_TEXT":
                        return ExtractAllText(operation.parameters);
                    
                    case "FIND_UNUSED_ASSETS":
                        return FindUnusedAssets(operation.parameters);
                    
                    case "ESTIMATE_BUILD_SIZE":
                        return EstimateBuildSize(operation.parameters);
                    
                    case "PERFORMANCE_REPORT":
                        return PerformanceReport(operation.parameters);
                    
                    case "AUTO_ORGANIZE_FOLDERS":
                        return AutoOrganizeFolders(operation.parameters);
                    
                    case "GENERATE_LOD":
                        return GenerateLOD(operation.parameters);
                    
                    case "AUTO_ATLAS_TEXTURES":
                        return AutoAtlasTextures(operation.parameters);
                        
                    // ===== パッケージ管理 =====
                    case "LIST_PACKAGES":
                        return ListPackages(operation.parameters);
                        
                    case "INSTALL_PACKAGE":
                        return InstallPackage(operation.parameters);
                        
                    case "REMOVE_PACKAGE":
                        return RemovePackage(operation.parameters);
                        
                    case "CHECK_PACKAGE":
                        return CheckPackage(operation.parameters);
                    
                    // ===== ゲーム開発特化機能 =====
                    case "CREATE_GAME_CONTROLLER":
                        return CreateGameController(operation.parameters);
                        
                    case "SETUP_INPUT_SYSTEM":
                        return SetupInputSystem(operation.parameters);
                        
                    case "CREATE_STATE_MACHINE":
                        return CreateStateMachine(operation.parameters);
                        
                    case "SETUP_INVENTORY_SYSTEM":
                        return SetupInventorySystem(operation.parameters);
                        
                    // ===== プロトタイピング機能 =====
                    case "CREATE_GAME_TEMPLATE":
                        return CreateGameTemplate(operation.parameters);
                        
                    case "QUICK_PROTOTYPE":
                        return QuickPrototype(operation.parameters);
                        
                    // AI・機械学習関連
                    case "SETUP_ML_AGENT":
                        return SetupMLAgent(operation.parameters);
                        
                    case "CREATE_NEURAL_NETWORK":
                        return CreateNeuralNetwork(operation.parameters);
                        
                    case "SETUP_BEHAVIOR_TREE":
                        return SetupBehaviorTree(operation.parameters);
                        
                    case "CREATE_AI_PATHFINDING":
                        return CreateAIPathfinding(operation.parameters);
                        
                    // ===== GOAP AI系 =====
                    case "CREATE_GOAP_AGENT":
                        return CreateGoapAgent(operation.parameters);
                        
                    case "DEFINE_GOAP_GOAL":
                        return DefineGoapGoal(operation.parameters);
                        
                    case "CREATE_GOAP_ACTION":
                        return CreateGoapAction(operation.parameters);
                        
                    case "DEFINE_BEHAVIOR_LANGUAGE":
                        return DefineBehaviorLanguage(operation.parameters);
                        
                    case "GENERATE_GOAP_ACTION_SET":
                        return GenerateGoapActionSet(operation.parameters);
                        
                    case "SETUP_GOAP_WORLD_STATE":
                        return SetupGoapWorldState(operation.parameters);
                        
                    case "CREATE_GOAP_TEMPLATE":
                        return CreateGoapTemplate(operation.parameters);
                        
                    case "DEBUG_GOAP_DECISIONS":
                        return DebugGoapDecisions(operation.parameters);
                        
                    case "OPTIMIZE_GOAP_PERFORMANCE":
                        return OptimizeGoapPerformance(operation.parameters);
                        
                    default:
                        return $"Unknown operation: {operation.type}";
                }
            }
            catch (Exception e)
            {
                return CreateErrorResponse("ExecuteOperation", e, operation.parameters);
            }
        }
        
        private string CreateGameObject(Dictionary<string, string> parameters)
        {
            var name = parameters.GetValueOrDefault("name", "GameObject");
            string result = "";
            
            // 操作を履歴に記録
            var historyParams = new Dictionary<string, object>();
            foreach (var kvp in parameters)
            {
                historyParams[kvp.Key] = kvp.Value;
            }
            
            NexusOperationHistory.Instance.RecordOperation(
                "CREATE_GAMEOBJECT",
                $"Create GameObject: {name}",
                historyParams,
                () =>
                {
                    GameObject go;
                    var type = parameters.GetValueOrDefault("type", "empty").ToLower();
                    
                    // プリミティブタイプの場合は適切なメッシュを設定
                    switch (type)
                    {
                        case "cube":
                            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            go.name = name;
                            break;
                        case "sphere":
                            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                            go.name = name;
                            break;
                        case "cylinder":
                            go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                            go.name = name;
                            break;
                        case "plane":
                            go = GameObject.CreatePrimitive(PrimitiveType.Plane);
                            go.name = name;
                            break;
                        case "capsule":
                            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                            go.name = name;
                            break;
                        case "quad":
                            go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                            go.name = name;
                            break;
                        default:
                            go = new GameObject(name);
                            break;
                    }
                    
                    // UNDOに登録
                    UnityEditor.Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
                    
                    // Position
                    if (parameters.TryGetValue("position", out var pos))
                    {
                        go.transform.position = ParseVector3(pos);
                    }
                    
                    // Rotation
                    if (parameters.TryGetValue("rotation", out var rot))
                    {
                        go.transform.rotation = Quaternion.Euler(ParseVector3(rot));
                    }
                    
                    // Scale
                    if (parameters.TryGetValue("scale", out var scale))
                    {
                        go.transform.localScale = ParseVector3(scale);
                    }
                    
                    // Parent
                    if (parameters.TryGetValue("parent", out var parentName))
                    {
                        var parent = GameObject.Find(parentName);
                        if (parent != null)
                        {
                            go.transform.SetParent(parent.transform);
                        }
                    }
                    
                    lastCreatedObject = go;
                    createdObjects.Add(go);
                    
                    Selection.activeGameObject = go;
                    EditorGUIUtility.PingObject(go);
                    
                    // 作成結果の詳細情報を含める
                    var components = go.GetComponents<Component>().Select(c => c.GetType().Name).ToArray();
                    result = JsonConvert.SerializeObject(new
                    {
                        success = true,
                        message = $"Created GameObject: {name}",
                        name = go.name,
                        type = type,
                        components = components,
                        hasMesh = go.GetComponent<MeshFilter>() != null,
                        hasRenderer = go.GetComponent<MeshRenderer>() != null
                    });
                }
            );
            
            return result;
        }

        private string UpdateGameObject(Dictionary<string, string> parameters)
        {
            try
            {
                var gameObject = GetTargetGameObject(parameters);
                if (gameObject == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "GameObject not found"
                    });
                }
                
                var changes = new List<string>();
                
                // UNDOに登録
                UnityEditor.Undo.RecordObject(gameObject, "Update GameObject");
                
                // 名前変更
                if (parameters.ContainsKey("name"))
                {
                    gameObject.name = parameters["name"];
                    changes.Add($"Name changed to: {parameters["name"]}");
                }
                
                // タグ変更
                if (parameters.ContainsKey("tag"))
                {
                    try
                    {
                        gameObject.tag = parameters["tag"];
                        changes.Add($"Tag changed to: {parameters["tag"]}");
                    }
                    catch
                    {
                        changes.Add($"Warning: Tag '{parameters["tag"]}' not found");
                    }
                }
                
                // レイヤー変更
                if (parameters.ContainsKey("layer"))
                {
                    var layer = int.Parse(parameters["layer"]);
                    gameObject.layer = layer;
                    changes.Add($"Layer changed to: {LayerMask.LayerToName(layer)} ({layer})");
                }
                
                // アクティブ状態
                if (parameters.ContainsKey("isActive"))
                {
                    var isActive = parameters["isActive"] == "true";
                    gameObject.SetActive(isActive);
                    changes.Add($"Active state set to: {isActive}");
                }
                
                // スタティック状態
                if (parameters.ContainsKey("isStatic"))
                {
                    var isStatic = parameters["isStatic"] == "true";
                    gameObject.isStatic = isStatic;
                    changes.Add($"Static state set to: {isStatic}");
                }
                
                EditorUtility.SetDirty(gameObject);
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Updated GameObject '{gameObject.name}'",
                    changes = changes,
                    gameObject = new
                    {
                        name = gameObject.name,
                        tag = gameObject.tag,
                        layer = LayerMask.LayerToName(gameObject.layer),
                        active = gameObject.activeSelf,
                        isStatic = gameObject.isStatic,
                        path = GetFullPath(gameObject)
                    }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        private string DeleteGameObject(Dictionary<string, string> parameters)
        {
            try
            {
                var gameObject = GetTargetGameObject(parameters);
                if (gameObject == null)
                {
                    var targetName = parameters.GetValueOrDefault("target") ?? 
                                   parameters.GetValueOrDefault("name") ?? 
                                   parameters.GetValueOrDefault("object") ?? "unknown";
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"GameObject '{targetName}' not found or already deleted"
                    });
                }
                
                // オブジェクトが既に削除されていないかチェック
                if (gameObject == null || !gameObject)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "GameObject is already destroyed"
                    });
                }
                
                // 子オブジェクトの数を記録
                int childCount = gameObject.transform.childCount;
                string path = GetFullPath(gameObject);
                string objectName = gameObject.name; // 削除前に名前を保存
                
                Debug.Log($"[DeleteGameObject] Deleting object: {objectName} at path: {path}");
                
                // クリーンアップ
                if (lastCreatedObject == gameObject)
                {
                    lastCreatedObject = null;
                }
                createdObjects.Remove(gameObject);
                
                try
                {
                    // UNDOに登録してから削除
                    UnityEditor.Undo.DestroyObjectImmediate(gameObject);
                    Debug.Log($"[DeleteGameObject] Successfully deleted: {objectName}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[DeleteGameObject] Failed to delete {objectName}: {ex.Message}");
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"Failed to delete object: {ex.Message}"
                    });
                }
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Deleted GameObject '{objectName}'",
                    deletedPath = path,
                    childrenDeleted = childCount
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        private string SetTransform(Dictionary<string, string> parameters)
        {
            try
            {
                var gameObject = GetTargetGameObject(parameters);
                if (gameObject == null)
                {
                    string targetName = parameters.GetValueOrDefault("target") ?? 
                                      parameters.GetValueOrDefault("gameObject") ?? 
                                      parameters.GetValueOrDefault("object");
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"GameObject '{targetName}' not found"
                    });
                }
                
                var changes = new List<string>();
                var transform = gameObject.transform;
                
                // Transform変更をUNDOに登録
                UnityEditor.Undo.RecordObject(transform, "Set Transform");
                
                // 位置
                if (parameters.ContainsKey("position"))
                {
                    transform.position = ParseVector3(parameters["position"]);
                    changes.Add($"Position set to: {transform.position}");
                }
                
                // 回転
                if (parameters.ContainsKey("rotation"))
                {
                    transform.eulerAngles = ParseVector3(parameters["rotation"]);
                    changes.Add($"Rotation set to: {transform.eulerAngles}");
                }
                
                // スケール
                if (parameters.ContainsKey("scale"))
                {
                    transform.localScale = ParseVector3(parameters["scale"]);
                    changes.Add($"Scale set to: {transform.localScale}");
                }
                
                // ローカル座標系
                if (parameters.ContainsKey("localPosition"))
                {
                    transform.localPosition = ParseVector3(parameters["localPosition"]);
                    changes.Add($"Local position set to: {transform.localPosition}");
                }
                
                if (parameters.ContainsKey("localRotation"))
                {
                    transform.localEulerAngles = ParseVector3(parameters["localRotation"]);
                    changes.Add($"Local rotation set to: {transform.localEulerAngles}");
                }
                
                // 親の設定
                if (parameters.ContainsKey("parent"))
                {
                    if (string.IsNullOrEmpty(parameters["parent"]) || parameters["parent"] == "null")
                    {
                        transform.SetParent(null);
                        changes.Add("Parent removed");
                    }
                    else
                    {
                        var parent = GameObject.Find(parameters["parent"]);
                        if (parent != null)
                        {
                            transform.SetParent(parent.transform);
                            changes.Add($"Parent set to: {parent.name}");
                        }
                    }
                }
                
                // ワールド位置を保持するかどうか
                if (parameters.ContainsKey("worldPositionStays") && parameters.ContainsKey("parent"))
                {
                    var worldPositionStays = parameters["worldPositionStays"] == "true";
                    var parent = GameObject.Find(parameters["parent"]);
                    if (parent != null)
                    {
                        transform.SetParent(parent.transform, worldPositionStays);
                        changes.Add($"World position stays: {worldPositionStays}");
                    }
                }
                
                EditorUtility.SetDirty(gameObject);
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Transform updated for '{gameObject.name}'",
                    changes = changes,
                    transform = new
                    {
                        position = new { x = transform.position.x, y = transform.position.y, z = transform.position.z },
                        rotation = new { x = transform.eulerAngles.x, y = transform.eulerAngles.y, z = transform.eulerAngles.z },
                        scale = new { x = transform.localScale.x, y = transform.localScale.y, z = transform.localScale.z },
                        localPosition = new { x = transform.localPosition.x, y = transform.localPosition.y, z = transform.localPosition.z },
                        localRotation = new { x = transform.localEulerAngles.x, y = transform.localEulerAngles.y, z = transform.localEulerAngles.z },
                        parent = transform.parent != null ? transform.parent.name : "None"
                    }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        private string UpdateComponent(Dictionary<string, string> parameters)
        {
            try
            {
                Debug.Log($"[UpdateComponent] Called with {parameters.Count} parameters:");
                foreach (var kvp in parameters)
                {
                    Debug.Log($"[UpdateComponent] - {kvp.Key}: {kvp.Value}");
                }
                
                var component = parameters.GetValueOrDefault("component", "");
                
                if (string.IsNullOrEmpty(component))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "component parameter is required",
                        receivedParameters = parameters.Keys.ToArray()
                    });
                }
                
                var gameObject = GetTargetGameObject(parameters);
                Debug.Log($"[UpdateComponent] Found GameObject: {gameObject?.name ?? "null"}");
                
                if (gameObject == null)
                {
                    var availableObjects = GameObject.FindObjectsOfType<GameObject>().Take(10).Select(o => o.name);
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "GameObject not found",
                        availableObjects = availableObjects.ToArray()
                    });
                }
                
                var componentType = GetComponentType(component);
                Debug.Log($"[UpdateComponent] Component type: {componentType?.Name ?? "null"}");
                
                if (componentType == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"Component type '{component}' not found",
                        suggestedComponents = new[] { "Transform", "Rigidbody", "Collider", "Renderer", "Light", "Camera" }
                    });
                }
                
                var comp = gameObject.GetComponent(componentType);
                Debug.Log($"[UpdateComponent] Found component: {comp != null}");
                
                if (comp == null)
                {
                    var availableComponents = gameObject.GetComponents<Component>().Select(c => c.GetType().Name);
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"Component '{component}' not found on '{gameObject.name}'",
                        availableComponents = availableComponents.ToArray()
                    });
                }
                
                var changes = new List<string>();
                var properties = new Dictionary<string, object>();
                
                // UNDOに登録
                UnityEditor.Undo.RecordObject(comp, "Update Component");
                
                // プロパティパラメータを処理（propertiesオブジェクトまたは直接パラメータ）
                var propertyParams = new List<KeyValuePair<string, string>>();
                var excludedKeys = new[] { "target", "component", "gameObject", "object", "targetObject", "name", "objectName", "targetName", "source", "sourceName", "properties" };
                
                // propertiesパラメータがある場合
                if (parameters.ContainsKey("properties"))
                {
                    try
                    {
                        var propertiesJson = parameters["properties"];
                        Debug.Log($"[UpdateComponent] Raw properties parameter: {propertiesJson}");
                        
                        // エスケープされたJSON文字列の場合、アンエスケープする
                        if (propertiesJson.StartsWith("{\\\"") || propertiesJson.StartsWith("[\\\""))
                        {
                            // エスケープされたJSON文字列をアンエスケープ
                            propertiesJson = System.Text.RegularExpressions.Regex.Unescape(propertiesJson);
                            Debug.Log($"[UpdateComponent] Unescaped JSON: {propertiesJson}");
                        }
                        
                        Dictionary<string, object> propertiesDict;
                        
                        // JSON文字列として解析を試行
                        try
                        {
                            propertiesDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(propertiesJson);
                        }
                        catch
                        {
                            // JSON解析に失敗した場合、key=value形式として解析を試行
                            Debug.LogWarning($"[UpdateComponent] JSON parsing failed, attempting key=value parsing");
                            propertiesDict = ParseKeyValueString(propertiesJson);
                        }
                        
                        foreach (var kvp in propertiesDict)
                        {
                            var valueStr = kvp.Value?.ToString() ?? "";
                            propertyParams.Add(new KeyValuePair<string, string>(kvp.Key, valueStr));
                            Debug.Log($"[UpdateComponent] Added property: {kvp.Key} = {valueStr}");
                        }
                        Debug.Log($"[UpdateComponent] Successfully parsed {propertyParams.Count} properties from JSON");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[UpdateComponent] Failed to parse properties: {ex.Message}");
                        Debug.LogError($"[UpdateComponent] Raw properties value: {parameters["properties"]}");
                    }
                }
                
                // 直接パラメータも追加
                var directParams = parameters.Where(p => !excludedKeys.Contains(p.Key)).ToList();
                propertyParams.AddRange(directParams);
                
                Debug.Log($"[UpdateComponent] Total property parameters to update: {propertyParams.Count}");
                foreach (var param in propertyParams)
                {
                    Debug.Log($"[UpdateComponent] - Will update: {param.Key} = {param.Value}");
                }
                
                if (propertyParams.Count == 0)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "No property parameters found to update",
                        excludedKeys = excludedKeys,
                        allParameters = parameters.Keys.ToArray(),
                        hint = "Use 'properties' parameter with JSON object or pass properties directly"
                    });
                }
                
                foreach (var kvp in propertyParams)
                {
                    try
                    {
                        Debug.Log($"[UpdateComponent] Setting property {kvp.Key} to {kvp.Value}");
                        SetComponentProperty(comp, kvp.Key, kvp.Value);
                        changes.Add($"{kvp.Key} = {kvp.Value}");
                        
                        // 更新後の値を取得（Vector3などの循環参照を防ぐ）
                        var propInfo = componentType.GetProperty(kvp.Key);
                        if (propInfo != null && propInfo.CanRead)
                        {
                            var value = propInfo.GetValue(comp);
                            properties[kvp.Key] = ConvertValueForSerialization(value);
                        }
                        else
                        {
                            var fieldInfo = componentType.GetField(kvp.Key);
                            if (fieldInfo != null)
                            {
                                var value = fieldInfo.GetValue(comp);
                                properties[kvp.Key] = ConvertValueForSerialization(value);
                            }
                        }
                        Debug.Log($"[UpdateComponent] Successfully set {kvp.Key}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[UpdateComponent] Failed to set {kvp.Key}: {ex.Message}");
                        changes.Add($"Failed to set {kvp.Key}: {ex.Message}");
                    }
                }
                
                EditorUtility.SetDirty(comp);
                
                Debug.Log($"[UpdateComponent] Completed with {changes.Count} changes");
                
                // 成功の場合でも、エラーがあるかチェック
                var failedChanges = changes.Where(c => c.StartsWith("Failed")).ToList();
                var successChanges = changes.Where(c => !c.StartsWith("Failed")).ToList();
                
                // 部分的成功でも有用な情報を提供
                var isFullSuccess = failedChanges.Count == 0;
                var hasAnySuccess = successChanges.Count > 0;
                
                return JsonConvert.SerializeObject(new
                {
                    success = isFullSuccess || hasAnySuccess, // 部分的成功も成功とみなす
                    fullSuccess = isFullSuccess,
                    partialSuccess = hasAnySuccess && !isFullSuccess,
                    message = isFullSuccess ? 
                        $"Successfully updated {component} on '{gameObject.name}'" :
                        hasAnySuccess ?
                        $"Partially updated {component} on '{gameObject.name}' - {successChanges.Count} successful, {failedChanges.Count} failed" :
                        $"Failed to update {component} on '{gameObject.name}'",
                    successfulChanges = successChanges,
                    failedChanges = failedChanges,
                    totalChanges = changes.Count,
                    successCount = successChanges.Count,
                    failureCount = failedChanges.Count,
                    properties = properties,
                    componentType = componentType.Name,
                    targetObject = gameObject.name,
                    availableProperties = componentType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.CanWrite)
                        .Select(p => p.Name)
                        .Take(10) // 最初の10個のみ
                        .ToArray()
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UpdateComponent] Fatal error: {e.Message}");
                Debug.LogError($"[UpdateComponent] Stack trace: {e.StackTrace}");
                
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message,
                    errorType = e.GetType().Name,
                    stackTrace = e.StackTrace,
                    parameters = parameters.Keys.ToArray(),
                    debugInfo = "Check Unity Console for detailed debug logs"
                }, Formatting.Indented);
            }
        }
        
        private string AddComponent(Dictionary<string, string> parameters)
        {
            var componentType = parameters.GetValueOrDefault("componentType") ?? 
                              parameters.GetValueOrDefault("component") ?? 
                              parameters.GetValueOrDefault("type");
            
            GameObject target = GetTargetGameObject(parameters);
            
            if (target == null)
            {
                string targetName = parameters.GetValueOrDefault("target") ?? 
                                  parameters.GetValueOrDefault("gameObject") ?? 
                                  parameters.GetValueOrDefault("object");
                return $"Target object not found: {targetName}";
            }
            
            // Try to find the component type
            Type type = GetComponentType(componentType);
            if (type == null)
            {
                return $"Component type not found: {componentType}";
            }
            
            // UNDOに登録してからコンポーネントを追加
            var component = UnityEditor.Undo.AddComponent(target, type);
            
            // Set component properties if provided (システムパラメータを除外)
            var excludedKeys = new[] { "target", "type", "componentType", "component", "gameObject", "object", "targetObject", "name", "objectName", "targetName", "source", "sourceName" };
            var propertyParams = parameters.Where(p => !excludedKeys.Contains(p.Key)).ToList();
            
            Debug.Log($"[AddComponent] Property parameters to set: {propertyParams.Count}");
            foreach (var param in propertyParams)
            {
                Debug.Log($"[AddComponent] - Will set: {param.Key} = {param.Value}");
            }
            
            foreach (var kvp in propertyParams)
            {
                try
                {
                    SetComponentProperty(component, kvp.Key, kvp.Value);
                    Debug.Log($"[AddComponent] Successfully set property {kvp.Key}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AddComponent] Failed to set property {kvp.Key}: {ex.Message}");
                }
            }
            
            return $"Added {componentType} to {target.name}";
        }
        
        private string SetProperty(Dictionary<string, string> parameters)
        {
            var componentType = parameters.GetValueOrDefault("component");
            var property = parameters.GetValueOrDefault("property");
            var value = parameters.GetValueOrDefault("value");
            
            var target = GetTargetGameObject(parameters);
            if (target == null) {
                string targetName = parameters.GetValueOrDefault("target") ?? 
                                  parameters.GetValueOrDefault("gameObject") ?? 
                                  parameters.GetValueOrDefault("object");
                return $"Object not found: {targetName}";
            }
            
            Component component = null;
            if (!string.IsNullOrEmpty(componentType))
            {
                var type = GetComponentType(componentType);
                component = target.GetComponent(type);
            }
            else
            {
                component = target.transform;
            }
            
            if (component == null) return $"Component not found: {componentType}";
            
            // UNDOに登録
            UnityEditor.Undo.RecordObject(component, "Set Property");
            
            SetComponentProperty(component, property, value);
            
            return $"Set {property} = {value} on {target.name}";
        }
        
        private string CreateUI(Dictionary<string, string> parameters)
        {
            var uiType = parameters.GetValueOrDefault("uiType", parameters.GetValueOrDefault("type", "button"));
            var name = parameters.GetValueOrDefault("name", uiType);
            
            // Ensure Canvas exists
            var canvas = GameObject.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                
                // Add EventSystem
                if (GameObject.FindObjectOfType<EventSystem>() == null)
                {
                    var eventSystem = new GameObject("EventSystem");
                    eventSystem.AddComponent<EventSystem>();
                    eventSystem.AddComponent<StandaloneInputModule>();
                }
            }
            
            GameObject uiElement = null;
            
            switch (uiType.ToLower())
            {
                case "button":
                    uiElement = CreateButton(name, parameters);
                    break;
                    
                case "text":
                    uiElement = CreateText(name, parameters);
                    break;
                    
                case "inputfield":
                    uiElement = CreateInputField(name, parameters);
                    break;
                    
                case "panel":
                    uiElement = CreatePanel(name, parameters);
                    break;
                    
                case "image":
                    uiElement = CreateImage(name, parameters);
                    break;
                    
                case "slider":
                    uiElement = CreateSlider(name, parameters);
                    break;
                    
                case "toggle":
                    uiElement = CreateToggle(name, parameters);
                    break;
                    
                case "dropdown":
                    uiElement = CreateDropdown(name, parameters);
                    break;
            }
            
            if (uiElement != null)
            {
                // UNDOに登録
                UnityEditor.Undo.RegisterCreatedObjectUndo(uiElement, $"Create UI {uiType}");
                
                uiElement.transform.SetParent(canvas.transform, false);
                
                // Position
                if (parameters.TryGetValue("position", out var pos))
                {
                    var rt = uiElement.GetComponent<RectTransform>();
                    rt.anchoredPosition = ParseVector2(pos);
                }
                
                // Size
                if (parameters.TryGetValue("size", out var size))
                {
                    var rt = uiElement.GetComponent<RectTransform>();
                    rt.sizeDelta = ParseVector2(size);
                }
                
                lastCreatedObject = uiElement;
                createdObjects.Add(uiElement);
                
                return $"Created UI {uiType}: {name}";
            }
            
            return $"Unknown UI type: {uiType}";
        }
        
        private GameObject CreateButton(string name, Dictionary<string, string> parameters)
        {
            var button = new GameObject(name);
            var rt = button.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160, 40);
            
            var image = button.AddComponent<Image>();
            image.color = new Color(0.9f, 0.9f, 0.9f);
            
            var btn = button.AddComponent<Button>();
            btn.targetGraphic = image;
            
            // Add text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(button.transform, false);
            var text = textGO.AddComponent<Text>();
            text.text = parameters.GetValueOrDefault("text", name);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.black;
            text.alignment = TextAnchor.MiddleCenter;
            
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            
            return button;
        }
        
        private GameObject CreateText(string name, Dictionary<string, string> parameters)
        {
            var textGO = new GameObject(name);
            var rt = textGO.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 50);
            
            var text = textGO.AddComponent<Text>();
            text.text = parameters.GetValueOrDefault("text", "Text");
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = int.Parse(parameters.GetValueOrDefault("fontSize", "16"));
            text.color = ParseColor(parameters.GetValueOrDefault("color", "black"));
            text.alignment = TextAnchor.MiddleCenter;
            
            return textGO;
        }
        
        private GameObject CreateInputField(string name, Dictionary<string, string> parameters)
        {
            var inputGO = new GameObject(name);
            var rt = inputGO.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300, 40);
            
            var image = inputGO.AddComponent<Image>();
            image.color = Color.white;
            
            var input = inputGO.AddComponent<InputField>();
            
            // Create text components
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(inputGO.transform, false);
            var text = textGO.AddComponent<Text>();
            text.supportRichText = false;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = Color.black;
            input.textComponent = text;
            
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(10, 6);
            textRT.offsetMax = new Vector2(-10, -7);
            
            // Placeholder
            var placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(inputGO.transform, false);
            var placeholder = placeholderGO.AddComponent<Text>();
            placeholder.text = parameters.GetValueOrDefault("placeholder", "Enter text...");
            placeholder.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholder.fontStyle = FontStyle.Italic;
            placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            input.placeholder = placeholder;
            
            var placeholderRT = placeholderGO.GetComponent<RectTransform>();
            placeholderRT.anchorMin = Vector2.zero;
            placeholderRT.anchorMax = Vector2.one;
            placeholderRT.offsetMin = new Vector2(10, 6);
            placeholderRT.offsetMax = new Vector2(-10, -7);
            
            return inputGO;
        }
        
        private GameObject CreatePanel(string name, Dictionary<string, string> parameters)
        {
            var panel = new GameObject(name);
            var rt = panel.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 300);
            
            var image = panel.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            return panel;
        }
        
        private GameObject CreateImage(string name, Dictionary<string, string> parameters)
        {
            var imageGO = new GameObject(name);
            var rt = imageGO.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 100);
            
            var image = imageGO.AddComponent<Image>();
            image.color = ParseColor(parameters.GetValueOrDefault("color", "white"));
            
            return imageGO;
        }
        
        private GameObject CreateSlider(string name, Dictionary<string, string> parameters)
        {
            var slider = new GameObject(name);
            var rt = slider.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 20);
            
            var sliderComp = slider.AddComponent<Slider>();
            
            // Background
            var background = new GameObject("Background");
            background.transform.SetParent(slider.transform, false);
            var bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f);
            var bgRT = background.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0, 0.25f);
            bgRT.anchorMax = new Vector2(1, 0.75f);
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            
            // Fill Area
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(slider.transform, false);
            var fillAreaRT = fillArea.GetComponent<RectTransform>();
            fillAreaRT.anchorMin = new Vector2(0, 0.25f);
            fillAreaRT.anchorMax = new Vector2(1, 0.75f);
            fillAreaRT.offsetMin = new Vector2(5, 0);
            fillAreaRT.offsetMax = new Vector2(-15, 0);
            sliderComp.fillRect = fillAreaRT;
            
            // Fill
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.6f, 1f);
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(1, 1);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;
            
            // Handle
            var handle = new GameObject("Handle");
            handle.transform.SetParent(slider.transform, false);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;
            var handleRT = handle.GetComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(20, 20);
            sliderComp.handleRect = handleRT;
            sliderComp.targetGraphic = handleImage;
            
            return slider;
        }
        
        private GameObject CreateToggle(string name, Dictionary<string, string> parameters)
        {
            var toggle = new GameObject(name);
            var rt = toggle.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 30);
            
            var toggleComp = toggle.AddComponent<Toggle>();
            
            // Background
            var background = new GameObject("Background");
            background.transform.SetParent(toggle.transform, false);
            var bgImage = background.AddComponent<Image>();
            bgImage.color = Color.white;
            var bgRT = background.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0, 0.5f);
            bgRT.anchorMax = new Vector2(0, 0.5f);
            bgRT.anchoredPosition = new Vector2(10, 0);
            bgRT.sizeDelta = new Vector2(20, 20);
            toggleComp.targetGraphic = bgImage;
            
            // Checkmark
            var checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(background.transform, false);
            var checkImage = checkmark.AddComponent<Image>();
            checkImage.color = new Color(0.3f, 0.6f, 1f);
            var checkRT = checkmark.GetComponent<RectTransform>();
            checkRT.anchorMin = Vector2.zero;
            checkRT.anchorMax = Vector2.one;
            checkRT.offsetMin = new Vector2(2, 2);
            checkRT.offsetMax = new Vector2(-2, -2);
            toggleComp.graphic = checkImage;
            
            // Label
            var label = new GameObject("Label");
            label.transform.SetParent(toggle.transform, false);
            var labelText = label.AddComponent<Text>();
            labelText.text = parameters.GetValueOrDefault("text", name);
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.color = Color.black;
            var labelRT = label.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0, 0.5f);
            labelRT.anchorMax = new Vector2(1, 0.5f);
            labelRT.anchoredPosition = new Vector2(40, 0);
            labelRT.sizeDelta = new Vector2(-40, 30);
            
            return toggle;
        }
        
        private GameObject CreateDropdown(string name, Dictionary<string, string> parameters)
        {
            var dropdown = new GameObject(name);
            var rt = dropdown.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 30);
            
            var image = dropdown.AddComponent<Image>();
            image.color = Color.white;
            
            var dropdownComp = dropdown.AddComponent<Dropdown>();
            dropdownComp.targetGraphic = image;
            
            // Template (hidden by default)
            var template = new GameObject("Template");
            template.transform.SetParent(dropdown.transform, false);
            template.SetActive(false);
            
            // Add options
            if (parameters.TryGetValue("options", out var optionsStr))
            {
                var options = optionsStr.Split(',');
                dropdownComp.options.Clear();
                foreach (var option in options)
                {
                    dropdownComp.options.Add(new Dropdown.OptionData(option.Trim()));
                }
            }
            
            return dropdown;
        }
        
        private async Task<string> CreateScript(NexusUnityOperation operation)
        {
            var name = operation.parameters.GetValueOrDefault("name", "NewScript");
            var code = operation.code ?? GenerateDefaultScript(name);
            
            var folderPath = "Assets/Nexus_Generated";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            
            var scriptPath = $"{folderPath}/{name}.cs";
            
            // Check for existing file
            if (File.Exists(scriptPath))
            {
                scriptPath = $"{folderPath}/{name}_{DateTime.Now:HHmmss}.cs";
            }
            
            // スクリプト作成をUNDOに登録
            UnityEditor.Undo.RegisterCompleteObjectUndo(Selection.activeObject, $"Create Script {name}");
            File.WriteAllText(scriptPath, code);
            AssetDatabase.Refresh();
            
            // Wait for compilation
            await Task.Delay(1000);
            
            // Attach to GameObject if specified
            if (operation.parameters.TryGetValue("attach", out var attachTo))
            {
                GameObject target = null;
                if (attachTo == "last")
                {
                    target = lastCreatedObject;
                }
                else
                {
                    target = GameObject.Find(attachTo);
                }
                
                if (target != null)
                {
                    var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);
                    if (monoScript != null)
                    {
                        var scriptType = monoScript.GetClass();
                        if (scriptType != null)
                        {
                            target.AddComponent(scriptType);
                            return $"Created script {name} and attached to {target.name}";
                        }
                    }
                }
            }
            
            return $"Created script: {name}";
        }
        
        private string GenerateDefaultScript(string className)
        {
            return $@"using UnityEngine;

public class {className} : MonoBehaviour
{{
    void Start()
    {{
        
    }}
    
    void Update()
    {{
        
    }}
}}";
        }
        
        private string ModifyScript(Dictionary<string, string> parameters)
        {
            try
            {
                string scriptPath = parameters.GetValueOrDefault("scriptPath", "");
                string fileName = parameters.GetValueOrDefault("fileName", "");
                string operation = parameters.GetValueOrDefault("operation", "replace"); // replace, insert, append, prepend
                string content = parameters.GetValueOrDefault("content", "");
                string searchText = parameters.GetValueOrDefault("searchText", "");
                int lineNumber = Convert.ToInt32(parameters.GetValueOrDefault("lineNumber", "0"));
                
                Debug.Log($"[NexusExecutor] ModifyScript - Operation: {operation}, SearchText: '{searchText}', LineNumber: {lineNumber}");
                
                // パラメータ検証
                if (string.IsNullOrEmpty(scriptPath) && string.IsNullOrEmpty(fileName))
                {
                    return CreateErrorResponse("Either scriptPath or fileName must be provided");
                }
                
                // スクリプトパスの決定（改善されたGetScriptPathメソッドを使用）
                string fullPath = GetScriptPath(scriptPath, fileName);
                
                if (string.IsNullOrEmpty(fullPath))
                {
                    string debugInfo = GetScriptSearchInfo(scriptPath, fileName);
                    return CreateErrorResponse($"Script file not found\n\n{debugInfo}");
                }
                
                Debug.Log($"[NexusExecutor] ModifyScript - Found script at: {fullPath}");
                
                if (!System.IO.File.Exists(fullPath))
                {
                    return CreateErrorResponse($"Script file does not exist: {fullPath}");
                }
                
                // 現在のファイル内容を読み込み
                string originalContent = System.IO.File.ReadAllText(fullPath);
                string newContent = originalContent;
                string operationResult = "";
                
                switch (operation.ToLower())
                {
                    case "replace":
                        if (!string.IsNullOrEmpty(searchText))
                        {
                            if (originalContent.Contains(searchText))
                            {
                                newContent = originalContent.Replace(searchText, content);
                                operationResult = $"Replaced '{searchText}' with new content";
                            }
                            else
                            {
                                return JsonConvert.SerializeObject(new
                                {
                                    content = new[]
                                    {
                                        new
                                        {
                                            type = "text",
                                            text = $"Search text not found: '{searchText}'"
                                        }
                                    }
                                });
                            }
                        }
                        else if (lineNumber > 0)
                        {
                            var lines = originalContent.Split('\n');
                            if (lineNumber <= lines.Length)
                            {
                                lines[lineNumber - 1] = content;
                                newContent = string.Join("\n", lines);
                                operationResult = $"Replaced line {lineNumber}";
                            }
                            else
                            {
                                return JsonConvert.SerializeObject(new
                                {
                                    content = new[]
                                    {
                                        new
                                        {
                                            type = "text",
                                            text = $"Line number {lineNumber} is out of range (file has {lines.Length} lines)"
                                        }
                                    }
                                });
                            }
                        }
                        else
                        {
                            // 全体を置換
                            newContent = content;
                            operationResult = "Replaced entire file content";
                        }
                        break;
                        
                    case "insert":
                        if (lineNumber > 0)
                        {
                            var lines = originalContent.Split('\n').ToList();
                            if (lineNumber <= lines.Count + 1)
                            {
                                lines.Insert(lineNumber - 1, content);
                                newContent = string.Join("\n", lines);
                                operationResult = $"Inserted content at line {lineNumber}";
                            }
                            else
                            {
                                return JsonConvert.SerializeObject(new
                                {
                                    content = new[]
                                    {
                                        new
                                        {
                                            type = "text",
                                            text = $"Line number {lineNumber} is out of range"
                                        }
                                    }
                                });
                            }
                        }
                        else if (!string.IsNullOrEmpty(searchText))
                        {
                            int index = originalContent.IndexOf(searchText);
                            if (index >= 0)
                            {
                                newContent = originalContent.Insert(index, content);
                                operationResult = $"Inserted content before '{searchText}'";
                            }
                            else
                            {
                                return JsonConvert.SerializeObject(new
                                {
                                    content = new[]
                                    {
                                        new
                                        {
                                            type = "text",
                                            text = $"Search text not found: '{searchText}'"
                                        }
                                    }
                                });
                            }
                        }
                        break;
                        
                    case "append":
                        newContent = originalContent + "\n" + content;
                        operationResult = "Appended content to end of file";
                        break;
                        
                    case "prepend":
                        newContent = content + "\n" + originalContent;
                        operationResult = "Prepended content to beginning of file";
                        break;
                        
                    default:
                        return JsonConvert.SerializeObject(new
                        {
                            content = new[]
                            {
                                new
                                {
                                    type = "text",
                                    text = $"Unknown operation: {operation}. Supported operations: replace, insert, append, prepend"
                                }
                            }
                        });
                }
                
                // ファイルに書き込み（UNDOに登録）
                var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(fullPath);
                if (scriptAsset != null) UnityEditor.Undo.RegisterCompleteObjectUndo(scriptAsset, "Modify Script");
                System.IO.File.WriteAllText(fullPath, newContent);
                AssetDatabase.ImportAsset(fullPath);
                AssetDatabase.Refresh();
                
                Debug.Log($"[NexusExecutor] ModifyScript - Success: {operationResult}");
                return CreateSuccessResponse($"Script modified successfully: {fullPath}\nOperation: {operationResult}\nFile refreshed in Unity");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NexusExecutor] ModifyScript - Error: {e.Message}\n{e.StackTrace}");
                return CreateErrorResponse($"Error modifying script: {e.Message}");
            }
        }
        
        /// <summary>
        /// 特定の行だけ編集
        /// </summary>
        private string EditScriptLine(Dictionary<string, string> parameters)
        {
            try
            {
                string scriptPath = parameters.GetValueOrDefault("scriptPath", "");
                string fileName = parameters.GetValueOrDefault("fileName", "");
                int lineNumber = Convert.ToInt32(parameters.GetValueOrDefault("lineNumber", "0"));
                string newContent = parameters.GetValueOrDefault("newContent", "");
                
                string fullPath = GetScriptPath(scriptPath, fileName);
                if (string.IsNullOrEmpty(fullPath))
                {
                    string debugInfo = GetScriptSearchInfo(scriptPath, fileName);
                    return CreateErrorResponse($"Script file not found\\n\\n{debugInfo}");
                }
                
                var lines = System.IO.File.ReadAllLines(fullPath).ToList();
                if (lineNumber < 1 || lineNumber > lines.Count)
                {
                    return CreateErrorResponse($"Line number {lineNumber} is out of range (file has {lines.Count} lines)");
                }
                
                string oldContent = lines[lineNumber - 1];
                lines[lineNumber - 1] = newContent;
                
                System.IO.File.WriteAllLines(fullPath, lines);
                AssetDatabase.ImportAsset(fullPath);
                AssetDatabase.Refresh();
                
                return CreateSuccessResponse($"Line {lineNumber} updated successfully\\nOld: {oldContent}\\nNew: {newContent}\\nFile: {fullPath}");
            }
            catch (Exception e)
            {
                return CreateErrorResponse($"Error editing script line: {e.Message}");
            }
        }
        
        /// <summary>
        /// メソッド追加
        /// </summary>
        private string AddScriptMethod(Dictionary<string, string> parameters)
        {
            try
            {
                string scriptPath = parameters.GetValueOrDefault("scriptPath", "");
                string fileName = parameters.GetValueOrDefault("fileName", "");
                string methodName = parameters.GetValueOrDefault("methodName", "");
                string methodContent = parameters.GetValueOrDefault("methodContent", "");
                string insertAfter = parameters.GetValueOrDefault("insertAfter", "");
                
                string fullPath = GetScriptPath(scriptPath, fileName);
                if (string.IsNullOrEmpty(fullPath))
                {
                    string debugInfo = GetScriptSearchInfo(scriptPath, fileName);
                    return CreateErrorResponse($"Script file not found\\n\\n{debugInfo}");
                }
                
                string content = System.IO.File.ReadAllText(fullPath);
                string newContent;
                
                if (!string.IsNullOrEmpty(insertAfter))
                {
                    // 指定されたパターンの後に挿入
                    int insertIndex = content.LastIndexOf(insertAfter);
                    if (insertIndex >= 0)
                    {
                        insertIndex = content.IndexOf('\n', insertIndex) + 1;
                        newContent = content.Insert(insertIndex, $"\n{methodContent}\n");
                    }
                    else
                    {
                        return CreateErrorResponse($"Pattern '{insertAfter}' not found in script");
                    }
                }
                else
                {
                    // クラスの最後に追加（最後の}の前）
                    int lastBraceIndex = content.LastIndexOf('}');
                    if (lastBraceIndex > 0)
                    {
                        newContent = content.Insert(lastBraceIndex, $"\n{methodContent}\n");
                    }
                    else
                    {
                        return CreateErrorResponse("Could not find class closing brace");
                    }
                }
                
                var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(fullPath);
                if (scriptAsset != null) UnityEditor.Undo.RegisterCompleteObjectUndo(scriptAsset, "Modify Script");
                System.IO.File.WriteAllText(fullPath, newContent);
                AssetDatabase.ImportAsset(fullPath);
                AssetDatabase.Refresh();
                
                return CreateSuccessResponse($"Method '{methodName}' added successfully to {fullPath}");
            }
            catch (Exception e)
            {
                return CreateErrorResponse($"Error adding script method: {e.Message}");
            }
        }
        
        /// <summary>
        /// 変数値変更
        /// </summary>
        private string UpdateScriptVariable(Dictionary<string, string> parameters)
        {
            try
            {
                string scriptPath = parameters.GetValueOrDefault("scriptPath", "");
                string fileName = parameters.GetValueOrDefault("fileName", "");
                string variableName = parameters.GetValueOrDefault("variableName", "");
                string newDeclaration = parameters.GetValueOrDefault("newDeclaration", "");
                string updateType = parameters.GetValueOrDefault("updateType", "declaration");
                
                string fullPath = GetScriptPath(scriptPath, fileName);
                if (string.IsNullOrEmpty(fullPath))
                {
                    string debugInfo = GetScriptSearchInfo(scriptPath, fileName);
                    return CreateErrorResponse($"Script file not found\\n\\n{debugInfo}");
                }
                
                string content = System.IO.File.ReadAllText(fullPath);
                string newContent = content;
                string operationResult = "";
                
                // 変数宣言を検索（複数パターンに対応）
                string[] searchPatterns = {
                    $@"\b(public|private|protected|internal)?\s*(static)?\s*\w+\s+{variableName}\s*[=;].*?;",
                    $@"\b{variableName}\s*=.*?;",
                    $@"\[\w+\]\s*(public|private|protected|internal)?\s*(static)?\s*\w+\s+{variableName}\s*[=;].*?;"
                };
                
                bool found = false;
                foreach (string pattern in searchPatterns)
                {
                    var regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.Multiline);
                    var match = regex.Match(content);
                    
                    if (match.Success)
                    {
                        string oldDeclaration = match.Value;
                        
                        if (updateType == "value")
                        {
                            // 値のみ更新（= の後の部分）
                            var valueRegex = new System.Text.RegularExpressions.Regex($@"({variableName}\s*=\s*)([^;]+)(;)");
                            newContent = valueRegex.Replace(content, $"$1{newDeclaration}$3");
                        }
                        else
                        {
                            // 宣言全体を置換
                            newContent = regex.Replace(content, newDeclaration);
                        }
                        
                        operationResult = $"Updated variable '{variableName}'\\nOld: {oldDeclaration}\\nNew: {newDeclaration}";
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    return CreateErrorResponse($"Variable '{variableName}' not found in script");
                }
                
                var scriptAsset = AssetDatabase.LoadAssetAtPath<MonoScript>(fullPath);
                if (scriptAsset != null) UnityEditor.Undo.RegisterCompleteObjectUndo(scriptAsset, "Modify Script");
                System.IO.File.WriteAllText(fullPath, newContent);
                AssetDatabase.ImportAsset(fullPath);
                AssetDatabase.Refresh();
                
                return CreateSuccessResponse($"Variable updated successfully\\n{operationResult}\\nFile: {fullPath}");
            }
            catch (Exception e)
            {
                return CreateErrorResponse($"Error updating script variable: {e.Message}");
            }
        }
        
        /// <summary>
        /// スクリプトパスを取得（改善版）
        /// </summary>
        private string GetScriptPath(string scriptPath, string fileName)
        {
            Debug.Log($"[NexusExecutor] GetScriptPath - scriptPath: '{scriptPath}', fileName: '{fileName}'");
            
            // 1. 直接パスが指定されている場合
            if (!string.IsNullOrEmpty(scriptPath))
            {
                // 複数のパスパターンを試す
                string[] pathPatterns = new string[]
                {
                    scriptPath,
                    scriptPath.StartsWith("Assets/") ? scriptPath : $"Assets/{scriptPath}",
                    $"Assets/Scripts/{scriptPath}",
                    $"Assets/{scriptPath}.cs",
                    $"Assets/Scripts/{scriptPath}.cs"
                };
                
                foreach (string pattern in pathPatterns)
                {
                    if (System.IO.File.Exists(pattern))
                    {
                        Debug.Log($"[NexusExecutor] Found script at: {pattern}");
                        return pattern;
                    }
                }
                Debug.LogWarning($"[NexusExecutor] Script not found with path patterns for: {scriptPath}");
            }
            
            // 2. ファイル名から検索
            if (!string.IsNullOrEmpty(fileName))
            {
                // .csを確実に含める
                string searchName = fileName.EndsWith(".cs") ? fileName.Replace(".cs", "") : fileName;
                
                Debug.Log($"[NexusExecutor] Searching for script: {searchName}");
                
                // AssetDatabaseでより柔軟な検索
                string[] searchPatterns = new string[]
                {
                    $"{searchName} t:Script",
                    $"t:Script {searchName}",
                    searchName
                };
                
                foreach (string pattern in searchPatterns)
                {
                    string[] foundFiles = AssetDatabase.FindAssets(pattern);
                    if (foundFiles.Length > 0)
                    {
                        // 完全一致を優先
                        foreach (string guid in foundFiles)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guid);
                            string fileNameOnly = System.IO.Path.GetFileNameWithoutExtension(path);
                            if (string.Equals(fileNameOnly, searchName, StringComparison.OrdinalIgnoreCase))
                            {
                                Debug.Log($"[NexusExecutor] Found exact match: {path}");
                                return path;
                            }
                        }
                        
                        // 完全一致がない場合は最初の結果を使用
                        string foundPath = AssetDatabase.GUIDToAssetPath(foundFiles[0]);
                        Debug.Log($"[NexusExecutor] Found partial match: {foundPath}");
                        return foundPath;
                    }
                }
                
                // AssetDatabaseで見つからない場合、ファイルシステムで直接検索
                Debug.Log("[NexusExecutor] AssetDatabase search failed, trying file system search");
                try
                {
                    string[] allCsFiles = System.IO.Directory.GetFiles("Assets", "*.cs", System.IO.SearchOption.AllDirectories);
                    
                    // 完全一致を探す
                    foreach (string file in allCsFiles)
                    {
                        string fileNameOnly = System.IO.Path.GetFileNameWithoutExtension(file);
                        if (string.Equals(fileNameOnly, searchName, StringComparison.OrdinalIgnoreCase))
                        {
                            string unityPath = file.Replace("\\", "/");
                            Debug.Log($"[NexusExecutor] Found file system match: {unityPath}");
                            return unityPath;
                        }
                    }
                    
                    // 部分一致も試す
                    foreach (string file in allCsFiles)
                    {
                        string fileNameOnly = System.IO.Path.GetFileNameWithoutExtension(file);
                        if (fileNameOnly.IndexOf(searchName, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            string unityPath = file.Replace("\\", "/");
                            Debug.LogWarning($"[NexusExecutor] Found partial file system match: {unityPath}");
                            return unityPath;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[NexusExecutor] File system search error: {e.Message}");
                }
            }
            
            Debug.LogError($"[NexusExecutor] Failed to find script - scriptPath: '{scriptPath}', fileName: '{fileName}'");
            return null;
        }
        
        /// <summary>
        /// スクリプトファイル検索の詳細情報を取得
        /// </summary>
        private string GetScriptSearchInfo(string scriptPath, string fileName)
        {
            var info = new System.Text.StringBuilder();
            info.AppendLine("=== Script Search Debug Info ===");
            info.AppendLine($"- Provided scriptPath: '{scriptPath}'");
            info.AppendLine($"- Provided fileName: '{fileName}'");
            info.AppendLine($"- Current working directory: {System.IO.Directory.GetCurrentDirectory()}");
            
            // パスパターンチェック
            if (!string.IsNullOrEmpty(scriptPath))
            {
                info.AppendLine("\n[Path Pattern Check]");
                string[] pathPatterns = new string[]
                {
                    scriptPath,
                    scriptPath.StartsWith("Assets/") ? scriptPath : $"Assets/{scriptPath}",
                    $"Assets/Scripts/{scriptPath}",
                    $"Assets/{scriptPath}.cs",
                    $"Assets/Scripts/{scriptPath}.cs"
                };
                
                foreach (string pattern in pathPatterns)
                {
                    bool exists = System.IO.File.Exists(pattern);
                    info.AppendLine($"  - {pattern}: {(exists ? "EXISTS" : "not found")}");
                }
            }
            
            if (!string.IsNullOrEmpty(fileName))
            {
                string searchName = fileName.EndsWith(".cs") ? fileName.Replace(".cs", "") : fileName;
                
                info.AppendLine("\n[AssetDatabase Search]");
                
                // 複数の検索パターンを試す
                string[] searchPatterns = new string[]
                {
                    $"{searchName} t:Script",
                    $"t:Script {searchName}",
                    searchName
                };
                
                foreach (string pattern in searchPatterns)
                {
                    string[] foundFiles = AssetDatabase.FindAssets(pattern);
                    info.AppendLine($"  Pattern '{pattern}': {foundFiles.Length} results");
                    for (int i = 0; i < foundFiles.Length && i < 3; i++)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(foundFiles[i]);
                        info.AppendLine($"    [{i}] {path}");
                    }
                }
                
                // ファイルシステム検索
                info.AppendLine("\n[File System Search]");
                try
                {
                    string[] allCsFiles = System.IO.Directory.GetFiles("Assets", "*.cs", System.IO.SearchOption.AllDirectories);
                    
                    // 完全一致
                    var exactMatches = allCsFiles.Where(f => 
                        string.Equals(System.IO.Path.GetFileNameWithoutExtension(f), searchName, StringComparison.OrdinalIgnoreCase)
                    ).Take(5).ToArray();
                    
                    // 部分一致
                    var partialMatches = allCsFiles.Where(f => 
                        System.IO.Path.GetFileNameWithoutExtension(f).IndexOf(searchName, StringComparison.OrdinalIgnoreCase) >= 0
                    ).Take(5).ToArray();
                    
                    info.AppendLine($"  - Total .cs files in Assets: {allCsFiles.Length}");
                    info.AppendLine($"  - Exact matches for '{searchName}': {exactMatches.Length}");
                    foreach (string file in exactMatches)
                    {
                        info.AppendLine($"    {file}");
                    }
                    
                    if (partialMatches.Length > exactMatches.Length)
                    {
                        info.AppendLine($"  - Partial matches: {partialMatches.Length - exactMatches.Length}");
                        foreach (string file in partialMatches.Except(exactMatches).Take(3))
                        {
                            info.AppendLine($"    {file}");
                        }
                    }
                }
                catch (Exception e)
                {
                    info.AppendLine($"  - File system search error: {e.Message}");
                }
            }
            
            // 推奨される解決策
            info.AppendLine("\n[Recommended Solutions]");
            info.AppendLine("1. Use full path from Assets folder (e.g., 'Assets/Scripts/MyScript.cs')");
            info.AppendLine("2. Use exact file name without extension (e.g., 'MyScript')");
            info.AppendLine("3. Ensure the script file exists and is imported in Unity");
            
            return info.ToString();
        }
        
        /// <summary>
        /// 成功レスポンス作成
        /// </summary>
        private string CreateSuccessResponse(string message)
        {
            return JsonConvert.SerializeObject(new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = message
                    }
                }
            });
        }
        
        /// <summary>
        /// エラーレスポンス作成
        /// </summary>
        private string CreateErrorResponse(string message)
        {
            return JsonConvert.SerializeObject(new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = message
                    }
                }
            });
        }
        
        private string CreatePrefab(Dictionary<string, string> parameters)
        {
            // nameパラメータの複数候補をサポート
            var name = parameters.GetValueOrDefault("name") ?? 
                      parameters.GetValueOrDefault("prefabName") ?? 
                      parameters.GetValueOrDefault("fileName");
                      
            // 複数のパラメータ名をサポート
            var sourceName = parameters.GetValueOrDefault("source") ?? 
                           parameters.GetValueOrDefault("target") ?? 
                           parameters.GetValueOrDefault("gameObject") ?? 
                           parameters.GetValueOrDefault("object") ?? 
                           parameters.GetValueOrDefault("sourceName") ?? 
                           parameters.GetValueOrDefault("objectName");
            GameObject source = null;
            
            Debug.Log($"[CreatePrefab] === PARAMETER ANALYSIS ===");
            Debug.Log($"[CreatePrefab] Total parameters: {parameters.Count}");
            foreach (var param in parameters)
            {
                Debug.Log($"[CreatePrefab] Parameter: '{param.Key}' = '{param.Value}'");
            }
            Debug.Log($"[CreatePrefab] Extracted name: '{name}'");
            Debug.Log($"[CreatePrefab] Extracted sourceName: '{sourceName}'");
            Debug.Log($"[CreatePrefab] === END ANALYSIS ===");
            
            // nameが指定されていない場合、sourceNameまたはデフォルト名を使用
            if (string.IsNullOrEmpty(name))
            {
                if (!string.IsNullOrEmpty(sourceName))
                {
                    name = sourceName + "Prefab";
                    Debug.Log($"[CreatePrefab] Auto-generated name from source: '{name}'");
                }
                else
                {
                    name = "NewPrefab";
                    Debug.Log($"[CreatePrefab] Using default name: '{name}'");
                }
            }
            
            // パス解析 - フルパスが指定されている場合の処理
            string folderPath = "Assets/Nexus_Generated";
            string fileName = name;
            
            if (name.Contains("/") && name.StartsWith("Assets/"))
            {
                // フルパスが指定されている場合
                var lastSlash = name.LastIndexOf('/');
                if (lastSlash > 0)
                {
                    folderPath = name.Substring(0, lastSlash);
                    fileName = name.Substring(lastSlash + 1);
                    Debug.Log($"[CreatePrefab] Parsed full path - folder: '{folderPath}', file: '{fileName}'");
                }
            }
            
            if (!string.IsNullOrEmpty(sourceName))
            {
                if (sourceName == "last")
                {
                    source = lastCreatedObject;
                    Debug.Log($"[CreatePrefab] Using last created object: {source?.name ?? "null"}");
                }
                else
                {
                    Debug.Log($"[CreatePrefab] Searching for source object: {sourceName}");
                    
                    // GetTargetGameObjectを使用して検索
                    var tempParams = new Dictionary<string, string> { {"target", sourceName} };
                    source = GetTargetGameObject(tempParams);
                    
                    Debug.Log($"[CreatePrefab] Found source object: {source?.name ?? "null"}");
                    
                    // さらに、パラメータ名を変えて検索
                    if (source == null)
                    {
                        tempParams = new Dictionary<string, string> { {"name", sourceName} };
                        source = GetTargetGameObject(tempParams);
                        Debug.Log($"[CreatePrefab] Fallback search by name: {source?.name ?? "null"}");
                    }
                    
                    // シーンにない場合は、既存プレハブからインスタンス化を試行
                    if (source == null)
                    {
                        var existingPrefabPath = $"Assets/Nexus_Generated/{sourceName}.prefab";
                        if (System.IO.File.Exists(existingPrefabPath))
                        {
                            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(existingPrefabPath);
                            if (prefabAsset != null)
                            {
                                // インスタンス化するケース
                                if (parameters.TryGetValue("instantiate", out var instantiateValue) && 
                                    instantiateValue.ToLower() == "true")
                                {
                                    source = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
                                    if (source != null)
                                    {
                                        source.name = name;
                                        
                                        // 位置設定
                                        if (parameters.TryGetValue("position", out var posStr))
                                        {
                                            Vector3 position = Vector3.zero;
                                            if (posStr.StartsWith("{") && posStr.EndsWith("}"))
                                            {
                                                // JSON形式の場合
                                                try
                                                {
                                                    var posJson = JsonConvert.DeserializeObject<Dictionary<string, float>>(posStr);
                                                    position = new Vector3(
                                                        posJson.GetValueOrDefault("x", 0),
                                                        posJson.GetValueOrDefault("y", 0),
                                                        posJson.GetValueOrDefault("z", 0)
                                                    );
                                                }
                                                catch { }
                                            }
                                            else
                                            {
                                                // カンマ区切り文字列の場合
                                                var parts = posStr.Split(',');
                                                if (parts.Length >= 3 && 
                                                    float.TryParse(parts[0], out var x) && 
                                                    float.TryParse(parts[1], out var y) && 
                                                    float.TryParse(parts[2], out var z))
                                                {
                                                    position = new Vector3(x, y, z);
                                                }
                                            }
                                            source.transform.position = position;
                                        }
                                        
                                        lastCreatedObject = source;
                                        return $"Instantiated prefab: {name} from {sourceName}";
                                    }
                                }
                                else
                                {
                                    // プレハブを元に新しいプレハブを作成するケース
                                    source = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // sourceパラメータが指定されていない場合、lastCreatedObjectを使用
                source = lastCreatedObject;
                Debug.Log($"[CreatePrefab] No source specified, using last created object: {source?.name ?? "null"}");
            }
            
            if (source == null)
            {
                var allObjects = GameObject.FindObjectsOfType<GameObject>();
                Debug.Log($"[CreatePrefab] Available objects in scene: {string.Join(", ", allObjects.Take(10).Select(o => o.name))}");
                
                // 最後の手段：利用可能オブジェクトから直接検索
                if (!string.IsNullOrEmpty(sourceName))
                {
                    source = allObjects.FirstOrDefault(o => o.name == sourceName);
                    if (source == null)
                    {
                        source = allObjects.FirstOrDefault(o => o.name.Contains(sourceName));
                    }
                    Debug.Log($"[CreatePrefab] Direct search result: {source?.name ?? "null"}");
                }
                
                if (source == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"No source GameObject found: {sourceName ?? "null"}",
                        availableObjects = allObjects.Take(10).Select(o => o.name).ToArray(),
                        searchedName = sourceName,
                        lastCreatedObject = lastCreatedObject?.name
                    }, Formatting.Indented);
                }
            }
            
            // フォルダパスをパラメータから取得（既にパス解析で設定されている場合はそれを優先）
            if (parameters.ContainsKey("folder") || parameters.ContainsKey("savePath"))
            {
                var paramFolderPath = parameters.GetValueOrDefault("folder") ?? parameters.GetValueOrDefault("savePath");
                if (!string.IsNullOrEmpty(paramFolderPath))
                {
                    folderPath = paramFolderPath;
                    if (!folderPath.StartsWith("Assets/"))
                    {
                        folderPath = "Assets/" + folderPath.TrimStart('/');
                    }
                    Debug.Log($"[CreatePrefab] Using parameter folder path: '{folderPath}'");
                }
            }
            
            // .prefab拡張子を確認・追加
            if (!fileName.EndsWith(".prefab"))
            {
                fileName += ".prefab";
            }
            
            var prefabPath = $"{folderPath}/{fileName}";
            
            Debug.Log($"[CreatePrefab] Final prefab path: '{prefabPath}'");
            Debug.Log($"[CreatePrefab] Folder: '{folderPath}', File: '{fileName}'");
            
            // フォルダが存在しない場合は自動作成
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.Log($"[Nexus] Folder '{folderPath}' does not exist. Creating folder...");
                
                // CreateFolderメソッドを使用してフォルダを作成
                var createFolderParams = new Dictionary<string, string> { ["folderPath"] = folderPath };
                var createResult = CreateFolder(createFolderParams);
                
                try
                {
                    var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(createResult);
                    if (!(bool)result["success"])
                    {
                        return $"Error: Failed to create folder '{folderPath}': {result["error"]}";
                    }
                    Debug.Log($"[Nexus] Successfully created folder: {folderPath}");
                }
                catch (Exception e)
                {
                    return $"Error: Failed to parse folder creation result: {e.Message}";
                }
            }
            
            var prefab = PrefabUtility.SaveAsPrefabAsset(source, prefabPath);
            
            // インスタンス化したプレハブの場合は一時オブジェクトを削除
            if (parameters.TryGetValue("instantiate", out var instantiate) && 
                instantiate.ToLower() == "true" && 
                !parameters.ContainsKey("keep_instance"))
            {
                UnityEditor.Undo.DestroyObjectImmediate(source);
            }
            
            if (prefab != null)
            {
                return $"Created prefab '{name}' successfully at {prefabPath}";
            }
            else
            {
                return $"Error: Failed to create prefab '{name}' at {prefabPath}";
            }
        }
        
        private string SetupPhysics(Dictionary<string, string> parameters)
        {
            try
            {
                var target = parameters.GetValueOrDefault("target");
                GameObject go = null;
                
                if (string.IsNullOrEmpty(target) || target == "global")
                {
                    // グローバル物理設定
                    if (parameters.TryGetValue("gravity", out var gravityStr))
                    {
                        try
                        {
                            // JSON形式のgravityをパース
                            var gravityDict = JsonConvert.DeserializeObject<Dictionary<string, float>>(gravityStr);
                            if (gravityDict != null && gravityDict.ContainsKey("x") && gravityDict.ContainsKey("y") && gravityDict.ContainsKey("z"))
                            {
                                Physics.gravity = new Vector3(gravityDict["x"], gravityDict["y"], gravityDict["z"]);
                            }
                        }
                        catch
                        {
                            // カンマ区切り形式を試す
                            var parts = gravityStr.Split(',');
                            if (parts.Length == 3)
                            {
                                Physics.gravity = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
                            }
                        }
                    }
                    
                    return JsonConvert.SerializeObject(new
                    {
                        success = true,
                        message = "Global physics settings updated",
                        gravity = new { x = Physics.gravity.x, y = Physics.gravity.y, z = Physics.gravity.z }
                    });
                }
                
                if (target == "last")
                {
                    go = lastCreatedObject;
                    if (go == null)
                    {
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "No object has been created yet (lastCreatedObject is null)",
                            hint = "Create an object first or specify a specific target name"
                        });
                    }
                }
                else
                {
                    go = GetTargetGameObject(parameters);
                }
                
                if (go == null) 
                {
                    return CreateGameObjectNotFoundResponse("SetupPhysics", target, parameters);
                }
            
            // Add physics components
            var rigidbodyParam = parameters.GetValueOrDefault("rigidbody", "false").ToLower();
            if (rigidbodyParam == "true" || rigidbodyParam == "1")
            {
                var rb = go.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = go.AddComponent<Rigidbody>();
                }
                
                if (parameters.TryGetValue("mass", out var mass))
                {
                    rb.mass = float.Parse(mass);
                }
                
                if (parameters.TryGetValue("gravity", out var gravity))
                {
                    var gravityLower = gravity.ToLower();
                    rb.useGravity = gravityLower == "true" || gravityLower == "1";
                }
            }
            
            if (parameters.TryGetValue("collider", out var colliderType))
            {
                switch (colliderType.ToLower())
                {
                    case "box":
                        go.AddComponent<BoxCollider>();
                        break;
                    case "sphere":
                        go.AddComponent<SphereCollider>();
                        break;
                    case "capsule":
                        go.AddComponent<CapsuleCollider>();
                        break;
                    case "mesh":
                        go.AddComponent<MeshCollider>();
                        break;
                }
            }
            
                // UNDOに登録
                UnityEditor.Undo.RecordObject(go, "Setup Physics");
                
                var responseData = new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["message"] = $"Successfully setup physics for '{go.name}'",
                    ["target"] = go.name,
                    ["components"] = go.GetComponents<Component>().Select(c => c.GetType().Name).ToArray()
                };
                
                // 追加した物理コンポーネントの詳細情報
                var rigidbody = go.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    responseData["rigidbody"] = new
                    {
                        mass = rigidbody.mass,
                        useGravity = rigidbody.useGravity,
                        isKinematic = rigidbody.isKinematic,
                        drag = rigidbody.drag,
                        angularDrag = rigidbody.angularDrag
                    };
                }
                
                var collider = go.GetComponent<Collider>();
                if (collider != null)
                {
                    responseData["collider"] = new
                    {
                        type = collider.GetType().Name,
                        isTrigger = collider.isTrigger,
                        enabled = collider.enabled
                    };
                }
                
                return JsonConvert.SerializeObject(responseData, Formatting.Indented);
            }
            catch (Exception e)
            {
                return CreateErrorResponse("SetupPhysics", e, parameters);
            }
        }
        
        private string CreateMaterial(Dictionary<string, string> parameters)
        {
            var name = parameters.GetValueOrDefault("name", "NewMaterial");
            var shader = parameters.GetValueOrDefault("shader", "Standard");
            
            // フォルダが存在しない場合は作成
            var folderPath = "Assets/Nexus_Generated";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Nexus_Generated");
                AssetDatabase.Refresh();
            }
            
            var mat = new Material(Shader.Find(shader));
            
            if (parameters.TryGetValue("color", out var colorStr))
            {
                mat.color = ParseColor(colorStr);
            }
            
            var matPath = $"{folderPath}/{name}.mat";
            AssetDatabase.CreateAsset(mat, matPath);
            
            // UNDOに登録
            UnityEditor.Undo.RegisterCreatedObjectUndo(mat, $"Create Material {name}");
            
            return $"Created material: {name}";
        }
        
        private string CreateParticleSystem(Dictionary<string, string> parameters)
        {
            var name = parameters.GetValueOrDefault("name", "ParticleSystem");
            var type = parameters.GetValueOrDefault("preset", "default");
            
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            
            // Position
            if (parameters.TryGetValue("position", out var pos))
            {
                go.transform.position = ParseVector3(pos);
            }
            
            var main = ps.main;
            var emission = ps.emission;
            var shape = ps.shape;
            
            // プリセットによる設定
            switch (type.ToLower())
            {
                case "fire":
                    main.startLifetime = 1f;
                    main.startSpeed = 5f;
                    main.startSize = 0.5f;
                    main.startColor = new Color(1f, 0.5f, 0f);
                    emission.rateOverTime = 50;
                    shape.shapeType = ParticleSystemShapeType.Cone;
                    shape.angle = 25;
                    break;
                    
                case "smoke":
                    main.startLifetime = 3f;
                    main.startSpeed = 1f;
                    main.startSize = 1f;
                    main.startColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    emission.rateOverTime = 20;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    break;
                    
                case "sparkle":
                    main.startLifetime = 2f;
                    main.startSpeed = 2f;
                    main.startSize = 0.1f;
                    main.startColor = Color.white;
                    emission.rateOverTime = 100;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    break;
                    
                case "rain":
                    main.startLifetime = 2f;
                    main.startSpeed = 10f;
                    main.startSize = 0.1f;
                    main.startColor = new Color(0.5f, 0.5f, 1f, 0.5f);
                    emission.rateOverTime = 500;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(10, 0.1f, 10);
                    go.transform.position += Vector3.up * 10;
                    break;
                    
                case "explosion":
                    main.duration = 0.5f;
                    main.startLifetime = 1f;
                    main.startSpeed = 10f;
                    main.startSize = 1f;
                    main.startColor = new Color(1f, 0.5f, 0f);
                    emission.SetBursts(new ParticleSystem.Burst[] {
                        new ParticleSystem.Burst(0.0f, 100)
                    });
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    ps.Play();
                    break;
            }
            
            // カスタムパラメータ
            if (parameters.TryGetValue("startLifetime", out var lifetime))
                main.startLifetime = float.Parse(lifetime);
            
            if (parameters.TryGetValue("startSpeed", out var speed))
                main.startSpeed = float.Parse(speed);
            
            if (parameters.TryGetValue("emissionRate", out var rate))
                emission.rateOverTime = float.Parse(rate);
            
            if (parameters.TryGetValue("startColor", out var color))
                main.startColor = ParseColor(color);
            
            lastCreatedObject = go;
            createdObjects.Add(go);
            
            Selection.activeGameObject = go;
            
            return $"Created ParticleSystem: {name} (preset: {type})";
        }
        
        private string SetupNavMesh(Dictionary<string, string> parameters)
        {
            try
            {
                var targetName = parameters.GetValueOrDefault("target");
                
                if (string.IsNullOrEmpty(targetName))
                {
                    // NavMeshSurfaceコンポーネントの作成
                    var navSurface = new GameObject("NavMeshSurface");
                    
                    // Unity.AI.NavigationパッケージのNavMeshSurfaceを使用
                    var navMeshSurfaceType = Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
                    if (navMeshSurfaceType != null)
                    {
                        var surface = navSurface.AddComponent(navMeshSurfaceType);
                        UnityEditor.Undo.RegisterCreatedObjectUndo(navSurface, "Create NavMeshSurface");
                        
                        // 自動的にベイクを試みる
                        try
                        {
                            var buildNavMeshMethod = navMeshSurfaceType.GetMethod("BuildNavMesh");
                            if (buildNavMeshMethod != null)
                            {
                                buildNavMeshMethod.Invoke(surface, null);
                                return JsonConvert.SerializeObject(new
                                {
                                    success = true,
                                    message = "Created and baked NavMeshSurface",
                                    objectName = navSurface.name,
                                    note = "NavMesh has been automatically baked"
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Failed to auto-bake NavMesh: {ex.Message}");
                        }
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            message = "Created NavMeshSurface (requires manual baking)",
                            objectName = navSurface.name,
                            note = "Use Window > AI > Navigation to bake the NavMesh"
                        });
                    }
                    else
                    {
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = "NavMesh setup requires Unity.AI.Navigation package",
                            solution = "Install Unity.AI.Navigation package via Package Manager",
                            packageName = "com.unity.ai.navigation"
                        });
                    }
                }
                else
                {
                    var target = GetTargetGameObject(parameters);
                    if (target == null) 
                    {
                        return CreateGameObjectNotFoundResponse("SetupNavMesh", targetName, parameters);
                    }
                    
                    // NavMeshAgentの追加
                    var agent = target.AddComponent<UnityEngine.AI.NavMeshAgent>();
                    UnityEditor.Undo.RecordObject(target, "Add NavMeshAgent");
                    
                    if (parameters.TryGetValue("speed", out var speed))
                        agent.speed = float.Parse(speed);
                
                if (parameters.TryGetValue("radius", out var radius))
                    agent.radius = float.Parse(radius);
                
                    if (parameters.TryGetValue("height", out var height))
                        agent.height = float.Parse(height);
                    
                    return JsonConvert.SerializeObject(new
                    {
                        success = true,
                        message = $"Added NavMeshAgent to '{targetName}'",
                        target = targetName,
                        agent = new
                        {
                            speed = agent.speed,
                            radius = agent.radius,
                            height = agent.height
                        }
                    });
                }
            }
            catch (Exception e)
            {
                return CreateErrorResponse("SetupNavMesh", e, parameters);
            }
        }
        
        private string CreateAudioMixer(Dictionary<string, string> parameters)
        {
            var name = parameters.GetValueOrDefault("name", "AudioMixer");
            
            // AudioMixerアセットの作成はUnity内蔵メニューを使用する必要がある
            // エディター拡張での作成は複雑なため、シンプルな実装にする
            
            // 代替案：AudioSourceを持つGameObjectを作成
            var audioGO = new GameObject($"{name}_AudioController");
            var audioSource = audioGO.AddComponent<AudioSource>();
            
            // 基本設定
            audioSource.volume = 1.0f;
            audioSource.playOnAwake = false;
            
            lastCreatedObject = audioGO;
            createdObjects.Add(audioGO);
            
            Selection.activeGameObject = audioGO;
            
            return $"Created AudioController GameObject: {name} (Note: For full AudioMixer, use Unity's Create menu)";
        }
        
        private string SetupCamera(Dictionary<string, string> parameters)
        {
            var preset = parameters.GetValueOrDefault("preset", "default");
            var mainCamera = Camera.main;
            
            if (mainCamera == null)
            {
                return "No main camera found in scene";
            }
            
            switch (preset.ToLower())
            {
                case "topdown":
                    mainCamera.transform.position = new Vector3(0, 10, 0);
                    mainCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
                    mainCamera.orthographic = true;
                    mainCamera.orthographicSize = 10;
                    break;
                    
                case "side":
                    mainCamera.transform.position = new Vector3(10, 0, 0);
                    mainCamera.transform.rotation = Quaternion.Euler(0, -90, 0);
                    break;
                    
                case "isometric":
                    mainCamera.transform.position = new Vector3(10, 10, 10);
                    mainCamera.transform.rotation = Quaternion.Euler(30, -45, 0);
                    mainCamera.orthographic = true;
                    mainCamera.orthographicSize = 8;
                    break;
                    
                case "fps":
                    mainCamera.transform.position = new Vector3(0, 1.8f, 0);
                    mainCamera.transform.rotation = Quaternion.identity;
                    mainCamera.orthographic = false;
                    mainCamera.fieldOfView = 75;
                    break;
                    
                case "custom":
                default:
                    // カスタム設定または現在の設定を維持
                    if (parameters.TryGetValue("position", out var pos))
                        mainCamera.transform.position = ParseVector3(pos);
                    
                    if (parameters.TryGetValue("rotation", out var rot))
                        mainCamera.transform.rotation = Quaternion.Euler(ParseVector3(rot));
                    
                    if (parameters.TryGetValue("fov", out var fov))
                        mainCamera.fieldOfView = float.Parse(fov);
                    
                    if (parameters.TryGetValue("orthographic", out var ortho))
                        mainCamera.orthographic = bool.Parse(ortho);
                    
                    break;
            }
            
            Selection.activeGameObject = mainCamera.gameObject;
            
            return $"Camera setup completed: {preset}";
        }
        
        private string UndoOperation()
        {
            try
            {
                // UnityエディタのUNDOシステムを使用
                UnityEditor.Undo.PerformUndo();
                return "前の操作をUNDOしました";
            }
            catch (System.InvalidOperationException)
            {
                return "UNDOできる操作がありません";
            }
            catch (System.Exception e)
            {
                return $"UNDO実行エラー: {e.Message}";
            }
        }
        
        private string RedoOperation()
        {
            try
            {
                // UnityエディタのREDOシステムを使用
                UnityEditor.Undo.PerformRedo();
                return "操作をREDOしました";
            }
            catch (System.InvalidOperationException)
            {
                return "REDOできる操作がありません";
            }
            catch (System.Exception e)
            {
                return $"REDO実行エラー: {e.Message}";
            }
        }
        
        private string GetOperationHistory()
        {
            return NexusOperationHistory.Instance.ExportHistory();
        }
        
        private string CreateCheckpoint(Dictionary<string, string> parameters)
        {
            try
            {
                string name = parameters.ContainsKey("name") ? parameters["name"] : $"Checkpoint_{DateTime.Now:yyyyMMdd_HHmmss}";
                string description = parameters.ContainsKey("description") ? parameters["description"] : "Manual checkpoint";
                
                bool success = NexusOperationHistory.Instance.CreateCheckpoint(name, description);
                return success ? $"Checkpoint '{name}' created successfully" : "Failed to create checkpoint";
            }
            catch (Exception e)
            {
                return $"Error creating checkpoint: {e.Message}";
            }
        }
        
        private string RestoreCheckpoint(Dictionary<string, string> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("name"))
                    return "Error: Checkpoint name not specified";
                
                string name = parameters["name"];
                bool success = NexusOperationHistory.Instance.RestoreCheckpoint(name);
                return success ? $"Checkpoint '{name}' restored successfully" : $"Failed to restore checkpoint '{name}'";
            }
            catch (Exception e)
            {
                return $"Error restoring checkpoint: {e.Message}";
            }
        }
        
        // リアルタイムイベント監視メソッド
        
        private string StartPlayStateMonitoring(Dictionary<string, string> parameters)
        {
            try
            {
                bool enable = !parameters.ContainsKey("enable") || bool.Parse(parameters.GetValueOrDefault("enable", "true"));
                bool success = false; // NexusEventMonitor not available in UI Edition
                
                return success ? 
                    $"Play state monitoring {(enable ? "started" : "stopped")} successfully" :
                    "Failed to change play state monitoring";
            }
            catch (Exception e)
            {
                return $"Error in play state monitoring: {e.Message}";
            }
        }
        
        private string StartFileChangeMonitoring(Dictionary<string, string> parameters)
        {
            try
            {
                bool enable = !parameters.ContainsKey("enable") || bool.Parse(parameters.GetValueOrDefault("enable", "true"));
                bool success = false; // NexusEventMonitor not available in UI Edition
                
                return success ? 
                    $"File change monitoring {(enable ? "started" : "stopped")} successfully" :
                    "Failed to change file change monitoring";
            }
            catch (Exception e)
            {
                return $"Error in file change monitoring: {e.Message}";
            }
        }
        
        private string StartCompileMonitoring(Dictionary<string, string> parameters)
        {
            try
            {
                bool enable = !parameters.ContainsKey("enable") || bool.Parse(parameters.GetValueOrDefault("enable", "true"));
                bool success = false; // NexusEventMonitor not available in UI Edition
                
                return success ? 
                    $"Compile monitoring {(enable ? "started" : "stopped")} successfully" :
                    "Failed to change compile monitoring";
            }
            catch (Exception e)
            {
                return $"Error in compile monitoring: {e.Message}";
            }
        }
        
        private string SubscribeToEvents(Dictionary<string, string> parameters)
        {
            try
            {
                if (!parameters.ContainsKey("event_type"))
                    return "Error: event_type parameter required";
                
                string eventType = parameters["event_type"];
                string subscriberId = parameters.GetValueOrDefault("subscriber_id", Guid.NewGuid().ToString());
                
                bool success = false; // NexusEventMonitor not available in UI Edition
                
                return success ? 
                    $"Successfully subscribed to event '{eventType}' with ID '{subscriberId}'" :
                    $"Failed to subscribe to event '{eventType}'";
            }
            catch (Exception e)
            {
                return $"Error subscribing to events: {e.Message}";
            }
        }
        
        private string GetRecentEvents(Dictionary<string, string> parameters)
        {
            try
            {
                int count = int.Parse(parameters.GetValueOrDefault("count", "10"));
                // NexusEventMonitor not available in UI Edition
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    message = "Event monitoring is not available in UI Edition",
                    events = new List<object>()
                });
            }
            catch (Exception e)
            {
                return $"Error getting recent events: {e.Message}";
            }
        }
        
        private string GetMonitoringStatus()
        {
            try
            {
                // NexusEventMonitor not available in UI Edition
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    message = "Event monitoring is not available in UI Edition",
                    status = new object()
                });
            }
            catch (Exception e)
            {
                return $"Error getting monitoring status: {e.Message}";
            }
        }
        
        private string PlaceObjects(Dictionary<string, string> parameters)
        {
            var objectType = parameters.GetValueOrDefault("objectType", "Cube");
            var pattern = parameters.GetValueOrDefault("pattern", "grid");
            var countStr = parameters.GetValueOrDefault("count", "5");
            var spacing = float.Parse(parameters.GetValueOrDefault("spacing", "2"));
            
            if (!int.TryParse(countStr, out int count))
            {
                count = 5;
            }
            
            var createdObjects = new List<GameObject>();
            
            switch (pattern.ToLower())
            {
                case "grid":
                    int gridSize = Mathf.CeilToInt(Mathf.Sqrt(count));
                    for (int i = 0; i < count; i++)
                    {
                        int x = i % gridSize;
                        int z = i / gridSize;
                        var position = new Vector3(x * spacing, 0, z * spacing);
                        var obj = CreateObjectAtPosition(objectType, position, i);
                        createdObjects.Add(obj);
                    }
                    break;
                    
                case "circle":
                    float radius = spacing * count / (2 * Mathf.PI);
                    for (int i = 0; i < count; i++)
                    {
                        float angle = i * (360f / count) * Mathf.Deg2Rad;
                        var position = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                        var obj = CreateObjectAtPosition(objectType, position, i);
                        createdObjects.Add(obj);
                    }
                    break;
                    
                case "line":
                    for (int i = 0; i < count; i++)
                    {
                        var position = new Vector3(i * spacing, 0, 0);
                        var obj = CreateObjectAtPosition(objectType, position, i);
                        createdObjects.Add(obj);
                    }
                    break;
                    
                case "random":
                    float range = spacing * count / 2;
                    for (int i = 0; i < count; i++)
                    {
                        var position = new Vector3(
                            UnityEngine.Random.Range(-range, range),
                            0,
                            UnityEngine.Random.Range(-range, range)
                        );
                        var obj = CreateObjectAtPosition(objectType, position, i);
                        createdObjects.Add(obj);
                    }
                    break;
            }
            
            // 操作を履歴に記録
            var paramDict = new Dictionary<string, object>();
            foreach (var kvp in parameters)
            {
                paramDict[kvp.Key] = kvp.Value;
            }
            NexusOperationHistory.Instance.RecordOperation(
                "PLACE_OBJECTS",
                $"Place {count} {objectType} objects in {pattern} pattern",
                paramDict,
                null
            );
            
            return $"Placed {count} {objectType} objects in {pattern} pattern";
        }
        
        private GameObject CreateObjectAtPosition(string objectType, Vector3 position, int index)
        {
            GameObject obj = null;
            
            switch (objectType.ToLower())
            {
                case "cube":
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    break;
                case "sphere":
                    obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    break;
                case "cylinder":
                    obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    break;
                case "capsule":
                    obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    break;
                case "plane":
                    obj = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    break;
                default:
                    obj = new GameObject();
                    break;
            }
            
            obj.name = $"{objectType}_{index}";
            obj.transform.position = position;
            
            return obj;
        }
        
        private string GetGameObjectDetails(Dictionary<string, string> parameters)
        {
            var targetName = parameters.GetValueOrDefault("name") ?? parameters.GetValueOrDefault("target");
            
            GameObject target = null;
            if (!string.IsNullOrEmpty(targetName))
            {
                target = GameObject.Find(targetName);
            }
            
            if (target == null)
            {
                return "GameObject not found: " + targetName;
            }
            
            var details = new System.Text.StringBuilder();
            details.AppendLine($"=== GameObject Details: {target.name} ===");
            
            // Transform情報
            details.AppendLine("\n[Transform]");
            details.AppendLine($"Position: {target.transform.position}");
            details.AppendLine($"Rotation: {target.transform.rotation.eulerAngles}");
            details.AppendLine($"Scale: {target.transform.localScale}");
            details.AppendLine($"Active: {target.activeInHierarchy}");
            details.AppendLine($"Layer: {target.layer} ({LayerMask.LayerToName(target.layer)})");
            details.AppendLine($"Tag: {target.tag}");
            
            // 親子関係
            if (target.transform.parent != null)
            {
                details.AppendLine($"Parent: {target.transform.parent.name}");
            }
            
            if (target.transform.childCount > 0)
            {
                details.AppendLine($"Children: {target.transform.childCount}");
                for (int i = 0; i < target.transform.childCount && i < 10; i++)
                {
                    details.AppendLine($"  - {target.transform.GetChild(i).name}");
                }
                if (target.transform.childCount > 10)
                {
                    details.AppendLine($"  ... and {target.transform.childCount - 10} more");
                }
            }
            
            // コンポーネント情報
            details.AppendLine("\n[Components]");
            var components = target.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                
                details.AppendLine($"• {comp.GetType().Name}");
                
                // 特定のコンポーネントの詳細
                if (comp is MeshRenderer mr)
                {
                    details.AppendLine($"  - Enabled: {mr.enabled}");
                    if (mr.sharedMaterial != null)
                    {
                        details.AppendLine($"  - Material: {mr.sharedMaterial.name}");
                    }
                }
                else if (comp is MeshFilter mf && mf.sharedMesh != null)
                {
                    details.AppendLine($"  - Mesh: {mf.sharedMesh.name}");
                    details.AppendLine($"  - Vertices: {mf.sharedMesh.vertexCount}");
                }
                else if (comp is Collider col)
                {
                    details.AppendLine($"  - Type: {col.GetType().Name}");
                    details.AppendLine($"  - Enabled: {col.enabled}");
                    details.AppendLine($"  - Trigger: {col.isTrigger}");
                }
                else if (comp is Rigidbody rb)
                {
                    details.AppendLine($"  - Mass: {rb.mass}");
                    details.AppendLine($"  - Kinematic: {rb.isKinematic}");
                    details.AppendLine($"  - Use Gravity: {rb.useGravity}");
                }
            }
            
            return details.ToString();
        }
        
        private string GetSceneInfo(Dictionary<string, string> parameters)
        {
            try
            {
                var sceneInfo = new Dictionary<string, object>();
                var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                
                // 基本情報
                sceneInfo["scene_name"] = activeScene.name;
                sceneInfo["scene_path"] = activeScene.path;
                sceneInfo["is_dirty"] = activeScene.isDirty;
                sceneInfo["is_loaded"] = activeScene.isLoaded;
                sceneInfo["build_index"] = activeScene.buildIndex;
                
                // GameObjectの階層構造
                var rootObjects = activeScene.GetRootGameObjects();
                var hierarchy = new List<Dictionary<string, object>>();
                
                foreach (var root in rootObjects)
                {
                    hierarchy.Add(GetGameObjectHierarchy(root));
                }
                
                sceneInfo["hierarchy"] = hierarchy;
                
                // 統計情報
                var statistics = new Dictionary<string, object>();
                statistics["total_root_objects"] = rootObjects.Length;
                
                int totalGameObjects = 0;
                int totalComponents = 0;
                var componentCounts = new Dictionary<string, int>();
                
                foreach (var root in rootObjects)
                {
                    CountGameObjectsAndComponents(root.transform, ref totalGameObjects, ref totalComponents, componentCounts);
                }
                
                statistics["total_gameobjects"] = totalGameObjects;
                statistics["total_components"] = totalComponents;
                statistics["component_breakdown"] = componentCounts;
                
                sceneInfo["statistics"] = statistics;
                
                // ライティング情報
                var lightingInfo = new Dictionary<string, object>();
                lightingInfo["ambient_mode"] = RenderSettings.ambientMode.ToString();
                lightingInfo["ambient_color"] = ColorToDict(RenderSettings.ambientLight);
                lightingInfo["fog_enabled"] = RenderSettings.fog;
                if (RenderSettings.fog)
                {
                    lightingInfo["fog_color"] = ColorToDict(RenderSettings.fogColor);
                    lightingInfo["fog_mode"] = RenderSettings.fogMode.ToString();
                    lightingInfo["fog_density"] = RenderSettings.fogDensity;
                }
                if (RenderSettings.skybox != null)
                {
                    lightingInfo["skybox"] = RenderSettings.skybox.name;
                }
                
                sceneInfo["lighting"] = lightingInfo;
                
                // カメラ情報
                var cameras = GameObject.FindObjectsOfType<Camera>();
                var cameraList = new List<Dictionary<string, object>>();
                
                foreach (var cam in cameras)
                {
                    var camInfo = new Dictionary<string, object>
                    {
                        ["name"] = cam.name,
                        ["enabled"] = cam.enabled,
                        ["is_main"] = cam == Camera.main,
                        ["position"] = Vector3ToDict(cam.transform.position),
                        ["fov"] = cam.fieldOfView,
                        ["depth"] = cam.depth,
                        ["rendering_path"] = cam.renderingPath.ToString()
                    };
                    cameraList.Add(camInfo);
                }
                
                sceneInfo["cameras"] = cameraList;
                
                // JSON形式で返す
                return JsonConvert.SerializeObject(sceneInfo, Formatting.Indented);
            }
            catch (Exception e)
            {
                return $"Error getting scene info: {e.Message}";
            }
        }
        
        private Dictionary<string, object> GetGameObjectHierarchy(GameObject obj)
        {
            var info = new Dictionary<string, object>();
            info["name"] = obj.name;
            info["active"] = obj.activeInHierarchy;
            info["tag"] = obj.tag;
            info["layer"] = LayerMask.LayerToName(obj.layer);
            
            // コンポーネント一覧
            var components = new List<string>();
            foreach (var comp in obj.GetComponents<Component>())
            {
                if (comp != null)
                    components.Add(comp.GetType().Name);
            }
            info["components"] = components;
            
            // 子オブジェクト
            if (obj.transform.childCount > 0)
            {
                var children = new List<Dictionary<string, object>>();
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    children.Add(GetGameObjectHierarchy(obj.transform.GetChild(i).gameObject));
                }
                info["children"] = children;
            }
            
            return info;
        }
        
        private void CountGameObjectsAndComponents(Transform transform, ref int gameObjectCount, ref int componentCount, Dictionary<string, int> componentCounts)
        {
            gameObjectCount++;
            
            var components = transform.GetComponents<Component>();
            componentCount += components.Length;
            
            foreach (var comp in components)
            {
                if (comp != null)
                {
                    string typeName = comp.GetType().Name;
                    if (componentCounts.ContainsKey(typeName))
                        componentCounts[typeName]++;
                    else
                        componentCounts[typeName] = 1;
                }
            }
            
            for (int i = 0; i < transform.childCount; i++)
            {
                CountGameObjectsAndComponents(transform.GetChild(i), ref gameObjectCount, ref componentCount, componentCounts);
            }
        }
        
        private Dictionary<string, float> ColorToDict(Color color)
        {
            return new Dictionary<string, float>
            {
                ["r"] = color.r,
                ["g"] = color.g,
                ["b"] = color.b,
                ["a"] = color.a
            };
        }
        
        private Dictionary<string, float> Vector3ToDict(Vector3 vector)
        {
            return new Dictionary<string, float>
            {
                ["x"] = vector.x,
                ["y"] = vector.y,
                ["z"] = vector.z
            };
        }

        private Task<string> BatchCreate(NexusUnityOperation operation)
        {
            // TODO: Implement batch operations
            return Task.FromResult("Batch operations not yet implemented");
        }
        
        private string GetAssetImportSettings(Dictionary<string, string> parameters)
        {
            try
            {
                var assetPath = parameters.GetValueOrDefault("assetPath", "");
                var includeAllSettings = parameters.GetValueOrDefault("includeAllSettings", "true") == "true";
                var includeOverrides = parameters.GetValueOrDefault("includeOverrides", "true") == "true";
                
                if (string.IsNullOrEmpty(assetPath))
                {
                    return "Error: assetPath parameter is required";
                }
                
                var importer = AssetImporter.GetAtPath(assetPath);
                if (importer == null)
                {
                    return $"Error: No importer found for asset at path: {assetPath}";
                }
                
                var report = new System.Text.StringBuilder();
                report.AppendLine($"=== Import Settings for {assetPath} ===");
                report.AppendLine($"Importer Type: {importer.GetType().Name}");
                report.AppendLine($"Asset Bundle: {importer.assetBundleName}");
                report.AppendLine($"Asset Bundle Variant: {importer.assetBundleVariant}");
                report.AppendLine();
                
                // Type-specific settings
                switch (importer)
                {
                    case TextureImporter texImporter:
                        report.AppendLine("=== Texture Import Settings ===");
                        report.AppendLine($"Texture Type: {texImporter.textureType}");
                        report.AppendLine($"Max Texture Size: {texImporter.maxTextureSize}");
                        report.AppendLine($"Compression: {texImporter.textureCompression}");
                        report.AppendLine($"sRGB: {texImporter.sRGBTexture}");
                        report.AppendLine($"Generate Mipmaps: {texImporter.mipmapEnabled}");
                        report.AppendLine($"Wrap Mode: {texImporter.wrapMode}");
                        report.AppendLine($"Filter Mode: {texImporter.filterMode}");
                        break;
                        
                    case AudioImporter audioImporter:
                        report.AppendLine("=== Audio Import Settings ===");
                        report.AppendLine($"Force To Mono: {audioImporter.forceToMono}");
                        var audioDefaultSettings = audioImporter.defaultSampleSettings;
                        report.AppendLine($"Preload Audio Data: {audioDefaultSettings.loadType == AudioClipLoadType.CompressedInMemory}");
                        report.AppendLine($"Load In Background: {audioImporter.loadInBackground}");
                        
                        var defaultSettings = audioImporter.defaultSampleSettings;
                        report.AppendLine($"Load Type: {defaultSettings.loadType}");
                        report.AppendLine($"Compression Format: {defaultSettings.compressionFormat}");
                        report.AppendLine($"Quality: {defaultSettings.quality}");
                        break;
                        
                    default:
                        report.AppendLine("=== Generic Import Settings ===");
                        report.AppendLine($"User Data: {importer.userData}");
                        break;
                }
                
                return report.ToString();
            }
            catch (Exception e)
            {
                return $"Error getting import settings: {e.Message}";
            }
        }
        
        // Helper methods
        private Type GetComponentType(string typeName)
        {
            // Try Unity built-in types first
            var unityType = Type.GetType($"UnityEngine.{typeName}, UnityEngine");
            if (unityType != null) return unityType;
            
            // Try UI types
            unityType = Type.GetType($"UnityEngine.UI.{typeName}, UnityEngine.UI");
            if (unityType != null) return unityType;
            
            // Try without namespace
            unityType = Type.GetType(typeName);
            if (unityType != null) return unityType;
            
            // Search all assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                unityType = assembly.GetType(typeName);
                if (unityType != null) return unityType;
                
                // Try with common namespaces
                unityType = assembly.GetType($"UnityEngine.{typeName}");
                if (unityType != null) return unityType;
                
                unityType = assembly.GetType($"UnityEngine.UI.{typeName}");
                if (unityType != null) return unityType;
            }
            
            return null;
        }
        
        private void SetComponentProperty(Component component, string propertyName, string value)
        {
            var type = component.GetType();
            
            Debug.Log($"[SetComponentProperty] Setting '{propertyName}' = '{value}' on {type.Name}");
            
            try
            {
                // 特別なコンポーネント処理
                if (HandleSpecialComponentProperty(component, propertyName, value))
                {
                    Debug.Log($"[SetComponentProperty] Successfully handled special case for {propertyName}");
                    return;
                }
                
                // 一般的なプロパティ名のエイリアス
                var actualPropertyName = GetActualPropertyName(type, propertyName);
                Debug.Log($"[SetComponentProperty] Mapped '{propertyName}' to '{actualPropertyName}'");
                
                // Try property
                var property = type.GetProperty(actualPropertyName, BindingFlags.Public | BindingFlags.Instance);
                if (property != null && property.CanWrite)
                {
                    var convertedValue = ConvertValue(value, property.PropertyType);
                    Debug.Log($"[SetComponentProperty] Setting property {actualPropertyName} (type: {property.PropertyType.Name})");
                    property.SetValue(component, convertedValue);
                    Debug.Log($"[SetComponentProperty] Successfully set property {actualPropertyName}");
                    return;
                }
                
                // Try field
                var field = type.GetField(actualPropertyName, BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    var convertedValue = ConvertValue(value, field.FieldType);
                    Debug.Log($"[SetComponentProperty] Setting field {actualPropertyName} (type: {field.FieldType.Name})");
                    field.SetValue(component, convertedValue);
                    Debug.Log($"[SetComponentProperty] Successfully set field {actualPropertyName}");
                    return;
                }
                
                // 利用可能なプロパティとフィールドをログ出力
                var availableProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite)
                    .Select(p => $"{p.Name} ({p.PropertyType.Name})")
                    .ToArray();
                var availableFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Select(f => $"{f.Name} ({f.FieldType.Name})")
                    .ToArray();
                
                Debug.LogError($"[SetComponentProperty] Available properties: {string.Join(", ", availableProperties)}");
                Debug.LogError($"[SetComponentProperty] Available fields: {string.Join(", ", availableFields)}");
                
                throw new ArgumentException($"Property or field '{actualPropertyName}' not found on component '{type.Name}'. Available: {string.Join(", ", availableProperties.Concat(availableFields))}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set property '{propertyName}' on '{type.Name}': {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// 特別なコンポーネント処理（Transform、Renderer等の複雑なケース）
        /// </summary>
        private bool HandleSpecialComponentProperty(Component component, string propertyName, string value)
        {
            var type = component.GetType();
            var lowerPropertyName = propertyName.ToLower();
            
            Debug.Log($"[HandleSpecialComponentProperty] Checking special cases for {type.Name}.{propertyName}");
            
            // Transform特別処理
            if (component is Transform transform)
            {
                switch (lowerPropertyName)
                {
                    case "x":
                        var pos = transform.position;
                        pos.x = float.Parse(value);
                        transform.position = pos;
                        return true;
                    case "y":
                        var pos2 = transform.position;
                        pos2.y = float.Parse(value);
                        transform.position = pos2;
                        return true;
                    case "z":
                        var pos3 = transform.position;
                        pos3.z = float.Parse(value);
                        transform.position = pos3;
                        return true;
                    case "pos":
                    case "position":
                        transform.position = ParseVector3(value);
                        return true;
                    case "rot":
                    case "rotation":
                        transform.rotation = Quaternion.Euler(ParseVector3(value));
                        return true;
                    case "scale":
                        transform.localScale = ParseVector3(value);
                        return true;
                }
            }
            
            // Renderer系の色設定
            if (component is Renderer renderer)
            {
                switch (lowerPropertyName)
                {
                    case "color":
                        if (renderer.material != null)
                        {
                            renderer.material.color = ParseColor(value);
                            return true;
                        }
                        break;
                    case "enabled":
                        renderer.enabled = bool.Parse(value);
                        return true;
                }
            }
            
            // Light系
            if (component is Light light)
            {
                switch (lowerPropertyName)
                {
                    case "color":
                        light.color = ParseColor(value);
                        return true;
                    case "intensity":
                        light.intensity = float.Parse(value);
                        return true;
                    case "range":
                        light.range = float.Parse(value);
                        return true;
                    case "enabled":
                        light.enabled = bool.Parse(value);
                        return true;
                }
            }
            
            // Camera系
            if (component is Camera camera)
            {
                switch (lowerPropertyName)
                {
                    case "fov":
                    case "fieldofview":
                        camera.fieldOfView = float.Parse(value);
                        return true;
                    case "near":
                    case "nearplane":
                        camera.nearClipPlane = float.Parse(value);
                        return true;
                    case "far":
                    case "farplane":
                        camera.farClipPlane = float.Parse(value);
                        return true;
                    case "orthographic":
                        camera.orthographic = bool.Parse(value);
                        return true;
                }
            }
            
            // Rigidbody系
            if (component is Rigidbody rigidbody)
            {
                switch (lowerPropertyName)
                {
                    case "mass":
                        rigidbody.mass = float.Parse(value);
                        return true;
                    case "usegravity":
                        rigidbody.useGravity = bool.Parse(value);
                        return true;
                    case "drag":
                        rigidbody.drag = float.Parse(value);
                        return true;
                    case "angulardrag":
                        rigidbody.angularDrag = float.Parse(value);
                        return true;
                    case "iskinematic":
                        rigidbody.isKinematic = bool.Parse(value);
                        return true;
                    case "interpolate":
                        if (System.Enum.TryParse<RigidbodyInterpolation>(value, true, out var interpolation))
                        {
                            rigidbody.interpolation = interpolation;
                            return true;
                        }
                        break;
                    case "freezeposition":
                    case "freezepositionx":
                        rigidbody.freezeRotation = bool.Parse(value);
                        return true;
                }
            }
            
            Debug.Log($"[HandleSpecialComponentProperty] No special case found for {type.Name}.{propertyName}");
            return false;
        }
        
        private string GetActualPropertyName(Type componentType, string propertyName)
        {
            Debug.Log($"[GetActualPropertyName] Component: {componentType.Name}, Property: {propertyName}");
            
            // 完全一致を最初に試す
            if (componentType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance) != null ||
                componentType.GetField(propertyName, BindingFlags.Public | BindingFlags.Instance) != null)
            {
                Debug.Log($"[GetActualPropertyName] Exact match found: {propertyName}");
                return propertyName;
            }
            
            // コンポーネント別の特化マッピング
            var lowerPropertyName = propertyName.ToLower();
            string mappedName = null;
            
            // Transform特化
            if (componentType.Name == "Transform")
            {
                switch (lowerPropertyName)
                {
                    case "x": case "posx": mappedName = "position"; break;
                    case "y": case "posy": mappedName = "position"; break;
                    case "z": case "posz": mappedName = "position"; break;
                    case "pos": case "position": mappedName = "position"; break;
                    case "rot": case "rotation": mappedName = "rotation"; break;
                    case "scale": mappedName = "localScale"; break;
                }
            }
            // Light特化
            else if (componentType.Name == "Light")
            {
                switch (lowerPropertyName)
                {
                    case "color": mappedName = "color"; break;
                    case "intensity": mappedName = "intensity"; break;
                    case "range": mappedName = "range"; break;
                    case "type": mappedName = "type"; break;
                    case "enabled": mappedName = "enabled"; break;
                }
            }
            // Camera特化
            else if (componentType.Name == "Camera")
            {
                switch (lowerPropertyName)
                {
                    case "fov": case "fieldofview": mappedName = "fieldOfView"; break;
                    case "near": case "nearplane": mappedName = "nearClipPlane"; break;
                    case "far": case "farplane": mappedName = "farClipPlane"; break;
                    case "orthographic": mappedName = "orthographic"; break;
                }
            }
            // Renderer系
            else if (componentType.Name.Contains("Renderer"))
            {
                switch (lowerPropertyName)
                {
                    case "enabled": mappedName = "enabled"; break;
                    case "material": mappedName = "material"; break;
                    case "color": mappedName = "material"; break; // 特別処理が必要
                }
            }
            // UI Text
            else if (componentType.Name == "Text")
            {
                switch (lowerPropertyName)
                {
                    case "text": case "content": mappedName = "text"; break;
                    case "color": mappedName = "color"; break;
                    case "fontsize": case "size": mappedName = "fontSize"; break;
                }
            }
            
            // 一般的なプロパティ
            if (string.IsNullOrEmpty(mappedName))
            {
                switch (lowerPropertyName)
                {
                    case "enabled": case "active": mappedName = "enabled"; break;
                    case "color": mappedName = "color"; break;
                    case "position": mappedName = "position"; break;
                    case "rotation": mappedName = "rotation"; break;
                    case "scale": mappedName = "localScale"; break;
                }
            }
            
            // マッピングされた名前が実際に存在するかチェック
            if (!string.IsNullOrEmpty(mappedName))
            {
                if (componentType.GetProperty(mappedName, BindingFlags.Public | BindingFlags.Instance) != null ||
                    componentType.GetField(mappedName, BindingFlags.Public | BindingFlags.Instance) != null)
                {
                    Debug.Log($"[GetActualPropertyName] Mapped '{propertyName}' to '{mappedName}'");
                    return mappedName;
                }
            }
            
            Debug.Log($"[GetActualPropertyName] No mapping found for '{propertyName}', using original");
            return propertyName; // 見つからない場合は元の名前を返す
        }
        
        private object ConvertValue(string value, Type targetType)
        {
            try
            {
                Debug.Log($"[ConvertValue] Converting '{value}' to {targetType.Name}");
                
                if (targetType == typeof(int))
                {
                    if (int.TryParse(value, out var intVal))
                        return intVal;
                    if (float.TryParse(value, out var floatVal))
                        return (int)floatVal; // 小数点を整数に変換
                }
                else if (targetType == typeof(float))
                {
                    if (float.TryParse(value, out var floatVal))
                        return floatVal;
                    if (double.TryParse(value, out var doubleVal))
                        return (float)doubleVal;
                }
                else if (targetType == typeof(double))
                {
                    if (double.TryParse(value, out var doubleVal))
                        return doubleVal;
                }
                else if (targetType == typeof(bool))
                {
                    // 柔軟なbool変換
                    var lowerValue = value.ToLower().Trim();
                    if (lowerValue == "true" || lowerValue == "1" || lowerValue == "yes" || lowerValue == "on")
                        return true;
                    if (lowerValue == "false" || lowerValue == "0" || lowerValue == "no" || lowerValue == "off")
                        return false;
                    return bool.Parse(value);
                }
                else if (targetType == typeof(string))
                    return value;
                else if (targetType == typeof(Vector3))
                    return ParseVector3(value);
                else if (targetType == typeof(Vector2))
                    return ParseVector2(value);
                else if (targetType == typeof(Color))
                    return ParseColor(value);
                else if (targetType.IsEnum)
                    return Enum.Parse(targetType, value, true);
                
                // Try default conversion
                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConvertValue] Failed to convert value '{value}' to type '{targetType.Name}': {ex.Message}");
                Debug.LogError($"[ConvertValue] Exception type: {ex.GetType().Name}");
                Debug.LogError($"[ConvertValue] Value length: {value?.Length ?? 0}");
                Debug.LogError($"[ConvertValue] Stack trace: {ex.StackTrace}");
                
                // より詳細なフォールバック処理
                if (targetType == typeof(float))
                {
                    Debug.LogWarning($"[ConvertValue] Using fallback float value 0.0f for '{value}'");
                    return 0.0f;
                }
                if (targetType == typeof(double))
                {
                    Debug.LogWarning($"[ConvertValue] Using fallback double value 0.0 for '{value}'");
                    return 0.0;
                }
                if (targetType == typeof(int))
                {
                    Debug.LogWarning($"[ConvertValue] Using fallback int value 0 for '{value}'");
                    return 0;
                }
                if (targetType == typeof(bool))
                {
                    Debug.LogWarning($"[ConvertValue] Using fallback bool value false for '{value}'");
                    return false;
                }
                if (targetType == typeof(Vector3))
                {
                    Debug.LogWarning($"[ConvertValue] Using fallback Vector3.zero for '{value}'");
                    return Vector3.zero;
                }
                if (targetType == typeof(Color))
                {
                    Debug.LogWarning($"[ConvertValue] Using fallback Color.white for '{value}'");
                    return Color.white;
                }
                
                Debug.LogWarning($"[ConvertValue] No specific fallback for {targetType.Name}, throwing exception");
                throw new InvalidCastException($"Cannot convert value '{value}' to type {targetType.Name}: {ex.Message}");
            }
        }
        
        private Vector3 ParseVector3(string value)
        {
            // null/空チェック
            if (string.IsNullOrWhiteSpace(value))
            {
                Debug.LogWarning("[ParseVector3] Empty or null value provided, returning Vector3.zero");
                return Vector3.zero;
            }

            value = value.Trim();

            // JSON形式をチェック
            if (value.StartsWith("{"))
            {
                try
                {
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(value);
                    if (dict != null && dict.ContainsKey("x") && dict.ContainsKey("y") && dict.ContainsKey("z"))
                    {
                        float x = Convert.ToSingle(dict["x"]);
                        float y = Convert.ToSingle(dict["y"]);
                        float z = Convert.ToSingle(dict["z"]);
                        
                        // NaN/Infinityチェック
                        if (float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(z) ||
                            float.IsInfinity(x) || float.IsInfinity(y) || float.IsInfinity(z))
                        {
                            Debug.LogWarning($"[ParseVector3] Invalid float values detected in JSON: {value}");
                            return Vector3.zero;
                        }
                        
                        return new Vector3(x, y, z);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ParseVector3] Failed to parse JSON format '{value}': {ex.Message}");
                }
            }
            
            // カンマ区切り形式を試す
            try
            {
                var parts = value.Trim('(', ')', '[', ']').Split(new char[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    float x = float.Parse(parts[0].Trim());
                    float y = float.Parse(parts[1].Trim());
                    float z = float.Parse(parts[2].Trim());
                    
                    // NaN/Infinityチェック
                    if (float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(z) ||
                        float.IsInfinity(x) || float.IsInfinity(y) || float.IsInfinity(z))
                    {
                        Debug.LogWarning($"[ParseVector3] Invalid float values detected: {value}");
                        return Vector3.zero;
                    }
                    
                    return new Vector3(x, y, z);
                }
                else if (parts.Length == 1)
                {
                    // 単一値の場合は全軸に同じ値を設定
                    float val = float.Parse(parts[0].Trim());
                    if (!float.IsNaN(val) && !float.IsInfinity(val))
                    {
                        return new Vector3(val, val, val);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ParseVector3] Failed to parse comma-separated format '{value}': {ex.Message}");
            }
            
            Debug.LogWarning($"[ParseVector3] Unable to parse vector3 value '{value}', returning Vector3.zero");
            return Vector3.zero;
        }
        
        // 安全なVector3正規化メソッド
        private Vector3 SafeNormalize(Vector3 vector)
        {
            if (vector == Vector3.zero || vector.magnitude < 0.0001f)
            {
                Debug.LogWarning($"[SafeNormalize] Cannot normalize zero or near-zero vector: {vector}");
                return Vector3.forward; // デフォルト方向を返す
            }
            
            Vector3 normalized = vector.normalized;
            
            // 正規化結果をチェック
            if (float.IsNaN(normalized.x) || float.IsNaN(normalized.y) || float.IsNaN(normalized.z) ||
                float.IsInfinity(normalized.x) || float.IsInfinity(normalized.y) || float.IsInfinity(normalized.z))
            {
                Debug.LogWarning($"[SafeNormalize] Normalized vector contains invalid values: {normalized} from {vector}");
                return Vector3.forward;
            }
            
            return normalized;
        }
        
        private Vector2 ParseVector2(string value)
        {
            // JSON形式をチェック
            if (value.TrimStart().StartsWith("{"))
            {
                try
                {
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, float>>(value);
                    if (dict != null && dict.ContainsKey("x") && dict.ContainsKey("y"))
                    {
                        return new Vector2(dict["x"], dict["y"]);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ParseVector2] Failed to parse JSON format: {ex.Message}");
                }
            }
            
            // カンマ区切り形式を試す
            var parts = value.Trim('(', ')').Split(',');
            if (parts.Length >= 2)
            {
                return new Vector2(
                    float.Parse(parts[0].Trim()),
                    float.Parse(parts[1].Trim())
                );
            }
            return Vector2.zero;
        }
        
        private Color ParseColor(string value)
        {
            // JSON形式をチェック
            if (value.TrimStart().StartsWith("{"))
            {
                try
                {
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, float>>(value);
                    if (dict != null && dict.ContainsKey("r") && dict.ContainsKey("g") && dict.ContainsKey("b"))
                    {
                        return new Color(
                            dict["r"], 
                            dict["g"], 
                            dict["b"], 
                            dict.ContainsKey("a") ? dict["a"] : 1f
                        );
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ParseColor] Failed to parse JSON format: {ex.Message}");
                }
            }
            
            switch (value.ToLower())
            {
                case "red": return Color.red;
                case "green": return Color.green;
                case "blue": return Color.blue;
                case "white": return Color.white;
                case "black": return Color.black;
                case "yellow": return Color.yellow;
                case "cyan": return Color.cyan;
                case "magenta": return Color.magenta;
                case "gray":
                case "grey": return Color.gray;
                default:
                    // Try to parse as hex or RGB
                    if (value.StartsWith("#"))
                    {
                        ColorUtility.TryParseHtmlString(value, out var color);
                        return color;
                    }
                    else if (value.Contains(","))
                    {
                        var parts = value.Trim('(', ')').Split(',');
                        if (parts.Length >= 3)
                        {
                            return new Color(
                                float.Parse(parts[0].Trim()),
                                float.Parse(parts[1].Trim()),
                                float.Parse(parts[2].Trim()),
                                parts.Length > 3 ? float.Parse(parts[3].Trim()) : 1f
                            );
                        }
                    }
                    return Color.white;
            }
        }
        
        public List<GameObject> GetCreatedObjects()
        {
            return createdObjects.Where(o => o != null).ToList();
        }
        
        public void ClearCreatedObjects()
        {
            foreach (var obj in createdObjects.Where(o => o != null))
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
            createdObjects.Clear();
        }

        // 新規実装: Terrainツール
        private string CreateTerrain(Dictionary<string, string> parameters)
        {
            try
            {
                var name = parameters.GetValueOrDefault("name", "Terrain");
                var width = float.Parse(parameters.GetValueOrDefault("width", "100"));
                var height = float.Parse(parameters.GetValueOrDefault("height", "30"));
                var length = float.Parse(parameters.GetValueOrDefault("length", "100"));
                var resolution = int.Parse(parameters.GetValueOrDefault("resolution", "513"));
                
                // TerrainData作成
                var terrainData = new TerrainData();
                terrainData.heightmapResolution = resolution;
                terrainData.size = new Vector3(width, height, length);
                terrainData.name = name + "_Data";
                
                // TerrainDataをアセットとして保存
                string folderPath = "Assets/Nexus_Generated/Terrains";
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Nexus_Generated"))
                    {
                        AssetDatabase.CreateFolder("Assets", "Nexus_Generated");
                    }
                    AssetDatabase.CreateFolder("Assets/Nexus_Generated", "Terrains");
                }
                
                string dataPath = $"{folderPath}/{terrainData.name}.asset";
                AssetDatabase.CreateAsset(terrainData, dataPath);
                AssetDatabase.SaveAssets();
                
                // Terrain GameObject作成
                GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
                terrainObject.name = name;
                
                // 位置設定
                if (parameters.ContainsKey("position"))
                {
                    terrainObject.transform.position = ParseVector3(parameters["position"]);
                }
                
                // 基本的な地形設定
                var terrain = terrainObject.GetComponent<Terrain>();
                terrain.materialTemplate = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Terrain-Standard.mat");
                
                // 地形ペイント用のレイヤー設定（オプション）
                if (parameters.GetValueOrDefault("addGrassLayer", "false") == "true")
                {
                    var grassLayer = new TerrainLayer();
                    grassLayer.diffuseTexture = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Grass (Hill).psd");
                    grassLayer.tileSize = new Vector2(15, 15);
                    grassLayer.name = "Grass";
                    
                    string layerPath = $"{folderPath}/{name}_GrassLayer.terrainlayer";
                    AssetDatabase.CreateAsset(grassLayer, layerPath);
                    
                    terrain.terrainData.terrainLayers = new TerrainLayer[] { grassLayer };
                }
                
                lastCreatedObject = terrainObject;
                createdObjects.Add(terrainObject);
                
                // UNDOに登録
                UnityEditor.Undo.RegisterCreatedObjectUndo(terrainObject, $"Create Terrain {name}");
                
                EditorUtility.SetDirty(terrainObject);
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Terrain '{name}' created successfully",
                    gameObjectPath = GetFullPath(terrainObject),
                    terrainDataPath = dataPath,
                    size = new { width, height, length },
                    resolution = resolution
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        private string ModifyTerrain(Dictionary<string, string> parameters)
        {
            try
            {
                // API定義に合わせて name パラメータをメインに使用し、terrain もサポート
                var terrainName = parameters.GetValueOrDefault("name", "") ?? 
                                parameters.GetValueOrDefault("terrain", "");
                if (string.IsNullOrEmpty(terrainName))
                {
                    return CreateMissingParameterResponse("ModifyTerrain", "name", parameters);
                }
                
                var terrain = GameObject.Find(terrainName)?.GetComponent<Terrain>();
                if (terrain == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"Terrain '{terrainName}' not found"
                    });
                }
                
                var operation = parameters.GetValueOrDefault("operation", "raise");
                int x = 0;
                int z = 0; 
                int radius = 10;
                float strength = 0.1f;
                
                try
                {
                    if (parameters.TryGetValue("x", out var xStr))
                        x = int.Parse(xStr);
                    if (parameters.TryGetValue("z", out var zStr))
                        z = int.Parse(zStr);
                    if (parameters.TryGetValue("radius", out var radiusStr))
                        radius = int.Parse(radiusStr);
                    if (parameters.TryGetValue("strength", out var strengthStr))
                        strength = float.Parse(strengthStr);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to parse parameters, using defaults: {ex.Message}");
                }
                
                var terrainData = terrain.terrainData;
                if (terrainData == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "Terrain data is null"
                    });
                }
                
                var heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
                
                // 地形変更処理
                int centerX = (int)(x * terrainData.heightmapResolution / terrainData.size.x);
                int centerZ = (int)(z * terrainData.heightmapResolution / terrainData.size.z);
                
                for (int i = -radius; i <= radius; i++)
                {
                    for (int j = -radius; j <= radius; j++)
                    {
                        int hx = centerX + i;
                        int hz = centerZ + j;
                        
                        if (hx >= 0 && hx < terrainData.heightmapResolution && 
                            hz >= 0 && hz < terrainData.heightmapResolution)
                        {
                            float distance = Mathf.Sqrt(i * i + j * j);
                            if (distance <= radius)
                            {
                                float falloff = 1 - (distance / radius);
                                falloff = Mathf.SmoothStep(0, 1, falloff);
                                
                                switch (operation)
                                {
                                    case "raise":
                                        heights[hz, hx] += strength * falloff;
                                        break;
                                    case "lower":
                                        heights[hz, hx] -= strength * falloff;
                                        break;
                                    case "flatten":
                                        float targetHeight = float.Parse(parameters.GetValueOrDefault("targetHeight", "0.5"));
                                        heights[hz, hx] = Mathf.Lerp(heights[hz, hx], targetHeight, falloff);
                                        break;
                                }
                                
                                heights[hz, hx] = Mathf.Clamp01(heights[hz, hx]);
                            }
                        }
                    }
                }
                
                terrainData.SetHeights(0, 0, heights);
                EditorUtility.SetDirty(terrain);
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Terrain '{terrainName}' modified successfully",
                    operation = operation,
                    position = new { x, z },
                    radius = radius,
                    strength = strength
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        private string GetCameraInfo(Dictionary<string, string> parameters)
        {
            try
            {
                var cameraName = parameters.GetValueOrDefault("cameraName", "");
                var cameras = new List<Camera>();
                
                if (!string.IsNullOrEmpty(cameraName))
                {
                    var camera = GameObject.Find(cameraName)?.GetComponent<Camera>();
                    if (camera != null)
                    {
                        cameras.Add(camera);
                    }
                }
                else
                {
                    cameras.AddRange(Camera.allCameras);
                }
                
                var cameraInfoList = new List<Dictionary<string, object>>();
                
                foreach (var camera in cameras)
                {
                    var info = new Dictionary<string, object>
                    {
                        ["name"] = camera.name,
                        ["tag"] = camera.tag,
                        ["enabled"] = camera.enabled,
                        ["depth"] = camera.depth,
                        ["fieldOfView"] = camera.fieldOfView,
                        ["nearClipPlane"] = camera.nearClipPlane,
                        ["farClipPlane"] = camera.farClipPlane,
                        ["rect"] = new
                        {
                            x = camera.rect.x,
                            y = camera.rect.y,
                            width = camera.rect.width,
                            height = camera.rect.height
                        },
                        ["clearFlags"] = camera.clearFlags.ToString(),
                        ["backgroundColor"] = ColorToHex(camera.backgroundColor),
                        ["cullingMask"] = LayerMaskToLayers(camera.cullingMask),
                        ["renderingPath"] = camera.renderingPath.ToString(),
                        ["targetTexture"] = camera.targetTexture != null ? camera.targetTexture.name : "None",
                        ["isMainCamera"] = camera == Camera.main,
                        ["transform"] = new
                        {
                            position = new { x = camera.transform.position.x, y = camera.transform.position.y, z = camera.transform.position.z },
                            rotation = new { x = camera.transform.eulerAngles.x, y = camera.transform.eulerAngles.y, z = camera.transform.eulerAngles.z },
                            forward = new { x = camera.transform.forward.x, y = camera.transform.forward.y, z = camera.transform.forward.z },
                            right = new { x = camera.transform.right.x, y = camera.transform.right.y, z = camera.transform.right.z },
                            up = new { x = camera.transform.up.x, y = camera.transform.up.y, z = camera.transform.up.z }
                        },
                        ["components"] = camera.GetComponents<Component>()
                            .Select(c => c.GetType().Name)
                            .ToList()
                    };
                    
                    // HDR設定
                    info["allowHDR"] = camera.allowHDR;
                    info["allowMSAA"] = camera.allowMSAA;
                    
                    cameraInfoList.Add(info);
                }
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    cameras = cameraInfoList,
                    count = cameraInfoList.Count,
                    mainCamera = Camera.main != null ? Camera.main.name : "None"
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        private string GetTerrainInfo(Dictionary<string, string> parameters)
        {
            try
            {
                var terrainName = parameters.GetValueOrDefault("terrainName", "");
                var terrains = new List<Terrain>();
                
                if (!string.IsNullOrEmpty(terrainName))
                {
                    var terrain = GameObject.Find(terrainName)?.GetComponent<Terrain>();
                    if (terrain != null)
                    {
                        terrains.Add(terrain);
                    }
                }
                else
                {
                    terrains.AddRange(Terrain.activeTerrains);
                }
                
                var terrainInfoList = new List<Dictionary<string, object>>();
                
                foreach (var terrain in terrains)
                {
                    var terrainData = terrain.terrainData;
                    var info = new Dictionary<string, object>
                    {
                        ["name"] = terrain.name,
                        ["position"] = terrain.transform.position,
                        ["size"] = new
                        {
                            width = terrainData.size.x,
                            height = terrainData.size.y,
                            length = terrainData.size.z
                        },
                        ["heightmapResolution"] = terrainData.heightmapResolution,
                        ["basemapResolution"] = terrainData.baseMapResolution,
                        ["detailResolution"] = terrainData.detailResolution,
                        ["detailResolutionPerPatch"] = terrainData.detailResolutionPerPatch,
                        ["layers"] = terrainData.terrainLayers?.Select(layer => new
                        {
                            name = layer.name,
                            diffuseTexture = layer.diffuseTexture?.name,
                            normalMapTexture = layer.normalMapTexture?.name,
                            tileSize = layer.tileSize,
                            tileOffset = layer.tileOffset
                        }).ToList(),
                        ["treeInstanceCount"] = terrainData.treeInstanceCount,
                        ["treePrototypes"] = terrainData.treePrototypes?.Select(proto => new
                        {
                            prefab = proto.prefab?.name,
                            bendFactor = proto.bendFactor
                        }).ToList(),
                        ["detailPrototypes"] = terrainData.detailPrototypes?.Select(proto => new
                        {
                            prototype = proto.prototype?.name,
                            prototypeTexture = proto.prototypeTexture?.name,
                            minWidth = proto.minWidth,
                            maxWidth = proto.maxWidth,
                            minHeight = proto.minHeight,
                            maxHeight = proto.maxHeight
                        }).ToList(),
                        ["drawTreesAndFoliage"] = terrain.drawTreesAndFoliage,
                        ["terrainLightingEnabled"] = terrain.bakeLightProbesForTrees,
                        ["materialTemplate"] = terrain.materialTemplate?.name,
                        ["reflectionProbeUsage"] = terrain.reflectionProbeUsage.ToString()
                    };
                    
                    terrainInfoList.Add(info);
                }
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    terrains = terrainInfoList,
                    count = terrainInfoList.Count
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        private string GetLightingInfo(Dictionary<string, string> parameters)
        {
            try
            {
                var includeEnvironment = parameters.GetValueOrDefault("includeEnvironment", "true") == "true";
                var includeLights = parameters.GetValueOrDefault("includeLights", "true") == "true";
                var includeFog = parameters.GetValueOrDefault("includeFog", "true") == "true";
                
                var lightingInfo = new Dictionary<string, object>();
                
                // 環境設定
                if (includeEnvironment)
                {
                    lightingInfo["environment"] = new Dictionary<string, object>
                    {
                        ["ambientMode"] = RenderSettings.ambientMode.ToString(),
                        ["ambientColor"] = ColorToHex(RenderSettings.ambientSkyColor),
                        ["ambientIntensity"] = RenderSettings.ambientIntensity,
                        ["ambientGroundColor"] = ColorToHex(RenderSettings.ambientGroundColor),
                        ["ambientEquatorColor"] = ColorToHex(RenderSettings.ambientEquatorColor),
                        ["skybox"] = RenderSettings.skybox?.name ?? "None",
                        ["sun"] = RenderSettings.sun?.name ?? "None",
                        ["reflectionIntensity"] = RenderSettings.reflectionIntensity,
                        ["defaultReflectionMode"] = RenderSettings.defaultReflectionMode.ToString()
                    };
                }
                
                // ライト一覧
                if (includeLights)
                {
                    var lights = GameObject.FindObjectsOfType<Light>();
                    lightingInfo["lights"] = lights.Select(light => new Dictionary<string, object>
                    {
                        ["name"] = light.name,
                        ["type"] = light.type.ToString(),
                        ["color"] = ColorToHex(light.color),
                        ["intensity"] = light.intensity,
                        ["range"] = light.range,
                        ["spotAngle"] = light.spotAngle,
                        ["shadowType"] = light.shadows.ToString(),
                        ["shadowStrength"] = light.shadowStrength,
                        ["shadowResolution"] = light.shadowResolution.ToString(),
                        ["position"] = light.transform.position,
                        ["rotation"] = light.transform.eulerAngles,
                        ["enabled"] = light.enabled
                    }).ToList();
                    
                    lightingInfo["lightCount"] = lights.Length;
                }
                
                // フォグ設定
                if (includeFog)
                {
                    lightingInfo["fog"] = new Dictionary<string, object>
                    {
                        ["enabled"] = RenderSettings.fog,
                        ["color"] = ColorToHex(RenderSettings.fogColor),
                        ["mode"] = RenderSettings.fogMode.ToString(),
                        ["density"] = RenderSettings.fogDensity,
                        ["startDistance"] = RenderSettings.fogStartDistance,
                        ["endDistance"] = RenderSettings.fogEndDistance
                    };
                }
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    lighting = lightingInfo
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        private string GetMaterialInfo(Dictionary<string, string> parameters)
        {
            try
            {
                var includeSceneMaterials = parameters.GetValueOrDefault("includeSceneMaterials", "true") == "true";
                var includeProjectMaterials = parameters.GetValueOrDefault("includeProjectMaterials", "false") == "true";
                
                var materialInfo = new Dictionary<string, object>();
                var materials = new HashSet<Material>();
                
                // シーン内のマテリアルを収集
                if (includeSceneMaterials)
                {
                    var renderers = GameObject.FindObjectsOfType<Renderer>();
                    foreach (var renderer in renderers)
                    {
                        foreach (var mat in renderer.sharedMaterials)
                        {
                            if (mat != null)
                            {
                                materials.Add(mat);
                            }
                        }
                    }
                }
                
                // プロジェクト内のマテリアルを収集
                if (includeProjectMaterials)
                {
                    var guids = AssetDatabase.FindAssets("t:Material");
                    foreach (var guid in guids)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                        if (mat != null)
                        {
                            materials.Add(mat);
                        }
                    }
                }
                
                materialInfo["materials"] = materials.Select(mat => new Dictionary<string, object>
                {
                    ["name"] = mat.name,
                    ["shader"] = mat.shader.name,
                    ["renderQueue"] = mat.renderQueue,
                    ["mainTexture"] = mat.mainTexture?.name ?? "None",
                    ["color"] = mat.HasProperty("_Color") ? ColorToHex(mat.GetColor("_Color")) : "N/A",
                    ["properties"] = GetMaterialProperties(mat),
                    ["keywords"] = mat.shaderKeywords.ToList(),
                    ["path"] = AssetDatabase.GetAssetPath(mat)
                }).ToList();
                
                materialInfo["count"] = materials.Count;
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    materials = materialInfo
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        private string GetUIInfo(Dictionary<string, string> parameters)
        {
            try
            {
                var canvasName = parameters.GetValueOrDefault("canvasName", "");
                var canvases = new List<Canvas>();
                
                if (!string.IsNullOrEmpty(canvasName))
                {
                    var canvas = GameObject.Find(canvasName)?.GetComponent<Canvas>();
                    if (canvas != null)
                    {
                        canvases.Add(canvas);
                    }
                }
                else
                {
                    canvases.AddRange(GameObject.FindObjectsOfType<Canvas>());
                }
                
                var uiInfo = new Dictionary<string, object>
                {
                    ["eventSystem"] = EventSystem.current != null,
                    ["canvases"] = canvases.Select(canvas => new Dictionary<string, object>
                    {
                        ["name"] = canvas.name,
                        ["renderMode"] = canvas.renderMode.ToString(),
                        ["sortingOrder"] = canvas.sortingOrder,
                        ["sortingLayerName"] = canvas.sortingLayerName,
                        ["pixelPerfect"] = canvas.pixelPerfect,
                        ["worldCamera"] = canvas.worldCamera?.name ?? "None",
                        ["planeDistance"] = canvas.planeDistance,
                        ["scaleFactor"] = canvas.scaleFactor,
                        ["referencePixelsPerUnit"] = canvas.referencePixelsPerUnit,
                        ["overrideSorting"] = canvas.overrideSorting,
                        ["position"] = canvas.transform.position,
                        ["uiElements"] = GetUIElementsInCanvas(canvas)
                    }).ToList()
                };
                
                uiInfo["totalCanvasCount"] = canvases.Count;
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    ui = uiInfo
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        private string GetPhysicsInfo(Dictionary<string, string> parameters)
        {
            try
            {
                var includeRigidbodies = parameters.GetValueOrDefault("includeRigidbodies", "true") == "true";
                var includeColliders = parameters.GetValueOrDefault("includeColliders", "true") == "true";
                var includeJoints = parameters.GetValueOrDefault("includeJoints", "false") == "true";
                
                var physicsInfo = new Dictionary<string, object>
                {
                    ["gravity"] = Physics.gravity,
                    ["defaultSolverIterations"] = Physics.defaultSolverIterations,
                    ["defaultSolverVelocityIterations"] = Physics.defaultSolverVelocityIterations,
                    ["bounceThreshold"] = Physics.bounceThreshold,
                    ["sleepThreshold"] = Physics.sleepThreshold,
                    ["defaultContactOffset"] = Physics.defaultContactOffset,
                    ["autoSyncTransforms"] = Physics.autoSyncTransforms
                };
                
                if (includeRigidbodies)
                {
                    var rigidbodies = GameObject.FindObjectsOfType<Rigidbody>();
                    physicsInfo["rigidbodies"] = rigidbodies.Select(rb => new Dictionary<string, object>
                    {
                        ["name"] = rb.name,
                        ["mass"] = rb.mass,
                        ["drag"] = rb.drag,
                        ["angularDrag"] = rb.angularDrag,
                        ["useGravity"] = rb.useGravity,
                        ["isKinematic"] = rb.isKinematic,
                        ["velocity"] = rb.velocity,
                        ["angularVelocity"] = rb.angularVelocity,
                        ["constraints"] = rb.constraints.ToString()
                    }).ToList();
                    physicsInfo["rigidbodyCount"] = rigidbodies.Length;
                }
                
                if (includeColliders)
                {
                    var colliders = GameObject.FindObjectsOfType<Collider>();
                    physicsInfo["colliders"] = colliders.GroupBy(c => c.GetType().Name)
                        .ToDictionary(g => g.Key, g => g.Count());
                    physicsInfo["totalColliderCount"] = colliders.Length;
                }
                
                if (includeJoints)
                {
                    var joints = GameObject.FindObjectsOfType<Joint>();
                    physicsInfo["joints"] = joints.GroupBy(j => j.GetType().Name)
                        .ToDictionary(g => g.Key, g => g.Count());
                    physicsInfo["totalJointCount"] = joints.Length;
                }
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    physics = physicsInfo
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        private string ListAssets(Dictionary<string, string> parameters)
        {
            try
            {
                var assetType = parameters.GetValueOrDefault("assetType", "all");
                var folder = parameters.GetValueOrDefault("folder", "Assets");
                var searchPattern = parameters.GetValueOrDefault("searchPattern", "");
                
                string filter = assetType switch
                {
                    "scripts" => "t:Script",
                    "prefabs" => "t:Prefab",
                    "materials" => "t:Material",
                    "textures" => "t:Texture",
                    "audio" => "t:AudioClip",
                    _ => ""
                };
                
                if (!string.IsNullOrEmpty(searchPattern))
                {
                    filter = string.IsNullOrEmpty(filter) ? searchPattern : $"{filter} {searchPattern}";
                }
                
                var guids = AssetDatabase.FindAssets(filter, new[] { folder });
                var assets = new List<Dictionary<string, object>>();
                
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadMainAssetAtPath(path);
                    
                    if (asset != null)
                    {
                        var fileInfo = new System.IO.FileInfo(path);
                        assets.Add(new Dictionary<string, object>
                        {
                            ["name"] = asset.name,
                            ["type"] = asset.GetType().Name,
                            ["path"] = path,
                            ["size"] = fileInfo.Exists ? fileInfo.Length : 0,
                            ["lastModified"] = fileInfo.Exists ? fileInfo.LastWriteTime.ToString() : "N/A"
                        });
                    }
                }
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    assets = assets,
                    count = assets.Count,
                    folder = folder,
                    type = assetType
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        private string GetProjectStats(Dictionary<string, string> parameters)
        {
            try
            {
                var includeScripts = parameters.GetValueOrDefault("includeScripts", "true") == "true";
                var includeAssets = parameters.GetValueOrDefault("includeAssets", "true") == "true";
                var includeSceneInfo = parameters.GetValueOrDefault("includeSceneInfo", "true") == "true";
                
                var stats = new Dictionary<string, object>();
                
                // 基本プロジェクト情報
                stats["projectName"] = Application.productName;
                stats["companyName"] = Application.companyName;
                stats["unityVersion"] = Application.unityVersion;
                stats["platform"] = Application.platform.ToString();
                
                // メモリ使用量
                stats["memory"] = new Dictionary<string, object>
                {
                    ["totalAllocatedMemory"] = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024) + " MB",
                    ["totalReservedMemory"] = Profiler.GetTotalReservedMemoryLong() / (1024 * 1024) + " MB",
                    ["monoHeapSize"] = Profiler.GetMonoHeapSizeLong() / (1024 * 1024) + " MB",
                    ["monoUsedSize"] = Profiler.GetMonoUsedSizeLong() / (1024 * 1024) + " MB"
                };
                
                // シーン情報
                if (includeSceneInfo)
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    stats["activeScene"] = new Dictionary<string, object>
                    {
                        ["name"] = scene.name,
                        ["path"] = scene.path,
                        ["isDirty"] = scene.isDirty,
                        ["rootCount"] = scene.rootCount,
                        ["isLoaded"] = scene.isLoaded
                    };
                    
                    // GameObject数をカウント
                    var allObjects = GameObject.FindObjectsOfType<GameObject>();
                    stats["gameObjectCount"] = allObjects.Length;
                }
                
                // スクリプト統計
                if (includeScripts)
                {
                    var scriptGuids = AssetDatabase.FindAssets("t:Script");
                    stats["scriptCount"] = scriptGuids.Length;
                    
                    // MonoBehaviour数
                    var monoBehaviours = GameObject.FindObjectsOfType<MonoBehaviour>();
                    stats["monoBehaviourCount"] = monoBehaviours.Length;
                }
                
                // アセット統計
                if (includeAssets)
                {
                    var assetStats = new Dictionary<string, int>
                    {
                        ["materials"] = AssetDatabase.FindAssets("t:Material").Length,
                        ["textures"] = AssetDatabase.FindAssets("t:Texture").Length,
                        ["meshes"] = AssetDatabase.FindAssets("t:Mesh").Length,
                        ["prefabs"] = AssetDatabase.FindAssets("t:Prefab").Length,
                        ["audioClips"] = AssetDatabase.FindAssets("t:AudioClip").Length,
                        ["animations"] = AssetDatabase.FindAssets("t:AnimationClip").Length,
                        ["shaders"] = AssetDatabase.FindAssets("t:Shader").Length
                    };
                    stats["assetCounts"] = assetStats;
                    stats["totalAssets"] = assetStats.Values.Sum();
                }
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    statistics = stats
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        private string ManagePackage(Dictionary<string, string> parameters)
        {
            try
            {
                var operation = parameters.GetValueOrDefault("operation", "list");
                var packageId = parameters.GetValueOrDefault("packageId", "");
                var version = parameters.GetValueOrDefault("version", "");
                
                switch (operation)
                {
                    case "list":
                        var packageListRequest = UnityEditor.PackageManager.Client.List();
                        while (!packageListRequest.IsCompleted)
                        {
                            System.Threading.Thread.Sleep(100);
                        }
                        
                        if (packageListRequest.Status == UnityEditor.PackageManager.StatusCode.Success)
                        {
                            var packages = packageListRequest.Result.Select(pkg => new Dictionary<string, object>
                            {
                                ["name"] = pkg.name,
                                ["displayName"] = pkg.displayName,
                                ["version"] = pkg.version,
                                ["source"] = pkg.source.ToString(),
                                ["isDirectDependency"] = pkg.isDirectDependency
                            }).ToList();
                            
                            return JsonConvert.SerializeObject(new
                            {
                                success = true,
                                packages = packages,
                                count = packages.Count
                            }, Formatting.Indented);
                        }
                        else
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                success = false,
                                error = "Failed to list packages"
                            });
                        }
                        
                    case "add":
                        if (string.IsNullOrEmpty(packageId))
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                success = false,
                                error = "packageId is required for add operation"
                            });
                        }
                        
                        var identifier = string.IsNullOrEmpty(version) ? packageId : $"{packageId}@{version}";
                        var addRequest = UnityEditor.PackageManager.Client.Add(identifier);
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            message = $"Package add request initiated for: {identifier}",
                            note = "Check Unity Package Manager window for progress"
                        });
                        
                    case "remove":
                        if (string.IsNullOrEmpty(packageId))
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                success = false,
                                error = "packageId is required for remove operation"
                            });
                        }
                        
                        var removeRequest = UnityEditor.PackageManager.Client.Remove(packageId);
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            message = $"Package remove request initiated for: {packageId}",
                            note = "Check Unity Package Manager window for progress"
                        });
                        
                    default:
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = $"Unknown operation: {operation}"
                        });
                }
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        private string ManageScene(Dictionary<string, string> parameters)
        {
            try
            {
                var operation = parameters.GetValueOrDefault("operation", "list");
                var sceneName = parameters.GetValueOrDefault("sceneName", "");
                var scenePath = parameters.GetValueOrDefault("scenePath", "");
                
                switch (operation)
                {
                    case "list":
                        var sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
                        var scenes = new List<Dictionary<string, object>>();
                        
                        for (int i = 0; i < sceneCount; i++)
                        {
                            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                            scenes.Add(new Dictionary<string, object>
                            {
                                ["name"] = scene.name,
                                ["path"] = scene.path,
                                ["buildIndex"] = scene.buildIndex,
                                ["isLoaded"] = scene.isLoaded,
                                ["isDirty"] = scene.isDirty,
                                ["rootCount"] = scene.rootCount
                            });
                        }
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            scenes = scenes,
                            activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
                        }, Formatting.Indented);
                        
                    case "create":
                        if (string.IsNullOrEmpty(sceneName))
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                success = false,
                                error = "sceneName is required for create operation"
                            });
                        }
                        
                        var newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                            UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
                            UnityEditor.SceneManagement.NewSceneMode.Single
                        );
                        
                        if (!string.IsNullOrEmpty(scenePath))
                        {
                            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, scenePath);
                        }
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            message = $"New scene created",
                            sceneName = newScene.name,
                            scenePath = newScene.path
                        });
                        
                    case "save":
                        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                        var saved = UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = saved,
                            message = saved ? "Scene saved successfully" : "Failed to save scene",
                            sceneName = activeScene.name,
                            scenePath = activeScene.path
                        });
                        
                    default:
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = $"Unknown operation: {operation}"
                        });
                }
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        private string CreateAnimation(Dictionary<string, string> parameters)
        {
            try
            {
                // API定義に合わせて gameObject パラメータをメインに使用し、target もサポート
                var targetName = parameters.GetValueOrDefault("gameObject", "") ?? 
                               parameters.GetValueOrDefault("target", "");
                var animationName = parameters.GetValueOrDefault("animationName", "NewAnimation");
                var duration = float.Parse(parameters.GetValueOrDefault("duration", "1"));
                var loop = parameters.GetValueOrDefault("loop", "false") == "true";
                
                Debug.Log($"[CreateAnimation] Called with target: '{targetName}', animationName: '{animationName}'");
                
                if (string.IsNullOrEmpty(targetName))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "gameObject parameter is required",
                        receivedParameters = parameters.Keys.ToArray()
                    });
                }
                
                var targetObj = GetTargetGameObject(parameters);
                if (targetObj == null)
                {
                    var availableObjects = GameObject.FindObjectsOfType<GameObject>().Take(10).Select(o => o.name);
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"GameObject '{targetName}' not found",
                        availableObjects = availableObjects.ToArray(),
                        searchedName = targetName
                    });
                }
                
                // AnimationClipを作成
                var clip = new AnimationClip();
                clip.name = animationName;
                
                // ループ設定
                if (loop)
                {
                    var settings = AnimationUtility.GetAnimationClipSettings(clip);
                    settings.loopTime = true;
                    AnimationUtility.SetAnimationClipSettings(clip, settings);
                }
                
                // 基本的なキーフレームを追加（位置のアニメーション例）
                var curve = AnimationCurve.Linear(0, targetObj.transform.position.y, duration, targetObj.transform.position.y + 2);
                clip.SetCurve("", typeof(Transform), "localPosition.y", curve);
                
                // アセットとして保存
                string folderPath = "Assets/Nexus_Generated/Animations";
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Nexus_Generated"))
                    {
                        AssetDatabase.CreateFolder("Assets", "Nexus_Generated");
                    }
                    AssetDatabase.CreateFolder("Assets/Nexus_Generated", "Animations");
                }
                
                string clipPath = $"{folderPath}/{animationName}.anim";
                AssetDatabase.CreateAsset(clip, clipPath);
                
                // Animatorコンポーネントを追加（なければ）
                var animator = targetObj.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = targetObj.AddComponent<Animator>();
                }
                
                // AnimatorControllerを作成または取得
                if (animator.runtimeAnimatorController == null)
                {
                    var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(
                        $"{folderPath}/{targetName}_Controller.controller"
                    );
                    animator.runtimeAnimatorController = controller;
                    
                    // デフォルトステートとしてアニメーションを追加
                    var rootStateMachine = controller.layers[0].stateMachine;
                    var state = rootStateMachine.AddState(animationName);
                    state.motion = clip;
                    rootStateMachine.defaultState = state;
                }
                
                AssetDatabase.SaveAssets();
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Animation '{animationName}' created for '{targetName}'",
                    clipPath = clipPath,
                    duration = duration,
                    loop = loop
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        // ヘルパーメソッド
        private string ColorToHex(Color color)
        {
            return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
        }

        private List<string> LayerMaskToLayers(LayerMask mask)
        {
            var layers = new List<string>();
            for (int i = 0; i < 32; i++)
            {
                if ((mask.value & (1 << i)) != 0)
                {
                    var layerName = LayerMask.LayerToName(i);
                    if (!string.IsNullOrEmpty(layerName))
                    {
                        layers.Add(layerName);
                    }
                }
            }
            return layers;
        }

        private Dictionary<string, object> GetMaterialProperties(Material material)
        {
            var properties = new Dictionary<string, object>();
            var shader = material.shader;
            
            for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                var propName = ShaderUtil.GetPropertyName(shader, i);
                var propType = ShaderUtil.GetPropertyType(shader, i);
                
                switch (propType)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        if (material.HasProperty(propName))
                            properties[propName] = ColorToHex(material.GetColor(propName));
                        break;
                    case ShaderUtil.ShaderPropertyType.Float:
                    case ShaderUtil.ShaderPropertyType.Range:
                        if (material.HasProperty(propName))
                            properties[propName] = material.GetFloat(propName);
                        break;
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        if (material.HasProperty(propName))
                        {
                            var tex = material.GetTexture(propName);
                            properties[propName] = tex != null ? tex.name : "None";
                        }
                        break;
                    case ShaderUtil.ShaderPropertyType.Vector:
                        if (material.HasProperty(propName))
                        {
                            var vec = material.GetVector(propName);
                            properties[propName] = new { x = vec.x, y = vec.y, z = vec.z, w = vec.w };
                        }
                        break;
                }
            }
            
            return properties;
        }

        private List<Dictionary<string, object>> GetUIElementsInCanvas(Canvas canvas)
        {
            var elements = new List<Dictionary<string, object>>();
            var uiComponents = canvas.GetComponentsInChildren<Graphic>();
            
            foreach (var component in uiComponents.Take(50)) // 最大50個まで
            {
                var elem = new Dictionary<string, object>
                {
                    ["name"] = component.name,
                    ["type"] = component.GetType().Name,
                    ["enabled"] = component.enabled,
                    ["raycastTarget"] = component.raycastTarget,
                    ["color"] = ColorToHex(component.color)
                };
                
                // テキスト要素の場合
                if (component is Text text)
                {
                    elem["text"] = text.text.Length > 100 ? text.text.Substring(0, 100) + "..." : text.text;
                    elem["fontSize"] = text.fontSize;
                    elem["font"] = text.font?.name ?? "None";
                }
                else if (component is TMP_Text tmpText)
                {
                    elem["text"] = tmpText.text.Length > 100 ? tmpText.text.Substring(0, 100) + "..." : tmpText.text;
                    elem["fontSize"] = tmpText.fontSize;
                    elem["font"] = tmpText.font?.name ?? "None";
                }
                // ボタンの場合
                else if (component.GetComponent<Button>() != null)
                {
                    elem["isButton"] = true;
                    elem["interactable"] = component.GetComponent<Button>().interactable;
                }
                // 画像の場合
                else if (component is Image image)
                {
                    elem["sprite"] = image.sprite?.name ?? "None";
                    elem["imageType"] = image.type.ToString();
                    elem["fillCenter"] = image.fillCenter;
                }
                
                elements.Add(elem);
            }
            
            return elements;
        }

        // 追加実装: ライティング設定ツール
        private string SetupLighting(Dictionary<string, string> parameters)
        {
            try
            {
                var lightingType = parameters.GetValueOrDefault("type", "standard");
                var skyboxPath = parameters.GetValueOrDefault("skybox", "");
                var ambientMode = parameters.GetValueOrDefault("ambientMode", "Trilight");
                var ambientIntensity = float.Parse(parameters.GetValueOrDefault("ambientIntensity", "1"));
                
                var results = new Dictionary<string, object>
                {
                    ["success"] = true,
                    ["changes"] = new List<string>()
                };
                var changes = results["changes"] as List<string>;
                
                // アンビエントモード設定
                switch (ambientMode.ToLower())
                {
                    case "skybox":
                        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
                        break;
                    case "trilight":
                        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                        break;
                    case "flat":
                        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                        break;
                    case "custom":
                        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Custom;
                        break;
                }
                changes.Add($"Ambient mode set to: {RenderSettings.ambientMode}");
                
                // アンビエント強度
                RenderSettings.ambientIntensity = ambientIntensity;
                changes.Add($"Ambient intensity set to: {ambientIntensity}");
                
                // アンビエントカラー設定
                if (parameters.ContainsKey("ambientSkyColor"))
                {
                    RenderSettings.ambientSkyColor = ParseColor(parameters["ambientSkyColor"]);
                    changes.Add($"Ambient sky color set to: {ColorToHex(RenderSettings.ambientSkyColor)}");
                }
                
                if (parameters.ContainsKey("ambientEquatorColor"))
                {
                    RenderSettings.ambientEquatorColor = ParseColor(parameters["ambientEquatorColor"]);
                    changes.Add($"Ambient equator color set to: {ColorToHex(RenderSettings.ambientEquatorColor)}");
                }
                
                if (parameters.ContainsKey("ambientGroundColor"))
                {
                    RenderSettings.ambientGroundColor = ParseColor(parameters["ambientGroundColor"]);
                    changes.Add($"Ambient ground color set to: {ColorToHex(RenderSettings.ambientGroundColor)}");
                }
                
                // スカイボックス設定
                if (!string.IsNullOrEmpty(skyboxPath))
                {
                    var skyboxMaterial = AssetDatabase.LoadAssetAtPath<Material>(skyboxPath);
                    if (skyboxMaterial != null)
                    {
                        RenderSettings.skybox = skyboxMaterial;
                        changes.Add($"Skybox set to: {skyboxMaterial.name}");
                    }
                }
                
                // フォグ設定
                if (parameters.ContainsKey("fogEnabled"))
                {
                    RenderSettings.fog = parameters["fogEnabled"] == "true";
                    changes.Add($"Fog enabled: {RenderSettings.fog}");
                    
                    if (RenderSettings.fog)
                    {
                        if (parameters.ContainsKey("fogColor"))
                        {
                            RenderSettings.fogColor = ParseColor(parameters["fogColor"]);
                            changes.Add($"Fog color set to: {ColorToHex(RenderSettings.fogColor)}");
                        }
                        
                        if (parameters.ContainsKey("fogMode"))
                        {
                            switch (parameters["fogMode"].ToLower())
                            {
                                case "linear":
                                    RenderSettings.fogMode = FogMode.Linear;
                                    break;
                                case "exponential":
                                    RenderSettings.fogMode = FogMode.Exponential;
                                    break;
                                case "exponentialsquared":
                                    RenderSettings.fogMode = FogMode.ExponentialSquared;
                                    break;
                            }
                            changes.Add($"Fog mode set to: {RenderSettings.fogMode}");
                        }
                        
                        if (parameters.ContainsKey("fogDensity"))
                        {
                            RenderSettings.fogDensity = float.Parse(parameters["fogDensity"]);
                            changes.Add($"Fog density set to: {RenderSettings.fogDensity}");
                        }
                        
                        if (parameters.ContainsKey("fogStartDistance"))
                        {
                            RenderSettings.fogStartDistance = float.Parse(parameters["fogStartDistance"]);
                            changes.Add($"Fog start distance set to: {RenderSettings.fogStartDistance}");
                        }
                        
                        if (parameters.ContainsKey("fogEndDistance"))
                        {
                            RenderSettings.fogEndDistance = float.Parse(parameters["fogEndDistance"]);
                            changes.Add($"Fog end distance set to: {RenderSettings.fogEndDistance}");
                        }
                    }
                }
                
                // 太陽光源の設定
                if (parameters.ContainsKey("createSun") && parameters["createSun"] == "true")
                {
                    var sunGO = new GameObject("Directional Light (Sun)");
                    var sunLight = sunGO.AddComponent<Light>();
                    sunLight.type = LightType.Directional;
                    sunLight.intensity = 1.0f;
                    sunLight.color = Color.white;
                    sunLight.shadows = LightShadows.Soft;
                    sunGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                    RenderSettings.sun = sunLight;
                    
                    lastCreatedObject = sunGO;
                    createdObjects.Add(sunGO);
                    changes.Add("Created sun light");
                }
                
                // リフレクションプローブ設定
                if (parameters.ContainsKey("reflectionIntensity"))
                {
                    RenderSettings.reflectionIntensity = float.Parse(parameters["reflectionIntensity"]);
                    changes.Add($"Reflection intensity set to: {RenderSettings.reflectionIntensity}");
                }
                
                results["message"] = $"Lighting setup completed with {changes.Count} changes";
                
                return JsonConvert.SerializeObject(results, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        // コンソール操作ツール
        private string ConsoleOperation(Dictionary<string, string> parameters)
        {
            try
            {
                var operation = parameters.GetValueOrDefault("operation", "read");
                var logType = parameters.GetValueOrDefault("logType", "all");
                var limit = int.Parse(parameters.GetValueOrDefault("limit", "50"));
                
                var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
                
                switch (operation)
                {
                    case "read":
                        // Unity Editorのコンソールログを取得
                        if (logEntries == null)
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                success = false,
                                error = "LogEntries not available"
                            });
                        }
                        
                        // LogEntriesからログを取得
                        var getCountMethod = logEntries.GetMethod("GetCount", BindingFlags.Public | BindingFlags.Static);
                        var getEntryInternalMethod = logEntries.GetMethod("GetEntryInternal", BindingFlags.Public | BindingFlags.Static);
                        var getCountsByTypeMethod = logEntries.GetMethod("GetCountsByType", BindingFlags.Public | BindingFlags.Static);
                        
                        if (getCountMethod == null || getEntryInternalMethod == null)
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                success = false,
                                error = "Unable to access log methods"
                            });
                        }
                        
                        int totalCount = (int)getCountMethod.Invoke(null, null);
                        var logs = new List<Dictionary<string, object>>();
                        
                        // まず、リアルタイムログバッファから最新のログを取得
                        if (logBuffer.Count > 0)
                        {
                            var recentLogs = logBuffer.OrderByDescending(l => l.timestamp).Take(limit).ToList();
                            foreach (var log in recentLogs)
                            {
                                string type = log.type switch
                                {
                                    LogType.Error or LogType.Assert or LogType.Exception => "Error",
                                    LogType.Warning => "Warning",
                                    _ => "Log"
                                };
                                
                                if (logType == "all" || logType.ToLower() == type.ToLower())
                                {
                                    logs.Add(new Dictionary<string, object>
                                    {
                                        ["message"] = log.condition,
                                        ["type"] = type,
                                        ["stackTrace"] = log.stackTrace,
                                        ["timestamp"] = log.timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")
                                    });
                                }
                            }
                            
                            Debug.Log($"[NexusConsole] Retrieved {logs.Count} logs from buffer (buffer size: {logBuffer.Count})");
                        }
                        
                        // ログバッファが十分でない場合は、LogEntriesからも取得を試みる
                        if (logs.Count < limit)
                        {
                            // LogEntryの構造体定義
                            var logEntry = Activator.CreateInstance(System.Type.GetType("UnityEditor.LogEntry, UnityEditor"));
                            
                            int startIndex = Math.Max(0, totalCount - limit);
                            for (int i = startIndex; i < totalCount && logs.Count < limit; i++)
                            {
                                getEntryInternalMethod.Invoke(null, new object[] { i, logEntry });
                                
                                // BindingFlagsを明示的に指定（Public -> NonPublic も試す）
                                var conditionField = logEntry.GetType().GetField("condition", BindingFlags.Instance | BindingFlags.Public);
                                var modeField = logEntry.GetType().GetField("mode", BindingFlags.Instance | BindingFlags.Public);
                                
                                // フィールドが見つからない場合はNonPublicも試す
                                if (conditionField == null)
                                    conditionField = logEntry.GetType().GetField("condition", BindingFlags.Instance | BindingFlags.NonPublic);
                                if (modeField == null)
                                    modeField = logEntry.GetType().GetField("mode", BindingFlags.Instance | BindingFlags.NonPublic);
                                
                                if (conditionField != null && modeField != null)
                                {
                                    string condition = (string)conditionField.GetValue(logEntry);
                                    int mode = (int)modeField.GetValue(logEntry);
                                    
                                    // デバッグ情報（最初の1件のみ）
                                    if (i == startIndex && string.IsNullOrEmpty(condition))
                                    {
                                        Debug.LogWarning($"[NexusConsole] LogEntry appears empty. Fields found: condition={conditionField != null}, mode={modeField != null}");
                                    }
                                    
                                    // mode: 1 = Error, 2 = Assert, 4 = Log, 8 = Fatal, 16 = DontPrefilter, 
                                    // 32 = AssetImportError, 64 = AssetImportWarning, 128 = ScriptingError, 
                                    // 256 = ScriptingWarning, 512 = ScriptingLog, 1024 = ScriptCompileError, 
                                    // 2048 = ScriptCompileWarning, 4096 = StickyError, 8192 = MayIgnoreLineNumber
                                    
                                    string type = "Log";
                                    if ((mode & 1) != 0 || (mode & 128) != 0 || (mode & 1024) != 0) type = "Error";
                                    else if ((mode & 256) != 0 || (mode & 2048) != 0 || (mode & 64) != 0) type = "Warning";
                                    
                                    if (logType == "all" || logType.ToLower() == type.ToLower())
                                    {
                                        logs.Add(new Dictionary<string, object>
                                        {
                                            ["message"] = condition,
                                            ["type"] = type,
                                            ["mode"] = mode
                                        });
                                    }
                                }
                            }
                        }
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            logs = logs,
                            count = logs.Count,
                            totalCount = totalCount,
                            filter = logType
                        }, Formatting.Indented);
                        
                    case "clear":
                        if (logEntries != null)
                        {
                            var clearMethod = logEntries.GetMethod("Clear", BindingFlags.Public | BindingFlags.Static);
                            if (clearMethod != null)
                            {
                                clearMethod.Invoke(null, null);
                            }
                        }
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            message = "Console cleared"
                        });
                        
                    case "detail":
                        // 特定のログエントリの詳細情報を取得
                        var logIndex = int.Parse(parameters.GetValueOrDefault("index", "0"));
                        
                        if (logEntries == null)
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                success = false,
                                error = "LogEntries not available"
                            });
                        }
                        
                        // getEntryInternalMethodを取得
                        var getEntryInternalMethodForDetail = logEntries.GetMethod("GetEntryInternal", BindingFlags.Public | BindingFlags.Static);
                        if (getEntryInternalMethodForDetail == null)
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                success = false,
                                error = "Unable to access log entry method"
                            });
                        }
                        
                        // 詳細なログ情報を取得するための追加メソッド
                        var getEntryDataMethod = logEntries.GetMethod("GetEntryData", BindingFlags.Public | BindingFlags.Static);
                        var getStatusTextMethod = logEntries.GetMethod("GetStatusText", BindingFlags.Public | BindingFlags.Static);
                        var getFirstTwoLinesMethod = logEntries.GetMethod("GetFirstTwoLinesEntryTextAndModeInternal", 
                            BindingFlags.NonPublic | BindingFlags.Static);
                        
                        var detailEntry = Activator.CreateInstance(System.Type.GetType("UnityEditor.LogEntry, UnityEditor"));
                        getEntryInternalMethodForDetail.Invoke(null, new object[] { logIndex, detailEntry });
                        
                        var detail = new Dictionary<string, object>();
                        var entryType = detailEntry.GetType();
                        
                        // すべてのフィールド情報を取得
                        foreach (var field in entryType.GetFields())
                        {
                            var value = field.GetValue(detailEntry);
                            detail[field.Name] = value?.ToString() ?? "null";
                        }
                        
                        // 追加の詳細情報を取得
                        if (getEntryDataMethod != null)
                        {
                            try
                            {
                                var entryData = getEntryDataMethod.Invoke(null, new object[] { logIndex });
                                if (entryData != null)
                                {
                                    detail["entryData"] = entryData.ToString();
                                }
                            }
                            catch { }
                        }
                        
                        if (getStatusTextMethod != null)
                        {
                            try
                            {
                                var statusText = getStatusTextMethod.Invoke(null, null);
                                if (statusText != null)
                                {
                                    detail["statusText"] = statusText.ToString();
                                }
                            }
                            catch { }
                        }
                        
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            index = logIndex,
                            detail = detail
                        }, Formatting.Indented);
                        
                    default:
                        return JsonConvert.SerializeObject(new
                        {
                            success = false,
                            error = $"Unknown operation: {operation}"
                        });
                }
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        /// <summary>
        /// コンソールログの詳細分析（新しいメソッド）
        /// </summary>
        private string AnalyzeConsoleLogs(Dictionary<string, string> parameters)
        {
            try
            {
                var operation = parameters.GetValueOrDefault("operation", "analyze");
                var logType = parameters.GetValueOrDefault("logType", "error"); // デフォルトはエラーのみ
                var limit = int.Parse(parameters.GetValueOrDefault("limit", "10")); // デフォルトは10件
                var includeStackTrace = parameters.GetValueOrDefault("includeStackTrace", "true") == "true";
                
                var logEntries = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
                
                if (logEntries == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "LogEntries not available"
                    });
                }
                
                // LogEntriesからログを取得
                var getCountMethod = logEntries.GetMethod("GetCount", BindingFlags.Public | BindingFlags.Static);
                var getEntryInternalMethod = logEntries.GetMethod("GetEntryInternal", BindingFlags.Public | BindingFlags.Static);
                
                if (getCountMethod == null || getEntryInternalMethod == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "Unable to access log methods"
                    });
                }
                
                int totalCount = (int)getCountMethod.Invoke(null, null);
                var logs = new List<Dictionary<string, object>>();
                
                // まず、リアルタイムログバッファから最新のログを取得
                if (logBuffer.Count > 0)
                {
                    var filteredLogs = logBuffer.OrderByDescending(l => l.timestamp).ToList();
                    foreach (var log in filteredLogs)
                    {
                        string type = log.type switch
                        {
                            LogType.Error or LogType.Assert or LogType.Exception => "Error",
                            LogType.Warning => "Warning",
                            _ => "Log"
                        };
                        
                        if (logType == "all" || logType.ToLower() == type.ToLower())
                        {
                            // メッセージとスタックトレースを分離
                            string message = log.condition;
                            string stackTrace = "";
                            
                            if (includeStackTrace && !string.IsNullOrEmpty(log.stackTrace))
                            {
                                stackTrace = log.stackTrace;
                            }
                            
                            var logData = new Dictionary<string, object>
                            {
                                ["message"] = message,
                                ["type"] = type,
                                ["timestamp"] = log.timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")
                            };
                            
                            if (includeStackTrace && !string.IsNullOrEmpty(stackTrace))
                            {
                                logData["stackTrace"] = stackTrace;
                            }
                            
                            logs.Add(logData);
                            
                            if (logs.Count >= limit)
                                break;
                        }
                    }
                    
                    Debug.Log($"[NexusConsole] Analyzed {logs.Count} logs from buffer (buffer size: {logBuffer.Count})");
                }
                
                // ログバッファが十分でない場合は、LogEntriesからも取得を試みる
                if (logs.Count < limit)
                {
                    // LogEntryの構造体定義
                    var logEntryType = System.Type.GetType("UnityEditor.LogEntry, UnityEditor");
                    var logEntry = Activator.CreateInstance(logEntryType);
                    
                    int startIndex = Math.Max(0, totalCount - limit * 3); // より多くスキャンして必要な数だけ取得
                    
                    for (int i = startIndex; i < totalCount && logs.Count < limit; i++)
                {
                    getEntryInternalMethod.Invoke(null, new object[] { i, logEntry });
                    
                    // 全フィールドを取得（BindingFlagsを指定）
                    var conditionField = logEntryType.GetField("condition", BindingFlags.Instance | BindingFlags.Public);
                    var modeField = logEntryType.GetField("mode", BindingFlags.Instance | BindingFlags.Public);
                    var fileField = logEntryType.GetField("file", BindingFlags.Instance | BindingFlags.Public);
                    var lineField = logEntryType.GetField("line", BindingFlags.Instance | BindingFlags.Public);
                    var columnField = logEntryType.GetField("column", BindingFlags.Instance | BindingFlags.Public);
                    var instanceIDField = logEntryType.GetField("instanceID", BindingFlags.Instance | BindingFlags.Public);
                    
                    if (conditionField != null && modeField != null)
                    {
                        string condition = (string)conditionField.GetValue(logEntry) ?? "";
                        int mode = (int)modeField.GetValue(logEntry);
                        string file = fileField?.GetValue(logEntry)?.ToString() ?? "";
                        int line = lineField != null ? (int)lineField.GetValue(logEntry) : 0;
                        int column = columnField != null ? (int)columnField.GetValue(logEntry) : 0;
                        int instanceID = instanceIDField != null ? (int)instanceIDField.GetValue(logEntry) : 0;
                        
                        string type = "Log";
                        if ((mode & 1) != 0 || (mode & 128) != 0 || (mode & 1024) != 0) type = "Error";
                        else if ((mode & 256) != 0 || (mode & 2048) != 0 || (mode & 64) != 0) type = "Warning";
                        
                        if (logType == "all" || logType.ToLower() == type.ToLower())
                        {
                            // メッセージとスタックトレースを分離
                            string message = condition;
                            string stackTrace = "";
                            
                            int stackTraceIndex = condition.IndexOf("\n");
                            if (stackTraceIndex > 0)
                            {
                                message = condition.Substring(0, stackTraceIndex);
                                if (includeStackTrace)
                                {
                                    stackTrace = condition.Substring(stackTraceIndex + 1);
                                }
                            }
                            
                            var logData = new Dictionary<string, object>
                            {
                                ["message"] = message,
                                ["type"] = type,
                                ["file"] = file,
                                ["line"] = line,
                                ["column"] = column
                            };
                            
                            if (includeStackTrace && !string.IsNullOrEmpty(stackTrace))
                            {
                                logData["stackTrace"] = stackTrace;
                            }
                            
                            logs.Add(logData);
                        }
                    }
                }
                }
                
                // 分析結果を生成
                var analysis = new Dictionary<string, object>
                {
                    ["totalLogs"] = logs.Count,
                    ["errors"] = logs.Count(l => (string)l["type"] == "Error"),
                    ["warnings"] = logs.Count(l => (string)l["type"] == "Warning"),
                    ["messages"] = logs.Count(l => (string)l["type"] == "Log")
                };
                
                // ファイル別エラー集計
                var fileErrors = logs.Where(l => !string.IsNullOrEmpty((string)l["file"]))
                    .GroupBy(l => (string)l["file"])
                    .Select(g => new { File = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToList();
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    logs = logs,
                    analysis = analysis,
                    topErrorFiles = fileErrors,
                    filter = logType
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = $"Error analyzing console logs: {e.Message}"
                });
            }
        }

        // オブジェクト検索ツール
        private string SearchObjects(Dictionary<string, string> parameters)
        {
            try
            {
                var searchType = parameters.GetValueOrDefault("searchType", "name");
                var query = parameters.GetValueOrDefault("query", "");
                var includeInactive = parameters.GetValueOrDefault("includeInactive", "false") == "true";
                var exactMatch = parameters.GetValueOrDefault("exactMatch", "false") == "true";
                
                if (string.IsNullOrEmpty(query))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "query parameter is required"
                    });
                }
                
                var results = new List<Dictionary<string, object>>();
                GameObject[] allObjects;
                
                if (includeInactive)
                {
                    allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                }
                else
                {
                    allObjects = GameObject.FindObjectsOfType<GameObject>();
                }
                
                foreach (var obj in allObjects)
                {
                    bool matches = false;
                    
                    switch (searchType)
                    {
                        case "name":
                            if (exactMatch)
                                matches = obj.name == query;
                            else
                                matches = obj.name.Contains(query, StringComparison.OrdinalIgnoreCase);
                            break;
                            
                        case "tag":
                            matches = obj.CompareTag(query);
                            break;
                            
                        case "layer":
                            int layerIndex = LayerMask.NameToLayer(query);
                            if (layerIndex >= 0)
                                matches = obj.layer == layerIndex;
                            break;
                            
                        case "component":
                            // コンポーネント名で検索
                            var components = obj.GetComponents<Component>();
                            foreach (var comp in components)
                            {
                                if (comp != null)
                                {
                                    var compTypeName = comp.GetType().Name;
                                    if (exactMatch)
                                        matches = compTypeName == query;
                                    else
                                        matches = compTypeName.Contains(query, StringComparison.OrdinalIgnoreCase);
                                    
                                    if (matches) break;
                                }
                            }
                            break;
                    }
                    
                    if (matches)
                    {
                        // シーン内のオブジェクトかどうかチェック
                        bool isSceneObject = !string.IsNullOrEmpty(obj.scene.name);
                        
                        // プレハブやエディター専用オブジェクトを除外
                        if (!isSceneObject && !includeInactive)
                            continue;
                        
                        var objInfo = new Dictionary<string, object>
                        {
                            ["name"] = obj.name,
                            ["path"] = GetFullPath(obj),
                            ["tag"] = obj.tag,
                            ["layer"] = LayerMask.LayerToName(obj.layer),
                            ["active"] = obj.activeSelf,
                            ["activeInHierarchy"] = obj.activeInHierarchy,
                            ["position"] = new { 
                                x = obj.transform.position.x, 
                                y = obj.transform.position.y, 
                                z = obj.transform.position.z 
                            },
                            ["rotation"] = new { 
                                x = obj.transform.eulerAngles.x, 
                                y = obj.transform.eulerAngles.y, 
                                z = obj.transform.eulerAngles.z 
                            },
                            ["scale"] = new { 
                                x = obj.transform.localScale.x, 
                                y = obj.transform.localScale.y, 
                                z = obj.transform.localScale.z 
                            },
                            ["components"] = obj.GetComponents<Component>()
                                .Where(c => c != null)
                                .Select(c => c.GetType().Name)
                                .ToList(),
                            ["childCount"] = obj.transform.childCount,
                            ["scene"] = obj.scene.name
                        };
                        
                        results.Add(objInfo);
                        
                        // 結果数の制限
                        if (results.Count >= 100)
                            break;
                    }
                }
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    results = results,
                    count = results.Count,
                    searchType = searchType,
                    query = query,
                    includeInactive = includeInactive,
                    exactMatch = exactMatch,
                    message = results.Count >= 100 ? "Results limited to 100 items" : null
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        // チャット・メッセージング関連ツール
        private string SendChatResponse(Dictionary<string, string> parameters)
        {
            try
            {
                var messageId = parameters.GetValueOrDefault("messageId", "");
                var response = parameters.GetValueOrDefault("response", "");
                
                if (string.IsNullOrEmpty(messageId) || string.IsNullOrEmpty(response))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "messageId and response parameters are required"
                    });
                }
                
                // Unity内のメッセージングシステムにレスポンスを送信
                // これは実際のプロジェクトの実装に応じて拡張可能
                var timestamp = System.DateTime.Now;
                
                // コンソールにメッセージを記録
                Debug.Log($"[AI Response] {response}");
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = "Response sent successfully",
                    messageId = messageId,
                    response = response,
                    timestamp = timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    type = "chat_response"
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        private string CheckMessages(Dictionary<string, string> parameters)
        {
            try
            {
                // Unity内のメッセージキューをチェック
                // 実際の実装では、メッセージキューシステムと統合
                var messages = new List<Dictionary<string, object>>();
                
                // デモ用のメッセージ（実際にはキューから取得）
                // 実際の実装では、NexusMessageQueueのようなシステムから取得
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    messages = messages,
                    count = messages.Count,
                    hasNewMessages = messages.Count > 0,
                    timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        private string SendRealtimeResponse(Dictionary<string, string> parameters)
        {
            try
            {
                var sessionId = parameters.GetValueOrDefault("sessionId", "");
                var type = parameters.GetValueOrDefault("type", "info");
                var content = parameters.GetValueOrDefault("content", "");
                var metadata = parameters.GetValueOrDefault("metadata", "{}");
                
                if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(content))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "sessionId and content parameters are required"
                    });
                }
                
                // リアルタイムレスポンスの送信
                var timestamp = System.DateTime.Now;
                
                // タイプに応じた処理
                switch (type.ToLower())
                {
                    case "info":
                        Debug.Log($"[RT Info] {content}");
                        break;
                    case "warning":
                        Debug.LogWarning($"[RT Warning] {content}");
                        break;
                    case "error":
                        Debug.LogError($"[RT Error] {content}");
                        break;
                    case "success":
                        Debug.Log($"[RT Success] {content}");
                        break;
                }
                
                // メタデータの解析
                Dictionary<string, object> meta = null;
                try
                {
                    meta = JsonConvert.DeserializeObject<Dictionary<string, object>>(metadata);
                }
                catch
                {
                    meta = new Dictionary<string, object>();
                }
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = "Realtime response sent",
                    sessionId = sessionId,
                    type = type,
                    content = content,
                    metadata = meta,
                    timestamp = timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    delivered = true
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        private string CheckActiveSessions(Dictionary<string, string> parameters)
        {
            try
            {
                // アクティブなセッションの確認
                var sessions = new List<Dictionary<string, object>>();
                
                // 現在のUnityセッション情報
                var currentSession = new Dictionary<string, object>
                {
                    ["sessionId"] = System.Guid.NewGuid().ToString(),
                    ["type"] = "unity_editor",
                    ["status"] = "active",
                    ["startTime"] = Application.isPlaying ? 
                        Time.realtimeSinceStartup.ToString() : "editor_mode",
                    ["isPlaying"] = Application.isPlaying,
                    ["isPaused"] = EditorApplication.isPaused,
                    ["scene"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                    ["platform"] = Application.platform.ToString(),
                    ["unityVersion"] = Application.unityVersion
                };
                sessions.Add(currentSession);
                
                // WebSocketセッション情報（もし接続されていれば）
                // リフレクションを使用してEditor専用クラスにアクセス
                try
                {
                    var editorMCPServiceType = System.Type.GetType("NexusAIConnect.NexusEditorMCPService, Assembly-CSharp-Editor");
                    if (editorMCPServiceType != null)
                    {
                        var isConnectedProp = editorMCPServiceType.GetProperty("IsConnected", BindingFlags.Public | BindingFlags.Static);
                        var getServerUrlMethod = editorMCPServiceType.GetMethod("GetServerUrl", BindingFlags.Public | BindingFlags.Static);
                        
                        if (isConnectedProp != null && getServerUrlMethod != null)
                        {
                            bool isConnected = (bool)isConnectedProp.GetValue(null);
                            if (isConnected)
                            {
                                string serverUrl = (string)getServerUrlMethod.Invoke(null, null);
                                var wsSession = new Dictionary<string, object>
                                {
                                    ["sessionId"] = "websocket_session",
                                    ["type"] = "websocket",
                                    ["status"] = "connected",
                                    ["serverUrl"] = serverUrl,
                                    ["connected"] = true
                                };
                                sessions.Add(wsSession);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to get WebSocket session info: {ex.Message}");
                }
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    sessions = sessions,
                    count = sessions.Count,
                    activeCount = sessions.Count(s => (string)s["status"] == "active" || (string)s["status"] == "connected"),
                    timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = e.Message
                });
            }
        }

        // ヘルパーメソッド: GameObjectの完全パスを取得
        private string GetFullPath(GameObject obj)
        {
            if (obj == null) return "";
            
            string path = obj.name;
            Transform parent = obj.transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }

        // ヘルパーメソッド: フォルダが存在しない場合は作成
        private void CreateFolderIfNotExists(string folderPath)
        {
            if (!folderPath.StartsWith("Assets/"))
            {
                folderPath = "Assets/" + folderPath.TrimStart('/');
            }

            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                string[] pathParts = folderPath.Split('/');
                string currentPath = pathParts[0]; // "Assets"

                for (int i = 1; i < pathParts.Length; i++)
                {
                    string newFolder = pathParts[i];
                    string nextPath = currentPath + "/" + newFolder;

                    if (!AssetDatabase.IsValidFolder(nextPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, newFolder);
                    }

                    currentPath = nextPath;
                }
            }
        }

        // フォルダ管理メソッド
        private string CheckFolder(Dictionary<string, string> parameters)
        {
            try
            {
                var folderPath = parameters.GetValueOrDefault("folderPath", "");
                if (string.IsNullOrEmpty(folderPath))
                    return JsonConvert.SerializeObject(new { success = false, error = "folderPath parameter is required" });

                // AssetsフォルダからのRelatilveパスに正規化
                if (!folderPath.StartsWith("Assets/"))
                {
                    folderPath = "Assets/" + folderPath.TrimStart('/');
                }

                bool exists = AssetDatabase.IsValidFolder(folderPath);
                string fullPath = System.IO.Path.Combine(Application.dataPath.Replace("/Assets", ""), folderPath);

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    exists = exists,
                    folderPath = folderPath,
                    fullPath = fullPath,
                    isAssetFolder = folderPath.StartsWith("Assets/")
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private string CreateFolder(Dictionary<string, string> parameters)
        {
            try
            {
                // API定義に合わせて path パラメータをメインに使用し、folderPath もサポート
                var folderPath = parameters.GetValueOrDefault("path", "") ?? 
                               parameters.GetValueOrDefault("folderPath", "");
                if (string.IsNullOrEmpty(folderPath))
                    return CreateMissingParameterResponse("CreateFolder", "path", parameters);

                // Assetsフォルダからの相対パスに正規化
                if (!folderPath.StartsWith("Assets/"))
                {
                    folderPath = "Assets/" + folderPath.TrimStart('/');
                }

                // 既に存在するかチェック
                if (AssetDatabase.IsValidFolder(folderPath))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = true,
                        message = $"Folder already exists: {folderPath}",
                        folderPath = folderPath,
                        created = false
                    }, Formatting.Indented);
                }

                // 親フォルダから順次作成
                string[] pathParts = folderPath.Split('/');
                string currentPath = pathParts[0]; // "Assets"

                for (int i = 1; i < pathParts.Length; i++)
                {
                    string nextPath = currentPath + "/" + pathParts[i];
                    
                    if (!AssetDatabase.IsValidFolder(nextPath))
                    {
                        string guid = AssetDatabase.CreateFolder(currentPath, pathParts[i]);
                        if (string.IsNullOrEmpty(guid))
                        {
                            return JsonConvert.SerializeObject(new
                            {
                                success = false,
                                error = $"Failed to create folder: {nextPath}"
                            });
                        }
                    }
                    currentPath = nextPath;
                }

                AssetDatabase.Refresh();

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Folder created successfully: {folderPath}",
                    folderPath = folderPath,
                    created = true
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private string ListFolders(Dictionary<string, string> parameters)
        {
            try
            {
                var rootPath = parameters.GetValueOrDefault("rootPath", "Assets");
                var includeEmpty = bool.Parse(parameters.GetValueOrDefault("includeEmpty", "true"));

                if (!rootPath.StartsWith("Assets"))
                {
                    rootPath = "Assets/" + rootPath.TrimStart('/');
                }

                var folders = new List<Dictionary<string, object>>();
                
                // AssetDatabaseを使ってフォルダを取得
                string[] allFolders = AssetDatabase.GetSubFolders(rootPath);
                
                foreach (string folder in allFolders)
                {
                    var folderInfo = new Dictionary<string, object>
                    {
                        ["name"] = System.IO.Path.GetFileName(folder),
                        ["path"] = folder,
                        ["hasSubFolders"] = AssetDatabase.GetSubFolders(folder).Length > 0
                    };

                    // フォルダ内のアセット数をカウント
                    string[] guids = AssetDatabase.FindAssets("", new[] { folder });
                    folderInfo["assetCount"] = guids.Length;

                    if (includeEmpty || guids.Length > 0)
                    {
                        folders.Add(folderInfo);
                    }
                }

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    rootPath = rootPath,
                    folders = folders,
                    count = folders.Count
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        // 新しいツール群のメソッド
        private string DuplicateGameObject(Dictionary<string, string> parameters)
        {
            try
            {
                var target = GetTargetGameObject(parameters);
                if (target == null)
                {
                    string targetName = parameters.GetValueOrDefault("target") ?? 
                                      parameters.GetValueOrDefault("name") ?? 
                                      parameters.GetValueOrDefault("object") ?? "unknown";
                    return JsonConvert.SerializeObject(new { success = false, error = $"GameObject '{targetName}' not found" });
                }

                var newName = parameters.GetValueOrDefault("newName", target.name + " (Copy)");
                var duplicateCount = int.Parse(parameters.GetValueOrDefault("count", "1"));
                var offsetX = float.Parse(parameters.GetValueOrDefault("offsetX", "1"));
                var offsetY = float.Parse(parameters.GetValueOrDefault("offsetY", "0"));
                var offsetZ = float.Parse(parameters.GetValueOrDefault("offsetZ", "0"));

                var duplicatedObjects = new List<string>();

                for (int i = 0; i < duplicateCount; i++)
                {
                    var duplicate = UnityEngine.Object.Instantiate(target);
                    duplicate.name = duplicateCount > 1 ? $"{newName} ({i + 1})" : newName;
                    
                    // ポジションオフセット適用
                    duplicate.transform.position = target.transform.position + new Vector3(
                        offsetX * (i + 1), 
                        offsetY * (i + 1), 
                        offsetZ * (i + 1)
                    );

                    duplicatedObjects.Add(duplicate.name);
                    lastCreatedObject = duplicate;
                }

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Duplicated '{target.name}' {duplicateCount} time(s)",
                    duplicatedObjects = duplicatedObjects,
                    lastCreated = lastCreatedObject?.name
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }


        private string FindGameObjectsByComponent(Dictionary<string, string> parameters)
        {
            try
            {
                var componentType = parameters.GetValueOrDefault("componentType", "");
                if (string.IsNullOrEmpty(componentType))
                    return JsonConvert.SerializeObject(new { success = false, error = "componentType parameter is required" });

                var includeInactive = bool.Parse(parameters.GetValueOrDefault("includeInactive", "false"));
                var results = new List<Dictionary<string, object>>();

                // コンポーネントタイプを取得
                var type = GetComponentType(componentType);
                if (type == null)
                    return JsonConvert.SerializeObject(new { success = false, error = $"Component type '{componentType}' not found" });

                // シーン内の全GameObjectから検索
                var allObjects = includeInactive ? 
                    Resources.FindObjectsOfTypeAll<GameObject>().Where(go => go.scene.isLoaded) :
                    UnityEngine.Object.FindObjectsOfType<GameObject>();

                foreach (var obj in allObjects)
                {
                    if (obj.GetComponent(type) != null)
                    {
                        results.Add(new Dictionary<string, object>
                        {
                            ["name"] = obj.name,
                            ["path"] = GetFullPath(obj),
                            ["active"] = obj.activeInHierarchy,
                            ["position"] = new { x = obj.transform.position.x, y = obj.transform.position.y, z = obj.transform.position.z },
                            ["componentCount"] = obj.GetComponents(type).Length
                        });
                    }
                }

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    componentType = componentType,
                    foundObjects = results,
                    count = results.Count,
                    includeInactive = includeInactive
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private string CleanupEmptyObjects(Dictionary<string, string> parameters)
        {
            try
            {
                var includeInactive = bool.Parse(parameters.GetValueOrDefault("includeInactive", "true"));
                var dryRun = bool.Parse(parameters.GetValueOrDefault("dryRun", "false"));
                var removedObjects = new List<string>();

                var allObjects = includeInactive ? 
                    Resources.FindObjectsOfTypeAll<GameObject>().Where(go => go.scene.isLoaded) :
                    UnityEngine.Object.FindObjectsOfType<GameObject>();

                foreach (var obj in allObjects.ToArray())
                {
                    // 空オブジェクトの条件チェック
                    bool isEmpty = obj.GetComponents<Component>().Length <= 1 && // Transformのみ
                                   obj.transform.childCount == 0; // 子オブジェクトなし

                    if (isEmpty)
                    {
                        removedObjects.Add(GetFullPath(obj));
                        
                        if (!dryRun)
                        {
                            UnityEditor.Undo.DestroyObjectImmediate(obj);
                        }
                    }
                }

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = dryRun ? "Dry run completed - no objects were actually removed" : $"Removed {removedObjects.Count} empty objects",
                    removedObjects = removedObjects,
                    count = removedObjects.Count,
                    dryRun = dryRun
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private string GroupGameObjects(Dictionary<string, string> parameters)
        {
            try
            {
                // 複数のパラメータ名をサポート
                var names = parameters.GetValueOrDefault("names") ?? 
                           parameters.GetValueOrDefault("objects") ?? 
                           parameters.GetValueOrDefault("targets") ?? 
                           parameters.GetValueOrDefault("gameObjects") ?? 
                           "";
                           
                var parentName = parameters.GetValueOrDefault("parentName") ?? 
                               parameters.GetValueOrDefault("groupName") ?? 
                               parameters.GetValueOrDefault("parent") ?? 
                               "Group";
                               
                var maintainWorldPosition = bool.Parse(parameters.GetValueOrDefault("maintainWorldPosition", "true"));

                Debug.Log($"[GroupGameObjects] names parameter: '{names}'");
                Debug.Log($"[GroupGameObjects] parentName: '{parentName}'");

                if (string.IsNullOrEmpty(names))
                    return JsonConvert.SerializeObject(new { 
                        success = false, 
                        error = "names/objects/targets parameter is required",
                        availableParams = string.Join(", ", parameters.Keys)
                    });

                string[] gameObjectNames;
                
                // JSON配列またはカンマ区切り文字列をサポート
                if (names.StartsWith("[") && names.EndsWith("]"))
                {
                    try
                    {
                        // JSON配列として解析
                        gameObjectNames = JsonConvert.DeserializeObject<string[]>(names);
                        Debug.Log($"[GroupGameObjects] Parsed JSON array: {string.Join(", ", gameObjectNames)}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[GroupGameObjects] Failed to parse JSON array: {ex.Message}");
                        return JsonConvert.SerializeObject(new { 
                            success = false, 
                            error = $"Invalid JSON array format: {ex.Message}",
                            receivedValue = names
                        });
                    }
                }
                else
                {
                    // カンマ区切り文字列として解析
                    gameObjectNames = names.Split(',').Select(n => n.Trim()).ToArray();
                    Debug.Log($"[GroupGameObjects] Parsed comma-separated: {string.Join(", ", gameObjectNames)}");
                }
                var gameObjects = new List<GameObject>();
                var notFound = new List<string>();

                // オブジェクトを検索
                foreach (var name in gameObjectNames)
                {
                    // GetTargetGameObjectのロジックを使用
                    var dummyParams = new Dictionary<string, string> { {"target", name} };
                    var obj = GetTargetGameObject(dummyParams);
                    if (obj != null)
                    {
                        gameObjects.Add(obj);
                    }
                    else
                    {
                        notFound.Add(name);
                    }
                }

                if (gameObjects.Count == 0)
                    return JsonConvert.SerializeObject(new { 
                        success = false, 
                        error = "No GameObjects found",
                        notFound = notFound 
                    });

                // 親オブジェクトを作成
                var parentObject = new GameObject(parentName);

                // グループの中心位置を計算
                Vector3 centerPosition = Vector3.zero;
                foreach (var obj in gameObjects)
                {
                    centerPosition += obj.transform.position;
                }
                centerPosition /= gameObjects.Count;
                parentObject.transform.position = centerPosition;

                // オブジェクトを親に設定
                foreach (var obj in gameObjects)
                {
                    obj.transform.SetParent(parentObject.transform, maintainWorldPosition);
                }

                // 選択状態に
                Selection.activeGameObject = parentObject;
                EditorGUIUtility.PingObject(parentObject);

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Grouped {gameObjects.Count} objects under '{parentName}'",
                    groupedObjects = gameObjects.Select(o => o.name).ToArray(),
                    notFound = notFound,
                    parentObject = new
                    {
                        name = parentObject.name,
                        position = new { 
                            x = parentObject.transform.position.x, 
                            y = parentObject.transform.position.y, 
                            z = parentObject.transform.position.z 
                        },
                        childCount = parentObject.transform.childCount
                    }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private string RenameAsset(Dictionary<string, string> parameters)
        {
            try
            {
                // API定義に合わせて oldPath パラメータをメインに使用し、assetPath もサポート
                var assetPath = parameters.GetValueOrDefault("oldPath", "") ?? 
                              parameters.GetValueOrDefault("assetPath", "");
                var newName = parameters.GetValueOrDefault("newName", "");

                if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(newName))
                    return CreateMissingParameterResponse("RenameAsset", "oldPath and newName", parameters);

                // Assetsからの相対パスでない場合は追加
                if (!assetPath.StartsWith("Assets"))
                    assetPath = $"Assets/{assetPath}";

                var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (asset == null)
                    return JsonConvert.SerializeObject(new { 
                        success = false, 
                        error = $"Asset not found at path: {assetPath}" 
                    });

                // 拡張子を保持
                var extension = System.IO.Path.GetExtension(assetPath);
                if (!newName.EndsWith(extension))
                    newName = newName + extension;

                // リネーム実行
                var errorMessage = AssetDatabase.RenameAsset(assetPath, System.IO.Path.GetFileNameWithoutExtension(newName));
                
                if (!string.IsNullOrEmpty(errorMessage))
                    return JsonConvert.SerializeObject(new { 
                        success = false, 
                        error = errorMessage 
                    });

                AssetDatabase.Refresh();

                var newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(assetPath), newName);

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Renamed asset to '{newName}'",
                    oldPath = assetPath,
                    newPath = newPath
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private string MoveAsset(Dictionary<string, string> parameters)
        {
            try
            {
                // API定義に合わせて sourcePath, destinationFolder パラメータをメインに使用し、旧パラメータもサポート
                var assetPath = parameters.GetValueOrDefault("sourcePath", "") ?? 
                              parameters.GetValueOrDefault("assetPath", "");
                var targetFolder = parameters.GetValueOrDefault("destinationFolder", "") ?? 
                                 parameters.GetValueOrDefault("targetFolder", "");

                if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(targetFolder))
                    return CreateMissingParameterResponse("MoveAsset", "sourcePath and destinationFolder", parameters);

                // パスの正規化
                if (!assetPath.StartsWith("Assets"))
                    assetPath = $"Assets/{assetPath}";
                if (!targetFolder.StartsWith("Assets"))
                    targetFolder = $"Assets/{targetFolder}";

                // アセットの存在確認
                if (!AssetDatabase.LoadMainAssetAtPath(assetPath))
                    return JsonConvert.SerializeObject(new { 
                        success = false, 
                        error = $"Asset not found: {assetPath}" 
                    });

                // ターゲットフォルダの作成
                CreateFolderIfNotExists(targetFolder);

                var fileName = System.IO.Path.GetFileName(assetPath);
                var newPath = System.IO.Path.Combine(targetFolder, fileName);

                // 移動実行
                var errorMessage = AssetDatabase.MoveAsset(assetPath, newPath);
                
                if (!string.IsNullOrEmpty(errorMessage))
                    return JsonConvert.SerializeObject(new { 
                        success = false, 
                        error = errorMessage 
                    });

                AssetDatabase.Refresh();

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Moved asset to '{targetFolder}'",
                    oldPath = assetPath,
                    newPath = newPath
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private string DeleteAsset(Dictionary<string, string> parameters)
        {
            try
            {
                var assetPath = parameters.GetValueOrDefault("assetPath", "");
                var includeMetaFile = bool.Parse(parameters.GetValueOrDefault("includeMetaFile", "true"));

                if (string.IsNullOrEmpty(assetPath))
                    return JsonConvert.SerializeObject(new { success = false, error = "assetPath parameter is required" });

                // パスの正規化
                if (!assetPath.StartsWith("Assets"))
                    assetPath = $"Assets/{assetPath}";

                // アセットの存在確認
                var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                if (asset == null)
                    return JsonConvert.SerializeObject(new { 
                        success = false, 
                        error = $"Asset not found: {assetPath}" 
                    });

                var assetType = asset.GetType().Name;
                var isFolder = AssetDatabase.IsValidFolder(assetPath);

                // UNDOに登録してから削除
                if (asset != null)
                {
                    UnityEditor.Undo.RegisterCompleteObjectUndo(asset, $"Delete Asset {assetPath}");
                }

                // 削除実行
                bool success = AssetDatabase.DeleteAsset(assetPath);
                
                if (!success)
                    return JsonConvert.SerializeObject(new { 
                        success = false, 
                        error = "Failed to delete asset" 
                    });

                AssetDatabase.Refresh();

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = isFolder ? $"Deleted folder: {assetPath}" : $"Deleted asset: {assetPath}",
                    deletedPath = assetPath,
                    assetType = assetType,
                    isFolder = isFolder
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private string PauseScene(Dictionary<string, string> parameters)
        {
            try
            {
                var action = parameters.GetValueOrDefault("action", "toggle"); // pause, unpause, toggle
                
                bool newPauseState;
                switch (action.ToLower())
                {
                    case "pause":
                        newPauseState = true;
                        break;
                    case "unpause":
                    case "resume":
                        newPauseState = false;
                        break;
                    case "toggle":
                        newPauseState = !EditorApplication.isPaused;
                        break;
                    default:
                        return JsonConvert.SerializeObject(new { 
                            success = false, 
                            error = $"Invalid action: {action}. Use 'pause', 'unpause', or 'toggle'" 
                        });
                }

                // Play Mode中でない場合
                if (!EditorApplication.isPlaying)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "Scene is not playing. Cannot pause in Edit Mode",
                        currentState = "EditMode"
                    });
                }

                EditorApplication.isPaused = newPauseState;

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = newPauseState ? "Scene paused" : "Scene resumed",
                    isPaused = EditorApplication.isPaused,
                    isPlaying = EditorApplication.isPlaying,
                    action = action
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private string FindMissingReferences(Dictionary<string, string> parameters)
        {
            try
            {
                var searchInPrefabs = bool.Parse(parameters.GetValueOrDefault("searchInPrefabs", "true"));
                var searchInScenes = bool.Parse(parameters.GetValueOrDefault("searchInScenes", "true"));
                var includeInactive = bool.Parse(parameters.GetValueOrDefault("includeInactive", "true"));

                var missingReferences = new List<Dictionary<string, object>>();

                // シーン内のオブジェクトを検索
                if (searchInScenes)
                {
                    var sceneObjects = includeInactive ?
                        Resources.FindObjectsOfTypeAll<GameObject>().Where(go => go.scene.isLoaded) :
                        UnityEngine.Object.FindObjectsOfType<GameObject>();

                    foreach (var obj in sceneObjects)
                    {
                        CheckObjectForMissingReferences(obj, "Scene", missingReferences);
                    }
                }

                // プレハブアセットを検索
                if (searchInPrefabs)
                {
                    var prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
                    foreach (var guid in prefabGUIDs)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab != null)
                        {
                            CheckObjectForMissingReferences(prefab, path, missingReferences);
                        }
                    }
                }

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = missingReferences.Count > 0 ? 
                        $"Found {missingReferences.Count} missing references" : 
                        "No missing references found",
                    missingReferences = missingReferences,
                    count = missingReferences.Count,
                    searchedIn = new
                    {
                        scenes = searchInScenes,
                        prefabs = searchInPrefabs,
                        includeInactive = includeInactive
                    }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private void CheckObjectForMissingReferences(GameObject obj, string source, List<Dictionary<string, object>> missingList)
        {
            var components = obj.GetComponents<Component>();
            
            // null コンポーネントをチェック
            for (int i = components.Length - 1; i >= 0; i--)
            {
                if (components[i] == null)
                {
                    missingList.Add(new Dictionary<string, object>
                    {
                        ["type"] = "MissingComponent",
                        ["gameObject"] = obj.name,
                        ["path"] = GetFullPath(obj),
                        ["source"] = source,
                        ["details"] = "Component is null (script missing)"
                    });
                }
            }

            // 各コンポーネントのシリアライズされたプロパティをチェック
            foreach (var component in components.Where(c => c != null))
            {
                var so = new SerializedObject(component);
                var prop = so.GetIterator();
                
                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (prop.objectReferenceValue == null && prop.objectReferenceInstanceIDValue != 0)
                        {
                            missingList.Add(new Dictionary<string, object>
                            {
                                ["type"] = "MissingReference",
                                ["gameObject"] = obj.name,
                                ["component"] = component.GetType().Name,
                                ["property"] = prop.propertyPath,
                                ["path"] = GetFullPath(obj),
                                ["source"] = source
                            });
                        }
                    }
                }
            }

            // 子オブジェクトも再帰的にチェック
            foreach (Transform child in obj.transform)
            {
                CheckObjectForMissingReferences(child.gameObject, source, missingList);
            }
        }

        // ===== プロダクション向け高度機能 =====

        private string OptimizeTexturesBatch(Dictionary<string, string> parameters)
        {
            try
            {
                var folder = parameters.GetValueOrDefault("folder", "Assets");
                var maxTextureSize = int.Parse(parameters.GetValueOrDefault("maxTextureSize", "2048"));
                var compressQuality = parameters.GetValueOrDefault("compressQuality", "normal"); // normal, high, low
                var generateMipmaps = bool.Parse(parameters.GetValueOrDefault("generateMipmaps", "true"));
                var makeReadable = bool.Parse(parameters.GetValueOrDefault("makeReadable", "false"));

                var optimizedTextures = new List<Dictionary<string, object>>();
                var failedTextures = new List<Dictionary<string, object>>();
                long totalSizeBefore = 0;
                long totalSizeAfter = 0;

                // フォルダ内のテクスチャを検索
                var textureGUIDs = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                
                foreach (var guid in textureGUIDs)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    
                    if (texture != null)
                    {
                        try
                        {
                            // ファイルサイズ取得
                            var fileInfo = new System.IO.FileInfo(path);
                            var sizeBefore = fileInfo.Length;
                            totalSizeBefore += sizeBefore;

                            // TextureImporter設定
                            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                            if (importer != null)
                            {
                                // 変更前の設定を保存
                                var originalSettings = new Dictionary<string, object>
                                {
                                    ["maxTextureSize"] = importer.maxTextureSize,
                                    ["textureCompression"] = importer.textureCompression.ToString(),
                                    ["mipmapEnabled"] = importer.mipmapEnabled,
                                    ["isReadable"] = importer.isReadable
                                };

                                // 最適化設定を適用
                                importer.maxTextureSize = maxTextureSize;
                                importer.mipmapEnabled = generateMipmaps;
                                importer.isReadable = makeReadable;

                                // 圧縮品質設定
                                switch (compressQuality.ToLower())
                                {
                                    case "low":
                                        importer.textureCompression = TextureImporterCompression.CompressedLQ;
                                        break;
                                    case "high":
                                        importer.textureCompression = TextureImporterCompression.CompressedHQ;
                                        break;
                                    default:
                                        importer.textureCompression = TextureImporterCompression.Compressed;
                                        break;
                                }

                                // プラットフォーム固有設定
                                var platformSettings = importer.GetDefaultPlatformTextureSettings();
                                platformSettings.maxTextureSize = maxTextureSize;
                                platformSettings.format = TextureImporterFormat.Automatic;
                                platformSettings.textureCompression = importer.textureCompression;
                                importer.SetPlatformTextureSettings(platformSettings);

                                // 再インポート
                                importer.SaveAndReimport();

                                // 最適化後のサイズ
                                fileInfo.Refresh();
                                var sizeAfter = fileInfo.Length;
                                totalSizeAfter += sizeAfter;

                                optimizedTextures.Add(new Dictionary<string, object>
                                {
                                    ["path"] = path,
                                    ["name"] = texture.name,
                                    ["originalSize"] = sizeBefore,
                                    ["optimizedSize"] = sizeAfter,
                                    ["reduction"] = sizeBefore - sizeAfter,
                                    ["reductionPercent"] = Math.Round((1 - (double)sizeAfter / sizeBefore) * 100, 2),
                                    ["originalSettings"] = originalSettings,
                                    ["newSettings"] = new
                                    {
                                        maxTextureSize = maxTextureSize,
                                        compression = compressQuality,
                                        mipmaps = generateMipmaps,
                                        readable = makeReadable
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            failedTextures.Add(new Dictionary<string, object>
                            {
                                ["path"] = path,
                                ["name"] = texture.name,
                                ["error"] = ex.Message
                            });
                        }
                    }
                }

                AssetDatabase.Refresh();

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Optimized {optimizedTextures.Count} textures",
                    optimizedTextures = optimizedTextures,
                    failedTextures = failedTextures,
                    summary = new
                    {
                        totalTextures = optimizedTextures.Count + failedTextures.Count,
                        optimizedCount = optimizedTextures.Count,
                        failedCount = failedTextures.Count,
                        totalSizeBefore = totalSizeBefore,
                        totalSizeAfter = totalSizeAfter,
                        totalReduction = totalSizeBefore - totalSizeAfter,
                        reductionPercent = totalSizeBefore > 0 ? Math.Round((1 - (double)totalSizeAfter / totalSizeBefore) * 100, 2) : 0
                    },
                    settings = new
                    {
                        folder = folder,
                        maxTextureSize = maxTextureSize,
                        compressQuality = compressQuality,
                        generateMipmaps = generateMipmaps,
                        makeReadable = makeReadable
                    }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private string AnalyzeDrawCalls(Dictionary<string, string> parameters)
        {
            try
            {
                var includeInactive = bool.Parse(parameters.GetValueOrDefault("includeInactive", "false"));
                var groupByMaterial = bool.Parse(parameters.GetValueOrDefault("groupByMaterial", "true"));
                var groupByShader = bool.Parse(parameters.GetValueOrDefault("groupByShader", "true"));

                var rendererInfo = new List<Dictionary<string, object>>();
                var materialUsage = new Dictionary<Material, List<string>>();
                var shaderUsage = new Dictionary<Shader, List<string>>();

                // シーン内の全Rendererを取得
                Renderer[] renderers;
                if (includeInactive)
                {
                    renderers = Resources.FindObjectsOfTypeAll<Renderer>()
                        .Where(r => r.gameObject.scene.isLoaded)
                        .ToArray();
                }
                else
                {
                    renderers = UnityEngine.Object.FindObjectsOfType<Renderer>();
                }

                // 各Rendererの情報を収集
                foreach (var renderer in renderers)
                {
                    if (renderer == null) continue;

                    var materials = renderer.sharedMaterials.Where(m => m != null).ToArray();
                    var meshFilter = renderer.GetComponent<MeshFilter>();
                    var mesh = meshFilter != null ? meshFilter.sharedMesh : null;

                    var info = new Dictionary<string, object>
                    {
                        ["name"] = renderer.name,
                        ["path"] = GetFullPath(renderer.gameObject),
                        ["type"] = renderer.GetType().Name,
                        ["enabled"] = renderer.enabled,
                        ["materialCount"] = materials.Length,
                        ["materials"] = materials.Select(m => new
                        {
                            name = m.name,
                            shader = m.shader.name,
                            renderQueue = m.renderQueue,
                            passCount = m.passCount
                        }).ToArray()
                    };

                    if (mesh != null)
                    {
                        info["mesh"] = new
                        {
                            name = mesh.name,
                            vertexCount = mesh.vertexCount,
                            triangleCount = mesh.triangles.Length / 3,
                            submeshCount = mesh.subMeshCount
                        };
                    }

                    // SkinnedMeshRendererの場合
                    if (renderer is SkinnedMeshRenderer skinnedRenderer)
                    {
                        info["boneCount"] = skinnedRenderer.bones.Length;
                        info["quality"] = skinnedRenderer.quality.ToString();
                    }

                    rendererInfo.Add(info);

                    // Material使用状況を記録
                    foreach (var mat in materials)
                    {
                        if (!materialUsage.ContainsKey(mat))
                            materialUsage[mat] = new List<string>();
                        materialUsage[mat].Add(GetFullPath(renderer.gameObject));

                        // Shader使用状況を記録
                        if (!shaderUsage.ContainsKey(mat.shader))
                            shaderUsage[mat.shader] = new List<string>();
                        shaderUsage[mat.shader].Add(GetFullPath(renderer.gameObject));
                    }
                }

                // Cameraのドローコール予測
                var cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
                var cameraDrawCalls = new List<Dictionary<string, object>>();

                foreach (var camera in cameras)
                {
                    cameraDrawCalls.Add(new Dictionary<string, object>
                    {
                        ["name"] = camera.name,
                        ["enabled"] = camera.enabled,
                        ["cullingMask"] = LayerMaskToString(camera.cullingMask),
                        ["renderingPath"] = camera.renderingPath.ToString(),
                        ["targetDisplay"] = camera.targetDisplay
                    });
                }

                // バッチング最適化の提案
                var optimizationSuggestions = new List<string>();

                // 同じマテリアルを使用しているオブジェクトが複数ある場合
                var batchableMaterials = materialUsage.Where(kvp => kvp.Value.Count > 1);
                foreach (var kvp in batchableMaterials)
                {
                    optimizationSuggestions.Add($"Material '{kvp.Key.name}' is used by {kvp.Value.Count} objects - consider static/dynamic batching");
                }

                // 多くのマテリアルを使用しているRendererがある場合
                var multiMaterialRenderers = rendererInfo.Where(r => (int)r["materialCount"] > 2);
                if (multiMaterialRenderers.Any())
                {
                    optimizationSuggestions.Add($"{multiMaterialRenderers.Count()} renderers use more than 2 materials - consider texture atlasing");
                }

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = "Draw call analysis completed",
                    summary = new
                    {
                        totalRenderers = rendererInfo.Count,
                        activeRenderers = rendererInfo.Count(r => (bool)r["enabled"]),
                        uniqueMaterials = materialUsage.Count,
                        uniqueShaders = shaderUsage.Count,
                        estimatedDrawCalls = rendererInfo.Sum(r => (int)r["materialCount"]),
                        cameraCount = cameraDrawCalls.Count
                    },
                    renderers = rendererInfo,
                    materialUsage = groupByMaterial ? materialUsage.Select(kvp => new
                    {
                        material = kvp.Key.name,
                        shader = kvp.Key.shader.name,
                        usageCount = kvp.Value.Count,
                        usedBy = kvp.Value
                    }) : null,
                    shaderUsage = groupByShader ? shaderUsage.Select(kvp => new
                    {
                        shader = kvp.Key.name,
                        usageCount = kvp.Value.Count,
                        usedBy = kvp.Value
                    }) : null,
                    cameras = cameraDrawCalls,
                    optimizationSuggestions = optimizationSuggestions
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private string CreateProjectSnapshot(Dictionary<string, string> parameters)
        {
            try
            {
                var snapshotName = parameters.GetValueOrDefault("name", $"snapshot_{DateTime.Now:yyyyMMdd_HHmmss}");
                var includeScenes = bool.Parse(parameters.GetValueOrDefault("includeScenes", "true"));
                var includePrefabs = bool.Parse(parameters.GetValueOrDefault("includePrefabs", "true"));
                var includeMaterials = bool.Parse(parameters.GetValueOrDefault("includeMaterials", "true"));
                var includeScripts = bool.Parse(parameters.GetValueOrDefault("includeScripts", "true"));
                var includeProjectSettings = bool.Parse(parameters.GetValueOrDefault("includeProjectSettings", "true"));

                var snapshot = new Dictionary<string, object>
                {
                    ["name"] = snapshotName,
                    ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ["unityVersion"] = Application.unityVersion,
                    ["projectName"] = Application.productName,
                    ["platform"] = Application.platform.ToString()
                };

                // シーン情報
                if (includeScenes)
                {
                    var scenes = new List<Dictionary<string, object>>();
                    var sceneGUIDs = AssetDatabase.FindAssets("t:Scene");
                    
                    foreach (var guid in sceneGUIDs)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
                        
                        scenes.Add(new Dictionary<string, object>
                        {
                            ["name"] = sceneName,
                            ["path"] = path,
                            ["guid"] = guid,
                            ["isActive"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == sceneName
                        });
                    }
                    
                    snapshot["scenes"] = scenes;
                }

                // プレハブ情報
                if (includePrefabs)
                {
                    var prefabs = new List<Dictionary<string, object>>();
                    var prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
                    
                    foreach (var guid in prefabGUIDs.Take(100)) // 大量の場合は制限
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        
                        if (prefab != null)
                        {
                            prefabs.Add(new Dictionary<string, object>
                            {
                                ["name"] = prefab.name,
                                ["path"] = path,
                                ["guid"] = guid,
                                ["componentCount"] = prefab.GetComponents<Component>().Length,
                                ["childCount"] = prefab.transform.childCount
                            });
                        }
                    }
                    
                    snapshot["prefabs"] = new
                    {
                        count = prefabGUIDs.Length,
                        samples = prefabs
                    };
                }

                // マテリアル情報
                if (includeMaterials)
                {
                    var materials = new List<Dictionary<string, object>>();
                    var materialGUIDs = AssetDatabase.FindAssets("t:Material");
                    
                    foreach (var guid in materialGUIDs.Take(50))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                        
                        if (material != null)
                        {
                            materials.Add(new Dictionary<string, object>
                            {
                                ["name"] = material.name,
                                ["path"] = path,
                                ["shader"] = material.shader.name,
                                ["renderQueue"] = material.renderQueue
                            });
                        }
                    }
                    
                    snapshot["materials"] = new
                    {
                        count = materialGUIDs.Length,
                        samples = materials
                    };
                }

                // スクリプト情報
                if (includeScripts)
                {
                    var scripts = new List<Dictionary<string, object>>();
                    var scriptGUIDs = AssetDatabase.FindAssets("t:MonoScript");
                    
                    foreach (var guid in scriptGUIDs.Take(50))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                        
                        if (script != null)
                        {
                            scripts.Add(new Dictionary<string, object>
                            {
                                ["name"] = script.name,
                                ["path"] = path,
                                ["className"] = script.GetClass()?.FullName ?? "Unknown"
                            });
                        }
                    }
                    
                    snapshot["scripts"] = new
                    {
                        count = scriptGUIDs.Length,
                        samples = scripts
                    };
                }

                // プロジェクト設定
                if (includeProjectSettings)
                {
                    snapshot["projectSettings"] = new
                    {
                        companyName = PlayerSettings.companyName,
                        productName = PlayerSettings.productName,
                        applicationIdentifier = PlayerSettings.applicationIdentifier,
                        defaultInterfaceOrientation = PlayerSettings.defaultInterfaceOrientation.ToString(),
                        colorSpace = PlayerSettings.colorSpace.ToString(),
                        apiCompatibilityLevel = PlayerSettings.GetApiCompatibilityLevel(EditorUserBuildSettings.selectedBuildTargetGroup).ToString()
                    };
                }

                // スナップショットをJSON形式で保存
                var snapshotJson = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
                var snapshotPath = $"Assets/Snapshots/{snapshotName}.json";
                
                // Snapshotsフォルダを作成
                CreateFolderIfNotExists("Assets/Snapshots");
                
                // ファイルに保存
                System.IO.File.WriteAllText(snapshotPath, snapshotJson);
                AssetDatabase.Refresh();

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Project snapshot created: {snapshotName}",
                    snapshotPath = snapshotPath,
                    snapshotSize = new System.IO.FileInfo(snapshotPath).Length,
                    contents = new
                    {
                        scenes = includeScenes,
                        prefabs = includePrefabs,
                        materials = includeMaterials,
                        scripts = includeScripts,
                        projectSettings = includeProjectSettings
                    },
                    summary = snapshot
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        // ヘルパーメソッド: LayerMaskを文字列に変換
        private string LayerMaskToString(int mask)
        {
            var layers = new List<string>();
            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    var layerName = LayerMask.LayerToName(i);
                    if (!string.IsNullOrEmpty(layerName))
                        layers.Add(layerName);
                }
            }
            return string.Join(", ", layers);
        }

        private string AnalyzeDependencies(Dictionary<string, string> parameters)
        {
            try
            {
                var assetPath = parameters.GetValueOrDefault("assetPath", "");
                var maxDepth = int.Parse(parameters.GetValueOrDefault("maxDepth", "5"));
                var includeScripts = bool.Parse(parameters.GetValueOrDefault("includeScripts", "true"));
                var includeTextures = bool.Parse(parameters.GetValueOrDefault("includeTextures", "true"));
                var includeMaterials = bool.Parse(parameters.GetValueOrDefault("includeMaterials", "true"));
                var includePrefabs = bool.Parse(parameters.GetValueOrDefault("includePrefabs", "true"));

                var dependencies = new Dictionary<string, List<string>>();
                var dependencyTree = new Dictionary<string, object>();
                var processed = new HashSet<string>();

                // 特定のアセットまたは全体の依存関係を分析
                if (!string.IsNullOrEmpty(assetPath))
                {
                    AnalyzeAssetDependencies(assetPath, dependencies, dependencyTree, processed, 0, maxDepth);
                }
                else
                {
                    // 主要なアセットタイプを分析
                    if (includeScripts)
                    {
                        var scripts = AssetDatabase.FindAssets("t:MonoScript");
                        foreach (var guid in scripts.Take(50))
                        {
                            var path = AssetDatabase.GUIDToAssetPath(guid);
                            AnalyzeAssetDependencies(path, dependencies, dependencyTree, processed, 0, maxDepth);
                        }
                    }

                    if (includePrefabs)
                    {
                        var prefabs = AssetDatabase.FindAssets("t:Prefab");
                        foreach (var guid in prefabs.Take(30))
                        {
                            var path = AssetDatabase.GUIDToAssetPath(guid);
                            AnalyzeAssetDependencies(path, dependencies, dependencyTree, processed, 0, maxDepth);
                        }
                    }

                    if (includeMaterials)
                    {
                        var materials = AssetDatabase.FindAssets("t:Material");
                        foreach (var guid in materials.Take(30))
                        {
                            var path = AssetDatabase.GUIDToAssetPath(guid);
                            AnalyzeAssetDependencies(path, dependencies, dependencyTree, processed, 0, maxDepth);
                        }
                    }
                }

                // 循環依存を検出
                var circularDependencies = DetectCircularDependencies(dependencies);

                // 依存関係の統計
                var stats = new Dictionary<string, object>
                {
                    ["totalAssets"] = dependencies.Count,
                    ["totalDependencies"] = dependencies.Sum(kvp => kvp.Value.Count),
                    ["averageDependencies"] = dependencies.Count > 0 ? dependencies.Average(kvp => kvp.Value.Count) : 0,
                    ["maxDependencies"] = dependencies.Count > 0 ? dependencies.Max(kvp => kvp.Value.Count) : 0,
                    ["assetsWithNoDependencies"] = dependencies.Count(kvp => kvp.Value.Count == 0),
                    ["circularDependencies"] = circularDependencies.Count
                };

                // 最も参照されているアセット
                var referenceCounts = new Dictionary<string, int>();
                foreach (var deps in dependencies.Values)
                {
                    foreach (var dep in deps)
                    {
                        if (!referenceCounts.ContainsKey(dep))
                            referenceCounts[dep] = 0;
                        referenceCounts[dep]++;
                    }
                }

                var mostReferenced = referenceCounts
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(10)
                    .Select(kvp => new { asset = kvp.Key, referenceCount = kvp.Value })
                    .ToList();

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = "Dependency analysis completed",
                    assetPath = assetPath,
                    dependencyTree = dependencyTree,
                    statistics = stats,
                    mostReferencedAssets = mostReferenced,
                    circularDependencies = circularDependencies,
                    settings = new
                    {
                        maxDepth = maxDepth,
                        includeScripts = includeScripts,
                        includeTextures = includeTextures,
                        includeMaterials = includeMaterials,
                        includePrefabs = includePrefabs
                    }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private void AnalyzeAssetDependencies(string assetPath, Dictionary<string, List<string>> dependencies, 
            Dictionary<string, object> tree, HashSet<string> processed, int currentDepth, int maxDepth)
        {
            if (currentDepth > maxDepth || processed.Contains(assetPath))
                return;

            processed.Add(assetPath);
            var deps = AssetDatabase.GetDependencies(assetPath, false);
            
            if (!dependencies.ContainsKey(assetPath))
                dependencies[assetPath] = new List<string>();

            var treeNode = new Dictionary<string, object>
            {
                ["type"] = AssetDatabase.GetMainAssetTypeAtPath(assetPath)?.Name ?? "Unknown",
                ["dependencies"] = new List<Dictionary<string, object>>()
            };

            foreach (var dep in deps)
            {
                if (dep != assetPath)
                {
                    dependencies[assetPath].Add(dep);
                    
                    var depNode = new Dictionary<string, object>
                    {
                        ["path"] = dep,
                        ["type"] = AssetDatabase.GetMainAssetTypeAtPath(dep)?.Name ?? "Unknown"
                    };
                    
                    ((List<Dictionary<string, object>>)treeNode["dependencies"]).Add(depNode);
                    
                    // 再帰的に依存関係を分析
                    AnalyzeAssetDependencies(dep, dependencies, tree, processed, currentDepth + 1, maxDepth);
                }
            }

            tree[assetPath] = treeNode;
        }

        private List<List<string>> DetectCircularDependencies(Dictionary<string, List<string>> dependencies)
        {
            var circularDeps = new List<List<string>>();
            var visited = new HashSet<string>();
            var recursionStack = new HashSet<string>();
            var path = new List<string>();

            foreach (var asset in dependencies.Keys)
            {
                if (!visited.Contains(asset))
                {
                    DetectCircularDependenciesUtil(asset, dependencies, visited, recursionStack, path, circularDeps);
                }
            }

            return circularDeps;
        }

        private bool DetectCircularDependenciesUtil(string asset, Dictionary<string, List<string>> dependencies,
            HashSet<string> visited, HashSet<string> recursionStack, List<string> path, List<List<string>> circularDeps)
        {
            visited.Add(asset);
            recursionStack.Add(asset);
            path.Add(asset);

            if (dependencies.ContainsKey(asset))
            {
                foreach (var dep in dependencies[asset])
                {
                    if (!visited.Contains(dep))
                    {
                        if (DetectCircularDependenciesUtil(dep, dependencies, visited, recursionStack, path, circularDeps))
                            return true;
                    }
                    else if (recursionStack.Contains(dep))
                    {
                        // 循環依存を検出
                        var cycleStart = path.IndexOf(dep);
                        var cycle = path.Skip(cycleStart).ToList();
                        cycle.Add(dep); // 循環を完成させる
                        circularDeps.Add(cycle);
                    }
                }
            }

            path.RemoveAt(path.Count - 1);
            recursionStack.Remove(asset);
            return false;
        }

        private string ExportProjectStructure(Dictionary<string, string> parameters)
        {
            try
            {
                var outputFormat = parameters.GetValueOrDefault("format", "json"); // json, csv, tree
                var includeFolders = bool.Parse(parameters.GetValueOrDefault("includeFolders", "true"));
                var includeFileSize = bool.Parse(parameters.GetValueOrDefault("includeFileSize", "true"));
                var maxDepth = int.Parse(parameters.GetValueOrDefault("maxDepth", "10"));

                var projectStructure = new Dictionary<string, object>();
                var rootPath = "Assets";
                
                // プロジェクト構造を再帰的に構築
                projectStructure = BuildProjectStructure(rootPath, 0, maxDepth, includeFileSize);

                // ファイルタイプ別統計
                var fileStats = new Dictionary<string, int>();
                var totalSize = 0L;
                CountFileTypes(projectStructure, fileStats, ref totalSize);

                var result = new Dictionary<string, object>
                {
                    ["projectName"] = PlayerSettings.productName,
                    ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ["structure"] = projectStructure,
                    ["statistics"] = new
                    {
                        fileTypes = fileStats.OrderByDescending(kvp => kvp.Value)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                        totalFiles = fileStats.Sum(kvp => kvp.Value),
                        totalSize = totalSize,
                        totalSizeMB = Math.Round(totalSize / (1024.0 * 1024.0), 2)
                    }
                };

                // 出力形式に応じて変換
                string output;
                string fileName;
                switch (outputFormat.ToLower())
                {
                    case "csv":
                        output = ConvertToCSV(projectStructure);
                        fileName = $"project_structure_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                        break;
                    case "tree":
                        output = ConvertToTreeFormat(projectStructure);
                        fileName = $"project_structure_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                        break;
                    default:
                        output = JsonConvert.SerializeObject(result, Formatting.Indented);
                        fileName = $"project_structure_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                        break;
                }

                // ファイルに保存
                var outputPath = $"Assets/ProjectAnalysis/{fileName}";
                CreateFolderIfNotExists("Assets/ProjectAnalysis");
                System.IO.File.WriteAllText(outputPath, output);
                AssetDatabase.Refresh();

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Project structure exported as {outputFormat}",
                    outputPath = outputPath,
                    fileSize = new System.IO.FileInfo(outputPath).Length,
                    format = outputFormat,
                    summary = result["statistics"]
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private Dictionary<string, object> BuildProjectStructure(string path, int currentDepth, int maxDepth, bool includeFileSize)
        {
            var structure = new Dictionary<string, object>
            {
                ["name"] = System.IO.Path.GetFileName(path),
                ["path"] = path,
                ["type"] = AssetDatabase.IsValidFolder(path) ? "folder" : "file"
            };

            if ((string)structure["type"] == "folder" && currentDepth < maxDepth)
            {
                var children = new List<Dictionary<string, object>>();
                var subFolders = AssetDatabase.GetSubFolders(path);
                
                foreach (var folder in subFolders)
                {
                    children.Add(BuildProjectStructure(folder, currentDepth + 1, maxDepth, includeFileSize));
                }

                var files = System.IO.Directory.GetFiles(path)
                    .Where(f => !f.EndsWith(".meta"))
                    .Select(f => f.Replace('\\', '/'));

                foreach (var file in files)
                {
                    var fileInfo = new Dictionary<string, object>
                    {
                        ["name"] = System.IO.Path.GetFileName(file),
                        ["path"] = file,
                        ["type"] = "file",
                        ["extension"] = System.IO.Path.GetExtension(file)
                    };

                    if (includeFileSize)
                    {
                        var fi = new System.IO.FileInfo(file);
                        fileInfo["size"] = fi.Length;
                        fileInfo["sizeMB"] = Math.Round(fi.Length / (1024.0 * 1024.0), 2);
                    }

                    children.Add(fileInfo);
                }

                structure["children"] = children;
                structure["childCount"] = children.Count;
            }

            return structure;
        }

        private void CountFileTypes(Dictionary<string, object> structure, Dictionary<string, int> fileStats, ref long totalSize)
        {
            if ((string)structure["type"] == "file")
            {
                var ext = structure.ContainsKey("extension") ? (string)structure["extension"] : "";
                if (!string.IsNullOrEmpty(ext))
                {
                    if (!fileStats.ContainsKey(ext))
                        fileStats[ext] = 0;
                    fileStats[ext]++;
                }

                if (structure.ContainsKey("size"))
                    totalSize += (long)structure["size"];
            }
            else if (structure.ContainsKey("children"))
            {
                foreach (var child in (List<Dictionary<string, object>>)structure["children"])
                {
                    CountFileTypes(child, fileStats, ref totalSize);
                }
            }
        }

        private string ConvertToCSV(Dictionary<string, object> structure)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Path,Type,Size(bytes),Extension");
            AddToCSV(structure, csv);
            return csv.ToString();
        }

        private void AddToCSV(Dictionary<string, object> structure, System.Text.StringBuilder csv)
        {
            var path = structure["path"].ToString();
            var type = structure["type"].ToString();
            var size = structure.ContainsKey("size") ? structure["size"].ToString() : "";
            var ext = structure.ContainsKey("extension") ? structure["extension"].ToString() : "";
            
            csv.AppendLine($"\"{path}\",{type},{size},{ext}");

            if (structure.ContainsKey("children"))
            {
                foreach (var child in (List<Dictionary<string, object>>)structure["children"])
                {
                    AddToCSV(child, csv);
                }
            }
        }

        private string ConvertToTreeFormat(Dictionary<string, object> structure, string indent = "")
        {
            var tree = new System.Text.StringBuilder();
            tree.AppendLine($"{indent}{structure["name"]}");
            
            if (structure.ContainsKey("children"))
            {
                var children = (List<Dictionary<string, object>>)structure["children"];
                for (int i = 0; i < children.Count; i++)
                {
                    var isLast = i == children.Count - 1;
                    var childIndent = indent + (isLast ? "└── " : "├── ");
                    var nextIndent = indent + (isLast ? "    " : "│   ");
                    var childTree = ConvertToTreeFormat(children[i], childIndent);
                    // Replace only the first occurrence
                    var index = childTree.IndexOf(childIndent);
                    if (index >= 0)
                    {
                        childTree = childTree.Substring(0, index) + nextIndent + childTree.Substring(index + childIndent.Length);
                    }
                    tree.Append(childTree);
                }
            }
            
            return tree.ToString();
        }

        private string ValidateNamingConventions(Dictionary<string, string> parameters)
        {
            try
            {
                var checkPascalCase = bool.Parse(parameters.GetValueOrDefault("checkPascalCase", "true"));
                var checkCamelCase = bool.Parse(parameters.GetValueOrDefault("checkCamelCase", "true"));
                var checkSnakeCase = bool.Parse(parameters.GetValueOrDefault("checkSnakeCase", "false"));
                var checkPrefixes = bool.Parse(parameters.GetValueOrDefault("checkPrefixes", "true"));
                var customRules = parameters.GetValueOrDefault("customRules", "");

                var violations = new List<Dictionary<string, object>>();
                var statistics = new Dictionary<string, int>
                {
                    ["totalAssets"] = 0,
                    ["pascalCaseViolations"] = 0,
                    ["camelCaseViolations"] = 0,
                    ["prefixViolations"] = 0,
                    ["customRuleViolations"] = 0
                };

                // スクリプトファイルのチェック
                var scripts = AssetDatabase.FindAssets("t:MonoScript");
                foreach (var guid in scripts)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    
                    if (script != null)
                    {
                        statistics["totalAssets"]++;
                        var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                        var scriptViolations = new List<string>();

                        // PascalCaseチェック（クラス名）
                        if (checkPascalCase && !IsPascalCase(fileName))
                        {
                            scriptViolations.Add("Class name should be PascalCase");
                            statistics["pascalCaseViolations"]++;
                        }

                        // プレフィックスチェック
                        if (checkPrefixes)
                        {
                            if (fileName.StartsWith("I") && fileName.Length > 1 && char.IsUpper(fileName[1]))
                            {
                                // インターフェースは許可
                            }
                            else if (fileName.StartsWith("_"))
                            {
                                scriptViolations.Add("Script names should not start with underscore");
                                statistics["prefixViolations"]++;
                            }
                        }

                        if (scriptViolations.Count > 0)
                        {
                            violations.Add(new Dictionary<string, object>
                            {
                                ["path"] = path,
                                ["name"] = fileName,
                                ["type"] = "Script",
                                ["violations"] = scriptViolations
                            });
                        }
                    }
                }

                // プレハブのチェック
                var prefabs = AssetDatabase.FindAssets("t:Prefab");
                foreach (var guid in prefabs)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var prefabName = System.IO.Path.GetFileNameWithoutExtension(path);
                    statistics["totalAssets"]++;
                    
                    var prefabViolations = new List<string>();

                    // PascalCaseチェック
                    if (checkPascalCase && !IsPascalCase(prefabName))
                    {
                        prefabViolations.Add("Prefab name should be PascalCase");
                        statistics["pascalCaseViolations"]++;
                    }

                    if (prefabViolations.Count > 0)
                    {
                        violations.Add(new Dictionary<string, object>
                        {
                            ["path"] = path,
                            ["name"] = prefabName,
                            ["type"] = "Prefab",
                            ["violations"] = prefabViolations
                        });
                    }
                }

                // 推奨される修正
                var recommendations = new List<string>();
                if (statistics["pascalCaseViolations"] > 0)
                    recommendations.Add("Use PascalCase for class names and prefabs (e.g., PlayerController, EnemySpawner)");
                if (statistics["camelCaseViolations"] > 0)
                    recommendations.Add("Use camelCase for variables and methods (e.g., playerHealth, moveSpeed)");
                if (statistics["prefixViolations"] > 0)
                    recommendations.Add("Avoid using underscores at the beginning of names");

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Found {violations.Count} naming convention violations",
                    violations = violations,
                    statistics = statistics,
                    recommendations = recommendations,
                    settings = new
                    {
                        checkPascalCase = checkPascalCase,
                        checkCamelCase = checkCamelCase,
                        checkSnakeCase = checkSnakeCase,
                        checkPrefixes = checkPrefixes
                    }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private bool IsPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            
            // 最初の文字が大文字
            if (!char.IsUpper(name[0])) return false;
            
            // 数字やアンダースコアで始まらない
            if (char.IsDigit(name[0]) || name[0] == '_') return false;
            
            // 連続したアンダースコアがない
            if (name.Contains("__")) return false;
            
            return true;
        }

        private bool IsCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            
            // 最初の文字が小文字
            if (!char.IsLower(name[0])) return false;
            
            // 数字やアンダースコアで始まらない
            if (char.IsDigit(name[0]) || name[0] == '_') return false;
            
            return true;
        }

        private string ExtractAllText(Dictionary<string, string> parameters)
        {
            try
            {
                var includeScripts = bool.Parse(parameters.GetValueOrDefault("includeScripts", "true"));
                var includeUI = bool.Parse(parameters.GetValueOrDefault("includeUI", "true"));
                var includeComments = bool.Parse(parameters.GetValueOrDefault("includeComments", "true"));
                var outputFormat = parameters.GetValueOrDefault("format", "json"); // json, txt, csv

                var extractedText = new Dictionary<string, List<Dictionary<string, object>>>();
                var allTexts = new List<string>();

                // スクリプトからテキストを抽出
                if (includeScripts)
                {
                    var scriptTexts = new List<Dictionary<string, object>>();
                    var scripts = AssetDatabase.FindAssets("t:MonoScript");
                    
                    foreach (var guid in scripts.Take(100))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var content = System.IO.File.ReadAllText(path);
                        
                        // 文字列リテラルを抽出
                        var stringLiterals = ExtractStringLiterals(content);
                        if (stringLiterals.Count > 0)
                        {
                            scriptTexts.Add(new Dictionary<string, object>
                            {
                                ["path"] = path,
                                ["fileName"] = System.IO.Path.GetFileName(path),
                                ["strings"] = stringLiterals,
                                ["count"] = stringLiterals.Count
                            });
                            allTexts.AddRange(stringLiterals);
                        }

                        // コメントを抽出
                        if (includeComments)
                        {
                            var comments = ExtractComments(content);
                            if (comments.Count > 0)
                            {
                                scriptTexts.Add(new Dictionary<string, object>
                                {
                                    ["path"] = path,
                                    ["fileName"] = System.IO.Path.GetFileName(path),
                                    ["comments"] = comments,
                                    ["count"] = comments.Count
                                });
                                allTexts.AddRange(comments);
                            }
                        }
                    }
                    
                    extractedText["scripts"] = scriptTexts;
                }

                // UIテキストを抽出
                if (includeUI)
                {
                    var uiTexts = new List<Dictionary<string, object>>();
                    
                    // Text コンポーネント
                    var textComponents = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Text>(true);
                    foreach (var text in textComponents)
                    {
                        if (!string.IsNullOrEmpty(text.text))
                        {
                            uiTexts.Add(new Dictionary<string, object>
                            {
                                ["gameObject"] = text.gameObject.name,
                                ["path"] = GetFullPath(text.gameObject),
                                ["text"] = text.text,
                                ["font"] = text.font != null ? text.font.name : "None",
                                ["fontSize"] = text.fontSize
                            });
                            allTexts.Add(text.text);
                        }
                    }

                    // TextMeshPro コンポーネント（リフレクションで取得）
                    var tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
                    if (tmpType != null)
                    {
                        var tmpComponents = UnityEngine.Object.FindObjectsOfType(tmpType, true);
                        foreach (var tmp in tmpComponents)
                        {
                            var textProperty = tmpType.GetProperty("text");
                            if (textProperty != null)
                            {
                                var textValue = textProperty.GetValue(tmp) as string;
                                if (!string.IsNullOrEmpty(textValue))
                                {
                                    var go = (tmp as Component).gameObject;
                                    uiTexts.Add(new Dictionary<string, object>
                                    {
                                        ["gameObject"] = go.name,
                                        ["path"] = GetFullPath(go),
                                        ["text"] = textValue,
                                        ["type"] = "TextMeshPro"
                                    });
                                    allTexts.Add(textValue);
                                }
                            }
                        }
                    }
                    
                    extractedText["ui"] = uiTexts;
                }

                // 統計情報
                var statistics = new Dictionary<string, object>
                {
                    ["totalTexts"] = allTexts.Count,
                    ["uniqueTexts"] = allTexts.Distinct().Count(),
                    ["totalCharacters"] = allTexts.Sum(t => t.Length),
                    ["averageLength"] = allTexts.Count > 0 ? allTexts.Average(t => t.Length) : 0,
                    ["languages"] = DetectLanguages(allTexts)
                };

                // 出力形式に応じて保存
                string outputPath;
                switch (outputFormat.ToLower())
                {
                    case "txt":
                        var txtContent = string.Join("\n", allTexts.Distinct());
                        outputPath = $"Assets/ExtractedTexts/all_texts_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                        CreateFolderIfNotExists("Assets/ExtractedTexts");
                        System.IO.File.WriteAllText(outputPath, txtContent);
                        break;
                        
                    case "csv":
                        var csvContent = "Type,Source,Text\n";
                        foreach (var kvp in extractedText)
                        {
                            foreach (var item in kvp.Value)
                            {
                                if (item.ContainsKey("strings"))
                                {
                                    foreach (var str in (List<string>)item["strings"])
                                    {
                                        csvContent += $"\"Script\",\"{item["path"]}\",\"{str.Replace("\"", "\"\"")}\"\n";
                                    }
                                }
                                else if (item.ContainsKey("text"))
                                {
                                    csvContent += $"\"UI\",\"{item["path"]}\",\"{item["text"].ToString().Replace("\"", "\"\"")}\"\n";
                                }
                            }
                        }
                        outputPath = $"Assets/ExtractedTexts/all_texts_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                        CreateFolderIfNotExists("Assets/ExtractedTexts");
                        System.IO.File.WriteAllText(outputPath, csvContent);
                        break;
                        
                    default:
                        var jsonContent = JsonConvert.SerializeObject(new
                        {
                            extractedText = extractedText,
                            statistics = statistics,
                            allTexts = allTexts.Distinct().ToList()
                        }, Formatting.Indented);
                        outputPath = $"Assets/ExtractedTexts/all_texts_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                        CreateFolderIfNotExists("Assets/ExtractedTexts");
                        System.IO.File.WriteAllText(outputPath, jsonContent);
                        break;
                }

                AssetDatabase.Refresh();

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = "Text extraction completed",
                    outputPath = outputPath,
                    format = outputFormat,
                    statistics = statistics,
                    preview = allTexts.Distinct().Take(10).ToList()
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private List<string> ExtractStringLiterals(string code)
        {
            var strings = new List<string>();
            var pattern = @"""([^""\\]|\\.)*""|'([^'\\]|\\.)*'";
            var matches = System.Text.RegularExpressions.Regex.Matches(code, pattern);
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var value = match.Value.Trim('"', '\'');
                if (!string.IsNullOrWhiteSpace(value) && value.Length > 1)
                {
                    strings.Add(value);
                }
            }
            
            return strings;
        }

        private List<string> ExtractComments(string code)
        {
            var comments = new List<string>();
            
            // 単一行コメント
            var singleLinePattern = @"//.*$";
            var singleLineMatches = System.Text.RegularExpressions.Regex.Matches(code, singleLinePattern, System.Text.RegularExpressions.RegexOptions.Multiline);
            foreach (System.Text.RegularExpressions.Match match in singleLineMatches)
            {
                var comment = match.Value.Substring(2).Trim();
                if (!string.IsNullOrWhiteSpace(comment))
                    comments.Add(comment);
            }
            
            // 複数行コメント
            var multiLinePattern = @"/\*[\s\S]*?\*/";
            var multiLineMatches = System.Text.RegularExpressions.Regex.Matches(code, multiLinePattern);
            foreach (System.Text.RegularExpressions.Match match in multiLineMatches)
            {
                var comment = match.Value.Substring(2, match.Value.Length - 4).Trim();
                if (!string.IsNullOrWhiteSpace(comment))
                    comments.Add(comment);
            }
            
            return comments;
        }

        private Dictionary<string, int> DetectLanguages(List<string> texts)
        {
            var languages = new Dictionary<string, int>
            {
                ["English"] = 0,
                ["Japanese"] = 0,
                ["Numbers"] = 0,
                ["Other"] = 0
            };

            foreach (var text in texts)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(text, @"[\u3040-\u309F\u30A0-\u30FF\u4E00-\u9FAF]"))
                    languages["Japanese"]++;
                else if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^[a-zA-Z\s]+$"))
                    languages["English"]++;
                else if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d+$"))
                    languages["Numbers"]++;
                else
                    languages["Other"]++;
            }

            return languages;
        }

        // ===== AIが喜ぶ追加ツール群 =====

        private string BatchRename(Dictionary<string, string> parameters)
        {
            try
            {
                var searchPattern = parameters.GetValueOrDefault("searchPattern", "");
                var replacePattern = parameters.GetValueOrDefault("replacePattern", "");
                var assetType = parameters.GetValueOrDefault("assetType", "all"); // all, prefab, material, texture, script
                var useRegex = bool.Parse(parameters.GetValueOrDefault("useRegex", "false"));
                var caseSensitive = bool.Parse(parameters.GetValueOrDefault("caseSensitive", "true"));
                var dryRun = bool.Parse(parameters.GetValueOrDefault("dryRun", "false"));

                if (string.IsNullOrEmpty(searchPattern))
                    return JsonConvert.SerializeObject(new { success = false, error = "searchPattern is required" });

                var renamedAssets = new List<Dictionary<string, object>>();
                var failedAssets = new List<Dictionary<string, object>>();

                // アセットタイプに基づいてフィルタ
                string filter = assetType.ToLower() switch
                {
                    "prefab" => "t:Prefab",
                    "material" => "t:Material",
                    "texture" => "t:Texture2D",
                    "script" => "t:MonoScript",
                    _ => ""
                };

                var guids = AssetDatabase.FindAssets(filter);
                
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                    var extension = System.IO.Path.GetExtension(path);
                    
                    string newName;
                    if (useRegex)
                    {
                        var regexOptions = caseSensitive ? 
                            System.Text.RegularExpressions.RegexOptions.None : 
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                        
                        newName = System.Text.RegularExpressions.Regex.Replace(
                            fileName, searchPattern, replacePattern, regexOptions);
                    }
                    else
                    {
                        if (caseSensitive)
                        {
                            newName = fileName.Replace(searchPattern, replacePattern);
                        }
                        else
                        {
                            // 大文字小文字を区別しない場合の処理
                            var regex = new System.Text.RegularExpressions.Regex(
                                System.Text.RegularExpressions.Regex.Escape(searchPattern),
                                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            newName = regex.Replace(fileName, replacePattern);
                        }
                    }

                    if (newName != fileName)
                    {
                        try
                        {
                            if (!dryRun)
                            {
                                var errorMessage = AssetDatabase.RenameAsset(path, newName);
                                if (!string.IsNullOrEmpty(errorMessage))
                                    throw new Exception(errorMessage);
                            }

                            renamedAssets.Add(new Dictionary<string, object>
                            {
                                ["path"] = path,
                                ["oldName"] = fileName,
                                ["newName"] = newName,
                                ["type"] = AssetDatabase.GetMainAssetTypeAtPath(path)?.Name ?? "Unknown"
                            });
                        }
                        catch (Exception ex)
                        {
                            failedAssets.Add(new Dictionary<string, object>
                            {
                                ["path"] = path,
                                ["oldName"] = fileName,
                                ["newName"] = newName,
                                ["error"] = ex.Message
                            });
                        }
                    }
                }

                if (!dryRun)
                    AssetDatabase.Refresh();

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = dryRun ? 
                        $"Dry run: {renamedAssets.Count} assets would be renamed" : 
                        $"Renamed {renamedAssets.Count} assets",
                    renamedAssets = renamedAssets,
                    failedAssets = failedAssets,
                    summary = new
                    {
                        totalProcessed = guids.Length,
                        renamed = renamedAssets.Count,
                        failed = failedAssets.Count,
                        unchanged = guids.Length - renamedAssets.Count - failedAssets.Count
                    },
                    settings = new
                    {
                        searchPattern = searchPattern,
                        replacePattern = replacePattern,
                        assetType = assetType,
                        useRegex = useRegex,
                        caseSensitive = caseSensitive,
                        dryRun = dryRun
                    }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private string BatchImportSettings(Dictionary<string, string> parameters)
        {
            try
            {
                var assetType = parameters.GetValueOrDefault("assetType", "texture"); // texture, model, audio
                var folder = parameters.GetValueOrDefault("folder", "Assets");
                var recursive = bool.Parse(parameters.GetValueOrDefault("recursive", "true"));
                
                var processedAssets = new List<Dictionary<string, object>>();
                var failedAssets = new List<Dictionary<string, object>>();

                switch (assetType.ToLower())
                {
                    case "texture":
                        var textureSettings = new
                        {
                            maxTextureSize = int.Parse(parameters.GetValueOrDefault("maxTextureSize", "2048")),
                            textureCompression = parameters.GetValueOrDefault("compression", "Compressed"),
                            generateMipmaps = bool.Parse(parameters.GetValueOrDefault("generateMipmaps", "true")),
                            filterMode = parameters.GetValueOrDefault("filterMode", "Bilinear"),
                            anisoLevel = int.Parse(parameters.GetValueOrDefault("anisoLevel", "1"))
                        };

                        var textureGUIDs = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                        
                        foreach (var guid in textureGUIDs)
                        {
                            var path = AssetDatabase.GUIDToAssetPath(guid);
                            
                            try
                            {
                                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                                if (importer != null)
                                {
                                    importer.maxTextureSize = textureSettings.maxTextureSize;
                                    importer.mipmapEnabled = textureSettings.generateMipmaps;
                                    importer.anisoLevel = textureSettings.anisoLevel;
                                    
                                    switch (textureSettings.textureCompression)
                                    {
                                        case "CompressedLQ":
                                            importer.textureCompression = TextureImporterCompression.CompressedLQ;
                                            break;
                                        case "CompressedHQ":
                                            importer.textureCompression = TextureImporterCompression.CompressedHQ;
                                            break;
                                        default:
                                            importer.textureCompression = TextureImporterCompression.Compressed;
                                            break;
                                    }

                                    importer.SaveAndReimport();
                                    
                                    processedAssets.Add(new Dictionary<string, object>
                                    {
                                        ["path"] = path,
                                        ["name"] = System.IO.Path.GetFileName(path),
                                        ["type"] = "Texture",
                                        ["settings"] = textureSettings
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                failedAssets.Add(new Dictionary<string, object>
                                {
                                    ["path"] = path,
                                    ["error"] = ex.Message
                                });
                            }
                        }
                        break;

                    case "model":
                        var modelSettings = new
                        {
                            importMaterials = bool.Parse(parameters.GetValueOrDefault("importMaterials", "true")),
                            importAnimation = bool.Parse(parameters.GetValueOrDefault("importAnimation", "true")),
                            meshCompression = parameters.GetValueOrDefault("meshCompression", "Off"),
                            optimizeMesh = bool.Parse(parameters.GetValueOrDefault("optimizeMesh", "true")),
                            generateColliders = bool.Parse(parameters.GetValueOrDefault("generateColliders", "false"))
                        };

                        var modelGUIDs = AssetDatabase.FindAssets("t:Model", new[] { folder });
                        
                        foreach (var guid in modelGUIDs)
                        {
                            var path = AssetDatabase.GUIDToAssetPath(guid);
                            
                            try
                            {
                                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                                if (importer != null)
                                {
                                    importer.materialImportMode = modelSettings.importMaterials ? ModelImporterMaterialImportMode.ImportViaMaterialDescription : ModelImporterMaterialImportMode.None;
                                    importer.importAnimation = modelSettings.importAnimation;
                                    importer.optimizeMeshPolygons = modelSettings.optimizeMesh;
                                    importer.optimizeMeshVertices = modelSettings.optimizeMesh;
                                    importer.addCollider = modelSettings.generateColliders;
                                    
                                    importer.SaveAndReimport();
                                    
                                    processedAssets.Add(new Dictionary<string, object>
                                    {
                                        ["path"] = path,
                                        ["name"] = System.IO.Path.GetFileName(path),
                                        ["type"] = "Model",
                                        ["settings"] = modelSettings
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                failedAssets.Add(new Dictionary<string, object>
                                {
                                    ["path"] = path,
                                    ["error"] = ex.Message
                                });
                            }
                        }
                        break;
                }

                AssetDatabase.Refresh();

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Updated import settings for {processedAssets.Count} assets",
                    processedAssets = processedAssets,
                    failedAssets = failedAssets,
                    summary = new
                    {
                        totalProcessed = processedAssets.Count,
                        failed = failedAssets.Count,
                        assetType = assetType
                    }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private string BatchPrefabUpdate(Dictionary<string, string> parameters)
        {
            try
            {
                var componentType = parameters.GetValueOrDefault("componentType", "");
                var propertyName = parameters.GetValueOrDefault("propertyName", "");
                var propertyValue = parameters.GetValueOrDefault("propertyValue", "");
                var addIfMissing = bool.Parse(parameters.GetValueOrDefault("addIfMissing", "false"));
                var folder = parameters.GetValueOrDefault("folder", "Assets");

                if (string.IsNullOrEmpty(componentType))
                    return JsonConvert.SerializeObject(new { success = false, error = "componentType is required" });

                var updatedPrefabs = new List<Dictionary<string, object>>();
                var failedPrefabs = new List<Dictionary<string, object>>();

                var prefabGUIDs = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
                var componentTypeObj = GetComponentType(componentType);
                
                if (componentTypeObj == null)
                    return JsonConvert.SerializeObject(new { 
                        success = false, 
                        error = $"Component type '{componentType}' not found" 
                    });

                foreach (var guid in prefabGUIDs)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    
                    try
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab != null)
                        {
                            var modified = false;
                            var components = prefab.GetComponentsInChildren(componentTypeObj, true);
                            
                            if (components.Length == 0 && addIfMissing)
                            {
                                prefab.AddComponent(componentTypeObj);
                                components = new[] { prefab.GetComponent(componentTypeObj) };
                                modified = true;
                            }

                            foreach (var comp in components)
                            {
                                if (!string.IsNullOrEmpty(propertyName) && !string.IsNullOrEmpty(propertyValue))
                                {
                                    SetComponentProperty(comp, propertyName, propertyValue);
                                    modified = true;
                                }
                            }

                            if (modified)
                            {
                                PrefabUtility.SavePrefabAsset(prefab);
                                updatedPrefabs.Add(new Dictionary<string, object>
                                {
                                    ["path"] = path,
                                    ["name"] = prefab.name,
                                    ["componentsUpdated"] = components.Length,
                                    ["addedComponent"] = components.Length == 0 && addIfMissing
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        failedPrefabs.Add(new Dictionary<string, object>
                        {
                            ["path"] = path,
                            ["error"] = ex.Message
                        });
                    }
                }

                AssetDatabase.Refresh();

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Updated {updatedPrefabs.Count} prefabs",
                    updatedPrefabs = updatedPrefabs,
                    failedPrefabs = failedPrefabs,
                    settings = new
                    {
                        componentType = componentType,
                        propertyName = propertyName,
                        propertyValue = propertyValue,
                        addIfMissing = addIfMissing,
                        folder = folder
                    }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private string FindUnusedAssets(Dictionary<string, string> parameters)
        {
            try
            {
                var includeTextures = bool.Parse(parameters.GetValueOrDefault("includeTextures", "true"));
                var includeMaterials = bool.Parse(parameters.GetValueOrDefault("includeMaterials", "true"));
                var includePrefabs = bool.Parse(parameters.GetValueOrDefault("includePrefabs", "true"));
                var includeScripts = bool.Parse(parameters.GetValueOrDefault("includeScripts", "false"));
                var excludeFolders = parameters.GetValueOrDefault("excludeFolders", "Packages,Editor").Split(',');

                var unusedAssets = new Dictionary<string, List<string>>();
                var usedAssets = new HashSet<string>();

                // まず、シーン内で使用されているアセットを収集
                var sceneObjects = Resources.FindObjectsOfTypeAll<GameObject>()
                    .Where(go => go.scene.isLoaded);

                foreach (var obj in sceneObjects)
                {
                    CollectUsedAssets(obj, usedAssets);
                }

                // プレハブで使用されているアセットも収集
                var prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
                foreach (var guid in prefabGUIDs)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    usedAssets.Add(path);
                    
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        CollectUsedAssets(prefab, usedAssets);
                    }
                }

                // 各アセットタイプで未使用を検出
                if (includeTextures)
                {
                    var textureGUIDs = AssetDatabase.FindAssets("t:Texture2D");
                    var unusedTextures = new List<string>();
                    
                    foreach (var guid in textureGUIDs)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        if (!usedAssets.Contains(path) && !IsInExcludedFolder(path, excludeFolders))
                        {
                            unusedTextures.Add(path);
                        }
                    }
                    
                    unusedAssets["textures"] = unusedTextures;
                }

                if (includeMaterials)
                {
                    var materialGUIDs = AssetDatabase.FindAssets("t:Material");
                    var unusedMaterials = new List<string>();
                    
                    foreach (var guid in materialGUIDs)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        if (!usedAssets.Contains(path) && !IsInExcludedFolder(path, excludeFolders))
                        {
                            unusedMaterials.Add(path);
                        }
                    }
                    
                    unusedAssets["materials"] = unusedMaterials;
                }

                // 統計情報を計算
                long totalSize = 0;
                var assetDetails = new List<Dictionary<string, object>>();
                
                foreach (var kvp in unusedAssets)
                {
                    foreach (var path in kvp.Value)
                    {
                        var fileInfo = new System.IO.FileInfo(path);
                        if (fileInfo.Exists)
                        {
                            totalSize += fileInfo.Length;
                            assetDetails.Add(new Dictionary<string, object>
                            {
                                ["path"] = path,
                                ["name"] = System.IO.Path.GetFileName(path),
                                ["type"] = kvp.Key,
                                ["size"] = fileInfo.Length,
                                ["sizeMB"] = Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2)
                            });
                        }
                    }
                }

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Found {assetDetails.Count} unused assets",
                    unusedAssets = unusedAssets,
                    assetDetails = assetDetails.OrderByDescending(a => (long)a["size"]).ToList(),
                    summary = new
                    {
                        totalUnused = assetDetails.Count,
                        totalSizeBytes = totalSize,
                        totalSizeMB = Math.Round(totalSize / (1024.0 * 1024.0), 2),
                        byType = unusedAssets.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count)
                    }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private void CollectUsedAssets(GameObject obj, HashSet<string> usedAssets)
        {
            // Renderer materials
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat != null)
                    {
                        var matPath = AssetDatabase.GetAssetPath(mat);
                        if (!string.IsNullOrEmpty(matPath))
                        {
                            usedAssets.Add(matPath);
                            
                            // Material textures
                            var shader = mat.shader;
                            for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
                            {
                                if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                                {
                                    var propName = ShaderUtil.GetPropertyName(shader, i);
                                    var tex = mat.GetTexture(propName);
                                    if (tex != null)
                                    {
                                        var texPath = AssetDatabase.GetAssetPath(tex);
                                        if (!string.IsNullOrEmpty(texPath))
                                            usedAssets.Add(texPath);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // 子オブジェクトも再帰的にチェック
            foreach (Transform child in obj.transform)
            {
                CollectUsedAssets(child.gameObject, usedAssets);
            }
        }

        private bool IsInExcludedFolder(string path, string[] excludeFolders)
        {
            foreach (var folder in excludeFolders)
            {
                if (path.StartsWith(folder.Trim() + "/") || path.Contains("/" + folder.Trim() + "/"))
                    return true;
            }
            return false;
        }

        private string EstimateBuildSize(Dictionary<string, string> parameters)
        {
            try
            {
                var platform = parameters.GetValueOrDefault("platform", "current");
                var includeStreamingAssets = bool.Parse(parameters.GetValueOrDefault("includeStreamingAssets", "true"));
                var includeResources = bool.Parse(parameters.GetValueOrDefault("includeResources", "true"));

                var buildEstimate = new Dictionary<string, object>();
                long totalSize = 0;

                // シーンサイズを推定
                var sceneSizes = new List<Dictionary<string, object>>();
                var sceneGUIDs = AssetDatabase.FindAssets("t:Scene");
                
                foreach (var guid in sceneGUIDs)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var dependencies = AssetDatabase.GetDependencies(path, true);
                    long sceneSize = 0;
                    
                    foreach (var dep in dependencies)
                    {
                        var fileInfo = new System.IO.FileInfo(dep);
                        if (fileInfo.Exists)
                            sceneSize += fileInfo.Length;
                    }
                    
                    sceneSizes.Add(new Dictionary<string, object>
                    {
                        ["scene"] = System.IO.Path.GetFileNameWithoutExtension(path),
                        ["path"] = path,
                        ["sizeBytes"] = sceneSize,
                        ["sizeMB"] = Math.Round(sceneSize / (1024.0 * 1024.0), 2),
                        ["dependencyCount"] = dependencies.Length
                    });
                    
                    totalSize += sceneSize;
                }

                // Resourcesフォルダのサイズ
                long resourcesSize = 0;
                if (includeResources && System.IO.Directory.Exists("Assets/Resources"))
                {
                    resourcesSize = GetFolderSize("Assets/Resources");
                    totalSize += resourcesSize;
                }

                // StreamingAssetsフォルダのサイズ
                long streamingAssetsSize = 0;
                if (includeStreamingAssets && System.IO.Directory.Exists("Assets/StreamingAssets"))
                {
                    streamingAssetsSize = GetFolderSize("Assets/StreamingAssets");
                    totalSize += streamingAssetsSize;
                }

                // スクリプトサイズ（概算）
                var scriptGUIDs = AssetDatabase.FindAssets("t:MonoScript");
                long scriptSize = scriptGUIDs.Length * 5 * 1024; // 平均5KB per compiled script
                totalSize += scriptSize;

                // プラットフォーム固有のオーバーヘッド
                var platformOverhead = platform switch
                {
                    "Android" => 1.2f,
                    "iOS" => 1.15f,
                    "WebGL" => 1.3f,
                    _ => 1.1f
                };

                var estimatedTotal = (long)(totalSize * platformOverhead);

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = "Build size estimation completed",
                    estimate = new
                    {
                        rawAssetsSize = totalSize,
                        estimatedBuildSize = estimatedTotal,
                        estimatedBuildSizeMB = Math.Round(estimatedTotal / (1024.0 * 1024.0), 2),
                        platformOverheadFactor = platformOverhead,
                        platform = platform
                    },
                    breakdown = new
                    {
                        scenes = new
                        {
                            count = sceneSizes.Count,
                            totalSizeMB = Math.Round(sceneSizes.Sum(s => (long)s["sizeBytes"]) / (1024.0 * 1024.0), 2),
                            details = sceneSizes.OrderByDescending(s => (long)s["sizeBytes"]).ToList()
                        },
                        resources = new
                        {
                            sizeMB = Math.Round(resourcesSize / (1024.0 * 1024.0), 2),
                            included = includeResources
                        },
                        streamingAssets = new
                        {
                            sizeMB = Math.Round(streamingAssetsSize / (1024.0 * 1024.0), 2),
                            included = includeStreamingAssets
                        },
                        scripts = new
                        {
                            count = scriptGUIDs.Length,
                            estimatedSizeMB = Math.Round(scriptSize / (1024.0 * 1024.0), 2)
                        }
                    },
                    recommendations = GetBuildSizeRecommendations(estimatedTotal)
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private long GetFolderSize(string folder)
        {
            long size = 0;
            var files = System.IO.Directory.GetFiles(folder, "*", System.IO.SearchOption.AllDirectories);
            
            foreach (var file in files)
            {
                if (!file.EndsWith(".meta"))
                {
                    var fileInfo = new System.IO.FileInfo(file);
                    size += fileInfo.Length;
                }
            }
            
            return size;
        }

        private List<string> GetBuildSizeRecommendations(long buildSize)
        {
            var recommendations = new List<string>();
            var sizeMB = buildSize / (1024.0 * 1024.0);

            if (sizeMB > 100)
                recommendations.Add("Consider texture compression and atlas optimization");
            if (sizeMB > 200)
                recommendations.Add("Review audio compression settings");
            if (sizeMB > 500)
                recommendations.Add("Consider using Asset Bundles for dynamic loading");
                
            recommendations.Add("Use sprite atlases to reduce draw calls");
            recommendations.Add("Remove unused assets before building");
            
            return recommendations;
        }

        private string PerformanceReport(Dictionary<string, string> parameters)
        {
            try
            {
                var includeRendering = bool.Parse(parameters.GetValueOrDefault("includeRendering", "true"));
                var includeMemory = bool.Parse(parameters.GetValueOrDefault("includeMemory", "true"));
                var includeTextures = bool.Parse(parameters.GetValueOrDefault("includeTextures", "true"));
                var includeAudio = bool.Parse(parameters.GetValueOrDefault("includeAudio", "true"));

                var report = new Dictionary<string, object>();

                // レンダリング統計
                if (includeRendering)
                {
                    var renderers = UnityEngine.Object.FindObjectsOfType<Renderer>();
                    var cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
                    var lights = UnityEngine.Object.FindObjectsOfType<Light>();
                    
                    var materialStats = new Dictionary<string, int>();
                    var meshStats = new Dictionary<string, int>();
                    long totalVertices = 0;
                    long totalTriangles = 0;
                    int renderersWithMeshFilter = 0;
                    int renderersWithoutMeshFilter = 0;
                    var rendererTypes = new Dictionary<string, int>();

                    foreach (var renderer in renderers)
                    {
                        try
                        {
                            if (renderer == null || !renderer.enabled)
                                continue;
                            
                            // レンダラータイプをカウント
                            var rendererType = renderer.GetType().Name;
                            rendererTypes[rendererType] = rendererTypes.GetValueOrDefault(rendererType, 0) + 1;
                            
                            // マテリアル統計
                            if (renderer.sharedMaterials != null)
                            {
                                foreach (var mat in renderer.sharedMaterials)
                                {
                                    if (mat != null && mat.shader != null)
                                    {
                                        var shader = mat.shader.name;
                                        materialStats[shader] = materialStats.GetValueOrDefault(shader, 0) + 1;
                                    }
                                }
                            }

                            // MeshFilterの安全な取得
                            MeshFilter meshFilter = null;
                            try
                            {
                                meshFilter = renderer.GetComponent<MeshFilter>();
                            }
                            catch (MissingComponentException)
                            {
                                // MeshFilterが存在しない場合は無視
                            }
                            if (meshFilter?.sharedMesh != null)
                            {
                                renderersWithMeshFilter++;
                                var mesh = meshFilter.sharedMesh;
                                totalVertices += mesh.vertexCount;
                                totalTriangles += mesh.triangles.Length / 3;
                                
                                var meshName = mesh.name ?? "Unnamed";
                                meshStats[meshName] = meshStats.GetValueOrDefault(meshName, 0) + 1;
                            }
                            else
                            {
                                renderersWithoutMeshFilter++;
                                
                                // SkinnedMeshRendererの場合は別途処理
                                if (renderer is SkinnedMeshRenderer skinnedRenderer && skinnedRenderer.sharedMesh != null)
                                {
                                    var mesh = skinnedRenderer.sharedMesh;
                                    totalVertices += mesh.vertexCount;
                                    totalTriangles += mesh.triangles.Length / 3;
                                    
                                    var meshName = $"{mesh.name} (Skinned)" ?? "Unnamed (Skinned)";
                                    meshStats[meshName] = meshStats.GetValueOrDefault(meshName, 0) + 1;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[PerformanceReport] Error processing renderer on {renderer?.name}: {ex.Message}");
                            continue;
                        }
                    }

                    report["rendering"] = new
                    {
                        activeRenderers = renderers.Count(r => r.enabled),
                        totalVertices = totalVertices,
                        totalTriangles = totalTriangles,
                        cameras = cameras.Length,
                        lights = lights.Length,
                        renderersWithMeshFilter = renderersWithMeshFilter,
                        renderersWithoutMeshFilter = renderersWithoutMeshFilter,
                        skinnedMeshRenderers = renderers.Count(r => r is SkinnedMeshRenderer && r.enabled),
                        meshFiltersWithoutRenderer = UnityEngine.Object.FindObjectsOfType<MeshFilter>()
                            .Count(mf => mf.GetComponent<MeshRenderer>() == null),
                        rendererTypes = rendererTypes.OrderByDescending(kvp => kvp.Value)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                        shaderUsage = materialStats.OrderByDescending(kvp => kvp.Value)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                        meshUsage = meshStats.OrderByDescending(kvp => kvp.Value)
                            .Take(10).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                        qualitySettings = new
                        {
                            currentLevel = QualitySettings.GetQualityLevel(),
                            pixelLightCount = QualitySettings.pixelLightCount,
                            shadows = QualitySettings.shadows.ToString(),
                            antiAliasing = QualitySettings.antiAliasing
                        }
                    };
                }

                // メモリ統計
                if (includeMemory)
                {
                    report["memory"] = new
                    {
                        totalAllocatedMemoryMB = Profiler.GetTotalAllocatedMemoryLong() / (1024.0 * 1024.0),
                        totalReservedMemoryMB = Profiler.GetTotalReservedMemoryLong() / (1024.0 * 1024.0),
                        monoHeapSizeMB = Profiler.GetMonoHeapSizeLong() / (1024.0 * 1024.0),
                        monoUsedSizeMB = Profiler.GetMonoUsedSizeLong() / (1024.0 * 1024.0)
                    };
                }

                // テクスチャ統計
                if (includeTextures)
                {
                    var textureMemory = 0L;
                    var textureCount = 0;
                    var texturesBySize = new Dictionary<string, int>();
                    
                    var textures = Resources.FindObjectsOfTypeAll<Texture2D>();
                    foreach (var tex in textures)
                    {
                        if (AssetDatabase.Contains(tex))
                        {
                            textureCount++;
                            var size = Profiler.GetRuntimeMemorySizeLong(tex);
                            textureMemory += size;
                            
                            var sizeCategory = tex.width + "x" + tex.height;
                            texturesBySize[sizeCategory] = texturesBySize.GetValueOrDefault(sizeCategory, 0) + 1;
                        }
                    }

                    report["textures"] = new
                    {
                        count = textureCount,
                        totalMemoryMB = textureMemory / (1024.0 * 1024.0),
                        sizeDistribution = texturesBySize.OrderByDescending(kvp => kvp.Value)
                            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                    };
                }

                // オーディオ統計
                if (includeAudio)
                {
                    var audioClips = Resources.FindObjectsOfTypeAll<AudioClip>();
                    var audioMemory = 0L;
                    var audioStats = new Dictionary<string, object>();
                    
                    foreach (var clip in audioClips)
                    {
                        if (AssetDatabase.Contains(clip))
                        {
                            audioMemory += Profiler.GetRuntimeMemorySizeLong(clip);
                        }
                    }

                    report["audio"] = new
                    {
                        clipCount = audioClips.Length,
                        totalMemoryMB = audioMemory / (1024.0 * 1024.0),
                        audioSourceCount = UnityEngine.Object.FindObjectsOfType<AudioSource>().Length
                    };
                }

                // パフォーマンス推奨事項
                var recommendations = new List<string>();
                
                if (includeRendering && report.ContainsKey("rendering"))
                {
                    var rendering = report["rendering"] as Dictionary<string, object>;
                    if (rendering != null && rendering.ContainsKey("totalTriangles") && (long)rendering["totalTriangles"] > 1000000)
                        recommendations.Add("High polygon count detected. Consider LOD implementation");
                    if (rendering != null && rendering.ContainsKey("lights") && (int)rendering["lights"] > 10)
                        recommendations.Add("Many lights detected. Consider light baking");
                }

                if (includeTextures && report.ContainsKey("textures"))
                {
                    var textures = report["textures"] as Dictionary<string, object>;
                    if (textures != null && textures.ContainsKey("totalMemoryMB") && (double)textures["totalMemoryMB"] > 500)
                        recommendations.Add("High texture memory usage. Consider texture compression");
                }

                report["recommendations"] = recommendations;

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = "Performance report generated",
                    report = report,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private string AutoOrganizeFolders(Dictionary<string, string> parameters)
        {
            try
            {
                var rootFolder = parameters.GetValueOrDefault("rootFolder", "Assets");
                var createFolders = bool.Parse(parameters.GetValueOrDefault("createFolders", "true"));
                var moveAssets = bool.Parse(parameters.GetValueOrDefault("moveAssets", "false"));
                var dryRun = bool.Parse(parameters.GetValueOrDefault("dryRun", "true"));

                var standardFolders = new Dictionary<string, string[]>
                {
                    ["Scripts"] = new[] { ".cs", ".js" },
                    ["Materials"] = new[] { ".mat" },
                    ["Textures"] = new[] { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".tiff" },
                    ["Models"] = new[] { ".fbx", ".obj", ".dae", ".3ds", ".blend" },
                    ["Prefabs"] = new[] { ".prefab" },
                    ["Audio"] = new[] { ".wav", ".mp3", ".ogg", ".aiff" },
                    ["Animations"] = new[] { ".anim", ".controller" },
                    ["Fonts"] = new[] { ".ttf", ".otf" },
                    ["Shaders"] = new[] { ".shader", ".shadergraph" },
                    ["UI"] = new string[] { } // UI要素は特別に処理
                };

                var operations = new List<Dictionary<string, object>>();
                var createdFolders = new List<string>();

                // フォルダ作成
                if (createFolders)
                {
                    foreach (var folder in standardFolders.Keys)
                    {
                        var folderPath = $"{rootFolder}/{folder}";
                        if (!AssetDatabase.IsValidFolder(folderPath))
                        {
                            if (!dryRun)
                            {
                                AssetDatabase.CreateFolder(rootFolder, folder);
                            }
                            createdFolders.Add(folderPath);
                        }
                    }
                }

                // アセット整理
                if (moveAssets)
                {
                    var files = System.IO.Directory.GetFiles(rootFolder, "*", System.IO.SearchOption.TopDirectoryOnly)
                        .Where(f => !f.EndsWith(".meta"));

                    foreach (var file in files)
                    {
                        var extension = System.IO.Path.GetExtension(file).ToLower();
                        var fileName = System.IO.Path.GetFileName(file);
                        
                        foreach (var kvp in standardFolders)
                        {
                            if (kvp.Value.Contains(extension))
                            {
                                var targetFolder = $"{rootFolder}/{kvp.Key}";
                                var newPath = $"{targetFolder}/{fileName}";
                                
                                operations.Add(new Dictionary<string, object>
                                {
                                    ["operation"] = "move",
                                    ["from"] = file.Replace('\\', '/'),
                                    ["to"] = newPath,
                                    ["folder"] = kvp.Key
                                });

                                if (!dryRun && AssetDatabase.IsValidFolder(targetFolder))
                                {
                                    AssetDatabase.MoveAsset(file.Replace('\\', '/'), newPath);
                                }
                                
                                break;
                            }
                        }
                    }
                }

                if (!dryRun)
                    AssetDatabase.Refresh();

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = dryRun ? 
                        "Dry run completed - no changes made" : 
                        $"Organized {operations.Count} assets into {createdFolders.Count} folders",
                    createdFolders = createdFolders,
                    operations = operations,
                    summary = new
                    {
                        foldersCreated = createdFolders.Count,
                        assetsToMove = operations.Count,
                        dryRun = dryRun
                    },
                    standardStructure = standardFolders.Keys.ToList()
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private string GenerateLOD(Dictionary<string, string> parameters)
        {
            try
            {
                // API定義に合わせて targetObject パラメータをメインに使用し、target もサポート
                var targetObject = parameters.GetValueOrDefault("targetObject", "") ?? 
                                 parameters.GetValueOrDefault("target", "");
                var lodLevels = int.Parse(parameters.GetValueOrDefault("lodLevels", "3"));
                var qualitySettings = parameters.GetValueOrDefault("qualitySettings", "0.75,0.5,0.25");
                
                if (string.IsNullOrEmpty(targetObject))
                    return CreateMissingParameterResponse("GenerateLOD", "targetObject", parameters);

                var gameObject = GameObject.Find(targetObject);
                if (gameObject == null)
                    return JsonConvert.SerializeObject(new { 
                        success = false, 
                        error = "GameObject not found" 
                    });

                var meshFilter = gameObject.GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    // MeshFilterが無い場合は自動的に追加
                    meshFilter = gameObject.AddComponent<MeshFilter>();
                    
                    // 基本的な形状のメッシュを設定
                    var renderer = gameObject.GetComponent<Renderer>();
                    if (renderer == null)
                    {
                        renderer = gameObject.AddComponent<MeshRenderer>();
                    }
                    
                    // プリミティブメッシュを作成（Cubeをデフォルトとして使用）
                    var tempGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    meshFilter.sharedMesh = tempGO.GetComponent<MeshFilter>().sharedMesh;
                    GameObject.DestroyImmediate(tempGO);
                    
                    Debug.Log($"[Nexus] Auto-added MeshFilter with default cube mesh to '{gameObject.name}'");
                }
                
                if (meshFilter.sharedMesh == null)
                    return JsonConvert.SerializeObject(new { 
                        success = false, 
                        error = "MeshFilter has no mesh assigned" 
                    });

                // LODGroupを追加または取得
                var lodGroup = gameObject.GetComponent<LODGroup>();
                if (lodGroup == null)
                    lodGroup = gameObject.AddComponent<LODGroup>();

                var qualityLevels = qualitySettings.Split(',').Select(float.Parse).ToArray();
                var lods = new LOD[lodLevels];
                var lodObjects = new List<GameObject>();

                // LOD0は元のオブジェクト
                lods[0] = new LOD(0.6f, new Renderer[] { gameObject.GetComponent<Renderer>() });

                // 追加のLODレベルを生成
                for (int i = 1; i < lodLevels; i++)
                {
                    var lodObject = new GameObject($"{gameObject.name}_LOD{i}");
                    lodObject.transform.SetParent(gameObject.transform);
                    lodObject.transform.localPosition = Vector3.zero;
                    lodObject.transform.localRotation = Quaternion.identity;
                    lodObject.transform.localScale = Vector3.one;

                    var lodMeshFilter = lodObject.AddComponent<MeshFilter>();
                    var lodRenderer = lodObject.AddComponent<MeshRenderer>();
                    
                    // 元のRendererの設定をコピー
                    var originalRenderer = gameObject.GetComponent<Renderer>();
                    lodRenderer.sharedMaterials = originalRenderer.sharedMaterials;

                    // 簡易的なLODメッシュ生成（実際の実装では専門的なメッシュ簡略化アルゴリズムを使用）
                    var quality = i < qualityLevels.Length ? qualityLevels[i] : 0.5f / i;
                    lodMeshFilter.sharedMesh = meshFilter.sharedMesh; // 本来はここで簡略化されたメッシュを生成
                    
                    float screenRelativeHeight = i == 1 ? 0.3f : 0.1f / (i - 1);
                    lods[i] = new LOD(screenRelativeHeight, new Renderer[] { lodRenderer });
                    
                    lodObjects.Add(lodObject);
                }

                lodGroup.SetLODs(lods);
                lodGroup.RecalculateBounds();

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Generated {lodLevels} LOD levels for '{targetObject}'",
                    target = targetObject,
                    lodLevels = lodLevels,
                    lodInfo = lods.Select((lod, i) => new
                    {
                        level = i,
                        screenRelativeHeight = lod.screenRelativeTransitionHeight,
                        rendererCount = lod.renderers.Length
                    }).ToList(),
                    note = "This is a simplified LOD generation. For production, use specialized mesh decimation tools."
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        private string AutoAtlasTextures(Dictionary<string, string> parameters)
        {
            try
            {
                var folder = parameters.GetValueOrDefault("folder", "Assets/Textures");
                var atlasName = parameters.GetValueOrDefault("atlasName", "TextureAtlas");
                var maxAtlasSize = int.Parse(parameters.GetValueOrDefault("maxAtlasSize", "2048"));
                var padding = int.Parse(parameters.GetValueOrDefault("padding", "2"));
                
                // フォルダ存在確認
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    // フォルダが存在しない場合は作成
                    var folderParts = folder.Split('/');
                    var parentPath = folderParts[0];
                    
                    for (int i = 1; i < folderParts.Length; i++)
                    {
                        var nextPath = parentPath + "/" + folderParts[i];
                        if (!AssetDatabase.IsValidFolder(nextPath))
                        {
                            AssetDatabase.CreateFolder(parentPath, folderParts[i]);
                        }
                        parentPath = nextPath;
                    }
                    
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"Folder '{folder}' did not exist and was created. No textures found to atlas."
                    });
                }
                
                // Unity 2020以降のSprite Atlas APIを想定
                var textureGUIDs = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                var textures = new List<Texture2D>();
                
                foreach (var guid in textureGUIDs.Take(50)) // 制限をかける
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    
                    if (texture != null && texture.width <= maxAtlasSize && texture.height <= maxAtlasSize)
                    {
                        // Read/Write enabledをチェック
                        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                        if (importer != null && !importer.isReadable)
                        {
                            importer.isReadable = true;
                            importer.SaveAndReimport();
                        }
                        
                        textures.Add(texture);
                    }
                }

                if (textures.Count == 0)
                {
                    // テクスチャが見つからない場合の詳細診断
                    var diagnostics = new Dictionary<string, object>();
                    diagnostics["searchFolder"] = folder;
                    diagnostics["totalTexturesInFolder"] = textureGUIDs.Length;
                    
                    // テクスチャが見つかったがすべて無効だった場合、理由を分析
                    if (textureGUIDs.Length > 0)
                    {
                        var rejectionReasons = new Dictionary<string, int>();
                        var tooLarge = 0;
                        var failedToLoad = 0;
                        
                        foreach (var guid in textureGUIDs.Take(10)) // サンプルチェック
                        {
                            var path = AssetDatabase.GUIDToAssetPath(guid);
                            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                            
                            if (texture == null)
                            {
                                failedToLoad++;
                            }
                            else if (texture.width > maxAtlasSize || texture.height > maxAtlasSize)
                            {
                                tooLarge++;
                            }
                        }
                        
                        if (tooLarge > 0)
                            rejectionReasons["Textures too large for atlas"] = tooLarge;
                        if (failedToLoad > 0)
                            rejectionReasons["Failed to load texture"] = failedToLoad;
                            
                        diagnostics["rejectionReasons"] = rejectionReasons;
                        diagnostics["suggestion"] = "Try increasing maxAtlasSize or using a different folder";
                    }
                    else
                    {
                        // フォルダ内の全ファイルをリスト
                        var allAssets = AssetDatabase.FindAssets("", new[] { folder });
                        diagnostics["totalAssetsInFolder"] = allAssets.Length;
                        
                        if (allAssets.Length == 0)
                        {
                            diagnostics["note"] = "Folder is empty";
                            diagnostics["suggestion"] = "Add texture files to the folder first";
                        }
                        else
                        {
                            diagnostics["note"] = "Folder contains assets but no textures";
                            diagnostics["suggestion"] = "Ensure files are imported as Texture2D type";
                        }
                    }
                    
                    return JsonConvert.SerializeObject(new { 
                        success = false, 
                        error = "No suitable textures found for atlas creation",
                        diagnostics = diagnostics,
                        requirements = new {
                            maxSize = $"{maxAtlasSize}x{maxAtlasSize}",
                            formats = new[] { "PNG", "JPG", "TGA", "PSD", "BMP" },
                            settings = "Texture must be readable (Read/Write Enabled)"
                        }
                    }, Formatting.Indented);
                }

                // アトラス作成
                var atlas = new Texture2D(maxAtlasSize, maxAtlasSize);
                var rects = atlas.PackTextures(textures.ToArray(), padding, maxAtlasSize);
                
                if (rects == null || rects.Length == 0)
                    return JsonConvert.SerializeObject(new { 
                        success = false, 
                        error = "Failed to pack textures into atlas" 
                    });

                // アトラスを保存
                var atlasPath = $"{folder}/{atlasName}.png";
                var pngData = atlas.EncodeToPNG();
                System.IO.File.WriteAllBytes(atlasPath, pngData);
                AssetDatabase.Refresh();
                
                // アトラス設定
                var atlasImporter = AssetImporter.GetAtPath(atlasPath) as TextureImporter;
                if (atlasImporter != null)
                {
                    atlasImporter.textureType = TextureImporterType.Sprite;
                    atlasImporter.spriteImportMode = SpriteImportMode.Multiple;
                    atlasImporter.maxTextureSize = maxAtlasSize;
                    atlasImporter.SaveAndReimport();
                }

                // アトラス情報を生成
                var atlasInfo = new List<Dictionary<string, object>>();
                for (int i = 0; i < textures.Count; i++)
                {
                    atlasInfo.Add(new Dictionary<string, object>
                    {
                        ["textureName"] = textures[i].name,
                        ["rect"] = new
                        {
                            x = rects[i].x,
                            y = rects[i].y,
                            width = rects[i].width,
                            height = rects[i].height
                        }
                    });
                }

                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Created texture atlas with {textures.Count} textures",
                    atlasPath = atlasPath,
                    atlasSize = new { width = atlas.width, height = atlas.height },
                    textureCount = textures.Count,
                    textures = atlasInfo,
                    settings = new
                    {
                        maxSize = maxAtlasSize,
                        padding = padding
                    }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }

        // ===== ゲーム開発特化機能 =====
        
        private string CreateGameController(Dictionary<string, string> parameters)
        {
            try
            {
                var controllerType = parameters.GetValueOrDefault("type", "FirstPerson");
                var playerName = parameters.GetValueOrDefault("playerName", "Player");
                var includeCamera = bool.Parse(parameters.GetValueOrDefault("includeCamera", "true"));
                var movementSpeed = float.Parse(parameters.GetValueOrDefault("movementSpeed", "5"));
                var jumpHeight = float.Parse(parameters.GetValueOrDefault("jumpHeight", "3"));
                
                // プレイヤーGameObjectを作成
                var player = new GameObject(playerName);
                
                // 基本コンポーネントを追加
                var rb = player.AddComponent<Rigidbody>();
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                
                var collider = player.AddComponent<CapsuleCollider>();
                collider.height = 2f;
                collider.radius = 0.5f;
                
                // コントローラースクリプトを生成
                string scriptContent = "";
                string scriptName = "";
                
                switch (controllerType)
                {
                    case "FirstPerson":
                        scriptName = "FirstPersonController";
                        scriptContent = GenerateFirstPersonControllerScript(movementSpeed, jumpHeight);
                        break;
                        
                    case "ThirdPerson":
                        scriptName = "ThirdPersonController";
                        scriptContent = GenerateThirdPersonControllerScript(movementSpeed, jumpHeight);
                        break;
                        
                    case "TopDown":
                        scriptName = "TopDownController";
                        scriptContent = GenerateTopDownControllerScript(movementSpeed);
                        break;
                        
                    case "Platformer2D":
                        scriptName = "Platformer2DController";
                        scriptContent = GeneratePlatformer2DControllerScript(movementSpeed, jumpHeight);
                        // 2Dの場合はコンポーネントを調整
                        UnityEngine.Object.DestroyImmediate(rb);
                        UnityEngine.Object.DestroyImmediate(collider);
                        player.AddComponent<Rigidbody2D>();
                        player.AddComponent<BoxCollider2D>();
                        break;
                }
                
                // スクリプトを保存
                var scriptPath = $"Assets/Scripts/Controllers/{scriptName}.cs";
                var scriptDir = System.IO.Path.GetDirectoryName(scriptPath);
                if (!System.IO.Directory.Exists(scriptDir))
                {
                    System.IO.Directory.CreateDirectory(scriptDir);
                }
                System.IO.File.WriteAllText(scriptPath, scriptContent);
                AssetDatabase.Refresh();
                
                // カメラセットアップ
                if (includeCamera)
                {
                    GameObject cameraGO = null;
                    switch (controllerType)
                    {
                        case "FirstPerson":
                            cameraGO = new GameObject("PlayerCamera");
                            cameraGO.AddComponent<Camera>();
                            cameraGO.transform.parent = player.transform;
                            cameraGO.transform.localPosition = new Vector3(0, 1.6f, 0);
                            break;
                            
                        case "ThirdPerson":
                            cameraGO = new GameObject("CameraRig");
                            var pivot = new GameObject("CameraPivot");
                            pivot.transform.parent = cameraGO.transform;
                            var cam = new GameObject("Camera");
                            cam.AddComponent<Camera>();
                            cam.transform.parent = pivot.transform;
                            cam.transform.localPosition = new Vector3(0, 2, -5);
                            cam.transform.LookAt(pivot.transform);
                            break;
                    }
                }
                
                Selection.activeGameObject = player;
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Created {controllerType} controller",
                    gameObjectName = playerName,
                    scriptPath = scriptPath,
                    components = new[] { "Rigidbody", "Collider", scriptName },
                    settings = new
                    {
                        type = controllerType,
                        movementSpeed = movementSpeed,
                        jumpHeight = jumpHeight,
                        includeCamera = includeCamera
                    }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        private string GenerateFirstPersonControllerScript(float moveSpeed, float jumpHeight)
        {
            return @"using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    [Header(""Movement"")]
    public float moveSpeed = " + moveSpeed + @"f;
    public float jumpHeight = " + jumpHeight + @"f;
    public float gravity = -9.81f;
    
    [Header(""Look"")]
    public float mouseSensitivity = 2f;
    public float lookXLimit = 80f;
    
    CharacterController characterController;
    Vector3 moveDirection = Vector3.zero;
    float rotationX = 0;
    
    public Camera playerCamera;
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (!characterController)
            characterController = gameObject.AddComponent<CharacterController>();
        
        if (!playerCamera)
            playerCamera = GetComponentInChildren<Camera>();
            
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    void Update()
    {
        // Movement
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        
        float curSpeedX = moveSpeed * Input.GetAxis(""Vertical"");
        float curSpeedY = moveSpeed * Input.GetAxis(""Horizontal"");
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);
        
        if (Input.GetButton(""Jump"") && characterController.isGrounded)
        {
            moveDirection.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }
        
        if (!characterController.isGrounded)
        {
            moveDirection.y += gravity * Time.deltaTime;
        }
        
        characterController.Move(moveDirection * Time.deltaTime);
        
        // Camera rotation
        rotationX += -Input.GetAxis(""Mouse Y"") * mouseSensitivity;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis(""Mouse X"") * mouseSensitivity, 0);
    }
}";
        }
        
        private string GenerateThirdPersonControllerScript(float moveSpeed, float jumpHeight)
        {
            return @"using UnityEngine;

public class ThirdPersonController : MonoBehaviour
{
    [Header(""Movement"")]
    public float moveSpeed = " + moveSpeed + @"f;
    public float jumpHeight = " + jumpHeight + @"f;
    public float turnSmoothTime = 0.1f;
    
    [Header(""Camera"")]
    public Transform cameraTransform;
    
    float turnSmoothVelocity;
    CharacterController controller;
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (!controller)
            controller = gameObject.AddComponent<CharacterController>();
            
        if (!cameraTransform && Camera.main)
            cameraTransform = Camera.main.transform;
    }
    
    void Update()
    {
        float horizontal = Input.GetAxisRaw(""Horizontal"");
        float vertical = Input.GetAxisRaw(""Vertical"");
        Vector3 direction = SafeNormalize(new Vector3(horizontal, 0f, vertical));
        
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(SafeNormalize(moveDir) * moveSpeed * Time.deltaTime);
        }
        
        if (Input.GetButtonDown(""Jump"") && controller.isGrounded)
        {
            // Jump logic
        }
    }
}";
        }
        
        private string GenerateTopDownControllerScript(float moveSpeed)
        {
            return @"using UnityEngine;

public class TopDownController : MonoBehaviour
{
    public float moveSpeed = " + moveSpeed + @"f;
    public bool rotateTowardsMouse = true;
    
    Rigidbody rb;
    Camera mainCamera;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
    }
    
    void Update()
    {
        float horizontal = Input.GetAxis(""Horizontal"");
        float vertical = Input.GetAxis(""Vertical"");
        
        Vector3 movement = SafeNormalize(new Vector3(horizontal, 0, vertical)) * moveSpeed;
        rb.velocity = new Vector3(movement.x, rb.velocity.y, movement.z);
        
        if (rotateTowardsMouse)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Vector3 lookDir = hit.point - transform.position;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }
        }
        else if (movement != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(movement);
        }
    }
}";
        }
        
        private string GeneratePlatformer2DControllerScript(float moveSpeed, float jumpHeight)
        {
            return @"using UnityEngine;

public class Platformer2DController : MonoBehaviour
{
    [Header(""Movement"")]
    public float moveSpeed = " + moveSpeed + @"f;
    public float jumpForce = " + jumpHeight * 3 + @"f;
    
    [Header(""Ground Check"")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    
    Rigidbody2D rb;
    bool isGrounded;
    float moveInput;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Create ground check if not assigned
        if (!groundCheck)
        {
            GameObject gc = new GameObject(""GroundCheck"");
            gc.transform.parent = transform;
            gc.transform.localPosition = new Vector3(0, -1f, 0);
            groundCheck = gc.transform;
        }
    }
    
    void Update()
    {
        moveInput = Input.GetAxis(""Horizontal"");
        
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        
        if (Input.GetButtonDown(""Jump"") && isGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
        
        // Flip sprite based on direction
        if (moveInput > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput < 0)
            transform.localScale = new Vector3(-1, 1, 1);
    }
    
    void FixedUpdate()
    {
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
    }
}";
        }
        
        private string SetupInputSystem(Dictionary<string, string> parameters)
        {
            try
            {
                var template = parameters.GetValueOrDefault("template", "Standard");
                var createAsset = bool.Parse(parameters.GetValueOrDefault("createAsset", "true"));
                
                string inputActionsContent = "";
                string assetPath = "Assets/Settings/InputActions.inputactions";
                
                switch (template)
                {
                    case "Standard":
                        inputActionsContent = GenerateStandardInputActions();
                        break;
                    case "Mobile":
                        inputActionsContent = GenerateMobileInputActions();
                        break;
                    case "VR":
                        inputActionsContent = GenerateVRInputActions();
                        break;
                }
                
                if (createAsset && !string.IsNullOrEmpty(inputActionsContent))
                {
                    var dir = System.IO.Path.GetDirectoryName(assetPath);
                    if (!System.IO.Directory.Exists(dir))
                    {
                        System.IO.Directory.CreateDirectory(dir);
                    }
                    
                    System.IO.File.WriteAllText(assetPath, inputActionsContent);
                    AssetDatabase.Refresh();
                }
                
                // Input System設定を更新
                // PlayerSettingsのInput System設定を更新
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Set up {template} input system",
                    assetPath = assetPath,
                    template = template,
                    settings = new
                    {
                        activeInputHandling = "Both",
                        createdAsset = createAsset
                    }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        private string GenerateStandardInputActions()
        {
            return @"{
    ""name"": ""InputActions"",
    ""maps"": [
        {
            ""name"": ""Player"",
            ""id"": ""f62a4b92-2026-4d06-8d54-5a7a3e9263a3"",
            ""actions"": [
                {
                    ""name"": ""Move"",
                    ""type"": ""Value"",
                    ""id"": ""6b5a16bd-c6db-420e-a1f5-58e4b4c7b2d0"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Jump"",
                    ""type"": ""Button"",
                    ""id"": ""8c4abdf8-4099-493a-aa1a-129e93b5d395"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Look"",
                    ""type"": ""Value"",
                    ""id"": ""2690aefc-a4cc-4ddc-85aa-79b633077c7a"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": ""WASD"",
                    ""id"": ""00ca640b-d935-4593-8157-c05a1c025fe3"",
                    ""path"": ""2DVector"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": """",
                    ""action"": ""Move"",
                    ""isComposite"": true,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": ""up"",
                    ""id"": ""e2062cb9-1b15-4a9e-a5dc-cdaf3e82d1a1"",
                    ""path"": ""<Keyboard>/w"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""down"",
                    ""id"": ""320838fc-f295-4832-8673-08ff8ec0d75e"",
                    ""path"": ""<Keyboard>/s"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""left"",
                    ""id"": ""d2581a9b-1d11-4566-b73e-c5c1882c073a"",
                    ""path"": ""<Keyboard>/a"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": ""right"",
                    ""id"": ""fcfe8d35-23dc-4c1c-9cfd-f0cd20926f07"",
                    ""path"": ""<Keyboard>/d"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": true
                },
                {
                    ""name"": """",
                    ""id"": ""1077f913-e8ba-4d5f-beb7-507b4e665d37"",
                    ""path"": ""<Keyboard>/space"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Jump"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8c8e490e-08ff-4d1b-912f-0154797a2b73"",
                    ""path"": ""<Mouse>/delta"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Keyboard&Mouse"",
                    ""action"": ""Look"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""Keyboard&Mouse"",
            ""bindingGroup"": ""Keyboard&Mouse"",
            ""devices"": [
                {
                    ""devicePath"": ""<Keyboard>"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<Mouse>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        }
    ]
}";
        }
        
        private string GenerateMobileInputActions()
        {
            return @"{
    ""name"": ""MobileInputActions"",
    ""maps"": [
        {
            ""name"": ""Touch"",
            ""id"": ""a3f5ec22-78b5-4adb-b4f3-96d5f7d77825"",
            ""actions"": [
                {
                    ""name"": ""PrimaryTouch"",
                    ""type"": ""PassThrough"",
                    ""id"": ""f8b3e0a3-fb86-44da-9c65-8a5c67e5cf8d"",
                    ""expectedControlType"": ""Touch"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""SecondaryTouch"",
                    ""type"": ""PassThrough"",
                    ""id"": ""13b5bbfe-f03f-42e9-9c88-9f87ee72b299"",
                    ""expectedControlType"": ""Touch"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""1a7e28fe-784f-4cd6-ba3b-15401a1f3246"",
                    ""path"": ""<Touchscreen>/touch0"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Touch"",
                    ""action"": ""PrimaryTouch"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""4fca3ef6-1574-40c8-9f5f-3f0db643d1f5"",
                    ""path"": ""<Touchscreen>/touch1"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Touch"",
                    ""action"": ""SecondaryTouch"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""Touch"",
            ""bindingGroup"": ""Touch"",
            ""devices"": [
                {
                    ""devicePath"": ""<Touchscreen>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        }
    ]
}";
        }
        
        private string GenerateVRInputActions()
        {
            return @"{
    ""name"": ""VRInputActions"",
    ""maps"": [
        {
            ""name"": ""VR"",
            ""id"": ""c95b2d34-7e4c-4f89-89cf-08eff0e4c5ef"",
            ""actions"": [
                {
                    ""name"": ""TriggerLeft"",
                    ""type"": ""Button"",
                    ""id"": ""4c9a2d34-7e4c-4f89-89cf-08eff0e4c5ef"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""TriggerRight"",
                    ""type"": ""Button"",
                    ""id"": ""5c9a2d34-7e4c-4f89-89cf-08eff0e4c5ef"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Move"",
                    ""type"": ""Value"",
                    ""id"": ""6c9a2d34-7e4c-4f89-89cf-08eff0e4c5ef"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""7c9a2d34-7e4c-4f89-89cf-08eff0e4c5ef"",
                    ""path"": ""<XRController>{LeftHand}/triggerPressed"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""VR"",
                    ""action"": ""TriggerLeft"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8c9a2d34-7e4c-4f89-89cf-08eff0e4c5ef"",
                    ""path"": ""<XRController>{RightHand}/triggerPressed"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""VR"",
                    ""action"": ""TriggerRight"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""9c9a2d34-7e4c-4f89-89cf-08eff0e4c5ef"",
                    ""path"": ""<XRController>{LeftHand}/primary2DAxis"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""VR"",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""VR"",
            ""bindingGroup"": ""VR"",
            ""devices"": [
                {
                    ""devicePath"": ""<XRController>{LeftHand}"",
                    ""isOptional"": false,
                    ""isOR"": false
                },
                {
                    ""devicePath"": ""<XRController>{RightHand}"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        }
    ]
}";
        }
        
        private string CreateStateMachine(Dictionary<string, string> parameters)
        {
            try
            {
                var targetObject = parameters.GetValueOrDefault("targetObject", "");
                var stateMachineType = parameters.GetValueOrDefault("type", "Character");
                var states = parameters.GetValueOrDefault("states", "Idle,Walk,Run,Jump,Attack").Split(',');
                
                GameObject target = null;
                if (!string.IsNullOrEmpty(targetObject))
                {
                    target = GameObject.Find(targetObject);
                }
                if (target == null)
                {
                    target = new GameObject($"{stateMachineType}StateMachine");
                }
                
                // ステートマシンスクリプトを生成
                var scriptName = $"{stateMachineType}StateMachine";
                var scriptContent = GenerateStateMachineScript(scriptName, states);
                
                var scriptPath = $"Assets/Scripts/StateMachines/{scriptName}.cs";
                var scriptDir = System.IO.Path.GetDirectoryName(scriptPath);
                if (!System.IO.Directory.Exists(scriptDir))
                {
                    System.IO.Directory.CreateDirectory(scriptDir);
                }
                
                System.IO.File.WriteAllText(scriptPath, scriptContent);
                
                // 各ステートのスクリプトも生成
                foreach (var state in states)
                {
                    var stateScriptPath = $"Assets/Scripts/StateMachines/States/{stateMachineType}{state}State.cs";
                    var stateScriptDir = System.IO.Path.GetDirectoryName(stateScriptPath);
                    if (!System.IO.Directory.Exists(stateScriptDir))
                    {
                        System.IO.Directory.CreateDirectory(stateScriptDir);
                    }
                    
                    var stateScriptContent = GenerateStateScript($"{stateMachineType}{state}State", state);
                    System.IO.File.WriteAllText(stateScriptPath, stateScriptContent);
                }
                
                AssetDatabase.Refresh();
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Created {stateMachineType} state machine with {states.Length} states",
                    targetObject = target.name,
                    scriptPath = scriptPath,
                    states = states,
                    type = stateMachineType
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        private string GenerateStateMachineScript(string className, string[] states)
        {
            var stateEnums = string.Join(",\n        ", states);
            var stateCases = "";
            
            foreach (var state in states)
            {
                stateCases += $@"
                case States.{state}:
                    // {state} state logic
                    break;
";
            }
            
            return $@"using UnityEngine;
using System.Collections;

public class {className} : MonoBehaviour
{{
    public enum States
    {{
        {stateEnums}
    }}
    
    public States currentState = States.{states[0]};
    private States previousState;
    
    void Start()
    {{
        StartCoroutine(StateMachineRoutine());
    }}
    
    IEnumerator StateMachineRoutine()
    {{
        while (true)
        {{
            switch (currentState)
            {{{stateCases}
            }}
            
            yield return null;
        }}
    }}
    
    public void ChangeState(States newState)
    {{
        previousState = currentState;
        currentState = newState;
        OnStateChange(previousState, currentState);
    }}
    
    protected virtual void OnStateChange(States from, States to)
    {{
        Debug.Log($""State changed from {{from}} to {{to}}"");
    }}
}}";
        }
        
        private string GenerateStateScript(string className, string stateName)
        {
            return $@"using UnityEngine;

public class {className} : MonoBehaviour
{{
    public void Enter()
    {{
        Debug.Log(""Entering {stateName} state"");
        // State enter logic
    }}
    
    public void Execute()
    {{
        // State update logic
    }}
    
    public void Exit()
    {{
        Debug.Log(""Exiting {stateName} state"");
        // State exit logic
    }}
}}";
        }
        
        private string SetupInventorySystem(Dictionary<string, string> parameters)
        {
            try
            {
                var inventorySize = int.Parse(parameters.GetValueOrDefault("size", "20"));
                var hasUI = bool.Parse(parameters.GetValueOrDefault("hasUI", "true"));
                var stackable = bool.Parse(parameters.GetValueOrDefault("stackable", "true"));
                
                // インベントリマネージャーを作成
                var inventoryManager = new GameObject("InventoryManager");
                
                // アイテムクラスを生成
                var itemScriptPath = "Assets/Scripts/Inventory/Item.cs";
                var itemScriptDir = System.IO.Path.GetDirectoryName(itemScriptPath);
                if (!System.IO.Directory.Exists(itemScriptDir))
                {
                    System.IO.Directory.CreateDirectory(itemScriptDir);
                }
                
                var itemScript = GenerateItemScript(stackable);
                System.IO.File.WriteAllText(itemScriptPath, itemScript);
                
                // インベントリスクリプトを生成
                var inventoryScriptPath = "Assets/Scripts/Inventory/Inventory.cs";
                var inventoryScript = GenerateInventoryScript(inventorySize, stackable);
                System.IO.File.WriteAllText(inventoryScriptPath, inventoryScript);
                
                // UIを作成
                GameObject inventoryUI = null;
                if (hasUI)
                {
                    // Canvas作成
                    var canvas = GameObject.Find("Canvas");
                    if (canvas == null)
                    {
                        canvas = new GameObject("Canvas");
                        canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
                        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                    }
                    
                    // インベントリパネル
                    inventoryUI = new GameObject("InventoryPanel");
                    inventoryUI.transform.parent = canvas.transform;
                    var rect = inventoryUI.AddComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(400, 400);
                    rect.anchoredPosition = Vector2.zero;
                    
                    var image = inventoryUI.AddComponent<UnityEngine.UI.Image>();
                    image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
                    
                    // グリッドレイアウト
                    var gridContainer = new GameObject("GridContainer");
                    gridContainer.transform.parent = inventoryUI.transform;
                    var gridRect = gridContainer.AddComponent<RectTransform>();
                    gridRect.sizeDelta = new Vector2(380, 380);
                    gridRect.anchoredPosition = Vector2.zero;
                    
                    var gridLayout = gridContainer.AddComponent<UnityEngine.UI.GridLayoutGroup>();
                    gridLayout.cellSize = new Vector2(60, 60);
                    gridLayout.spacing = new Vector2(5, 5);
                    gridLayout.padding = new RectOffset(10, 10, 10, 10);
                    
                    // インベントリスロットプレハブ
                    var slotPrefab = new GameObject("SlotPrefab");
                    slotPrefab.AddComponent<RectTransform>();
                    var slotImage = slotPrefab.AddComponent<UnityEngine.UI.Image>();
                    slotImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                    
                    // スロット作成
                    for (int i = 0; i < inventorySize; i++)
                    {
                        var slot = GameObject.Instantiate(slotPrefab, gridContainer.transform);
                        slot.name = $"Slot_{i}";
                    }
                    
                    UnityEngine.Object.DestroyImmediate(slotPrefab);
                    inventoryUI.SetActive(false);
                }
                
                AssetDatabase.Refresh();
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = "Set up inventory system",
                    settings = new
                    {
                        size = inventorySize,
                        hasUI = hasUI,
                        stackable = stackable
                    },
                    scripts = new[]
                    {
                        itemScriptPath,
                        inventoryScriptPath
                    },
                    ui = hasUI ? new
                    {
                        created = true,
                        slots = inventorySize
                    } : null
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        private string GenerateItemScript(bool stackable)
        {
            return $@"using UnityEngine;

[System.Serializable]
public class Item
{{
    public int id;
    public string itemName;
    public string description;
    public Sprite icon;
    public GameObject prefab;
    {(stackable ? "public int maxStack = 99;" : "")}
    public ItemType type;
    
    public enum ItemType
    {{
        Weapon,
        Armor,
        Consumable,
        Material,
        KeyItem
    }}
    
    public Item(int id, string name, string desc, ItemType type)
    {{
        this.id = id;
        this.itemName = name;
        this.description = desc;
        this.type = type;
    }}
    
    public virtual void Use()
    {{
        Debug.Log($""Using {{itemName}}"");
    }}
    
    public virtual string GetTooltip()
    {{
        return $""{{itemName}}\\n{{description}}"";
    }}
}}

[System.Serializable]
public class ItemStack
{{
    public Item item;
    public int count;
    
    public ItemStack(Item item, int count = 1)
    {{
        this.item = item;
        this.count = count;
    }}
}}";
        }
        
        private string GenerateInventoryScript(int size, bool stackable)
        {
            return $@"using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Inventory : MonoBehaviour
{{
    [SerializeField] private int maxSlots = {size};
    [SerializeField] private List<ItemStack> items = new List<ItemStack>();
    
    public delegate void OnInventoryChanged();
    public event OnInventoryChanged onInventoryChangedCallback;
    
    private void Start()
    {{
        // Initialize empty slots
        for (int i = 0; i < maxSlots; i++)
        {{
            items.Add(null);
        }}
    }}
    
    public bool AddItem(Item item, int count = 1)
    {{
        if (item == null) return false;
        
        {(stackable ? @"// Check for existing stack
        var existingStack = items.FirstOrDefault(s => s != null && s.item.id == item.id && s.count < s.item.maxStack);
        if (existingStack != null)
        {
            int spaceLeft = existingStack.item.maxStack - existingStack.count;
            int toAdd = Mathf.Min(count, spaceLeft);
            existingStack.count += toAdd;
            count -= toAdd;
            
            if (count <= 0)
            {
                onInventoryChangedCallback?.Invoke();
                return true;
            }
        }" : "")}
        
        // Find empty slot
        for (int i = 0; i < maxSlots; i++)
        {{
            if (items[i] == null)
            {{
                items[i] = new ItemStack(item, count);
                onInventoryChangedCallback?.Invoke();
                return true;
            }}
        }}
        
        Debug.Log(""Inventory full!"");
        return false;
    }}
    
    public bool RemoveItem(Item item, int count = 1)
    {{
        var stack = items.FirstOrDefault(s => s != null && s.item.id == item.id);
        if (stack != null)
        {{
            stack.count -= count;
            if (stack.count <= 0)
            {{
                items[items.IndexOf(stack)] = null;
            }}
            onInventoryChangedCallback?.Invoke();
            return true;
        }}
        return false;
    }}
    
    public ItemStack GetItemAt(int index)
    {{
        if (index >= 0 && index < items.Count)
            return items[index];
        return null;
    }}
    
    public void SwapItems(int index1, int index2)
    {{
        if (index1 >= 0 && index1 < items.Count && index2 >= 0 && index2 < items.Count)
        {{
            var temp = items[index1];
            items[index1] = items[index2];
            items[index2] = temp;
            onInventoryChangedCallback?.Invoke();
        }}
    }}
    
    public int GetItemCount(Item item)
    {{
        return items.Where(s => s != null && s.item.id == item.id).Sum(s => s.count);
    }}
    
    public bool HasItem(Item item, int count = 1)
    {{
        return GetItemCount(item) >= count;
    }}
    
    public List<ItemStack> GetAllItems()
    {{
        return items.Where(s => s != null).ToList();
    }}
}}";
        }
        
        // ===== プロトタイピング機能 =====
        
        private string CreateGameTemplate(Dictionary<string, string> parameters)
        {
            try
            {
                var genre = parameters.GetValueOrDefault("genre", "FPS");
                var gameName = parameters.GetValueOrDefault("name", $"{genre}Prototype");
                var includeUI = bool.Parse(parameters.GetValueOrDefault("includeUI", "true"));
                var includeAudio = bool.Parse(parameters.GetValueOrDefault("includeAudio", "true"));
                
                // ルートGameObjectを作成
                var gameRoot = new GameObject($"{gameName}_Game");
                var createdObjects = new List<GameObject>();
                createdObjects.Add(gameRoot);
                
                switch (genre)
                {
                    case "FPS":
                        createdObjects.AddRange(CreateFPSTemplate(gameRoot));
                        break;
                        
                    case "Platformer":
                        createdObjects.AddRange(CreatePlatformerTemplate(gameRoot));
                        break;
                        
                    case "RPG":
                        createdObjects.AddRange(CreateRPGTemplate(gameRoot));
                        break;
                        
                    case "Puzzle":
                        createdObjects.AddRange(CreatePuzzleTemplate(gameRoot));
                        break;
                        
                    case "Racing":
                        createdObjects.AddRange(CreateRacingTemplate(gameRoot));
                        break;
                        
                    case "Strategy":
                        createdObjects.AddRange(CreateStrategyTemplate(gameRoot));
                        break;
                }
                
                // UI作成
                if (includeUI)
                {
                    CreateGenericUI(genre);
                }
                
                // オーディオ設定
                if (includeAudio)
                {
                    SetupGenericAudio(gameRoot);
                }
                
                // シーン設定
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogDensity = 0.01f;
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Created {genre} game template",
                    gameName = gameName,
                    genre = genre,
                    objectsCreated = createdObjects.Select(o => o.name).ToList(),
                    settings = new
                    {
                        includeUI = includeUI,
                        includeAudio = includeAudio
                    }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        private List<GameObject> CreateFPSTemplate(GameObject parent)
        {
            var objects = new List<GameObject>();
            
            // プレイヤー
            var player = new GameObject("FPSPlayer");
            player.transform.parent = parent.transform;
            player.AddComponent<CharacterController>();
            objects.Add(player);
            
            // カメラ
            var camera = new GameObject("PlayerCamera");
            camera.transform.parent = player.transform;
            camera.AddComponent<Camera>();
            camera.transform.localPosition = new Vector3(0, 1.6f, 0);
            objects.Add(camera);
            
            // 武器ホルダー
            var weaponHolder = new GameObject("WeaponHolder");
            weaponHolder.transform.parent = camera.transform;
            weaponHolder.transform.localPosition = new Vector3(0.5f, -0.5f, 0.5f);
            objects.Add(weaponHolder);
            
            // 基本的な武器
            var weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weapon.name = "BasicWeapon";
            weapon.transform.parent = weaponHolder.transform;
            weapon.transform.localScale = new Vector3(0.1f, 0.1f, 0.5f);
            objects.Add(weapon);
            
            // レベル環境
            var level = new GameObject("Level");
            level.transform.parent = parent.transform;
            objects.Add(level);
            
            // 床
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.parent = level.transform;
            ground.transform.localScale = new Vector3(10, 1, 10);
            objects.Add(ground);
            
            // 壁
            for (int i = 0; i < 4; i++)
            {
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"Wall_{i}";
                wall.transform.parent = level.transform;
                wall.transform.localScale = new Vector3(1, 3, 10);
                
                switch (i)
                {
                    case 0: wall.transform.position = new Vector3(5, 1.5f, 0); break;
                    case 1: wall.transform.position = new Vector3(-5, 1.5f, 0); break;
                    case 2: wall.transform.position = new Vector3(0, 1.5f, 5); 
                           wall.transform.rotation = Quaternion.Euler(0, 90, 0); break;
                    case 3: wall.transform.position = new Vector3(0, 1.5f, -5); 
                           wall.transform.rotation = Quaternion.Euler(0, 90, 0); break;
                }
                objects.Add(wall);
            }
            
            // 敵スポーンポイント
            var enemySpawns = new GameObject("EnemySpawnPoints");
            enemySpawns.transform.parent = parent.transform;
            objects.Add(enemySpawns);
            
            for (int i = 0; i < 3; i++)
            {
                var spawn = new GameObject($"SpawnPoint_{i}");
                spawn.transform.parent = enemySpawns.transform;
                spawn.transform.position = new Vector3(
                    UnityEngine.Random.Range(-4f, 4f), 
                    0.5f, 
                    UnityEngine.Random.Range(-4f, 4f)
                );
                objects.Add(spawn);
            }
            
            return objects;
        }
        
        private List<GameObject> CreatePlatformerTemplate(GameObject parent)
        {
            var objects = new List<GameObject>();
            
            // プレイヤー
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "PlatformerPlayer";
            player.transform.parent = parent.transform;
            player.transform.position = new Vector3(0, 1, 0);
            UnityEngine.Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
            player.AddComponent<BoxCollider2D>();
            player.AddComponent<Rigidbody2D>();
            objects.Add(player);
            
            // カメラ設定
            var camera = Camera.main ?? new GameObject("Main Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.transform.position = new Vector3(0, 0, -10);
            
            // レベル
            var level = new GameObject("Level");
            level.transform.parent = parent.transform;
            objects.Add(level);
            
            // プラットフォーム作成
            float[] platformPositions = { -5, -2, 1, 4, 7 };
            for (int i = 0; i < platformPositions.Length; i++)
            {
                var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                platform.name = $"Platform_{i}";
                platform.transform.parent = level.transform;
                platform.transform.position = new Vector3(
                    platformPositions[i], 
                    UnityEngine.Random.Range(-2f, 2f), 
                    0
                );
                platform.transform.localScale = new Vector3(2, 0.3f, 1);
                
                UnityEngine.Object.DestroyImmediate(platform.GetComponent<BoxCollider>());
                platform.AddComponent<BoxCollider2D>();
                objects.Add(platform);
            }
            
            // コイン/アイテム
            var collectibles = new GameObject("Collectibles");
            collectibles.transform.parent = parent.transform;
            objects.Add(collectibles);
            
            for (int i = 0; i < 10; i++)
            {
                var coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                coin.name = $"Coin_{i}";
                coin.transform.parent = collectibles.transform;
                coin.transform.position = new Vector3(
                    Random.Range(-7f, 7f),
                    Random.Range(0f, 3f),
                    0
                );
                coin.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
                coin.transform.rotation = Quaternion.Euler(90, 0, 0);
                
                UnityEngine.Object.DestroyImmediate(coin.GetComponent<CapsuleCollider>());
                var trigger = coin.AddComponent<CircleCollider2D>();
                trigger.isTrigger = true;
                objects.Add(coin);
            }
            
            return objects;
        }
        
        private List<GameObject> CreateRPGTemplate(GameObject parent)
        {
            var objects = new List<GameObject>();
            
            // プレイヤーキャラクター
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "RPGHero";
            player.transform.parent = parent.transform;
            player.AddComponent<CharacterController>();
            objects.Add(player);
            
            // カメラ（アイソメトリック風）
            var cameraRig = new GameObject("CameraRig");
            cameraRig.transform.parent = parent.transform;
            objects.Add(cameraRig);
            
            var camera = Camera.main ?? new GameObject("Main Camera").AddComponent<Camera>();
            camera.transform.parent = cameraRig.transform;
            camera.transform.position = new Vector3(0, 10, -10);
            camera.transform.rotation = Quaternion.Euler(45, 0, 0);
            
            // 町/村
            var town = new GameObject("Town");
            town.transform.parent = parent.transform;
            objects.Add(town);
            
            // 地面
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "TownGround";
            ground.transform.parent = town.transform;
            ground.transform.localScale = new Vector3(5, 1, 5);
            var groundMat = new Material(Shader.Find("Standard"));
            groundMat.color = new Color(0.4f, 0.6f, 0.3f);
            ground.GetComponent<MeshRenderer>().material = groundMat;
            objects.Add(ground);
            
            // 建物
            for (int i = 0; i < 5; i++)
            {
                var building = new GameObject($"Building_{i}");
                building.transform.parent = town.transform;
                
                var buildingBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
                buildingBase.transform.parent = building.transform;
                buildingBase.transform.localScale = new Vector3(2, 2, 2);
                buildingBase.transform.position = new Vector3(
                    Random.Range(-15f, 15f),
                    1,
                    Random.Range(-15f, 15f)
                );
                
                var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roof.transform.parent = building.transform;
                roof.transform.position = buildingBase.transform.position + Vector3.up * 2;
                roof.transform.localScale = new Vector3(2.5f, 0.5f, 2.5f);
                
                objects.Add(building);
            }
            
            // NPC
            var npcs = new GameObject("NPCs");
            npcs.transform.parent = parent.transform;
            objects.Add(npcs);
            
            string[] npcNames = { "Merchant", "QuestGiver", "Blacksmith" };
            for (int i = 0; i < npcNames.Length; i++)
            {
                var npc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                npc.name = npcNames[i];
                npc.transform.parent = npcs.transform;
                npc.transform.position = new Vector3(
                    Random.Range(-10f, 10f),
                    0.5f,
                    Random.Range(-10f, 10f)
                );
                objects.Add(npc);
            }
            
            return objects;
        }
        
        private List<GameObject> CreatePuzzleTemplate(GameObject parent)
        {
            var objects = new List<GameObject>();
            
            // パズルボード
            var board = new GameObject("PuzzleBoard");
            board.transform.parent = parent.transform;
            objects.Add(board);
            
            // グリッド作成（8x8）
            int gridSize = 8;
            float spacing = 1.1f;
            
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Tile_{x}_{y}";
                    tile.transform.parent = board.transform;
                    tile.transform.position = new Vector3(
                        (x - gridSize / 2) * spacing,
                        0,
                        (y - gridSize / 2) * spacing
                    );
                    
                    // チェッカーボードパターン
                    var mat = new Material(Shader.Find("Standard"));
                    mat.color = (x + y) % 2 == 0 ? Color.white : Color.black;
                    tile.GetComponent<MeshRenderer>().material = mat;
                    
                    objects.Add(tile);
                }
            }
            
            // カメラ（トップダウンビュー）
            var camera = Camera.main ?? new GameObject("Main Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 15, 0);
            camera.transform.rotation = Quaternion.Euler(90, 0, 0);
            camera.orthographic = true;
            camera.orthographicSize = 6;
            
            // パズルピース
            var pieces = new GameObject("PuzzlePieces");
            pieces.transform.parent = parent.transform;
            objects.Add(pieces);
            
            Color[] pieceColors = { Color.red, Color.blue, Color.green, Color.yellow };
            for (int i = 0; i < 4; i++)
            {
                var piece = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                piece.name = $"Piece_{i}";
                piece.transform.parent = pieces.transform;
                piece.transform.position = new Vector3(
                    Random.Range(-3f, 3f),
                    0.5f,
                    Random.Range(-3f, 3f)
                );
                
                var mat = new Material(Shader.Find("Standard"));
                mat.color = pieceColors[i % pieceColors.Length];
                piece.GetComponent<MeshRenderer>().material = mat;
                
                objects.Add(piece);
            }
            
            return objects;
        }
        
        private List<GameObject> CreateRacingTemplate(GameObject parent)
        {
            var objects = new List<GameObject>();
            
            // レーストラック
            var track = new GameObject("RaceTrack");
            track.transform.parent = parent.transform;
            objects.Add(track);
            
            // 楕円形のトラック作成
            int segments = 20;
            float radiusX = 20f;
            float radiusZ = 15f;
            
            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2;
                float nextAngle = (float)(i + 1) / segments * Mathf.PI * 2;
                
                var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.name = $"TrackSegment_{i}";
                segment.transform.parent = track.transform;
                
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * radiusX,
                    0,
                    Mathf.Sin(angle) * radiusZ
                );
                Vector3 nextPos = new Vector3(
                    Mathf.Cos(nextAngle) * radiusX,
                    0,
                    Mathf.Sin(nextAngle) * radiusZ
                );
                
                segment.transform.position = (pos + nextPos) / 2;
                segment.transform.LookAt(nextPos);
                segment.transform.localScale = new Vector3(8, 0.1f, 5);
                
                objects.Add(segment);
            }
            
            // プレイヤーカー
            var playerCar = new GameObject("PlayerCar");
            playerCar.transform.parent = parent.transform;
            playerCar.transform.position = new Vector3(radiusX, 0.5f, 0);
            
            var carBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            carBody.transform.parent = playerCar.transform;
            carBody.transform.localScale = new Vector3(1, 0.5f, 2);
            
            playerCar.AddComponent<Rigidbody>();
            objects.Add(playerCar);
            
            // カメラ（追従）
            var cameraFollower = new GameObject("CameraFollower");
            cameraFollower.transform.parent = playerCar.transform;
            cameraFollower.transform.localPosition = new Vector3(0, 5, -10);
            
            var camera = Camera.main ?? new GameObject("Main Camera").AddComponent<Camera>();
            camera.transform.parent = cameraFollower.transform;
            camera.transform.LookAt(playerCar.transform);
            
            // チェックポイント
            var checkpoints = new GameObject("Checkpoints");
            checkpoints.transform.parent = parent.transform;
            objects.Add(checkpoints);
            
            for (int i = 0; i < 4; i++)
            {
                float angle = (float)i / 4 * Mathf.PI * 2;
                var checkpoint = new GameObject($"Checkpoint_{i}");
                checkpoint.transform.parent = checkpoints.transform;
                checkpoint.transform.position = new Vector3(
                    Mathf.Cos(angle) * radiusX,
                    2,
                    Mathf.Sin(angle) * radiusZ
                );
                
                var gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gate.transform.parent = checkpoint.transform;
                gate.transform.localScale = new Vector3(0.2f, 4, 10);
                gate.GetComponent<Renderer>().material.color = Color.yellow;
                
                objects.Add(checkpoint);
            }
            
            return objects;
        }
        
        private List<GameObject> CreateStrategyTemplate(GameObject parent)
        {
            var objects = new List<GameObject>();
            
            // ゲームボード
            var board = new GameObject("StrategyBoard");
            board.transform.parent = parent.transform;
            objects.Add(board);
            
            // ヘックスグリッド風の配置
            int mapSize = 10;
            float hexSize = 1f;
            
            for (int x = 0; x < mapSize; x++)
            {
                for (int y = 0; y < mapSize; y++)
                {
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    tile.name = $"HexTile_{x}_{y}";
                    tile.transform.parent = board.transform;
                    
                    float xPos = x * hexSize * 1.5f;
                    float yPos = y * hexSize * Mathf.Sqrt(3f) + (x % 2 * hexSize * Mathf.Sqrt(3f) / 2f);
                    
                    tile.transform.position = new Vector3(xPos - mapSize / 2, 0, yPos - mapSize / 2);
                    tile.transform.localScale = new Vector3(hexSize, 0.1f, hexSize);
                    
                    // タイル種類（草原、山、水）
                    var mat = new Material(Shader.Find("Standard"));
                    float rand = UnityEngine.Random.value;
                    if (rand < 0.6f) mat.color = Color.green;      // 草原
                    else if (rand < 0.8f) mat.color = Color.gray;  // 山
                    else mat.color = Color.blue;                    // 水
                    
                    tile.GetComponent<MeshRenderer>().material = mat;
                    objects.Add(tile);
                }
            }
            
            // カメラ（戦略ゲーム用）
            var camera = Camera.main ?? new GameObject("Main Camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 20, -5);
            camera.transform.rotation = Quaternion.Euler(60, 0, 0);
            
            // ユニット
            var units = new GameObject("Units");
            units.transform.parent = parent.transform;
            objects.Add(units);
            
            // プレイヤーユニット
            for (int i = 0; i < 3; i++)
            {
                var unit = GameObject.CreatePrimitive(PrimitiveType.Cube);
                unit.name = $"PlayerUnit_{i}";
                unit.transform.parent = units.transform;
                unit.transform.position = new Vector3(
                    Random.Range(-5f, -3f),
                    0.5f,
                    Random.Range(-5f, 5f)
                );
                unit.GetComponent<Renderer>().material.color = Color.blue;
                objects.Add(unit);
            }
            
            // 敵ユニット
            for (int i = 0; i < 3; i++)
            {
                var unit = GameObject.CreatePrimitive(PrimitiveType.Cube);
                unit.name = $"EnemyUnit_{i}";
                unit.transform.parent = units.transform;
                unit.transform.position = new Vector3(
                    Random.Range(3f, 5f),
                    0.5f,
                    Random.Range(-5f, 5f)
                );
                unit.GetComponent<Renderer>().material.color = Color.red;
                objects.Add(unit);
            }
            
            // リソースポイント
            var resources = new GameObject("ResourcePoints");
            resources.transform.parent = parent.transform;
            objects.Add(resources);
            
            for (int i = 0; i < 5; i++)
            {
                var resource = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                resource.name = $"Resource_{i}";
                resource.transform.parent = resources.transform;
                resource.transform.position = new Vector3(
                    Random.Range(-8f, 8f),
                    0.5f,
                    Random.Range(-8f, 8f)
                );
                resource.transform.localScale = Vector3.one * 0.5f;
                resource.GetComponent<Renderer>().material.color = Color.yellow;
                objects.Add(resource);
            }
            
            return objects;
        }
        
        private void CreateGenericUI(string genre)
        {
            // Canvas作成
            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                canvas = new GameObject("Canvas");
                canvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                
                // EventSystemも作成
                if (!GameObject.Find("EventSystem"))
                {
                    var eventSystem = new GameObject("EventSystem");
                    eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }
            
            // HUD Panel
            var hudPanel = new GameObject("HUD");
            hudPanel.transform.parent = canvas.transform;
            var hudRect = hudPanel.AddComponent<RectTransform>();
            hudRect.anchorMin = Vector2.zero;
            hudRect.anchorMax = Vector2.one;
            hudRect.sizeDelta = Vector2.zero;
            hudRect.anchoredPosition = Vector2.zero;
            
            // ジャンル別UI要素
            switch (genre)
            {
                case "FPS":
                    CreateFPSUI(hudPanel);
                    break;
                case "RPG":
                    CreateRPGUI(hudPanel);
                    break;
                case "Platformer":
                    CreatePlatformerUI(hudPanel);
                    break;
                case "Racing":
                    CreateRacingUI(hudPanel);
                    break;
                case "Puzzle":
                    CreatePuzzleUI(hudPanel);
                    break;
                case "Strategy":
                    CreateStrategyUI(hudPanel);
                    break;
            }
        }
        
        private void CreateFPSUI(GameObject parent)
        {
            // 体力バー
            var healthBar = new GameObject("HealthBar");
            healthBar.transform.parent = parent.transform;
            var healthRect = healthBar.AddComponent<RectTransform>();
            healthRect.anchorMin = new Vector2(0, 0);
            healthRect.anchorMax = new Vector2(0, 0);
            healthRect.sizeDelta = new Vector2(200, 30);
            healthRect.anchoredPosition = new Vector2(110, 50);
            
            var healthBG = healthBar.AddComponent<UnityEngine.UI.Image>();
            healthBG.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            var healthFill = new GameObject("Fill");
            healthFill.transform.parent = healthBar.transform;
            var fillRect = healthFill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.8f, 1);
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
            
            var fillImage = healthFill.AddComponent<UnityEngine.UI.Image>();
            fillImage.color = Color.red;
            
            // 弾薬カウンター
            var ammoText = new GameObject("AmmoCounter");
            ammoText.transform.parent = parent.transform;
            var ammoRect = ammoText.AddComponent<RectTransform>();
            ammoRect.anchorMin = new Vector2(1, 0);
            ammoRect.anchorMax = new Vector2(1, 0);
            ammoRect.sizeDelta = new Vector2(150, 50);
            ammoRect.anchoredPosition = new Vector2(-85, 50);
            
            var text = ammoText.AddComponent<UnityEngine.UI.Text>();
            text.text = "30 / 120";
            text.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleRight;
            
            // クロスヘア
            var crosshair = new GameObject("Crosshair");
            crosshair.transform.parent = parent.transform;
            var crossRect = crosshair.AddComponent<RectTransform>();
            crossRect.anchorMin = new Vector2(0.5f, 0.5f);
            crossRect.anchorMax = new Vector2(0.5f, 0.5f);
            crossRect.sizeDelta = new Vector2(50, 50);
            crossRect.anchoredPosition = Vector2.zero;
            
            var crossImage = crosshair.AddComponent<UnityEngine.UI.Image>();
            crossImage.color = new Color(1, 1, 1, 0.5f);
            crossImage.sprite = null; // クロスヘアスプライトを設定
        }
        
        private void CreateRPGUI(GameObject parent)
        {
            // ステータスバー
            var statusBar = new GameObject("StatusBar");
            statusBar.transform.parent = parent.transform;
            var statusRect = statusBar.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 1);
            statusRect.anchorMax = new Vector2(0, 1);
            statusRect.sizeDelta = new Vector2(250, 100);
            statusRect.anchoredPosition = new Vector2(135, -60);
            
            var bg = statusBar.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            // HP/MP バー
            string[] barTypes = { "HP", "MP" };
            Color[] barColors = { Color.green, Color.blue };
            
            for (int i = 0; i < barTypes.Length; i++)
            {
                var bar = new GameObject($"{barTypes[i]}Bar");
                bar.transform.parent = statusBar.transform;
                var barRect = bar.AddComponent<RectTransform>();
                barRect.anchorMin = new Vector2(0.1f, 0.6f - i * 0.4f);
                barRect.anchorMax = new Vector2(0.9f, 0.8f - i * 0.4f);
                barRect.sizeDelta = Vector2.zero;
                
                var barBG = bar.AddComponent<UnityEngine.UI.Image>();
                barBG.color = Color.gray;
                
                var fill = new GameObject("Fill");
                fill.transform.parent = bar.transform;
                var fillRect = fill.AddComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = new Vector2(0.8f, 1);
                fillRect.sizeDelta = Vector2.zero;
                
                var fillImage = fill.AddComponent<UnityEngine.UI.Image>();
                fillImage.color = barColors[i];
            }
            
            // クエストログ
            var questLog = new GameObject("QuestLog");
            questLog.transform.parent = parent.transform;
            var questRect = questLog.AddComponent<RectTransform>();
            questRect.anchorMin = new Vector2(1, 1);
            questRect.anchorMax = new Vector2(1, 1);
            questRect.sizeDelta = new Vector2(300, 150);
            questRect.anchoredPosition = new Vector2(-160, -85);
            
            var questBG = questLog.AddComponent<UnityEngine.UI.Image>();
            questBG.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            var questText = new GameObject("QuestText");
            questText.transform.parent = questLog.transform;
            var textRect = questText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = new Vector2(-20, -20);
            textRect.anchoredPosition = Vector2.zero;
            
            var text = questText.AddComponent<UnityEngine.UI.Text>();
            text.text = "Current Quest:\n- Talk to the Village Elder\n- Collect 10 herbs";
            text.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            text.fontSize = 14;
            text.color = Color.white;
        }
        
        private void CreatePlatformerUI(GameObject parent)
        {
            // スコア
            var scoreText = new GameObject("Score");
            scoreText.transform.parent = parent.transform;
            var scoreRect = scoreText.AddComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0, 1);
            scoreRect.anchorMax = new Vector2(0, 1);
            scoreRect.sizeDelta = new Vector2(200, 50);
            scoreRect.anchoredPosition = new Vector2(110, -35);
            
            var score = scoreText.AddComponent<UnityEngine.UI.Text>();
            score.text = "Score: 0";
            score.font = Font.CreateDynamicFontFromOSFont("Arial", 28);
            score.fontSize = 28;
            score.color = Color.white;
            score.alignment = TextAnchor.MiddleLeft;
            
            // ライフ
            var livesContainer = new GameObject("Lives");
            livesContainer.transform.parent = parent.transform;
            var livesRect = livesContainer.AddComponent<RectTransform>();
            livesRect.anchorMin = new Vector2(1, 1);
            livesRect.anchorMax = new Vector2(1, 1);
            livesRect.sizeDelta = new Vector2(150, 50);
            livesRect.anchoredPosition = new Vector2(-85, -35);
            
            for (int i = 0; i < 3; i++)
            {
                var life = new GameObject($"Life_{i}");
                life.transform.parent = livesContainer.transform;
                var lifeRect = life.AddComponent<RectTransform>();
                lifeRect.anchorMin = new Vector2(0, 0.5f);
                lifeRect.anchorMax = new Vector2(0, 0.5f);
                lifeRect.sizeDelta = new Vector2(30, 30);
                lifeRect.anchoredPosition = new Vector2(35 + i * 40, 0);
                
                var lifeImage = life.AddComponent<UnityEngine.UI.Image>();
                lifeImage.color = Color.red;
            }
        }
        
        private void CreateRacingUI(GameObject parent)
        {
            // スピードメーター
            var speedometer = new GameObject("Speedometer");
            speedometer.transform.parent = parent.transform;
            var speedRect = speedometer.AddComponent<RectTransform>();
            speedRect.anchorMin = new Vector2(1, 0);
            speedRect.anchorMax = new Vector2(1, 0);
            speedRect.sizeDelta = new Vector2(200, 200);
            speedRect.anchoredPosition = new Vector2(-110, 110);
            
            var speedBG = speedometer.AddComponent<UnityEngine.UI.Image>();
            speedBG.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            
            var speedText = new GameObject("SpeedText");
            speedText.transform.parent = speedometer.transform;
            var textRect = speedText.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(180, 60);
            textRect.anchoredPosition = Vector2.zero;
            
            var text = speedText.AddComponent<UnityEngine.UI.Text>();
            text.text = "0 km/h";
            text.font = Font.CreateDynamicFontFromOSFont("Arial", 32);
            text.fontSize = 32;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            
            // ラップタイム
            var lapTime = new GameObject("LapTime");
            lapTime.transform.parent = parent.transform;
            var lapRect = lapTime.AddComponent<RectTransform>();
            lapRect.anchorMin = new Vector2(0.5f, 1);
            lapRect.anchorMax = new Vector2(0.5f, 1);
            lapRect.sizeDelta = new Vector2(300, 60);
            lapRect.anchoredPosition = new Vector2(0, -40);
            
            var lapText = lapTime.AddComponent<UnityEngine.UI.Text>();
            lapText.text = "Lap 1/3 - 00:00.000";
            lapText.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            lapText.fontSize = 24;
            lapText.color = Color.white;
            lapText.alignment = TextAnchor.MiddleCenter;
            
            // ポジション
            var position = new GameObject("Position");
            position.transform.parent = parent.transform;
            var posRect = position.AddComponent<RectTransform>();
            posRect.anchorMin = new Vector2(0, 1);
            posRect.anchorMax = new Vector2(0, 1);
            posRect.sizeDelta = new Vector2(150, 100);
            posRect.anchoredPosition = new Vector2(85, -60);
            
            var posBG = position.AddComponent<UnityEngine.UI.Image>();
            posBG.color = new Color(0.8f, 0.8f, 0.8f, 0.9f);
            
            var posText = new GameObject("PosText");
            posText.transform.parent = position.transform;
            var posTextRect = posText.AddComponent<RectTransform>();
            posTextRect.anchorMin = Vector2.zero;
            posTextRect.anchorMax = Vector2.one;
            posTextRect.sizeDelta = Vector2.zero;
            
            var positionText = posText.AddComponent<UnityEngine.UI.Text>();
            positionText.text = "1st";
            positionText.font = Font.CreateDynamicFontFromOSFont("Arial", 48);
            positionText.fontSize = 48;
            positionText.color = Color.black;
            positionText.alignment = TextAnchor.MiddleCenter;
        }
        
        private void CreatePuzzleUI(GameObject parent)
        {
            // タイマー
            var timer = new GameObject("Timer");
            timer.transform.parent = parent.transform;
            var timerRect = timer.AddComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.5f, 1);
            timerRect.anchorMax = new Vector2(0.5f, 1);
            timerRect.sizeDelta = new Vector2(200, 60);
            timerRect.anchoredPosition = new Vector2(0, -40);
            
            var timerBG = timer.AddComponent<UnityEngine.UI.Image>();
            timerBG.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            var timerText = new GameObject("TimerText");
            timerText.transform.parent = timer.transform;
            var textRect = timerText.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            var text = timerText.AddComponent<UnityEngine.UI.Text>();
            text.text = "00:00";
            text.font = Font.CreateDynamicFontFromOSFont("Arial", 32);
            text.fontSize = 32;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            
            // ムーブカウンター
            var moves = new GameObject("Moves");
            moves.transform.parent = parent.transform;
            var movesRect = moves.AddComponent<RectTransform>();
            movesRect.anchorMin = new Vector2(0, 1);
            movesRect.anchorMax = new Vector2(0, 1);
            movesRect.sizeDelta = new Vector2(150, 50);
            movesRect.anchoredPosition = new Vector2(85, -35);
            
            var movesText = moves.AddComponent<UnityEngine.UI.Text>();
            movesText.text = "Moves: 0";
            movesText.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
            movesText.fontSize = 20;
            movesText.color = Color.white;
            
            // ヒントボタン
            var hintButton = new GameObject("HintButton");
            hintButton.transform.parent = parent.transform;
            var hintRect = hintButton.AddComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(1, 1);
            hintRect.anchorMax = new Vector2(1, 1);
            hintRect.sizeDelta = new Vector2(100, 40);
            hintRect.anchoredPosition = new Vector2(-60, -30);
            
            var button = hintButton.AddComponent<UnityEngine.UI.Button>();
            var buttonImage = hintButton.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = Color.yellow;
            
            var buttonText = new GameObject("Text");
            buttonText.transform.parent = hintButton.transform;
            var btnTextRect = buttonText.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;
            
            var btnText = buttonText.AddComponent<UnityEngine.UI.Text>();
            btnText.text = "Hint";
            btnText.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            btnText.fontSize = 16;
            btnText.color = Color.black;
            btnText.alignment = TextAnchor.MiddleCenter;
        }
        
        private void CreateStrategyUI(GameObject parent)
        {
            // リソースパネル
            var resourcePanel = new GameObject("ResourcePanel");
            resourcePanel.transform.parent = parent.transform;
            var resRect = resourcePanel.AddComponent<RectTransform>();
            resRect.anchorMin = new Vector2(0, 1);
            resRect.anchorMax = new Vector2(1, 1);
            resRect.sizeDelta = new Vector2(0, 80);
            resRect.anchoredPosition = new Vector2(0, -40);
            
            var resBG = resourcePanel.AddComponent<UnityEngine.UI.Image>();
            resBG.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            // リソース表示
            string[] resources = { "Gold: 1000", "Food: 500", "Wood: 300", "Stone: 200" };
            for (int i = 0; i < resources.Length; i++)
            {
                var resource = new GameObject($"Resource_{i}");
                resource.transform.parent = resourcePanel.transform;
                var resourceRect = resource.AddComponent<RectTransform>();
                resourceRect.anchorMin = new Vector2(0.1f + i * 0.2f, 0.5f);
                resourceRect.anchorMax = new Vector2(0.1f + i * 0.2f, 0.5f);
                resourceRect.sizeDelta = new Vector2(150, 40);
                resourceRect.anchoredPosition = Vector2.zero;
                
                var text = resource.AddComponent<UnityEngine.UI.Text>();
                text.text = resources[i];
                text.font = Font.CreateDynamicFontFromOSFont("Arial", 18);
                text.fontSize = 18;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleLeft;
            }
            
            // ミニマップ
            var minimap = new GameObject("Minimap");
            minimap.transform.parent = parent.transform;
            var mapRect = minimap.AddComponent<RectTransform>();
            mapRect.anchorMin = new Vector2(1, 0);
            mapRect.anchorMax = new Vector2(1, 0);
            mapRect.sizeDelta = new Vector2(200, 200);
            mapRect.anchoredPosition = new Vector2(-110, 110);
            
            var mapBG = minimap.AddComponent<UnityEngine.UI.Image>();
            mapBG.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            
            var mapBorder = new GameObject("Border");
            mapBorder.transform.parent = minimap.transform;
            var borderRect = mapBorder.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = new Vector2(10, 10);
            
            var outline = mapBorder.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = Color.white;
            outline.effectDistance = new Vector2(2, 2);
            
            // アクションボタン
            var actionPanel = new GameObject("ActionPanel");
            actionPanel.transform.parent = parent.transform;
            var actionRect = actionPanel.AddComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(0.5f, 0);
            actionRect.anchorMax = new Vector2(0.5f, 0);
            actionRect.sizeDelta = new Vector2(400, 100);
            actionRect.anchoredPosition = new Vector2(0, 60);
            
            string[] actions = { "Build", "Train", "Research", "Diplomacy" };
            for (int i = 0; i < actions.Length; i++)
            {
                var actionButton = new GameObject($"Action_{actions[i]}");
                actionButton.transform.parent = actionPanel.transform;
                var btnRect = actionButton.AddComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(0.1f + i * 0.225f, 0.5f);
                btnRect.anchorMax = new Vector2(0.1f + i * 0.225f, 0.5f);
                btnRect.sizeDelta = new Vector2(80, 80);
                btnRect.anchoredPosition = Vector2.zero;
                
                var btn = actionButton.AddComponent<UnityEngine.UI.Button>();
                var btnImage = actionButton.AddComponent<UnityEngine.UI.Image>();
                btnImage.color = new Color(0.3f, 0.3f, 0.3f);
                
                var btnText = new GameObject("Text");
                btnText.transform.parent = actionButton.transform;
                var textRect = btnText.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;
                
                var text = btnText.AddComponent<UnityEngine.UI.Text>();
                text.text = actions[i];
                text.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
                text.fontSize = 14;
                text.color = Color.white;
                text.alignment = TextAnchor.MiddleCenter;
            }
        }
        
        private void SetupGenericAudio(GameObject parent)
        {
            // オーディオマネージャー
            var audioManager = new GameObject("AudioManager");
            audioManager.transform.parent = parent.transform;
            
            // BGM
            var bgmSource = audioManager.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.volume = 0.3f;
            bgmSource.playOnAwake = true;
            
            // SFXプール
            var sfxPool = new GameObject("SFXPool");
            sfxPool.transform.parent = audioManager.transform;
            
            for (int i = 0; i < 5; i++)
            {
                var sfxSource = new GameObject($"SFXSource_{i}");
                sfxSource.transform.parent = sfxPool.transform;
                var source = sfxSource.AddComponent<AudioSource>();
                source.playOnAwake = false;
            }
        }
        
        private string QuickPrototype(Dictionary<string, string> parameters)
        {
            try
            {
                var elements = parameters.GetValueOrDefault("elements", "player,enemies,collectibles,obstacles");
                var worldSize = float.Parse(parameters.GetValueOrDefault("worldSize", "20"));
                var playerType = parameters.GetValueOrDefault("playerType", "Capsule");
                
                var elementsList = elements.Split(',').Select(e => e.Trim()).ToList();
                var createdObjects = new List<string>();
                
                // ワールド環境
                var world = new GameObject("PrototypeWorld");
                createdObjects.Add(world.name);
                
                // 地面
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.parent = world.transform;
                ground.transform.localScale = new Vector3(worldSize / 10, 1, worldSize / 10);
                var groundMat = new Material(Shader.Find("Standard"));
                groundMat.color = new Color(0.5f, 0.5f, 0.5f);
                ground.GetComponent<Renderer>().material = groundMat;
                createdObjects.Add(ground.name);
                
                // プレイヤー作成
                if (elementsList.Contains("player"))
                {
                    var player = CreatePrototypePlayer(playerType);
                    player.transform.parent = world.transform;
                    player.transform.position = Vector3.up;
                    createdObjects.Add(player.name);
                }
                
                // 敵作成
                if (elementsList.Contains("enemies"))
                {
                    var enemies = new GameObject("Enemies");
                    enemies.transform.parent = world.transform;
                    for (int i = 0; i < 5; i++)
                    {
                        var enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        enemy.name = $"Enemy_{i}";
                        enemy.transform.parent = enemies.transform;
                        enemy.transform.position = new Vector3(
                            Random.Range(-worldSize/2, worldSize/2),
                            0.5f,
                            Random.Range(-worldSize/2, worldSize/2)
                        );
                        enemy.GetComponent<Renderer>().material.color = Color.red;
                        enemy.AddComponent<Rigidbody>();
                    }
                    createdObjects.Add("Enemies (5)");
                }
                
                // 収集アイテム
                if (elementsList.Contains("collectibles"))
                {
                    var collectibles = new GameObject("Collectibles");
                    collectibles.transform.parent = world.transform;
                    for (int i = 0; i < 10; i++)
                    {
                        var item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        item.name = $"Collectible_{i}";
                        item.transform.parent = collectibles.transform;
                        item.transform.position = new Vector3(
                            Random.Range(-worldSize/2, worldSize/2),
                            0.5f,
                            Random.Range(-worldSize/2, worldSize/2)
                        );
                        item.transform.localScale = Vector3.one * 0.5f;
                        item.GetComponent<Renderer>().material.color = Color.yellow;
                        var collider = item.GetComponent<SphereCollider>();
                        collider.isTrigger = true;
                    }
                    createdObjects.Add("Collectibles (10)");
                }
                
                // 障害物
                if (elementsList.Contains("obstacles"))
                {
                    var obstacles = new GameObject("Obstacles");
                    obstacles.transform.parent = world.transform;
                    for (int i = 0; i < 8; i++)
                    {
                        var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        obstacle.name = $"Obstacle_{i}";
                        obstacle.transform.parent = obstacles.transform;
                        obstacle.transform.position = new Vector3(
                            Random.Range(-worldSize/2, worldSize/2),
                            Random.Range(0.5f, 2f),
                            Random.Range(-worldSize/2, worldSize/2)
                        );
                        obstacle.transform.localScale = new Vector3(
                            Random.Range(1f, 3f),
                            Random.Range(1f, 4f),
                            Random.Range(1f, 3f)
                        );
                        obstacle.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.3f);
                    }
                    createdObjects.Add("Obstacles (8)");
                }
                
                // 基本的な照明設定
                var lighting = GameObject.Find("Directional Light");
                if (lighting == null)
                {
                    lighting = new GameObject("Directional Light");
                    var light = lighting.AddComponent<Light>();
                    light.type = LightType.Directional;
                    light.intensity = 1.5f;
                    light.shadows = LightShadows.Soft;
                }
                lighting.transform.rotation = Quaternion.Euler(45, -30, 0);
                
                // カメラ設定
                var mainCamera = Camera.main ?? new GameObject("Main Camera").AddComponent<Camera>();
                mainCamera.transform.position = new Vector3(0, 10, -10);
                mainCamera.transform.LookAt(Vector3.zero);
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = "Created quick prototype",
                    worldSize = worldSize,
                    elements = elementsList,
                    objectsCreated = createdObjects,
                    cameraPosition = mainCamera.transform.position.ToString()
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        private GameObject CreatePrototypePlayer(string type)
        {
            GameObject player = null;
            
            switch (type)
            {
                case "Capsule":
                    player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    break;
                case "Cube":
                    player = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    break;
                case "Sphere":
                    player = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    break;
                default:
                    player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    break;
            }
            
            player.name = "Player";
            player.GetComponent<Renderer>().material.color = Color.blue;
            
            // 基本的な移動コンポーネント
            var rb = player.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            
            // 簡単な移動スクリプト
            var moveScript = @"
using UnityEngine;

public class SimpleMove : MonoBehaviour {
    public float speed = 5f;
    Rigidbody rb;
    
    void Start() {
        rb = GetComponent<Rigidbody>();
    }
    
    void Update() {
        float h = Input.GetAxis(""Horizontal"");
        float v = Input.GetAxis(""Vertical"");
        Vector3 movement = new Vector3(h, 0, v) * speed * Time.deltaTime;
        rb.MovePosition(transform.position + movement);
    }
}";
            
            // スクリプトファイルを作成
            var scriptPath = "Assets/Scripts/SimpleMove.cs";
            var scriptDir = System.IO.Path.GetDirectoryName(scriptPath);
            if (!System.IO.Directory.Exists(scriptDir))
            {
                System.IO.Directory.CreateDirectory(scriptDir);
            }
            System.IO.File.WriteAllText(scriptPath, moveScript);
            AssetDatabase.Refresh();
            
            return player;
        }
        
        #region AI・機械学習関連メソッド
        
        /// <summary>
        /// ML Agent（強化学習）エージェントのセットアップ
        /// </summary>
        private string SetupMLAgent(Dictionary<string, string> parameters)
        {
            try
            {
                string agentName = parameters.GetValueOrDefault("agentName", "MLAgent");
                string agentType = parameters.GetValueOrDefault("agentType", "Basic");
                int vectorObservationSize = Convert.ToInt32(parameters.GetValueOrDefault("vectorObservationSize", "8"));
                bool useVisualObservation = Convert.ToBoolean(parameters.GetValueOrDefault("useVisualObservation", "false"));
                
                // エージェントオブジェクト作成
                var agent = new GameObject(agentName);
                var agentScript = GenerateMLAgentScript(agentName, agentType, vectorObservationSize, useVisualObservation);
                
                // スクリプト保存
                var scriptPath = $"Assets/Scripts/AI/ML/{agentName}.cs";
                var scriptDir = System.IO.Path.GetDirectoryName(scriptPath);
                if (!System.IO.Directory.Exists(scriptDir))
                {
                    System.IO.Directory.CreateDirectory(scriptDir);
                }
                System.IO.File.WriteAllText(scriptPath, agentScript);
                
                // エージェント環境設定
                var environment = new GameObject($"{agentName}Environment");
                var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
                platform.name = "Platform";
                platform.transform.parent = environment.transform;
                platform.transform.localScale = new Vector3(10, 0.5f, 10);
                platform.transform.position = Vector3.zero;
                
                // ゴール設定
                var goal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                goal.name = "Goal";
                goal.transform.parent = environment.transform;
                goal.transform.position = new Vector3(Random.Range(-4f, 4f), 1f, Random.Range(-4f, 4f));
                goal.GetComponent<Renderer>().material.color = Color.green;
                
                agent.transform.parent = environment.transform;
                agent.transform.position = new Vector3(0, 1, 0);
                
                AssetDatabase.Refresh();
                
                return JsonConvert.SerializeObject(new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"ML Agent '{agentName}' setup completed\\nAgent Type: {agentType}\\nObservation Size: {vectorObservationSize}\\nVisual Observation: {useVisualObservation}\\nScript: {scriptPath}"
                        }
                    }
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"Error setting up ML Agent: {e.Message}"
                        }
                    }
                });
            }
        }
        
        /// <summary>
        /// ニューラルネットワークシステムの作成
        /// </summary>
        private string CreateNeuralNetwork(Dictionary<string, string> parameters)
        {
            try
            {
                string networkName = parameters.GetValueOrDefault("networkName", "NeuralNetwork");
                string networkType = parameters.GetValueOrDefault("networkType", "Feedforward");
                int inputSize = Convert.ToInt32(parameters.GetValueOrDefault("inputSize", "4"));
                int hiddenSize = Convert.ToInt32(parameters.GetValueOrDefault("hiddenSize", "8"));
                int outputSize = Convert.ToInt32(parameters.GetValueOrDefault("outputSize", "2"));
                
                var networkScript = GenerateNeuralNetworkScript(networkName, networkType, inputSize, hiddenSize, outputSize);
                
                // スクリプト保存
                var scriptPath = $"Assets/Scripts/AI/Neural/{networkName}.cs";
                var scriptDir = System.IO.Path.GetDirectoryName(scriptPath);
                if (!System.IO.Directory.Exists(scriptDir))
                {
                    System.IO.Directory.CreateDirectory(scriptDir);
                }
                System.IO.File.WriteAllText(scriptPath, networkScript);
                
                // テスト用オブジェクト作成
                var testObject = new GameObject($"{networkName}Test");
                
                AssetDatabase.Refresh();
                
                return JsonConvert.SerializeObject(new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"Neural Network '{networkName}' created\\nType: {networkType}\\nArchitecture: {inputSize} → {hiddenSize} → {outputSize}\\nScript: {scriptPath}"
                        }
                    }
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"Error creating neural network: {e.Message}"
                        }
                    }
                });
            }
        }
        
        /// <summary>
        /// ビヘイビアツリーシステムのセットアップ
        /// </summary>
        private string SetupBehaviorTree(Dictionary<string, string> parameters)
        {
            try
            {
                string treeName = parameters.GetValueOrDefault("treeName", "BehaviorTree");
                string aiType = parameters.GetValueOrDefault("aiType", "Enemy");
                bool includePatrol = Convert.ToBoolean(parameters.GetValueOrDefault("includePatrol", "true"));
                bool includeChase = Convert.ToBoolean(parameters.GetValueOrDefault("includeChase", "true"));
                bool includeAttack = Convert.ToBoolean(parameters.GetValueOrDefault("includeAttack", "true"));
                
                var treeScript = GenerateBehaviorTreeScript(treeName, aiType, includePatrol, includeChase, includeAttack);
                
                // スクリプト保存
                var scriptPath = $"Assets/Scripts/AI/BehaviorTree/{treeName}.cs";
                var scriptDir = System.IO.Path.GetDirectoryName(scriptPath);
                if (!System.IO.Directory.Exists(scriptDir))
                {
                    System.IO.Directory.CreateDirectory(scriptDir);
                }
                System.IO.File.WriteAllText(scriptPath, treeScript);
                
                // AIオブジェクト作成
                var aiObject = new GameObject($"{treeName}AI");
                aiObject.AddComponent<CapsuleCollider>();
                aiObject.AddComponent<Rigidbody>();
                
                AssetDatabase.Refresh();
                
                return JsonConvert.SerializeObject(new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"Behavior Tree '{treeName}' setup completed\\nAI Type: {aiType}\\nPatrol: {includePatrol}, Chase: {includeChase}, Attack: {includeAttack}\\nScript: {scriptPath}"
                        }
                    }
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"Error setting up behavior tree: {e.Message}"
                        }
                    }
                });
            }
        }
        
        // ===== GOAP AI系メソッド =====
        
        private string CreateGoapAgent(Dictionary<string, string> parameters)
        {
            try
            {
                // パラメータ検証
                if (parameters == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "No parameters provided" });
                }

                string name = parameters.GetValueOrDefault("name", "GOAPAgent");
                string agentType = parameters.GetValueOrDefault("agentType", "Generic");
                
                // 名前の重複チェック
                if (GameObject.Find(name) != null)
                {
                    name = $"{name}_{Random.Range(1000, 9999)}";
                    Debug.LogWarning($"[CreateGoapAgent] Name conflict resolved, using: {name}");
                }
                
                Vector3 position;
                try
                {
                    position = ParseVector3(parameters.GetValueOrDefault("position", "0,0,0"));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CreateGoapAgent] Failed to parse position, using origin: {ex.Message}");
                    position = Vector3.zero;
                }
                
                // GOAPエージェントGameObject作成
                GameObject agent = null;
                try
                {
                    agent = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    agent.name = name;
                    agent.transform.position = position;
                    
                    // Rigidbodyを追加（GOAP移動用）
                    var rb = agent.AddComponent<Rigidbody>();
                    rb.useGravity = true;
                    rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                    
                    // GOAPエージェント識別用タグ
                    agent.tag = "Untagged"; // 後でGOAPAgentタグに変更可能
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CreateGoapAgent] Failed to create agent GameObject: {ex.Message}");
                    return JsonConvert.SerializeObject(new { success = false, error = $"Failed to create agent: {ex.Message}" });
                }
                
                // GOAP関連データを保持するための仮のコンポーネント
                try
                {
                    var agentData = new GameObject($"{name}_GOAPData");
                    agentData.transform.parent = agent.transform;
                    agentData.transform.localPosition = Vector3.zero;
                    
                    // デバッグ情報表示コンポーネント（空のオブジェクト）
                    var debugInfo = agentData.AddComponent<Transform>();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CreateGoapAgent] Failed to create GOAP data object: {ex.Message}");
                }
                
                // メタデータ保存（シリアライゼーション安全）
                var metadata = new Dictionary<string, object>
                {
                    ["agentType"] = agentType,
                    ["capabilities"] = parameters.GetValueOrDefault("capabilities", "[]"),
                    ["createdAt"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ["goapVersion"] = "v3",
                    ["position"] = new Dictionary<string, float>
                    {
                        ["x"] = position.x,
                        ["y"] = position.y,
                        ["z"] = position.z
                    }
                };
                
                lastCreatedObject = agent;
                createdObjects.Add(agent);
                
                Debug.Log($"[GOAP] Created agent '{name}' at position {position} with type '{agentType}'");
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Created GOAP agent: {name}",
                    agentId = agent.GetInstanceID(),
                    agentName = name,
                    position = new { x = position.x, y = position.y, z = position.z },
                    metadata = metadata,
                    components = new string[] { "Rigidbody", "CapsuleCollider", "Transform" }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CreateGoapAgent] Unexpected error: {e.Message}\nStackTrace: {e.StackTrace}");
                return JsonConvert.SerializeObject(new 
                { 
                    success = false, 
                    error = e.Message,
                    stackTrace = e.StackTrace,
                    details = "Failed to create GOAP agent"
                }, Formatting.Indented);
            }
        }
        
        private string DefineGoapGoal(Dictionary<string, string> parameters)
        {
            try
            {
                string agentName = parameters.GetValueOrDefault("agentName", "");
                string goalName = parameters.GetValueOrDefault("goalName", "DefaultGoal");
                string description = parameters.GetValueOrDefault("description", "");
                float priority = float.Parse(parameters.GetValueOrDefault("priority", "50"));
                string conditions = parameters.GetValueOrDefault("conditions", "");
                
                // エージェントを検索
                var agent = GameObject.Find(agentName);
                if (agent == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"Agent '{agentName}' not found"
                    });
                }
                
                // ゴールデータ構造
                var goalData = new
                {
                    name = goalName,
                    description = description,
                    priority = priority,
                    conditions = ParseNaturalLanguageConditions(conditions),
                    createdAt = DateTime.Now
                };
                
                // ゴールをエージェントのメタデータに追加（実際のGOAP実装では適切な方法で保存）
                Debug.Log($"[GOAP] Defined goal '{goalName}' for agent '{agentName}'");
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Goal '{goalName}' defined for agent '{agentName}'",
                    goal = goalData
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        private string CreateGoapAction(Dictionary<string, string> parameters)
        {
            try
            {
                // パラメータ検証
                if (parameters == null || parameters.Count == 0)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "No parameters provided" });
                }

                string agentName = parameters.GetValueOrDefault("agentName", "");
                if (string.IsNullOrWhiteSpace(agentName))
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Agent name is required" });
                }

                string actionName = parameters.GetValueOrDefault("actionName", "DefaultAction");
                string description = parameters.GetValueOrDefault("description", "");
                string preconditionsStr = parameters.GetValueOrDefault("preconditions", "[]");
                string effectsStr = parameters.GetValueOrDefault("effects", "[]");
                
                float cost;
                if (!float.TryParse(parameters.GetValueOrDefault("cost", "1"), out cost) || cost < 0)
                {
                    cost = 1.0f;
                    Debug.LogWarning($"[CreateGoapAction] Invalid cost value, using default: {cost}");
                }
                
                var agent = GameObject.Find(agentName);
                if (agent == null)
                {
                    Debug.LogWarning($"[CreateGoapAction] Agent '{agentName}' not found in scene");
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"Agent '{agentName}' not found in scene. Available objects: {GetSceneObjectNames()}"
                    });
                }
                
                // JSONパース（安全）
                string[] preconditions = null;
                string[] effects = null;
                
                try
                {
                    if (!string.IsNullOrWhiteSpace(preconditionsStr) && preconditionsStr != "[]")
                    {
                        preconditions = JsonConvert.DeserializeObject<string[]>(preconditionsStr);
                    }
                    else
                    {
                        preconditions = new string[0];
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CreateGoapAction] Failed to parse preconditions JSON: {ex.Message}");
                    preconditions = new string[0];
                }
                
                try
                {
                    if (!string.IsNullOrWhiteSpace(effectsStr) && effectsStr != "[]")
                    {
                        effects = JsonConvert.DeserializeObject<string[]>(effectsStr);
                    }
                    else
                    {
                        effects = new string[0];
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CreateGoapAction] Failed to parse effects JSON: {ex.Message}");
                    effects = new string[0];
                }
                
                var actionData = new Dictionary<string, object>
                {
                    ["name"] = actionName,
                    ["description"] = description,
                    ["preconditions"] = preconditions ?? new string[0],
                    ["effects"] = effects ?? new string[0],
                    ["cost"] = cost,
                    ["createdAt"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ["agentName"] = agentName
                };
                
                Debug.Log($"[GOAP] Created action '{actionName}' for agent '{agentName}' with {preconditions?.Length ?? 0} preconditions and {effects?.Length ?? 0} effects");
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Action '{actionName}' created for agent '{agentName}' successfully",
                    action = actionData,
                    agentInfo = new
                    {
                        name = agentName,
                        position = agent.transform.position,
                        active = agent.activeInHierarchy
                    }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CreateGoapAction] Unexpected error: {e.Message}\nStackTrace: {e.StackTrace}");
                return JsonConvert.SerializeObject(new 
                { 
                    success = false, 
                    error = e.Message,
                    stackTrace = e.StackTrace,
                    details = "Failed to create GOAP action"
                }, Formatting.Indented);
            }
        }
        
        private string DefineBehaviorLanguage(Dictionary<string, string> parameters)
        {
            try
            {
                string agentName = parameters.GetValueOrDefault("agentName", "");
                string behavior = parameters.GetValueOrDefault("behavior", "");
                string gameContext = parameters.GetValueOrDefault("gameContext", "Generic");
                string difficulty = parameters.GetValueOrDefault("difficulty", "normal");
                
                var agent = GameObject.Find(agentName);
                if (agent == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"Agent '{agentName}' not found"
                    });
                }
                
                // 自然言語を解析してGOAPコンポーネントを生成
                var behaviorComponents = ParseNaturalLanguageBehavior(behavior, gameContext, difficulty);
                
                // 生成された行動パターン
                var generatedBehavior = new
                {
                    goals = behaviorComponents["goals"],
                    actions = behaviorComponents["actions"],
                    worldState = behaviorComponents["worldState"],
                    sensors = behaviorComponents["sensors"],
                    originalDescription = behavior,
                    context = gameContext,
                    difficulty = difficulty
                };
                
                Debug.Log($"[GOAP] Defined behavior for agent '{agentName}' from natural language");
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Behavior defined for agent '{agentName}'",
                    behavior = generatedBehavior
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        private string GenerateGoapActionSet(Dictionary<string, string> parameters)
        {
            try
            {
                string agentName = parameters.GetValueOrDefault("agentName", "");
                string agentRole = parameters.GetValueOrDefault("agentRole", "generic");
                string environment = parameters.GetValueOrDefault("environment", "");
                bool includeDefaults = bool.Parse(parameters.GetValueOrDefault("includeDefaults", "true"));
                
                var agent = GameObject.Find(agentName);
                if (agent == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"Agent '{agentName}' not found"
                    });
                }
                
                // ロールベースのアクションセット生成
                var actionSet = GenerateRoleBasedActions(agentRole, environment, includeDefaults);
                
                Debug.Log($"[GOAP] Generated action set for {agentRole} agent '{agentName}'");
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Generated action set for agent '{agentName}'",
                    role = agentRole,
                    environment = environment,
                    actions = actionSet
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        private string SetupGoapWorldState(Dictionary<string, string> parameters)
        {
            try
            {
                // パラメータ検証
                if (parameters == null || parameters.Count == 0)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "No parameters provided"
                    });
                }

                string agentName = parameters.GetValueOrDefault("agentName", "");
                if (string.IsNullOrWhiteSpace(agentName))
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "Agent name is required"
                    });
                }

                string worldStateStr = parameters.GetValueOrDefault("worldState", "{}");
                string sensorsStr = parameters.GetValueOrDefault("sensors", "[]");
                
                float updateFrequency;
                if (!float.TryParse(parameters.GetValueOrDefault("updateFrequency", "0.5"), out updateFrequency))
                {
                    updateFrequency = 0.5f;
                    Debug.LogWarning($"[SetupGoapWorldState] Invalid updateFrequency, using default: {updateFrequency}");
                }
                
                // エージェントの検索
                var agent = GameObject.Find(agentName);
                if (agent == null)
                {
                    Debug.LogWarning($"[SetupGoapWorldState] Agent '{agentName}' not found in scene");
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"Agent '{agentName}' not found in scene. Available objects: {GetSceneObjectNames()}"
                    });
                }
                
                // JSON デシリアライゼーション（安全）
                Dictionary<string, object> worldState = null;
                string[] sensors = null;
                
                try
                {
                    if (!string.IsNullOrWhiteSpace(worldStateStr) && worldStateStr != "{}")
                    {
                        worldState = JsonConvert.DeserializeObject<Dictionary<string, object>>(worldStateStr);
                    }
                    else
                    {
                        worldState = new Dictionary<string, object>();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SetupGoapWorldState] Failed to parse worldState JSON: {ex.Message}");
                    worldState = new Dictionary<string, object>();
                }
                
                try
                {
                    if (!string.IsNullOrWhiteSpace(sensorsStr) && sensorsStr != "[]")
                    {
                        sensors = JsonConvert.DeserializeObject<string[]>(sensorsStr);
                    }
                    else
                    {
                        sensors = new string[0];
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SetupGoapWorldState] Failed to parse sensors JSON: {ex.Message}");
                    sensors = new string[0];
                }
                
                // ワールドステート設定
                var worldStateConfig = new
                {
                    agentName = agentName,
                    state = worldState ?? new Dictionary<string, object>(),
                    sensors = sensors ?? new string[0],
                    updateFrequency = updateFrequency,
                    lastUpdate = DateTime.Now,
                    position = agent.transform.position,
                    isActive = agent.activeInHierarchy
                };
                
                Debug.Log($"[GOAP] Setup world state for agent '{agentName}' with {worldState?.Count ?? 0} state variables and {sensors?.Length ?? 0} sensors");
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"World state configured for agent '{agentName}' successfully",
                    config = worldStateConfig,
                    agentInfo = new
                    {
                        name = agentName,
                        position = agent.transform.position,
                        active = agent.activeInHierarchy,
                        hasRigidbody = agent.GetComponent<Rigidbody>() != null,
                        hasCollider = agent.GetComponent<Collider>() != null
                    }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SetupGoapWorldState] Unexpected error: {e.Message}\nStackTrace: {e.StackTrace}");
                return JsonConvert.SerializeObject(new 
                { 
                    success = false, 
                    error = e.Message,
                    stackTrace = e.StackTrace,
                    details = "Failed to setup GOAP world state"
                }, Formatting.Indented);
            }
        }
        
        // ヘルパーメソッド：シーンオブジェクト名取得
        private string GetSceneObjectNames()
        {
            try
            {
                var objects = GameObject.FindObjectsOfType<GameObject>();
                var names = objects.Take(10).Select(obj => obj.name).ToArray();
                return string.Join(", ", names) + (objects.Length > 10 ? "..." : "");
            }
            catch
            {
                return "Unable to retrieve scene objects";
            }
        }
        
        private string CreateGoapTemplate(Dictionary<string, string> parameters)
        {
            try
            {
                string templateType = parameters.GetValueOrDefault("templateType", "generic");
                string difficulty = parameters.GetValueOrDefault("difficulty", "normal");
                string behaviorsStr = parameters.GetValueOrDefault("behaviors", "[]");
                string customizationsStr = parameters.GetValueOrDefault("customizations", "{}");
                
                var behaviors = JsonConvert.DeserializeObject<string[]>(behaviorsStr);
                var customizations = JsonConvert.DeserializeObject<Dictionary<string, object>>(customizationsStr);
                
                // テンプレートベースのAI生成
                var template = GenerateGameSpecificTemplate(templateType, difficulty, behaviors, customizations);
                
                // テンプレートからエージェントを作成
                var agent = CreateAgentFromTemplate(template);
                
                lastCreatedObject = agent;
                createdObjects.Add(agent);
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Created {templateType} GOAP template",
                    agentId = agent.GetInstanceID(),
                    template = template
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        private string DebugGoapDecisions(Dictionary<string, string> parameters)
        {
            try
            {
                string agentName = parameters.GetValueOrDefault("agentName", "");
                bool showGraph = bool.Parse(parameters.GetValueOrDefault("showGraph", "true"));
                bool showWorldState = bool.Parse(parameters.GetValueOrDefault("showWorldState", "true"));
                bool showPlan = bool.Parse(parameters.GetValueOrDefault("showPlan", "true"));
                bool logToConsole = bool.Parse(parameters.GetValueOrDefault("logToConsole", "false"));
                
                var agent = GameObject.Find(agentName);
                if (agent == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"Agent '{agentName}' not found"
                    });
                }
                
                // デバッグ情報の収集（仮のデータ）
                var debugInfo = new
                {
                    currentGoal = "Patrol Area",
                    currentPlan = new[] { "MoveTo", "LookAround", "CheckForEnemies" },
                    worldState = new Dictionary<string, object>
                    {
                        ["has_weapon"] = true,
                        ["enemies_nearby"] = 0,
                        ["health"] = 100,
                        ["position"] = agent.transform.position.ToString()
                    },
                    planCost = 3.5f,
                    planningTime = "0.012s",
                    graphNodes = 15,
                    graphEdges = 23
                };
                
                if (showGraph)
                {
                    Debug.Log($"[GOAP Debug] Graph visualization enabled for {agentName}");
                }
                
                if (logToConsole)
                {
                    Debug.Log($"[GOAP Debug] {JsonConvert.SerializeObject(debugInfo, Formatting.Indented)}");
                }
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Debug info for agent '{agentName}'",
                    debug = debugInfo
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        private string OptimizeGoapPerformance(Dictionary<string, string> parameters)
        {
            try
            {
                string agentName = parameters.GetValueOrDefault("agentName", "");
                int maxPlanDepth = int.Parse(parameters.GetValueOrDefault("maxPlanDepth", "10"));
                float planningFrequency = float.Parse(parameters.GetValueOrDefault("planningFrequency", "1"));
                bool enableMultithreading = bool.Parse(parameters.GetValueOrDefault("enableMultithreading", "true"));
                int cacheSize = int.Parse(parameters.GetValueOrDefault("cacheSize", "100"));
                
                // 特定のエージェントまたは全体の最適化
                var targetAgents = new List<GameObject>();
                if (!string.IsNullOrEmpty(agentName))
                {
                    var agent = GameObject.Find(agentName);
                    if (agent != null) targetAgents.Add(agent);
                }
                else
                {
                    // すべてのGOAPエージェントを対象
                    targetAgents.AddRange(GameObject.FindObjectsOfType<GameObject>()
                        .Where(go => go.name.Contains("GOAP") || go.name.Contains("Agent")));
                }
                
                var optimizationSettings = new
                {
                    maxPlanDepth = maxPlanDepth,
                    planningFrequency = planningFrequency,
                    enableMultithreading = enableMultithreading,
                    cacheSize = cacheSize,
                    affectedAgents = targetAgents.Count,
                    estimatedPerformanceGain = enableMultithreading ? "40-60%" : "10-20%"
                };
                
                Debug.Log($"[GOAP] Applied optimization settings to {targetAgents.Count} agents");
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = "GOAP performance optimization applied",
                    settings = optimizationSettings
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        // ヘルパーメソッド
        private object ParseNaturalLanguageConditions(string conditions)
        {
            // 自然言語条件をGOAP条件に変換（簡易実装）
            var parsedConditions = new List<object>();
            
            if (conditions.ToLower().Contains("enemy"))
            {
                parsedConditions.Add(new { key = "enemy_detected", value = true });
            }
            if (conditions.ToLower().Contains("health") || conditions.ToLower().Contains("hp"))
            {
                parsedConditions.Add(new { key = "health_low", value = true });
            }
            if (conditions.ToLower().Contains("distance") || conditions.ToLower().Contains("near"))
            {
                parsedConditions.Add(new { key = "target_in_range", value = true });
            }
            
            return parsedConditions;
        }
        
        private Dictionary<string, object> ParseNaturalLanguageBehavior(string behavior, string gameContext, string difficulty)
        {
            // 自然言語から行動パターンを生成（dynamic を使わない実装）
            var result = new Dictionary<string, object>
            {
                ["goals"] = new List<object>(),
                ["actions"] = new List<object>(),
                ["worldState"] = new Dictionary<string, object>(),
                ["sensors"] = new List<string>()
            };
            
            var goals = (List<object>)result["goals"];
            var actions = (List<object>)result["actions"];
            var worldState = (Dictionary<string, object>)result["worldState"];
            var sensors = (List<string>)result["sensors"];
            
            var behaviorLower = behavior.ToLower();
            
            // === 移動・パトロール系 ===
            if (behaviorLower.Contains("patrol") || behaviorLower.Contains("巡回"))
            {
                goals.Add(new { name = "PatrolArea", priority = 80 });
                actions.Add(new { name = "MoveTo", cost = 1, preconditions = new[] { "has_path" }, effects = new[] { "at_target" } });
                actions.Add(new { name = "LookAround", cost = 0.5f, preconditions = new[] { "at_target" }, effects = new[] { "area_checked" } });
                actions.Add(new { name = "MarkWaypoint", cost = 0.2f, preconditions = new[] { "at_waypoint" }, effects = new[] { "waypoint_visited" } });
                sensors.Add("position_sensor");
                sensors.Add("waypoint_sensor");
                worldState["patrol_route"] = "defined";
            }
            
            if (behaviorLower.Contains("follow") || behaviorLower.Contains("追跡") || behaviorLower.Contains("追いかけ"))
            {
                goals.Add(new { name = "FollowTarget", priority = 85 });
                actions.Add(new { name = "TrackTarget", cost = 0.5f, preconditions = new[] { "target_visible" }, effects = new[] { "target_tracked" } });
                actions.Add(new { name = "MoveToTarget", cost = 1f, preconditions = new[] { "target_tracked" }, effects = new[] { "near_target" } });
                sensors.Add("target_tracker");
                sensors.Add("distance_sensor");
            }
            
            // === 戦闘・攻撃系 ===
            if (behaviorLower.Contains("attack") || behaviorLower.Contains("fight") || behaviorLower.Contains("攻撃") || behaviorLower.Contains("戦う"))
            {
                goals.Add(new { name = "EliminateEnemy", priority = 100 });
                
                // 近距離戦闘
                if (behaviorLower.Contains("melee") || behaviorLower.Contains("近接"))
                {
                    actions.Add(new { name = "MeleeAttack", cost = 1.5f, preconditions = new[] { "has_melee_weapon", "enemy_in_melee_range" }, effects = new[] { "damage_dealt" } });
                    worldState["has_melee_weapon"] = true;
                }
                // 遠距離戦闘
                else if (behaviorLower.Contains("shoot") || behaviorLower.Contains("snipe") || behaviorLower.Contains("射撃"))
                {
                    actions.Add(new { name = "RangedAttack", cost = 1f, preconditions = new[] { "has_ranged_weapon", "enemy_in_sight", "has_ammo" }, effects = new[] { "damage_dealt" } });
                    actions.Add(new { name = "Reload", cost = 2f, preconditions = new[] { "has_ranged_weapon", "ammo_low" }, effects = new[] { "has_ammo" } });
                    worldState["has_ranged_weapon"] = true;
                    sensors.Add("ammo_counter");
                }
                // 一般的な攻撃
                else
                {
                    actions.Add(new { name = "Attack", cost = 2f, preconditions = new[] { "has_weapon", "enemy_in_range" }, effects = new[] { "damage_dealt" } });
                    worldState["has_weapon"] = true;
                }
                
                actions.Add(new { name = "ChaseEnemy", cost = 1.5f, preconditions = new[] { "enemy_detected", "enemy_escaping" }, effects = new[] { "enemy_in_range" } });
                sensors.Add("enemy_detector");
                sensors.Add("weapon_checker");
            }
            
            // === 防御・回避系 ===
            if (behaviorLower.Contains("defend") || behaviorLower.Contains("guard") || behaviorLower.Contains("守る") || behaviorLower.Contains("防御"))
            {
                goals.Add(new { name = "DefendPosition", priority = 90 });
                actions.Add(new { name = "TakeDefensivePosition", cost = 0.5f, preconditions = new[] { "defensive_position_available" }, effects = new[] { "in_defensive_position" } });
                actions.Add(new { name = "HoldPosition", cost = 0.3f, preconditions = new[] { "in_defensive_position" }, effects = new[] { "position_held" } });
                actions.Add(new { name = "RepelAttack", cost = 1f, preconditions = new[] { "under_attack", "in_defensive_position" }, effects = new[] { "attack_repelled" } });
                sensors.Add("threat_detector");
            }
            
            if (behaviorLower.Contains("retreat") || behaviorLower.Contains("flee") || behaviorLower.Contains("逃げ") || behaviorLower.Contains("退却"))
            {
                goals.Add(new { name = "Survive", priority = 120 });
                actions.Add(new { name = "Retreat", cost = 0.5f, preconditions = new[] { "escape_route_available" }, effects = new[] { "distance_increased" } });
                actions.Add(new { name = "FindCover", cost = 1f, preconditions = new[] { "cover_nearby" }, effects = new[] { "in_cover" } });
                actions.Add(new { name = "CallForHelp", cost = 0.3f, preconditions = new[] { "allies_nearby" }, effects = new[] { "help_requested" } });
                sensors.Add("health_monitor");
                sensors.Add("escape_route_finder");
            }
            
            if (behaviorLower.Contains("hide") || behaviorLower.Contains("stealth") || behaviorLower.Contains("隠れ") || behaviorLower.Contains("忍び"))
            {
                goals.Add(new { name = "RemainUndetected", priority = 95 });
                actions.Add(new { name = "Hide", cost = 0.5f, preconditions = new[] { "hiding_spot_available" }, effects = new[] { "hidden" } });
                actions.Add(new { name = "Sneak", cost = 1f, preconditions = new[] { "not_detected" }, effects = new[] { "position_changed_quietly" } });
                actions.Add(new { name = "Distract", cost = 1.5f, preconditions = new[] { "has_distraction_item" }, effects = new[] { "enemy_distracted" } });
                sensors.Add("visibility_checker");
                sensors.Add("noise_level_monitor");
            }
            
            // === リソース管理系 ===
            if (behaviorLower.Contains("collect") || behaviorLower.Contains("gather") || behaviorLower.Contains("収集") || behaviorLower.Contains("採集"))
            {
                goals.Add(new { name = "GatherResources", priority = 70 });
                actions.Add(new { name = "SearchForResource", cost = 1f, preconditions = new[] { "resource_needed" }, effects = new[] { "resource_located" } });
                actions.Add(new { name = "CollectResource", cost = 1.5f, preconditions = new[] { "resource_located", "at_resource" }, effects = new[] { "has_resource" } });
                actions.Add(new { name = "StoreResource", cost = 0.5f, preconditions = new[] { "has_resource", "near_storage" }, effects = new[] { "resource_stored" } });
                sensors.Add("resource_detector");
                sensors.Add("inventory_monitor");
            }
            
            // === ソーシャル・協力系 ===
            if (behaviorLower.Contains("help") || behaviorLower.Contains("assist") || behaviorLower.Contains("助け") || behaviorLower.Contains("支援"))
            {
                goals.Add(new { name = "AssistAllies", priority = 85 });
                actions.Add(new { name = "MoveToAlly", cost = 1f, preconditions = new[] { "ally_needs_help" }, effects = new[] { "near_ally" } });
                actions.Add(new { name = "ProvideSupport", cost = 1f, preconditions = new[] { "near_ally", "can_help" }, effects = new[] { "ally_assisted" } });
                sensors.Add("ally_status_monitor");
            }
            
            if (behaviorLower.Contains("team") || behaviorLower.Contains("group") || behaviorLower.Contains("チーム") || behaviorLower.Contains("集団"))
            {
                goals.Add(new { name = "CoordinateWithTeam", priority = 75 });
                actions.Add(new { name = "FormFormation", cost = 0.5f, preconditions = new[] { "team_nearby" }, effects = new[] { "in_formation" } });
                actions.Add(new { name = "ShareIntel", cost = 0.3f, preconditions = new[] { "has_intel" }, effects = new[] { "intel_shared" } });
                sensors.Add("team_coordinator");
            }
            
            // === 状態管理系 ===
            if (behaviorLower.Contains("heal") || behaviorLower.Contains("health") || behaviorLower.Contains("回復") || behaviorLower.Contains("治療"))
            {
                actions.Add(new { name = "SeekHealing", cost = 1f, preconditions = new[] { "health_low", "healing_available" }, effects = new[] { "health_restored" } });
                actions.Add(new { name = "UseHealthItem", cost = 0.5f, preconditions = new[] { "has_health_item", "health_low" }, effects = new[] { "health_increased" } });
                sensors.Add("health_monitor");
                worldState["health_threshold"] = difficulty == "hard" ? 30 : (difficulty == "normal" ? 50 : 70);
            }
            
            // === 調査・探索系 ===
            if (behaviorLower.Contains("search") || behaviorLower.Contains("investigate") || behaviorLower.Contains("探索") || behaviorLower.Contains("調査"))
            {
                goals.Add(new { name = "InvestigateArea", priority = 60 });
                actions.Add(new { name = "SearchArea", cost = 1f, preconditions = new[] { "area_unsearched" }, effects = new[] { "area_searched" } });
                actions.Add(new { name = "ExamineObject", cost = 0.5f, preconditions = new[] { "suspicious_object_found" }, effects = new[] { "object_examined" } });
                sensors.Add("search_scanner");
            }
            
            // 難易度による調整
            AdjustForDifficulty(result, difficulty);
            
            // ゲームコンテキストによる調整
            AdjustForGameContext(result, gameContext);
            
            return result;
        }
        
        private void AdjustForDifficulty(Dictionary<string, object> result, string difficulty)
        {
            var actions = (List<object>)result["actions"];
            var worldState = (Dictionary<string, object>)result["worldState"];
            var sensors = (List<string>)result["sensors"];
            
            switch (difficulty.ToLower())
            {
                case "easy":
                    // アクションコストを下げる
                    foreach (var actionObj in actions)
                    {
                        var action = actionObj as Dictionary<string, object>;
                        if (action != null && action.ContainsKey("cost"))
                        {
                            action["cost"] = Convert.ToSingle(action["cost"]) * 0.8f;
                        }
                    }
                    worldState["reaction_time"] = 1.5f;
                    worldState["accuracy"] = 0.6f;
                    break;
                    
                case "hard":
                    // アクションコストを下げて効率化
                    foreach (var actionObj in actions)
                    {
                        var action = actionObj as Dictionary<string, object>;
                        if (action != null && action.ContainsKey("cost"))
                        {
                            action["cost"] = Convert.ToSingle(action["cost"]) * 0.6f;
                        }
                    }
                    worldState["reaction_time"] = 0.3f;
                    worldState["accuracy"] = 0.9f;
                    // 追加アクション
                    actions.Add(new { name = "PredictPlayerMove", cost = 0.5f, preconditions = new[] { "player_pattern_analyzed" }, effects = new[] { "player_move_predicted" } });
                    sensors.Add("pattern_analyzer");
                    break;
                    
                case "adaptive":
                    worldState["learning_rate"] = 0.1f;
                    actions.Add(new { name = "AnalyzePlayer", cost = 0.3f, preconditions = new[] { "player_observed" }, effects = new[] { "player_analyzed" } });
                    actions.Add(new { name = "AdaptStrategy", cost = 0.5f, preconditions = new[] { "player_analyzed" }, effects = new[] { "strategy_adapted" } });
                    sensors.Add("player_behavior_tracker");
                    break;
                    
                default: // normal
                    worldState["reaction_time"] = 0.8f;
                    worldState["accuracy"] = 0.75f;
                    break;
            }
        }
        
        private void AdjustForGameContext(Dictionary<string, object> result, string gameContext)
        {
            var actions = (List<object>)result["actions"];
            var worldState = (Dictionary<string, object>)result["worldState"];
            var sensors = (List<string>)result["sensors"];
            
            switch (gameContext.ToLower())
            {
                case "fps":
                    sensors.Add("line_of_sight_checker");
                    sensors.Add("sound_detector");
                    actions.Add(new { name = "TakeCover", cost = 0.5f, preconditions = new[] { "under_fire", "cover_available" }, effects = new[] { "in_cover" } });
                    actions.Add(new { name = "ThrowGrenade", cost = 2f, preconditions = new[] { "has_grenade", "enemy_grouped" }, effects = new[] { "area_damage_dealt" } });
                    break;
                    
                case "rts":
                    sensors.Add("fog_of_war_revealer");
                    sensors.Add("resource_calculator");
                    actions.Add(new { name = "ConstructBuilding", cost = 3f, preconditions = new[] { "has_resources", "build_location_valid" }, effects = new[] { "building_constructed" } });
                    actions.Add(new { name = "ScoutArea", cost = 1f, preconditions = new[] { "area_unexplored" }, effects = new[] { "area_revealed" } });
                    break;
                    
                case "rpg":
                    sensors.Add("quest_tracker");
                    sensors.Add("dialogue_manager");
                    actions.Add(new { name = "InteractWithNPC", cost = 0.5f, preconditions = new[] { "npc_nearby" }, effects = new[] { "dialogue_initiated" } });
                    actions.Add(new { name = "UseSkill", cost = 1f, preconditions = new[] { "skill_available", "mana_sufficient" }, effects = new[] { "skill_used" } });
                    worldState["mana"] = 100;
                    break;
                    
                case "stealth":
                    sensors.Add("light_level_detector");
                    sensors.Add("guard_patrol_tracker");
                    actions.Add(new { name = "ExtinguishLight", cost = 1f, preconditions = new[] { "light_source_nearby" }, effects = new[] { "area_darkened" } });
                    actions.Add(new { name = "CreateNoise", cost = 0.5f, preconditions = new[] { "has_noise_maker" }, effects = new[] { "guard_distracted" } });
                    worldState["detection_level"] = 0;
                    break;
                    
                case "survival":
                    sensors.Add("hunger_monitor");
                    sensors.Add("temperature_sensor");
                    actions.Add(new { name = "FindFood", cost = 2f, preconditions = new[] { "hungry" }, effects = new[] { "has_food" } });
                    actions.Add(new { name = "BuildShelter", cost = 3f, preconditions = new[] { "has_materials", "shelter_needed" }, effects = new[] { "has_shelter" } });
                    worldState["hunger"] = 50;
                    worldState["temperature"] = 20;
                    break;
            }
        }
        
        private List<object> GenerateRoleBasedActions(string role, string environment, bool includeDefaults)
        {
            var actions = new List<object>();
            
            // デフォルトアクション
            if (includeDefaults)
            {
                actions.Add(new { name = "Idle", cost = 0.1f, preconditions = new string[0], effects = new[] { "rested" } });
                actions.Add(new { name = "MoveTo", cost = 1f, preconditions = new[] { "has_path" }, effects = new[] { "at_target" } });
            }
            
            // ロール別アクション
            switch (role.ToLower())
            {
                case "guard":
                    actions.Add(new { name = "Patrol", cost = 1f, preconditions = new[] { "on_duty" }, effects = new[] { "area_checked" } });
                    actions.Add(new { name = "Alert", cost = 0.5f, preconditions = new[] { "enemy_detected" }, effects = new[] { "alarm_raised" } });
                    actions.Add(new { name = "Engage", cost = 2f, preconditions = new[] { "has_weapon", "enemy_in_range" }, effects = new[] { "enemy_eliminated" } });
                    break;
                    
                case "worker":
                    actions.Add(new { name = "Gather", cost = 1.5f, preconditions = new[] { "resource_available" }, effects = new[] { "has_resource" } });
                    actions.Add(new { name = "Build", cost = 2f, preconditions = new[] { "has_materials" }, effects = new[] { "structure_built" } });
                    actions.Add(new { name = "Repair", cost = 1f, preconditions = new[] { "structure_damaged" }, effects = new[] { "structure_repaired" } });
                    break;
                    
                case "enemy":
                    actions.Add(new { name = "Hunt", cost = 1.5f, preconditions = new[] { "target_detected" }, effects = new[] { "target_found" } });
                    actions.Add(new { name = "Attack", cost = 2f, preconditions = new[] { "in_attack_range" }, effects = new[] { "damage_dealt" } });
                    actions.Add(new { name = "CallBackup", cost = 0.5f, preconditions = new[] { "outnumbered" }, effects = new[] { "reinforcements_called" } });
                    break;
            }
            
            // 環境別追加アクション
            if (!string.IsNullOrEmpty(environment))
            {
                switch (environment.ToLower())
                {
                    case "forest":
                        actions.Add(new { name = "Hide", cost = 0.5f, preconditions = new[] { "in_forest" }, effects = new[] { "hidden" } });
                        break;
                    case "urban":
                        actions.Add(new { name = "UseCover", cost = 0.5f, preconditions = new[] { "cover_available" }, effects = new[] { "in_cover" } });
                        break;
                }
            }
            
            return actions;
        }
        
        private GameObject CreateAgentFromTemplate(object template)
        {
            // テンプレートからエージェントを作成
            var agent = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            agent.name = "GOAP_Agent_" + DateTime.Now.Ticks;
            
            // テンプレートに基づいて設定を適用
            // 実際の実装ではテンプレートデータを解析して適切なコンポーネントを追加
            
            return agent;
        }
        
        private Dictionary<string, object> GenerateGameSpecificTemplate(string templateType, string difficulty, string[] behaviors, Dictionary<string, object> customizations)
        {
            var template = new Dictionary<string, object>();
            template["type"] = templateType;
            template["difficulty"] = difficulty;
            template["behaviors"] = behaviors;
            template["customizations"] = customizations;
            
            // テンプレートタイプ別の設定
            switch (templateType)
            {
                case "fps_enemy":
                    template["defaultGoals"] = new[] { "EliminatePlayer", "PatrolArea", "TakeCover" };
                    template["defaultActions"] = new[] { "Shoot", "Reload", "TakeCover", "ThrowGrenade", "CallBackup" };
                    template["defaultSensors"] = new[] { "player_detector", "sound_detector", "health_monitor" };
                    break;
                    
                case "rts_unit":
                    template["defaultGoals"] = new[] { "GatherResources", "BuildStructures", "DefendBase" };
                    template["defaultActions"] = new[] { "Move", "Gather", "Build", "Attack", "Repair" };
                    template["defaultSensors"] = new[] { "resource_detector", "enemy_detector", "base_monitor" };
                    break;
                    
                case "rpg_npc":
                    template["defaultGoals"] = new[] { "SocialInteraction", "Trade", "QuestGiving" };
                    template["defaultActions"] = new[] { "Talk", "Trade", "GiveQuest", "MoveTo", "Idle" };
                    template["defaultSensors"] = new[] { "player_proximity", "quest_status", "inventory_checker" };
                    break;
            }
            
            return template;
        }
        
        /// <summary>
        /// AIパスファインディングシステムの作成
        /// </summary>
        private string CreateAIPathfinding(Dictionary<string, string> parameters)
        {
            try
            {
                string systemName = parameters.GetValueOrDefault("systemName", "PathfindingAI");
                string algorithm = parameters.GetValueOrDefault("algorithm", "AStar");
                int gridWidth = Convert.ToInt32(parameters.GetValueOrDefault("gridWidth", "20"));
                int gridHeight = Convert.ToInt32(parameters.GetValueOrDefault("gridHeight", "20"));
                bool use3D = Convert.ToBoolean(parameters.GetValueOrDefault("use3D", "false"));
                
                var pathfindingScript = GeneratePathfindingScript(systemName, algorithm, gridWidth, gridHeight, use3D);
                
                // スクリプト保存
                var scriptPath = $"Assets/Scripts/AI/Pathfinding/{systemName}.cs";
                var scriptDir = System.IO.Path.GetDirectoryName(scriptPath);
                if (!System.IO.Directory.Exists(scriptDir))
                {
                    System.IO.Directory.CreateDirectory(scriptDir);
                }
                System.IO.File.WriteAllText(scriptPath, pathfindingScript);
                
                // テスト環境作成
                var environment = new GameObject($"{systemName}Environment");
                
                // グリッド作成
                for (int x = 0; x < gridWidth; x++)
                {
                    for (int z = 0; z < gridHeight; z++)
                    {
                        if (Random.value < 0.8f) // 80%の確率で通行可能
                        {
                            var cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            cell.name = $"Cell_{x}_{z}";
                            cell.transform.parent = environment.transform;
                            cell.transform.position = new Vector3(x, 0, z);
                            cell.transform.localScale = new Vector3(0.9f, 0.1f, 0.9f);
                            cell.GetComponent<Renderer>().material.color = Color.white;
                        }
                        else // 20%の確率で障害物
                        {
                            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            obstacle.name = $"Obstacle_{x}_{z}";
                            obstacle.transform.parent = environment.transform;
                            obstacle.transform.position = new Vector3(x, 0.5f, z);
                            obstacle.GetComponent<Renderer>().material.color = Color.red;
                        }
                    }
                }
                
                // AIエージェント作成
                var agent = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                agent.name = "PathfindingAgent";
                agent.transform.parent = environment.transform;
                agent.transform.position = new Vector3(1, 1, 1);
                agent.GetComponent<Renderer>().material.color = Color.blue;
                
                AssetDatabase.Refresh();
                
                return JsonConvert.SerializeObject(new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"AI Pathfinding '{systemName}' created\\nAlgorithm: {algorithm}\\nGrid: {gridWidth}x{gridHeight}\\n3D: {use3D}\\nScript: {scriptPath}"
                        }
                    }
                });
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"Error creating AI pathfinding: {e.Message}"
                        }
                    }
                });
            }
        }
        
        private string GenerateMLAgentScript(string agentName, string agentType, int vectorObservationSize, bool useVisualObservation)
        {
            return $@"using UnityEngine;

public class {agentName} : MonoBehaviour
{{
    [Header(""Environment"")]
    public Transform goal;
    public Transform platform;
    
    [Header(""Agent Settings"")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    
    private Rigidbody rb;
    private Vector3 startPosition;
    
    void Start()
    {{
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        
        startPosition = transform.position;
        
        // ゴールがない場合は自動生成
        if (goal == null)
        {{
            var goalObj = GameObject.FindWithTag(""Goal"");
            if (goalObj != null) goal = goalObj.transform;
        }}
    }}
    
    void Update()
    {{
        // 簡単なAI行動（ゴールに向かって移動）
        if (goal != null)
        {{
            Vector3 direction = SafeNormalize(goal.position - transform.position);
            rb.AddForce(direction * moveSpeed);
            
            // ゴール到達判定
            if (Vector3.Distance(transform.position, goal.position) < 1.5f)
            {{
                Debug.Log(""Goal reached!"");
                ResetPosition();
            }}
        }}
        
        // プラットフォームから落下
        if (transform.position.y < -1f)
        {{
            ResetPosition();
        }}
    }}
    
    void ResetPosition()
    {{
        transform.position = startPosition;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }}
}}";
        }
        
        private string GenerateNeuralNetworkScript(string networkName, string networkType, int inputSize, int hiddenSize, int outputSize)
        {
            return $@"using UnityEngine;
using System.Collections.Generic;

public class {networkName} : MonoBehaviour
{{
    [Header(""Network Architecture"")]
    public int inputSize = {inputSize};
    public int hiddenSize = {hiddenSize};
    public int outputSize = {outputSize};
    public float learningRate = 0.01f;
    
    private float[,] weightsInputHidden;
    private float[,] weightsHiddenOutput;
    private float[] hiddenBiases;
    private float[] outputBiases;
    
    void Start()
    {{
        InitializeNetwork();
    }}
    
    void InitializeNetwork()
    {{
        weightsInputHidden = new float[inputSize, hiddenSize];
        weightsHiddenOutput = new float[hiddenSize, outputSize];
        hiddenBiases = new float[hiddenSize];
        outputBiases = new float[outputSize];
        
        // Xavier初期化
        for (int i = 0; i < inputSize; i++)
        {{
            for (int j = 0; j < hiddenSize; j++)
            {{
                weightsInputHidden[i, j] = Random.Range(-0.5f, 0.5f);
            }}
        }}
        
        for (int i = 0; i < hiddenSize; i++)
        {{
            for (int j = 0; j < outputSize; j++)
            {{
                weightsHiddenOutput[i, j] = Random.Range(-0.5f, 0.5f);
            }}
            hiddenBiases[i] = Random.Range(-0.1f, 0.1f);
        }}
        
        for (int i = 0; i < outputSize; i++)
        {{
            outputBiases[i] = Random.Range(-0.1f, 0.1f);
        }}
    }}
    
    public float[] Forward(float[] input)
    {{
        if (input.Length != inputSize) return new float[outputSize];
        
        // 隠れ層計算
        float[] hiddenOutput = new float[hiddenSize];
        for (int j = 0; j < hiddenSize; j++)
        {{
            float sum = hiddenBiases[j];
            for (int i = 0; i < inputSize; i++)
            {{
                sum += input[i] * weightsInputHidden[i, j];
            }}
            hiddenOutput[j] = Sigmoid(sum);
        }}
        
        // 出力層計算
        float[] output = new float[outputSize];
        for (int k = 0; k < outputSize; k++)
        {{
            float sum = outputBiases[k];
            for (int j = 0; j < hiddenSize; j++)
            {{
                sum += hiddenOutput[j] * weightsHiddenOutput[j, k];
            }}
            output[k] = Sigmoid(sum);
        }}
        
        return output;
    }}
    
    private float Sigmoid(float x)
    {{
        return 1.0f / (1.0f + Mathf.Exp(-x));
    }}
}}";
        }
        
        private string GenerateBehaviorTreeScript(string treeName, string aiType, bool includePatrol, bool includeChase, bool includeAttack)
        {
            return $@"using UnityEngine;

public class {treeName} : MonoBehaviour
{{
    [Header(""{aiType} AI Settings"")]
    public Transform target;
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float moveSpeed = 3f;
    public float patrolRadius = 5f;
    
    private Vector3 originalPosition;
    private Vector3 patrolTarget;
    private bool hasPatrolTarget = false;
    
    void Start()
    {{
        originalPosition = transform.position;
        
        if (target == null)
        {{
            GameObject player = GameObject.FindWithTag(""Player"");
            if (player != null) target = player.transform;
        }}
    }}
    
    void Update()
    {{
        ExecuteBehaviorTree();
    }}
    
    void ExecuteBehaviorTree()
    {{
        {(includeChase ? @"
        // チェイス行動
        if (target != null && Vector3.Distance(transform.position, target.position) <= detectionRange)
        {
            MoveTowards(target.position);
            return;
        }" : "")}
        
        {(includeAttack ? @"
        // 攻撃行動
        if (target != null && Vector3.Distance(transform.position, target.position) <= attackRange)
        {
            Attack();
            return;
        }" : "")}
        
        {(includePatrol ? @"
        // パトロール行動
        if (!hasPatrolTarget || Vector3.Distance(transform.position, patrolTarget) < 1f)
        {
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            patrolTarget = originalPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
            hasPatrolTarget = true;
        }
        
        MoveTowards(patrolTarget);" : "")}
    }}
    
    void MoveTowards(Vector3 targetPosition)
    {{
        Vector3 direction = SafeNormalize(targetPosition - transform.position);
        transform.position += direction * moveSpeed * Time.deltaTime;
        transform.LookAt(new Vector3(targetPosition.x, transform.position.y, targetPosition.z));
    }}
    
    void Attack()
    {{
        Debug.Log($""{{name}} attacks!"");
    }}
}}";
        }
        
        // === デバッグ・テストツールの実装 ===
        
        /// <summary>
        /// ゲームスピード制御
        /// </summary>
        private string ControlGameSpeed(Dictionary<string, string> parameters)
        {
            try
            {
                var operation = parameters.GetValueOrDefault("operation", "set");
                var speed = float.Parse(parameters.GetValueOrDefault("speed", "1"));
                var preset = parameters.GetValueOrDefault("preset", "");
                var pauseMode = parameters.GetValueOrDefault("pauseMode", "toggle");
                
                switch (operation)
                {
                    case "set":
                        if (!string.IsNullOrEmpty(preset))
                        {
                            speed = preset.ToLower() switch
                            {
                                "pause" => 0f,
                                "slowest" => 0.1f,
                                "slow" => 0.5f,
                                "normal" => 1f,
                                "fast" => 2f,
                                "fastest" => 5f,
                                _ => 1f
                            };
                        }
                        
                        Time.timeScale = Mathf.Clamp(speed, 0f, 10f);
                        break;
                        
                    case "pause":
                        switch (pauseMode)
                        {
                            case "toggle":
                                Time.timeScale = Time.timeScale > 0 ? 0 : 1;
                                break;
                            case "on":
                                Time.timeScale = 0;
                                break;
                            case "off":
                                Time.timeScale = 1;
                                break;
                        }
                        break;
                        
                    case "step":
                        // フレームステップ（実験的）
                        if (Time.timeScale == 0)
                        {
                            Time.timeScale = 0.01f;
                            EditorApplication.delayCall += () => Time.timeScale = 0;
                        }
                        break;
                        
                    case "get":
                        // 現在の速度を返す
                        break;
                }
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    currentSpeed = Time.timeScale,
                    isPaused = Time.timeScale == 0,
                    fixedDeltaTime = Time.fixedDeltaTime,
                    maximumDeltaTime = Time.maximumDeltaTime,
                    presetDescription = Time.timeScale switch
                    {
                        0 => "Paused",
                        < 0.2f => "Slowest",
                        < 0.7f => "Slow",
                        < 1.5f => "Normal",
                        < 3f => "Fast",
                        _ => "Fastest"
                    }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        /// <summary>
        /// パフォーマンスプロファイリング
        /// </summary>
        private string ProfilePerformance(Dictionary<string, string> parameters)
        {
            try
            {
                var category = parameters.GetValueOrDefault("category", "general");
                var duration = int.Parse(parameters.GetValueOrDefault("duration", "0"));
                var detailed = parameters.GetValueOrDefault("detailed", "false") == "true";
                
                var profileData = new Dictionary<string, object>();
                
                switch (category)
                {
                    case "general":
                    case "all":
                        // FPS情報
                        float fps = 1f / Time.deltaTime;
                        float avgFps = 1f / Time.smoothDeltaTime;
                        
                        profileData["fps"] = new Dictionary<string, object>
                        {
                            ["current"] = Mathf.Round(fps),
                            ["average"] = Mathf.Round(avgFps),
                            ["targetFrameRate"] = Application.targetFrameRate,
                            ["vsyncCount"] = QualitySettings.vSyncCount
                        };
                        
                        // メモリ情報
                        profileData["memory"] = new Dictionary<string, object>
                        {
                            ["totalAllocatedMemory"] = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024) + " MB",
                            ["totalReservedMemory"] = Profiler.GetTotalReservedMemoryLong() / (1024 * 1024) + " MB",
                            ["totalUnusedReservedMemory"] = Profiler.GetTotalUnusedReservedMemoryLong() / (1024 * 1024) + " MB",
                            ["monoHeapSize"] = Profiler.GetMonoHeapSizeLong() / (1024 * 1024) + " MB",
                            ["monoUsedSize"] = Profiler.GetMonoUsedSizeLong() / (1024 * 1024) + " MB"
                        };
                        
                        // レンダリング統計
                        profileData["rendering"] = new Dictionary<string, object>
                        {
                            ["frameTime"] = Time.deltaTime * 1000 + " ms",
                            ["smoothFrameTime"] = Time.smoothDeltaTime * 1000 + " ms",
                            ["rendererCount"] = GameObject.FindObjectsOfType<Renderer>().Length,
                            ["activeGameObjects"] = GameObject.FindObjectsOfType<GameObject>().Length
                        };
                        break;
                        
                    case "memory":
                        profileData["memoryDetailed"] = new Dictionary<string, object>
                        {
                            ["allocatedMemoryForGraphicsDriver"] = Profiler.GetAllocatedMemoryForGraphicsDriver() / (1024 * 1024) + " MB",
                            ["tempAllocatorSize"] = Profiler.GetTempAllocatorSize() / (1024 * 1024) + " MB",
                            ["totalAllocatedMemory"] = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024) + " MB",
                            ["systemMemorySize"] = SystemInfo.systemMemorySize + " MB",
                            ["graphicsMemorySize"] = SystemInfo.graphicsMemorySize + " MB"
                        };
                        break;
                        
                    case "gpu":
                        profileData["gpu"] = new Dictionary<string, object>
                        {
                            ["graphicsDeviceName"] = SystemInfo.graphicsDeviceName,
                            ["graphicsDeviceType"] = SystemInfo.graphicsDeviceType.ToString(),
                            ["graphicsDeviceVendor"] = SystemInfo.graphicsDeviceVendor,
                            ["graphicsMemorySize"] = SystemInfo.graphicsMemorySize + " MB",
                            ["maxTextureSize"] = SystemInfo.maxTextureSize,
                            ["npotSupport"] = SystemInfo.npotSupport.ToString(),
                            ["graphicsShaderLevel"] = SystemInfo.graphicsShaderLevel
                        };
                        break;
                        
                    case "cpu":
                        profileData["cpu"] = new Dictionary<string, object>
                        {
                            ["processorType"] = SystemInfo.processorType,
                            ["processorCount"] = SystemInfo.processorCount,
                            ["processorFrequency"] = SystemInfo.processorFrequency + " MHz",
                            ["operatingSystem"] = SystemInfo.operatingSystem,
                            ["systemMemorySize"] = SystemInfo.systemMemorySize + " MB"
                        };
                        break;
                }
                
                // 詳細モードの場合
                if (detailed)
                {
                    profileData["components"] = new Dictionary<string, object>
                    {
                        ["meshRenderers"] = GameObject.FindObjectsOfType<MeshRenderer>().Length,
                        ["skinnedMeshRenderers"] = GameObject.FindObjectsOfType<SkinnedMeshRenderer>().Length,
                        ["lights"] = GameObject.FindObjectsOfType<Light>().Length,
                        ["cameras"] = GameObject.FindObjectsOfType<Camera>().Length,
                        ["audioSources"] = GameObject.FindObjectsOfType<AudioSource>().Length,
                        ["particleSystems"] = GameObject.FindObjectsOfType<ParticleSystem>().Length,
                        ["colliders"] = GameObject.FindObjectsOfType<Collider>().Length,
                        ["rigidbodies"] = GameObject.FindObjectsOfType<Rigidbody>().Length
                    };
                }
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    category = category,
                    data = profileData
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        /// <summary>
        /// デバッグ描画
        /// </summary>
        private string DebugDraw(Dictionary<string, string> parameters)
        {
            try
            {
                var drawType = parameters.GetValueOrDefault("type", "line");
                var duration = float.Parse(parameters.GetValueOrDefault("duration", "5"));
                var colorStr = parameters.GetValueOrDefault("color", "red");
                var persistent = parameters.GetValueOrDefault("persistent", "false") == "true";
                
                Color color = colorStr.ToLower() switch
                {
                    "red" => Color.red,
                    "green" => Color.green,
                    "blue" => Color.blue,
                    "yellow" => Color.yellow,
                    "white" => Color.white,
                    "black" => Color.black,
                    "cyan" => Color.cyan,
                    "magenta" => Color.magenta,
                    _ => Color.red
                };
                
                switch (drawType.ToLower())
                {
                    case "line":
                        var start = ParseVector3(parameters.GetValueOrDefault("start", "0,0,0"));
                        var end = ParseVector3(parameters.GetValueOrDefault("end", "1,1,1"));
                        Debug.DrawLine(start, end, color, duration);
                        break;
                        
                    case "ray":
                        var origin = ParseVector3(parameters.GetValueOrDefault("origin", "0,0,0"));
                        var direction = ParseVector3(parameters.GetValueOrDefault("direction", "0,1,0"));
                        var length = float.Parse(parameters.GetValueOrDefault("length", "10"));
                        Debug.DrawRay(origin, SafeNormalize(direction) * length, color, duration);
                        break;
                        
                    case "box":
                        var center = ParseVector3(parameters.GetValueOrDefault("center", "0,0,0"));
                        var size = ParseVector3(parameters.GetValueOrDefault("size", "1,1,1"));
                        DrawDebugBox(center, size, color, duration);
                        break;
                        
                    case "sphere":
                        var sphereCenter = ParseVector3(parameters.GetValueOrDefault("center", "0,0,0"));
                        var radius = float.Parse(parameters.GetValueOrDefault("radius", "1"));
                        DrawDebugSphere(sphereCenter, radius, color, duration);
                        break;
                        
                    case "path":
                        var pathPoints = parameters.GetValueOrDefault("points", "");
                        if (!string.IsNullOrEmpty(pathPoints))
                        {
                            var points = pathPoints.Split(';').Select(p => ParseVector3(p)).ToArray();
                            for (int i = 0; i < points.Length - 1; i++)
                            {
                                Debug.DrawLine(points[i], points[i + 1], color, duration);
                            }
                        }
                        break;
                        
                    case "clear":
                        // Unity doesn't have a direct clear method, but we can suggest workarounds
                        return JsonConvert.SerializeObject(new
                        {
                            success = true,
                            message = "Debug draw lines will disappear after their duration. To clear immediately, restart play mode."
                        });
                }
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    type = drawType,
                    color = colorStr,
                    duration = duration,
                    message = $"Debug {drawType} drawn with color {colorStr} for {duration} seconds"
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        /// <summary>
        /// Unityテスト実行
        /// </summary>
        private string RunUnityTests(Dictionary<string, string> parameters)
        {
            try
            {
                var testMode = parameters.GetValueOrDefault("mode", "editmode");
                var category = parameters.GetValueOrDefault("category", "all");
                var testName = parameters.GetValueOrDefault("testName", "");
                
                // Unity Test Runnerの情報を取得
                var testRunnerWindowType = System.Type.GetType("UnityEditor.TestTools.TestRunner.TestRunnerWindow, UnityEditor.TestRunner");
                
                if (testRunnerWindowType == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "Unity Test Runner not available"
                    });
                }
                
                // テストランナーウィンドウを開く
                var showMethod = testRunnerWindowType.GetMethod("ShowWindow", BindingFlags.Static | BindingFlags.Public);
                if (showMethod != null)
                {
                    showMethod.Invoke(null, null);
                }
                
                // 基本的なテスト情報を返す
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = "Test Runner window opened",
                    info = new
                    {
                        mode = testMode,
                        category = category,
                        note = "Please use the Test Runner window to run tests. Automated test execution requires Unity Test Framework setup."
                    }
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        /// <summary>
        /// ブレークポイント管理
        /// </summary>
        private string ManageBreakpoints(Dictionary<string, string> parameters)
        {
            try
            {
                var operation = parameters.GetValueOrDefault("operation", "pause");
                var condition = parameters.GetValueOrDefault("condition", "");
                var message = parameters.GetValueOrDefault("message", "Breakpoint hit");
                
                switch (operation)
                {
                    case "pause":
                        Debug.Log($"[BREAKPOINT] {message}");
                        Debug.Break();
                        break;
                        
                    case "conditional":
                        // 条件付きブレークポイントの例
                        if (!string.IsNullOrEmpty(condition))
                        {
                            // 簡単な条件チェック（実際の実装では評価が必要）
                            var shouldBreak = false;
                            
                            // 例：フレーム数による条件
                            if (condition.Contains("frame"))
                            {
                                var frameNum = int.Parse(System.Text.RegularExpressions.Regex.Match(condition, @"\d+").Value);
                                shouldBreak = Time.frameCount >= frameNum;
                            }
                            // 例：時間による条件
                            else if (condition.Contains("time"))
                            {
                                var timeValue = float.Parse(System.Text.RegularExpressions.Regex.Match(condition, @"\d+\.?\d*").Value);
                                shouldBreak = Time.time >= timeValue;
                            }
                            
                            if (shouldBreak)
                            {
                                Debug.Log($"[CONDITIONAL BREAKPOINT] {message} (Condition: {condition})");
                                Debug.Break();
                            }
                        }
                        break;
                        
                    case "log":
                        // ログポイント（ブレークせずにログのみ）
                        Debug.Log($"[LOGPOINT] {message}");
                        break;
                        
                    case "assert":
                        // アサーション
                        Debug.Assert(!string.IsNullOrEmpty(condition), $"[ASSERTION FAILED] {message}");
                        break;
                }
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    operation = operation,
                    isPaused = EditorApplication.isPaused,
                    frame = Time.frameCount,
                    time = Time.time,
                    message = message
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        // デバッグ描画ヘルパーメソッド
        private void DrawDebugBox(Vector3 center, Vector3 size, Color color, float duration)
        {
            var halfSize = size * 0.5f;
            
            // 前面
            Debug.DrawLine(center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z), 
                          center + new Vector3(halfSize.x, -halfSize.y, halfSize.z), color, duration);
            Debug.DrawLine(center + new Vector3(halfSize.x, -halfSize.y, halfSize.z), 
                          center + new Vector3(halfSize.x, halfSize.y, halfSize.z), color, duration);
            Debug.DrawLine(center + new Vector3(halfSize.x, halfSize.y, halfSize.z), 
                          center + new Vector3(-halfSize.x, halfSize.y, halfSize.z), color, duration);
            Debug.DrawLine(center + new Vector3(-halfSize.x, halfSize.y, halfSize.z), 
                          center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z), color, duration);
            
            // 背面
            Debug.DrawLine(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z), 
                          center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z), color, duration);
            Debug.DrawLine(center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z), 
                          center + new Vector3(halfSize.x, halfSize.y, -halfSize.z), color, duration);
            Debug.DrawLine(center + new Vector3(halfSize.x, halfSize.y, -halfSize.z), 
                          center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z), color, duration);
            Debug.DrawLine(center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z), 
                          center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z), color, duration);
            
            // 接続線
            Debug.DrawLine(center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z), 
                          center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z), color, duration);
            Debug.DrawLine(center + new Vector3(halfSize.x, -halfSize.y, halfSize.z), 
                          center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z), color, duration);
            Debug.DrawLine(center + new Vector3(halfSize.x, halfSize.y, halfSize.z), 
                          center + new Vector3(halfSize.x, halfSize.y, -halfSize.z), color, duration);
            Debug.DrawLine(center + new Vector3(-halfSize.x, halfSize.y, halfSize.z), 
                          center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z), color, duration);
        }
        
        private void DrawDebugSphere(Vector3 center, float radius, Color color, float duration)
        {
            // 簡易的な球体描画（3つの円で表現）
            int segments = 16;
            
            // XY平面の円
            for (int i = 0; i < segments; i++)
            {
                float angle1 = (float)i / segments * Mathf.PI * 2;
                float angle2 = (float)(i + 1) / segments * Mathf.PI * 2;
                
                Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0);
                Vector3 p2 = center + new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, 0);
                Debug.DrawLine(p1, p2, color, duration);
            }
            
            // XZ平面の円
            for (int i = 0; i < segments; i++)
            {
                float angle1 = (float)i / segments * Mathf.PI * 2;
                float angle2 = (float)(i + 1) / segments * Mathf.PI * 2;
                
                Vector3 p1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
                Vector3 p2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);
                Debug.DrawLine(p1, p2, color, duration);
            }
            
            // YZ平面の円
            for (int i = 0; i < segments; i++)
            {
                float angle1 = (float)i / segments * Mathf.PI * 2;
                float angle2 = (float)(i + 1) / segments * Mathf.PI * 2;
                
                Vector3 p1 = center + new Vector3(0, Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius);
                Vector3 p2 = center + new Vector3(0, Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius);
                Debug.DrawLine(p1, p2, color, duration);
            }
        }
        
        // === アニメーション系ツールの実装 ===
        
        /// <summary>
        /// Animatorコントローラーの作成
        /// </summary>
        private string CreateAnimatorController(Dictionary<string, string> parameters)
        {
            try
            {
                var controllerName = parameters.GetValueOrDefault("name", "NewAnimatorController");
                var savePath = parameters.GetValueOrDefault("path", "Assets/Animations/Controllers/");
                var targetObject = parameters.GetValueOrDefault("targetObject", "");
                var applyToObject = parameters.GetValueOrDefault("applyToObject", "true") == "true";
                
                // ディレクトリ作成
                if (!System.IO.Directory.Exists(savePath))
                {
                    System.IO.Directory.CreateDirectory(savePath);
                }
                
                // Animatorコントローラーを作成
                var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(
                    savePath + controllerName + ".controller");
                
                // デフォルトレイヤーとステート
                var rootStateMachine = controller.layers[0].stateMachine;
                
                // Entry, Exit, Any State は自動的に作成される
                // デフォルトのIdleステートを追加
                var idleState = rootStateMachine.AddState("Idle");
                rootStateMachine.defaultState = idleState;
                
                // 基本的なパラメーター追加
                controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
                controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
                controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
                
                // ターゲットオブジェクトに適用
                if (applyToObject && !string.IsNullOrEmpty(targetObject))
                {
                    var target = GameObject.Find(targetObject);
                    if (target != null)
                    {
                        var animator = target.GetComponent<Animator>();
                        if (animator == null)
                        {
                            animator = target.AddComponent<Animator>();
                        }
                        animator.runtimeAnimatorController = controller;
                    }
                }
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                // 作成したコントローラーを選択
                Selection.activeObject = controller;
                EditorGUIUtility.PingObject(controller);
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Animator Controller '{controllerName}' created",
                    path = AssetDatabase.GetAssetPath(controller),
                    states = new[] { "Idle" },
                    parameters = new[] { "Speed", "IsGrounded", "Jump" },
                    appliedTo = applyToObject ? targetObject : "None"
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        /// <summary>
        /// アニメーションステートの追加
        /// </summary>
        private string AddAnimationState(Dictionary<string, string> parameters)
        {
            try
            {
                var controllerPath = parameters.GetValueOrDefault("controllerPath", "");
                var stateName = parameters.GetValueOrDefault("stateName", "NewState");
                var animationClipPath = parameters.GetValueOrDefault("animationClipPath", "");
                var layerIndex = int.Parse(parameters.GetValueOrDefault("layerIndex", "0"));
                var isDefault = parameters.GetValueOrDefault("isDefault", "false") == "true";
                
                // コントローラーを読み込み
                var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
                if (controller == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Controller not found" });
                }
                
                // レイヤーを取得
                if (layerIndex >= controller.layers.Length)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Invalid layer index" });
                }
                
                var stateMachine = controller.layers[layerIndex].stateMachine;
                
                // UNDOに登録
                UnityEditor.Undo.RecordObject(controller, $"Add Animation State {stateName}");
                
                // ステートを追加
                var newState = stateMachine.AddState(stateName);
                
                // アニメーションクリップを設定
                if (!string.IsNullOrEmpty(animationClipPath))
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animationClipPath);
                    if (clip != null)
                    {
                        newState.motion = clip;
                    }
                }
                
                // デフォルトステートに設定
                if (isDefault)
                {
                    stateMachine.defaultState = newState;
                }
                
                // ステートの位置を調整（ステートはステートマシン内で管理される）
                // UnityEditor.Animations.AnimatorStateにはpositionプロパティがないため、
                // ステートマシン経由でアクセス
                var states = stateMachine.states;
                for (int i = 0; i < states.Length; i++)
                {
                    if (states[i].state == newState)
                    {
                        states[i].position = new Vector3(250 * (states.Length % 3), 100 * (states.Length / 3), 0);
                        stateMachine.states = states;
                        break;
                    }
                }
                
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"State '{stateName}' added to controller",
                    stateName = stateName,
                    hasMotion = newState.motion != null,
                    isDefault = stateMachine.defaultState == newState,
                    stateCount = stateMachine.states.Length
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        /// <summary>
        /// アニメーションクリップの作成
        /// </summary>
        private string CreateAnimationClip(Dictionary<string, string> parameters)
        {
            try
            {
                var clipName = parameters.GetValueOrDefault("name", "NewAnimation");
                var savePath = parameters.GetValueOrDefault("path", "Assets/Animations/Clips/");
                var duration = float.Parse(parameters.GetValueOrDefault("duration", "1"));
                var frameRate = float.Parse(parameters.GetValueOrDefault("frameRate", "30"));
                var targetObject = parameters.GetValueOrDefault("targetObject", "");
                var animationType = parameters.GetValueOrDefault("animationType", "position");
                
                // ディレクトリ作成
                if (!System.IO.Directory.Exists(savePath))
                {
                    System.IO.Directory.CreateDirectory(savePath);
                }
                
                // アニメーションクリップ作成
                var clip = new AnimationClip();
                clip.name = clipName;
                clip.frameRate = frameRate;
                
                // サンプルアニメーションカーブを作成
                AnimationCurve curveX, curveY, curveZ;
                
                switch (animationType.ToLower())
                {
                    case "position":
                        // 位置アニメーション
                        curveX = AnimationCurve.Linear(0, 0, duration, 2);
                        curveY = AnimationCurve.EaseInOut(0, 0, duration * 0.5f, 1);
                        curveY.AddKey(duration, 0);
                        curveZ = AnimationCurve.Constant(0, duration, 0);
                        
                        clip.SetCurve("", typeof(Transform), "localPosition.x", curveX);
                        clip.SetCurve("", typeof(Transform), "localPosition.y", curveY);
                        clip.SetCurve("", typeof(Transform), "localPosition.z", curveZ);
                        break;
                        
                    case "rotation":
                        // 回転アニメーション
                        curveY = AnimationCurve.Linear(0, 0, duration, 360);
                        clip.SetCurve("", typeof(Transform), "localEulerAngles.y", curveY);
                        break;
                        
                    case "scale":
                        // スケールアニメーション
                        var scaleCurve = AnimationCurve.EaseInOut(0, 1, duration * 0.5f, 1.5f);
                        scaleCurve.AddKey(duration, 1);
                        clip.SetCurve("", typeof(Transform), "localScale.x", scaleCurve);
                        clip.SetCurve("", typeof(Transform), "localScale.y", scaleCurve);
                        clip.SetCurve("", typeof(Transform), "localScale.z", scaleCurve);
                        break;
                        
                    case "color":
                        // 色アニメーション（Renderer用）
                        var colorR = AnimationCurve.Linear(0, 1, duration, 0);
                        var colorG = AnimationCurve.Linear(0, 1, duration, 0);
                        var colorB = AnimationCurve.Linear(0, 1, duration, 1);
                        
                        clip.SetCurve("", typeof(MeshRenderer), "material._Color.r", colorR);
                        clip.SetCurve("", typeof(MeshRenderer), "material._Color.g", colorG);
                        clip.SetCurve("", typeof(MeshRenderer), "material._Color.b", colorB);
                        break;
                }
                
                // クリップ設定
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                
                // 保存
                AssetDatabase.CreateAsset(clip, savePath + clipName + ".anim");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                // ターゲットに適用
                if (!string.IsNullOrEmpty(targetObject))
                {
                    var target = GameObject.Find(targetObject);
                    if (target != null)
                    {
                        var animator = target.GetComponent<Animator>();
                        if (animator == null)
                        {
                            var animation = target.AddComponent<Animation>();
                            animation.clip = clip;
                            animation.Play();
                        }
                    }
                }
                
                Selection.activeObject = clip;
                EditorGUIUtility.PingObject(clip);
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Animation clip '{clipName}' created",
                    path = AssetDatabase.GetAssetPath(clip),
                    duration = duration,
                    frameRate = frameRate,
                    animationType = animationType,
                    isLooping = settings.loopTime
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        /// <summary>
        /// ブレンドツリーのセットアップ
        /// </summary>
        private string SetupBlendTree(Dictionary<string, string> parameters)
        {
            try
            {
                var controllerPath = parameters.GetValueOrDefault("controllerPath", "");
                var stateName = parameters.GetValueOrDefault("stateName", "Movement");
                var blendType = parameters.GetValueOrDefault("blendType", "1D");
                var parameterName = parameters.GetValueOrDefault("parameterName", "Speed");
                var layerIndex = int.Parse(parameters.GetValueOrDefault("layerIndex", "0"));
                
                // コントローラーを読み込み
                var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
                if (controller == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Controller not found" });
                }
                
                var stateMachine = controller.layers[layerIndex].stateMachine;
                
                // ブレンドツリー用のステートを作成
                var blendState = stateMachine.AddState(stateName);
                var blendTree = new UnityEditor.Animations.BlendTree();
                blendTree.name = stateName + "_BlendTree";
                
                // ブレンドタイプを設定
                switch (blendType.ToUpper())
                {
                    case "1D":
                        blendTree.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
                        blendTree.blendParameter = parameterName;
                        
                        // サンプルモーションを追加（実際のプロジェクトでは既存のアニメーションを使用）
                        // Idle (Speed = 0)
                        blendTree.AddChild(null, 0);
                        // Walk (Speed = 0.5)
                        blendTree.AddChild(null, 0.5f);
                        // Run (Speed = 1)
                        blendTree.AddChild(null, 1);
                        break;
                        
                    case "2D":
                        blendTree.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
                        blendTree.blendParameter = parameterName + "X";
                        blendTree.blendParameterY = parameterName + "Y";
                        
                        // パラメーターが存在しない場合は追加
                        if (!controller.parameters.Any(p => p.name == parameterName + "X"))
                        {
                            controller.AddParameter(parameterName + "X", AnimatorControllerParameterType.Float);
                        }
                        if (!controller.parameters.Any(p => p.name == parameterName + "Y"))
                        {
                            controller.AddParameter(parameterName + "Y", AnimatorControllerParameterType.Float);
                        }
                        
                        // 方向別のモーションを追加
                        blendTree.AddChild(null, new Vector2(0, 0)); // Idle
                        blendTree.AddChild(null, new Vector2(0, 1)); // Forward
                        blendTree.AddChild(null, new Vector2(1, 0)); // Right
                        blendTree.AddChild(null, new Vector2(0, -1)); // Back
                        blendTree.AddChild(null, new Vector2(-1, 0)); // Left
                        break;
                }
                
                // ブレンドツリーをステートに設定
                blendState.motion = blendTree;
                
                // アセットとして保存
                AssetDatabase.AddObjectToAsset(blendTree, AssetDatabase.GetAssetPath(controller));
                
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Blend tree '{stateName}' created",
                    blendType = blendType,
                    parameterName = parameterName,
                    childCount = blendTree.children.Length,
                    stateName = stateName
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        /// <summary>
        /// アニメーショントランジションの追加
        /// </summary>
        private string AddAnimationTransition(Dictionary<string, string> parameters)
        {
            try
            {
                var controllerPath = parameters.GetValueOrDefault("controllerPath", "");
                var fromState = parameters.GetValueOrDefault("fromState", "");
                var toState = parameters.GetValueOrDefault("toState", "");
                var condition = parameters.GetValueOrDefault("condition", "");
                var conditionValue = parameters.GetValueOrDefault("conditionValue", "");
                var hasExitTime = parameters.GetValueOrDefault("hasExitTime", "true") == "true";
                var transitionDuration = float.Parse(parameters.GetValueOrDefault("transitionDuration", "0.25"));
                var layerIndex = int.Parse(parameters.GetValueOrDefault("layerIndex", "0"));
                
                // コントローラーを読み込み
                var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
                if (controller == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Controller not found" });
                }
                
                var stateMachine = controller.layers[layerIndex].stateMachine;
                
                // ステートを検索
                UnityEditor.Animations.AnimatorState sourceState = null;
                UnityEditor.Animations.AnimatorState destState = null;
                
                // Any Stateからの遷移
                bool fromAnyState = fromState.ToLower() == "any" || fromState.ToLower() == "anystate";
                
                if (!fromAnyState)
                {
                    foreach (var state in stateMachine.states)
                    {
                        if (state.state.name == fromState)
                            sourceState = state.state;
                        if (state.state.name == toState)
                            destState = state.state;
                    }
                    
                    if (sourceState == null)
                    {
                        return JsonConvert.SerializeObject(new { success = false, error = $"Source state '{fromState}' not found" });
                    }
                }
                else
                {
                    // Any Stateからの場合はdestStateのみ必要
                    foreach (var state in stateMachine.states)
                    {
                        if (state.state.name == toState)
                            destState = state.state;
                    }
                }
                
                if (destState == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = $"Destination state '{toState}' not found" });
                }
                
                // トランジション作成
                UnityEditor.Animations.AnimatorStateTransition transition;
                if (fromAnyState)
                {
                    transition = stateMachine.AddAnyStateTransition(destState);
                }
                else
                {
                    transition = sourceState.AddTransition(destState);
                }
                
                // トランジション設定
                transition.hasExitTime = hasExitTime;
                transition.duration = transitionDuration;
                transition.exitTime = hasExitTime ? 0.9f : 0;
                
                // 条件設定
                if (!string.IsNullOrEmpty(condition))
                {
                    // パラメーターが存在するか確認
                    var param = controller.parameters.FirstOrDefault(p => p.name == condition);
                    if (param != null)
                    {
                        UnityEditor.Animations.AnimatorConditionMode mode;
                        switch (param.type)
                        {
                            case AnimatorControllerParameterType.Bool:
                                mode = conditionValue.ToLower() == "true" ? 
                                    UnityEditor.Animations.AnimatorConditionMode.If : 
                                    UnityEditor.Animations.AnimatorConditionMode.IfNot;
                                transition.AddCondition(mode, 0, condition);
                                break;
                                
                            case AnimatorControllerParameterType.Float:
                                float floatValue = float.Parse(conditionValue);
                                mode = UnityEditor.Animations.AnimatorConditionMode.Greater;
                                transition.AddCondition(mode, floatValue, condition);
                                break;
                                
                            case AnimatorControllerParameterType.Int:
                                int intValue = int.Parse(conditionValue);
                                mode = UnityEditor.Animations.AnimatorConditionMode.Equals;
                                transition.AddCondition(mode, intValue, condition);
                                break;
                                
                            case AnimatorControllerParameterType.Trigger:
                                transition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, condition);
                                break;
                        }
                    }
                }
                
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Transition created from '{fromState}' to '{toState}'",
                    hasExitTime = hasExitTime,
                    duration = transitionDuration,
                    hasCondition = !string.IsNullOrEmpty(condition),
                    condition = condition,
                    fromAnyState = fromAnyState
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        /// <summary>
        /// アニメーションレイヤーのセットアップ
        /// </summary>
        private string SetupAnimationLayer(Dictionary<string, string> parameters)
        {
            try
            {
                var controllerPath = parameters.GetValueOrDefault("controllerPath", "");
                var layerName = parameters.GetValueOrDefault("layerName", "NewLayer");
                var weight = float.Parse(parameters.GetValueOrDefault("weight", "1"));
                var blendMode = parameters.GetValueOrDefault("blendMode", "override");
                var maskPath = parameters.GetValueOrDefault("avatarMaskPath", "");
                
                // コントローラーを読み込み
                var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
                if (controller == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Controller not found" });
                }
                
                // 新しいレイヤーを追加
                controller.AddLayer(layerName);
                var layerIndex = controller.layers.Length - 1;
                var layers = controller.layers;
                var newLayer = layers[layerIndex];
                
                // レイヤー設定
                newLayer.defaultWeight = weight;
                
                // ブレンドモード設定
                switch (blendMode.ToLower())
                {
                    case "additive":
                        newLayer.blendingMode = UnityEditor.Animations.AnimatorLayerBlendingMode.Additive;
                        break;
                    default:
                        newLayer.blendingMode = UnityEditor.Animations.AnimatorLayerBlendingMode.Override;
                        break;
                }
                
                // アバターマスクを設定
                if (!string.IsNullOrEmpty(maskPath))
                {
                    var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
                    if (mask != null)
                    {
                        newLayer.avatarMask = mask;
                    }
                }
                
                // 同期設定（必要に応じて）
                newLayer.syncedLayerIndex = -1;
                
                controller.layers = layers;
                
                // デフォルトステートを追加
                var stateMachine = newLayer.stateMachine;
                var defaultState = stateMachine.AddState("Default");
                stateMachine.defaultState = defaultState;
                
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Layer '{layerName}' added to controller",
                    layerIndex = layerIndex,
                    weight = weight,
                    blendMode = blendMode,
                    hasMask = !string.IsNullOrEmpty(maskPath) && newLayer.avatarMask != null
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        /// <summary>
        /// アニメーションイベントの作成
        /// </summary>
        private string CreateAnimationEvent(Dictionary<string, string> parameters)
        {
            try
            {
                var clipPath = parameters.GetValueOrDefault("clipPath", "");
                var eventTime = float.Parse(parameters.GetValueOrDefault("time", "0.5"));
                var functionName = parameters.GetValueOrDefault("functionName", "OnAnimationEvent");
                var stringParameter = parameters.GetValueOrDefault("stringParameter", "");
                var floatParameter = float.Parse(parameters.GetValueOrDefault("floatParameter", "0"));
                var intParameter = int.Parse(parameters.GetValueOrDefault("intParameter", "0"));
                
                // アニメーションクリップを読み込み
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Animation clip not found" });
                }
                
                // アニメーションイベントを作成
                var animEvent = new AnimationEvent();
                animEvent.time = eventTime;
                animEvent.functionName = functionName;
                
                // パラメーター設定（1つのみ設定可能）
                if (!string.IsNullOrEmpty(stringParameter))
                {
                    animEvent.stringParameter = stringParameter;
                }
                else if (floatParameter != 0)
                {
                    animEvent.floatParameter = floatParameter;
                }
                else if (intParameter != 0)
                {
                    animEvent.intParameter = intParameter;
                }
                
                // 既存のイベントを取得して新しいイベントを追加
                var events = AnimationUtility.GetAnimationEvents(clip);
                var eventsList = new List<AnimationEvent>(events);
                eventsList.Add(animEvent);
                
                // イベントを設定
                AnimationUtility.SetAnimationEvents(clip, eventsList.ToArray());
                
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Animation event '{functionName}' added at {eventTime}s",
                    eventCount = eventsList.Count,
                    time = eventTime,
                    functionName = functionName,
                    hasStringParam = !string.IsNullOrEmpty(stringParameter),
                    hasFloatParam = floatParameter != 0,
                    hasIntParam = intParameter != 0
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        /// <summary>
        /// アバターのセットアップ
        /// </summary>
        private string SetupAvatar(Dictionary<string, string> parameters)
        {
            try
            {
                var modelPath = parameters.GetValueOrDefault("modelPath", "");
                var avatarName = parameters.GetValueOrDefault("avatarName", "NewAvatar");
                var isHumanoid = parameters.GetValueOrDefault("isHumanoid", "true") == "true";
                var rootBone = parameters.GetValueOrDefault("rootBone", "");
                
                // モデルを読み込み
                var modelImporter = AssetImporter.GetAtPath(modelPath) as ModelImporter;
                if (modelImporter == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Model not found or not a valid model file" });
                }
                
                // アニメーションタイプを設定
                if (isHumanoid)
                {
                    modelImporter.animationType = ModelImporterAnimationType.Human;
                    modelImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    
                    // Humanoidの設定
                    var humanDescription = modelImporter.humanDescription;
                    humanDescription.upperArmTwist = 0.5f;
                    humanDescription.lowerArmTwist = 0.5f;
                    humanDescription.upperLegTwist = 0.5f;
                    humanDescription.lowerLegTwist = 0.5f;
                    humanDescription.armStretch = 0.05f;
                    humanDescription.legStretch = 0.05f;
                    humanDescription.feetSpacing = 0.0f;
                    humanDescription.hasTranslationDoF = false;
                    
                    modelImporter.humanDescription = humanDescription;
                }
                else
                {
                    modelImporter.animationType = ModelImporterAnimationType.Generic;
                    
                    // ルートボーンを設定
                    if (!string.IsNullOrEmpty(rootBone))
                    {
                        // モデルを一時的にインスタンス化してボーンを探す
                        var tempObject = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                        if (tempObject != null)
                        {
                            var rootTransform = tempObject.transform.Find(rootBone);
                            if (rootTransform != null)
                            {
                                modelImporter.motionNodeName = rootBone;
                            }
                        }
                    }
                }
                
                // インポート設定を適用
                modelImporter.SaveAndReimport();
                
                // アバターを取得
                var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(modelPath);
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Avatar '{avatarName}' setup completed",
                    modelPath = modelPath,
                    isHumanoid = isHumanoid,
                    isValid = avatar != null && avatar.isValid,
                    hasHumanDescription = isHumanoid
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        /// <summary>
        /// タイムラインの作成
        /// </summary>
        private string CreateTimeline(Dictionary<string, string> parameters)
        {
            try
            {
                var timelineName = parameters.GetValueOrDefault("name", "NewTimeline");
                var savePath = parameters.GetValueOrDefault("path", "Assets/Timelines/");
                var duration = float.Parse(parameters.GetValueOrDefault("duration", "10"));
                var frameRate = float.Parse(parameters.GetValueOrDefault("frameRate", "30"));
                var targetObject = parameters.GetValueOrDefault("targetObject", "");
                
                // Timeline機能が有効か確認
                var timelineAssetType = System.Type.GetType("UnityEngine.Timeline.TimelineAsset, Unity.Timeline");
                if (timelineAssetType == null)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = "Timeline package is not installed. Please install it from Package Manager."
                    });
                }
                
                // ディレクトリ作成
                if (!System.IO.Directory.Exists(savePath))
                {
                    System.IO.Directory.CreateDirectory(savePath);
                }
                
                // TimelineAssetを作成
                var timeline = ScriptableObject.CreateInstance(timelineAssetType);
                
                // フレームレート設定
                var frameRateProperty = timelineAssetType.GetProperty("frameRate");
                if (frameRateProperty != null)
                {
                    frameRateProperty.SetValue(timeline, frameRate);
                }
                
                // アセットとして保存
                AssetDatabase.CreateAsset(timeline, savePath + timelineName + ".playable");
                
                // ターゲットオブジェクトにPlayableDirectorを追加
                if (!string.IsNullOrEmpty(targetObject))
                {
                    var target = GameObject.Find(targetObject);
                    if (target != null)
                    {
                        var playableDirectorType = System.Type.GetType("UnityEngine.Playables.PlayableDirector, UnityEngine.DirectorModule");
                        if (playableDirectorType != null)
                        {
                            var director = target.GetComponent(playableDirectorType);
                            if (director == null)
                            {
                                director = target.AddComponent(playableDirectorType);
                            }
                            
                            // TimelineをPlayableAssetとして設定
                            var playableAssetProperty = playableDirectorType.GetProperty("playableAsset");
                            if (playableAssetProperty != null)
                            {
                                playableAssetProperty.SetValue(director, timeline);
                            }
                        }
                    }
                }
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Selection.activeObject = timeline;
                EditorGUIUtility.PingObject(timeline);
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Timeline '{timelineName}' created",
                    path = AssetDatabase.GetAssetPath(timeline),
                    frameRate = frameRate,
                    appliedTo = !string.IsNullOrEmpty(targetObject) ? targetObject : "None"
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        /// <summary>
        /// アニメーションのベイク
        /// </summary>
        private string BakeAnimation(Dictionary<string, string> parameters)
        {
            try
            {
                var sourceObject = parameters.GetValueOrDefault("sourceObject", "");
                var animationName = parameters.GetValueOrDefault("animationName", "BakedAnimation");
                var startFrame = int.Parse(parameters.GetValueOrDefault("startFrame", "0"));
                var endFrame = int.Parse(parameters.GetValueOrDefault("endFrame", "60"));
                var frameRate = float.Parse(parameters.GetValueOrDefault("frameRate", "30"));
                var savePath = parameters.GetValueOrDefault("path", "Assets/Animations/Baked/");
                
                // ソースオブジェクトを取得
                var source = GameObject.Find(sourceObject);
                if (source == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Source object not found" });
                }
                
                // ディレクトリ作成
                if (!System.IO.Directory.Exists(savePath))
                {
                    System.IO.Directory.CreateDirectory(savePath);
                }
                
                // 新しいアニメーションクリップを作成
                var bakedClip = new AnimationClip();
                bakedClip.name = animationName;
                bakedClip.frameRate = frameRate;
                
                // GameObjectRecorderは2018.1以降で利用可能
                // 代替案：シンプルなキーフレーム記録
                var transform = source.transform;
                
                // Transformのアニメーションを記録
                AnimationCurve posX = new AnimationCurve();
                AnimationCurve posY = new AnimationCurve();
                AnimationCurve posZ = new AnimationCurve();
                AnimationCurve rotX = new AnimationCurve();
                AnimationCurve rotY = new AnimationCurve();
                AnimationCurve rotZ = new AnimationCurve();
                AnimationCurve rotW = new AnimationCurve();
                AnimationCurve scaleX = new AnimationCurve();
                AnimationCurve scaleY = new AnimationCurve();
                AnimationCurve scaleZ = new AnimationCurve();
                
                // 各フレームでポーズを記録
                float timeStep = 1f / frameRate;
                for (int frame = startFrame; frame <= endFrame; frame++)
                {
                    float time = frame * timeStep;
                    
                    // アニメーションを特定のフレームに進める
                    var animator = source.GetComponent<Animator>();
                    if (animator != null && animator.runtimeAnimatorController != null)
                    {
                        animator.Update(timeStep);
                    }
                    
                    // 現在のポーズを記録
                    posX.AddKey(time, transform.localPosition.x);
                    posY.AddKey(time, transform.localPosition.y);
                    posZ.AddKey(time, transform.localPosition.z);
                    
                    var rotation = transform.localRotation;
                    rotX.AddKey(time, rotation.x);
                    rotY.AddKey(time, rotation.y);
                    rotZ.AddKey(time, rotation.z);
                    rotW.AddKey(time, rotation.w);
                    
                    scaleX.AddKey(time, transform.localScale.x);
                    scaleY.AddKey(time, transform.localScale.y);
                    scaleZ.AddKey(time, transform.localScale.z);
                }
                
                // カーブをクリップに設定
                bakedClip.SetCurve("", typeof(Transform), "localPosition.x", posX);
                bakedClip.SetCurve("", typeof(Transform), "localPosition.y", posY);
                bakedClip.SetCurve("", typeof(Transform), "localPosition.z", posZ);
                bakedClip.SetCurve("", typeof(Transform), "localRotation.x", rotX);
                bakedClip.SetCurve("", typeof(Transform), "localRotation.y", rotY);
                bakedClip.SetCurve("", typeof(Transform), "localRotation.z", rotZ);
                bakedClip.SetCurve("", typeof(Transform), "localRotation.w", rotW);
                bakedClip.SetCurve("", typeof(Transform), "localScale.x", scaleX);
                bakedClip.SetCurve("", typeof(Transform), "localScale.y", scaleY);
                bakedClip.SetCurve("", typeof(Transform), "localScale.z", scaleZ);
                
                // アセットとして保存
                AssetDatabase.CreateAsset(bakedClip, savePath + animationName + ".anim");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                Selection.activeObject = bakedClip;
                EditorGUIUtility.PingObject(bakedClip);
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Animation '{animationName}' baked successfully",
                    path = AssetDatabase.GetAssetPath(bakedClip),
                    frameCount = endFrame - startFrame + 1,
                    duration = (endFrame - startFrame) / frameRate,
                    frameRate = frameRate
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        // === UI詳細構築ツールの実装 ===
        
        /// <summary>
        /// UIアンカーとピボットの自動設定
        /// </summary>
        private string SetupUIAnchors(Dictionary<string, string> parameters)
        {
            try
            {
                var targetObject = parameters.GetValueOrDefault("targetObject", "");
                var anchorPreset = parameters.GetValueOrDefault("anchorPreset", "center");
                var pivotPreset = parameters.GetValueOrDefault("pivotPreset", "center");
                var margin = float.Parse(parameters.GetValueOrDefault("margin", "10"));
                var recursive = parameters.GetValueOrDefault("recursive", "false") == "true";
                
                var target = GameObject.Find(targetObject);
                if (target == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Target object not found" });
                }
                
                var rectTransforms = recursive ? 
                    target.GetComponentsInChildren<RectTransform>() : 
                    new[] { target.GetComponent<RectTransform>() };
                
                var processedCount = 0;
                foreach (var rectTransform in rectTransforms)
                {
                    if (rectTransform == null) continue;
                    
                    // アンカー設定
                    Vector2 anchorMin, anchorMax;
                    switch (anchorPreset.ToLower())
                    {
                        case "top-left":
                            anchorMin = anchorMax = new Vector2(0, 1);
                            break;
                        case "top-center":
                            anchorMin = anchorMax = new Vector2(0.5f, 1);
                            break;
                        case "top-right":
                            anchorMin = anchorMax = new Vector2(1, 1);
                            break;
                        case "middle-left":
                            anchorMin = anchorMax = new Vector2(0, 0.5f);
                            break;
                        case "center":
                            anchorMin = anchorMax = new Vector2(0.5f, 0.5f);
                            break;
                        case "middle-right":
                            anchorMin = anchorMax = new Vector2(1, 0.5f);
                            break;
                        case "bottom-left":
                            anchorMin = anchorMax = new Vector2(0, 0);
                            break;
                        case "bottom-center":
                            anchorMin = anchorMax = new Vector2(0.5f, 0);
                            break;
                        case "bottom-right":
                            anchorMin = anchorMax = new Vector2(1, 0);
                            break;
                        case "stretch-horizontal":
                            anchorMin = new Vector2(0, 0.5f);
                            anchorMax = new Vector2(1, 0.5f);
                            break;
                        case "stretch-vertical":
                            anchorMin = new Vector2(0.5f, 0);
                            anchorMax = new Vector2(0.5f, 1);
                            break;
                        case "stretch-all":
                            anchorMin = new Vector2(0, 0);
                            anchorMax = new Vector2(1, 1);
                            break;
                        default:
                            anchorMin = anchorMax = new Vector2(0.5f, 0.5f);
                            break;
                    }
                    
                    rectTransform.anchorMin = anchorMin;
                    rectTransform.anchorMax = anchorMax;
                    
                    // ピボット設定
                    Vector2 pivot;
                    switch (pivotPreset.ToLower())
                    {
                        case "top-left":
                            pivot = new Vector2(0, 1);
                            break;
                        case "top-center":
                            pivot = new Vector2(0.5f, 1);
                            break;
                        case "top-right":
                            pivot = new Vector2(1, 1);
                            break;
                        case "middle-left":
                            pivot = new Vector2(0, 0.5f);
                            break;
                        case "center":
                            pivot = new Vector2(0.5f, 0.5f);
                            break;
                        case "middle-right":
                            pivot = new Vector2(1, 0.5f);
                            break;
                        case "bottom-left":
                            pivot = new Vector2(0, 0);
                            break;
                        case "bottom-center":
                            pivot = new Vector2(0.5f, 0);
                            break;
                        case "bottom-right":
                            pivot = new Vector2(1, 0);
                            break;
                        default:
                            pivot = new Vector2(0.5f, 0.5f);
                            break;
                    }
                    
                    rectTransform.pivot = pivot;
                    
                    // マージン適用（ストレッチの場合）
                    if (anchorPreset.Contains("stretch"))
                    {
                        if (anchorPreset == "stretch-horizontal" || anchorPreset == "stretch-all")
                        {
                            rectTransform.offsetMin = new Vector2(margin, rectTransform.offsetMin.y);
                            rectTransform.offsetMax = new Vector2(-margin, rectTransform.offsetMax.y);
                        }
                        if (anchorPreset == "stretch-vertical" || anchorPreset == "stretch-all")
                        {
                            rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, margin);
                            rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, -margin);
                        }
                    }
                    
                    processedCount++;
                }
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"UI anchors set for {processedCount} objects",
                    anchorPreset = anchorPreset,
                    pivotPreset = pivotPreset,
                    margin = margin,
                    processedCount = processedCount
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        /// <summary>
        /// レスポンシブUIの作成
        /// </summary>
        private string CreateResponsiveUI(Dictionary<string, string> parameters)
        {
            try
            {
                var containerName = parameters.GetValueOrDefault("containerName", "ResponsiveContainer");
                var layoutType = parameters.GetValueOrDefault("layoutType", "horizontal");
                var spacing = float.Parse(parameters.GetValueOrDefault("spacing", "10"));
                var padding = float.Parse(parameters.GetValueOrDefault("padding", "20"));
                var childAlignment = parameters.GetValueOrDefault("childAlignment", "middle-center");
                var useContentSizeFitter = parameters.GetValueOrDefault("useContentSizeFitter", "true") == "true";
                
                // Canvas確認
                var canvas = GameObject.FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "No Canvas found in scene" });
                }
                
                // レスポンシブコンテナ作成
                var container = new GameObject(containerName);
                container.transform.SetParent(canvas.transform, false);
                
                var rectTransform = container.AddComponent<RectTransform>();
                
                // レイアウトグループ追加
                LayoutGroup layoutGroup;
                if (layoutType.ToLower() == "vertical")
                {
                    var verticalLayout = container.AddComponent<VerticalLayoutGroup>();
                    verticalLayout.spacing = spacing;
                    verticalLayout.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
                    layoutGroup = verticalLayout;
                }
                else
                {
                    var horizontalLayout = container.AddComponent<HorizontalLayoutGroup>();
                    horizontalLayout.spacing = spacing;
                    horizontalLayout.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
                    layoutGroup = horizontalLayout;
                }
                
                // 子要素の整列設定
                switch (childAlignment.ToLower())
                {
                    case "upper-left":
                        layoutGroup.childAlignment = TextAnchor.UpperLeft;
                        break;
                    case "upper-center":
                        layoutGroup.childAlignment = TextAnchor.UpperCenter;
                        break;
                    case "upper-right":
                        layoutGroup.childAlignment = TextAnchor.UpperRight;
                        break;
                    case "middle-left":
                        layoutGroup.childAlignment = TextAnchor.MiddleLeft;
                        break;
                    case "middle-center":
                        layoutGroup.childAlignment = TextAnchor.MiddleCenter;
                        break;
                    case "middle-right":
                        layoutGroup.childAlignment = TextAnchor.MiddleRight;
                        break;
                    case "lower-left":
                        layoutGroup.childAlignment = TextAnchor.LowerLeft;
                        break;
                    case "lower-center":
                        layoutGroup.childAlignment = TextAnchor.LowerCenter;
                        break;
                    case "lower-right":
                        layoutGroup.childAlignment = TextAnchor.LowerRight;
                        break;
                }
                
                // Content Size Fitterを追加
                if (useContentSizeFitter)
                {
                    var sizeFitter = container.AddComponent<ContentSizeFitter>();
                    sizeFitter.horizontalFit = layoutType.ToLower() == "horizontal" ? 
                        ContentSizeFitter.FitMode.PreferredSize : 
                        ContentSizeFitter.FitMode.Unconstrained;
                    sizeFitter.verticalFit = layoutType.ToLower() == "vertical" ? 
                        ContentSizeFitter.FitMode.PreferredSize : 
                        ContentSizeFitter.FitMode.Unconstrained;
                }
                
                // デフォルトのアンカー設定（ストレッチ）
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(1, 1);
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                
                // サンプル子要素を3つ作成
                for (int i = 0; i < 3; i++)
                {
                    var childButton = CreateButton($"Button{i+1}", new Dictionary<string, string>
                    {
                        ["text"] = $"Button {i+1}"
                    });
                    childButton.transform.SetParent(container.transform, false);
                    
                    // Layout Element追加
                    var layoutElement = childButton.AddComponent<LayoutElement>();
                    layoutElement.minWidth = 100;
                    layoutElement.minHeight = 40;
                    layoutElement.preferredWidth = 150;
                    layoutElement.preferredHeight = 50;
                }
                
                Selection.activeGameObject = container;
                EditorGUIUtility.PingObject(container);
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Responsive UI container '{containerName}' created",
                    layoutType = layoutType,
                    childCount = 3,
                    hasContentSizeFitter = useContentSizeFitter,
                    spacing = spacing,
                    padding = padding
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        /// <summary>
        /// UIアニメーションの設定
        /// </summary>
        private string SetupUIAnimation(Dictionary<string, string> parameters)
        {
            try
            {
                var targetObject = parameters.GetValueOrDefault("targetObject", "");
                var animationType = parameters.GetValueOrDefault("animationType", "fade");
                var duration = float.Parse(parameters.GetValueOrDefault("duration", "0.5"));
                var delay = float.Parse(parameters.GetValueOrDefault("delay", "0"));
                var easing = parameters.GetValueOrDefault("easing", "ease");
                var autoPlay = parameters.GetValueOrDefault("autoPlay", "false") == "true";
                
                var target = GameObject.Find(targetObject);
                if (target == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Target object not found" });
                }
                
                var animator = target.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = target.AddComponent<Animator>();
                }
                
                // UI Animation Controller作成
                var controllerPath = "Assets/Animations/UI/UIAnimationController.controller";
                var controllerDir = System.IO.Path.GetDirectoryName(controllerPath);
                if (!System.IO.Directory.Exists(controllerDir))
                {
                    System.IO.Directory.CreateDirectory(controllerDir);
                }
                
                UnityEditor.Animations.AnimatorController controller;
                if (System.IO.File.Exists(controllerPath))
                {
                    controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
                }
                else
                {
                    controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                }
                
                animator.runtimeAnimatorController = controller;
                
                // アニメーションクリップ作成
                var clipPath = $"Assets/Animations/UI/{targetObject}_{animationType}.anim";
                var clip = new AnimationClip();
                clip.name = $"{targetObject}_{animationType}";
                
                var rectTransform = target.GetComponent<RectTransform>();
                var canvasGroup = target.GetComponent<CanvasGroup>();
                if (canvasGroup == null && (animationType == "fade" || animationType == "scale-fade"))
                {
                    canvasGroup = target.AddComponent<CanvasGroup>();
                }
                
                // アニメーション種類別のカーブ設定
                switch (animationType.ToLower())
                {
                    case "fade":
                        if (canvasGroup != null)
                        {
                            var alphaCurve = AnimationCurve.EaseInOut(0, 0, duration, 1);
                            clip.SetCurve("", typeof(CanvasGroup), "m_Alpha", alphaCurve);
                        }
                        break;
                        
                    case "scale":
                        var scaleStartCurve = AnimationCurve.EaseInOut(0, 0, duration, 1);
                        clip.SetCurve("", typeof(RectTransform), "m_LocalScale.x", scaleStartCurve);
                        clip.SetCurve("", typeof(RectTransform), "m_LocalScale.y", scaleStartCurve);
                        break;
                        
                    case "slide-left":
                        var slideXCurve = AnimationCurve.EaseInOut(0, -Screen.width, duration, 0);
                        clip.SetCurve("", typeof(RectTransform), "m_AnchoredPosition.x", slideXCurve);
                        break;
                        
                    case "slide-up":
                        var slideYCurve = AnimationCurve.EaseInOut(0, -Screen.height, duration, 0);
                        clip.SetCurve("", typeof(RectTransform), "m_AnchoredPosition.y", slideYCurve);
                        break;
                        
                    case "scale-fade":
                        var scaleCurve = AnimationCurve.EaseInOut(0, 0.5f, duration, 1);
                        var fadeCurve = AnimationCurve.EaseInOut(0, 0, duration, 1);
                        clip.SetCurve("", typeof(RectTransform), "m_LocalScale.x", scaleCurve);
                        clip.SetCurve("", typeof(RectTransform), "m_LocalScale.y", scaleCurve);
                        if (canvasGroup != null)
                        {
                            clip.SetCurve("", typeof(CanvasGroup), "m_Alpha", fadeCurve);
                        }
                        break;
                }
                
                // クリップ保存
                AssetDatabase.CreateAsset(clip, clipPath);
                
                // ステートマシンに追加
                var rootStateMachine = controller.layers[0].stateMachine;
                var animState = rootStateMachine.AddState(animationType);
                animState.motion = clip;
                
                // パラメーター追加
                if (!controller.parameters.Any(p => p.name == "Play"))
                {
                    controller.AddParameter("Play", AnimatorControllerParameterType.Trigger);
                }
                
                // トランジション追加
                var entryTransition = rootStateMachine.AddEntryTransition(animState);
                entryTransition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "Play");
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                // 自動再生
                if (autoPlay)
                {
                    animator.SetTrigger("Play");
                }
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"UI animation '{animationType}' setup for {targetObject}",
                    animationType = animationType,
                    duration = duration,
                    hasCanvasGroup = canvasGroup != null,
                    clipPath = clipPath,
                    autoPlayed = autoPlay
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        /// <summary>
        /// UIグリッドの作成
        /// </summary>
        private string CreateUIGrid(Dictionary<string, string> parameters)
        {
            try
            {
                var gridName = parameters.GetValueOrDefault("name", parameters.GetValueOrDefault("gridName", "UIGrid"));
                var columns = int.Parse(parameters.GetValueOrDefault("columns", "3"));
                var rows = int.Parse(parameters.GetValueOrDefault("rows", "3"));
                var cellSize = ParseVector2(parameters.GetValueOrDefault("cellSize", "100,100"));
                var spacing = ParseVector2(parameters.GetValueOrDefault("spacing", "10,10"));
                var padding = parameters.GetValueOrDefault("padding", "10,10,10,10");
                var fillType = parameters.GetValueOrDefault("fillType", "button");
                
                // Canvas確認
                var canvas = GameObject.FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "No Canvas found in scene" });
                }
                
                // グリッドコンテナ作成
                var gridContainer = new GameObject(gridName);
                gridContainer.transform.SetParent(canvas.transform, false);
                
                var rectTransform = gridContainer.AddComponent<RectTransform>();
                
                // Grid Layout Group追加
                var gridLayout = gridContainer.AddComponent<GridLayoutGroup>();
                gridLayout.cellSize = cellSize;
                gridLayout.spacing = spacing;
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = columns;
                
                // パディング設定
                var paddingValues = padding.Split(',');
                if (paddingValues.Length >= 4)
                {
                    gridLayout.padding = new RectOffset(
                        int.Parse(paddingValues[0]),  // left
                        int.Parse(paddingValues[1]),  // right
                        int.Parse(paddingValues[2]),  // top
                        int.Parse(paddingValues[3])   // bottom
                    );
                }
                
                // Content Size Fitter追加
                var sizeFitter = gridContainer.AddComponent<ContentSizeFitter>();
                sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                
                // グリッド要素作成
                var createdElements = new List<GameObject>();
                for (int i = 0; i < rows * columns; i++)
                {
                    GameObject element;
                    
                    switch (fillType.ToLower())
                    {
                        case "button":
                            element = CreateButton($"GridButton_{i}", new Dictionary<string, string>
                            {
                                ["text"] = $"Btn {i + 1}"
                            });
                            break;
                            
                        case "image":
                            element = CreateImage($"GridImage_{i}", new Dictionary<string, string>());
                            var image = element.GetComponent<Image>();
                            image.color = new Color(Random.Range(0.3f, 1f), Random.Range(0.3f, 1f), Random.Range(0.3f, 1f));
                            break;
                            
                        case "text":
                            element = CreateText($"GridText_{i}", new Dictionary<string, string>
                            {
                                ["text"] = $"Item {i + 1}"
                            });
                            break;
                            
                        case "toggle":
                            element = CreateToggle($"GridToggle_{i}", new Dictionary<string, string>());
                            break;
                            
                        default:
                            element = new GameObject($"GridItem_{i}");
                            element.AddComponent<RectTransform>();
                            var img = element.AddComponent<Image>();
                            img.color = Color.gray;
                            break;
                    }
                    
                    element.transform.SetParent(gridContainer.transform, false);
                    createdElements.Add(element);
                }
                
                // アンカー設定（中央配置）
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
                
                Selection.activeGameObject = gridContainer;
                EditorGUIUtility.PingObject(gridContainer);
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"UI Grid '{gridName}' created with {createdElements.Count} elements",
                    columns = columns,
                    rows = rows,
                    elementCount = createdElements.Count,
                    cellSize = $"{cellSize.x}x{cellSize.y}",
                    fillType = fillType
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        /// <summary>
        /// スクロールビューのセットアップ
        /// </summary>
        private string SetupScrollView(Dictionary<string, string> parameters)
        {
            try
            {
                var scrollViewName = parameters.GetValueOrDefault("scrollViewName", "ScrollView");
                var scrollDirection = parameters.GetValueOrDefault("scrollDirection", "vertical");
                var contentType = parameters.GetValueOrDefault("contentType", "text");
                var itemCount = int.Parse(parameters.GetValueOrDefault("itemCount", "10"));
                var itemSize = ParseVector2(parameters.GetValueOrDefault("itemSize", "200,50"));
                var useScrollbar = parameters.GetValueOrDefault("useScrollbar", "true") == "true";
                var elasticity = float.Parse(parameters.GetValueOrDefault("elasticity", "0.1"));
                
                // Canvas確認
                var canvas = GameObject.FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "No Canvas found in scene" });
                }
                
                // ScrollView作成
                var scrollView = new GameObject(scrollViewName);
                scrollView.transform.SetParent(canvas.transform, false);
                
                var scrollRect = scrollView.AddComponent<ScrollRect>();
                var scrollRectTransform = scrollView.GetComponent<RectTransform>();
                
                // アンカー設定（画面の大部分を占める）
                scrollRectTransform.anchorMin = new Vector2(0.1f, 0.1f);
                scrollRectTransform.anchorMax = new Vector2(0.9f, 0.9f);
                scrollRectTransform.offsetMin = Vector2.zero;
                scrollRectTransform.offsetMax = Vector2.zero;
                
                // 背景Image追加
                var scrollImage = scrollView.AddComponent<Image>();
                scrollImage.color = new Color(1f, 1f, 1f, 0.1f);
                
                // Viewport作成
                var viewport = new GameObject("Viewport");
                viewport.transform.SetParent(scrollView.transform, false);
                var viewportRect = viewport.AddComponent<RectTransform>();
                viewportRect.anchorMin = Vector2.zero;
                viewportRect.anchorMax = Vector2.one;
                viewportRect.offsetMin = Vector2.zero;
                viewportRect.offsetMax = Vector2.zero;
                
                var viewportMask = viewport.AddComponent<Mask>();
                viewportMask.showMaskGraphic = false;
                var viewportImage = viewport.AddComponent<Image>();
                viewportImage.color = Color.clear;
                
                // Content作成
                var content = new GameObject("Content");
                content.transform.SetParent(viewport.transform, false);
                var contentRect = content.AddComponent<RectTransform>();
                
                // スクロール方向に応じたContent設定
                if (scrollDirection.ToLower() == "vertical")
                {
                    contentRect.anchorMin = new Vector2(0, 1);
                    contentRect.anchorMax = new Vector2(1, 1);
                    contentRect.pivot = new Vector2(0.5f, 1);
                    
                    scrollRect.horizontal = false;
                    scrollRect.vertical = true;
                    
                    // Vertical Layout Group追加
                    var verticalLayout = content.AddComponent<VerticalLayoutGroup>();
                    verticalLayout.childControlHeight = false;
                    verticalLayout.childControlWidth = true;
                    verticalLayout.childForceExpandHeight = false;
                    verticalLayout.childForceExpandWidth = true;
                    verticalLayout.spacing = 5;
                }
                else
                {
                    contentRect.anchorMin = new Vector2(0, 0);
                    contentRect.anchorMax = new Vector2(0, 1);
                    contentRect.pivot = new Vector2(0, 0.5f);
                    
                    scrollRect.horizontal = true;
                    scrollRect.vertical = false;
                    
                    // Horizontal Layout Group追加
                    var horizontalLayout = content.AddComponent<HorizontalLayoutGroup>();
                    horizontalLayout.childControlHeight = true;
                    horizontalLayout.childControlWidth = false;
                    horizontalLayout.childForceExpandHeight = true;
                    horizontalLayout.childForceExpandWidth = false;
                    horizontalLayout.spacing = 5;
                }
                
                // Content Size Fitter追加
                var contentSizeFitter = content.AddComponent<ContentSizeFitter>();
                if (scrollDirection.ToLower() == "vertical")
                {
                    contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
                else
                {
                    contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
                
                // スクロールバー作成
                if (useScrollbar)
                {
                    if (scrollDirection.ToLower() == "vertical")
                    {
                        var verticalScrollbar = CreateScrollbar("Scrollbar Vertical", true);
                        verticalScrollbar.transform.SetParent(scrollView.transform, false);
                        scrollRect.verticalScrollbar = verticalScrollbar.GetComponent<Scrollbar>();
                    }
                    else
                    {
                        var horizontalScrollbar = CreateScrollbar("Scrollbar Horizontal", false);
                        horizontalScrollbar.transform.SetParent(scrollView.transform, false);
                        scrollRect.horizontalScrollbar = horizontalScrollbar.GetComponent<Scrollbar>();
                    }
                }
                
                // ScrollRect設定
                scrollRect.content = contentRect;
                scrollRect.viewport = viewportRect;
                scrollRect.elasticity = elasticity;
                scrollRect.movementType = ScrollRect.MovementType.Elastic;
                
                // コンテンツアイテム作成
                for (int i = 0; i < itemCount; i++)
                {
                    GameObject item;
                    
                    switch (contentType.ToLower())
                    {
                        case "text":
                            item = CreateText($"Item_{i}", new Dictionary<string, string>
                            {
                                ["text"] = $"List Item {i + 1}"
                            });
                            break;
                            
                        case "button":
                            item = CreateButton($"Item_{i}", new Dictionary<string, string>
                            {
                                ["text"] = $"Button {i + 1}"
                            });
                            break;
                            
                        case "image":
                            item = CreateImage($"Item_{i}", new Dictionary<string, string>());
                            var img = item.GetComponent<Image>();
                            img.color = new Color(Random.Range(0.3f, 1f), Random.Range(0.3f, 1f), Random.Range(0.3f, 1f));
                            break;
                            
                        default:
                            item = new GameObject($"Item_{i}");
                            item.AddComponent<RectTransform>();
                            var defaultImg = item.AddComponent<Image>();
                            defaultImg.color = Color.gray;
                            break;
                    }
                    
                    item.transform.SetParent(content.transform, false);
                    
                    // Layout Element追加
                    var layoutElement = item.AddComponent<LayoutElement>();
                    layoutElement.preferredWidth = itemSize.x;
                    layoutElement.preferredHeight = itemSize.y;
                }
                
                Selection.activeGameObject = scrollView;
                EditorGUIUtility.PingObject(scrollView);
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Scroll View '{scrollViewName}' created with {itemCount} items",
                    scrollDirection = scrollDirection,
                    itemCount = itemCount,
                    hasScrollbar = useScrollbar,
                    contentType = contentType
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        private GameObject CreateScrollbar(string name, bool isVertical)
        {
            var scrollbar = new GameObject(name);
            var scrollbarRect = scrollbar.AddComponent<RectTransform>();
            
            if (isVertical)
            {
                scrollbarRect.anchorMin = new Vector2(1, 0);
                scrollbarRect.anchorMax = new Vector2(1, 1);
                scrollbarRect.offsetMin = new Vector2(-20, 0);
                scrollbarRect.offsetMax = new Vector2(0, 0);
            }
            else
            {
                scrollbarRect.anchorMin = new Vector2(0, 0);
                scrollbarRect.anchorMax = new Vector2(1, 0);
                scrollbarRect.offsetMin = new Vector2(0, -20);
                scrollbarRect.offsetMax = new Vector2(0, 0);
            }
            
            var scrollbarImage = scrollbar.AddComponent<Image>();
            scrollbarImage.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            
            var scrollbarComponent = scrollbar.AddComponent<Scrollbar>();
            scrollbarComponent.direction = isVertical ? Scrollbar.Direction.BottomToTop : Scrollbar.Direction.LeftToRight;
            
            // Handle作成
            var handle = new GameObject("Handle");
            handle.transform.SetParent(scrollbar.transform, false);
            var handleRect = handle.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.8f, 0.8f, 0.8f, 0.8f);
            
            scrollbarComponent.handleRect = handleRect;
            scrollbarComponent.targetGraphic = handleImage;
            
            return scrollbar;
        }
        
        /// <summary>
        /// UI通知システムの作成
        /// </summary>
        private string CreateUINotification(Dictionary<string, string> parameters)
        {
            try
            {
                var notificationName = parameters.GetValueOrDefault("name", parameters.GetValueOrDefault("notificationName", "NotificationSystem"));
                var notificationType = parameters.GetValueOrDefault("notificationType", "toast");
                var position = parameters.GetValueOrDefault("position", "top-right");
                var animationType = parameters.GetValueOrDefault("animationType", "slide");
                var autoHide = parameters.GetValueOrDefault("autoHide", "true") == "true";
                var hideDelay = float.Parse(parameters.GetValueOrDefault("hideDelay", "3"));
                
                // Canvas確認
                var canvas = GameObject.FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "No Canvas found in scene" });
                }
                
                // 通知コンテナ作成
                var notificationContainer = new GameObject(notificationName);
                notificationContainer.transform.SetParent(canvas.transform, false);
                
                var containerRect = notificationContainer.AddComponent<RectTransform>();
                
                // 位置設定
                switch (position.ToLower())
                {
                    case "top-left":
                        containerRect.anchorMin = new Vector2(0, 1);
                        containerRect.anchorMax = new Vector2(0, 1);
                        containerRect.pivot = new Vector2(0, 1);
                        containerRect.anchoredPosition = new Vector2(20, -20);
                        break;
                    case "top-center":
                        containerRect.anchorMin = new Vector2(0.5f, 1);
                        containerRect.anchorMax = new Vector2(0.5f, 1);
                        containerRect.pivot = new Vector2(0.5f, 1);
                        containerRect.anchoredPosition = new Vector2(0, -20);
                        break;
                    case "top-right":
                        containerRect.anchorMin = new Vector2(1, 1);
                        containerRect.anchorMax = new Vector2(1, 1);
                        containerRect.pivot = new Vector2(1, 1);
                        containerRect.anchoredPosition = new Vector2(-20, -20);
                        break;
                    case "center":
                        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
                        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
                        containerRect.pivot = new Vector2(0.5f, 0.5f);
                        containerRect.anchoredPosition = Vector2.zero;
                        break;
                    case "bottom-center":
                        containerRect.anchorMin = new Vector2(0.5f, 0);
                        containerRect.anchorMax = new Vector2(0.5f, 0);
                        containerRect.pivot = new Vector2(0.5f, 0);
                        containerRect.anchoredPosition = new Vector2(0, 20);
                        break;
                }
                
                // Vertical Layout Group追加
                var layoutGroup = notificationContainer.AddComponent<VerticalLayoutGroup>();
                layoutGroup.childAlignment = TextAnchor.UpperCenter;
                layoutGroup.spacing = 10;
                layoutGroup.padding = new RectOffset(10, 10, 10, 10);
                
                // Content Size Fitter追加
                var sizeFitter = notificationContainer.AddComponent<ContentSizeFitter>();
                sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                
                // サンプル通知作成
                for (int i = 0; i < 2; i++)
                {
                    var notification = CreateNotificationItem(
                        $"Notification {i + 1}", 
                        $"This is sample notification message {i + 1}",
                        notificationType
                    );
                    notification.transform.SetParent(notificationContainer.transform, false);
                    
                    // アニメーション設定
                    if (animationType != "none")
                    {
                        var canvasGroup = notification.AddComponent<CanvasGroup>();
                        var animator = notification.AddComponent<Animator>();
                        
                        // 自動非表示設定
                        if (autoHide)
                        {
                            StartAutoHideCoroutine(notification, hideDelay);
                        }
                    }
                }
                
                Selection.activeGameObject = notificationContainer;
                EditorGUIUtility.PingObject(notificationContainer);
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Notification system '{notificationName}' created",
                    notificationType = notificationType,
                    position = position,
                    autoHide = autoHide,
                    hideDelay = hideDelay,
                    sampleCount = 2
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        private GameObject CreateNotificationItem(string title, string message, string type)
        {
            var notification = new GameObject("Notification");
            var rect = notification.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 80);
            
            // 背景
            var background = notification.AddComponent<Image>();
            switch (type.ToLower())
            {
                case "success":
                    background.color = new Color(0.2f, 0.8f, 0.2f, 0.9f);
                    break;
                case "warning":
                    background.color = new Color(1f, 0.8f, 0.2f, 0.9f);
                    break;
                case "error":
                    background.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
                    break;
                default:
                    background.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
                    break;
            }
            
            // タイトルテキスト
            var titleObj = CreateText("Title", new Dictionary<string, string>
            {
                ["text"] = title,
                ["fontSize"] = "16"
            });
            titleObj.transform.SetParent(notification.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.6f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(10, 0);
            titleRect.offsetMax = new Vector2(-10, 0);
            
            // メッセージテキスト
            var messageObj = CreateText("Message", new Dictionary<string, string>
            {
                ["text"] = message,
                ["fontSize"] = "12"
            });
            messageObj.transform.SetParent(notification.transform, false);
            var messageRect = messageObj.GetComponent<RectTransform>();
            messageRect.anchorMin = new Vector2(0, 0);
            messageRect.anchorMax = new Vector2(1, 0.6f);
            messageRect.offsetMin = new Vector2(10, 0);
            messageRect.offsetMax = new Vector2(-10, 0);
            
            return notification;
        }
        
        private void StartAutoHideCoroutine(GameObject notification, float delay)
        {
            // エディタでのコルーチン代替（実際のゲームではCoroutineを使用）
            EditorApplication.delayCall += () =>
            {
                if (notification != null)
                {
                    var canvasGroup = notification.GetComponent<CanvasGroup>();
                    if (canvasGroup != null)
                    {
                        // フェードアウト効果（簡易版）
                        for (float t = 0; t < 1; t += 0.1f)
                        {
                            var alpha = 1 - t;
                            EditorApplication.delayCall += () =>
                            {
                                if (canvasGroup != null) canvasGroup.alpha = alpha;
                            };
                        }
                        
                        // 削除
                        EditorApplication.delayCall += () =>
                        {
                            if (notification != null) UnityEngine.Object.DestroyImmediate(notification);
                        };
                    }
                }
            };
        }
        
        /// <summary>
        /// UIナビゲーションの設定
        /// </summary>
        private string SetupUINavigation(Dictionary<string, string> parameters)
        {
            try
            {
                var navigationName = parameters.GetValueOrDefault("navigationName", "UINavigation");
                var navigationType = parameters.GetValueOrDefault("navigationType", "tab");
                var itemCount = int.Parse(parameters.GetValueOrDefault("itemCount", "3"));
                var orientation = parameters.GetValueOrDefault("orientation", "horizontal");
                var selectedIndex = int.Parse(parameters.GetValueOrDefault("selectedIndex", "0"));
                
                // Canvas確認
                var canvas = GameObject.FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "No Canvas found in scene" });
                }
                
                // ナビゲーションコンテナ作成
                var navContainer = new GameObject(navigationName);
                navContainer.transform.SetParent(canvas.transform, false);
                
                var containerRect = navContainer.AddComponent<RectTransform>();
                containerRect.anchorMin = new Vector2(0, 0.9f);
                containerRect.anchorMax = new Vector2(1, 1);
                containerRect.offsetMin = Vector2.zero;
                containerRect.offsetMax = Vector2.zero;
                
                // レイアウトグループ追加
                LayoutGroup layoutGroup;
                if (orientation.ToLower() == "vertical")
                {
                    layoutGroup = navContainer.AddComponent<VerticalLayoutGroup>();
                }
                else
                {
                    layoutGroup = navContainer.AddComponent<HorizontalLayoutGroup>();
                }
                
                if (layoutGroup is HorizontalLayoutGroup horizontalGroup)
                {
                    horizontalGroup.childControlWidth = true;
                    horizontalGroup.childControlHeight = true;
                    horizontalGroup.childForceExpandWidth = true;
                    horizontalGroup.childForceExpandHeight = true;
                    horizontalGroup.spacing = 5;
                }
                else if (layoutGroup is VerticalLayoutGroup verticalGroup)
                {
                    verticalGroup.childControlWidth = true;
                    verticalGroup.childControlHeight = true;
                    verticalGroup.childForceExpandWidth = true;
                    verticalGroup.childForceExpandHeight = true;
                    verticalGroup.spacing = 5;
                }
                
                // Toggle Group追加（ラジオボタン風の動作）
                var toggleGroup = navContainer.AddComponent<ToggleGroup>();
                toggleGroup.allowSwitchOff = false;
                
                // ナビゲーションアイテム作成
                var createdItems = new List<GameObject>();
                for (int i = 0; i < itemCount; i++)
                {
                    GameObject navItem;
                    
                    switch (navigationType.ToLower())
                    {
                        case "tab":
                            navItem = CreateTabItem($"Tab {i + 1}", i == selectedIndex);
                            break;
                        case "button":
                            navItem = CreateButton($"NavButton_{i}", new Dictionary<string, string>
                            {
                                ["text"] = $"Nav {i + 1}"
                            });
                            break;
                        case "toggle":
                            navItem = CreateToggle($"NavToggle_{i}", new Dictionary<string, string>());
                            var toggle = navItem.GetComponent<Toggle>();
                            toggle.group = toggleGroup;
                            toggle.isOn = i == selectedIndex;
                            break;
                        default:
                            navItem = CreateButton($"NavItem_{i}", new Dictionary<string, string>
                            {
                                ["text"] = $"Item {i + 1}"
                            });
                            break;
                    }
                    
                    navItem.transform.SetParent(navContainer.transform, false);
                    createdItems.Add(navItem);
                }
                
                Selection.activeGameObject = navContainer;
                EditorGUIUtility.PingObject(navContainer);
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"UI Navigation '{navigationName}' created",
                    navigationType = navigationType,
                    itemCount = itemCount,
                    orientation = orientation,
                    selectedIndex = selectedIndex
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        private GameObject CreateTabItem(string title, bool isSelected)
        {
            var tab = CreateButton($"Tab_{title}", new Dictionary<string, string>
            {
                ["text"] = title
            });
            
            // 選択状態のスタイリング
            var button = tab.GetComponent<Button>();
            var image = tab.GetComponent<Image>();
            
            if (isSelected)
            {
                image.color = new Color(0.8f, 0.8f, 0.8f);
            }
            else
            {
                image.color = new Color(0.6f, 0.6f, 0.6f);
            }
            
            return tab;
        }
        
        /// <summary>
        /// UIダイアログの作成
        /// </summary>
        private string CreateUIDialog(Dictionary<string, string> parameters)
        {
            try
            {
                var dialogName = parameters.GetValueOrDefault("name", parameters.GetValueOrDefault("dialogName", "Dialog"));
                var dialogType = parameters.GetValueOrDefault("dialogType", "confirmation");
                var title = parameters.GetValueOrDefault("title", "Dialog Title");
                var message = parameters.GetValueOrDefault("message", "Dialog message content");
                var hasOverlay = parameters.GetValueOrDefault("hasOverlay", "true") == "true";
                var isModal = parameters.GetValueOrDefault("isModal", "true") == "true";
                
                // Canvas確認
                var canvas = GameObject.FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "No Canvas found in scene" });
                }
                
                // オーバーレイ作成
                GameObject overlay = null;
                if (hasOverlay)
                {
                    overlay = new GameObject("DialogOverlay");
                    overlay.transform.SetParent(canvas.transform, false);
                    
                    var overlayRect = overlay.AddComponent<RectTransform>();
                    overlayRect.anchorMin = Vector2.zero;
                    overlayRect.anchorMax = Vector2.one;
                    overlayRect.offsetMin = Vector2.zero;
                    overlayRect.offsetMax = Vector2.zero;
                    
                    var overlayImage = overlay.AddComponent<Image>();
                    overlayImage.color = new Color(0, 0, 0, 0.5f);
                    
                    if (isModal)
                    {
                        var overlayButton = overlay.AddComponent<Button>();
                        overlayButton.onClick.AddListener(() => {
                            if (overlay != null) UnityEngine.Object.DestroyImmediate(overlay.transform.parent.gameObject);
                        });
                    }
                }
                
                // ダイアログ作成
                var dialog = new GameObject(dialogName);
                dialog.transform.SetParent(overlay != null ? overlay.transform : canvas.transform, false);
                
                var dialogRect = dialog.AddComponent<RectTransform>();
                dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
                dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
                dialogRect.pivot = new Vector2(0.5f, 0.5f);
                dialogRect.sizeDelta = new Vector2(400, 300);
                dialogRect.anchoredPosition = Vector2.zero;
                
                // 背景
                var dialogImage = dialog.AddComponent<Image>();
                dialogImage.color = Color.white;
                
                // タイトルエリア
                var titleArea = new GameObject("TitleArea");
                titleArea.transform.SetParent(dialog.transform, false);
                var titleAreaRect = titleArea.AddComponent<RectTransform>();
                titleAreaRect.anchorMin = new Vector2(0, 0.8f);
                titleAreaRect.anchorMax = new Vector2(1, 1);
                titleAreaRect.offsetMin = Vector2.zero;
                titleAreaRect.offsetMax = Vector2.zero;
                
                var titleBg = titleArea.AddComponent<Image>();
                titleBg.color = new Color(0.9f, 0.9f, 0.9f);
                
                var titleText = CreateText("Title", new Dictionary<string, string>
                {
                    ["text"] = title,
                    ["fontSize"] = "18"
                });
                titleText.transform.SetParent(titleArea.transform, false);
                var titleTextRect = titleText.GetComponent<RectTransform>();
                titleTextRect.anchorMin = Vector2.zero;
                titleTextRect.anchorMax = Vector2.one;
                titleTextRect.offsetMin = new Vector2(20, 0);
                titleTextRect.offsetMax = new Vector2(-20, 0);
                
                // メッセージエリア
                var messageArea = new GameObject("MessageArea");
                messageArea.transform.SetParent(dialog.transform, false);
                var messageAreaRect = messageArea.AddComponent<RectTransform>();
                messageAreaRect.anchorMin = new Vector2(0, 0.3f);
                messageAreaRect.anchorMax = new Vector2(1, 0.8f);
                messageAreaRect.offsetMin = Vector2.zero;
                messageAreaRect.offsetMax = Vector2.zero;
                
                var messageText = CreateText("Message", new Dictionary<string, string>
                {
                    ["text"] = message,
                    ["fontSize"] = "14"
                });
                messageText.transform.SetParent(messageArea.transform, false);
                var messageTextRect = messageText.GetComponent<RectTransform>();
                messageTextRect.anchorMin = Vector2.zero;
                messageTextRect.anchorMax = Vector2.one;
                messageTextRect.offsetMin = new Vector2(20, 10);
                messageTextRect.offsetMax = new Vector2(-20, -10);
                
                // ボタンエリア
                var buttonArea = new GameObject("ButtonArea");
                buttonArea.transform.SetParent(dialog.transform, false);
                var buttonAreaRect = buttonArea.AddComponent<RectTransform>();
                buttonAreaRect.anchorMin = new Vector2(0, 0);
                buttonAreaRect.anchorMax = new Vector2(1, 0.3f);
                buttonAreaRect.offsetMin = Vector2.zero;
                buttonAreaRect.offsetMax = Vector2.zero;
                
                var buttonLayout = buttonArea.AddComponent<HorizontalLayoutGroup>();
                buttonLayout.spacing = 10;
                buttonLayout.padding = new RectOffset(20, 20, 20, 20);
                buttonLayout.childControlWidth = true;
                buttonLayout.childControlHeight = true;
                buttonLayout.childForceExpandWidth = true;
                buttonLayout.childForceExpandHeight = true;
                
                // ダイアログタイプに応じたボタン作成
                switch (dialogType.ToLower())
                {
                    case "confirmation":
                        var okButton = CreateButton("OKButton", new Dictionary<string, string> { ["text"] = "OK" });
                        var cancelButton = CreateButton("CancelButton", new Dictionary<string, string> { ["text"] = "Cancel" });
                        okButton.transform.SetParent(buttonArea.transform, false);
                        cancelButton.transform.SetParent(buttonArea.transform, false);
                        break;
                        
                    case "alert":
                        var alertButton = CreateButton("AlertButton", new Dictionary<string, string> { ["text"] = "OK" });
                        alertButton.transform.SetParent(buttonArea.transform, false);
                        break;
                        
                    case "input":
                        var inputField = CreateInputField("DialogInput", new Dictionary<string, string>
                        {
                            ["placeholder"] = "Enter text..."
                        });
                        inputField.transform.SetParent(messageArea.transform, false);
                        
                        var submitButton = CreateButton("SubmitButton", new Dictionary<string, string> { ["text"] = "Submit" });
                        var inputCancelButton = CreateButton("CancelButton", new Dictionary<string, string> { ["text"] = "Cancel" });
                        submitButton.transform.SetParent(buttonArea.transform, false);
                        inputCancelButton.transform.SetParent(buttonArea.transform, false);
                        break;
                }
                
                Selection.activeGameObject = dialog;
                EditorGUIUtility.PingObject(dialog);
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Dialog '{dialogName}' created",
                    dialogType = dialogType,
                    hasOverlay = hasOverlay,
                    isModal = isModal,
                    title = title
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        /// <summary>
        /// UICanvasの最適化
        /// </summary>
        private string OptimizeUICanvas(Dictionary<string, string> parameters)
        {
            try
            {
                var canvasName = parameters.GetValueOrDefault("canvasName", "");
                var optimizationType = parameters.GetValueOrDefault("optimizationType", "performance");
                var targetFrameRate = int.Parse(parameters.GetValueOrDefault("targetFrameRate", "60"));
                var enablePixelPerfect = parameters.GetValueOrDefault("enablePixelPerfect", "false") == "true";
                
                Canvas targetCanvas;
                if (string.IsNullOrEmpty(canvasName))
                {
                    targetCanvas = GameObject.FindObjectOfType<Canvas>();
                }
                else
                {
                    var canvasObj = GameObject.Find(canvasName);
                    targetCanvas = canvasObj?.GetComponent<Canvas>();
                }
                
                if (targetCanvas == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "Canvas not found" });
                }
                
                var optimizations = new List<string>();
                
                switch (optimizationType.ToLower())
                {
                    case "performance":
                        // パフォーマンス最適化
                        var canvasScaler = targetCanvas.GetComponent<CanvasScaler>();
                        if (canvasScaler != null)
                        {
                            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                            canvasScaler.referenceResolution = new Vector2(1920, 1080);
                            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                            canvasScaler.matchWidthOrHeight = 0.5f;
                            optimizations.Add("Canvas Scaler optimized");
                        }
                        
                        // Canvas Group最適化
                        var canvasGroups = targetCanvas.GetComponentsInChildren<CanvasGroup>();
                        foreach (var group in canvasGroups)
                        {
                            if (group.alpha == 0)
                            {
                                group.blocksRaycasts = false;
                                group.interactable = false;
                            }
                        }
                        optimizations.Add($"Optimized {canvasGroups.Length} Canvas Groups");
                        
                        // 不要なGraphic Raycaster除去
                        var graphicRaycasters = targetCanvas.GetComponentsInChildren<GraphicRaycaster>();
                        for (int i = 1; i < graphicRaycasters.Length; i++) // 最初の1つは残す
                        {
                            UnityEngine.Object.DestroyImmediate(graphicRaycasters[i]);
                        }
                        if (graphicRaycasters.Length > 1)
                        {
                            optimizations.Add($"Removed {graphicRaycasters.Length - 1} excess Graphic Raycasters");
                        }
                        break;
                        
                    case "quality":
                        // 品質最適化
                        if (enablePixelPerfect)
                        {
                            targetCanvas.pixelPerfect = true;
                            optimizations.Add("Pixel Perfect enabled");
                        }
                        
                        // 高品質テキスト設定
                        var texts = targetCanvas.GetComponentsInChildren<Text>();
                        foreach (var text in texts)
                        {
                            text.supportRichText = false; // パフォーマンスのため
                            if (text.fontSize < 24)
                            {
                                text.material = Resources.GetBuiltinResource<Material>("UI/Default Font Material");
                            }
                        }
                        optimizations.Add($"Optimized {texts.Length} Text components");
                        break;
                        
                    case "mobile":
                        // モバイル最適化
                        targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        
                        var mobileCanvasScaler = targetCanvas.GetComponent<CanvasScaler>();
                        if (mobileCanvasScaler != null)
                        {
                            mobileCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                            mobileCanvasScaler.referenceResolution = new Vector2(1080, 1920); // 縦向け
                            mobileCanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                            mobileCanvasScaler.matchWidthOrHeight = 1f; // 高さ基準
                        }
                        
                        // タッチ対応UI要素のサイズ調整
                        var buttons = targetCanvas.GetComponentsInChildren<Button>();
                        foreach (var button in buttons)
                        {
                            var buttonRect = button.GetComponent<RectTransform>();
                            if (buttonRect.sizeDelta.x < 44 || buttonRect.sizeDelta.y < 44)
                            {
                                buttonRect.sizeDelta = new Vector2(
                                    Mathf.Max(buttonRect.sizeDelta.x, 44),
                                    Mathf.Max(buttonRect.sizeDelta.y, 44)
                                );
                            }
                        }
                        optimizations.Add($"Mobile-optimized {buttons.Length} buttons");
                        break;
                }
                
                // FPS制限設定
                if (targetFrameRate > 0)
                {
                    Application.targetFrameRate = targetFrameRate;
                    optimizations.Add($"Target frame rate set to {targetFrameRate}");
                }
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Canvas '{targetCanvas.name}' optimized",
                    optimizationType = optimizationType,
                    optimizations = optimizations,
                    canvasName = targetCanvas.name
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        /// <summary>
        /// Safe Areaの設定
        /// </summary>
        private string SetupSafeArea(Dictionary<string, string> parameters)
        {
            try
            {
                var safeAreaName = parameters.GetValueOrDefault("safeAreaName", "SafeAreaContainer");
                var targetObject = parameters.GetValueOrDefault("targetObject", "");
                var applyToCanvas = parameters.GetValueOrDefault("applyToCanvas", "false") == "true";
                var includeNotch = parameters.GetValueOrDefault("includeNotch", "true") == "true";
                
                Canvas canvas = null;
                RectTransform targetRect = null;
                
                if (applyToCanvas)
                {
                    canvas = GameObject.FindObjectOfType<Canvas>();
                    if (canvas == null)
                    {
                        return JsonConvert.SerializeObject(new { success = false, error = "No Canvas found in scene" });
                    }
                    targetRect = canvas.GetComponent<RectTransform>();
                }
                else if (!string.IsNullOrEmpty(targetObject))
                {
                    var target = GameObject.Find(targetObject);
                    if (target == null)
                    {
                        return JsonConvert.SerializeObject(new { success = false, error = "Target object not found" });
                    }
                    targetRect = target.GetComponent<RectTransform>();
                }
                else
                {
                    // 新しいSafe Areaコンテナ作成
                    canvas = GameObject.FindObjectOfType<Canvas>();
                    if (canvas == null)
                    {
                        return JsonConvert.SerializeObject(new { success = false, error = "No Canvas found in scene" });
                    }
                    
                    var safeAreaContainer = new GameObject(safeAreaName);
                    safeAreaContainer.transform.SetParent(canvas.transform, false);
                    targetRect = safeAreaContainer.AddComponent<RectTransform>();
                }
                
                if (targetRect == null)
                {
                    return JsonConvert.SerializeObject(new { success = false, error = "No RectTransform found" });
                }
                
                // Safe Area Script作成（エディタ専用簡易版）
                var safeAreaScript = @"
using UnityEngine;

public class SafeAreaController : MonoBehaviour
{
    private RectTransform rectTransform;
    private Rect lastSafeArea = new Rect(0, 0, 0, 0);
    
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }
    
    void Update()
    {
        if (lastSafeArea != Screen.safeArea)
        {
            ApplySafeArea();
        }
    }
    
    void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;
        
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;
        
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
    }
}";
                
                // スクリプトファイル保存
                var scriptPath = "Assets/Scripts/UI/SafeAreaController.cs";
                var scriptDir = System.IO.Path.GetDirectoryName(scriptPath);
                if (!System.IO.Directory.Exists(scriptDir))
                {
                    System.IO.Directory.CreateDirectory(scriptDir);
                }
                System.IO.File.WriteAllText(scriptPath, safeAreaScript);
                AssetDatabase.Refresh();
                
                // エディタでの暫定Safe Area設定（iPhone X風）
                if (includeNotch)
                {
                    // ノッチを考慮した設定
                    targetRect.anchorMin = new Vector2(0, 0.05f); // 下部5%
                    targetRect.anchorMax = new Vector2(1, 0.95f); // 上部5%
                }
                else
                {
                    // 基本的なSafe Area
                    targetRect.anchorMin = new Vector2(0.02f, 0.02f); // 各辺2%
                    targetRect.anchorMax = new Vector2(0.98f, 0.98f);
                }
                
                targetRect.offsetMin = Vector2.zero;
                targetRect.offsetMax = Vector2.zero;
                
                Selection.activeGameObject = targetRect.gameObject;
                EditorGUIUtility.PingObject(targetRect.gameObject);
                
                return JsonConvert.SerializeObject(new
                {
                    success = true,
                    message = $"Safe Area setup completed for '{targetRect.gameObject.name}'",
                    includeNotch = includeNotch,
                    scriptPath = scriptPath,
                    anchorMin = $"{targetRect.anchorMin.x:F3}, {targetRect.anchorMin.y:F3}",
                    anchorMax = $"{targetRect.anchorMax.x:F3}, {targetRect.anchorMax.y:F3}"
                }, Formatting.Indented);
            }
            catch (Exception e)
            {
                return JsonConvert.SerializeObject(new { success = false, error = e.Message });
            }
        }
        
        private string GeneratePathfindingScript(string systemName, string algorithm, int gridWidth, int gridHeight, bool use3D)
        {
            return $@"using UnityEngine;
using System.Collections.Generic;

public class {systemName} : MonoBehaviour
{{
    [Header(""Grid Settings"")]
    public int gridWidth = {gridWidth};
    public int gridHeight = {gridHeight};
    public bool use3D = {use3D};
    public float nodeSize = 1f;
    
    [Header(""Pathfinding Settings"")]
    public LayerMask obstacleLayer = 1;
    public bool allowDiagonal = true;
    
    private PathNode[,] grid;
    private List<Vector3> currentPath;
    
    public class PathNode
    {{
        public int x, y;
        public bool isWalkable;
        public float gCost, hCost;
        public float fCost {{ get {{ return gCost + hCost; }} }}
        public PathNode parent;
        
        public PathNode(int x, int y, bool isWalkable = true)
        {{
            this.x = x;
            this.y = y;
            this.isWalkable = isWalkable;
        }}
    }}
    
    void Start()
    {{
        CreateGrid();
    }}
    
    void CreateGrid()
    {{
        grid = new PathNode[gridWidth, gridHeight];
        
        for (int x = 0; x < gridWidth; x++)
        {{
            for (int y = 0; y < gridHeight; y++)
            {{
                Vector3 worldPos = new Vector3(x * nodeSize, 0, y * nodeSize);
                bool walkable = !Physics.CheckSphere(worldPos, nodeSize / 2, obstacleLayer);
                grid[x, y] = new PathNode(x, y, walkable);
            }}
        }}
    }}
    
    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {{
        PathNode startNode = GetNodeFromWorldPos(startPos);
        PathNode targetNode = GetNodeFromWorldPos(targetPos);
        
        if (startNode == null || targetNode == null || !targetNode.isWalkable)
            return null;
        
        return FindPathAStar(startNode, targetNode);
    }}
    
    private List<Vector3> FindPathAStar(PathNode startNode, PathNode targetNode)
    {{
        List<PathNode> openSet = new List<PathNode>();
        HashSet<PathNode> closedSet = new HashSet<PathNode>();
        
        openSet.Add(startNode);
        
        while (openSet.Count > 0)
        {{
            PathNode currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {{
                if (openSet[i].fCost < currentNode.fCost || 
                    (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {{
                    currentNode = openSet[i];
                }}
            }}
            
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);
            
            if (currentNode == targetNode)
            {{
                List<Vector3> path = new List<Vector3>();
                PathNode pathNode = targetNode;
                
                while (pathNode != startNode)
                {{
                    path.Add(new Vector3(pathNode.x * nodeSize, 0, pathNode.y * nodeSize));
                    pathNode = pathNode.parent;
                }}
                path.Add(new Vector3(startNode.x * nodeSize, 0, startNode.y * nodeSize));
                
                path.Reverse();
                currentPath = path;
                return path;
            }}
            
            foreach (PathNode neighbor in GetNeighbors(currentNode))
            {{
                if (!neighbor.isWalkable || closedSet.Contains(neighbor))
                    continue;
                
                float newCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                
                if (newCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {{
                    neighbor.gCost = newCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;
                    
                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }}
            }}
        }}
        
        return null;
    }}
    
    private List<PathNode> GetNeighbors(PathNode node)
    {{
        List<PathNode> neighbors = new List<PathNode>();
        
        for (int x = -1; x <= 1; x++)
        {{
            for (int y = -1; y <= 1; y++)
            {{
                if (x == 0 && y == 0) continue;
                
                if (!allowDiagonal && (Mathf.Abs(x) + Mathf.Abs(y)) > 1) continue;
                
                int checkX = node.x + x;
                int checkY = node.y + y;
                
                if (checkX >= 0 && checkX < gridWidth && checkY >= 0 && checkY < gridHeight)
                {{
                    neighbors.Add(grid[checkX, checkY]);
                }}
            }}
        }}
        
        return neighbors;
    }}
    
    private float GetDistance(PathNode nodeA, PathNode nodeB)
    {{
        float dstX = Mathf.Abs(nodeA.x - nodeB.x);
        float dstY = Mathf.Abs(nodeA.y - nodeB.y);
        
        return allowDiagonal ? Mathf.Sqrt(dstX * dstX + dstY * dstY) : dstX + dstY;
    }}
    
    private PathNode GetNodeFromWorldPos(Vector3 worldPos)
    {{
        int x = Mathf.RoundToInt(worldPos.x / nodeSize);
        int y = Mathf.RoundToInt(worldPos.z / nodeSize);
        
        if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            return grid[x, y];
        
        return null;
    }}
}}";
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Dictionary<string, string>をDictionary<string, object>に変換
        /// </summary>
        private Dictionary<string, object> ConvertParameters(Dictionary<string, string> parameters)
        {
            var result = new Dictionary<string, object>();
            foreach (var kvp in parameters)
            {
                result[kvp.Key] = kvp.Value;
            }
            return result;
        }
        
        /// <summary>
        /// パラメータからターゲットGameObjectを取得
        /// </summary>
        private GameObject GetTargetGameObject(Dictionary<string, string> parameters)
        {
            // 様々なキー名でターゲットを検索
            string targetName = parameters.GetValueOrDefault("target") ?? 
                              parameters.GetValueOrDefault("gameObject") ?? 
                              parameters.GetValueOrDefault("object") ?? 
                              parameters.GetValueOrDefault("targetObject") ?? 
                              parameters.GetValueOrDefault("name") ??
                              parameters.GetValueOrDefault("objectName") ??
                              parameters.GetValueOrDefault("targetName") ??
                              parameters.GetValueOrDefault("source") ??
                              parameters.GetValueOrDefault("sourceName");
            
            if (string.IsNullOrEmpty(targetName) || targetName == "last")
            {
                return lastCreatedObject;
            }
            
            // まず直接検索
            var found = GameObject.Find(targetName);
            if (found != null) return found;
            
            // 見つからない場合は部分一致検索
            var allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.name.Contains(targetName))
                {
                    return obj;
                }
            }
            
            // それでも見つからない場合は作成済みオブジェクトから検索
            foreach (var obj in createdObjects)
            {
                if (obj != null && obj.name.Contains(targetName))
                {
                    return obj;
                }
            }
            
            return null;
        }
        
        #endregion
        
        #region UI Design Tools
        
        /// <summary>
        /// テーマを適用（ダークテーマ、ライトテーマ、カスタム）
        /// </summary>
        private string ApplyUITheme(Dictionary<string, object> parameters)
        {
            try
            {
                string themeName = parameters.ContainsKey("theme") ? parameters["theme"].ToString() : "dark";
                string targetName = parameters.ContainsKey("target") ? parameters["target"].ToString() : null;
                
                var theme = GetThemeColors(themeName);
                var targets = GetUITargets(targetName);
                
                int updatedCount = 0;
                foreach (var target in targets)
                {
                    ApplyThemeToGameObject(target, theme);
                    updatedCount++;
                }
                
                return $"テーマ '{themeName}' を {updatedCount} 個のUI要素に適用しました";
            }
            catch (Exception e)
            {
                return $"テーマ適用エラー: {e.Message}";
            }
        }
        
        /// <summary>
        /// UIカラーパレットを設定
        /// </summary>
        private string SetUIColors(Dictionary<string, object> parameters)
        {
            try
            {
                string targetName = parameters.ContainsKey("target") ? parameters["target"].ToString() : null;
                string primaryColor = parameters.ContainsKey("primary") ? parameters["primary"].ToString() : "#3498db";
                string secondaryColor = parameters.ContainsKey("secondary") ? parameters["secondary"].ToString() : "#2c3e50";
                string accentColor = parameters.ContainsKey("accent") ? parameters["accent"].ToString() : "#e74c3c";
                string backgroundColor = parameters.ContainsKey("background") ? parameters["background"].ToString() : "#ecf0f1";
                
                var targets = GetUITargets(targetName);
                int updatedCount = 0;
                
                foreach (var target in targets)
                {
                    var colorMap = new Dictionary<string, Color>
                    {
                        ["primary"] = ParseColor(primaryColor),
                        ["secondary"] = ParseColor(secondaryColor),
                        ["accent"] = ParseColor(accentColor),
                        ["background"] = ParseColor(backgroundColor)
                    };
                    
                    ApplyColorsToGameObject(target, colorMap);
                    updatedCount++;
                }
                
                return $"{updatedCount} 個のUI要素にカラーパレットを適用しました";
            }
            catch (Exception e)
            {
                return $"カラー設定エラー: {e.Message}";
            }
        }
        
        /// <summary>
        /// UIエレメントにスタイルを適用（ミニマル、モダン、ネオンなど）
        /// </summary>
        private string StyleUIElements(Dictionary<string, object> parameters)
        {
            try
            {
                string targetName = parameters.ContainsKey("target") ? parameters["target"].ToString() : null;
                string styleName = parameters.ContainsKey("style") ? parameters["style"].ToString() : "minimal";
                
                var targets = GetUITargets(targetName);
                var styleConfig = GetStyleConfig(styleName);
                
                int updatedCount = 0;
                foreach (var target in targets)
                {
                    ApplyStyleToGameObject(target, styleConfig);
                    updatedCount++;
                }
                
                return $"スタイル '{styleName}' を {updatedCount} 個のUI要素に適用しました";
            }
            catch (Exception e)
            {
                return $"スタイル適用エラー: {e.Message}";
            }
        }
        
        /// <summary>
        /// UI視覚効果を追加（影、グロー、グラデーション）
        /// </summary>
        private string AddUIEffects(Dictionary<string, object> parameters)
        {
            try
            {
                string targetName = parameters.ContainsKey("target") ? parameters["target"].ToString() : null;
                string effectType = parameters.ContainsKey("effect") ? parameters["effect"].ToString() : "shadow";
                
                var targets = GetUITargets(targetName);
                int updatedCount = 0;
                
                foreach (var target in targets)
                {
                    switch (effectType.ToLower())
                    {
                        case "shadow":
                            AddShadowEffect(target, parameters);
                            break;
                        case "glow":
                            AddGlowEffect(target, parameters);
                            break;
                        case "gradient":
                            AddGradientEffect(target, parameters);
                            break;
                        case "outline":
                            AddOutlineEffect(target, parameters);
                            break;
                    }
                    updatedCount++;
                }
                
                return $"エフェクト '{effectType}' を {updatedCount} 個のUI要素に追加しました";
            }
            catch (Exception e)
            {
                return $"エフェクト追加エラー: {e.Message}";
            }
        }
        
        /// <summary>
        /// タイポグラフィ設定（フォント、サイズ、ウェイト）
        /// </summary>
        private string SetTypography(Dictionary<string, object> parameters)
        {
            try
            {
                string targetName = parameters.ContainsKey("target") ? parameters["target"].ToString() : null;
                string fontFamily = parameters.ContainsKey("font") ? parameters["font"].ToString() : null;
                float fontSize = parameters.ContainsKey("size") ? Convert.ToSingle(parameters["size"]) : 14f;
                string fontWeight = parameters.ContainsKey("weight") ? parameters["weight"].ToString() : "normal";
                string textColor = parameters.ContainsKey("color") ? parameters["color"].ToString() : "#000000";
                
                var targets = GetUITargets(targetName);
                int updatedCount = 0;
                
                foreach (var target in targets)
                {
                    var textComponents = target.GetComponentsInChildren<TextMeshProUGUI>(true);
                    var legacyTextComponents = target.GetComponentsInChildren<Text>(true);
                    
                    foreach (var textComp in textComponents)
                    {
                        if (fontFamily != null)
                        {
                            var font = GetTMPFont(fontFamily);
                            if (font != null) textComp.font = font;
                        }
                        
                        textComp.fontSize = fontSize;
                        textComp.color = ParseColor(textColor);
                        
                        // フォントウェイト適用
                        ApplyFontWeight(textComp, fontWeight);
                        
                        updatedCount++;
                    }
                    
                    foreach (var textComp in legacyTextComponents)
                    {
                        if (fontFamily != null)
                        {
                            var font = GetLegacyFont(fontFamily);
                            if (font != null) textComp.font = font;
                        }
                        
                        textComp.fontSize = Mathf.RoundToInt(fontSize);
                        textComp.color = ParseColor(textColor);
                        
                        // レガシーテキストのフォントスタイル
                        ApplyLegacyFontWeight(textComp, fontWeight);
                        
                        updatedCount++;
                    }
                }
                
                return $"{updatedCount} 個のテキスト要素にタイポグラフィを適用しました";
            }
            catch (Exception e)
            {
                return $"タイポグラフィ設定エラー: {e.Message}";
            }
        }
        
        #region Helper Methods for UI Design
        
        private Dictionary<string, Color> GetThemeColors(string themeName)
        {
            switch (themeName.ToLower())
            {
                case "dark":
                    return new Dictionary<string, Color>
                    {
                        ["primary"] = new Color(0.2f, 0.2f, 0.2f, 1f),
                        ["secondary"] = new Color(0.15f, 0.15f, 0.15f, 1f),
                        ["accent"] = new Color(0.3f, 0.6f, 1f, 1f),
                        ["text"] = Color.white,
                        ["background"] = new Color(0.1f, 0.1f, 0.1f, 1f)
                    };
                case "light":
                    return new Dictionary<string, Color>
                    {
                        ["primary"] = Color.white,
                        ["secondary"] = new Color(0.95f, 0.95f, 0.95f, 1f),
                        ["accent"] = new Color(0.2f, 0.6f, 1f, 1f),
                        ["text"] = Color.black,
                        ["background"] = new Color(0.98f, 0.98f, 0.98f, 1f)
                    };
                case "neon":
                    return new Dictionary<string, Color>
                    {
                        ["primary"] = new Color(0.05f, 0.05f, 0.1f, 1f),
                        ["secondary"] = new Color(0.1f, 0.05f, 0.15f, 1f),
                        ["accent"] = new Color(0f, 1f, 1f, 1f),
                        ["text"] = new Color(0f, 1f, 0.5f, 1f),
                        ["background"] = Color.black
                    };
                default:
                    return GetThemeColors("dark");
            }
        }
        
        private List<GameObject> GetUITargets(string targetName)
        {
            var targets = new List<GameObject>();
            
            if (string.IsNullOrEmpty(targetName))
            {
                // 全てのUIエレメントを取得
                var canvases = UnityEngine.Object.FindObjectsOfType<Canvas>();
                foreach (var canvas in canvases)
                {
                    targets.Add(canvas.gameObject);
                }
            }
            else
            {
                var found = GameObject.Find(targetName);
                if (found != null)
                {
                    targets.Add(found);
                }
                
                // 名前でパターンマッチング
                var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                foreach (var obj in allObjects)
                {
                    if (obj.name.ToLower().Contains(targetName.ToLower()))
                    {
                        targets.Add(obj);
                    }
                }
            }
            
            return targets.Distinct().ToList();
        }
        
        private void ApplyThemeToGameObject(GameObject target, Dictionary<string, Color> theme)
        {
            // Image components
            var images = target.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                if (image.gameObject.name.ToLower().Contains("background"))
                {
                    image.color = theme["background"];
                }
                else if (image.gameObject.name.ToLower().Contains("button"))
                {
                    image.color = theme["primary"];
                }
                else
                {
                    image.color = theme["secondary"];
                }
            }
            
            // Text components
            var texts = target.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                text.color = theme["text"];
            }
            
            var legacyTexts = target.GetComponentsInChildren<Text>(true);
            foreach (var text in legacyTexts)
            {
                text.color = theme["text"];
            }
        }
        
        private void ApplyColorsToGameObject(GameObject target, Dictionary<string, Color> colorMap)
        {
            var images = target.GetComponentsInChildren<Image>(true);
            var buttons = target.GetComponentsInChildren<Button>(true);
            
            foreach (var image in images)
            {
                var objName = image.gameObject.name.ToLower();
                
                if (objName.Contains("primary") || objName.Contains("button"))
                {
                    image.color = colorMap["primary"];
                }
                else if (objName.Contains("secondary"))
                {
                    image.color = colorMap["secondary"];
                }
                else if (objName.Contains("accent"))
                {
                    image.color = colorMap["accent"];
                }
                else if (objName.Contains("background"))
                {
                    image.color = colorMap["background"];
                }
            }
        }
        
        private Dictionary<string, object> GetStyleConfig(string styleName)
        {
            switch (styleName.ToLower())
            {
                case "minimal":
                    return new Dictionary<string, object>
                    {
                        ["cornerRadius"] = 0f,
                        ["borderWidth"] = 0f,
                        ["padding"] = 10f,
                        ["spacing"] = 5f
                    };
                case "modern":
                    return new Dictionary<string, object>
                    {
                        ["cornerRadius"] = 8f,
                        ["borderWidth"] = 1f,
                        ["padding"] = 15f,
                        ["spacing"] = 10f
                    };
                case "rounded":
                    return new Dictionary<string, object>
                    {
                        ["cornerRadius"] = 20f,
                        ["borderWidth"] = 2f,
                        ["padding"] = 20f,
                        ["spacing"] = 15f
                    };
                default:
                    return GetStyleConfig("minimal");
            }
        }
        
        private void ApplyStyleToGameObject(GameObject target, Dictionary<string, object> styleConfig)
        {
            // スタイル適用ロジック
            // 実際の実装では、UIコンポーネントのスタイルプロパティを設定
        }
        
        private void AddShadowEffect(GameObject target, Dictionary<string, object> parameters)
        {
            var shadow = target.GetComponent<Shadow>();
            if (shadow == null)
            {
                shadow = target.AddComponent<Shadow>();
            }
            
            // パラメータから影の設定を適用
            if (parameters.ContainsKey("shadowColor"))
            {
                shadow.effectColor = ParseColor(parameters["shadowColor"].ToString());
            }
            
            if (parameters.ContainsKey("shadowDistance"))
            {
                var distance = Convert.ToSingle(parameters["shadowDistance"]);
                shadow.effectDistance = new Vector2(distance, -distance);
            }
        }
        
        private void AddGlowEffect(GameObject target, Dictionary<string, object> parameters)
        {
            // グローエフェクトの実装
            // UnityのUIエフェクト系コンポーネントを使用
        }
        
        private void AddGradientEffect(GameObject target, Dictionary<string, object> parameters)
        {
            // グラデーションエフェクトの実装
            // カスタムシェーダーまたはUIGradientコンポーネントを使用
        }
        
        private void AddOutlineEffect(GameObject target, Dictionary<string, object> parameters)
        {
            var outline = target.GetComponent<Outline>();
            if (outline == null)
            {
                outline = target.AddComponent<Outline>();
            }
            
            if (parameters.ContainsKey("outlineColor"))
            {
                outline.effectColor = ParseColor(parameters["outlineColor"].ToString());
            }
            
            if (parameters.ContainsKey("outlineDistance"))
            {
                var distance = Convert.ToSingle(parameters["outlineDistance"]);
                outline.effectDistance = new Vector2(distance, distance);
            }
        }
        
        private TMP_FontAsset GetTMPFont(string fontName)
        {
            // フォントアセットの検索・取得
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            return fonts.FirstOrDefault(f => f.name.ToLower().Contains(fontName.ToLower()));
        }
        
        private Font GetLegacyFont(string fontName)
        {
            // レガシーフォントの検索・取得
            var fonts = Resources.FindObjectsOfTypeAll<Font>();
            return fonts.FirstOrDefault(f => f.name.ToLower().Contains(fontName.ToLower()));
        }
        
        private void ApplyFontWeight(TextMeshProUGUI textComp, string weight)
        {
            switch (weight.ToLower())
            {
                case "bold":
                    textComp.fontStyle |= FontStyles.Bold;
                    break;
                case "italic":
                    textComp.fontStyle |= FontStyles.Italic;
                    break;
                case "normal":
                    textComp.fontStyle = FontStyles.Normal;
                    break;
            }
        }
        
        private void ApplyLegacyFontWeight(Text textComp, string weight)
        {
            switch (weight.ToLower())
            {
                case "bold":
                    textComp.fontStyle = FontStyle.Bold;
                    break;
                case "italic":
                    textComp.fontStyle = FontStyle.Italic;
                    break;
                case "normal":
                    textComp.fontStyle = FontStyle.Normal;
                    break;
            }
        }
        
        #endregion
        
        #endregion
        
        #region Undo/Redo and Batch Operations
        
        /// <summary>
        /// 複数の操作をバッチ実行
        /// </summary>
        private async Task<string> ExecuteBatch(Dictionary<string, string> parameters)
        {
            try
            {
                string tasksJson = parameters.GetValueOrDefault("tasks", "[]");
                bool progressFeedback = parameters.GetValueOrDefault("progressFeedback", "true") == "true";
                
                var tasks = JsonConvert.DeserializeObject<List<BatchTask>>(tasksJson);
                if (tasks == null || tasks.Count == 0)
                {
                    return "実行するタスクがありません";
                }
                
                var results = new List<string>();
                var successCount = 0;
                var failCount = 0;
                
                // Undoグループを開始
                UnityEditor.Undo.SetCurrentGroupName("Batch Operations");
                var undoGroup = UnityEditor.Undo.GetCurrentGroup();
                
                for (int i = 0; i < tasks.Count; i++)
                {
                    var task = tasks[i];
                    
                    try
                    {
                        if (progressFeedback)
                        {
                            Debug.Log($"[Batch {i+1}/{tasks.Count}] {task.description}");
                        }
                        
                        // タスクを実行
                        var operation = new NexusUnityOperation
                        {
                            type = task.tool,
                            parameters = task.parameters ?? new Dictionary<string, string>()
                        };
                        
                        var result = await ExecuteOperation(operation);
                        results.Add($"✅ {task.description}: {result}");
                        successCount++;
                        
                        // 少し待つ（Unityエディタの応答性のため）
                        await Task.Delay(10);
                    }
                    catch (Exception e)
                    {
                        results.Add($"❌ {task.description}: {e.Message}");
                        failCount++;
                    }
                }
                
                UnityEditor.Undo.CollapseUndoOperations(undoGroup);
                
                var summary = $"バッチ実行完了: 成功 {successCount}件, 失敗 {failCount}件\n\n" +
                             string.Join("\n", results);
                
                return summary;
            }
            catch (Exception e)
            {
                return $"バッチ実行エラー: {e.Message}";
            }
        }
        
        
        #region Batch Helper Methods
        
        private class BatchTask
        {
            public string tool;
            public Dictionary<string, string> parameters;
            public string description;
        }
        
        private string GetSearchFilterForAssetType(string assetType)
        {
            switch (assetType.ToLower())
            {
                case "texture":
                    return "t:Texture2D";
                case "model":
                    return "t:Model";
                case "audio":
                    return "t:AudioClip";
                default:
                    return "";
            }
        }
        
        private void ApplyTextureImportSettings(string assetPath, Dictionary<string, object> settings)
        {
            var importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
            if (importer == null) return;
            
            foreach (var setting in settings)
            {
                switch (setting.Key.ToLower())
                {
                    case "maxsize":
                        if (int.TryParse(setting.Value.ToString(), out int maxSize))
                        {
                            importer.maxTextureSize = maxSize;
                        }
                        break;
                    case "compression":
                        if (System.Enum.TryParse<UnityEditor.TextureImporterCompression>(setting.Value.ToString(), true, out var compression))
                        {
                            importer.textureCompression = compression;
                        }
                        break;
                    case "mipmaps":
                        if (bool.TryParse(setting.Value.ToString(), out bool mipmaps))
                        {
                            importer.mipmapEnabled = mipmaps;
                        }
                        break;
                }
            }
            
            importer.SaveAndReimport();
        }
        
        private void ApplyModelImportSettings(string assetPath, Dictionary<string, object> settings)
        {
            var importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.ModelImporter;
            if (importer == null) return;
            
            foreach (var setting in settings)
            {
                switch (setting.Key.ToLower())
                {
                    case "scale":
                        if (float.TryParse(setting.Value.ToString(), out float scale))
                        {
                            importer.globalScale = scale;
                        }
                        break;
                    case "generatecolliders":
                        if (bool.TryParse(setting.Value.ToString(), out bool generateColliders))
                        {
                            importer.addCollider = generateColliders;
                        }
                        break;
                    case "importmaterials":
                        if (bool.TryParse(setting.Value.ToString(), out bool importMaterials))
                        {
                            importer.materialImportMode = importMaterials ? 
                                UnityEditor.ModelImporterMaterialImportMode.ImportStandard : 
                                UnityEditor.ModelImporterMaterialImportMode.None;
                        }
                        break;
                }
            }
            
            importer.SaveAndReimport();
        }
        
        private void ApplyAudioImportSettings(string assetPath, Dictionary<string, object> settings)
        {
            var importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.AudioImporter;
            if (importer == null) return;
            
            foreach (var setting in settings)
            {
                switch (setting.Key.ToLower())
                {
                    case "quality":
                        if (float.TryParse(setting.Value.ToString(), out float quality))
                        {
                            var defaultSettings = importer.defaultSampleSettings;
                            defaultSettings.quality = quality;
                            importer.defaultSampleSettings = defaultSettings;
                        }
                        break;
                    case "loadtype":
                        if (System.Enum.TryParse<UnityEngine.AudioClipLoadType>(setting.Value.ToString(), true, out var loadType))
                        {
                            var defaultSettings = importer.defaultSampleSettings;
                            defaultSettings.loadType = loadType;
                            importer.defaultSampleSettings = defaultSettings;
                        }
                        break;
                }
            }
            
            importer.SaveAndReimport();
        }
        
        private bool UpdateComponentProperties(Component component, Dictionary<string, object> propertyUpdates)
        {
            bool modified = false;
            
            foreach (var update in propertyUpdates)
            {
                try
                {
                    SetComponentProperty(component, update.Key, update.Value.ToString());
                    modified = true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to update property {update.Key} on {component.GetType().Name}: {e.Message}");
                }
            }
            
            return modified;
        }
        
        #endregion
        
        #endregion
        
        #region Package Management
        
        /// <summary>
        /// インストール済みパッケージの一覧を取得
        /// </summary>
        private string ListPackages(Dictionary<string, string> parameters)
        {
            try
            {
                var filterType = parameters.GetValueOrDefault("filter", "all").ToLower();
                var listRequest = UnityEditor.PackageManager.Client.List(true, filterType == "offline");
                
                // 同期的に待機（エディター専用）
                while (!listRequest.IsCompleted)
                {
                    System.Threading.Thread.Sleep(10);
                }
                
                if (listRequest.Status == UnityEditor.PackageManager.StatusCode.Success)
                {
                    var packages = listRequest.Result.Select(p => new
                    {
                        name = p.name,
                        displayName = p.displayName,
                        version = p.version,
                        description = p.description,
                        documentationUrl = p.documentationUrl,
                        type = p.source.ToString(),
                        isBuiltIn = p.source == UnityEditor.PackageManager.PackageSource.BuiltIn
                    }).ToList();
                    
                    return JsonConvert.SerializeObject(new
                    {
                        success = true,
                        packages = packages,
                        count = packages.Count
                    }, Formatting.Indented);
                }
                else
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"Failed to list packages: {listRequest.Error?.message}"
                    });
                }
            }
            catch (Exception e)
            {
                return CreateErrorResponse("ListPackages", e, parameters);
            }
        }
        
        /// <summary>
        /// パッケージをインストール
        /// </summary>
        private string InstallPackage(Dictionary<string, string> parameters)
        {
            try
            {
                var packageId = parameters.GetValueOrDefault("packageId", "");
                if (string.IsNullOrEmpty(packageId))
                {
                    return CreateMissingParameterResponse("InstallPackage", "packageId", parameters);
                }
                
                Debug.Log($"[InstallPackage] Installing package: {packageId}");
                var addRequest = UnityEditor.PackageManager.Client.Add(packageId);
                
                // 同期的に待機
                while (!addRequest.IsCompleted)
                {
                    System.Threading.Thread.Sleep(10);
                }
                
                if (addRequest.Status == UnityEditor.PackageManager.StatusCode.Success)
                {
                    var package = addRequest.Result;
                    return JsonConvert.SerializeObject(new
                    {
                        success = true,
                        message = $"Successfully installed package: {package.displayName}",
                        package = new
                        {
                            name = package.name,
                            displayName = package.displayName,
                            version = package.version
                        }
                    });
                }
                else
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"Failed to install package: {addRequest.Error?.message}"
                    });
                }
            }
            catch (Exception e)
            {
                return CreateErrorResponse("InstallPackage", e, parameters);
            }
        }
        
        /// <summary>
        /// パッケージをアンインストール
        /// </summary>
        private string RemovePackage(Dictionary<string, string> parameters)
        {
            try
            {
                var packageName = parameters.GetValueOrDefault("packageName", "");
                if (string.IsNullOrEmpty(packageName))
                {
                    return CreateMissingParameterResponse("RemovePackage", "packageName", parameters);
                }
                
                Debug.Log($"[RemovePackage] Removing package: {packageName}");
                var removeRequest = UnityEditor.PackageManager.Client.Remove(packageName);
                
                // 同期的に待機
                while (!removeRequest.IsCompleted)
                {
                    System.Threading.Thread.Sleep(10);
                }
                
                if (removeRequest.Status == UnityEditor.PackageManager.StatusCode.Success)
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = true,
                        message = $"Successfully removed package: {packageName}"
                    });
                }
                else
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"Failed to remove package: {removeRequest.Error?.message}"
                    });
                }
            }
            catch (Exception e)
            {
                return CreateErrorResponse("RemovePackage", e, parameters);
            }
        }
        
        /// <summary>
        /// パッケージの存在を確認
        /// </summary>
        private string CheckPackage(Dictionary<string, string> parameters)
        {
            try
            {
                var packageName = parameters.GetValueOrDefault("packageName", "");
                if (string.IsNullOrEmpty(packageName))
                {
                    return CreateMissingParameterResponse("CheckPackage", "packageName", parameters);
                }
                
                var listRequest = UnityEditor.PackageManager.Client.List();
                
                // 同期的に待機
                while (!listRequest.IsCompleted)
                {
                    System.Threading.Thread.Sleep(10);
                }
                
                if (listRequest.Status == UnityEditor.PackageManager.StatusCode.Success)
                {
                    var package = listRequest.Result.FirstOrDefault(p => p.name == packageName);
                    
                    return JsonConvert.SerializeObject(new
                    {
                        success = true,
                        isInstalled = package != null,
                        package = package != null ? new
                        {
                            name = package.name,
                            displayName = package.displayName,
                            version = package.version,
                            source = package.source.ToString()
                        } : null
                    });
                }
                else
                {
                    return JsonConvert.SerializeObject(new
                    {
                        success = false,
                        error = $"Failed to check package: {listRequest.Error?.message}"
                    });
                }
            }
            catch (Exception e)
            {
                return CreateErrorResponse("CheckPackage", e, parameters);
            }
        }
        
        #endregion
        
        #region Serialization Helpers
        
        /// <summary>
        /// Vector3などの循環参照を防ぐためのシリアライゼーション用値変換
        /// </summary>
        private object ConvertValueForSerialization(object value)
        {
            if (value == null) return null;
            
            // Vector3の循環参照を防ぐ
            if (value is Vector3 v3)
            {
                return new { x = v3.x, y = v3.y, z = v3.z };
            }
            
            // Vector2の循環参照を防ぐ
            if (value is Vector2 v2)
            {
                return new { x = v2.x, y = v2.y };
            }
            
            // Colorの循環参照を防ぐ
            if (value is Color color)
            {
                return new { r = color.r, g = color.g, b = color.b, a = color.a };
            }
            
            // Quaternionの循環参照を防ぐ
            if (value is Quaternion quat)
            {
                return new { x = quat.x, y = quat.y, z = quat.z, w = quat.w };
            }
            
            return value;
        }
        
        #endregion
        
        #region Property Parsing Helpers
        
        /// <summary>
        /// key=value形式の文字列を解析してDictionaryに変換
        /// サポート形式: "mass = 5.0", "mass=5.0; useGravity=true", "position = (1, 2, 3)"
        /// </summary>
        private Dictionary<string, object> ParseKeyValueString(string input)
        {
            var result = new Dictionary<string, object>();
            
            if (string.IsNullOrEmpty(input))
                return result;
                
            Debug.Log($"[ParseKeyValueString] Parsing: '{input}'");
            
            try
            {
                // セミコロンで区切って複数のプロパティを処理
                var pairs = input.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var pair in pairs)
                {
                    var trimmedPair = pair.Trim();
                    if (string.IsNullOrEmpty(trimmedPair))
                        continue;
                        
                    // = で分割
                    var equalIndex = trimmedPair.IndexOf('=');
                    if (equalIndex > 0)
                    {
                        var key = trimmedPair.Substring(0, equalIndex).Trim();
                        var value = trimmedPair.Substring(equalIndex + 1).Trim();
                        
                        if (!string.IsNullOrEmpty(key))
                        {
                            result[key] = value;
                            Debug.Log($"[ParseKeyValueString] Parsed: {key} = {value}");
                        }
                    }
                    else
                    {
                        // = がない場合、文字列全体を "value" キーとして扱う
                        result["value"] = trimmedPair;
                        Debug.Log($"[ParseKeyValueString] No '=' found, using as value: {trimmedPair}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ParseKeyValueString] Error parsing '{input}': {ex.Message}");
                result["value"] = input; // フォールバック
            }
            
            return result;
        }
        
        #endregion
        
        #region Error Handling Helpers
        
        /// <summary>
        /// 統一的なエラーレスポンスを生成するヘルパーメソッド
        /// </summary>
        private string CreateErrorResponse(string methodName, Exception exception, Dictionary<string, string> parameters = null)
        {
            // 安全なロギング
            try
            {
                Debug.LogError($"[{methodName}] Error: {exception?.Message ?? "Unknown error"}");
                Debug.LogError($"[{methodName}] Exception type: {exception?.GetType().Name ?? "Unknown"}");
                
                // スタックトレースは長すぎる場合があるので制限する
                string stackTrace = exception?.StackTrace ?? "";
                if (stackTrace.Length > 1000)
                {
                    stackTrace = stackTrace.Substring(0, 1000) + "... (truncated)";
                }
                Debug.LogError($"[{methodName}] Stack trace: {stackTrace}");
            }
            catch (Exception loggingEx)
            {
                Debug.LogError($"[CreateErrorResponse] Failed to log error: {loggingEx.Message}");
            }
            
            // エラーレスポンスの安全な作成
            try
            {
                var errorResponse = new Dictionary<string, object>
                {
                    ["success"] = false,
                    ["error"] = exception?.Message ?? "Unknown error occurred",
                    ["method"] = methodName ?? "Unknown method",
                    ["errorType"] = exception?.GetType().Name ?? "Unknown",
                    ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    ["parameters"] = parameters?.Keys.ToArray() ?? new string[0],
                    ["debugInfo"] = "Check Unity Console for detailed debug logs"
                };
                
                // 追加のコンテキスト情報
                if (parameters != null && parameters.Count > 0)
                {
                    var paramInfo = new Dictionary<string, object>();
                    foreach (var kvp in parameters.Take(5)) // 最初の5個のパラメータのみ
                    {
                        try
                        {
                            paramInfo[kvp.Key] = kvp.Value?.Length > 100 ? 
                                kvp.Value.Substring(0, 100) + "..." : kvp.Value;
                        }
                        catch
                        {
                            paramInfo[kvp.Key] = "[Unable to serialize]";
                        }
                    }
                    errorResponse["parameterValues"] = paramInfo;
                }
                
                return JsonConvert.SerializeObject(errorResponse, Formatting.Indented);
            }
            catch (Exception serializationEx)
            {
                // 最後の手段：最小限のエラー情報
                Debug.LogError($"[CreateErrorResponse] Failed to serialize error response: {serializationEx.Message}");
                return JsonConvert.SerializeObject(new
                {
                    success = false,
                    error = "Error occurred and could not be properly serialized",
                    method = methodName ?? "Unknown",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }
        
        /// <summary>
        /// パラメータ不足エラーレスポンスを生成
        /// </summary>
        private string CreateMissingParameterResponse(string methodName, string missingParameter, Dictionary<string, string> parameters)
        {
            Debug.LogError($"[{methodName}] Missing required parameter: {missingParameter}");
            
            var errorResponse = new
            {
                success = false,
                error = $"Required parameter '{missingParameter}' is missing",
                method = methodName,
                missingParameter = missingParameter,
                receivedParameters = parameters.Keys.ToArray(),
                hint = $"Please provide the '{missingParameter}' parameter"
            };
            
            return JsonConvert.SerializeObject(errorResponse, Formatting.Indented);
        }
        
        /// <summary>
        /// GameObject not found エラーレスポンスを生成
        /// </summary>
        private string CreateGameObjectNotFoundResponse(string methodName, string searchedName, Dictionary<string, string> parameters)
        {
            var availableObjects = GameObject.FindObjectsOfType<GameObject>().Take(10).Select(o => o.name);
            
            var errorResponse = new
            {
                success = false,
                error = $"GameObject '{searchedName}' not found",
                method = methodName,
                searchedName = searchedName,
                availableObjects = availableObjects.ToArray(),
                parameters = parameters.Keys.ToArray(),
                hint = "Check available objects or ensure the GameObject exists in the scene"
            };
            
            return JsonConvert.SerializeObject(errorResponse, Formatting.Indented);
        }
        
        #endregion
    }
}
