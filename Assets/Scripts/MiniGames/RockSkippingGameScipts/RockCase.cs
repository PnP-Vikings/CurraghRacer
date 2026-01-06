using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class RockCase : MonoBehaviour
{
    [SerializeField] Transform topCaseTransform;
    [SerializeField] Transform wholeCaseTransform;
    [SerializeField] List<Transform> rockSpawnPoints;
    public Vector3 wholecaseBaseTransform, baseTopRotation, baseTopTransform;

    [SerializeField] List<RockVisual> rocksInCase;

    public event Action OnCaseClosed;
    
    Sequence openCaseSequence;

    Sequence closeCaseSequence;
    public void OpenCase()
    {
        openCaseSequence = DOTween.Sequence();
        openCaseSequence.Append(wholeCaseTransform.DOMove(wholeCaseTransform.position + new Vector3(0, 1.2f, 0), 0.3f))
            .AppendInterval(0.5f) // Wait 0.5 seconds
            .Append(topCaseTransform.DOLocalRotate(new Vector3(-68.76f, 0f, 0), 3f).SetEase(Ease.OutBack))
            .Join(topCaseTransform.DOMove(topCaseTransform.position + new Vector3(0, 1.70f, -.10f), 3f, false)
                .SetEase(Ease.OutBack));
    }


    public void Start()
    {
       wholecaseBaseTransform = wholeCaseTransform.position;
       baseTopRotation = topCaseTransform.eulerAngles;
       baseTopTransform = topCaseTransform.position;
       
       openCaseSequence.Kill();
       DOVirtual.DelayedCall(0 ,() => OpenCase());    
        
    }
   
   public void SpawnRocksInCase(List<RockVisual> rocksToSpawn)
   {
       for (int i = 0; i < rocksToSpawn.Count; i++)
       {
           RockVisual rockVisual = Instantiate(rocksToSpawn[i], rockSpawnPoints[i].position, rocksToSpawn[i].transform.rotation, rockSpawnPoints[i]);
           
           // Setup the rock visual AFTER instantiation (can't access materials on prefabs)
           rockVisual.SetupAfterInstantiation();
           
           rocksInCase.Add(rockVisual);
       }
   }
   
   public void ResetCase()
   {
       openCaseSequence.Kill();
       closeCaseSequence.Kill();
         
       closeCaseSequence = DOTween.Sequence();
       
       closeCaseSequence .Append(topCaseTransform.DOLocalRotate(new Vector3(0f, 0f, 0), 3f).SetEase(Ease.InBack))
           .Join(topCaseTransform.DOMove(topCaseTransform.position + new Vector3(0, -.47f, .13f), 3f).SetEase(Ease.InBack))
           .AppendInterval(0.2f)
           .Append(wholeCaseTransform.DOMove(wholecaseBaseTransform, 3f)).AppendCallback( () =>
           {
               foreach (var rock in rocksInCase)
               {
                   Destroy(rock.gameObject);
               }
               rocksInCase.Clear(); 
               
               OnCaseClosed.Invoke();
               
               /*if(RockSkippingGameController.Instance != null)
               {
                   RockSkippingGameController.Instance.StartAimingStage();
               }*/
           }) ;
       
   }
   
   public List<RockVisual> GetSpawnedRocks()
   {
       return rocksInCase;
   }
}
