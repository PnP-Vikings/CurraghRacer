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
   
   
   public void OpenCase()
   {
       topCaseTransform.DOLocalRotate(new Vector3(-90,0,0),0.5f).SetEase(Ease.OutBack);
       wholeCaseTransform.DOLocalMoveY(-0.2f,0.5f).SetEase(Ease.OutBack);
   }


   public void Start()
   {
       baseTransform = wholeCaseTransform.position;
       baseTopRotation = topCaseTransform.eulerAngles;
       baseTopTransform = topCaseTransform.position;
       
       
       DOVirtual.DelayedCall(3 ,() => OpenCase());    
        
   }
}
