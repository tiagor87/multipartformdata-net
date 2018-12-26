using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using MultiPartFormDataNet.Core.Extensions;
using MultiPartFormDataNet.Core.Tests.FakeApi.Dtos;
using MultiPartFormDataNet.Core.Tests.Helpers;
using Newtonsoft.Json;
using Xunit;

namespace MultiPartFormDataNet.Core.Tests
{
    public class MultiPartFormDataExtensionsTests : IClassFixture<IntegrationTestsWebAppFactory>
    {
        private readonly HttpClient _client;

        public MultiPartFormDataExtensionsTests(IntegrationTestsWebAppFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Should_upload_file_and_data_on_multipart_form_data()
        {
            const string text = "Text property";
            const int integer = int.MaxValue;
            var vector = new[] {"Index 0", "Index 1", "Index 2"};
            var uniqueId = Guid.NewGuid();
            var request = new RequestDto
            {
                TextProperty = text,
                IntProperty = integer,
                ArrayProperty = vector,
                UniqueIdProperty = uniqueId,
                ListProperty = vector.ToList()
            };

            var content = new MultipartFormDataContent();
            content.Add(Helper.GetByteArrayContent(1_000), "file", "file.csv");
            content.AddJsonObject(request);

            var response = await _client.PostAsync("/api/upload", content);
            var dto = JsonConvert.DeserializeObject<ResponseDto>(await response.Content.ReadAsStringAsync());

            dto.Should().NotBeNull();
            dto.Request.Should().NotBeNull();
            dto.Request.TextProperty.Should().Be(text);
            dto.Request.IntProperty.Should().Be(integer);
            dto.Request.ArrayProperty.Should().BeEquivalentTo(vector);
            dto.Request.ListProperty.Should().BeEquivalentTo(vector.ToList());
            dto.Length.Should().BeGreaterOrEqualTo(31000);
        }

        [Fact]
        public async Task Should_upload_file_on_multipart_form_data()
        {
            var content = new MultipartFormDataContent();
            content.Add(Helper.GetByteArrayContent(1_000), "file", "file.csv");

            var response = await _client.PostAsync("/api/upload", content);
            var dto = JsonConvert.DeserializeObject<ResponseDto>(await response.Content.ReadAsStringAsync());

            dto.Should().NotBeNull();
            dto.Length.Should().BeGreaterOrEqualTo(31000);
        }
    }
}