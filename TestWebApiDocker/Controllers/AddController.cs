using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TestWebApiDocker.Model;

namespace TestWebApiDocker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AddController : ControllerBase
    {
        public MathOperationDb _mathOperationDb;

        public AddController(MathOperationDb mathOperationDb)
        {
            _mathOperationDb = mathOperationDb;
        }

        [HttpPost]
        public async Task<IActionResult> Post(int num1, int num2)
        {
            try
            {
                Sum sum = new Sum()
                {
                    Num1 = num1,
                    Num2 = num2,
                    Total = num1 + num2
                };

                _mathOperationDb.Add(sum);
                await _mathOperationDb.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
        }
    }
}
