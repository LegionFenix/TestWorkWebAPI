namespace DataAccess;

public class Results : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public double DeltaSeconds { get; set; }
    public DateTime MinDate { get; set; }
    public double AvgExecutionTime { get; set; }
    public double AvgValue { get; set; }
    public double MedianValue { get; set; }
    public double MaxValue { get; set; }
    public double MinValue { get; set; }

    public ICollection<Values> Values { get; set; } = new List<Values>();
}