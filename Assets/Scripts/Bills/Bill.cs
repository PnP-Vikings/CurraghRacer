using UnityEngine;

public class Bill : MonoBehaviour
{
    public string billName;
    public float amount;
    public int dueDay; // Day of the month the bill is due
    public bool isPaid = false;
    public BillType billType;
    public bool isRecurring; // e.g., "Monthly", "One-time"
    public int daysUntilDue; // Days left until the bill is due
    public int daysTillNextBill; // Days until the next bill is generated for recurring bills
    public bool isOverdue => daysUntilDue <= 0 && !isPaid;

    public Bill(string name, float amt, int due, BillType type, bool recurring)
    {
        billName = name;
        amount = amt;
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
        if (PlayerManager.Instance.coins >= amount && !isPaid)
        {
            PlayerManager.Instance.coins -= amount;
            isPaid = true;
            Debug.Log($"Bill '{billName} {billType}  paid. Amount: {amount}");
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
        amount += lateFeeAmount;
        Debug.Log($"Late fee of {lateFeeAmount} applied to bill '{billName}'. New amount: {amount}");
    }
}

public enum BillType
{
    Rent,
    Utilities,
    LoanPayment,
    Other
}
