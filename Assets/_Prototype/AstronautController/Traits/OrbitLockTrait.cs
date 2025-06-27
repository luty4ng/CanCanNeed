using UnityEngine;

[CreateAssetMenu(menuName = "Traits/OrbitLockTrait")]
public class OrbitLockTrait : TraitBase
{
    public override void OnAttach(GameObject target)
    {
        // 锁轨特性附加逻辑
    }

    public override void OnDetach(GameObject target)
    {
        // 锁轨特性移除逻辑
    }

    public override void Activate(GameObject user)
    {
        // 主动技能：进入指定星球的标准圆周轨道
    }
} 