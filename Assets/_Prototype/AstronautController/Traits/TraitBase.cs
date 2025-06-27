using UnityEngine;

public abstract class TraitBase : ScriptableObject
{
    public string traitName;
    public string description;

    // 运行时附加到物体时调用
    public virtual void OnAttach(GameObject target) { }

    // 运行时移除时调用
    public virtual void OnDetach(GameObject target) { }

    // 主动技能（如玩家主动触发）
    public abstract void Activate(GameObject user);
} 