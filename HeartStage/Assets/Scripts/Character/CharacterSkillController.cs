using UnityEngine;

public class CharacterSkillController : MonoBehaviour
{
    [HideInInspector]
    public int skillId;

    private CharacterAttack characterAttack;

    private bool isReady = false;

    private void Start()
    {
        characterAttack = GetComponent<CharacterAttack>();
    }

    public void SkillReady()
    {
        isReady = true;
        characterAttack.animator.SetTrigger(CharacterAttack.HashSkillReady);
    }

    //private void Update()
    //{
    //    if(isReady && Input.GetKeyDown(KeyCode.Alpha1))
    //    {
    //        ActiveSkillManager.Instance.TryUseSkill(gameObject, skillId);
    //        characterAttack.animator.SetTrigger(CharacterAttack.HashIdle);
    //    }
    //}
}