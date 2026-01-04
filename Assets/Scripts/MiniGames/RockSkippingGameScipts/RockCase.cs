using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class RockCase : MonoBehaviour
{
   [SerializeField]Transform topCaseTransform;
   [SerializeField]Transform wholeCaseTransform;
   [SerializeField]List<Transform> rockSpawnPoints;
   public Vector3 baseTransform, baseTopRotation, baseTopTransform;
   
   Sequence openCaseSequence;
   
   public void OpenCase()
   {
         openCaseSequence = DOTween.Sequence();
         openCaseSequence.Append(topCaseTransform.DOLocalRotate(baseTopRotation +new Vector3(-70,2.82f,0),3f).SetEase(Ease.OutBack)
             ).Join(topCaseTransform.DOMove(baseTopTransform + new Vector3(0,0.2f,-.37f), 3f).SetEase(Ease.OutBack));
      
   }


   public void Start()
   {
       baseTransform = wholeCaseTransform.position;
       baseTopRotation = topCaseTransform.eulerAngles;
       baseTopTransform = topCaseTransform.position;
       
       openCaseSequence.Kill();
       DOVirtual.DelayedCall(3 ,() => OpenCase());    
        
   }
}
