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
        private static string _tempCommandPath;
        private static string _tempResultPath;

        static ATMNextCommandBridge()
        {
            string projectSettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "ProjectSettings", "AwesomeTaskManager");
            if (!Directory.Exists(projectSettingsPath))
            {
                Directory.CreateDirectory(projectSettingsPath);
            }
            
            _commandPath = Path.Combine(projectSettingsPath, CommandFileName);
            _resultPath = Path.Combine(projectSettingsPath, ResultFileName);

            string tempPath = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "AwesomeTaskManager");
            _tempCommandPath = Path.Combine(tempPath, CommandFileName);
            _tempResultPath = Path.Combine(tempPath, ResultFileName);

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
                ProcessCommand(_commandPath, _resultPath);
            }
            else if (File.Exists(_tempCommandPath))
            {
                ProcessCommand(_tempCommandPath, _tempResultPath);
            }
        }

        public static void ProcessCommand(string commandPath = null, string resultPath = null)
        {
            commandPath ??= _commandPath;
            resultPath ??= _resultPath;

            string json = null;
            for (int retry = 0; retry < 5; retry++)
            {
                try
                {
                    if (!File.Exists(commandPath)) return;
                    using (var fs = new FileStream(commandPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
                    {
                        json = sr.ReadToEnd();
                    }
                    File.Delete(commandPath);
                    break;
                }
                catch (IOException)
                {
                    System.Threading.Thread.Sleep(20);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ATMNext] Failed to read command file: {e.Message}");
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(json)) return;

            // Clear any old result file before executing so caller never reads stale data
            try
            {
                if (File.Exists(resultPath))
                {
                    File.Delete(resultPath);
                }
            }
            catch {}

            object result = ExecuteCommand(json);
            WriteResult(AddRequestIdToResult(result, json), resultPath);
        }

        private static object AddRequestIdToResult(object result, string commandJson)
        {
            try
            {
                JToken requestId = JObject.Parse(commandJson)["requestId"];
                if (requestId == null) return result;

                JToken resultToken = result as JToken ?? (result == null ? JValue.CreateNull() : JToken.FromObject(result));
                JObject resultObject = resultToken as JObject;
                if (resultObject != null)
                {
                    resultObject.Remove("requestId");
                    resultObject.AddFirst(new JProperty("requestId", requestId.DeepClone()));
                    return resultObject;
                }

                var resultEnvelope = new JObject
                {
                    ["requestId"] = requestId.DeepClone(),
                    ["result"] = resultToken
                };
                return resultEnvelope;
            }
            catch
            {
                return result;
            }
        }

        /// <summary>
        /// Directly executes a JSON command string and returns the result object.
        /// </summary>
        public static object ExecuteCommand(string json)
        {
            try
            {
                var command = JsonConvert.DeserializeObject<JObject>(json);
                if (command == null) return new { success = false, message = "Empty command JSON." };

                string toolName = command["tool"]?.ToString();
                JToken parameters = command["parameters"];

                if (string.IsNullOrEmpty(toolName))
                {
                    return new { success = false, message = "Missing 'tool' field in command JSON." };
                }

                if (string.Equals(toolName, "atm_sync_agent_files", StringComparison.OrdinalIgnoreCase))
                {
                    int failedCount;
                    int updatedCount = ATMNextAgentProfile.EnsureAllAgentFilesUpdated(out failedCount);
                    return new
                    {
                        success = failedCount == 0,
                        message = failedCount == 0 ? "Agent instructions and ATM guard files synchronized." : "Agent instruction synchronization completed with failures.",
                        updatedFiles = updatedCount,
                        failedCount
                    };
                }

                if (string.Equals(toolName, "atm_refresh", StringComparison.OrdinalIgnoreCase) || string.Equals(toolName, "refresh", StringComparison.OrdinalIgnoreCase))
                {
                    AssetDatabase.Refresh();
                    return new { success = true, message = "AssetDatabase refreshed." };
                }

                if (string.Equals(toolName, "atm_help", StringComparison.OrdinalIgnoreCase))
                    return GetToolHelp(parameters);

                return ExecuteTool(toolName, parameters);
            }
            catch (Exception e)
            {
                return new { success = false, message = $"JSON parse error: {e.Message}" };
            }
        }

        private static object ExecuteTool(string toolName, JToken parameters)
        {
            var methods = GetToolMethods();

            string normalizedToolName = toolName.StartsWith("atm_", StringComparison.OrdinalIgnoreCase)
                ? toolName.Substring(4)
                : toolName;

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
                            return InvokeMethod(method, parameters);
                        }
                    }
                }

                // 2. Try matching by method name directly (with or without atm_ prefix)
                if (string.Equals(method.Name, toolName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(method.Name, normalizedToolName, StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeMethod(method, parameters);
                }
                
                // 3. Try matching by snake_case conversion
                string snake = ToSnakeCase(method.Name);
                if (string.Equals("atm_" + snake, toolName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(snake, toolName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(snake, normalizedToolName, StringComparison.OrdinalIgnoreCase))
                {
                    return InvokeMethod(method, parameters);
                }
            }

            return new
            {
                success = false,
                message = $"Unknown ATM tool '{toolName}'. Use atm_help to list available tools.",
                suggestions = GetClosestToolNames(toolName)
            };
        }

        private static object GetToolHelp(JToken parameters)
        {
            string requestedTool = parameters is JObject parameterObject ? parameterObject["Tool"]?.ToString() : null;
            var methods = GetToolMethods();

            if (!string.IsNullOrWhiteSpace(requestedTool))
            {
                string normalizedName = requestedTool.StartsWith("atm_", StringComparison.OrdinalIgnoreCase)
                    ? requestedTool
                    : "atm_" + requestedTool;
                if (string.Equals(normalizedName, "atm_refresh", StringComparison.OrdinalIgnoreCase))
                    return new { success = true, tool = "atm_refresh", parameters = Array.Empty<object>() };
                if (string.Equals(normalizedName, "atm_sync_agent_files", StringComparison.OrdinalIgnoreCase))
                    return new { success = true, tool = "atm_sync_agent_files", parameters = Array.Empty<object>() };
                var method = methods.FirstOrDefault(candidate =>
                    string.Equals(GetToolName(candidate), normalizedName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(candidate.Name, requestedTool, StringComparison.OrdinalIgnoreCase));

                if (method == null)
                {
                    return new
                    {
                        success = false,
                        message = $"Unknown ATM tool '{requestedTool}'. Use atm_help to list available tools.",
                        suggestions = GetClosestToolNames(requestedTool)
                    };
                }

                return new
                {
                    success = true,
                    tool = GetToolName(method),
                    parameters = GetParameterHelp(method)
                };
            }

            return new
            {
                success = true,
                message = "Available ATM tools and bridge utilities. Pass {\"Tool\":\"atm_tool_name\"} to inspect one tool's parameters.",
                tools = methods.Select(GetToolName)
                    .Concat(new[] { "atm_help", "atm_refresh", "atm_sync_agent_files" })
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToArray()
            };
        }

        private static MethodInfo[] GetToolMethods()
        {
            return typeof(ATMNextMcpTools).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.ReturnType == typeof(object) && method.GetParameters().Length <= 1)
                .OrderBy(GetToolName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string GetToolName(MethodInfo method)
        {
            foreach (object attribute in method.GetCustomAttributes(false))
            {
                Type attributeType = attribute.GetType();
                if (attributeType.Name != "McpToolAttribute") continue;
                string registeredName = attributeType.GetProperty("Name")?.GetValue(attribute) as string;
                if (!string.IsNullOrEmpty(registeredName)) return registeredName;
            }

            return "atm_" + ToSnakeCase(method.Name);
        }

        private static object[] GetParameterHelp(MethodInfo method)
        {
            ParameterInfo[] methodParameters = method.GetParameters();
            if (methodParameters.Length == 0) return Array.Empty<object>();

            Type parameterType = methodParameters[0].ParameterType;
            object defaults = Activator.CreateInstance(parameterType);
            return parameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead)
                .Select(property => (object)new
                {
                    name = property.Name,
                    type = GetFriendlyTypeName(property.PropertyType),
                    defaultValue = defaults != null ? property.GetValue(defaults, null) : null
                })
                .ToArray();
        }

        private static string GetFriendlyTypeName(Type type)
        {
            Type nullableType = Nullable.GetUnderlyingType(type);
            if (nullableType != null) return GetFriendlyTypeName(nullableType) + "?";
            if (type.IsArray) return GetFriendlyTypeName(type.GetElementType()) + "[]";
            if (type.IsGenericType) return GetFriendlyTypeName(type.GetGenericArguments()[0]) + "[]";
            return type.Name;
        }

        private static string[] GetClosestToolNames(string toolName)
        {
            string normalizedToolName = (toolName ?? "").StartsWith("atm_", StringComparison.OrdinalIgnoreCase)
                ? (toolName ?? "").ToLowerInvariant()
                : "atm_" + (toolName ?? "").ToLowerInvariant();
            return GetToolMethods()
                .Select(GetToolName)
                .OrderBy(candidate => EditDistance(normalizedToolName, candidate.ToLowerInvariant()))
                .ThenBy(candidate => candidate, StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToArray();
        }

        private static int EditDistance(string left, string right)
        {
            int[,] distance = new int[left.Length + 1, right.Length + 1];
            for (int i = 0; i <= left.Length; i++) distance[i, 0] = i;
            for (int j = 0; j <= right.Length; j++) distance[0, j] = j;

            for (int i = 1; i <= left.Length; i++)
            {
                for (int j = 1; j <= right.Length; j++)
                {
                    int cost = left[i - 1] == right[j - 1] ? 0 : 1;
                    distance[i, j] = Math.Min(
                        Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                        distance[i - 1, j - 1] + cost);
                }
            }

            return distance[left.Length, right.Length];
        }

        private static string ToSnakeCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return string.Concat(text.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }

        private static object InvokeMethod(MethodInfo method, JToken parameters)
        {
            try
            {
                var paramInfos = method.GetParameters();
                object[] invokeParams = null;

                if (paramInfos.Length > 0)
                {
                    var paramType = paramInfos[0].ParameterType;
                    var validProperties = paramType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(property => property.CanWrite)
                        .ToArray();
                    var suppliedObject = parameters as JObject;
                    if (parameters != null && suppliedObject == null)
                    {
                        return new
                        {
                            success = false,
                            message = $"Parameters for '{GetToolName(method)}' must be a JSON object.",
                            validParameters = validProperties.Select(property => property.Name).ToArray(),
                            hint = $"Pass a JSON object or use atm_help with {{\"Tool\":\"{GetToolName(method)}\"}} to inspect the accepted parameters."
                        };
                    }

                    string[] validNames = validProperties.Select(property => property.Name).ToArray();
                    string[] unknownNames = suppliedObject?.Properties()
                        .Select(property => property.Name)
                        .Where(name => !validNames.Any(validName => string.Equals(validName, name, StringComparison.OrdinalIgnoreCase)))
                        .ToArray() ?? Array.Empty<string>();
                    if (unknownNames.Length > 0)
                    {
                        return new
                        {
                            success = false,
                            message = $"Unknown parameter(s) for '{GetToolName(method)}'.",
                            unknownParameters = unknownNames,
                            validParameters = validNames,
                            hint = $"Correct the parameter name(s) and retry. Use atm_help with {{\"Tool\":\"{GetToolName(method)}\"}} for parameter types and defaults."
                        };
                    }

                    // Use Newtonsoft to deserialize the parameters into the specific Params object type
                    object paramInstance = suppliedObject != null ? suppliedObject.ToObject(paramType) : Activator.CreateInstance(paramType);
                    invokeParams = new[] { paramInstance };
                }
                else if (parameters is JObject parameterObject && parameterObject.Properties().Any())
                {
                    return new
                    {
                        success = false,
                        message = $"Tool '{GetToolName(method)}' does not accept parameters.",
                        unknownParameters = parameterObject.Properties().Select(property => property.Name).ToArray(),
                        validParameters = Array.Empty<string>(),
                        hint = "Remove the parameters or use atm_help to inspect this tool."
                    };
                }

                return method.Invoke(null, invokeParams);
            }
            catch (TargetInvocationException tie)
            {
                return new
                {
                    success = false,
                    message = $"Error calling '{GetToolName(method)}': {tie.InnerException?.Message ?? tie.Message}",
                    hint = $"Use atm_help with {{\"Tool\":\"{GetToolName(method)}\"}} to verify its parameters."
                };
            }
            catch (Exception e)
            {
                return new
                {
                    success = false,
                    message = $"Invocation error for '{GetToolName(method)}': {e.Message}",
                    hint = $"Use atm_help with {{\"Tool\":\"{GetToolName(method)}\"}} to verify its parameters."
                };
            }
        }

        private static void WriteResult(object result, string resultPath = null)
        {
            resultPath ??= _resultPath;
            try
            {
                string dir = Path.GetDirectoryName(resultPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string json = JsonConvert.SerializeObject(result, Formatting.Indented);
                string temporaryResultPath = resultPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    File.WriteAllText(temporaryResultPath, json);
                    if (File.Exists(resultPath)) File.Delete(resultPath);
                    File.Move(temporaryResultPath, resultPath);
                }
                finally
                {
                    if (File.Exists(temporaryResultPath)) File.Delete(temporaryResultPath);
                }
                Debug.Log($"[ATMNext] Command Bridge: Executed tool and saved result to {Path.GetFileName(resultPath)}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ATMNext] Failed to write result file: {e.Message}");
            }
        }
    }
}
