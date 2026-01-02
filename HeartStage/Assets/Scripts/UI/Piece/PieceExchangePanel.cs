using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PieceExchangePanel : MonoBehaviour
{
    public static PieceExchangePanel Instance;

    public CharacterAcquirePanel characterAcquirePanel;

    [HideInInspector]
    public List<int> acquireCharacterIds = new List<int>();

    private Image background;

    private void Awake()
    {
        Instance = this;
        background = GetComponent<Image>();
    }

    // 조각 변환 가능한지 체크
    public void CheckPieceExchangeable()
    {
        var pieceIds = DataTableManager.PieceTable.GetPieceIds();
        var itemInven = SaveLoadManager.Data.itemList;
        foreach (var pieceId in pieceIds)
        {
            var pieceData = DataTableManager.PieceTable.Get(pieceId);
            // 조각이 0개면 continue
            if (!itemInven.ContainsKey(pieceId))
                continue;

            // 조각이 재료개수 이상이고, 해당 캐릭터가 없을 때
            if (itemInven[pieceId] >= pieceData.piece_ingrd_amount && !CharacterHelper.HasCharacter(pieceData.piece_result))
            {
                // 조각 사용 -> 캐릭터 획득
                if (ItemInvenHelper.TryConsumeItem(pieceId, pieceData.piece_ingrd_amount))
                {
                    CharacterHelper.AcquireCharacter(pieceData.piece_result, DataTableManager.CharacterTable);
                    acquireCharacterIds.Add(pieceData.piece_result);
                    SaveLoadManager.SaveToServer().Forget();
                }
            }
            // 4등급 변환 로직은 가챠에서 직접 처리하므로 제거됨
        }

        UpdatePanel();
    }

    // 캐릭터 획득 후 호출
    public void AfterAcquirCharacter(int characterId)
    {
        acquireCharacterIds.Remove(characterId);
        UpdatePanel();
    }

    // 패널 On/Off 업데이트
    private void UpdatePanel()
    {
        background.enabled = false;

        if (acquireCharacterIds.Count > 0)
        {
            characterAcquirePanel.gameObject.SetActive(true);
            characterAcquirePanel.Open(acquireCharacterIds[0]);
            background.enabled = true;
            // 다른 UI 위에 표시되도록 맨 앞으로 이동
            transform.SetAsLastSibling();
        }
    }
}