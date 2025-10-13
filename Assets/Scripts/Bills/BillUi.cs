using UnityEngine;
using UnityEngine.UI;

public class BillUi : MonoBehaviour
{
    Bill billData;
    public TMPro.TMP_Text billName,billAmountText, daysTillDueText;
    public Button payBillButton;

    FMOD.Studio.EventInstance PayBill;

    public void SetBillUi(Bill bill)
    {
        if (bill == null)
        {
            Debug.LogError("Bill data is null!");
            return;
        }
        billData = bill;
        float amountDue = bill.amountDue;
        int daysTillDue = bill.daysUntilDue;
        
        
        billName.text = bill.billName;
        billAmountText.text = "Amount: €" + amountDue.ToString("F2");
        daysTillDueText.text = "Days Till Due: " + daysTillDue.ToString();
        
        if (daysTillDue <= 0)
        {
            daysTillDueText.color = Color.red;
        }
        else if (daysTillDue <= 3)
        {
            daysTillDueText.color = new Color(1f, 0.65f, 0f); // Orange color
        }
        else
        {
            daysTillDueText.color = Color.green;
        }
        
        payBillButton.onClick.RemoveAllListeners();
        payBillButton.onClick.AddListener(OnPayBillButtonClicked);
        
        if (BillsController.Instance.CanPayBill(bill))
        {
            payBillButton.GetComponent<Image>().color = Color.white;
            payBillButton.interactable = true;
        }
        else
        {
            payBillButton.GetComponent<Image>().color = new Color(0.55f, 0f, 0f); // Dark red
           // payBillButton.interactable = false;
        }
        
    }
    
    public void OnPayBillButtonClicked()
    {
        Debug.Log("Pay Bill Button Clicked");
        
        if(BillsController.Instance.PayBill(billData))
        {
            Destroy(gameObject);

            PayBill = FMODUnity.RuntimeManager.CreateInstance("event:/Bulletin Board/Pay Bill");
            PayBill.start();
        }
      
        
    }
    
}
