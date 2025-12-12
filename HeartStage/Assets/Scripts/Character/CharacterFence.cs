using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;

public class CharacterFence : MonoBehaviour, IDamageable
{
    public static CharacterFence Instance;
    public static List<CharacterFence> allFences = new List<CharacterFence>(); // 모든 펜스 관리 중앙 팬스 추가 떄문에 사용

    public TextMeshProUGUI currentHPText;
    public Transform imageGo;

    private static int maxHp = 0;
    private static int hp = 0;

    // 흔들기 효과
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 10f;
    private CancellationTokenSource shakeCts;
    private Vector3 originalPos;
    private bool isShaking = false;

    private void Awake()
    {
        Instance = this;

        if(!allFences.Contains(this))
        {
            allFences.Add(this);
        }
    }

    private void OnDestroy()
    {
        shakeCts?.Cancel();
        shakeCts?.Dispose();
        shakeCts = null;

        allFences.Remove(this);
        if (Instance == this)
        {
            Instance = allFences.Count > 0 ? allFences[0] : null;
        }
    }

    public void Init()
    {
        int prevMaxHp = maxHp;
        int prevHp = hp;

        // 캐릭터 HP 총합 구하기
        int totalHp = 0;
        var characters = GameObject.FindGameObjectsWithTag(Tag.Tower);

        foreach (var character in characters)
        {
            var characterAttack = character.GetComponent<CharacterAttack>();
            var baseData = DataTableManager.CharacterTable.Get(characterAttack.id);

            int finalHp = Mathf.FloorToInt(
                StatCalc.GetFinalStat(character, StatType.MaxHp, baseData.char_hp)
            );

            totalHp += finalHp;
        }

        maxHp = totalHp;

        //  HP 증가량만큼 현재 체력 증가시키기
        int delta = maxHp - prevMaxHp;

        hp = prevHp + delta;

        if (hp > maxHp)
            hp = maxHp;

        UpdateAllFencesHpText();
    }

    public static void ResetStaticHP()
    {
        maxHp = 0;
        hp = 0;
    }

    public void SetHpText()
    {
        currentHPText.text = $"HP: {hp} / {maxHp}";
    }

    private static void UpdateAllFencesHpText()
    {
        foreach (var fence in allFences)
        {
            if (fence != null)
            {
                fence.SetHpText();
            }
        }
    }

    public void Heal(int amount)
    {
        hp += amount;

        if (hp > maxHp)
            hp = maxHp;

        UpdateAllFencesHpText();
    }

    public void OnDamage(int damage, bool isCritical = false)
    {
        hp -= damage;
        StartShake();

        if (hp <= 0)
        {
            hp = 0;
            Die();            
        }
        UpdateAllFencesHpText();
    }

    public void Die()
    {
        StageManager.Instance.Defeat();
    }

    private void StartShake()
    {
        shakeCts?.Cancel();
        shakeCts = new CancellationTokenSource();
        ShakeAsync(shakeCts.Token).Forget();
    }

    // 흔들기
    private async UniTask ShakeAsync(CancellationToken token)
    {
        if (imageGo == null)
            return;

        if (!isShaking)
        {
            if (imageGo == null) 
                return;

            originalPos = imageGo.localPosition;
            isShaking = true;
        }

        float timer = 0f;

        try
        {
            while (timer < shakeDuration)
            {
                if (token.IsCancellationRequested || imageGo == null)
                    return;

                float y = Mathf.Sin(timer * 50f) * shakeMagnitude;
                imageGo.localPosition = new Vector3(originalPos.x, originalPos.y + y, originalPos.z);

                timer += Time.deltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        finally
        {
            if (!token.IsCancellationRequested && imageGo != null)
            {
                imageGo.localPosition = originalPos;
                isShaking = false;
            }
        }
    }
#if UNITY_EDITOR
    // 테스트 코드
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.D))
        {
            Die();
        }
    }
#endif
}