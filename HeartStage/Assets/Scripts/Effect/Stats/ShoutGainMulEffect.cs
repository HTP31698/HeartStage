using UnityEngine;

public class ShoutGainMulEffect : EffectBase, IStatMulSource
{
    private const int EffectId = 3008; // 함성 증감

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RegisterSelf()
    {
        EffectRegistry.Register(
            EffectId,
            (target, value, duration, tick) =>
                Add<ShoutGainMulEffect>(target, duration, value, tick)
        );
    }

    protected override void OnApply()
    {
    }

    protected override void OnRemove()
    {
    }

    public bool TryGetMul(StatType stat, out float mul)
    {
        if (stat == StatType.ShoutGainRate)
        {
            float factor = magnitude; // magnitude 그대로 쓰는 공식
            factor = Mathf.Max(0f, factor);
            mul = factor;
            return true;
        }

        mul = 1f;
        return false;
    }
}
