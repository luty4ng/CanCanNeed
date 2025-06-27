using UnityEngine;
using System.Collections.Generic;

public class TraitHolder : MonoBehaviour
{
    public List<TraitBase> traits = new();

    // 运行时添加特性
    public void AddTrait(TraitBase trait)
    {
        if (!traits.Contains(trait))
        {
            traits.Add(trait);
            trait.OnAttach(gameObject);
        }
    }

    // 运行时移除特性
    public void RemoveTrait(TraitBase trait)
    {
        if (traits.Contains(trait))
        {
            trait.OnDetach(gameObject);
            traits.Remove(trait);
        }
    }

    // 主动触发所有特性技能
    public void ActivateAllTraits()
    {
        foreach (var trait in traits)
        {
            trait.Activate(gameObject);
        }
    }

    // 主动触发指定特性技能
    public void ActivateTrait(TraitBase trait)
    {
        if (traits.Contains(trait))
        {
            trait.Activate(gameObject);
        }
    }
} 