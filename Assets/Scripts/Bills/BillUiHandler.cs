using UnityEngine;

public class BillUiHandler : MonoBehaviour
{
    public GameObject billUiPrefab;
    public Transform billsUiParent;
    void OnEnable()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsTutorialModeActive() )
        {
            GameManager.Instance.CompleteTutorialTask(TutorialTaskType.OpenBillMenuTask);
        }
        
        if(billUiPrefab == null && BillsController.Instance != null)
        {
            billUiPrefab = BillsController.Instance.billPrefab;
        }
        if (billsUiParent!=null)
        {
            if (BillsController.Instance != null)
            {

                if (BillsController.Instance.bills == null || BillsController.Instance.bills.Count == 0)
                {
                    Debug.Log("BillUiHandler: No bills to display.");
                    return;
                }
                foreach (Bill bill in BillsController.Instance.bills)
                {
                    Debug.Log("BillUiHandler: Creating UI for bill: " + bill.billName);
                    GameObject billUi = Instantiate(billUiPrefab, billsUiParent);
                    BillUi billUiComponent = billUi.GetComponent<BillUi>();
                    if (billUiComponent != null)
                    {
                        billUiComponent.SetBillUi(bill);
                    }
                }
            }
            else
            {
                Debug.LogError("BillUiHandler: BillsController instance is null.");
                return;
            }
            
        }
        
    }
    
    void OnDisable()
    {
        ClearBillUis();
        if (GameManager.Instance != null && GameManager.Instance.IsTutorialModeActive() )
        {
            GameManager.Instance.CompleteTutorialTask(TutorialTaskType.CloseBillMenu);
        }
    }
    
    public void ClearBillUis()
    {
        foreach (Transform child in billsUiParent)
        {
            Destroy(child.gameObject);
        }
    }
    
    
}
