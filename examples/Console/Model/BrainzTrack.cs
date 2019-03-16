namespace Console.Model
{
    public class BrainzTrack
    {
        public string Title { get; set; }
        public string MBID { get; set; }
        public double Score { get; set; }
        public int Disc { get; set; }
        public int Position { get; set; }
        public string Number { get; set; }
        public int Length { get; set; }
        public string[] AlternateTitles { get; set; }
    }
}
