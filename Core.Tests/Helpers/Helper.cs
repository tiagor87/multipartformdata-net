using System.Net.Http;
using System.Text;

namespace MultiPartFormDataNet.Core.Tests.Helpers
{
    public static class Helper
    {
        public static ByteArrayContent GetByteArrayContent(int lineCount)
        {
            const string line = "value1, value2, value3, value4";

            var sb = new StringBuilder();
            for (var i = 0; i < lineCount; i++) sb.AppendLine(line);

            return new ByteArrayContent(Encoding.UTF8.GetBytes(sb.ToString()));
        }
    }
}