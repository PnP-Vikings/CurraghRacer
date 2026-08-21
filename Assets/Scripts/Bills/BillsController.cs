using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

public class BillsController : MonoBehaviour
{
    public static BillsController Instance { get; private set; }
    public float electricityBillAmount = 20f;
    public float heatingBillAmount = 15f;
    public float rentBillAmount = 50f;
    public float totalBillsAmount;
    public int daysUntilNextRecurringBill =14;

    public List <Bill> bills = new List<Bill>();
    public List <Bill> recurringPaidBills = new List<Bill>();
    public GameObject billPrefab;
    
    
    LocalizedString localizedelectricityBillName = new LocalizedString { TableReference = "BillsController", TableEntryReference = "BillsController.ElectricityBillName" };
    LocalizedString localizedheatingBillName = new LocalizedString { TableReference = "BillsController", TableEntryReference = "BillsController.HeatingBillName" };
    LocalizedString localizedrentBillName = new LocalizedString { TableReference = "BillsController", TableEntryReference = "BillsController.RentBillName" };
    LocalizedString localizedYouCantAfford = new LocalizedString { TableReference = "BillsController", TableEntryReference = "BillsController.YouCantAfford" };
    LocalizedString localizedYouHavePaid = new LocalizedString { TableReference = "BillsController", TableEntryReference = "BillsController.YouHavePaid" };
    LocalizedString localizedAutoPay = new LocalizedString { TableReference = "BillsController", TableEntryReference = "BillsController.AutoPay" };
    LocalizedString localizedBillOverdue = new LocalizedString { TableReference = "BillsController", TableEntryReference = "BillsController.BillOverdue" };
    LocalizedString localizedBilldue = new LocalizedString { TableReference = "BillsController", TableEntryReference = "BillsController.BillDue" };
    
    string rentBillName= "Rent";
    string electricityBillName = "Electricity";
    string heatingBillName = "Heating";
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    

    private void OnEnable()
    {
        if(TimeManager.Instance != null)
                TimeManager.Instance.onNewDay.AddListener(HandleNewDay);
        
        if(localizedelectricityBillName != null && !localizedelectricityBillName.IsEmpty)
        {
            electricityBillName = localizedelectricityBillName.GetLocalizedString();
        }
        if(localizedheatingBillName != null && !localizedheatingBillName.IsEmpty)
        {
            heatingBillName = localizedheatingBillName.GetLocalizedString();
        }
        if(localizedrentBillName != null && !localizedrentBillName.IsEmpty)
        {
            rentBillName = localizedrentBillName.GetLocalizedString();
        }
        
        if(localizedYouCantAfford != null && !localizedYouCantAfford.IsEmpty)
        {
            localizedYouCantAfford.Arguments = new object[] { "" };
            localizedYouCantAfford.RefreshString();
        }
        if(localizedYouHavePaid != null && !localizedYouHavePaid.IsEmpty)
        {
            localizedYouHavePaid.Arguments = new object[] { "",0 };
            localizedYouHavePaid.RefreshString();
        }
        if(localizedAutoPay != null && !localizedAutoPay.IsEmpty)
        {
            localizedAutoPay.Arguments = new object[] { "",0 };
            localizedAutoPay.RefreshString();
        }
        if (localizedBillOverdue != null && !localizedBillOverdue.IsEmpty)
        {
            localizedBillOverdue.Arguments = new object[]
            {
                "", 0
            };
            localizedBillOverdue.RefreshString();
        }
        if (localizedBilldue != null && !localizedBilldue.IsEmpty)
        {
            localizedBilldue.Arguments = new object[]
            {
                "", 0
            };
            localizedBilldue.RefreshString();
        }

    }
    void OnDisable()
    {
        if(TimeManager.Instance != null)
            TimeManager.Instance.onNewDay.RemoveListener(HandleNewDay);
    }

