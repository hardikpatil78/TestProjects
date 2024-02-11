﻿namespace TestWebApp.Models
{
    public class tblFileViewModel
    {
        public int FileId { get; set; }
        public Nullable<int> DriveId { get; set; }
        public Nullable<int> ProjectId { get; set; }
        public string FileName { get; set; }
        public string FileLocalUrl { get; set; }
        public string FileDriveUrl { get; set; }
        public string DriveFolderId { get; set; }
        public Nullable<bool> IsUplodedOnDrive { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<int> CreatedBy { get; set; }
        public Nullable<System.DateTime> CreatedAt { get; set; }
        public Nullable<System.DateTime> UpdatedAt { get; set; }
        public string Id { get; set; }
        public string DriveAccount { get; set; }
        public string ProjectName { get; set; }
        public string ProjectId_FileName { get; set; }
    }
}
