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
       
       // Kill sequence only if it exists and is active
       if (openCaseSequence != null && openCaseSequence.IsActive())
       {
           openCaseSequence.Kill();
       }
       
       DOVirtual.DelayedCall(0 ,() => OpenCase());    
    }
   
   public void SpawnRocksInCase(List<RockVisual> rocksToSpawn)
   {
       for (int i = 0; i < rocksToSpawn.Count; i++)
       {
           RockVisual rockVisual = Instantiate(rocksToSpawn[i], rockSpawnPoints[i].position, rocksToSpawn[i].transform.rotation, rockSpawnPoints[i]);
           rockVisual.gameObject.SetActive(true);
           // Setup the rock visual AFTER instantiation (can't access materials on prefabs)
           rockVisual.SetupAfterInstantiation();
           
           rocksInCase.Add(rockVisual);
       }
   }
   
   public void ResetCase()
   {
       // Kill sequences only if they exist and are active
       if (openCaseSequence != null && openCaseSequence.IsActive())
       {
           openCaseSequence.Kill();
       }
       
       if (closeCaseSequence != null && closeCaseSequence.IsActive())
       {
           closeCaseSequence.Kill();
       }
         
       closeCaseSequence = DOTween.Sequence();
       
       closeCaseSequence.Append(topCaseTransform.DOLocalRotate(new Vector3(0f, 0f, 0), 3f).SetEase(Ease.InBack))
           .Join(topCaseTransform.DOMove(topCaseTransform.position + new Vector3(0, -.47f, .13f), 3f).SetEase(Ease.InBack).OnComplete( () =>
           {
               if (AudioManager.instance != null )
               {
                   AudioManager.instance.punchBagAudio.start();
               }
           }))
           .AppendInterval(0.2f)
           .Append(wholeCaseTransform.DOMove(wholecaseBaseTransform, 3f)).AppendCallback( () =>
           {
               foreach (var rock in rocksInCase)
               {
                   Destroy(rock.gameObject);
               }
               rocksInCase.Clear(); 
               
               OnCaseClosed?.Invoke();
               
               /*if(RockSkippingGameController.Instance != null)
               {
                   RockSkippingGameController.Instance.StartAimingStage();
               }*/
           });
   }
   
   public void SetAllRocksToNotSelected()
   {
       foreach (var rock in rocksInCase)
       {
           rock.ResetVisuals();
       }
   }
   
   public List<RockVisual> GetSpawnedRocks()
   {
       return rocksInCase;
   }
   
   private void OnDestroy()
   {
       // Clean up sequences
       if (openCaseSequence != null && openCaseSequence.IsActive())
       {
           openCaseSequence.Kill();
       }
       
       if (closeCaseSequence != null && closeCaseSequence.IsActive())
       {
           closeCaseSequence.Kill();
       }
       
       // Kill any tweens on these transforms
       if (topCaseTransform != null)
       {
           topCaseTransform.DOKill();
       }
       
       if (wholeCaseTransform != null)
       {
           wholeCaseTransform.DOKill();
       }
   }
}
