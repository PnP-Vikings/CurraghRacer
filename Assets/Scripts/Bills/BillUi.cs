using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class BillUi : MonoBehaviour
{
    Bill billData;
    public TMPro.TMP_Text billName,billAmountText, daysTillDueText;
    public Button payBillButton;
    public LocalizedString _localizedDayTillDueText =new LocalizedString { TableReference = "GarageScene", TableEntryReference = "Garage.Bills.DaysTillDue" };
    public LocalizedString _localizedBillAmountText =new LocalizedString { TableReference = "GarageScene", TableEntryReference = "Garage.Bills.BillAmount" };
    
    
    
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
        
        if(!_localizedBillAmountText.IsEmpty)
        {
            _localizedBillAmountText.Arguments = new object[] { amountDue.ToString("F2") };
            _localizedBillAmountText.RefreshString();
            
            
            billAmountText.text = _localizedBillAmountText.GetLocalizedString();
        }
        else
        {
            billAmountText.text = "Amount: €" + amountDue.ToString("F2");
        }
        if(!_localizedDayTillDueText.IsEmpty)
        {
            _localizedDayTillDueText.Arguments = new object[] { daysTillDue.ToString() };
            _localizedDayTillDueText.RefreshString();
            daysTillDueText.text = _localizedDayTillDueText.GetLocalizedString();
        }
        else
        {
            daysTillDueText.text = "Days Till Due: " + daysTillDue.ToString();
        }
        
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

        if (BillsController.Instance.PayBill(billData))
        {
            Destroy(gameObject);

            PayBillsAudio();
        }      
    }

    private void PayBillsAudio()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.payBill.start();

            if (PlayerManager.Instance != null)
            {
                float coins = PlayerManager.Instance.GetPlayerCoins();
                if (coins < 50)
                {
                    AudioManager.instance.payBillWhilePoorDialogue.start();
                }
            }
        }
    }
}
