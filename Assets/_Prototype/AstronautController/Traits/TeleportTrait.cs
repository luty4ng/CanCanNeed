using UnityEngine;

[CreateAssetMenu(menuName = "Traits/TeleportTrait")]
public class TeleportTrait : TraitBase
{
    public override void OnAttach(GameObject target)
    {
        // 跃迁特性附加逻辑
    }

    public override void OnDetach(GameObject target)
    {
        // 跃迁特性移除逻辑
    }

    public override void Activate(GameObject user)
    {
        // 主动技能：瞬间跃迁到目标星球大气表面
    }
} 