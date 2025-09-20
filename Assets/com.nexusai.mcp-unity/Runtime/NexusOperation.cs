using System;
using System.Collections.Generic;

namespace NexusAIConnect
{
    /// <summary>
    /// Unity操作を表すデータクラス
    /// MCPサーバーやAIとの連携で使用
    /// </summary>
    [Serializable]
    public class NexusUnityOperation
    {
        public string id;
        public string type;
        public Dictionary<string, string> parameters;
        public string code;
        public string description;
        public List<string> dependencies;
        public OperationStatus status;
        
        public enum OperationStatus
        {
            Pending,
            Executing,
            Completed,
            Failed,
            Skipped
        }
        
        public NexusUnityOperation()
        {
            id = Guid.NewGuid().ToString();
            parameters = new Dictionary<string, string>();
            dependencies = new List<string>();
            status = OperationStatus.Pending;
        }
        
        public NexusUnityOperation(string operationType) : this()
        {
            type = operationType;
        }
    }
    
    /// <summary>
    /// 操作結果を表すクラス
    /// </summary>
    [Serializable]
    public class NexusOperationResult
    {
        public bool success;
        public string message;
        public string operationId;
        public object resultData;
        
        public NexusOperationResult(string opId, bool isSuccess, string msg)
        {
            operationId = opId;
            success = isSuccess;
            message = msg;
        }
    }
}