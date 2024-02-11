using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System;
using TestWebAPI.Model;

namespace TestWebAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ContestController : ControllerBase
    {
        
        private readonly string[] _permittedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".avi", ".mov", ".wmv", ".flv" };
        private readonly long _fileSizeLimit = 100_000_000; // 100 MB

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] ContestCreateDto contestDto)
        {
            foreach(var file in contestDto.File)
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Please upload a file");
                }

                if (file.Length > _fileSizeLimit)
                {
                    return BadRequest("File size exceeded limit");
                }

                var extension = Path.GetExtension(file.FileName);

                if (!_permittedExtensions.Contains(extension))
                {
                    return BadRequest("Invalid file extension");
                }

                var filePath = Path.Combine("uploads", Guid.NewGuid().ToString() + extension);
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                string bytesStr = string.Join(",", fileBytes);

                dynamic contest = new System.Dynamic.ExpandoObject();

                contest.UserId = "ef126ddb-ba18-467a-8234-474571a7e14d";
                contest.ActivityType = 0;
                contest.IsPrivate = true;
                contest.Title = "Test";
                contest.Description = "Test";
                contest.Location = "Test";
                contest.Duration = 0;
                contest.startDate = DateTime.UtcNow;
                contest.endDate = DateTime.UtcNow.AddHours(1);
                contest.days = 0;
                contest.files = new List<System.Dynamic.ExpandoObject>();

                dynamic filedata = new System.Dynamic.ExpandoObject();
                //file.data = fileBytes;


                return Ok(bytesStr);

            }
            

            return Ok(contestDto);
        }

    }
}
