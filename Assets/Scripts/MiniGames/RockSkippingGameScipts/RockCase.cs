using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
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

    private bool rockSelectSoundsAreMuted = false;
    public void OpenCase()
    {
        openCaseSequence = DOTween.Sequence();
        openCaseSequence.Append(wholeCaseTransform.DOMove(wholeCaseTransform.position + new Vector3(0, 1.2f, 0), 0.3f))
            .AppendInterval(0.5f) // Wait 0.5 seconds
            .Append(topCaseTransform.DOLocalRotate(new Vector3(-68.76f, 0f, 0), 3f).SetEase(Ease.OutBack))
            .Join(topCaseTransform.DOMove(topCaseTransform.position + new Vector3(0, 1.70f, -.10f), 3f, false)
                .SetEase(Ease.OutBack));
    }

    private void Update()
    {
        if (openCaseSequence != null && openCaseSequence.IsActive() && rockSelectSoundsAreMuted == false)
        {

            StartCoroutine(MuteRockSelectSounds());

            rockSelectSoundsAreMuted = true;

            //Debug.Log("Mute rock Select Sounds Coroutine is active - AudioDebug");
            //Debug.Log("rockSelectSoundsAreMuted is true - AudioDebug");
        }
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

       //StartCoroutine(MuteRockSelect());
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
               if (AudioManager.instance != null)
               {
                   AudioManager.instance.closeRockCase.start();
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

    IEnumerator MuteRockSelectSounds()
    {
        if (AudioManager.instance != null)
        {
            //Debug.Log("Muting UIClick1 & rockSelect for 900ms because Rock Case is opening - AudioDebug");

            AudioManager.instance.rockSelect.setParameterByName("Rock Select Volume", 0f);
            AudioManager.instance.UIClick1.setParameterByName("UI Click 1 Volume", 0f);
            yield return new WaitForSeconds(0.10f);
            AudioManager.instance.rockSelect.setParameterByName("Rock Select Volume", 0f);
            AudioManager.instance.UIClick1.setParameterByName("UI Click 1 Volume", 0f);
            yield return new WaitForSeconds(0.10f);
            AudioManager.instance.rockSelect.setParameterByName("Rock Select Volume", 0f);
            AudioManager.instance.UIClick1.setParameterByName("UI Click 1 Volume", 0f);
            yield return new WaitForSeconds(0.10f);
            AudioManager.instance.rockSelect.setParameterByName("Rock Select Volume", 0f);
            AudioManager.instance.UIClick1.setParameterByName("UI Click 1 Volume", 0f);
            yield return new WaitForSeconds(0.10f);
            AudioManager.instance.rockSelect.setParameterByName("Rock Select Volume", 0f);
            AudioManager.instance.UIClick1.setParameterByName("UI Click 1 Volume", 0f);
            yield return new WaitForSeconds(0.10f);
            AudioManager.instance.rockSelect.setParameterByName("Rock Select Volume", 0f);
            AudioManager.instance.UIClick1.setParameterByName("UI Click 1 Volume", 0f);
            yield return new WaitForSeconds(0.10f);  
            AudioManager.instance.rockSelect.setParameterByName("Rock Select Volume", 0f);
            AudioManager.instance.UIClick1.setParameterByName("UI Click 1 Volume", 0f);
            yield return new WaitForSeconds(0.10f);
            AudioManager.instance.rockSelect.setParameterByName("Rock Select Volume", 0f);
            AudioManager.instance.UIClick1.setParameterByName("UI Click 1 Volume", 0f);
            yield return new WaitForSeconds(0.10f);  
            AudioManager.instance.rockSelect.setParameterByName("Rock Select Volume", 0f);
            AudioManager.instance.UIClick1.setParameterByName("UI Click 1 Volume", 0f);
            yield return new WaitForSeconds(0.10f);   

            AudioManager.instance.rockSelect.setParameterByName("Rock Select Volume", 1f);
            AudioManager.instance.UIClick1.setParameterByName("UI Click 1 Volume", 1f);
            //Debug.Log("Unmuting UIClick1 & rockSelect because Rock Case is open - AudioDebug");

            yield return new WaitForSeconds(2.9f);

            rockSelectSoundsAreMuted = false;

            //Debug.Log("rockSelectSoundIsMuted is false - AudioDebug");
        }
    }
}
