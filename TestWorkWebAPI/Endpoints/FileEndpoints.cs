using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using DataAccess;
using static Microsoft.AspNetCore.Http.Results;

namespace TestWorkWebAPI.Endpoints;

public class ResultsFilter
{
    public string? FileName { get; set; }
    public DateTime? MinDate { get; set; }
    public DateTime? MaxDate { get; set; }
    public double? MinAvgValue { get; set; }
    public double? MaxAvgValue { get; set; }
    public double? MinAvgExecutionTime { get; set; }
    public double? MaxAvgExecutionTime { get; set; }
}

public class ResultsDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public double DeltaSeconds { get; set; }
    public DateTime MinDate { get; set; }
    public double AvgExecutionTime { get; set; }
    public double AvgValue { get; set; }
    public double MedianValue { get; set; }
    public double MaxValue { get; set; }
    public double MinValue { get; set; }
}

public class ValuesDto
{
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public long ExecutionTime { get; set; }
    public float Value { get; set; }
}
public class UploadDto
{
    public IFormFile File { get; set; } = null!;
}

public static class FileEndpoints
{
    public static async Task<IResult> FirstMethod(
        [FromForm] UploadDto dto, 
        [FromServices] DataContext context)
    {
        var file = dto.File;
        if (file == null || file.Length == 0)
            return Microsoft.AspNetCore.Http.Results.BadRequest("Файл не выбран или пуст.");

        if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            return Microsoft.AspNetCore.Http.Results.BadRequest("Допустимы только файлы с расширением .csv.");

        var lines = new List<string>();
        using (var reader = new StreamReader(file.OpenReadStream()))
        {
            string? line;
            bool isHeader = true;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (isHeader) { isHeader = false; continue; }
                if (!string.IsNullOrWhiteSpace(line))
                    lines.Add(line);
            }
        }

        if (lines.Count < 1 || lines.Count > 10000)
            return Microsoft.AspNetCore.Http.Results.BadRequest($"Количество строк ({lines.Count}) должно быть от 1 до 10000.");

        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var existing = await context.ResultsRecord
                .FirstOrDefaultAsync(r => r.FileName == file.FileName);
            if (existing != null)
            {
                context.ResultsRecord.Remove(existing);
                await context.SaveChangesAsync();
            }

            var parsedValues = new List<DataAccess.Values>();
            var dates = new List<DateTime>();
            var execTimes = new List<long>();
            var floatValues = new List<float>();

            foreach (var line in lines)
            {
                try
                {
                    var (date, execTime, value) = ParseLineFile(line);
                    parsedValues.Add(new DataAccess.Values
                    {
                        Date = date,
                        ExecutionTime = execTime,
                        Value = value
                    });
                    dates.Add(date);
                    execTimes.Add(execTime);
                    floatValues.Add(value);
                }
                catch (ValidationException ex)
                {
                    return Microsoft.AspNetCore.Http.Results.BadRequest($"Ошибка в строке: {line}\n{ex.Message}");
                }
            }

            var results = CalcResults(file.FileName, dates, execTimes, floatValues);
            results.Values = parsedValues;

            context.ResultsRecord.Add(results);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Microsoft.AspNetCore.Http.Results.Ok($"Файл '{file.FileName}' успешно обработан.");
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static (DateTime date, long execTime, float value) ParseLineFile(string line)
    {
        var parts = line.Split(';');
        if (parts.Length != 3)
            throw new ValidationException("Ожидается 3 поля, разделённых ';'.");
        
        if (!DateTime.TryParseExact(parts[0].Trim(), "yyyy-MM-ddTHH-mm-ss.ffffZ",
                CultureInfo.InvariantCulture, 
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, 
                out var date))
            throw new ValidationException($"Неверный формат даты: {parts[0]}");

        var minAllowed = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc); 
        if (date < minAllowed || date > DateTime.UtcNow)
            throw new ValidationException($"Дата {date} вне допустимого диапазона.");

        if (!long.TryParse(parts[1].Trim(), out var execTime) || execTime < 0)
            throw new ValidationException($"Неверное или отрицательное время выполнения: {parts[1]}");

        if (!float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || value < 0)
            throw new ValidationException($"Неверное или отрицательное значение: {parts[2]}");

        return (date, execTime, value);
    }

    private static DataAccess.Results CalcResults(string fileName, List<DateTime> dates, List<long> execTimes, List<float> values)
    {
        var deltaSeconds = (dates.Max() - dates.Min()).TotalSeconds;
        var avgExecTime = execTimes.Average();
        var avgValue = values.Average();

        var sorted = values.OrderBy(v => v).ToList();
        double median;
        int count = sorted.Count;
        if (count % 2 == 1)
            median = sorted[count / 2];
        else
            median = (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;

        return new DataAccess.Results
        {
            FileName = fileName,
            DeltaSeconds = deltaSeconds,
            MinDate = dates.Min(),
            AvgExecutionTime = avgExecTime,
            AvgValue = avgValue,
            MedianValue = median,
            MaxValue = values.Max(),
            MinValue = values.Min()
        };
    }
    
    public static async Task<IResult> SecondMethod(
        [AsParameters] ResultsFilter filter,
        [FromServices] DataContext context)
    {
        var query = context.ResultsRecord.AsNoTracking();
        
        if (!string.IsNullOrWhiteSpace(filter.FileName))
            query = query.Where(r => r.FileName.Contains(filter.FileName));
        
        if (filter.MinDate.HasValue)
            query = query.Where(r => r.MinDate >= filter.MinDate.Value);
        if (filter.MaxDate.HasValue)
            query = query.Where(r => r.MinDate <= filter.MaxDate.Value);
        
        if (filter.MinAvgValue.HasValue)
            query = query.Where(r => r.AvgValue >= filter.MinAvgValue.Value);
        if (filter.MaxAvgValue.HasValue)
            query = query.Where(r => r.AvgValue <= filter.MaxAvgValue.Value);
        
        if (filter.MinAvgExecutionTime.HasValue)
            query = query.Where(r => r.AvgExecutionTime >= filter.MinAvgExecutionTime.Value);
        if (filter.MaxAvgExecutionTime.HasValue)
            query = query.Where(r => r.AvgExecutionTime <= filter.MaxAvgExecutionTime.Value);

        var results = await query
            .Select(r => new ResultsDto
            {
                Id = r.Id,
                FileName = r.FileName,
                DeltaSeconds = r.DeltaSeconds,
                MinDate = r.MinDate,
                AvgExecutionTime = r.AvgExecutionTime,
                AvgValue = r.AvgValue,
                MedianValue = r.MedianValue,
                MaxValue = r.MaxValue,
                MinValue = r.MinValue
            })
            .ToListAsync();

        return Ok(results);
    }
    
    public static async Task<IResult> ThirdMethod(
        string fileName,
        [FromServices] DataContext context)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest("Не указано имя файла.");
        
        if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            fileName += ".csv";

        var results = await context.ResultsRecord
            .FirstOrDefaultAsync(r => r.FileName == fileName);

        if (results == null)
            return NotFound($"Файл с именем '{fileName}' не найден.");

        
        var values = await context.ValuesRecord
            .Where(v => v.ResultsId == results.Id)
            .OrderByDescending(v => v.Date)
            .Take(10)
            .Select(v => new ValuesDto
            {
                Id = v.Id,
                Date = v.Date,
                ExecutionTime = v.ExecutionTime,
                Value = v.Value
            })
            .ToListAsync();

        return Ok(values);
    }

    private class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
    }
}