using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MultiPartFormDataNet.Core.Extensions;
using MultiPartFormDataNet.Core.Tests.FakeApi.Dtos;

namespace MultiPartFormDataNet.Core.Tests.FakeApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult<ResponseDto>> Upload()
        {
            var response = new ResponseDto();
            using (var stream = new MemoryStream())
            {
                response.Request = await this.ReadMultiPartFormData<RequestDto>(stream);
                response.Length = stream.Length;
            }

            return StatusCode(201, response);
        }
    }
}