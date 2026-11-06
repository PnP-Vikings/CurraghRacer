using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AwesomeTaskManager.Next.Mcp;

namespace AwesomeTaskManager.Next
{
    /// <summary>
    /// Provides a file-based command bridge for external AI agents (like Junie, Rider AI, Claude Code)
    /// to invoke ATM Next MCP tools without being directly connected to the Unity MCP server.
    /// 
    /// Usage: Write a JSON file to ProjectSettings/AwesomeTaskManager/external_command.json
    /// Format: { "tool": "atm_create_task", "parameters": { "Board": "Active Board", "Title": "Hello from Rider" } }
    /// Result will be written to ProjectSettings/AwesomeTaskManager/external_result.json
    /// </summary>
    [InitializeOnLoad]
    public static class ATMNextCommandBridge
    {
        private const string CommandFileName = "external_command.json";
        private const string ResultFileName = "external_result.json";
        private static string _commandPath;
        private static string _resultPath;

        static ATMNextCommandBridge()
        {
            string projectSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "ProjectSettings", "AwesomeTaskManager");
            if (!Directory.Exists(projectSettingsPath))
            {
                Directory.CreateDirectory(projectSettingsPath);
            }
            
            _commandPath = Path.Combine(projectSettingsPath, CommandFileName);
            _resultPath = Path.Combine(projectSettingsPath, ResultFileName);

            // Using EditorApplication.update for simple polling. 
            // FileSystemWatcher can sometimes be unreliable with certain IDE/OS combinations in Unity.
            EditorApplication.update += CheckForCommand;
        }

        private static void CheckForCommand()
        {
            // Avoid processing during compilation or when Unity is busy
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }
            
            if (File.Exists(_commandPath))
            {
                ProcessCommand();
            }
        }

        private static void ProcessCommand()
        {
            string json;
            try
            {
                // Read and then immediately delete to acknowledge receipt
                json = File.ReadAllText(_commandPath);
                File.Delete(_commandPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ATMNext] Failed to read command file: {e.Message}");
                return;
            }

            try
            {
                var command = JsonConvert.DeserializeObject<JObject>(json);
                if (command == null) return;

                string toolName = command["tool"]?.ToString();
                JToken parameters = command["parameters"];

                if (string.IsNullOrEmpty(toolName))
                {
                    WriteResult(new { success = false, message = "Missing 'tool' field in command JSON." });
                    return;
                }

                if (string.Equals(toolName, "atm_refresh", StringComparison.OrdinalIgnoreCase) || string.Equals(toolName, "refresh", StringComparison.OrdinalIgnoreCase))
                {
                    AssetDatabase.Refresh();
                    WriteResult(new { success = true, message = "AssetDatabase refreshed." });
                    return;
                }

                ExecuteTool(toolName, parameters);
            }
            catch (Exception e)
            {
                WriteResult(new { success = false, message = $"JSON parse error: {e.Message}" });
            }
        }

        private static void ExecuteTool(string toolName, JToken parameters)
        {
            var type = typeof(ATMNextMcpTools);
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

            foreach (var method in methods)
            {
                // 1. Try matching by McpTool attribute name
                var attrs = method.GetCustomAttributes(false);
                foreach (var attr in attrs)
                {
                    var attrType = attr.GetType();
                    if (attrType.Name == "McpToolAttribute")
                    {
                        var nameProp = attrType.GetProperty("Name");
                        string registeredName = nameProp?.GetValue(attr) as string;
                        if (string.Equals(registeredName, toolName, StringComparison.OrdinalIgnoreCase))
                        {
                            InvokeMethod(method, parameters);
                            return;
                        }
                    }
                }

                // 2. Try matching by method name directly
                if (string.Equals(method.Name, toolName, StringComparison.OrdinalIgnoreCase))
                {
                    InvokeMethod(method, parameters);
                    return;
                }
                
                // 3. Try matching by snake_case conversion
                if (string.Equals("atm_" + ToSnakeCase(method.Name), toolName, StringComparison.OrdinalIgnoreCase))
                {
                     InvokeMethod(method, parameters);
                     return;
                }
            }

            WriteResult(new { success = false, message = $"Tool '{toolName}' not found in ATMNextMcpTools." });
        }

        private static string ToSnakeCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return string.Concat(text.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }

        private static void InvokeMethod(MethodInfo method, JToken parameters)
        {
            try
            {
                var paramInfos = method.GetParameters();
                object[] invokeParams = null;

                if (paramInfos.Length > 0)
                {
                    var paramType = paramInfos[0].ParameterType;
                    // Use Newtonsoft to deserialize the parameters into the specific Params object type
                    object paramInstance = parameters != null ? parameters.ToObject(paramType) : Activator.CreateInstance(paramType);
                    invokeParams = new[] { paramInstance };
                }

                object result = method.Invoke(null, invokeParams);
                WriteResult(result);
            }
            catch (TargetInvocationException tie)
            {
                WriteResult(new { success = false, message = $"Error: {tie.InnerException?.Message ?? tie.Message}" });
            }
            catch (Exception e)
            {
                WriteResult(new { success = false, message = $"Invocation error: {e.Message}" });
            }
        }

        private static void WriteResult(object result)
        {
            try
            {
                string json = JsonConvert.SerializeObject(result, Formatting.Indented);
                File.WriteAllText(_resultPath, json);
                Debug.Log($"[ATMNext] Command Bridge: Executed tool and saved result to {ResultFileName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ATMNext] Failed to write result file: {e.Message}");
            }
        }
    }
}
