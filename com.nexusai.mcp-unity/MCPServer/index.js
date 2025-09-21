const express = require('express');
const http = require('http');
const WebSocket = require('ws');
const cors = require('cors');
const { z } = require('zod');
const { createServer } = require('./mcp-server');
const util = require('util');
const fs = require('fs');
const path = require('path');

const app = express();
app.use(cors());
app.use(express.json());

const server = http.createServer(app);
let wss = null; // 後で初期化

let unityWebSocket = null;
let mcpServer = null;
let desktopAppSocket = null; // デスクトップアプリ接続用
const BridgeHandler = require('./bridge-handler');
const bridgeHandler = new BridgeHandler();

// Unity WebSocket接続の管理（関数として定義）
function setupWebSocketHandlers() {
    if (!wss) return;
    
    wss.on('connection', (ws, req) => {
        // 接続タイプを判定
        const isUnity = req.headers['x-client-type'] === 'unity' || req.url === '/unity';
        const isMCP = req.headers['x-client-type'] === 'mcp' || req.url === '/mcp';
        
        if (isMCP) {
            // デスクトップアプリ接続
            if (desktopAppSocket) {
                desktopAppSocket.close();
            }
            desktopAppSocket = ws;
            
            // ブリッジハンドラーに接続を設定
            bridgeHandler.setDesktopConnection(ws);
            
            ws.on('message', async (message) => {
                try {
                    const data = JSON.parse(message);
                    
                    // デスクトップアプリからのメッセージをUnityに転送
                    if (data.type === 'chat_response' && unityWebSocket) {
                        unityWebSocket.send(JSON.stringify({
                            type: 'assistant_message',
                            content: data.content
                        }));
                    }
                    
                    // ツール実行命令をUnityに転送
                    if (data.type === 'execute_tool' && unityWebSocket) {
                        unityWebSocket.send(JSON.stringify({
                            type: 'tool_call',
                            command: data.tool,
                            parameters: data.parameters,
                            id: data.id
                        }));
                    }
                    
                    // ブリッジハンドラーも使用（追加機能）
                    if (data.type === 'assistant_response') {
                        bridgeHandler.forwardToUnity(data);
                    }
                } catch (e) {
                    // console.error('MCP message error:', e);
                }
            });
            
            ws.on('close', () => {
                desktopAppSocket = null;
                bridgeHandler.handleDisconnect('desktop');
            });
            
            return;
        }
        
        // Unity接続処理
        // 古い接続をクリーンアップ
        if (unityWebSocket) {
            unityWebSocket.close();
        }
        
        // Unity接続（ログを出力しない）
        unityWebSocket = ws;
        
        // ブリッジハンドラーにも設定
        bridgeHandler.setUnityConnection(ws);
        
        // デバッグ用：ファイルに記録
        fs.appendFileSync('/tmp/mcp-debug.log', `[${new Date().toISOString()}] Unity connected\n`);

        ws.on('message', async (message) => {
            try {
                const data = JSON.parse(message);
                
                // Unity内チャットメッセージをデスクトップアプリに転送
                if (data.type === 'chat_message') {
                    if (desktopAppSocket) {
                        desktopAppSocket.send(JSON.stringify({
                            type: 'user_message',
                            content: data.message,
                            context: {
                                source: 'unity',
                                project: data.projectName || 'Unity Project'
                            }
                        }));
                    }
                    
                    // ブリッジハンドラーも使用（会話履歴管理）
                    bridgeHandler.forwardToDesktop({
                        content: data.message,
                        projectName: data.projectName
                    });
                    return;
                }
                
                if (data.type === 'operation_result' && data.id) {
                    // idを数値に変換して検索
                    const numericId = typeof data.id === 'string' ? parseInt(data.id) : data.id;
                    
                    if (pendingRequests.has(numericId)) {
                        const { resolve, reject, timeout } = pendingRequests.get(numericId);
                        clearTimeout(timeout);
                        pendingRequests.delete(numericId);
                        
                        // Unity側は content フィールドに結果を格納し、data.success で成功/失敗を示す
                        if (data.data && data.data.success) {
                            resolve(data.content);
                        } else {
                            reject(new Error(data.content || 'Unity command failed'));
                        }
                    }
                }
            } catch (e) {
                // エラーログを出力しない
            }
        });

        ws.on('close', () => {
            // Unity切断（ログを出力しない）
            unityWebSocket = null;
            bridgeHandler.handleDisconnect('unity');
        });

        ws.on('error', (error) => {
            // WebSocketエラー（ログを出力しない）
        });
    });
}

// Unityコマンド送信用のヘルパー関数
const pendingRequests = new Map();
let requestId = 0;

async function sendUnityCommand(command, params = {}) {
    if (!unityWebSocket || unityWebSocket.readyState !== WebSocket.OPEN) {
        throw new Error('Unity is not connected');
    }

    const id = ++requestId;
    
    return new Promise((resolve, reject) => {
        const timeout = setTimeout(() => {
            pendingRequests.delete(id);
            reject(new Error(`Unity command timeout: ${command} (id: ${id})`));
        }, 30000);

        pendingRequests.set(id, { resolve, reject, timeout });

        // Unity側が期待するフォーマットに合わせる
        const message = JSON.stringify({ 
            id, 
            type: 'tool_call',
            tool: command,
            command, 
            parameters: params  // Unity側は 'parameters' キーを期待
        });
        // ログを出力しない（JSON-RPC通信を妨害するため）
        unityWebSocket.send(message);
    });
}