    public void HandleNewDay()
    {
        Debug.Log("A new day has started, updating bills.");
        
        // Update days for all bills exactly once at the start of the day
        foreach (var bill in bills)
        {
            bill.daysUntilDue -= 1;
            bill.daysTillNextBill -= 1;
        }
        foreach (var bill in recurringPaidBills)
        {
            bill.daysTillNextBill -= 1;
        }

        // Update all active bills
        // We iterate over a copy of the list to allow removing items (like during auto-pay) without errors
        foreach (var bill in new List<Bill>(bills))
        {
            if (bill.isOverdue)
            {
                if(PlayerStatsView.Instance != null)
                {
                    string billOverdueMessage = $"Your {bill.billName} bill is overdue amount due is now {bill.amountDue}";
                    if(localizedBillOverdue != null && !localizedBillOverdue.IsEmpty)
                    {
                        localizedBillOverdue.Arguments = new object[] { bill.billName, bill.amountDue };
                        localizedBillOverdue.Arguments[0] = bill.billName;
                        localizedBillOverdue.Arguments[1] = bill.amountDue;
                        localizedBillOverdue.RefreshString();
                        billOverdueMessage = localizedBillOverdue.GetLocalizedString();
                    }
                    
                    PlayerStatsView.Instance.DisplayInfo(billOverdueMessage, 3);
                }
                bill.ApplyLateFee(bill.amountDue * 0.1f); // 10% late fee
            }
            
            if(bill.daysTillNextBill <= 0)
            {
                bill.amountDue += bill.amount;
                bill.daysTillNextBill = daysUntilNextRecurringBill; // Reset for next month
                bill.daysUntilDue =    bill.daysTillNextBill-5; // Reset due day
                if(PlayerStatsView.Instance != null)
                {
                    string billDueMessage = $"Your {bill.billName} bill of amount €{bill.amount} is due again";
                    if(localizedBilldue != null && !localizedBilldue.IsEmpty)
                    {
                        localizedBilldue.Arguments = new object[] { bill.billName, bill.amount };
                        localizedBilldue.Arguments[0] = bill.billName;
                        localizedBilldue.Arguments[1] = bill.amount;
                        localizedBilldue.RefreshString();
                        billDueMessage = localizedBilldue.GetLocalizedString();
                    }
                    PlayerStatsView.Instance.DisplayInfo(billDueMessage, 3);
                }
            }

            if (bill.BillIsOverdueBy(5) && !bill.isPaid)
            {
                float cost = bill.amountDue;
                bill.ProcessAutoPay();
                ProcessBillAfterPayment(bill, true, cost);
            }
        }
        
        // Collect bills that need to be moved from recurringPaidBills to bills
        List<Bill> billsToReactivate = new List<Bill>();
        
        foreach (var paidBill in recurringPaidBills)
        {
            if(paidBill.daysTillNextBill <= 0)
            {
                billsToReactivate.Add(paidBill);
            }
        }
        
        // Now move the bills (after enumeration is complete)
        foreach (var paidBill in billsToReactivate)
        {
            paidBill.isPaid = false;
            paidBill.daysTillNextBill = daysUntilNextRecurringBill; // Reset for next month
            paidBill.daysUntilDue = paidBill.daysTillNextBill - 5; // Reset due day
            paidBill.amountDue += paidBill.amount; // Reset amount due
            
            recurringPaidBills.Remove(paidBill);
            bills.Add(paidBill);
            
            if(PlayerStatsView.Instance != null)
            {
                string billDueMessage = $"Your {paidBill.billName} bill of amount €{paidBill.amount} is due again";
                if(localizedBilldue != null && !localizedBilldue.IsEmpty)
                {
                    localizedBilldue.Arguments = new object[] { paidBill.billName, paidBill.amount };
                    localizedBilldue.Arguments[0] = paidBill.billName;
                    localizedBilldue.Arguments[1] = paidBill.amount;
                    localizedBilldue.RefreshString();
                    billDueMessage = localizedBilldue.GetLocalizedString();
                }
                PlayerStatsView.Instance.DisplayInfo(billDueMessage, 3);
            }
        }
    }
    

    public void GenerateBills()
    {
        if(SaveSystem.Instance != null && SaveSystem.Instance.IsNewGame)
        {
            RefreshBillNames();
            Debug.Log("New game detected, generating initial bills.");
            bills.Clear();
            
        
            if(GameManager.Instance != null && GameManager.Instance.IsTutorialModeActive() && !GameManager.Instance.IsTutorialModeCompleted())
            {
                CreateNewBill(rentBillName, 5, 5, 1, BillType.Rent, false);
            }
            else
            {
                CreateNewBill(electricityBillName, electricityBillAmount, electricityBillAmount, 5, BillType.Utilities, true);
                CreateNewBill(heatingBillName, heatingBillAmount, heatingBillAmount, 10, BillType.Utilities, true);
                CreateNewBill(rentBillName, rentBillAmount, rentBillAmount, 5, BillType.Rent, true);
            }
        }
        else
        {
            Debug.Log("Not a new game, skipping bill generation.");
        }
    }
    
    public void GenerateBillsAfterTutorial()
    {
        if(SaveSystem.Instance != null && SaveSystem.Instance.IsNewGame)
        {
            RefreshBillNames();
            Debug.Log("New game detected, generating initial bills after tutorial.");
            bills.Clear();
            CreateNewBill(electricityBillName, electricityBillAmount, electricityBillAmount, 5, BillType.Utilities, true);
            CreateNewBill(heatingBillName, heatingBillAmount, heatingBillAmount, 10, BillType.Utilities, true);
            CreateNewBill(rentBillName, rentBillAmount, rentBillAmount, 7, BillType.Rent, true);
        }
        else
        {
            Debug.Log("Not a new game, skipping bill generation after tutorial.");
        }
    }
    
