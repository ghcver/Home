namespace Riskmap.DB
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class DBModel : DbContext
    {
        public DBModel()
            : base("name=DBModel")
        {
        }

        public virtual DbSet<Range> Range { get; set; }
        public virtual DbSet<Range_Backup> Range_Backup { get; set; }
        public virtual DbSet<Risk> Risk { get; set; }
        public virtual DbSet<Risk_Backup> Risk_Backup { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Risk>()
                .HasMany(e => e.Range)
                .WithRequired(e => e.Risk)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Risk_Backup>()
                .HasMany(e => e.Range_Backup)
                .WithRequired(e => e.Risk_Backup)
                .HasForeignKey(e => e.RiskID)
                .WillCascadeOnDelete(false);
        }
    }
}
