using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using DataAccess;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;

namespace TestWorkWebAPI.Tests.Integration
{
    public class FileEndpointsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly IServiceScopeFactory _scopeFactory;

        public FileEndpointsIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
            _scopeFactory = factory.Services.GetRequiredService<IServiceScopeFactory>();
        }

        private async Task CleanDatabase()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            context.ResultsRecord.RemoveRange(context.ResultsRecord);
            context.ValuesRecord.RemoveRange(context.ValuesRecord);
            await context.SaveChangesAsync();
        }

        #region FirstMethod (POST /api/Values/upload)

        [Fact]
        public async Task UploadFile_ValidCsv_ReturnsOk()
        {
            await CleanDatabase();

            var fileContent = "Date;ExecutionTime;Value\n" +
                              "2025-03-20T10-15-30.1234Z;120;45.67\n" +
                              "2025-03-20T10-16-45.5678Z;95;32.12";
            var multipartContent = new MultipartFormDataContent();
            var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
            var fileContentStream = new StreamContent(fileStream);
            fileContentStream.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            multipartContent.Add(fileContentStream, "file", "valid_data.csv");

            var response = await _client.PostAsync("/api/Values/upload", multipartContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Response: {response.StatusCode}\nBody: {errorBody}");
            }

            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            Assert.Contains("успешно обработан", responseBody);
        }

        [Fact]
        public async Task UploadFile_EmptyFile_ReturnsBadRequest()
        {
            await CleanDatabase();

            var multipartContent = new MultipartFormDataContent();
            var emptyStream = new MemoryStream();
            var content = new StreamContent(emptyStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            multipartContent.Add(content, "file", "empty.csv");

            var response = await _client.PostAsync("/api/Values/upload", multipartContent);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("Файл не выбран или пуст", body);
        }

        [Fact]
        public async Task UploadFile_InvalidExtension_ReturnsBadRequest()
        {
            await CleanDatabase();

            var fileContent = "dummy";
            var multipartContent = new MultipartFormDataContent();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
            var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            multipartContent.Add(content, "file", "invalid.txt");

            var response = await _client.PostAsync("/api/Values/upload", multipartContent);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("Допустимы только файлы с расширением .csv", body);
        }

        [Fact]
        public async Task UploadFile_MoreThan10000Lines_ReturnsBadRequest()
        {
            await CleanDatabase();

            var sb = new StringBuilder();
            sb.AppendLine("Date;ExecutionTime;Value");
            for (int i = 0; i < 10001; i++)
                sb.AppendLine($"2025-03-20T10-15-30.1234Z;{i};{i}.5");
            var fileContent = sb.ToString();

            var multipartContent = new MultipartFormDataContent();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
            var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            multipartContent.Add(content, "file", "large.csv");

            var response = await _client.PostAsync("/api/Values/upload", multipartContent);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("должно быть от 1 до 10000", body);
        }

        [Fact]
        public async Task UploadFile_InvalidLine_ReturnsBadRequestAndRollback()
        {
            await CleanDatabase();

            var fileContent = "Date;ExecutionTime;Value\n" +
                              "2025-03-20T10-15-30.1234Z;-5;45.67";
            var multipartContent = new MultipartFormDataContent();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
            var content = new StreamContent(stream);
            content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            multipartContent.Add(content, "file", "invalid.csv");

            var response = await _client.PostAsync("/api/Values/upload", multipartContent);
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("отрицательное время выполнения", body);
        }

        [Fact]
        public async Task UploadFile_OverwriteExistingFile_ReplacesData()
        {
            await CleanDatabase();

            // Первая загрузка
            var fileContent1 = "Date;ExecutionTime;Value\n" +
                               "2025-03-20T10-15-30.1234Z;100;10.0";
            var multipart1 = new MultipartFormDataContent();
            var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(fileContent1));
            var content1 = new StreamContent(stream1);
            content1.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            multipart1.Add(content1, "file", "same.csv");
            var resp1 = await _client.PostAsync("/api/Values/upload", multipart1);
            resp1.EnsureSuccessStatusCode();

            // Вторая загрузка с другими данными
            var fileContent2 = "Date;ExecutionTime;Value\n" +
                               "2025-03-21T10-00-00.0000Z;200;20.0\n" +
                               "2025-03-21T10-01-00.0000Z;300;30.0";
            var multipart2 = new MultipartFormDataContent();
            var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(fileContent2));
            var content2 = new StreamContent(stream2);
            content2.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            multipart2.Add(content2, "file", "same.csv");
            var resp2 = await _client.PostAsync("/api/Values/upload", multipart2);
            resp2.EnsureSuccessStatusCode();

            // Проверяем, что в Results теперь одна запись с новым именем
            var getResp = await _client.GetAsync("/api/Results?FileName=same.csv");
            getResp.EnsureSuccessStatusCode();
            var results = await getResp.Content.ReadFromJsonAsync<List<ResultsDto>>();
            Assert.Single(results);
            Assert.Equal(25.0, results[0].AvgValue);
            Assert.Equal(250, results[0].AvgExecutionTime); // исправлено с 2 на 250
        }

        #endregion

        #region SecondMethod (GET /api/Results)

        [Fact]
        public async Task GetResults_WithFileNameFilter_ReturnsFiltered()
        {
            await CleanDatabase();

            await UploadTestFile("filter_test.csv", new[] { ("2025-03-20T10-00-00.0000Z", "100", "10.0") });

            var response = await _client.GetAsync("/api/Results?FileName=filter_test");
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<List<ResultsDto>>();
            Assert.NotEmpty(data);
            Assert.All(data, r => Assert.Contains("filter_test", r.FileName));
        }

        [Fact]
        public async Task GetResults_WithDateRange_ReturnsFiltered()
        {
            await CleanDatabase();

            await UploadTestFile("file1.csv", new[] { ("2025-03-20T10-00-00.0000Z", "100", "10.0") });
            await UploadTestFile("file2.csv", new[] { ("2025-03-21T10-00-00.0000Z", "200", "20.0") });

            // исправленный формат даты: двоеточия
            var response = await _client.GetAsync("/api/Results?MinDate=2025-03-21T00:00:00.0000Z&MaxDate=2025-03-21T23:59:59.9999Z");
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<List<ResultsDto>>();
            Assert.Single(data);
            Assert.Equal("file2.csv", data[0].FileName);
        }

        [Fact]
        public async Task GetResults_WithAvgValueRange_ReturnsFiltered()
        {
            await CleanDatabase();

            await UploadTestFile("fileA.csv", new[] { ("2025-03-20T10-00-00.0000Z", "100", "10.0") });
            await UploadTestFile("fileB.csv", new[] { ("2025-03-20T10-00-00.0000Z", "200", "30.0") });

            var response = await _client.GetAsync("/api/Results?MinAvgValue=20&MaxAvgValue=40");
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<List<ResultsDto>>();
            Assert.Single(data);
            Assert.Equal(30.0, data[0].AvgValue);
        }

        [Fact]
        public async Task GetResults_NoFilters_ReturnsAll()
        {
            await CleanDatabase();

            await UploadTestFile("file1.csv", new[] { ("2025-03-20T10-00-00.0000Z", "100", "10.0") });
            await UploadTestFile("file2.csv", new[] { ("2025-03-21T10-00-00.0000Z", "200", "20.0") });

            var response = await _client.GetAsync("/api/Results");
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<List<ResultsDto>>();
            Assert.Equal(2, data.Count);
        }

        #endregion

        #region ThirdMethod (GET /api/Values/latest)

        [Fact]
        public async Task GetLatestValues_ExistingFile_ReturnsLast10()
        {
            await CleanDatabase();

            var lines = new List<string>();
            for (int i = 1; i <= 12; i++)
                lines.Add($"2025-03-20T10-{i:D2}-00.0000Z;{i * 10};{(i * 1.5f).ToString(CultureInfo.InvariantCulture)}");
            await UploadTestFile("latest.csv", lines.Select(l => l.Split(';')).Select(p => (p[0], p[1], p[2])).ToArray());

            var response = await _client.GetAsync("/api/Values/latest?fileName=latest.csv");
            response.EnsureSuccessStatusCode();
            var values = await response.Content.ReadFromJsonAsync<List<ValuesDto>>();
            Assert.Equal(10, values.Count);
            for (int i = 0; i < values.Count - 1; i++)
                Assert.True(values[i].Date >= values[i + 1].Date);
        }

        [Fact]
        public async Task GetLatestValues_NonExistentFile_ReturnsNotFound()
        {
            await CleanDatabase();

            var response = await _client.GetAsync("/api/Values/latest?fileName=unknown.csv");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetLatestValues_FileNameWithoutExtension_Works()
        {
            await CleanDatabase();

            await UploadTestFile("test.csv", new[] { ("2025-03-20T10-00-00.0000Z", "100", "10.0") });
            var response = await _client.GetAsync("/api/Values/latest?fileName=test");
            response.EnsureSuccessStatusCode();
            var values = await response.Content.ReadFromJsonAsync<List<ValuesDto>>();
            Assert.Single(values);
        }

        #endregion

        // DTO для десериализации
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

        // Вспомогательный метод для загрузки тестового файла
        private async Task UploadTestFile(string fileName, (string date, string exec, string value)[] rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Date;ExecutionTime;Value");
            foreach (var row in rows)
                sb.AppendLine($"{row.date};{row.exec};{row.value}");
            var content = sb.ToString();

            var multipart = new MultipartFormDataContent();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            multipart.Add(streamContent, "file", fileName);

            var response = await _client.PostAsync("/api/Values/upload", multipart);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Upload failed: {response.StatusCode}\n{errorBody}");
            }
            response.EnsureSuccessStatusCode();
        }
    }
}