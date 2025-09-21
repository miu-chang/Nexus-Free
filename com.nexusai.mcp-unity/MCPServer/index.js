// Nexus AI MCP Unity Integration - UI Edition
// This is the free UI-focused version with 23 essential tools only
// For the full version with 147+ tools, please upgrade to Nexus Pro

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

// 自動シャットダウン機能
let shutdownTimer = null;
let connectionCount = 0;

function checkAndScheduleShutdown() {
    // 接続がすべて切れた場合、30秒後に自動終了
    if (connectionCount === 0) {
        console.log('All connections closed. Scheduling shutdown in 30 seconds...');
        shutdownTimer = setTimeout(() => {
            console.log('No active connections for 30 seconds. Shutting down MCP server...');
            process.exit(0);
        }, 30000); // 30秒
    } else if (shutdownTimer) {
        // 新しい接続があったらタイマーをキャンセル
        clearTimeout(shutdownTimer);
        shutdownTimer = null;
    }
}

function updateConnectionCount(delta) {
    connectionCount += delta;
    console.log(`Active connections: ${connectionCount}`);
    checkAndScheduleShutdown();
}

// Unity WebSocket接続の管理（関数として定義）
function setupWebSocketHandlers() {
    if (!wss) return;
    
    wss.on('connection', (ws, req) => {
        updateConnectionCount(1); // 接続時にカウント増加
        
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
                            message: data.message
                        }));
                    }
                } catch (error) {
                    console.error('Desktop app message error:', error);
                }
            });
            
            ws.on('close', () => {
                if (desktopAppSocket === ws) {
                    desktopAppSocket = null;
                    bridgeHandler.clearDesktopConnection();
                }
                updateConnectionCount(-1); // 切断時にカウント減少
            });
        } else {
            // Unity接続
            if (unityWebSocket) {
                unityWebSocket.close();
            }
            unityWebSocket = ws;
            
            // ブリッジハンドラーにUnity接続を設定
            bridgeHandler.setUnityConnection(ws);
            
            ws.on('message', async (message) => {
                try {
                    const data = JSON.parse(message);
                    
                    if (data.type === 'tool_response') {
                        // MCPサーバーからのレスポンスを処理
                        bridgeHandler.handleMCPResponse(data.result);
                    }
                } catch (error) {
                    console.error('Unity WebSocket message error:', error);
                }
            });
            
            ws.on('close', () => {
                if (unityWebSocket === ws) {
                    unityWebSocket = null;
                    bridgeHandler.clearUnityConnection();
                }
                updateConnectionCount(-1); // 切断時にカウント減少
            });
        }
    });
}

