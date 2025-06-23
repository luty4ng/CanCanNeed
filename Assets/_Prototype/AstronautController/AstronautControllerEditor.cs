using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using PlayerController.Modules;
using PlayerController.Modules.Gravity;


[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ModuleDisplayNameAttribute : Attribute
{
    public string DisplayName { get; }

    public ModuleDisplayNameAttribute(string displayName)
    {
        DisplayName = displayName;
    }
}

/// <summary>
/// AstronautController的自定义Inspector编辑器（模块开关控制）
/// </summary>
[CustomEditor(typeof(AstronautController))]
public class AstronautControllerEditor : Editor
{
    private AstronautController controller;
    private List<ModuleToggleInfo> moduleToggles;

    private class ModuleToggleInfo
    {
        public string label;
        public IAstronautModule module;
        public bool enabled;
        public FieldInfo fieldInfo;
    }

    private void OnEnable()
    {
        controller = (AstronautController)target;
        moduleToggles = new List<ModuleToggleInfo>();
    }

    public override void OnInspectorGUI()
    {
        DrawModuleToggles();
    }

    private void DrawModuleToggles()
    {
        if (controller == null)
        {
            EditorGUILayout.HelpBox("未找到AstronautController。", MessageType.Error);
            return;
        }

        UpdateModuleToggles();

        foreach (var toggle in moduleToggles)
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUI.BeginChangeCheck();
            bool newEnabled = EditorGUILayout.Toggle(toggle.enabled, GUILayout.Width(18));
            
            if (EditorGUI.EndChangeCheck() && toggle.module != null)
            {
                toggle.module.Enabled = newEnabled;
                toggle.enabled = newEnabled;
            }
            
            EditorGUILayout.LabelField(toggle.label);
            EditorGUILayout.EndHorizontal();
        }
    }

    private void UpdateModuleToggles()
    {
        moduleToggles.Clear();
        var fields = controller.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (typeof(AstronautModuleBase).IsAssignableFrom(field.FieldType))
            {
                var module = field.GetValue(controller) as IAstronautModule;
                if (module != null)
                {
                    string label = GetModuleDisplayName(field.FieldType);

                    moduleToggles.Add(new ModuleToggleInfo
                    {
                        label = label,
                        module = module,
                        enabled = module.Enabled,
                        fieldInfo = field
                    });
                }
            }
        }
    }

    private string GetModuleDisplayName(Type moduleType)
    {
        // 通过反射获取ModuleDisplayNameAttribute
        var attribute = moduleType.GetCustomAttribute<ModuleDisplayNameAttribute>();
        if (attribute != null)
        {
            return attribute.DisplayName;
        }

        // 如果没有Attribute，返回类型名称
        return moduleType.Name;
    }
}