    public void RefreshBillNames()
    {
        if(localizedelectricityBillName != null && !localizedelectricityBillName.IsEmpty)
        {
            localizedelectricityBillName.RefreshString();
            electricityBillName = localizedelectricityBillName.GetLocalizedString();
        }
        if(localizedheatingBillName != null && !localizedheatingBillName.IsEmpty)
        {
            localizedheatingBillName.RefreshString();
            heatingBillName = localizedheatingBillName.GetLocalizedString();
        }
        if(localizedrentBillName != null && !localizedrentBillName.IsEmpty)
        {
            localizedrentBillName.RefreshString();
            rentBillName = localizedrentBillName.GetLocalizedString();
        }
    }
    
    public void CreateNewBill(string billName, float amount, float amountDue, int dueDay, BillType billType, bool isRecurring)
    {
        Bill newBill = new Bill(billName, amount,amountDue, dueDay, billType, isRecurring);
        bills.Add(newBill);
    }
    
    public bool CanPayBill(Bill bill)
    {
        return PlayerManager.Instance.coins >= bill.amountDue;
    }

    public bool PayBill(Bill bill)
    {
        float cost = bill.amountDue;
        if (PlayerManager.Instance.PurchaseItem(bill.amountDue, PurchaseType.Bill))
        {
            ProcessBillAfterPayment(bill, false, cost);

            if (GameManager.Instance != null && GameManager.Instance.IsTutorialModeActive() && GameManager.Instance.IsTutorialTaskActive(TutorialTaskType.PayBillTask))
            {
                GameManager.Instance.CompleteTutorialTask(TutorialTaskType.PayBillTask);
            }
            
            Debug.Log($"Paid {bill.billName} bill of amount {bill.amount}");
            return true;
        }
        else
        {
            if(PlayerStatsView.Instance != null)
            {
                PlayerStatsView.Instance.ClearInfo(); 
                string cannotAffordMessage = $"You cannot Afford to pay your {bill.billName} bill";
                if(localizedYouCantAfford != null && !localizedYouCantAfford.IsEmpty)
                {
                    localizedYouCantAfford.Arguments = new object[] { bill.billName };
                    localizedYouCantAfford.Arguments[0] = bill.billName;
                    localizedYouCantAfford.RefreshString();
                    cannotAffordMessage = localizedYouCantAfford.GetLocalizedString();
                }
                PlayerStatsView.Instance.DisplayInfo(cannotAffordMessage, 3);
            }
            
            Debug.Log("Not enough coins to pay the bill!");
            return false;
        }
    }
    
    
    private void ProcessBillAfterPayment(Bill bill,bool isAutoPay = false,float cost = 0)
    {
        if(!bill.isRecurring)
        {
            bill.isPaid = true;
            bill.amountDue = 0; // Reset amount due
            bills.Remove(bill);
        }
        else
        {
            bill.isPaid = true;
            bill.amountDue = 0; // Reset amount due
            recurringPaidBills.Add(bill);
            bills.Remove(bill);
        }
        
        if (PlayerStatsView.Instance == null) return;
        
        if (!isAutoPay)
        {
            PlayerStatsView.Instance.ClearInfo(); 
            string paidBillMessage = $"You have paid your {bill.billName} bill of amount €{cost}";
            if(localizedYouHavePaid != null && !localizedYouHavePaid.IsEmpty)
            {
                localizedYouHavePaid.Arguments = new object[] { bill.billName, cost };
                localizedYouHavePaid.Arguments[0] = bill.billName;
                localizedYouHavePaid.Arguments[1] = cost;
                localizedYouHavePaid.RefreshString();
                paidBillMessage = localizedYouHavePaid.GetLocalizedString();
            }
            PlayerStatsView.Instance.DisplayInfo(paidBillMessage, 3);
        }
        else
        {
            string autoPaidBillMessage = $"Your {bill.billName} bill has been auto-paid due to being overdue for 10 days\n You were Charged {cost}";
            if(localizedAutoPay != null && !localizedAutoPay.IsEmpty)
            {
                localizedYouHavePaid.Arguments = new object[] { bill.billName, cost };
                localizedYouHavePaid.Arguments[0] = bill.billName;
                localizedYouHavePaid.Arguments[1] = cost;
                localizedAutoPay.RefreshString();
                autoPaidBillMessage = localizedAutoPay.GetLocalizedString();
            }
            PlayerStatsView.Instance.DisplayInfo(autoPaidBillMessage, 3);
        }
    }
    
   
  
    
   
    
}
