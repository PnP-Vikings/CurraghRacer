using System;
using System.Collections.Generic;
using UnityEngine;

public class BillsController : MonoBehaviour
{
    public static BillsController Instance { get; private set; }
    public float electricityBillAmount = 20f;
    public float heatingBillAmount = 15f;
    public float rentBillAmount = 50f;
    public float totalBillsAmount;
    

    public List <Bill> bills = new List<Bill>();
    public List <Bill> recurringPaidBills = new List<Bill>();
    public GameObject billPrefab;
    
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

    void Start()
    {
        GenerateBills();
    }

    private void OnEnable()
    {
        if(TimeManager.Instance != null)
            TimeManager.Instance.onNewDay.AddListener(HandleNewDay);
    }
    void OnDisable()
    {
        if(TimeManager.Instance != null)
            TimeManager.Instance.onNewDay.RemoveListener(HandleNewDay);
    }

    public void HandleNewDay()
    {
        // Update all active bills
        foreach (var bill in bills)
        {
            bill.daysUntilDue -= 1;
            bill.daysTillNextBill -= 1;
            if (bill.isOverdue)
            {
                if(PlayerStatsView.Instance != null)
                {
                    PlayerStatsView.Instance.DisplayInfo($"Your {bill.billName} bill is overdue amount due is now {bill.amountDue}", 3);
                }
                bill.ApplyLateFee(bill.amountDue * 0.1f); // 10% late fee
            }
            
            if(bill.daysTillNextBill <= 0)
            {
                bill.amountDue += bill.amount;
                bill.daysTillNextBill = 30; // Reset for next month
                bill.daysUntilDue =    bill.daysTillNextBill-5; // Reset due day
                if(PlayerStatsView.Instance != null)
                {
                    PlayerStatsView.Instance.DisplayInfo($"Your {bill.billName} bill of amount €{bill.amount} is due again", 3);
                }
            }
        }
        
        // Collect bills that need to be moved from recurringPaidBills to bills
        List<Bill> billsToReactivate = new List<Bill>();
        
        foreach (var paidBill in recurringPaidBills)
        {
            paidBill.daysTillNextBill -= 1;
            if(paidBill.daysTillNextBill <= 0)
            {
                billsToReactivate.Add(paidBill);
            }
        }
        
        // Now move the bills (after enumeration is complete)
        foreach (var paidBill in billsToReactivate)
        {
            paidBill.isPaid = false;
            paidBill.daysTillNextBill = 30; // Reset for next month
            paidBill.daysUntilDue = paidBill.daysTillNextBill - 5; // Reset due day
            paidBill.amountDue += paidBill.amount; // Reset amount due
            
            recurringPaidBills.Remove(paidBill);
            bills.Add(paidBill);
            
            if(PlayerStatsView.Instance != null)
            {
                PlayerStatsView.Instance.DisplayInfo($"Your {paidBill.billName} bill of amount €{paidBill.amount} is due again", 3);
            }
        }
    }

    public void GenerateBills()
    {
        bills.Clear();
        CreateNewBill("Electricity", electricityBillAmount, electricityBillAmount,5, BillType.Utilities, true);
        CreateNewBill("Heating", heatingBillAmount, heatingBillAmount,10, BillType.Utilities, true);
        CreateNewBill("Rent", rentBillAmount, rentBillAmount,1, BillType.Rent, true);
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
        if (PlayerManager.Instance.PurchaseItem(bill.amountDue))
        {
            if(!bill.isRecurring)
            {
                bill.isPaid = true;
                if(PlayerStatsView.Instance != null)
                {
                    PlayerStatsView.Instance.ClearInfo(); 
                    PlayerStatsView.Instance.DisplayInfo($"You have paid your {bill.billName} bill of amount €{bill.amountDue}", 3);
                }
                bill.amountDue = 0; // Reset amount due
                bills.Remove(bill);
            }
            else
            {
                bill.isPaid = true;
                
                if(PlayerStatsView.Instance != null)
                {
                   PlayerStatsView.Instance.ClearInfo(); 
                   PlayerStatsView.Instance.DisplayInfo($"You have paid your {bill.billName} bill of amount €{bill.amountDue}", 3);
                }
                bill.amountDue = 0; // Reset amount due
                
                recurringPaidBills.Add(bill);
                bills.Remove(bill);
                
            }
            
            Debug.Log($"Paid {bill.billName} bill of amount {bill.amount}");
            return true;
        }
        else
        {
            if(PlayerStatsView.Instance != null)
            {
                PlayerStatsView.Instance.ClearInfo(); 
                PlayerStatsView.Instance.DisplayInfo($"You cannot Afford to pay your {bill.billName} bill", 3);
            }
            
            Debug.Log("Not enough coins to pay the bill!");
            return false;
        }
    }
  
    
   
    
}
