namespace DataAccess;

public class Values: BaseEntity
{
    public DateTime Date { get; set; }
    public long ExecutionTime { get; set; }
    public float Value { get; set; }
    
    public Guid ResultsId { get; set; }
    public Results? Results { get; set; }
}