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
    
    public partial class SubverseSetList
    {
        public int ID { get; set; }
        public int SubverseSetID { get; set; }
        public int SubverseID { get; set; }
        public System.DateTime CreationDate { get; set; }
    
        public virtual SubverseSet SubverseSet { get; set; }
    }
}
