using System;
using System.Collections.Generic;

namespace MultiPartFormDataNet.Core.Tests.FakeApi.Dtos
{
    public class RequestDto
    {
        public string TextProperty { get; set; }
        public int IntProperty { get; set; }
        public string[] ArrayProperty { get; set; }
        public Guid UniqueIdProperty { get; set; }
        public List<string> ListProperty { get; set; }
    }
}