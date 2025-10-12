using UnityEngine;

[System.Serializable]
public class Bill
{
    public string billName;
    [Tooltip("Amount for the Bill if Recurring, or Total Amount if One-time")]
    public float amount;
    public float amountDue;
    public int dueDay; // Day of the month the bill is due
    public bool isPaid = false;
    public BillType billType;
    public bool isRecurring; // e.g., "Monthly", "One-time"
    public int daysUntilDue; // Days left until the bill is due
    public int daysTillNextBill; // Days until the next bill is generated for recurring bills
    public bool isOverdue => daysUntilDue <= 0 && !isPaid;

    public Bill(string name, float amt,float amtDue, int due, BillType type, bool recurring)
    {
        billName = name;
        amount = amt;
        amountDue = amtDue;
        dueDay = due;
        billType = type;
        isRecurring = recurring;
        isPaid = false;
        daysUntilDue = dueDay; // Initialize days until due
        daysTillNextBill = isRecurring ? 30 : 0; // Example: monthly bills recur every 30 days
    }
    
    /// <summary>
    /// Pay the bill if the player has enough coins.
    /// </summary>
    /// <returns>True if the bill was paid, false otherwise.</returns>
    public bool PayBill()
    {
        if (PlayerManager.Instance.coins >= amountDue && !isPaid)
        {
            PlayerManager.Instance.coins -= amountDue;
            isPaid = true;
            Debug.Log($"Bill '{billName} {billType}  paid. Amount: {amountDue}");
            return true;
        }
        Debug.Log($"Not enough coins to pay bill '{billName}' or it is already paid.");
        return false;
    }

    /// <summary>
    /// Reset the bill status for a new billing cycle.
    /// </summary>
    public void ResetBill()
    {
        isPaid = false;
        Debug.Log($"Bill '{billName}' has been reset for the new billing cycle.");
    }
    
    public void ApplyLateFee(float lateFeeAmount)
    {
        amountDue += lateFeeAmount;
        Debug.Log($"Late fee of {lateFeeAmount} applied to bill '{billName}'. New amount: {amountDue}");
    }
}

public enum BillType
{
    Rent,
    Utilities,
    LoanPayment,
    Other
}
