namespace Riskmap.DB
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Range")]
    public partial class Range
    {
        public long ID { get; set; }

        public long RiskID { get; set; }

        [Column(TypeName = "real")]
        public double PointLatitude { get; set; }

        [Column(TypeName = "real")]
        public double PointLongitude { get; set; }

        public virtual Risk Risk { get; set; }
    }
}
