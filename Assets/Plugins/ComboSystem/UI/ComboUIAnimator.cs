using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ComboUIAnimator : MonoBehaviour
{
    public RectTransform comboUI;
    public RectTransform comboNumberUI;

    public void ShowComboUI()
    {
        //comboUI.DOAnchorPosX(-750,1,true);
        comboUI.DOScale(1.0f, 1);
        Debug.Log("Show Combo UI");
    }

    public void HideComboUI()
    {
        //comboUI.DOAnchorPosX(-1170, 1, true);
        comboUI.DOScale(0.0f, 1);
        Debug.Log("Hide Combo UI");
    }

    public void ShakeComboUI()
    {
        //comboUI.DOAnchorPosX(-1170, 1, true);
        comboNumberUI.DOShakePosition(2, 4);
    }

    public void HeartbeatComboUI()
    {
        Sequence heartbeatSequence = DOTween.Sequence();

        heartbeatSequence.Append(comboNumberUI.DOScale(1.1f, 0.3f))
          .Append(comboNumberUI.DOScale(1f, 0.3f))
          .PrependInterval(1);
    }
}
