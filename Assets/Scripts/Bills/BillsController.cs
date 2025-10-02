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
    
    public void HandleNewDay()
    {
        foreach (var bill in bills)
        {
            bill.daysUntilDue -= 1;
            bill.daysTillNextBill -= 1;
            if (bill.isOverdue)
            {
                bill.ApplyLateFee(bill.amount * 0.1f); // 10% late fee
            }
            
            if(bill.daysTillNextBill <= 0)
            {
               
            }
        }
        
        foreach (var paidBill in recurringPaidBills)
        {
            if(paidBill.daysTillNextBill <= 0)
            {
                paidBill.isPaid = false;
                paidBill.daysTillNextBill = 30; // Reset for next month
                bills.Add(paidBill);
                recurringPaidBills.Remove(paidBill);
               
            }
        }
    }

    public void GenerateBills()
    {
        bills.Clear();
        CreateNewBill("Electricity", electricityBillAmount, 5, BillType.Utilities, true);
        CreateNewBill("Heating", heatingBillAmount, 10, BillType.Utilities, true);
        CreateNewBill("Rent", rentBillAmount, 1, BillType.Rent, true);
    }
    
    public void CreateNewBill(string billName, float amount, int dueDay, BillType billType, bool isRecurring)
    {
        Bill newBill = new Bill(billName, amount, dueDay, billType, isRecurring);
        bills.Add(newBill);
    }
    
    public bool CanPayBill(Bill bill)
    {
        return PlayerManager.Instance.coins >= bill.amount;
    }
    
    public void PayBill(Bill bill)
    {
        if (PlayerManager.Instance.coins >= bill.amount)
        {
            PlayerManager.Instance.coins -= bill.amount;
            if(!bill.isRecurring)
            {
                bills.Remove(bill);
            }
            else
            {
                bill.isPaid = true;
                recurringPaidBills.Add(bill);
                bills.Remove(bill);
                
            }
            Debug.Log($"Paid {bill.billName} bill of amount {bill.amount}");
        }
        else
        {
            Debug.Log("Not enough coins to pay the bill!");
        }
    }
  
    
   
    
}
