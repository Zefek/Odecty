namespace OdectyStat1.Business
{
    public partial class Statistic
    {
        public int Id { get; set; }

        public DateTime? Created { get; set; }

        public double? CreatedTs { get; set; }

        public int? MetadataId { get; set; }

        public DateTime? Start { get; set; }

        public double? StartTs { get; set; }

        public double? Mean { get; set; }

        public double? Min { get; set; }

        public double? Max { get; set; }

        public DateTime? LastReset { get; set; }

        public double? LastResetTs { get; set; }

        public double? State { get; set; }

        public double? Sum { get; set; }

        public double? MeanWeight { get; set; }
    }
}