// MCPサーバーの初期化
async function initializeMCPServer() {
    try {
        // Unityにコマンドを送信する関数
        const sendUnityCommand = async (operation, parameters = {}) => {
            if (!unityWebSocket || unityWebSocket.readyState !== WebSocket.OPEN) {
                throw new Error('Unity is not connected');
            }
            
            return new Promise((resolve, reject) => {
                const requestId = Date.now().toString();
                
                const timeoutId = setTimeout(() => {
                    bridgeHandler.clearPendingRequest(requestId);
                    reject(new Error('Unity command timeout'));
                }, 30000); // 30秒のタイムアウト
                
                // リクエストを保存
                bridgeHandler.savePendingRequest(requestId, { resolve, reject, timeoutId });
                
                // Unityに送信
                unityWebSocket.send(JSON.stringify({
                    type: 'tool_request',
                    requestId,
                    operation,
                    parameters
                }));
            });
        };
        
        mcpServer = await createServer();
        
        // ===== UI作成ツール (9 tools) =====
        
        mcpServer.registerTool('unity_create_ui', {
            title: 'Create Unity UI',
            description: 'Create UI elements like Canvas, Button, Text, Image, Panel, InputField, Slider, Toggle, Dropdown, ScrollView',
            inputSchema: z.object({
                uiType: z.enum(['canvas', 'button', 'text', 'image', 'panel', 'inputfield', 'slider', 'toggle', 'dropdown', 'scrollview']),
                name: z.string().optional(),
                parent: z.string().optional(),
                text: z.string().optional(),
                position: z.object({
                    x: z.number(),
                    y: z.number(),
                    z: z.number().optional()
                }).optional(),
                size: z.object({
                    width: z.number(),
                    height: z.number()
                }).optional(),
                anchorPreset: z.enum(['top-left', 'top-center', 'top-right', 'middle-left', 'middle-center', 'middle-right', 'bottom-left', 'bottom-center', 'bottom-right', 'stretch-stretch']).optional()
            })
        }, async (params) => {
            const result = await sendUnityCommand('CREATE_UI', params);
            return {
                content: [{
                    type: 'text',
                    text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
                }]
            };
        });
        
        mcpServer.registerTool('unity_setup_ui_anchors', {
            title: 'Setup UI Anchors',
            description: 'Configure UI element anchoring and positioning',
            inputSchema: z.object({
                objectName: z.string(),
                preset: z.enum(['top-left', 'top-center', 'top-right', 'middle-left', 'middle-center', 'middle-right', 'bottom-left', 'bottom-center', 'bottom-right', 'stretch-horizontal', 'stretch-vertical', 'stretch-stretch']).optional(),
                customAnchors: z.object({
                    minX: z.number(),
                    minY: z.number(),
                    maxX: z.number(),
                    maxY: z.number()
                }).optional()
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
        
        mcpServer.registerTool('unity_setup_ui_animation', {
            title: 'Setup UI Animation',
            description: 'Add animations to UI elements',
            inputSchema: z.object({
                objectName: z.string(),
                animationType: z.enum(['fade', 'slide', 'scale', 'rotate']),
                duration: z.number().default(0.5),
                easing: z.enum(['linear', 'ease-in', 'ease-out', 'ease-in-out']).default('ease-in-out')
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
            description: 'Create a grid layout for UI elements',
            inputSchema: z.object({
                name: z.string().default('UIGrid'),
                rows: z.number().default(3),
                columns: z.number().default(3),
                spacing: z.object({
                    x: z.number().default(10),
                    y: z.number().default(10)
                }).optional(),
                cellSize: z.object({
                    width: z.number().default(100),
                    height: z.number().default(100)
                }).optional()
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
        
        mcpServer.registerTool('unity_create_ui_notification', {
            title: 'Create UI Notification',
            description: 'Create a notification popup UI',
            inputSchema: z.object({
                message: z.string(),
                type: z.enum(['info', 'success', 'warning', 'error']).default('info'),
                duration: z.number().default(3),
                position: z.enum(['top', 'center', 'bottom']).default('top')
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
            description: 'Configure navigation between UI elements',
            inputSchema: z.object({
                elements: z.array(z.string()).describe('UI element names to connect'),
                navigationMode: z.enum(['automatic', 'horizontal', 'vertical', 'explicit']).default('automatic')
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
            description: 'Create a dialog/modal window',
            inputSchema: z.object({
                title: z.string(),
                message: z.string(),
                buttons: z.array(z.string()).default(['OK', 'Cancel']),
                modal: z.boolean().default(true)
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
            description: 'Optimize UI Canvas settings for performance',
            inputSchema: z.object({
                canvasName: z.string().optional(),
                pixelPerfect: z.boolean().default(false),
                optimizeForMobile: z.boolean().default(false)
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
        
        mcpServer.registerTool('unity_get_ui_info', {
            title: 'Get UI Information',
            description: 'Get information about UI elements in the scene',
            inputSchema: z.object({
                includeInactive: z.boolean().default(false)
            })
        }, async (params) => {
            const result = await sendUnityCommand('get_ui_info', params);
            return {
                content: [{
                    type: 'text',
                    text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
                }]
            };
        });
        
        // ===== 基本的なGameObject操作 (9 tools) =====
        
        mcpServer.registerTool('unity_create_gameobject', {
            title: 'Create GameObject',
            description: 'Create a new GameObject in Unity',
            inputSchema: z.object({
                name: z.string().optional(),
                primitiveType: z.enum(['cube', 'sphere', 'cylinder', 'plane', 'capsule', 'empty']).optional(),
                position: z.object({
                    x: z.number().default(0),
                    y: z.number().default(0),
                    z: z.number().default(0)
                }).optional(),
                rotation: z.object({
                    x: z.number().default(0),
                    y: z.number().default(0),
                    z: z.number().default(0)
                }).optional(),
                scale: z.object({
                    x: z.number().default(1),
                    y: z.number().default(1),
                    z: z.number().default(1)
                }).optional(),
                parent: z.string().optional()
            })
        }, async (params) => {
            const result = await sendUnityCommand('CREATE_GAMEOBJECT', params);
            return {
                content: [{
                    type: 'text',
                    text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
                }]
            };
        });
        
        mcpServer.registerTool('unity_update_gameobject', {
            title: 'Update GameObject',
            description: 'Update an existing GameObject',
            inputSchema: z.object({
                name: z.string(),
                newName: z.string().optional(),
                active: z.boolean().optional(),
                layer: z.string().optional(),
                tag: z.string().optional()
            })
        }, async (params) => {
            const result = await sendUnityCommand('UPDATE_GAMEOBJECT', params);
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
                name: z.string()
            })
        }, async (params) => {
            const result = await sendUnityCommand('DELETE_GAMEOBJECT', params);
            return {
                content: [{
                    type: 'text',
                    text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
                }]
            };
        });
        
        mcpServer.registerTool('unity_set_transform', {
            title: 'Set Transform',
            description: 'Set the transform properties of a GameObject',
            inputSchema: z.object({
                objectName: z.string(),
                position: z.object({
                    x: z.number(),
                    y: z.number(),
                    z: z.number()
                }).optional(),
                rotation: z.object({
                    x: z.number(),
                    y: z.number(),
                    z: z.number()
                }).optional(),
                scale: z.object({
                    x: z.number(),
                    y: z.number(),
                    z: z.number()
                }).optional(),
                space: z.enum(['world', 'local']).default('world')
            })
        }, async (params) => {
            const result = await sendUnityCommand('SET_TRANSFORM', params);
            return {
                content: [{
                    type: 'text',
                    text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
                }]
            };
        });
        
        mcpServer.registerTool('unity_add_component', {
            title: 'Add Component',
            description: 'Add a component to a GameObject',
            inputSchema: z.object({
                objectName: z.string(),
                componentType: z.string()
            })
        }, async (params) => {
            const result = await sendUnityCommand('ADD_COMPONENT', params);
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
                objectName: z.string(),
                componentType: z.string(),
                properties: z.record(z.any())
            })
        }, async (params) => {
            const result = await sendUnityCommand('UPDATE_COMPONENT', params);
            return {
                content: [{
                    type: 'text',
                    text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
                }]
            };
        });
        
        mcpServer.registerTool('unity_set_property', {
            title: 'Set Property',
            description: 'Set a property on a GameObject or component',
            inputSchema: z.object({
                objectName: z.string(),
                componentType: z.string().optional(),
                propertyPath: z.string(),
                value: z.any()
            })
        }, async (params) => {
            const result = await sendUnityCommand('SET_PROPERTY', params);
            return {
                content: [{
                    type: 'text',
                    text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
                }]
            };
        });
        
        mcpServer.registerTool('unity_get_gameobject_details', {
            title: 'Get GameObject Details',
            description: 'Get detailed information about a GameObject',
            inputSchema: z.object({
                name: z.string(),
                includeComponents: z.boolean().default(true),
                includeChildren: z.boolean().default(false)
            })
        }, async (params) => {
            const result = await sendUnityCommand('GET_GAMEOBJECT_DETAILS', params);
            return {
                content: [{
                    type: 'text',
                    text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
                }]
            };
        });
        
        mcpServer.registerTool('unity_get_scene_info', {
            title: 'Get Scene Information',
            description: 'Get information about the current scene',
            inputSchema: z.object({
                includeHierarchy: z.boolean().default(true),
                includeLighting: z.boolean().default(false),
                includeStats: z.boolean().default(true)
            })
        }, async (params) => {
            const result = await sendUnityCommand('GET_SCENE_INFO', params);
            return {
                content: [{
                    type: 'text',
                    text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
                }]
            };
        });
        
        // ===== プロジェクト管理ツール (5 tools) =====
        
        mcpServer.registerTool('unity_create_checkpoint', {
            title: 'Create Checkpoint',
            description: 'Save the current state as a checkpoint',
            inputSchema: z.object({
                name: z.string(),
                description: z.string().optional()
            })
        }, async (params) => {
            const result = await sendUnityCommand('CREATE_CHECKPOINT', params);
            return {
                content: [{
                    type: 'text',
                    text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
                }]
            };
        });
        
        mcpServer.registerTool('unity_restore_checkpoint', {
            title: 'Restore Checkpoint',
            description: 'Restore a previously saved checkpoint',
            inputSchema: z.object({
                name: z.string()
            })
        }, async (params) => {
            const result = await sendUnityCommand('RESTORE_CHECKPOINT', params);
            return {
                content: [{
                    type: 'text',
                    text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
                }]
            };
        });
        
        mcpServer.registerTool('unity_get_history', {
            title: 'Get Operation History',
            description: 'Get the history of operations',
            inputSchema: z.object({
                limit: z.number().default(10)
            })
        }, async (params) => {
            const result = await sendUnityCommand('GET_HISTORY', params);
            return {
                content: [{
                    type: 'text',
                    text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
                }]
            };
        });
        
        mcpServer.registerTool('unity_undo_operation', {
            title: 'Undo Operation',
            description: 'Undo the last operation',
            inputSchema: z.object({})
        }, async (params) => {
            const result = await sendUnityCommand('UNDO', params);
            return {
                content: [{
                    type: 'text',
                    text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
                }]
            };
        });
        
        mcpServer.registerTool('unity_redo_operation', {
            title: 'Redo Operation',
            description: 'Redo the last undone operation',
            inputSchema: z.object({})
        }, async (params) => {
            const result = await sendUnityCommand('REDO', params);
            return {
                content: [{
                    type: 'text',
                    text: typeof result === 'string' ? result : JSON.stringify(result, null, 2)
                }]
            };
        });
        
        console.log('MCP Server initialized with UI Edition tools (23 tools)');
        
        // MCPサーバーを起動（stdio経由でClaudeと通信）
        await mcpServer.start();
        console.log('MCP Server started successfully');
    } catch (error) {
        console.error('Failed to initialize MCP server:', error);
        throw error;
    }
}

// サーバー起動
const PORT = process.env.PORT || 8080;

server.listen(PORT, async () => {
    // WebSocketサーバーの作成と設定
    wss = new WebSocket.Server({ server });
    setupWebSocketHandlers();
    console.log(`Nexus MCP Unity Integration - UI Edition`);
    console.log(`Server running on port ${PORT}`);
    console.log(`This version includes 23 essential UI and GameObject tools`);
    console.log(`For 147+ professional tools, upgrade to Nexus Pro`);
    
    try {
        await initializeMCPServer();
        console.log('MCP Server initialized successfully');
    } catch (error) {
        console.error('Failed to initialize MCP server:', error);
        process.exit(1);
    }
});


// エラーハンドリング
process.on('uncaughtException', (error) => {
    console.error('Uncaught Exception:', error);
});

process.on('unhandledRejection', (reason, promise) => {
    console.error('Unhandled Rejection at:', promise, 'reason:', reason);
});

// グレースフルシャットダウン
process.on('SIGTERM', () => {
    console.log('Received SIGTERM, shutting down gracefully...');
    if (wss) {
        wss.close(() => {
            process.exit(0);
        });
    } else {
        process.exit(0);
    }
});

process.on('SIGINT', () => {
    console.log('Received SIGINT, shutting down gracefully...');
    if (wss) {
        wss.close(() => {
            process.exit(0);
        });
    } else {
        process.exit(0);
    }
});