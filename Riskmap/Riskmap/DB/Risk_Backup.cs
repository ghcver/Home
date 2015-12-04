namespace Riskmap.DB
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Risk_Backup
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Risk_Backup()
        {
            Range_Backup = new HashSet<Range_Backup>();
        }

        public long ID { get; set; }

        [Required]
        [StringLength(2147483647)]
        public string Name { get; set; }

        [StringLength(2147483647)]
        public string Status { get; set; }

        [StringLength(2147483647)]
        public string Area { get; set; }

        [StringLength(2147483647)]
        public string CloseDate { get; set; }

        [StringLength(2147483647)]
        public string ConfirmDate { get; set; }

        [StringLength(2147483647)]
        public string Pollutant { get; set; }

        [StringLength(2147483647)]
        public string FinishDate { get; set; }

        [Column(TypeName = "real")]
        public double Latitude { get; set; }

        [Column(TypeName = "real")]
        public double Longitude { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Range_Backup> Range_Backup { get; set; }
    }
}
