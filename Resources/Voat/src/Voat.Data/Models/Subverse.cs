//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Voat.Data.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Subverse
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Subverse()
        {
            //this.StickiedSubmissions = new HashSet<StickiedSubmission>();
            //this.Submissions = new HashSet<Submission>();
            //this.SubverseModerators = new HashSet<SubverseModerator>();
            //this.SubverseBans = new HashSet<SubverseBan>();
            //this.SubverseFlairs = new HashSet<SubverseFlair>();
        }
        public int ID { get; set; }

        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string SideBar { get; set; }
        public bool IsAdult { get; set; }
        public bool IsThumbnailEnabled { get; set; }
        public bool ExcludeSitewideBans { get; set; }
        public System.DateTime CreationDate { get; set; }
        public string Stylesheet { get; set; }
        public Nullable<int> SubscriberCount { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsAuthorizedOnly { get; set; }
        public Nullable<bool> IsAnonymized { get; set; }
        public Nullable<System.DateTime> LastSubmissionDate { get; set; }
        public int MinCCPForDownvote { get; set; }
        public bool IsAdminPrivate { get; set; }
        public Nullable<bool> IsAdminDisabled { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<System.DateTime> LastUpdateDate { get; set; }



        ////public virtual DefaultSubverse DefaultSubverse { get; set; }
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        //public virtual ICollection<StickiedSubmission> StickiedSubmissions { get; set; }
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        //public virtual ICollection<Submission> Submissions { get; set; }
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        //public virtual ICollection<SubverseModerator> SubverseModerators { get; set; }
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        //public virtual ICollection<SubverseBan> SubverseBans { get; set; }
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        //public virtual ICollection<SubverseFlair> SubverseFlairs { get; set; }
    }
}
