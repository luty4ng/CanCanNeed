using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Audio2Face
{
    public static class AnimationCurveUtility
    {
        public static void UpdateKeyValue(AnimationCurve curve, float newTime, float newValue)
        {
            bool keyframeExists = false;
            for (int i = 0; i < curve.keys.Length; i++)
            {
                if (Mathf.Approximately(curve.keys[i].time, newTime))
                {
                    Keyframe newKey = curve.keys[i];
                    newKey.value = newValue;
                    curve.MoveKey(i, newKey);
                    keyframeExists = true;
                    break;
                }
            }

            if (!keyframeExists)
                curve.AddKey(new Keyframe(newTime, newValue));
        }

        public static AnimationCurve GetCopiedCurve(AnimationCurve sourceCurve)
        {
            if (sourceCurve == null)
                return null;

            AnimationCurve copiedCurve = new AnimationCurve();
            for (int i = 0; i < sourceCurve.keys.Length; i++)
            {
                Keyframe originalKey = sourceCurve.keys[i];

                Keyframe copiedKey = new Keyframe(
                    originalKey.time,
                    originalKey.value,
                    originalKey.inTangent,
                    originalKey.outTangent
                );

                copiedKey.weightedMode = originalKey.weightedMode;
                copiedKey.inWeight = originalKey.inWeight;
                copiedKey.outWeight = originalKey.outWeight;

                copiedCurve.AddKey(copiedKey);
            }

            // 复制曲线的预处理和后处理模式
            copiedCurve.preWrapMode = sourceCurve.preWrapMode;
            copiedCurve.postWrapMode = sourceCurve.postWrapMode;

            return copiedCurve;
        }

        public static AnimationCurve GetNormalizedTimeCurve(AnimationCurve sourceCurve, float duration)
        {
            AnimationCurve normalizedCurve = new AnimationCurve();

            for (int i = 0; i < sourceCurve.keys.Length; i++)
            {
                Keyframe originalKey = sourceCurve.keys[i];

                Keyframe normalizedKey = new Keyframe(
                    originalKey.time / duration,
                    originalKey.value,
                    originalKey.inTangent * duration,
                    originalKey.outTangent * duration
                );

                normalizedKey.weightedMode = originalKey.weightedMode;
                normalizedKey.inWeight = originalKey.inWeight;
                normalizedKey.outWeight = originalKey.outWeight;

                normalizedCurve.AddKey(normalizedKey);
            }

            return normalizedCurve;
        }

        public static AnimationCurve GetActualTimeCurve(AnimationCurve normalizedCurve, float duration)
        {
            AnimationCurve actualCurve = new AnimationCurve();

            for (int i = 0; i < normalizedCurve.keys.Length; i++)
            {
                Keyframe normalizedKey = normalizedCurve.keys[i];

                Keyframe actualKey = new Keyframe(
                    normalizedKey.time * duration,
                    normalizedKey.value,
                    normalizedKey.inTangent / duration,
                    normalizedKey.outTangent / duration
                );

                actualKey.weightedMode = normalizedKey.weightedMode;
                actualKey.inWeight = normalizedKey.inWeight;
                actualKey.outWeight = normalizedKey.outWeight;

                actualCurve.AddKey(actualKey);
            }

            return actualCurve;
        }


        // 输入为一个骨骼不同层的数值曲线和权重
        public static AnimationCurve BlendCurve(List<AnimationCurve> curvesToBlend, List<float> normalizedWeights)
        {
            AnimationCurve resultCurve = new AnimationCurve();
            for (int i = 0; i < curvesToBlend.Count; i++)
                BlendCurve(resultCurve, curvesToBlend[i], normalizedWeights[i]);
            return resultCurve;
        }

        // 输入为一个骨骼不同层的数值曲线和权重
        public static AnimationCurve BlendCurve(List<AnimationCurve> curvesToBlend, List<AnimationCurve> normalizedWeightCurves)
        {
            AnimationCurve resultCurve = new AnimationCurve();
            for (int i = 0; i < curvesToBlend.Count; i++)
                BlendCurve(resultCurve, curvesToBlend[i], normalizedWeightCurves[i]);
            return resultCurve;
        }

        public static void BlendCurve(AnimationCurve resultCurve, AnimationCurve targetCurve, AnimationCurve targetWeightCurve)
        {
            HashSet<float> timePoints = new HashSet<float>();
            foreach (var key in targetCurve.keys)
                timePoints.Add(key.time);
            foreach (var key in targetWeightCurve.keys)
                timePoints.Add(key.time);

            foreach (float time in timePoints)
            {
                float curveValue = targetCurve.Evaluate(time);
                float weight = targetWeightCurve.Evaluate(time);

                float valueBefore = targetCurve.Evaluate(time - 0.001f);
                float valueAfter = targetCurve.Evaluate(time + 0.001f);
                float inTangent = curveValue > valueBefore ? curveValue - valueBefore : valueAfter - curveValue;
                float outTangent = valueAfter > curveValue ? valueAfter - curveValue : curveValue - valueBefore;

                Keyframe weightedKey = new Keyframe(
                    time,
                    curveValue * weight,
                    inTangent * weight,
                    outTangent * weight
                );

                int existingIndex = -1;
                for (int i = 0; i < resultCurve.keys.Length; i++)
                {
                    if (Mathf.Approximately(resultCurve.keys[i].time, time))
                    {
                        existingIndex = i;
                        break;
                    }
                }

                if (existingIndex >= 0)
                {
                    Keyframe blendedKey = resultCurve.keys[existingIndex];
                    blendedKey.value += weightedKey.value;
                    blendedKey.inTangent = (blendedKey.inTangent + weightedKey.inTangent) / 2f;
                    blendedKey.outTangent = (blendedKey.outTangent + weightedKey.outTangent) / 2f;
                    resultCurve.MoveKey(existingIndex, blendedKey);
                }
                else
                {
                    resultCurve.AddKey(weightedKey);
                }
            }
        }


        public static void BlendCurve(AnimationCurve resultCurve, AnimationCurve targetCurve, float targetWeight)
        {
            if (targetWeight == 0)
                return;
            foreach (var targetKey in targetCurve.keys)
            {
                int existingIndex = -1;
                for (int i = 0; i < resultCurve.keys.Length; i++)
                {
                    if (Mathf.Approximately(resultCurve.keys[i].time, targetKey.time))
                    {
                        existingIndex = i;
                        break;
                    }
                }

                if (existingIndex >= 0)
                {
                    Keyframe blendedKey = resultCurve.keys[existingIndex];
                    blendedKey.value += targetKey.value * targetWeight;
                    blendedKey.inTangent = (blendedKey.inTangent + targetKey.inTangent * targetWeight) / 2f;
                    blendedKey.outTangent = (blendedKey.outTangent + targetKey.outTangent * targetWeight) / 2f;
                    resultCurve.MoveKey(existingIndex, blendedKey);
                }
                else
                {
                    Keyframe weightedKey = new Keyframe(
                        targetKey.time,
                        targetKey.value * targetWeight,
                        targetKey.inTangent * targetWeight,
                        targetKey.outTangent * targetWeight
                    );
                    resultCurve.AddKey(weightedKey);
                }
            }
        }

        public static void NormalizeCurvesValue(Dictionary<string, AnimationCurve> curves, out List<AnimationCurve> normalizedValueCurves)
        {
            normalizedValueCurves = new List<AnimationCurve>();
            if (curves == null || curves.Count <= 1)
                return;

            HashSet<float> allKeyTimes = new HashSet<float>();
            foreach (var curve in curves.Values)
            {
                foreach (var key in curve.keys)
                {
                    allKeyTimes.Add(key.time);
                }
            }

            List<float> keyTimesList = new List<float>(allKeyTimes);
            keyTimesList.Sort();

            foreach (float time in keyTimesList)
            {
                float totalWeight = 0f;
                Dictionary<string, float> currentValues = new Dictionary<string, float>();

                foreach (var kvp in curves)
                {
                    float value = kvp.Value.Evaluate(time);
                    currentValues[kvp.Key] = value;
                    totalWeight += value;
                }

                if (totalWeight <= 0)
                    continue;

                foreach (var kvp in curves)
                {
                    string layerName = kvp.Key;
                    AnimationCurve curve = new AnimationCurve(kvp.Value.keys);
                    float normalizedValue = currentValues[layerName] / totalWeight;

                    int keyIndex = -1;
                    for (int i = 0; i < curve.keys.Length; i++)
                    {
                        if (Mathf.Approximately(curve.keys[i].time, time))
                        {
                            keyIndex = i;
                            break;
                        }
                    }

                    if (keyIndex >= 0)
                    {
                        Keyframe key = curve.keys[keyIndex];
                        key.value = normalizedValue;
                        curve.MoveKey(keyIndex, key);
                    }

                    normalizedValueCurves.Add(curve);
                }
            }
        }
    }

    public static class AnimationClipUtility
    {
        public static string ConvertAnimationClipToJson(AnimationClip clip, string exportPath = "", float exportFps = 30.0f)
        {
            if (clip == null)
            {
                Debug.LogError("AnimationClip is null!");
                return null;
            }
            
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            List<EditorCurveBinding> facsBindings = bindings.ToList();
            List<string> facsNames = facsBindings.Select(binding => binding.path.Split("/").LastOrDefault()).ToList();
            int numPoses = bindings.Length;
            float clipLength = clip.length;
            int numFrames = Mathf.CeilToInt(clipLength * exportFps);
            List<List<float>> weightMat = new List<List<float>>();

            // 对每一帧进行采样
            for (int frameIndex = 0; frameIndex < numFrames; frameIndex++)
            {
                float time = frameIndex / exportFps;
                List<float> frameWeights = new List<float>();

                // 对每个绑定属性进行采样
                for (int poseIndex = 0; poseIndex < numPoses; poseIndex++)
                {
                    EditorCurveBinding binding = facsBindings[poseIndex];
                    AnimationCurve curve = AnimationUtility.GetEditorCurve(clip, binding);

                    if (curve != null)
                    {
                        frameWeights.Add(curve.Evaluate(time));
                    }
                    else
                    {
                        frameWeights.Add(0f);
                    }
                }

                weightMat.Add(frameWeights);
            }

            // 构建JSON对象
            var jsonObject = new Dictionary<string, object>
            {
                { "exportFps", exportFps },
                { "trackPath", exportPath },
                { "numPoses", (int)(numPoses / 3f) },
                { "numFrames", numFrames },
                { "facsNames", facsNames },
                { "weightMat", weightMat }
            };

            // 使用Newtonsoft.Json转换为JSON字符串
            string jsonString = JsonConvert.SerializeObject(jsonObject, Formatting.Indented);
            if (!string.IsNullOrEmpty(exportPath))
            {
                File.WriteAllText(exportPath, jsonString);
                Debug.Log($"Animation data exported to: {exportPath}");
            }
            return jsonString;
        }
    }

    public static class Extension
    {
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component == null)
                component = go.AddComponent<T>();
            return component;
        }
    }
}