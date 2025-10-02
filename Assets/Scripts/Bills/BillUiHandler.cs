using UnityEngine;

public class BillUiHandler : MonoBehaviour
{
    public GameObject billUiPrefab;
    public Transform billsUiParent;
    void OnEnable()
    {
        if(billUiPrefab == null)
        {
            BillsController.Instance.billPrefab = billUiPrefab;
        }
        if (billsUiParent!=null)
        {
            foreach(Bill bill in BillsController.Instance.bills)
            {
                GameObject billUi = Instantiate(billUiPrefab, billsUiParent);
                BillUi billUiComponent = billUi.GetComponent<BillUi>();
                if (billUiComponent != null)
                {
                    billUiComponent.SetBillUi(bill);
                }
            }
        }
        
    }
    
}