// MCP サーバーの設定
async function setupMCPServer() {
    mcpServer = createServer();

    // ===== GameObject基本操作 =====
    mcpServer.registerTool('unity_create_gameobject', {
        title: 'Create GameObject',
        description: 'Create a new GameObject in Unity scene',
        inputSchema: z.object({
            name: z.string().describe('Name of the GameObject'),
            type: z.enum(['empty', 'cube', 'sphere', 'cylinder', 'plane', 'capsule', 'quad']).optional().default('empty').describe('Type of primitive to create'),
            parent: z.string().optional().describe('Parent GameObject name'),
            position: z.object({
                x: z.number(),
                y: z.number(),
                z: z.number()
            }).optional()
        })
    }, async (params) => {
        try {
            const result = await sendUnityCommand('create_gameobject', params);
            
            // MCPの仕様に従って、必ずcontent配列を返す
            return {
                content: [{
                    type: 'text',
                    text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
                }]
            };
        } catch (error) {
            throw error;
        }
    });

    mcpServer.registerTool('unity_update_gameobject', {
        title: 'Update GameObject',
        description: 'Update properties of an existing GameObject',
        inputSchema: z.object({
            name: z.string().describe('Name of the GameObject'),
            newName: z.string().optional(),
            active: z.boolean().optional(),
            tag: z.string().optional(),
            layer: z.number().optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('update_gameobject', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_delete_gameobject', {
        title: 'Delete GameObject',
        description: 'Delete a GameObject from the scene',
        inputSchema: z.object({
            name: z.string().describe('Name of the GameObject to delete')
        })
    }, async (params) => {
        const result = await sendUnityCommand('delete_gameobject', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== Transform操作 =====
    mcpServer.registerTool('unity_set_transform', {
        title: 'Set Transform',
        description: 'Set position, rotation, and scale of a GameObject',
        inputSchema: z.object({
            gameObject: z.string().describe('GameObject name'),
            position: z.object({
                x: z.number(),
                y: z.number(),
                z: z.number()
            }).optional(),
            rotation: z.object({
                x: z.number(),
                y: z.number(),
                z: z.number()
            }).optional().describe('Euler angles in degrees'),
            scale: z.object({
                x: z.number(),
                y: z.number(),
                z: z.number()
            }).optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('set_transform', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== コンポーネント操作 =====
    mcpServer.registerTool('unity_add_component', {
        title: 'Add Component',
        description: 'Add a component to a GameObject',
        inputSchema: z.object({
            gameObject: z.string().describe('GameObject name'),
            component: z.string().describe('Component type (e.g., Rigidbody, BoxCollider)')
        })
    }, async (params) => {
        const result = await sendUnityCommand('add_component', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_update_component', {
        title: 'Update Component',
        description: 'Update component properties',
        inputSchema: z.object({
            gameObject: z.string().describe('GameObject name'),
            component: z.string().describe('Component type'),
            properties: z.union([
                z.record(z.any()),
                z.string()
            ]).describe('Properties to update (JSON object or string)')
        })
    }, async (params) => {
        const result = await sendUnityCommand('update_component', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== UI操作 =====
    mcpServer.registerTool('unity_create_ui', {
        title: 'Create UI Element',
        description: 'Create UI elements in Unity',
        inputSchema: z.object({
            type: z.enum(['Canvas', 'Button', 'Text', 'Image', 'Slider', 'Toggle', 'InputField', 'Panel']),
            name: z.string(),
            parent: z.string().optional(),
            position: z.object({
                x: z.number(),
                y: z.number()
            }).optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_ui', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== 地形操作 =====
    mcpServer.registerTool('unity_create_terrain', {
        title: 'Create Terrain',
        description: 'Create a terrain in Unity',
        inputSchema: z.object({
            name: z.string(),
            width: z.number().optional().default(500),
            height: z.number().optional().default(600),
            length: z.number().optional().default(500),
            heightmapResolution: z.number().optional().default(513)
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_terrain', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_modify_terrain', {
        title: 'Modify Terrain',
        description: 'Modify terrain height or textures',
        inputSchema: z.object({
            name: z.string(),
            operation: z.enum(['raise', 'lower', 'flatten', 'smooth']),
            position: z.object({
                x: z.number(),
                y: z.number(),
                z: z.number()
            }),
            radius: z.number().optional().default(10),
            strength: z.number().optional().default(0.5)
        })
    }, async (params) => {
        const result = await sendUnityCommand('modify_terrain', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== カメラ操作 =====
    mcpServer.registerTool('unity_setup_camera', {
        title: 'Setup Camera',
        description: 'Setup camera in the scene',
        inputSchema: z.object({
            name: z.string().optional().default('Main Camera'),
            position: z.object({
                x: z.number(),
                y: z.number(),
                z: z.number()
            }),
            lookAt: z.object({
                x: z.number(),
                y: z.number(),
                z: z.number()
            }).optional(),
            fieldOfView: z.number().optional().default(60),
            cameraType: z.enum(['Perspective', 'Orthographic']).optional().default('Perspective')
        })
    }, async (params) => {
        const result = await sendUnityCommand('setup_camera', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== ライティング =====
    mcpServer.registerTool('unity_setup_lighting', {
        title: 'Setup Lighting',
        description: 'Setup lighting in the scene',
        inputSchema: z.object({
            ambientMode: z.enum(['skybox', 'trilight', 'flat', 'custom']).optional(),
            ambientIntensity: z.number().optional(),
            fogEnabled: z.boolean().optional(),
            fogMode: z.enum(['linear', 'exponential', 'exponentialsquared']).optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('setup_lighting', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== マテリアル =====
    mcpServer.registerTool('unity_create_material', {
        title: 'Create Material',
        description: 'Create a new material',
        inputSchema: z.object({
            name: z.string(),
            shader: z.string().optional().default('Standard'),
            color: z.object({
                r: z.number().min(0).max(1),
                g: z.number().min(0).max(1),
                b: z.number().min(0).max(1),
                a: z.number().min(0).max(1).optional().default(1)
            }).optional(),
            metallic: z.number().min(0).max(1).optional(),
            smoothness: z.number().min(0).max(1).optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_material', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== プレハブ =====
    mcpServer.registerTool('unity_create_prefab', {
        title: 'Create Prefab',
        description: 'Create a prefab from GameObject',
        inputSchema: z.object({
            gameObject: z.string().describe('GameObject to convert to prefab'),
            path: z.string().describe('Save path for the prefab')
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_prefab', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== スクリプト操作 =====
    mcpServer.registerTool('unity_create_script', {
        title: 'Create Script',
        description: 'Create a new C# script',
        inputSchema: z.object({
            name: z.string(),
            template: z.enum(['MonoBehaviour', 'ScriptableObject', 'Empty']).optional().default('MonoBehaviour'),
            content: z.string().optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_script', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== シーン管理 =====
    mcpServer.registerTool('unity_manage_scene', {
        title: 'Manage Scene',
        description: 'Scene management operations',
        inputSchema: z.object({
            operation: z.enum(['save', 'load', 'new']),
            path: z.string().optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('manage_scene', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== アニメーション =====
    mcpServer.registerTool('unity_create_animation', {
        title: 'Create Animation',
        description: 'Create animation for GameObject',
        inputSchema: z.object({
            gameObject: z.string(),
            animationName: z.string(),
            duration: z.number().optional().default(1),
            loop: z.boolean().optional().default(false)
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_animation', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== 物理設定 =====
    mcpServer.registerTool('unity_setup_physics', {
        title: 'Setup Physics',
        description: 'Setup physics settings for a GameObject or global physics',
        inputSchema: z.object({
            target: z.string().describe('Target GameObject name (or "global" for global physics settings)').optional(),
            rigidbody: z.boolean().describe('Add Rigidbody component').optional(),
            mass: z.number().describe('Rigidbody mass').optional(),
            gravity: z.union([
                z.boolean(),
                z.string(), // JSON文字列形式も受け入れる
                z.object({
                    x: z.number(),
                    y: z.number(),
                    z: z.number()
                })
            ]).describe('Use gravity (bool), JSON string, or global gravity vector').optional(),
            collider: z.string().describe('Type of collider to add: box, sphere, capsule, mesh').optional(),
            defaultMaterial: z.string().optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('setup_physics', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== パーティクルシステム =====
    mcpServer.registerTool('unity_create_particle_system', {
        title: 'Create Particle System',
        description: 'Create a particle system',
        inputSchema: z.object({
            name: z.string(),
            preset: z.enum(['fire', 'smoke', 'sparkle', 'rain', 'explosion']).optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_particle_system', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== NavMesh =====
    mcpServer.registerTool('unity_setup_navmesh', {
        title: 'Setup NavMesh',
        description: 'Setup navigation mesh',
        inputSchema: z.object({
            agentRadius: z.number().optional().default(0.5),
            agentHeight: z.number().optional().default(2.0),
            maxSlope: z.number().optional().default(45),
            stepHeight: z.number().optional().default(0.4)
        })
    }, async (params) => {
        const result = await sendUnityCommand('setup_navmesh', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== オーディオ =====
    mcpServer.registerTool('unity_create_audio_mixer', {
        title: 'Create Audio Mixer',
        description: 'Create an audio mixer',
        inputSchema: z.object({
            name: z.string(),
            groups: z.array(z.string()).optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_audio_mixer', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== 検索 =====
    mcpServer.registerTool('unity_search', {
        title: 'Search Objects',
        description: 'Search for objects in the scene',
        inputSchema: z.object({
            searchType: z.enum(['name', 'tag', 'layer', 'component']),
            query: z.string(),
            includeInactive: z.boolean().optional().default(false)
        })
    }, async (params) => {
        const result = await sendUnityCommand('search_objects', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== コンソール操作 =====
    mcpServer.registerTool('unity_console', {
        title: 'Console Operations',
        description: 'Unity console log count and basic info retrieval',
        inputSchema: z.object({
            operation: z.enum(['read', 'clear']).default('read'),
            logType: z.enum(['all', 'info', 'warning', 'error']).optional().default('all'),
            limit: z.number().optional().default(50)
        })
    }, async (params) => {
        const result = await sendUnityCommand('console_operation', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_analyze_console_logs', {
        title: 'Analyze Console Logs',
        description: 'Detailed analysis of Unity console logs with file paths, line numbers and stack traces',
        inputSchema: z.object({
            logType: z.enum(['all', 'error', 'warning', 'log']).optional().default('error'),
            limit: z.number().optional().default(10),
            includeStackTrace: z.boolean().optional().default(true),
            operation: z.enum(['analyze']).optional().default('analyze')
        })
    }, async (params) => {
        const result = await sendUnityCommand('analyze_console_logs', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== パッケージ管理 =====
    mcpServer.registerTool('unity_list_packages', {
        title: 'List Packages',
        description: 'List all installed Unity packages',
        inputSchema: z.object({
            filter: z.enum(['all', 'offline']).optional().default('all')
        })
    }, async (params) => {
        const result = await sendUnityCommand('list_packages', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_install_package', {
        title: 'Install Package',
        description: 'Install a Unity package',
        inputSchema: z.object({
            packageId: z.string().describe('Package identifier (e.g., com.unity.ai.navigation)')
        })
    }, async (params) => {
        const result = await sendUnityCommand('install_package', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_remove_package', {
        title: 'Remove Package',
        description: 'Remove an installed Unity package',
        inputSchema: z.object({
            packageName: z.string().describe('Package name (e.g., com.unity.ai.navigation)')
        })
    }, async (params) => {
        const result = await sendUnityCommand('remove_package', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_check_package', {
        title: 'Check Package',
        description: 'Check if a package is installed',
        inputSchema: z.object({
            packageName: z.string().describe('Package name to check')
        })
    }, async (params) => {
        const result = await sendUnityCommand('check_package', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== 配置ツール =====
    mcpServer.registerTool('unity_place_objects', {
        title: 'Place Objects',
        description: 'Place multiple objects with pattern',
        inputSchema: z.object({
            prefab: z.string(),
            pattern: z.enum(['grid', 'circle', 'line', 'random']),
            count: z.number(),
            spacing: z.number().optional().default(1),
            center: z.object({
                x: z.number(),
                y: z.number(),
                z: z.number()
            }).optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('place_objects', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== 履歴・Undo/Redo =====
    mcpServer.registerTool('unity_get_operation_history', {
        title: 'Get Operation History',
        description: 'Get history of Unity operations',
        inputSchema: z.object({
            count: z.number().optional().default(10)
        })
    }, async (params) => {
        const result = await sendUnityCommand('get_operation_history', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_undo_operation', {
        title: 'Undo Operation',
        description: 'Undo last Unity operation',
        inputSchema: z.object({})
    }, async (params) => {
        const result = await sendUnityCommand('undo_operation', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_redo_operation', {
        title: 'Redo Operation',
        description: 'Redo previously undone operation',
        inputSchema: z.object({})
    }, async (params) => {
        const result = await sendUnityCommand('redo_operation', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_create_checkpoint', {
        title: 'Create Checkpoint',
        description: 'Create a checkpoint to restore later',
        inputSchema: z.object({
            name: z.string()
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_checkpoint', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_restore_checkpoint', {
        title: 'Restore Checkpoint',
        description: 'Restore a previously created checkpoint',
        inputSchema: z.object({
            name: z.string()
        })
    }, async (params) => {
        const result = await sendUnityCommand('restore_checkpoint', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== リアルタイムモニタリング =====
    mcpServer.registerTool('unity_monitor_play_state', {
        title: 'Monitor Play State',
        description: 'Monitor Unity play mode state changes',
        inputSchema: z.object({
            enable: z.boolean()
        })
    }, async (params) => {
        const result = await sendUnityCommand('monitor_play_state', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_monitor_file_changes', {
        title: 'Monitor File Changes',
        description: 'Monitor file changes in the project',
        inputSchema: z.object({
            enable: z.boolean(),
            folders: z.array(z.string()).optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('monitor_file_changes', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_monitor_compile', {
        title: 'Monitor Compilation',
        description: 'Monitor script compilation events',
        inputSchema: z.object({
            enable: z.boolean()
        })
    }, async (params) => {
        const result = await sendUnityCommand('monitor_compile', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_subscribe_events', {
        title: 'Subscribe to Events',
        description: 'Subscribe to Unity events',
        inputSchema: z.object({
            events: z.array(z.string())
        })
    }, async (params) => {
        const result = await sendUnityCommand('subscribe_events', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_get_events', {
        title: 'Get Events',
        description: 'Get recent Unity events',
        inputSchema: z.object({
            count: z.number().optional().default(10)
        })
    }, async (params) => {
        const result = await sendUnityCommand('get_events', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_get_monitoring_status', {
        title: 'Get Monitoring Status',
        description: 'Get current monitoring status',
        inputSchema: z.object({})
    }, async (params) => {
        const result = await sendUnityCommand('get_monitoring_status', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== プロジェクト設定 =====
    mcpServer.registerTool('unity_get_build_settings', {
        title: 'Get Build Settings',
        description: 'Get Unity build settings',
        inputSchema: z.object({})
    }, async (params) => {
        const result = await sendUnityCommand('get_build_settings', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_get_player_settings', {
        title: 'Get Player Settings',
        description: 'Get Unity player settings',
        inputSchema: z.object({})
    }, async (params) => {
        const result = await sendUnityCommand('get_player_settings', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_get_quality_settings', {
        title: 'Get Quality Settings',
        description: 'Get Unity quality settings',
        inputSchema: z.object({})
    }, async (params) => {
        const result = await sendUnityCommand('get_quality_settings', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_get_input_settings', {
        title: 'Get Input Settings',
        description: 'Get Unity input settings',
        inputSchema: z.object({})
    }, async (params) => {
        const result = await sendUnityCommand('get_input_settings', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_get_physics_settings', {
        title: 'Get Physics Settings',
        description: 'Get Unity physics settings',
        inputSchema: z.object({})
    }, async (params) => {
        const result = await sendUnityCommand('get_physics_settings', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_get_project_summary', {
        title: 'Get Project Summary',
        description: 'Get overall project summary',
        inputSchema: z.object({})
    }, async (params) => {
        const result = await sendUnityCommand('get_project_summary', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== アセット管理 =====
    mcpServer.registerTool('unity_list_assets', {
        title: 'List Assets',
        description: 'List assets in the project',
        inputSchema: z.object({
            path: z.string().optional().default('Assets'),
            type: z.string().optional(),
            recursive: z.boolean().optional().default(true)
        })
    }, async (params) => {
        const result = await sendUnityCommand('list_assets', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== フォルダ管理 =====
    mcpServer.registerTool('unity_check_folder', {
        title: 'Check Folder',
        description: 'Check if folder exists',
        inputSchema: z.object({
            path: z.string()
        })
    }, async (params) => {
        const result = await sendUnityCommand('check_folder', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_create_folder', {
        title: 'Create Folder',
        description: 'Create a new folder',
        inputSchema: z.object({
            path: z.string()
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_folder', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_list_folders', {
        title: 'List Folders',
        description: 'List folders in a path',
        inputSchema: z.object({
            path: z.string().optional().default('Assets')
        })
    }, async (params) => {
        const result = await sendUnityCommand('list_folders', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== 新しいツール群 =====
    mcpServer.registerTool('unity_duplicate_gameobject', {
        title: 'Duplicate GameObject',
        description: 'Duplicate an existing GameObject',
        inputSchema: z.object({
            gameObject: z.string(),
            newName: z.string().optional(),
            position: z.object({
                x: z.number(),
                y: z.number(),
                z: z.number()
            }).optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('duplicate_gameobject', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_find_gameobjects_by_component', {
        title: 'Find GameObjects by Component',
        description: 'Find all GameObjects with specific component',
        inputSchema: z.object({
            componentType: z.string(),
            includeInactive: z.boolean().optional().default(false)
        })
    }, async (params) => {
        const result = await sendUnityCommand('find_by_component', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_cleanup_empty_objects', {
        title: 'Cleanup Empty Objects',
        description: 'Remove empty GameObjects from scene',
        inputSchema: z.object({
            dryRun: z.boolean().optional().default(true)
        })
    }, async (params) => {
        const result = await sendUnityCommand('cleanup_empty_objects', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== 優先度中のツール =====
    mcpServer.registerTool('unity_group_gameobjects', {
        title: 'Group GameObjects',
        description: 'Group multiple GameObjects under a parent',
        inputSchema: z.object({
            gameObjects: z.array(z.string()).describe('Names of GameObjects to group'),
            parentName: z.string().describe('Name for the parent group'),
            position: z.object({
                x: z.number(),
                y: z.number(),
                z: z.number()
            }).optional().describe('Position of the parent group')
        })
    }, async (params) => {
        const result = await sendUnityCommand('group_gameobjects', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_rename_asset', {
        title: 'Rename Asset',
        description: 'Rename an asset file',
        inputSchema: z.object({
            oldPath: z.string().describe('Current asset path'),
            newName: z.string().describe('New name for the asset')
        })
    }, async (params) => {
        const result = await sendUnityCommand('rename_asset', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_move_asset', {
        title: 'Move Asset',
        description: 'Move an asset to a different folder',
        inputSchema: z.object({
            sourcePath: z.string().describe('Current asset path'),
            destinationFolder: z.string().describe('Destination folder path')
        })
    }, async (params) => {
        const result = await sendUnityCommand('move_asset', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_delete_asset', {
        title: 'Delete Asset',
        description: 'Delete an asset from the project',
        inputSchema: z.object({
            assetPath: z.string().describe('Path of asset to delete'),
            moveToTrash: z.boolean().optional().default(true).describe('Move to trash instead of permanent delete')
        })
    }, async (params) => {
        const result = await sendUnityCommand('delete_asset', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_pause_scene', {
        title: 'Pause Scene',
        description: 'Pause or unpause the scene view',
        inputSchema: z.object({
            pause: z.boolean().describe('True to pause, false to unpause')
        })
    }, async (params) => {
        const result = await sendUnityCommand('pause_scene', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_find_missing_references', {
        title: 'Find Missing References',
        description: 'Find GameObjects with missing script references or components',
        inputSchema: z.object({
            searchScope: z.enum(['scene', 'project', 'both']).optional().default('scene'),
            fixAutomatically: z.boolean().optional().default(false)
        })
    }, async (params) => {
        const result = await sendUnityCommand('find_missing_references', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== 最適化ツール =====
    mcpServer.registerTool('unity_optimize_textures_batch', {
        title: 'Optimize Textures Batch',
        description: 'Batch optimize texture import settings',
        inputSchema: z.object({
            folder: z.string().optional().default('Assets'),
            maxTextureSize: z.number().optional(),
            compressionQuality: z.enum(['low', 'normal', 'high']).optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('optimize_textures_batch', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_analyze_draw_calls', {
        title: 'Analyze Draw Calls',
        description: 'Analyze and report draw call optimization opportunities',
        inputSchema: z.object({})
    }, async (params) => {
        const result = await sendUnityCommand('analyze_draw_calls', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== スナップショット =====
    mcpServer.registerTool('unity_create_project_snapshot', {
        title: 'Create Project Snapshot',
        description: 'Create a snapshot of the current project state',
        inputSchema: z.object({
            name: z.string(),
            description: z.string().optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_project_snapshot', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== プロジェクト分析ツール =====
    mcpServer.registerTool('unity_analyze_dependencies', {
        title: 'Analyze Asset Dependencies',
        description: 'Analyze and visualize asset dependencies in the project',
        inputSchema: z.object({
            assetPath: z.string().optional().describe('Specific asset to analyze (optional, analyzes all if not specified)'),
            maxDepth: z.number().optional().default(3).describe('Maximum depth of dependency tree'),
            includePackages: z.boolean().optional().default(false).describe('Include package dependencies')
        })
    }, async (params) => {
        const result = await sendUnityCommand('analyze_dependencies', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_export_project_structure', {
        title: 'Export Project Structure',
        description: 'Export project folder structure',
        inputSchema: z.object({
            format: z.enum(['json', 'tree', 'csv']).optional().default('tree'),
            includeFileSize: z.boolean().optional().default(true)
        })
    }, async (params) => {
        const result = await sendUnityCommand('export_project_structure', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_validate_naming_conventions', {
        title: 'Validate Naming Conventions',
        description: 'Check if project assets follow naming conventions',
        inputSchema: z.object({
            checkPascalCase: z.boolean().optional().default(true).describe('Check PascalCase for scripts'),
            checkCamelCase: z.boolean().optional().default(true).describe('Check camelCase for variables'),
            checkUnderscores: z.boolean().optional().default(true).describe('Check for underscores in names'),
            customPatterns: z.array(z.string()).optional().describe('Custom regex patterns to check')
        })
    }, async (params) => {
        const result = await sendUnityCommand('validate_naming_conventions', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_extract_all_text', {
        title: 'Extract All Text',
        description: 'Extract all text content from the project for localization or AI training',
        inputSchema: z.object({
            includeScripts: z.boolean().optional().default(true).describe('Extract text from scripts'),
            includeUI: z.boolean().optional().default(true).describe('Extract text from UI elements'),
            includeComments: z.boolean().optional().default(true).describe('Include code comments'),
            format: z.string().optional().default('json').describe('Output format: json, txt, csv')
        })
    }, async (params) => {
        const result = await sendUnityCommand('extract_all_text', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== AIが喜ぶ追加ツール群 =====
    
    // バッチ処理系
    mcpServer.registerTool('unity_batch_rename', {
        title: 'Batch Rename Assets',
        description: 'Batch rename multiple assets using patterns or regex',
        inputSchema: z.object({
            searchPattern: z.string().describe('Search pattern (supports wildcards and regex)'),
            replacePattern: z.string().describe('Replace pattern'),
            scope: z.enum(['selected', 'folder', 'project']).default('folder'),
            folderPath: z.string().optional().describe('Folder path if scope is folder'),
            useRegex: z.boolean().optional().default(false),
            caseSensitive: z.boolean().optional().default(true),
            dryRun: z.boolean().optional().default(true)
        })
    }, async (params) => {
        const result = await sendUnityCommand('batch_rename', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_batch_import_settings', {
        title: 'Batch Import Settings',
        description: 'Apply import settings to multiple assets at once',
        inputSchema: z.object({
            assetType: z.enum(['texture', 'model', 'audio']),
            folder: z.string().describe('Target folder path'),
            includeSubfolders: z.boolean().optional().default(true),
            settings: z.object({
                // Texture settings
                textureType: z.enum(['Default', 'NormalMap', 'Sprite', 'Cursor', 'Cookie', 'Lightmap']).optional(),
                maxSize: z.number().optional(),
                compression: z.enum(['None', 'Low', 'Normal', 'High']).optional(),
                generateMipmaps: z.boolean().optional(),
                
                // Model settings
                importMaterials: z.boolean().optional(),
                importAnimation: z.boolean().optional(),
                optimizeMesh: z.boolean().optional(),
                generateColliders: z.boolean().optional(),
                
                // Audio settings
                forceToMono: z.boolean().optional(),
                loadInBackground: z.boolean().optional(),
                preloadAudioData: z.boolean().optional()
            })
        })
    }, async (params) => {
        const result = await sendUnityCommand('batch_import_settings', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_batch_prefab_update', {
        title: 'Batch Update Prefabs',
        description: 'Update multiple prefabs with component changes',
        inputSchema: z.object({
            prefabFolder: z.string().describe('Folder containing prefabs'),
            componentType: z.string().describe('Component type to modify'),
            operation: z.enum(['add', 'remove', 'modify']),
            properties: z.record(z.any()).optional().describe('Properties to set if modifying'),
            filter: z.object({
                hasComponent: z.string().optional(),
                nameContains: z.string().optional(),
                tag: z.string().optional()
            }).optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('batch_prefab_update', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // 分析・レポート系
    mcpServer.registerTool('unity_find_unused_assets', {
        title: 'Find Unused Assets',
        description: 'Find assets that are not referenced in the project',
        inputSchema: z.object({
            assetTypes: z.array(z.enum(['texture', 'material', 'prefab', 'script', 'audio', 'model'])).optional(),
            excludeFolders: z.array(z.string()).optional().describe('Folders to exclude from search'),
            includePackages: z.boolean().optional().default(false)
        })
    }, async (params) => {
        const result = await sendUnityCommand('find_unused_assets', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_estimate_build_size', {
        title: 'Estimate Build Size',
        description: 'Estimate build size for different platforms',
        inputSchema: z.object({
            platforms: z.array(z.enum(['Windows', 'Mac', 'Linux', 'Android', 'iOS', 'WebGL'])).optional(),
            includeStreamingAssets: z.boolean().optional().default(true),
            compressionLevel: z.enum(['none', 'fastest', 'normal', 'best']).optional().default('normal')
        })
    }, async (params) => {
        const result = await sendUnityCommand('estimate_build_size', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_performance_report', {
        title: 'Generate Performance Report',
        description: 'Generate comprehensive performance analysis report',
        inputSchema: z.object({
            includeRendering: z.boolean().optional().default(true),
            includeTextures: z.boolean().optional().default(true),
            includeMeshes: z.boolean().optional().default(true),
            includeScripts: z.boolean().optional().default(true),
            includeAudio: z.boolean().optional().default(true),
            targetPlatform: z.enum(['Mobile', 'Desktop', 'Console', 'VR']).optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('performance_report', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // 自動化系
    mcpServer.registerTool('unity_auto_organize_folders', {
        title: 'Auto Organize Project Folders',
        description: 'Automatically organize assets into appropriate folders',
        inputSchema: z.object({
            rootFolder: z.string().optional().default('Assets'),
            createStandardFolders: z.boolean().optional().default(true),
            moveAssets: z.boolean().optional().default(false),
            dryRun: z.boolean().optional().default(true)
        })
    }, async (params) => {
        const result = await sendUnityCommand('auto_organize_folders', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_generate_lod', {
        title: 'Generate LOD Groups',
        description: 'Automatically generate LOD groups for meshes',
        inputSchema: z.object({
            targetObject: z.string().describe('GameObject or folder to process'),
            lodLevels: z.number().min(2).max(4).optional().default(3),
            lodDistances: z.array(z.number()).optional().describe('Custom LOD distances'),
            generateSimplifiedMeshes: z.boolean().optional().default(false)
        })
    }, async (params) => {
        const result = await sendUnityCommand('generate_lod', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_auto_atlas_textures', {
        title: 'Auto Create Texture Atlas',
        description: 'Automatically create texture atlases from multiple textures',
        inputSchema: z.object({
            sourceFolder: z.string().describe('Folder containing textures'),
            atlasName: z.string().describe('Name for the atlas'),
            maxAtlasSize: z.number().optional().default(2048),
            padding: z.number().optional().default(2),
            includeInBuild: z.boolean().optional().default(true)
        })
    }, async (params) => {
        const result = await sendUnityCommand('auto_atlas_textures', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== ゲーム開発特化機能 =====
    mcpServer.registerTool('unity_create_game_controller', {
        title: 'Create Game Controller',
        description: 'Create player controller for different game types (FirstPerson, ThirdPerson, TopDown, Platformer2D)',
        inputSchema: z.object({
            type: z.enum(['FirstPerson', 'ThirdPerson', 'TopDown', 'Platformer2D']).default('FirstPerson'),
            playerName: z.string().optional().default('Player'),
            includeCamera: z.boolean().optional().default(true),
            movementSpeed: z.number().optional().default(5),
            jumpHeight: z.number().optional().default(3)
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_game_controller', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_setup_input_system', {
        title: 'Setup Input System',
        description: 'Setup Unity Input System with predefined templates',
        inputSchema: z.object({
            template: z.enum(['Standard', 'Mobile', 'VR']).default('Standard'),
            createAsset: z.boolean().optional().default(true)
        })
    }, async (params) => {
        const result = await sendUnityCommand('setup_input_system', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_create_state_machine', {
        title: 'Create State Machine',
        description: 'Create a state machine for character or game states',
        inputSchema: z.object({
            targetObject: z.string().optional(),
            type: z.string().optional().default('Character'),
            states: z.string().optional().default('Idle,Walk,Run,Jump,Attack').describe('Comma-separated state names')
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_state_machine', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_setup_inventory_system', {
        title: 'Setup Inventory System',
        description: 'Create an inventory system with UI',
        inputSchema: z.object({
            size: z.number().optional().default(20),
            hasUI: z.boolean().optional().default(true),
            stackable: z.boolean().optional().default(true)
        })
    }, async (params) => {
        const result = await sendUnityCommand('setup_inventory_system', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== プロトタイピング機能 =====
    mcpServer.registerTool('unity_create_game_template', {
        title: 'Create Game Template',
        description: 'Create complete game templates for different genres',
        inputSchema: z.object({
            genre: z.enum(['FPS', 'Platformer', 'RPG', 'Puzzle', 'Racing', 'Strategy']).default('FPS'),
            name: z.string().optional(),
            includeUI: z.boolean().optional().default(true),
            includeAudio: z.boolean().optional().default(true)
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_game_template', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_quick_prototype', {
        title: 'Quick Prototype',
        description: 'Create a quick playable prototype with specified elements',
        inputSchema: z.object({
            elements: z.string().optional().default('player,enemies,collectibles,obstacles'),
            worldSize: z.number().optional().default(20),
            playerType: z.enum(['Capsule', 'Cube', 'Sphere']).optional().default('Capsule')
        })
    }, async (params) => {
        const result = await sendUnityCommand('quick_prototype', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== AI・機械学習関連 =====
    mcpServer.registerTool('unity_setup_ml_agent', {
        title: 'Setup ML Agent',
        description: 'Setup a Machine Learning Agent for reinforcement learning',
        inputSchema: z.object({
            agentName: z.string().default('MLAgent'),
            agentType: z.enum(['Basic', 'Advanced', 'Reward-based']).default('Basic'),
            vectorObservationSize: z.number().default(8),
            useVisualObservation: z.boolean().default(false)
        })
    }, async (params) => {
        const result = await sendUnityCommand('setup_ml_agent', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_create_neural_network', {
        title: 'Create Neural Network',
        description: 'Create a neural network system for AI decision making',
        inputSchema: z.object({
            networkName: z.string().default('NeuralNetwork'),
            networkType: z.enum(['Feedforward', 'Recurrent', 'Convolutional']).default('Feedforward'),
            inputSize: z.number().default(4),
            hiddenSize: z.number().default(8),
            outputSize: z.number().default(2)
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_neural_network', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_setup_behavior_tree', {
        title: 'Setup Behavior Tree',
        description: 'Setup a behavior tree AI system for complex AI behaviors',
        inputSchema: z.object({
            treeName: z.string().default('BehaviorTree'),
            aiType: z.enum(['Enemy', 'NPC', 'Companion', 'Guard']).default('Enemy'),
            includePatrol: z.boolean().default(true),
            includeChase: z.boolean().default(true),
            includeAttack: z.boolean().default(true)
        })
    }, async (params) => {
        const result = await sendUnityCommand('setup_behavior_tree', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_create_ai_pathfinding', {
        title: 'Create AI Pathfinding',
        description: 'Create an AI pathfinding system with A* algorithm',
        inputSchema: z.object({
            systemName: z.string().default('PathfindingAI'),
            algorithm: z.enum(['AStar', 'Dijkstra', 'BFS']).default('AStar'),
            gridWidth: z.number().default(20),
            gridHeight: z.number().default(20),
            use3D: z.boolean().default(false)
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_ai_pathfinding', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // ===== デバッグ・テストツール =====
    
    mcpServer.registerTool('unity_control_game_speed', {
        title: 'Control Game Speed',
        description: 'Control Unity game speed (time scale) for debugging',
        inputSchema: z.object({
            operation: z.enum(['set', 'pause', 'step', 'get']).default('set'),
            speed: z.number().optional(),
            preset: z.enum(['pause', 'slowest', 'slow', 'normal', 'fast', 'fastest']).optional(),
            pauseMode: z.enum(['toggle', 'on', 'off']).default('toggle')
        })
    }, async (params) => {
        const result = await sendUnityCommand('control_game_speed', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_profile_performance', {
        title: 'Profile Performance',
        description: 'Get Unity performance profiling data',
        inputSchema: z.object({
            category: z.enum(['general', 'all', 'memory', 'gpu', 'cpu']).default('general'),
            duration: z.number().default(0),
            detailed: z.boolean().default(false)
        })
    }, async (params) => {
        const result = await sendUnityCommand('profile_performance', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_debug_draw', {
        title: 'Debug Draw',
        description: 'Draw debug shapes in Unity scene',
        inputSchema: z.object({
            type: z.enum(['line', 'ray', 'box', 'sphere', 'path', 'clear']).default('line'),
            duration: z.number().default(5),
            color: z.enum(['red', 'green', 'blue', 'yellow', 'white', 'black', 'cyan', 'magenta']).default('red'),
            // Line parameters
            start: z.string().optional().describe('Start position for line (e.g., "0,0,0")'),
            end: z.string().optional().describe('End position for line (e.g., "1,1,1")'),
            // Ray parameters
            origin: z.string().optional().describe('Origin position for ray'),
            direction: z.string().optional().describe('Direction vector for ray'),
            length: z.number().optional().describe('Length of ray'),
            // Box parameters
            center: z.string().optional().describe('Center position'),
            size: z.string().optional().describe('Box size (e.g., "1,1,1")'),
            // Sphere parameters
            radius: z.number().optional().describe('Sphere radius'),
            // Path parameters
            points: z.string().optional().describe('Semicolon-separated points (e.g., "0,0,0;1,1,0;2,1,0")')
        })
    }, async (params) => {
        const result = await sendUnityCommand('debug_draw', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_run_tests', {
        title: 'Run Unity Tests',
        description: 'Run Unity Test Runner tests',
        inputSchema: z.object({
            mode: z.enum(['editmode', 'playmode']).default('editmode'),
            category: z.string().default('all'),
            testName: z.string().optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('run_unity_tests', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_manage_breakpoints', {
        title: 'Manage Breakpoints',
        description: 'Manage debugging breakpoints in Unity',
        inputSchema: z.object({
            operation: z.enum(['pause', 'conditional', 'log', 'assert']).default('pause'),
            condition: z.string().optional().describe('Condition for breakpoint (e.g., "frame > 100", "time > 5")'),
            message: z.string().default('Breakpoint hit')
        })
    }, async (params) => {
        const result = await sendUnityCommand('manage_breakpoints', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    // ===== GOAP AI系ツール =====
    
    mcpServer.registerTool('unity_create_goap_agent', {
        title: 'Create GOAP Agent',
        description: 'Create a GOAP (Goal Oriented Action Planning) AI agent with configurable behaviors',
        inputSchema: z.object({
            name: z.string().describe('Name of the GOAP agent'),
            agentType: z.string().describe('Type of agent (e.g., "Guard", "Worker", "Enemy")').optional(),
            position: z.object({
                x: z.number(),
                y: z.number(),
                z: z.number()
            }).optional(),
            capabilities: z.array(z.string()).describe('List of capabilities (e.g., ["movement", "combat", "collection"])').optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_goap_agent', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_define_goap_goal', {
        title: 'Define GOAP Goal',
        description: 'Define a goal for GOAP agents using natural language',
        inputSchema: z.object({
            agentName: z.string().describe('Name of the GOAP agent'),
            goalName: z.string().describe('Name of the goal'),
            description: z.string().describe('Natural language description of the goal (e.g., "Keep area safe from enemies")'),
            priority: z.number().min(0).max(100).default(50).describe('Priority of the goal (0-100)'),
            conditions: z.string().optional().describe('Conditions for goal activation (e.g., "enemy detected within 10 units")')
        })
    }, async (params) => {
        const result = await sendUnityCommand('define_goap_goal', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_create_goap_action', {
        title: 'Create GOAP Action',
        description: 'Create an action for GOAP agents',
        inputSchema: z.object({
            agentName: z.string().describe('Name of the GOAP agent'),
            actionName: z.string().describe('Name of the action'),
            description: z.string().describe('What this action does'),
            preconditions: z.array(z.string()).describe('Conditions that must be true before action (e.g., ["has_weapon", "enemy_in_range"])'),
            effects: z.array(z.string()).describe('Effects after action completion (e.g., ["enemy_defeated", "area_secure"])'),
            cost: z.number().default(1).describe('Cost of performing this action')
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_goap_action', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_define_behavior_language', {
        title: 'Define Behavior Using Natural Language',
        description: 'Define complete AI behavior using natural language description',
        inputSchema: z.object({
            agentName: z.string().describe('Name of the GOAP agent'),
            behavior: z.string().describe('Natural language behavior description (e.g., "Patrol between points A and B, attack enemies on sight, retreat when health is low")'),
            gameContext: z.string().optional().describe('Game context (e.g., "FPS", "RTS", "RPG", "Stealth")'),
            difficulty: z.enum(['easy', 'normal', 'hard', 'adaptive']).default('normal')
        })
    }, async (params) => {
        const result = await sendUnityCommand('define_behavior_language', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_generate_goap_action_set', {
        title: 'Generate GOAP Action Set',
        description: 'Automatically generate a complete action set based on agent type and goals',
        inputSchema: z.object({
            agentName: z.string().describe('Name of the GOAP agent'),
            agentRole: z.enum(['guard', 'worker', 'enemy', 'npc', 'companion']).describe('Role of the agent'),
            environment: z.string().optional().describe('Environment type (e.g., "forest", "urban", "dungeon")'),
            includeDefaults: z.boolean().default(true).describe('Include default actions for the role')
        })
    }, async (params) => {
        const result = await sendUnityCommand('generate_goap_action_set', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_setup_goap_world_state', {
        title: 'Setup GOAP World State',
        description: 'Configure the world state for GOAP planning',
        inputSchema: z.object({
            agentName: z.string().describe('Name of the GOAP agent'),
            worldState: z.record(z.union([z.boolean(), z.number(), z.string()])).describe('World state key-value pairs (e.g., {"has_weapon": true, "enemies_nearby": 2})'),
            sensors: z.array(z.string()).optional().describe('Sensors to add (e.g., ["enemy_detector", "health_monitor"])'),
            updateFrequency: z.number().default(0.5).describe('How often to update world state (seconds)')
        })
    }, async (params) => {
        const result = await sendUnityCommand('setup_goap_world_state', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_create_goap_template', {
        title: 'Create GOAP Template',
        description: 'Create a game-specific GOAP AI template',
        inputSchema: z.object({
            templateType: z.enum(['fps_enemy', 'rts_unit', 'rpg_npc', 'survival_creature', 'platformer_enemy']),
            difficulty: z.enum(['easy', 'normal', 'hard']).default('normal'),
            behaviors: z.array(z.string()).optional().describe('Additional behaviors to include'),
            customizations: z.record(z.any()).optional().describe('Template-specific customizations')
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_goap_template', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_debug_goap_decisions', {
        title: 'Debug GOAP Decisions',
        description: 'Visualize and debug GOAP decision-making process',
        inputSchema: z.object({
            agentName: z.string().describe('Name of the GOAP agent to debug'),
            showGraph: z.boolean().default(true).describe('Show decision graph'),
            showWorldState: z.boolean().default(true).describe('Show current world state'),
            showPlan: z.boolean().default(true).describe('Show current action plan'),
            logToConsole: z.boolean().default(false).describe('Log decisions to console')
        })
    }, async (params) => {
        const result = await sendUnityCommand('debug_goap_decisions', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_optimize_goap_performance', {
        title: 'Optimize GOAP Performance',
        description: 'Optimize GOAP agent performance for better frame rates',
        inputSchema: z.object({
            agentName: z.string().optional().describe('Specific agent to optimize (or all if not specified)'),
            maxPlanDepth: z.number().default(10).describe('Maximum planning depth'),
            planningFrequency: z.number().default(1).describe('How often to replan (seconds)'),
            enableMultithreading: z.boolean().default(true).describe('Use multithreading for planning'),
            cacheSize: z.number().default(100).describe('Plan cache size')
        })
    }, async (params) => {
        const result = await sendUnityCommand('optimize_goap_performance', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    // ===== アニメーション系ツール =====
    
    mcpServer.registerTool('unity_create_animator_controller', {
        title: 'Create Animator Controller',
        description: 'Create a new Animator Controller with default states and parameters',
        inputSchema: z.object({
            name: z.string().default('NewAnimatorController'),
            path: z.string().default('Assets/Animations/Controllers/'),
            targetObject: z.string().optional().describe('GameObject to apply the controller to'),
            applyToObject: z.boolean().default(true)
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_animator_controller', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_add_animation_state', {
        title: 'Add Animation State',
        description: 'Add a new state to an Animator Controller',
        inputSchema: z.object({
            controllerPath: z.string().describe('Path to the Animator Controller asset'),
            stateName: z.string().default('NewState'),
            animationClipPath: z.string().optional().describe('Path to animation clip'),
            layerIndex: z.number().default(0),
            isDefault: z.boolean().default(false)
        })
    }, async (params) => {
        const result = await sendUnityCommand('add_animation_state', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_create_animation_clip', {
        title: 'Create Animation Clip',
        description: 'Create a new animation clip with sample curves',
        inputSchema: z.object({
            name: z.string().default('NewAnimation'),
            path: z.string().default('Assets/Animations/Clips/'),
            duration: z.number().default(1),
            frameRate: z.number().default(30),
            targetObject: z.string().optional(),
            animationType: z.enum(['position', 'rotation', 'scale', 'color']).default('position')
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_animation_clip', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_setup_blend_tree', {
        title: 'Setup Blend Tree',
        description: 'Create a blend tree for smooth animation transitions',
        inputSchema: z.object({
            controllerPath: z.string().describe('Path to the Animator Controller'),
            stateName: z.string().default('Movement'),
            blendType: z.enum(['1D', '2D']).default('1D'),
            parameterName: z.string().default('Speed'),
            layerIndex: z.number().default(0)
        })
    }, async (params) => {
        const result = await sendUnityCommand('setup_blend_tree', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_add_animation_transition', {
        title: 'Add Animation Transition',
        description: 'Create a transition between animation states',
        inputSchema: z.object({
            controllerPath: z.string().describe('Path to the Animator Controller'),
            fromState: z.string().describe('Source state name (use "Any" for Any State)'),
            toState: z.string().describe('Destination state name'),
            condition: z.string().optional().describe('Parameter name for condition'),
            conditionValue: z.string().optional().describe('Value for condition'),
            hasExitTime: z.boolean().default(true),
            transitionDuration: z.number().default(0.25),
            layerIndex: z.number().default(0)
        })
    }, async (params) => {
        const result = await sendUnityCommand('add_animation_transition', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_setup_animation_layer', {
        title: 'Setup Animation Layer',
        description: 'Add and configure an animation layer',
        inputSchema: z.object({
            controllerPath: z.string().describe('Path to the Animator Controller'),
            layerName: z.string().default('NewLayer'),
            weight: z.number().default(1),
            blendMode: z.enum(['override', 'additive']).default('override'),
            avatarMaskPath: z.string().optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('setup_animation_layer', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_create_animation_event', {
        title: 'Create Animation Event',
        description: 'Add an event to an animation clip',
        inputSchema: z.object({
            clipPath: z.string().describe('Path to the animation clip'),
            time: z.number().default(0.5).describe('Time in seconds'),
            functionName: z.string().default('OnAnimationEvent'),
            stringParameter: z.string().optional(),
            floatParameter: z.number().optional(),
            intParameter: z.number().optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_animation_event', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_setup_avatar', {
        title: 'Setup Avatar',
        description: 'Configure avatar for a 3D model',
        inputSchema: z.object({
            modelPath: z.string().describe('Path to the 3D model'),
            avatarName: z.string().default('NewAvatar'),
            isHumanoid: z.boolean().default(true),
            rootBone: z.string().optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('setup_avatar', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_create_timeline', {
        title: 'Create Timeline',
        description: 'Create a Unity Timeline for cinematic sequences',
        inputSchema: z.object({
            name: z.string().default('NewTimeline'),
            path: z.string().default('Assets/Timelines/'),
            duration: z.number().default(10),
            frameRate: z.number().default(30),
            targetObject: z.string().optional()
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_timeline', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_bake_animation', {
        title: 'Bake Animation',
        description: 'Bake runtime animation into an animation clip',
        inputSchema: z.object({
            sourceObject: z.string().describe('GameObject with animation to bake'),
            animationName: z.string().default('BakedAnimation'),
            startFrame: z.number().default(0),
            endFrame: z.number().default(60),
            frameRate: z.number().default(30),
            path: z.string().default('Assets/Animations/Baked/')
        })
    }, async (params) => {
        const result = await sendUnityCommand('bake_animation', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    // ===== UI詳細構築ツール =====
    
    mcpServer.registerTool('unity_setup_ui_anchors', {
        title: 'Setup UI Anchors',
        description: 'Automatically setup anchors and pivots for UI elements',
        inputSchema: z.object({
            targetObject: z.string().describe('Target GameObject name'),
            anchorPreset: z.enum([
                'top-left', 'top-center', 'top-right',
                'middle-left', 'center', 'middle-right', 
                'bottom-left', 'bottom-center', 'bottom-right',
                'stretch-horizontal', 'stretch-vertical', 'stretch-all'
            ]).default('center'),
            pivotPreset: z.enum([
                'top-left', 'top-center', 'top-right',
                'middle-left', 'center', 'middle-right',
                'bottom-left', 'bottom-center', 'bottom-right'
            ]).default('center'),
            margin: z.number().default(10),
            recursive: z.boolean().default(false)
        })
    }, async (params) => {
        const result = await sendUnityCommand('setup_ui_anchors', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_create_responsive_ui', {
        title: 'Create Responsive UI',
        description: 'Create responsive UI container with layout groups',
        inputSchema: z.object({
            containerName: z.string().default('ResponsiveContainer'),
            layoutType: z.enum(['horizontal', 'vertical']).default('horizontal'),
            spacing: z.number().default(10),
            padding: z.number().default(20),
            childAlignment: z.enum([
                'upper-left', 'upper-center', 'upper-right',
                'middle-left', 'middle-center', 'middle-right',
                'lower-left', 'lower-center', 'lower-right'
            ]).default('middle-center'),
            useContentSizeFitter: z.boolean().default(true)
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_responsive_ui', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_setup_ui_animation', {
        title: 'Setup UI Animation',
        description: 'Setup UI animations for elements (fade, scale, slide)',
        inputSchema: z.object({
            targetObject: z.string().describe('Target GameObject name'),
            animationType: z.enum(['fade', 'scale', 'slide-left', 'slide-up', 'scale-fade']).default('fade'),
            duration: z.number().default(0.5),
            delay: z.number().default(0),
            easing: z.string().default('ease'),
            autoPlay: z.boolean().default(false)
        })
    }, async (params) => {
        const result = await sendUnityCommand('setup_ui_animation', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_create_ui_grid', {
        title: 'Create UI Grid',
        description: 'Create UI grid layout with customizable elements',
        inputSchema: z.object({
            gridName: z.string().default('UIGrid'),
            columns: z.number().default(3),
            rows: z.number().default(3),
            cellSize: z.string().default('100,100').describe('Cell size as "width,height"'),
            spacing: z.string().default('10,10').describe('Spacing as "x,y"'),
            padding: z.string().default('10,10,10,10').describe('Padding as "left,right,top,bottom"'),
            fillType: z.enum(['button', 'image', 'text', 'toggle']).default('button')
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_ui_grid', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_setup_scroll_view', {
        title: 'Setup Scroll View',
        description: 'Create complete scroll view with content and scrollbars',
        inputSchema: z.object({
            scrollViewName: z.string().default('ScrollView'),
            scrollDirection: z.enum(['vertical', 'horizontal']).default('vertical'),
            contentType: z.enum(['text', 'button', 'image']).default('text'),
            itemCount: z.number().default(10),
            itemSize: z.string().default('200,50').describe('Item size as "width,height"'),
            useScrollbar: z.boolean().default(true),
            elasticity: z.number().default(0.1)
        })
    }, async (params) => {
        const result = await sendUnityCommand('setup_scroll_view', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_create_ui_notification', {
        title: 'Create UI Notification',
        description: 'Create notification system with different types and positions',
        inputSchema: z.object({
            notificationName: z.string().default('NotificationSystem'),
            notificationType: z.enum(['toast', 'success', 'warning', 'error']).default('toast'),
            position: z.enum(['top-left', 'top-center', 'top-right', 'center', 'bottom-center']).default('top-right'),
            animationType: z.enum(['slide', 'fade', 'scale', 'none']).default('slide'),
            autoHide: z.boolean().default(true),
            hideDelay: z.number().default(3)
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_ui_notification', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_setup_ui_navigation', {
        title: 'Setup UI Navigation',
        description: 'Create UI navigation system (tabs, buttons, toggles)',
        inputSchema: z.object({
            navigationName: z.string().default('UINavigation'),
            navigationType: z.enum(['tab', 'button', 'toggle']).default('tab'),
            itemCount: z.number().default(3),
            orientation: z.enum(['horizontal', 'vertical']).default('horizontal'),
            selectedIndex: z.number().default(0)
        })
    }, async (params) => {
        const result = await sendUnityCommand('setup_ui_navigation', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_create_ui_dialog', {
        title: 'Create UI Dialog',
        description: 'Create modal dialogs (confirmation, alert, input)',
        inputSchema: z.object({
            dialogName: z.string().default('Dialog'),
            dialogType: z.enum(['confirmation', 'alert', 'input']).default('confirmation'),
            title: z.string().default('Dialog Title'),
            message: z.string().default('Dialog message content'),
            hasOverlay: z.boolean().default(true),
            isModal: z.boolean().default(true)
        })
    }, async (params) => {
        const result = await sendUnityCommand('create_ui_dialog', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_optimize_ui_canvas', {
        title: 'Optimize UI Canvas',
        description: 'Optimize Canvas for performance, quality, or mobile',
        inputSchema: z.object({
            canvasName: z.string().optional().describe('Specific canvas name (leave empty for first found)'),
            optimizationType: z.enum(['performance', 'quality', 'mobile']).default('performance'),
            targetFrameRate: z.number().default(60),
            enablePixelPerfect: z.boolean().default(false)
        })
    }, async (params) => {
        const result = await sendUnityCommand('optimize_ui_canvas', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    mcpServer.registerTool('unity_setup_safe_area', {
        title: 'Setup Safe Area',
        description: 'Setup Safe Area for mobile devices with notch support',
        inputSchema: z.object({
            safeAreaName: z.string().default('SafeAreaContainer'),
            targetObject: z.string().optional().describe('Target object (leave empty to create new)'),
            applyToCanvas: z.boolean().default(false),
            includeNotch: z.boolean().default(true)
        })
    }, async (params) => {
        const result = await sendUnityCommand('setup_safe_area', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });
    
    // ===== スクリプト編集機能 =====
    mcpServer.registerTool('unity_modify_script', {
        title: 'Modify Script',
        description: 'Edit existing Unity scripts for error fixes and code modifications. Either scriptPath OR fileName is required.',
        inputSchema: z.object({
            scriptPath: z.string().optional().describe('Full path to script (e.g., Assets/Scripts/MyScript.cs) - use this OR fileName'),
            fileName: z.string().optional().describe('Just the script name (e.g., MyScript.cs or MyScript) - use this OR scriptPath'),
            operation: z.enum(['replace', 'insert', 'append', 'prepend']).optional().default('replace').describe('Type of modification operation'),
            content: z.string().describe('New content to add or replace'),
            searchText: z.string().optional().describe('Text to search for when using replace or insert operations'),
            lineNumber: z.number().optional().describe('Line number for line-specific operations (1-based)')
        })
    }, async (params) => {
        const result = await sendUnityCommand('modify_script', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_edit_script_line', {
        title: 'Edit Script Line',
        description: 'Edit a specific line in a Unity script. Provide either scriptPath OR fileName to locate the file.',
        inputSchema: z.object({
            scriptPath: z.string().optional().describe('Full path (e.g., Assets/Scripts/MyScript.cs) - use this OR fileName'),
            fileName: z.string().optional().describe('Script name (e.g., MyScript.cs or just MyScript) - use this OR scriptPath'),
            lineNumber: z.number().describe('Line number to edit (1-based)'),
            newContent: z.string().describe('New content for the line')
        })
    }, async (params) => {
        const result = await sendUnityCommand('edit_script_line', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_add_script_method', {
        title: 'Add Script Method',
        description: 'Add a new method to a Unity script. Provide either scriptPath OR fileName to locate the file.',
        inputSchema: z.object({
            scriptPath: z.string().optional().describe('Full path (e.g., Assets/Scripts/MyScript.cs) - use this OR fileName'),
            fileName: z.string().optional().describe('Script name (e.g., MyScript.cs or just MyScript) - use this OR scriptPath'),
            methodName: z.string().describe('Name of the method to add'),
            methodContent: z.string().describe('Complete method implementation with proper indentation'),
            insertAfter: z.string().optional().describe('Insert after this method/pattern (optional, defaults to end of class)')
        })
    }, async (params) => {
        const result = await sendUnityCommand('add_script_method', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    mcpServer.registerTool('unity_update_script_variable', {
        title: 'Update Script Variable',
        description: 'Update variable declaration or value in a Unity script',
        inputSchema: z.object({
            scriptPath: z.string().optional().describe('Path to the script file'),
            fileName: z.string().optional().describe('Name of the script file to find'),
            variableName: z.string().describe('Name of the variable to update'),
            newDeclaration: z.string().describe('New variable declaration (e.g., "public float speed = 10f;")'),
            updateType: z.enum(['declaration', 'value']).optional().default('declaration').describe('Update type: declaration or just value')
        })
    }, async (params) => {
        const result = await sendUnityCommand('update_script_variable', params);
        return {
            content: [{
                type: 'text',
                text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
            }]
        };
    });

    // リソース定義
    mcpServer.registerResource('unity://project-stats', {
        title: 'Unity Project Statistics',
        description: 'Get project statistics and implementation status',
        mimeType: 'application/json'
    }, async () => {
        // registeredToolsプロパティを直接チェック
        const toolCount = mcpServer._private ? Object.keys(mcpServer._private.registeredTools || {}).length : 0;
        
        return {
            content: [
                {
                    type: 'text',
                    text: JSON.stringify({
                        implementedTools: toolCount,
                        status: 'active',
                        unityConnection: unityWebSocket && unityWebSocket.readyState === WebSocket.OPEN
                    }, null, 2)
                }
            ]
        };
    });

    // 登録されたツールの数を確認（ログは出力しない）
    
    // 全てのサーバーを起動
    await mcpServer.start();
}

// HTTPエンドポイント
app.get('/health', (req, res) => {
    res.json({ 
        status: 'ok',
        unityConnected: unityWebSocket !== null && unityWebSocket.readyState === WebSocket.OPEN
    });
});

// サーバー起動
async function startServer() {
    // 最初にMCPサーバーをセットアップ
    await setupMCPServer();
    
    // WebSocketサーバーを作成
    wss = new WebSocket.Server({ server });
    setupWebSocketHandlers();
    
    const port = process.env.PORT || 8080;
    server.listen(port, () => {
        // サーバー起動（ログを出力しない）
    });
}

// プロセス終了時のクリーンアップ
// 終了処理用の共通関数
function shutdownServer() {
    // Unity WebSocket接続を閉じる
    if (unityWebSocket && unityWebSocket.readyState === WebSocket.OPEN) {
        unityWebSocket.close();
    }
    
    // WebSocketサーバーを閉じる
    if (wss) {
        wss.close();
    }
    
    // HTTPサーバーを閉じる
    if (server && server.listening) {
        server.close(() => {
            process.exit(0);
        });
    } else {
        process.exit(0);
    }
    
    // 5秒後に強制終了
    setTimeout(() => {
        process.exit(1);
    }, 5000);
}

process.on('SIGINT', shutdownServer);
process.on('SIGTERM', shutdownServer);

// stdioが閉じられた時も終了
process.stdin.on('close', () => {
    shutdownServer();
});

// エラーハンドリング
process.on('uncaughtException', (error) => {
    // console.error('[MCP Server] Uncaught Exception:', error);
});

process.on('unhandledRejection', (reason, promise) => {
    // console.error('[MCP Server] Unhandled Rejection at:', promise, 'reason:', reason);
});

startServer().catch(err => {
    // console.error(err);
});