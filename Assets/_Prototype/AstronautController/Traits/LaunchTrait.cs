using UnityEngine;

[CreateAssetMenu(menuName = "Traits/LaunchTrait")]
public class LaunchTrait : TraitBase
{
    public override void OnAttach(GameObject target)
    {
        // 弹射特性附加逻辑
    }

    public override void OnDetach(GameObject target)
    {
        // 弹射特性移除逻辑
    }

    public override void Activate(GameObject user)
    {
        // 主动技能：在地表产生巨大弹力起飞，导致轨道偏移
    }
} 