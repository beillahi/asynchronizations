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
    
    public partial class Submission
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Submission()
        {
            this.Comments = new HashSet<Comment>();
            this.SubmissionVoteTrackers = new HashSet<SubmissionVoteTracker>();
            
            //CORE_PORT: Kill relationships... 
            //this.SubmissionSaveTrackers = new HashSet<SubmissionSaveTracker>();
            //this.ViewStatistics = new HashSet<ViewStatistic>();
        }

        public int ID { get; set; }
        public string UserName { get; set; }
        public System.DateTime CreationDate { get; set; }
        public int Type { get; set; }
        public string Title { get; set; }
        public double Rank { get; set; }
        public string Subverse { get; set; }
        public int UpCount { get; set; }
        public int DownCount { get; set; }
        public string Thumbnail { get; set; }
        public Nullable<System.DateTime> LastEditDate { get; set; }
        public string FlairLabel { get; set; }
        public string FlairCss { get; set; }
        public bool IsAnonymized { get; set; }
        public double Views { get; set; }
        public bool IsDeleted { get; set; }
        public string Content { get; set; }
        public double RelativeRank { get; set; }
        public string Url { get; set; }
        public string FormattedContent { get; set; }
        public bool IsAdult { get; set; }
        public Nullable<System.DateTime> ArchiveDate { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Comment> Comments { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<SubmissionVoteTracker> SubmissionVoteTrackers { get; set; }
        public string DomainReversed { get; set; }
        
        //CORE_PORT: Kill relationships... 
        //public virtual StickiedSubmission StickiedSubmission { get; set; }
        //public virtual Subverse Subverse1 { get; set; }
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        //public virtual ICollection<SubmissionSaveTracker> SubmissionSaveTrackers { get; set; }
        //public virtual SubmissionRemovalLog SubmissionRemovalLog { get; set; }
        //[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        //public virtual ICollection<ViewStatistic> ViewStatistics { get; set; }
    }
}
