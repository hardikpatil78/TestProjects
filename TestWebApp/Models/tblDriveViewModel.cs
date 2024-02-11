namespace TestWebApp.Models
{
    public class tblDriveViewModel
    {
        public int DriveId { get; set; }
        public string DriveAccount { get; set; }
        public string Token { get; set; }
        public Nullable<System.DateTime> CreatedAt { get; set; }
        public Nullable<int> CreatedBy { get; set; }
        public bool IsActive { get; set; }
        public int fileUploadLimit { get; set; }
        public IFormFile SelectFile { get; set; }
        public List<IFormFile> SelectFiles { get; set; }
        public int ProjectId { get; set; }
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public List<tblDriveViewModel> Drives { get; set; }
        public string FileName { get; set; }
        public string ClientName { get; set; }
    }
}
