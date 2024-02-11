using System.ComponentModel.DataAnnotations;

namespace TestWebApp.Data
{
    public partial class tblDrive
    {
        [Key]
        public int DriveId { get; set; }
        public string DriveAccount { get; set; }
        public string Token { get; set; }
        public Nullable<System.DateTime> CreatedAt { get; set; }
        public Nullable<int> CreatedBy { get; set; }
        public bool IsActive { get; set; }
        public int fileUploadLimit { get; set; }
    }
}
