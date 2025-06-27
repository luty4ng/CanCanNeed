using UnityEngine;

[CreateAssetMenu(menuName = "Traits/GravityOnlyTrait")]
public class GravityOnlyTrait : TraitBase
{
    public override void OnAttach(GameObject target)
    {
        // 只受某天体引力影响的逻辑
    }

    public override void OnDetach(GameObject target)
    {
        // 取消引力影响逻辑
    }

    public override void Activate(GameObject user)
    {
        // 主动技能逻辑（如暂时屏蔽其他引力源）
    }
} 